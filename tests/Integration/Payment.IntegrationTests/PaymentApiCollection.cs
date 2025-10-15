using Payment.IntegrationTests.Fixtures;
using Xunit;

namespace Payment.IntegrationTests;

/// <summary>
/// Collection definition for Payment API tests
/// Ensures all tests in this collection share the same fixture instance
/// This prevents multiple containers from being created and improves test performance
/// </summary>
[CollectionDefinition("PaymentApiCollection")]
public class PaymentApiCollection : ICollectionFixture<PaymentApiFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
