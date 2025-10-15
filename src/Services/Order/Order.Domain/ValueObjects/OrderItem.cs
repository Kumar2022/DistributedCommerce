namespace Order.Domain.ValueObjects;

/// <summary>
/// Order item value object representing a line item in an order
/// </summary>
public sealed class OrderItem : ValueObject
{
    private OrderItem(
        Guid productId,
        string productName,
        int quantity,
        Money unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public Money UnitPrice { get; }
    public Money TotalPrice => UnitPrice.Multiply(Quantity);

    public static Result<OrderItem> Create(
        Guid productId,
        string productName,
        int quantity,
        Money unitPrice)
    {
        if (productId == Guid.Empty)
            return Result.Failure<OrderItem>(Error.Validation(nameof(ProductId), "Product ID is required"));

        if (string.IsNullOrWhiteSpace(productName))
            return Result.Failure<OrderItem>(Error.Validation(nameof(ProductName), "Product name is required"));

        if (quantity <= 0)
            return Result.Failure<OrderItem>(Error.Validation(nameof(Quantity), "Quantity must be positive"));

        if (unitPrice.Amount <= 0)
            return Result.Failure<OrderItem>(Error.Validation(nameof(UnitPrice), "Unit price must be positive"));

        return Result.Success(new OrderItem(productId, productName.Trim(), quantity, unitPrice));
    }

    public OrderItem UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(newQuantity));

        return new OrderItem(ProductId, ProductName, newQuantity, UnitPrice);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ProductId;
        yield return ProductName;
        yield return Quantity;
        yield return UnitPrice;
    }

    public override string ToString() =>
        $"{ProductName} x {Quantity} @ {UnitPrice} = {TotalPrice}";
}
