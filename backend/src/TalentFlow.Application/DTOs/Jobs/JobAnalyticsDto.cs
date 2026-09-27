namespace TalentFlow.Application.DTOs.Jobs;

/// <summary>
/// Analytics data for a single job posting.
/// </summary>
public class JobAnalyticsResponse
{
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
    public int ShortlistedCandidates { get; set; }
    public int InterviewingCandidates { get; set; }
    public int OfferedCandidates { get; set; }
    public int HiredCandidates { get; set; }
    public int RejectedCandidates { get; set; }
    public int VacancyCount { get; set; }
    public int RemainingVacancies { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public double AverageAiScore { get; set; }
}

/// <summary>
/// Company-wide recruitment dashboard analytics.
/// </summary>
public class DashboardAnalyticsResponse
{
    public int TotalActiveJobs { get; set; }
    public int TotalApplications { get; set; }
    public int PendingInterviews { get; set; }
    public int ActiveEmployees { get; set; }
    public int PendingOnboarding { get; set; }
    public int AiWorkflowsRun { get; set; }
    public List<JobAnalyticsResponse> TopJobsByApplications { get; set; } = new();
}
