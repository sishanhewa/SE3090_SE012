using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentFlow.Domain.Entities;
using ApplicationEntity = TalentFlow.Domain.Entities.Application;

namespace TalentFlow.Infrastructure.Persistence.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.CoverLetter)
            .HasMaxLength(5000);

        builder.Property(a => a.AiScore)
            .HasPrecision(5, 2);

        builder.Property(a => a.AiRecommendation)
            .HasMaxLength(2000);

        // Prevent duplicate applications for the same job by the same candidate
        builder.HasIndex(a => new { a.JobId, a.CandidateProfileId })
            .IsUnique();

        builder.HasIndex(a => a.Status);

        builder.HasMany(a => a.History)
            .WithOne(h => h.Application)
            .HasForeignKey(h => h.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Interviews)
            .WithOne(i => i.Application)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ApplicationHistoryConfiguration : IEntityTypeConfiguration<ApplicationHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationHistory> builder)
    {
        builder.HasKey(ah => ah.Id);

        builder.Property(ah => ah.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(ah => ah.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(ah => ah.ChangedBy)
            .HasMaxLength(200);

        builder.Property(ah => ah.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(ah => ah.ApplicationId);
    }
}
