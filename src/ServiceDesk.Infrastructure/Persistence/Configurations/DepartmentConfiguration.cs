using ServiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Name).IsRequired().HasMaxLength(Department.MaxNameLength);
        builder.HasIndex(d => d.Name).IsUnique();
    }
}
