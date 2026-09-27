using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Application.Common;
using TalentFlow.Application.DTOs.Applications;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Domain.Entities;
using TalentFlow.Infrastructure.Persistence;

namespace TalentFlow.Infrastructure.Services;

/// <summary>
/// Deterministic scoring engine for candidate evaluation.
/// Uses configurable weights and business rules — NOT LLM judgement.
/// </summary>
public class CandidateScoringService : ICandidateScoringService
{
    private readonly AppDbContext _context;

    // Score weights (out of 100 total)
    private const int MandatorySkillsMaxScore = 40;
    private const int PreferredSkillsMaxScore = 20;
    private const int ExperienceMaxScore = 25;
    private const int EducationMaxScore = 10;
    private const int CertificationMaxScore = 5;

    // Thresholds
    private const int InterviewThreshold = 75;
    private const int ManualReviewThreshold = 60;

    public CandidateScoringService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CandidateScoreResult>> CalculateScoreAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        // Load application with all related data
        var application = await _context.Applications
            .Include(a => a.Job)
                .ThenInclude(j => j.SkillRequirements)
                    .ThenInclude(sr => sr.Skill)
            .Include(a => a.CandidateProfile)
                .ThenInclude(cp => cp.Skills)
                    .ThenInclude(cs => cs.Skill)
            .Include(a => a.CandidateProfile)
                .ThenInclude(cp => cp.Experience)
            .Include(a => a.CandidateProfile)
                .ThenInclude(cp => cp.Education)
            .Include(a => a.CandidateProfile)
                .ThenInclude(cp => cp.Documents)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
            return Result<CandidateScoreResult>.NotFound("Application not found.");

        var result = new CandidateScoreResult
        {
            ApplicationId = applicationId,
            CandidateProfileId = application.CandidateProfileId,
            JobId = application.JobId,
            MinimumRequiredYears = application.Job.MinimumExperience
        };

        // 1. Score mandatory skills
        var mandatorySkills = application.Job.SkillRequirements
            .Where(sr => sr.IsMandatory).ToList();
        var candidateSkills = application.CandidateProfile.Skills.ToList();

        result.MandatorySkillMatches = ScoreSkills(
            mandatorySkills, candidateSkills, MandatorySkillsMaxScore,
            out int mandatoryScore);
        result.MandatorySkillsScore = mandatoryScore;

        // 2. Score preferred skills
        var preferredSkills = application.Job.SkillRequirements
            .Where(sr => !sr.IsMandatory).ToList();

        result.PreferredSkillMatches = ScoreSkills(
            preferredSkills, candidateSkills, PreferredSkillsMaxScore,
            out int preferredScore);
        result.PreferredSkillsScore = preferredScore;

        // 3. Score experience
        result.TotalYearsExperience = CalculateTotalExperience(
            application.CandidateProfile.Experience.ToList());
        result.MeetsExperienceRequirement =
            result.TotalYearsExperience >= application.Job.MinimumExperience;

        if (application.Job.MinimumExperience > 0)
        {
            var experienceRatio = Math.Min(
                (double)result.TotalYearsExperience / application.Job.MinimumExperience, 1.5);
            result.ExperienceScore = (int)Math.Round(
                Math.Min(experienceRatio, 1.0) * ExperienceMaxScore);
        }
        else
        {
            result.ExperienceScore = ExperienceMaxScore;
        }

        // 4. Score education
        result.EducationScore = ScoreEducation(
            application.CandidateProfile.Education.ToList());

        // 5. Score certifications/documents
        result.CertificationScore = ScoreCertifications(
            application.CandidateProfile.Documents.ToList());

        // 6. Calculate total
        result.TotalScore = result.MandatorySkillsScore
            + result.PreferredSkillsScore
            + result.ExperienceScore
            + result.EducationScore
            + result.CertificationScore;

        // 7. Determine recommendation
        if (result.TotalScore >= InterviewThreshold)
        {
            result.Recommendation = "EligibleForInterview";
            result.RecommendationReason =
                $"Score {result.TotalScore}/100 meets the interview threshold ({InterviewThreshold}+).";
        }
        else if (result.TotalScore >= ManualReviewThreshold)
        {
            result.Recommendation = "ManualReview";
            result.RecommendationReason =
                $"Score {result.TotalScore}/100 requires manual review ({ManualReviewThreshold}-{InterviewThreshold - 1}).";
        }
        else
        {
            result.Recommendation = "NotRecommended";
            result.RecommendationReason =
                $"Score {result.TotalScore}/100 below minimum threshold ({ManualReviewThreshold}).";
        }

        return Result<CandidateScoreResult>.Success(result);
    }

    private static List<SkillMatch> ScoreSkills(
        List<JobSkillRequirement> requiredSkills,
        List<CandidateSkill> candidateSkills,
        int maxScore,
        out int totalScore)
    {
        var matches = new List<SkillMatch>();
        totalScore = 0;

        if (!requiredSkills.Any())
        {
            totalScore = maxScore;
            return matches;
        }

        int totalWeight = requiredSkills.Sum(s => s.Weight);
        if (totalWeight == 0) totalWeight = 1;

        foreach (var required in requiredSkills)
        {
            var candidateSkill = candidateSkills
                .FirstOrDefault(cs => cs.SkillId == required.SkillId);

            var match = new SkillMatch
            {
                SkillName = required.Skill.Name,
                IsMatched = candidateSkill != null,
                CandidateProficiency = candidateSkill?.ProficiencyLevel,
                CandidateYearsExperience = candidateSkill?.YearsOfExperience ?? 0,
                Weight = required.Weight
            };

            if (candidateSkill != null)
            {
                double proportion = (double)required.Weight / totalWeight;
                match.ScoreAwarded = (int)Math.Round(proportion * maxScore);
                totalScore += match.ScoreAwarded;
            }

            matches.Add(match);
        }

        // Cap at max
        totalScore = Math.Min(totalScore, maxScore);
        return matches;
    }

    private static decimal CalculateTotalExperience(List<CandidateExperience> experiences)
    {
        decimal totalYears = 0;
        foreach (var exp in experiences)
        {
            var endDate = exp.EndDate ?? DateTime.UtcNow;
            var years = (decimal)(endDate - exp.StartDate).TotalDays / 365.25m;
            totalYears += Math.Max(0, years);
        }
        return Math.Round(totalYears, 1);
    }

    private static int ScoreEducation(List<CandidateEducation> education)
    {
        if (!education.Any()) return 0;

        int score = 2; // Base for having any education

        // Higher degree = more points
        foreach (var edu in education)
        {
            var degree = edu.Degree.ToLowerInvariant();
            if (degree.Contains("phd") || degree.Contains("doctorate"))
                score = Math.Max(score, EducationMaxScore);
            else if (degree.Contains("master") || degree.Contains("msc") || degree.Contains("mba"))
                score = Math.Max(score, 8);
            else if (degree.Contains("bachelor") || degree.Contains("bsc") || degree.Contains("beng"))
                score = Math.Max(score, 6);
            else if (degree.Contains("diploma") || degree.Contains("associate"))
                score = Math.Max(score, 4);
        }

        return Math.Min(score, EducationMaxScore);
    }

    private static int ScoreCertifications(List<CandidateDocument> documents)
    {
        var certDocs = documents.Where(d =>
            d.FileType.Equals("Certificate", StringComparison.OrdinalIgnoreCase) ||
            d.FileType.Equals("Certification", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!certDocs.Any()) return 0;

        // 1 point per cert, max 5
        return Math.Min(certDocs.Count, CertificationMaxScore);
    }
}
