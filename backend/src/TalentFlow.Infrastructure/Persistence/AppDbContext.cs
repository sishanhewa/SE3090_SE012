using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Persistence;

/// <summary>
/// Main database context for the TalentFlow application.
/// Inherits from IdentityDbContext for ASP.NET Identity support.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Company & Job Management
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<CompanyMembership> CompanyMemberships => Set<CompanyMembership>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobRequirement> JobRequirements => Set<JobRequirement>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<JobSkillRequirement> JobSkillRequirements => Set<JobSkillRequirement>();

    // Candidate & Application Management
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<CandidateEducation> CandidateEducation => Set<CandidateEducation>();
    public DbSet<CandidateExperience> CandidateExperience => Set<CandidateExperience>();
    public DbSet<CandidateDocument> CandidateDocuments => Set<CandidateDocument>();
    public DbSet<Domain.Entities.Application> Applications => Set<Domain.Entities.Application>();
    public DbSet<ApplicationHistory> ApplicationHistory => Set<ApplicationHistory>();

    // Interview & Hiring Management
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewPanelMember> InterviewPanelMembers => Set<InterviewPanelMember>();
    public DbSet<InterviewFeedback> InterviewFeedback => Set<InterviewFeedback>();
    public DbSet<HiringDecision> HiringDecisions => Set<HiringDecision>();
    public DbSet<Offer> Offers => Set<Offer>();

    // Employee & Onboarding Management
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeStatusHistory> EmployeeStatusHistory => Set<EmployeeStatusHistory>();
    public DbSet<OnboardingTemplate> OnboardingTemplates => Set<OnboardingTemplate>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();
    public DbSet<EmployeeOnboardingTask> EmployeeOnboardingTasks => Set<EmployeeOnboardingTask>();

    // Workflow (Agentic AI)
    public DbSet<WorkflowExecution> WorkflowExecutions => Set<WorkflowExecution>();
    public DbSet<AgentStep> AgentSteps => Set<AgentStep>();
    public DbSet<ToolCall> ToolCalls => Set<ToolCall>();
    public DbSet<WorkflowValidationResult> WorkflowValidationResults => Set<WorkflowValidationResult>();
    public DbSet<WorkflowApproval> WorkflowApprovals => Set<WorkflowApproval>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Automatically sets CreatedAt/UpdatedAt on save.
    /// </summary>
    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            if (entry.Metadata.FindProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }

            if (entry.State == EntityState.Added && entry.Metadata.FindProperty("CreatedAt") != null)
            {
                entry.Property("CreatedAt").CurrentValue = now;
            }
        }
    }
}
