namespace Payment.Domain.Enums;

/// <summary>
/// Payment method enumeration
/// </summary>
public enum PaymentMethod
{
    CreditCard = 0,
    DebitCard = 1,
    PayPal = 2,
    BankTransfer = 3,
    Crypto = 4,
    Cash = 5
}
