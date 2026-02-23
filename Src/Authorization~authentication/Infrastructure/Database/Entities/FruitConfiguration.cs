using Authorization_authentication.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authorization_authentication.Infrastructure.Database.Entities;

/// <summary>
/// Configuration for the Fruit entity.
/// </summary>
public class FruitConfiguration : IEntityTypeConfiguration<Fruit>
{
    public void Configure(EntityTypeBuilder<Fruit> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FruitType);
        builder.Property(f => f.Quantity);
        builder.ToTable("fruits");
    }
}
