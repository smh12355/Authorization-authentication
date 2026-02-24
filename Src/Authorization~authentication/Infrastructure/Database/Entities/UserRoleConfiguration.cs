using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authorization_authentication.Infrastructure.Database.Entities;

/// <summary>
/// Configuration for the UserRole junction table.
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        // Composite primary key
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.Property(ur => ur.AssignedBy)
            .IsRequired()
            .HasMaxLength(100);

        // Relationships are configured in User and Role configurations
    }
}
