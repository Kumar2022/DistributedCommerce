namespace BuildingBlocks.Authentication.Authorization;

/// <summary>
/// Centralized authorization policy names
/// </summary>
public static class PolicyNames
{
    // Basic policies
    public const string Authenticated = nameof(Authenticated);
    public const string AdminOnly = nameof(AdminOnly);
    public const string CustomerOnly = nameof(CustomerOnly);
    public const string CustomerOrAdmin = nameof(CustomerOrAdmin);
    public const string ServiceAccount = nameof(ServiceAccount);
    
    // Resource-based policies
    public const string ManageOrders = nameof(ManageOrders);
    public const string ManageProducts = nameof(ManageProducts);
    public const string ManageInventory = nameof(ManageInventory);
    public const string ManagePayments = nameof(ManagePayments);
    public const string ManageShipments = nameof(ManageShipments);
    public const string ViewAnalytics = nameof(ViewAnalytics);
    public const string ManageUsers = nameof(ManageUsers);
}

/// <summary>
/// Role constants
/// </summary>
public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string Customer = nameof(Customer);
    public const string ServiceAccount = nameof(ServiceAccount);
    public const string WarehouseManager = nameof(WarehouseManager);
    public const string Analyst = nameof(Analyst);
}

/// <summary>
/// Permission constants for fine-grained authorization
/// </summary>
public static class Permissions
{
    // Order permissions
    public const string OrdersRead = "orders:read";
    public const string OrdersWrite = "orders:write";
    public const string OrdersDelete = "orders:delete";
    public const string OrdersCancel = "orders:cancel";
    
    // Product permissions
    public const string ProductsRead = "products:read";
    public const string ProductsWrite = "products:write";
    public const string ProductsDelete = "products:delete";
    
    // Inventory permissions
    public const string InventoryRead = "inventory:read";
    public const string InventoryWrite = "inventory:write";
    public const string InventoryReserve = "inventory:reserve";
    
    // Payment permissions
    public const string PaymentsRead = "payments:read";
    public const string PaymentsProcess = "payments:process";
    public const string PaymentsRefund = "payments:refund";
    
    // Shipping permissions
    public const string ShipmentsRead = "shipments:read";
    public const string ShipmentsCreate = "shipments:create";
    public const string ShipmentsCancel = "shipments:cancel";
    
    // Analytics permissions
    public const string AnalyticsView = "analytics:view";
    public const string AnalyticsExport = "analytics:export";
    
    // User permissions
    public const string UsersRead = "users:read";
    public const string UsersWrite = "users:write";
    public const string UsersDelete = "users:delete";
}

/// <summary>
/// Claim type constants
/// </summary>
public static class ClaimTypes
{
    public const string UserId = "user_id";
    public const string Username = "username";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = "permission";
    public const string TenantId = "tenant_id";
    public const string ServiceAccount = "service_account";
    public const string ApiKey = "api_key";
    public const string CorrelationId = "correlation_id";
}
