using ServiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        builder.ToTable("WorkflowTemplates");
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(WorkflowTemplate.NameMaxLength);
        builder.Property(t => t.RequestType).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(t => t.RequestType).IsUnique();

        builder.HasMany(t => t.Steps).WithOne().HasForeignKey(s => s.WorkflowTemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
