using TalentFlow.Application.DTOs.Auth;
using TalentFlow.Application.Common;

namespace TalentFlow.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserInfoResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
