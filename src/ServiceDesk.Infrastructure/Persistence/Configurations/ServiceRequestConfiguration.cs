using ServiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Title).IsRequired().HasMaxLength(ServiceRequest.TitleMaxLength);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(ServiceRequest.DescriptionMaxLength);
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.RequesterPosition).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.RowVersion).IsRowVersion();
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RequesterId);
        builder.Ignore(r => r.CurrentStep);

        builder.HasMany(r => r.ApprovalSteps).WithOne().HasForeignKey(s => s.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.ApprovalSteps).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(r => r.AuditLogs).WithOne().HasForeignKey(l => l.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.AuditLogs).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<User>().WithMany().HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(r => r.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
