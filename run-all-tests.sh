#!/bin/bash

echo "=================================="
echo "  DISTRIBUTED COMMERCE - TEST RUN"
echo "=================================="
echo ""

echo "🔨 Building solution..."
dotnet build DistributedCommerce.sln --no-incremental -v quiet
if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi
echo "✅ Build successful!"
echo ""

echo "📊 Running Order.UnitTests..."
dotnet test tests/Unit/Order.UnitTests/Order.UnitTests.csproj --verbosity quiet --no-build
echo ""

echo "📊 Running Payment.UnitTests..."
dotnet test tests/Unit/Payment.UnitTests/Payment.UnitTests.csproj --verbosity quiet --no-build  
echo ""

echo "📊 Running Catalog.UnitTests..."
dotnet test tests/Unit/Catalog.UnitTests/Catalog.UnitTests.csproj --verbosity quiet --no-build
echo ""

echo "=================================="
echo "  ✅ TEST RUN COMPLETE"
echo "=================================="
