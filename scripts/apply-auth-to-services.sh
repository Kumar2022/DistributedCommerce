#!/bin/bash

# Script to apply authentication configuration to all services
# This script adds JWT configuration to appsettings.json files

SERVICES=(
    "/Users/omkarkumar/RiderProjects/DistributedCommerce/src/Services/Payment/Payment.API"
    "/Users/omkarkumar/RiderProjects/DistributedCommerce/src/Services/Inventory/Inventory.API"
    "/Users/omkarkumar/RiderProjects/DistributedCommerce/src/Services/Shipping/Shipping.API"
    "/Users/omkarkumar/RiderProjects/DistributedCommerce/src/Services/Notification/Notification.API"
    "/Users/omkarkumar/RiderProjects/DistributedCommerce/src/Services/Catalog/Catalog.API"
    "/Users/omkarkumar/RiderProjects/DistributedCommerce/src/Services/Analytics/Analytics.API"
)

JWT_CONFIG='
  "Jwt": {
    "Secret": "distributed-commerce-super-secret-jwt-key-min-32-characters-long",
    "Issuer": "DistributedCommerce.IdentityService",
    "Audience": "DistributedCommerce.Services",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true,
    "RequireExpirationTime": true,
    "RequireSignedTokens": true,
    "ClockSkewMinutes": 5
  },
  "Services": {
    "Identity": "http://localhost:5001",
    "Order": "http://localhost:5000",
    "Payment": "http://localhost:5002",
    "Inventory": "http://localhost:5003",
    "Shipping": "http://localhost:5004",
    "Notification": "http://localhost:5005",
    "Catalog": "http://localhost:5006",
    "Analytics": "http://localhost:5007"
  }'

echo "Applying authentication configuration to services..."

for SERVICE_PATH in "${SERVICES[@]}"; do
    SERVICE_NAME=$(basename "$SERVICE_PATH" .API)
    APPSETTINGS_FILE="$SERVICE_PATH/appsettings.json"
    
    if [ -f "$APPSETTINGS_FILE" ]; then
        echo "Processing $SERVICE_NAME..."
        
        # Check if JWT config already exists
        if grep -q '"Jwt"' "$APPSETTINGS_FILE"; then
            echo "  ✓ $SERVICE_NAME already has JWT configuration"
        else
            echo "  + Adding JWT configuration to $SERVICE_NAME"
            # This would need a proper JSON merge tool for production
            echo "  Manual update required for: $APPSETTINGS_FILE"
        fi
    else
        echo "  appsettings.json not found for $SERVICE_NAME"
    fi
done

echo ""
echo "Done! Please manually verify the configurations."
