using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Persistence.Configurations;

public class WorkflowExecutionConfiguration : IEntityTypeConfiguration<WorkflowExecution>
{
    public void Configure(EntityTypeBuilder<WorkflowExecution> builder)
    {
        builder.HasKey(we => we.Id);

        builder.Property(we => we.Objective)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(we => we.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(we => we.Plan)
            .HasColumnType("jsonb");

        builder.Property(we => we.FinalResult)
            .HasColumnType("jsonb");

        builder.Property(we => we.ErrorDetails)
            .HasMaxLength(5000);

        builder.Property(we => we.RelatedEntityType)
            .HasMaxLength(100);

        builder.HasIndex(we => we.Status);
        builder.HasIndex(we => we.CompanyId);
        builder.HasIndex(we => we.InitiatedById);

        builder.HasOne(we => we.InitiatedBy)
            .WithMany()
            .HasForeignKey(we => we.InitiatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(we => we.AgentSteps)
            .WithOne(s => s.WorkflowExecution)
            .HasForeignKey(s => s.WorkflowExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(we => we.ValidationResults)
            .WithOne(vr => vr.WorkflowExecution)
            .HasForeignKey(vr => vr.WorkflowExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(we => we.Approvals)
            .WithOne(a => a.WorkflowExecution)
            .HasForeignKey(a => a.WorkflowExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AgentStepConfiguration : IEntityTypeConfiguration<AgentStep>
{
    public void Configure(EntityTypeBuilder<AgentStep> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.AgentName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Input)
            .HasColumnType("jsonb");

        builder.Property(s => s.Output)
            .HasColumnType("jsonb");

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.ErrorDetails)
            .HasMaxLength(5000);

        builder.HasIndex(s => s.WorkflowExecutionId);

        builder.HasMany(s => s.ToolCalls)
            .WithOne(tc => tc.AgentStep)
            .HasForeignKey(tc => tc.AgentStepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ToolCallConfiguration : IEntityTypeConfiguration<ToolCall>
{
    public void Configure(EntityTypeBuilder<ToolCall> builder)
    {
        builder.HasKey(tc => tc.Id);

        builder.Property(tc => tc.ToolName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tc => tc.Input)
            .HasColumnType("jsonb");

        builder.Property(tc => tc.Output)
            .HasColumnType("jsonb");

        builder.HasIndex(tc => tc.AgentStepId);
    }
}

public class WorkflowValidationResultConfiguration : IEntityTypeConfiguration<WorkflowValidationResult>
{
    public void Configure(EntityTypeBuilder<WorkflowValidationResult> builder)
    {
        builder.HasKey(vr => vr.Id);

        builder.Property(vr => vr.ValidationType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(vr => vr.Errors)
            .HasColumnType("jsonb");

        builder.Property(vr => vr.Warnings)
            .HasColumnType("jsonb");

        builder.HasIndex(vr => vr.WorkflowExecutionId);
    }
}

public class WorkflowApprovalConfiguration : IEntityTypeConfiguration<WorkflowApproval>
{
    public void Configure(EntityTypeBuilder<WorkflowApproval> builder)
    {
        builder.HasKey(wa => wa.Id);

        builder.Property(wa => wa.RequestedAction)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(wa => wa.Decision)
            .HasMaxLength(30);

        builder.Property(wa => wa.Comments)
            .HasMaxLength(2000);

        builder.HasIndex(wa => wa.WorkflowExecutionId);

        builder.HasOne(wa => wa.DecidedBy)
            .WithMany()
            .HasForeignKey(wa => wa.DecidedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
