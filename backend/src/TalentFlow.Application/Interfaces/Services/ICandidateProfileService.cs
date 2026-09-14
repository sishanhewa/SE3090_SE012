using System;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.CandidateProfiles;

namespace TalentFlow.Application.Interfaces.Services;

public interface ICandidateProfileService
{
    Task<Result<CandidateProfileResponse>> CreateProfileAsync(Guid userId, CreateCandidateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<CandidateProfileResponse>> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<CandidateProfileResponse>> UpdateProfileAsync(Guid userId, UpdateCandidateProfileRequest request, CancellationToken cancellationToken = default);
}
