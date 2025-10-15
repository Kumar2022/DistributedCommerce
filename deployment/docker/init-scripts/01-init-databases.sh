#!/bin/bash
# Initialize PostgreSQL databases for all services

set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create separate databases for each service
    CREATE DATABASE "IdentityDb";
    CREATE DATABASE "OrderDb";
    CREATE DATABASE "PaymentDb";
    CREATE DATABASE "InventoryDb";
    CREATE DATABASE "CatalogDb";
    CREATE DATABASE "ShippingDb";
    CREATE DATABASE "NotificationDb";
    CREATE DATABASE "AnalyticsDb";

    -- Grant all privileges
    GRANT ALL PRIVILEGES ON DATABASE "IdentityDb" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "OrderDb" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "PaymentDb" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "InventoryDb" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "CatalogDb" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "ShippingDb" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "NotificationDb" TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE "AnalyticsDb" TO $POSTGRES_USER;

    -- Log the created databases
    \l
EOSQL

echo "✅ All databases created successfully!"
