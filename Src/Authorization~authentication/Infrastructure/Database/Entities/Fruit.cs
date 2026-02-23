using Authorization_authentication.Infrastructure.Database.Enums;

namespace Authorization_authentication.Infrastructure.Database.Entities;

/// <summary>
/// Represents a fruit entity in the database.
/// </summary>
public class Fruit
{
    /// <summary>
    /// Primary key for the fruit.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of the fruit (enum).
    /// </summary>
    public FruitType FruitType { get; set; }

    /// <summary>
    /// Quantity of the fruit.
    /// </summary>
    public int Quantity { get; set; }
}