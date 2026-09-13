using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Persistence.Configurations;

public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.MeetingUrl)
            .HasMaxLength(500);

        builder.Property(i => i.Location)
            .HasMaxLength(200);

        builder.Property(i => i.Notes)
            .HasMaxLength(2000);

        builder.Property(i => i.CalendarEventId)
            .HasMaxLength(200);

        builder.HasIndex(i => i.ScheduledAt);
        builder.HasIndex(i => i.Status);

        builder.HasMany(i => i.PanelMembers)
            .WithOne(pm => pm.Interview)
            .HasForeignKey(pm => pm.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Feedback)
            .WithOne(f => f.Interview)
            .HasForeignKey(f => f.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InterviewPanelMemberConfiguration : IEntityTypeConfiguration<InterviewPanelMember>
{
    public void Configure(EntityTypeBuilder<InterviewPanelMember> builder)
    {
        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.Role)
            .IsRequired()
            .HasMaxLength(50);

        // A user can only be a panel member once per interview
        builder.HasIndex(pm => new { pm.InterviewId, pm.UserId })
            .IsUnique();

        builder.HasOne(pm => pm.User)
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InterviewFeedbackConfiguration : IEntityTypeConfiguration<InterviewFeedback>
{
    public void Configure(EntityTypeBuilder<InterviewFeedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Comments)
            .HasMaxLength(5000);

        builder.Property(f => f.Recommendation)
            .HasMaxLength(20);

        // One feedback per reviewer per interview
        builder.HasIndex(f => new { f.InterviewId, f.ReviewerId })
            .IsUnique();

        builder.HasOne(f => f.Reviewer)
            .WithMany()
            .HasForeignKey(f => f.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class HiringDecisionConfiguration : IEntityTypeConfiguration<HiringDecision>
{
    public void Configure(EntityTypeBuilder<HiringDecision> builder)
    {
        builder.HasKey(hd => hd.Id);

        builder.Property(hd => hd.Decision)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(hd => hd.Reason)
            .HasMaxLength(2000);

        builder.HasIndex(hd => hd.ApplicationId);

        builder.HasOne(hd => hd.Application)
            .WithMany()
            .HasForeignKey(hd => hd.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(hd => hd.DecidedBy)
            .WithMany()
            .HasForeignKey(hd => hd.DecidedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Position)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Salary)
            .HasPrecision(18, 2);

        builder.Property(o => o.EmploymentType)
            .HasMaxLength(50);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.AdditionalTerms)
            .HasMaxLength(5000);

        // One offer per application
        builder.HasIndex(o => o.ApplicationId)
            .IsUnique();

        builder.HasOne(o => o.Application)
            .WithOne(a => a.Offer)
            .HasForeignKey<Offer>(o => o.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.ApprovedBy)
            .WithMany()
            .HasForeignKey(o => o.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
