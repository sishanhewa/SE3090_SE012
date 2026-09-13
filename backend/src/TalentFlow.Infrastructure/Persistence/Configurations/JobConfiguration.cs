using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(j => j.EmploymentType)
            .HasMaxLength(50);

        builder.Property(j => j.Location)
            .HasMaxLength(200);

        builder.Property(j => j.SalaryMin)
            .HasPrecision(18, 2);

        builder.Property(j => j.SalaryMax)
            .HasPrecision(18, 2);

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.CompanyId);
        builder.HasIndex(j => j.ApplicationDeadline);

        builder.HasMany(j => j.Requirements)
            .WithOne(r => r.Job)
            .HasForeignKey(r => r.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(j => j.SkillRequirements)
            .WithOne(sr => sr.Job)
            .HasForeignKey(sr => sr.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(j => j.Applications)
            .WithOne(a => a.Job)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobRequirementConfiguration : IEntityTypeConfiguration<JobRequirement>
{
    public void Configure(EntityTypeBuilder<JobRequirement> builder)
    {
        builder.HasKey(jr => jr.Id);

        builder.Property(jr => jr.Description)
            .IsRequired()
            .HasMaxLength(500);
    }
}

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Category)
            .HasMaxLength(100);

        builder.HasIndex(s => s.Name)
            .IsUnique();
    }
}

public class JobSkillRequirementConfiguration : IEntityTypeConfiguration<JobSkillRequirement>
{
    public void Configure(EntityTypeBuilder<JobSkillRequirement> builder)
    {
        builder.HasKey(jsr => jsr.Id);

        builder.HasIndex(jsr => new { jsr.JobId, jsr.SkillId })
            .IsUnique();

        builder.HasOne(jsr => jsr.Skill)
            .WithMany(s => s.JobSkillRequirements)
            .HasForeignKey(jsr => jsr.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
