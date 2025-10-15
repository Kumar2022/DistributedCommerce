#!/bin/bash

# Test Execution Script for Distributed Commerce Microservices
# This script runs unit tests for all services

echo "======================================"
echo "Running Unit Tests"
echo "======================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track results
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

run_test_project() {
    local project_path=$1
    local project_name=$2
    
    echo -e "${YELLOW}Testing: $project_name${NC}"
    echo "--------------------------------------"
    
    if [ -f "$project_path" ]; then
        result=$(dotnet test "$project_path" --verbosity minimal 2>&1)
        echo "$result"
        
        # Extract test results
        if echo "$result" | grep -q "Passed!"; then
            echo -e "${GREEN}✅ $project_name: PASSED${NC}"
            PASSED_TESTS=$((PASSED_TESTS + 1))
        elif echo "$result" | grep -q "Failed!"; then
            echo -e "${RED}❌ $project_name: FAILED${NC}"
            FAILED_TESTS=$((FAILED_TESTS + 1))
        else
            echo -e "${RED}❓ $project_name: BUILD FAILED OR NO TESTS${NC}"
            FAILED_TESTS=$((FAILED_TESTS + 1))
        fi
        TOTAL_TESTS=$((TOTAL_TESTS + 1))
    else
        echo -e "${YELLOW}⚠️  $project_name: NOT FOUND${NC}"
    fi
    
    echo ""
}

# Run all unit test projects
echo "Running Unit Tests..."
echo ""

run_test_project "tests/Unit/Catalog.UnitTests/Catalog.UnitTests.csproj" "Catalog Unit Tests"
run_test_project "tests/Unit/Order.UnitTests/Order.UnitTests.csproj" "Order Unit Tests"
run_test_project "tests/Unit/Payment.UnitTests/Payment.UnitTests.csproj" "Payment Unit Tests"
run_test_project "tests/Unit/Identity.UnitTests/Identity.UnitTests.csproj" "Identity Unit Tests"
run_test_project "tests/Unit/Inventory.UnitTests/Inventory.UnitTests.csproj" "Inventory Unit Tests"
run_test_project "tests/Unit/Shipping.UnitTests/Shipping.UnitTests.csproj" "Shipping Unit Tests"
run_test_project "tests/Unit/Notification.UnitTests/Notification.UnitTests.csproj" "Notification Unit Tests"
run_test_project "tests/Unit/Analytics.UnitTests/Analytics.UnitTests.csproj" "Analytics Unit Tests"

echo "======================================"
echo "Unit Test Summary"
echo "======================================"
echo "Total Projects: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"
echo "======================================"
echo ""

# Integration Tests (if they exist)
echo "======================================"
echo "Running Integration Tests"
echo "======================================"
echo ""

run_test_project "tests/Integration/Catalog.IntegrationTests/Catalog.IntegrationTests.csproj" "Catalog Integration Tests"

echo ""
echo "======================================"
echo "All Tests Complete"
echo "======================================"

# Exit with error code if any tests failed
if [ $FAILED_TESTS -gt 0 ]; then
    exit 1
else
    exit 0
fi
