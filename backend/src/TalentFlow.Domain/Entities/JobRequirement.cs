using System;
using TalentFlow.Domain.Common;

namespace TalentFlow.Domain.Entities;

public class JobRequirement : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
    public int Weight { get; set; } = 10; // For AI scoring

    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
}
