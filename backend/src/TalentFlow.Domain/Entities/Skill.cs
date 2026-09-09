using System;
using System.Collections.Generic;
using TalentFlow.Domain.Common;

namespace TalentFlow.Domain.Entities;

public class Skill : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }

    public ICollection<JobSkillRequirement> JobSkillRequirements { get; set; } = new List<JobSkillRequirement>();
    public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
}

public class JobSkillRequirement : BaseEntity
{
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;

    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;

    public bool IsMandatory { get; set; } = true;
    public int Weight { get; set; } = 10;
}
