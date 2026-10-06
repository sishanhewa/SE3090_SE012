using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Auth;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

/// <summary>
/// JWT authentication service handling registration, login, and token management.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly JwtSettings _jwtSettings;
    private readonly AppDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<JwtSettings> jwtSettings,
        AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtSettings = jwtSettings.Value;
        _context = context;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Conflict("A user with this email already exists.", "USER_EXISTS");
        }

        // Public registration must never grant staff or administrator privileges.
        if (!string.Equals(request.Role, "Candidate", StringComparison.Ordinal))
        {
            return Result<AuthResponse>.Failure("Public registration is available for candidates only.");
        }

        // Ensure role exists
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));
        }

        // Create user
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result<AuthResponse>.Failure($"Registration failed: {errors}");
        }

        // Assign role
        await _userManager.AddToRoleAsync(user, request.Role);

        // Generate tokens
        var authResponse = await GenerateAuthResponseAsync(user);

        return Result<AuthResponse>.Created(authResponse);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.", "INVALID_CREDENTIALS", 401);
        }

        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure("Your account has been deactivated.", "ACCOUNT_INACTIVE", 403);
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.", "INVALID_CREDENTIALS", 401);
        }

        var authResponse = await GenerateAuthResponseAsync(user);

        return Result<AuthResponse>.Success(authResponse);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        // In a production system, you would validate the refresh token against a database.
        // For Sprint 1, we use a simplified approach where the refresh token is
        // a signed JWT with a longer expiry.
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var principal = tokenHandler.ValidateToken(request.RefreshToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Result<AuthResponse>.Failure("Invalid refresh token.", "INVALID_TOKEN", 401);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Result<AuthResponse>.Failure("Invalid refresh token.", "INVALID_TOKEN", 401);
            }

            var authResponse = await GenerateAuthResponseAsync(user);
            return Result<AuthResponse>.Success(authResponse);
        }
        catch (Exception)
        {
            return Result<AuthResponse>.Failure("Invalid or expired refresh token.", "INVALID_TOKEN", 401);
        }
    }

    public async Task<Result<UserInfoResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<UserInfoResponse>.NotFound("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var companyId = await _context.CompanyMemberships.Where(m => m.UserId == user.Id)
            .OrderBy(m => m.CreatedAt).Select(m => (Guid?)m.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);

        var userInfo = new UserInfoResponse
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
            CompanyId = companyId
        };

        return Result<UserInfoResponse>.Success(userInfo);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var companyId = await _context.CompanyMemberships
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => (Guid?)m.CompanyId)
            .FirstOrDefaultAsync();
        var accessToken = GenerateJwtToken(user, roles, _jwtSettings.AccessTokenExpiryMinutes, companyId);
        var refreshToken = GenerateJwtToken(user, roles, _jwtSettings.RefreshTokenExpiryDays * 24 * 60, companyId);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            User = new UserInfoResponse
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                CompanyId = companyId
            }
        };
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles, int expiryMinutes, Guid? companyId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        if (companyId.HasValue)
            claims.Add(new Claim("CompanyId", companyId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
