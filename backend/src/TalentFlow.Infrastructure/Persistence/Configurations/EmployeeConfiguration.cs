using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Position)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Unique employee number
        builder.HasIndex(e => e.EmployeeNumber)
            .IsUnique();

        // One-to-one with ApplicationUser
        builder.HasIndex(e => e.UserId)
            .IsUnique();

        builder.HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.StatusHistory)
            .WithOne(sh => sh.Employee)
            .HasForeignKey(sh => sh.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.OnboardingTasks)
            .WithOne(ot => ot.Employee)
            .HasForeignKey(ot => ot.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmployeeStatusHistoryConfiguration : IEntityTypeConfiguration<EmployeeStatusHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeStatusHistory> builder)
    {
        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(sh => sh.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(sh => sh.ChangedBy)
            .HasMaxLength(200);

        builder.Property(sh => sh.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(sh => sh.EmployeeId);
    }
}

public class OnboardingTemplateConfiguration : IEntityTypeConfiguration<OnboardingTemplate>
{
    public void Configure(EntityTypeBuilder<OnboardingTemplate> builder)
    {
        builder.HasKey(ot => ot.Id);

        builder.Property(ot => ot.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ot => ot.Description)
            .HasMaxLength(1000);

        builder.HasOne(ot => ot.Company)
            .WithMany()
            .HasForeignKey(ot => ot.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ot => ot.Tasks)
            .WithOne(t => t.OnboardingTemplate)
            .HasForeignKey(t => t.OnboardingTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OnboardingTaskConfiguration : IEntityTypeConfiguration<OnboardingTask>
{
    public void Configure(EntityTypeBuilder<OnboardingTask> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);
    }
}

public class EmployeeOnboardingTaskConfiguration : IEntityTypeConfiguration<EmployeeOnboardingTask>
{
    public void Configure(EntityTypeBuilder<EmployeeOnboardingTask> builder)
    {
        builder.HasKey(eot => eot.Id);

        builder.Property(eot => eot.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(eot => new { eot.EmployeeId, eot.OnboardingTaskId })
            .IsUnique();

        builder.HasOne(eot => eot.OnboardingTask)
            .WithMany()
            .HasForeignKey(eot => eot.OnboardingTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
