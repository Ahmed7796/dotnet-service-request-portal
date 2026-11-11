using ServiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class WorkflowStepTemplateConfiguration : IEntityTypeConfiguration<WorkflowStepTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowStepTemplate> builder)
    {
        builder.ToTable("WorkflowStepTemplates");
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Position).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Scope).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(s => new { s.WorkflowTemplateId, s.Order }).IsUnique();
    }
}
