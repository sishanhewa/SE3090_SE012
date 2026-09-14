using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.CandidateProfiles;
using TalentFlow.Application.Interfaces.Repositories;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Services;

public class CandidateProfileService : ICandidateProfileService
{
    private readonly IRepository<CandidateProfile> _profileRepository;

    public CandidateProfileService(IRepository<CandidateProfile> profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result<CandidateProfileResponse>> CreateProfileAsync(Guid userId, CreateCandidateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var existingProfiles = await _profileRepository.FindAsync(p => p.UserId == userId, cancellationToken);
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

        await _profileRepository.AddAsync(profile, cancellationToken);

        return Result<CandidateProfileResponse>.Success(MapToResponse(profile));
    }

    public async Task<Result<CandidateProfileResponse>> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profiles = await _profileRepository.FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();
        
        if (profile == null)
            return Result<CandidateProfileResponse>.NotFound("Candidate profile not found.");

        return Result<CandidateProfileResponse>.Success(MapToResponse(profile));
    }

    public async Task<Result<CandidateProfileResponse>> UpdateProfileAsync(Guid userId, UpdateCandidateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profiles = await _profileRepository.FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
            return Result<CandidateProfileResponse>.NotFound("Candidate profile not found.");

        if (request.Summary != null) profile.Summary = request.Summary;
        if (request.Phone != null) profile.Phone = request.Phone;
        if (request.Address != null) profile.Address = request.Address;
        if (request.LinkedInUrl != null) profile.LinkedInUrl = request.LinkedInUrl;
        if (request.PortfolioUrl != null) profile.PortfolioUrl = request.PortfolioUrl;
        if (request.ProfileImageUrl != null) profile.ProfileImageUrl = request.ProfileImageUrl;

        await _profileRepository.UpdateAsync(profile, cancellationToken);

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
            ProfileImageUrl = profile.ProfileImageUrl
        };
    }
}
