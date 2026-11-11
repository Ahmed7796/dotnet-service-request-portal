using ServiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("ApprovalSteps");
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Position).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Scope).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Decision).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.EscalatedFrom).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Comment).HasMaxLength(ApprovalStep.CommentMaxLength);
        builder.HasIndex(s => new { s.ServiceRequestId, s.Order }).IsUnique();

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.DecidedById).OnDelete(DeleteBehavior.Restrict);
    }
}
