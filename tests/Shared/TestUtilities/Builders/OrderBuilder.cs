using Bogus;

namespace TestUtilities.Builders;

/// <summary>
/// Test data builder for Order entity
/// </summary>
public class OrderBuilder
{
    private readonly Faker _faker = new();
    private Guid _id = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private string _customerName = null!;
    private string _customerEmail = null!;
    private decimal _totalAmount;
    private string _status = "Pending";
    private List<OrderItemTestData> _items = new();

    public OrderBuilder()
    {
        SetDefaults();
    }

    private void SetDefaults()
    {
        _id = Guid.NewGuid();
        _customerId = Guid.NewGuid();
        _customerName = _faker.Name.FullName();
        _customerEmail = _faker.Internet.Email();
        _totalAmount = _faker.Random.Decimal(10, 1000);
        _status = "Pending";
        _items = new List<OrderItemTestData>();
    }

    public OrderBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public OrderBuilder WithCustomerId(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public OrderBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public OrderBuilder WithTotalAmount(decimal amount)
    {
        _totalAmount = amount;
        return this;
    }

    public OrderBuilder WithItem(OrderItemTestData item)
    {
        _items.Add(item);
        return this;
    }

    public OrderBuilder WithItems(params OrderItemTestData[] items)
    {
        _items.AddRange(items);
        return this;
    }

    public OrderTestData Build()
    {
        return new OrderTestData
        {
            Id = _id,
            CustomerId = _customerId,
            CustomerName = _customerName,
            CustomerEmail = _customerEmail,
            TotalAmount = _totalAmount,
            Status = _status,
            Items = _items
        };
    }

    public static OrderBuilder Create() => new();
}

public class OrderTestData
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = null!;
    public string CustomerEmail { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = null!;
    public List<OrderItemTestData> Items { get; init; } = new();
}

public class OrderItemTestData
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice => Quantity * UnitPrice;
}
