using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.CandidateProfiles;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

public class CandidateProfileService : ICandidateProfileService
{
    private readonly AppDbContext _context;

    public CandidateProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CandidateProfileResponse>> CreateProfileAsync(Guid userId, CreateCandidateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var existingProfiles = await _context.CandidateProfiles.Where(p => p.UserId == userId).ToListAsync(cancellationToken);
        if (existingProfiles.Any())
            return Result<CandidateProfileResponse>.Conflict("User already has a candidate profile.");

        var profile = new CandidateProfile
        {
            UserId = userId,
            Summary = request.Summary,
            Phone = request.Phone,
            Address = request.Address,
            LinkedInUrl = request.LinkedInUrl,
            PortfolioUrl = request.PortfolioUrl,
            ProfileImageUrl = request.ProfileImageUrl
        };

        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CandidateProfileResponse>.Success(MapToResponse(profile));
    }

    public async Task<Result<CandidateProfileResponse>> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _context.CandidateProfiles
            .Include(p => p.Skills).ThenInclude(s => s.Skill)
            .Include(p => p.Experience)
            .Include(p => p.Education)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        
        if (profile == null)
            return Result<CandidateProfileResponse>.NotFound("Candidate profile not found.");

        return Result<CandidateProfileResponse>.Success(MapToResponse(profile));
    }

    public async Task<Result<CandidateProfileResponse>> GetProfileByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await _context.CandidateProfiles
            .Include(p => p.Skills).ThenInclude(s => s.Skill)
            .Include(p => p.Experience)
            .Include(p => p.Education)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        
        if (profile == null)
            return Result<CandidateProfileResponse>.NotFound("Candidate profile not found.");

        return Result<CandidateProfileResponse>.Success(MapToResponse(profile));
    }

    public async Task<Result<CandidateProfileResponse>> UpdateProfileAsync(Guid userId, UpdateCandidateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _context.CandidateProfiles
            .Include(p => p.Skills).ThenInclude(s => s.Skill)
            .Include(p => p.Experience)
            .Include(p => p.Education)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
            return Result<CandidateProfileResponse>.NotFound("Candidate profile not found.");

        if (request.Summary != null) profile.Summary = request.Summary;
        if (request.Phone != null) profile.Phone = request.Phone;
        if (request.Address != null) profile.Address = request.Address;
        if (request.LinkedInUrl != null) profile.LinkedInUrl = request.LinkedInUrl;
        if (request.PortfolioUrl != null) profile.PortfolioUrl = request.PortfolioUrl;
        if (request.ProfileImageUrl != null) profile.ProfileImageUrl = request.ProfileImageUrl;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CandidateProfileResponse>.Success(MapToResponse(profile));
    }

    private static CandidateProfileResponse MapToResponse(CandidateProfile profile)
    {
        return new CandidateProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Summary = profile.Summary,
            Phone = profile.Phone,
            Address = profile.Address,
            LinkedInUrl = profile.LinkedInUrl,
            PortfolioUrl = profile.PortfolioUrl,
            ProfileImageUrl = profile.ProfileImageUrl,
            Skills = profile.Skills?.Select(s => new CandidateSkillDto
            {
                Id = s.Id,
                SkillName = s.Skill?.Name ?? string.Empty,
                YearsOfExperience = s.YearsOfExperience,
                ProficiencyLevel = s.ProficiencyLevel
            }).ToList() ?? new System.Collections.Generic.List<CandidateSkillDto>(),
            Experience = profile.Experience?.Select(e => new CandidateExperienceDto
            {
                Id = e.Id,
                CompanyName = e.CompanyName,
                JobTitle = e.JobTitle,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                IsCurrent = e.IsCurrent
            }).ToList() ?? new System.Collections.Generic.List<CandidateExperienceDto>(),
            Education = profile.Education?.Select(e => new CandidateEducationDto
            {
                Id = e.Id,
                Institution = e.Institution,
                Degree = e.Degree,
                FieldOfStudy = e.FieldOfStudy,
                StartDate = e.StartDate,
                EndDate = e.EndDate
            }).ToList() ?? new System.Collections.Generic.List<CandidateEducationDto>()
        };
    }
}
