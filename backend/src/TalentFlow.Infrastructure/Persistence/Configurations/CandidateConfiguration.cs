using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Persistence.Configurations;

public class CandidateProfileConfiguration : IEntityTypeConfiguration<CandidateProfile>
{
    public void Configure(EntityTypeBuilder<CandidateProfile> builder)
    {
        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.Summary)
            .HasMaxLength(2000);

        builder.Property(cp => cp.Phone)
            .HasMaxLength(20);

        builder.Property(cp => cp.Address)
            .HasMaxLength(500);

        builder.Property(cp => cp.LinkedInUrl)
            .HasMaxLength(500);

        builder.Property(cp => cp.PortfolioUrl)
            .HasMaxLength(500);

        builder.Property(cp => cp.ProfileImageUrl)
            .HasMaxLength(500);

        // One-to-one with ApplicationUser
        builder.HasIndex(cp => cp.UserId)
            .IsUnique();

        builder.HasOne(cp => cp.User)
            .WithOne(u => u.CandidateProfile)
            .HasForeignKey<CandidateProfile>(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cp => cp.Skills)
            .WithOne(cs => cs.CandidateProfile)
            .HasForeignKey(cs => cs.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cp => cp.Education)
            .WithOne(ce => ce.CandidateProfile)
            .HasForeignKey(ce => ce.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cp => cp.Experience)
            .WithOne(cx => cx.CandidateProfile)
            .HasForeignKey(cx => cx.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cp => cp.Documents)
            .WithOne(cd => cd.CandidateProfile)
            .HasForeignKey(cd => cd.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cp => cp.Applications)
            .WithOne(a => a.CandidateProfile)
            .HasForeignKey(a => a.CandidateProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CandidateSkillConfiguration : IEntityTypeConfiguration<CandidateSkill>
{
    public void Configure(EntityTypeBuilder<CandidateSkill> builder)
    {
        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.ProficiencyLevel)
            .HasMaxLength(20);

        builder.HasIndex(cs => new { cs.CandidateProfileId, cs.SkillId })
            .IsUnique();

        builder.HasOne(cs => cs.Skill)
            .WithMany(s => s.CandidateSkills)
            .HasForeignKey(cs => cs.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CandidateEducationConfiguration : IEntityTypeConfiguration<CandidateEducation>
{
    public void Configure(EntityTypeBuilder<CandidateEducation> builder)
    {
        builder.HasKey(ce => ce.Id);

        builder.Property(ce => ce.Institution)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ce => ce.Degree)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ce => ce.FieldOfStudy)
            .HasMaxLength(200);

        builder.Property(ce => ce.Grade)
            .HasMaxLength(20);
    }
}

public class CandidateExperienceConfiguration : IEntityTypeConfiguration<CandidateExperience>
{
    public void Configure(EntityTypeBuilder<CandidateExperience> builder)
    {
        builder.HasKey(cx => cx.Id);

        builder.Property(cx => cx.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cx => cx.JobTitle)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cx => cx.Description)
            .HasMaxLength(2000);
    }
}

public class CandidateDocumentConfiguration : IEntityTypeConfiguration<CandidateDocument>
{
    public void Configure(EntityTypeBuilder<CandidateDocument> builder)
    {
        builder.HasKey(cd => cd.Id);

        builder.Property(cd => cd.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(cd => cd.FileUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(cd => cd.FileType)
            .IsRequired()
            .HasMaxLength(50);
    }
}
