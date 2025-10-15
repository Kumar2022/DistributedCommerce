using Bogus;

namespace TestUtilities.Builders;

/// <summary>
/// Test data builder for Product entity
/// </summary>
public class ProductBuilder
{
    private readonly Faker _faker = new();
    private Guid _id = Guid.NewGuid();
    private string _name = null!;
    private string _description = null!;
    private decimal _price;
    private int _stock;
    private Guid _categoryId = Guid.NewGuid();

    public ProductBuilder()
    {
        SetDefaults();
    }

    private void SetDefaults()
    {
        _id = Guid.NewGuid();
        _name = _faker.Commerce.ProductName();
        _description = _faker.Commerce.ProductDescription();
        _price = decimal.Parse(_faker.Commerce.Price());
        _stock = _faker.Random.Int(0, 1000);
        _categoryId = Guid.NewGuid();
    }

    public ProductBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ProductBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ProductBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public ProductBuilder WithStock(int stock)
    {
        _stock = stock;
        return this;
    }

    public ProductBuilder WithCategoryId(Guid categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public ProductBuilder WithNoStock()
    {
        _stock = 0;
        return this;
    }

    public ProductBuilder WithNegativePrice()
    {
        _price = -10;
        return this;
    }

    public ProductTestData Build()
    {
        return new ProductTestData
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Price = _price,
            Stock = _stock,
            CategoryId = _categoryId
        };
    }

    public static ProductBuilder Create() => new();
}

public class ProductTestData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public Guid CategoryId { get; init; }
}
