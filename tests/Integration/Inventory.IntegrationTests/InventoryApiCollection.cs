using Inventory.IntegrationTests.Fixtures;

namespace Inventory.IntegrationTests;

/// <summary>
/// Collection definition for Inventory API integration tests
/// Ensures test fixture is shared across test classes but with proper isolation
/// </summary>
[CollectionDefinition("InventoryApiCollection")]
public class InventoryApiCollection : ICollectionFixture<InventoryApiFixture>
{
    // This class has no code, and is never instantiated.
    // Its purpose is simply to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
