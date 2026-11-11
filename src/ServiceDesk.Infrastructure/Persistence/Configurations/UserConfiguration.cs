using ServiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength);
        builder.Property(u => u.Position).HasMaxLength(30).HasConversion<string>();
        builder.Property(u => u.Role).HasMaxLength(30).HasConversion<string>();
        builder.Property(u => u.DisplayName).HasMaxLength(User.DisplayNameMaxLength);
        builder.Property(u => u.PasswordHash).HasMaxLength(256);
        builder.Property(u => u.EmployeeNumber).HasMaxLength(User.EmployeeNumberLength);
        builder.HasIndex(u => u.EmployeeNumber).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Ignore(u => u.IsApprover);

        builder.HasOne<Department>().WithMany().HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
