using TalentFlow.Application.Common;
using TalentFlow.Domain.Enums;

namespace TalentFlow.Application.DTOs.Jobs;

public class JobSearchParams : PaginationParams
{
    public JobStatus? Status { get; set; }
    public string? Department { get; set; }
    public string? EmploymentType { get; set; }
}
