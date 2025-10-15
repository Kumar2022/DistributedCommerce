#!/bin/bash

# Run All Tests Script
# Executes unit, integration, and load tests

set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_header() { echo -e "${BLUE}=== $1 ===${NC}"; }
print_success() { echo -e "${GREEN}✓ $1${NC}"; }
print_error() { echo -e "${RED}✗ $1${NC}"; }
print_warning() { echo -e "${YELLOW}⚠ $1${NC}"; }

# Test results
UNIT_TESTS_PASSED=0
INTEGRATION_TESTS_PASSED=0
LOAD_TESTS_PASSED=0

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║          DISTRIBUTED COMMERCE - TEST SUITE                  ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# Phase 1: Unit Tests
print_header "Running Unit Tests"
if dotnet test tests/Unit/ --configuration Release --logger "console;verbosity=normal"; then
    print_success "Unit tests passed"
    UNIT_TESTS_PASSED=1
else
    print_error "Unit tests failed"
fi

echo ""

# Phase 2: Integration Tests
print_header "Running Integration Tests"
print_warning "Starting Docker containers..."

if dotnet test tests/Integration/ --configuration Release --logger "console;verbosity=normal"; then
    print_success "Integration tests passed"
    INTEGRATION_TESTS_PASSED=1
else
    print_error "Integration tests failed"
fi

echo ""

# Phase 3: Load Tests (optional)
if command -v k6 &> /dev/null; then
    print_header "Running Load Tests"
    
    read -p "Run load tests? (y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        if k6 run tests/Load/k6/catalog-load.js; then
            print_success "Load tests passed"
            LOAD_TESTS_PASSED=1
        else
            print_error "Load tests failed"
        fi
    else
        print_warning "Skipping load tests"
        LOAD_TESTS_PASSED=-1
    fi
else
    print_warning "k6 not installed, skipping load tests"
    LOAD_TESTS_PASSED=-1
fi

echo ""

# Summary
print_header "Test Summary"

if [ $UNIT_TESTS_PASSED -eq 1 ]; then
    print_success "Unit Tests: PASSED"
else
    print_error "Unit Tests: FAILED"
fi

if [ $INTEGRATION_TESTS_PASSED -eq 1 ]; then
    print_success "Integration Tests: PASSED"
else
    print_error "Integration Tests: FAILED"
fi

if [ $LOAD_TESTS_PASSED -eq 1 ]; then
    print_success "Load Tests: PASSED"
elif [ $LOAD_TESTS_PASSED -eq -1 ]; then
    print_warning "Load Tests: SKIPPED"
else
    print_error "Load Tests: FAILED"
fi

echo ""

# Code Coverage
print_header "Generating Code Coverage Report"
if dotnet test tests/Unit/ /p:CollectCoverage=true /p:CoverletOutputFormat=opencover; then
    print_success "Coverage report generated"
    
    if command -v reportgenerator &> /dev/null; then
        reportgenerator -reports:**/coverage.opencover.xml -targetdir:./coverage-report -reporttypes:Html
        print_success "HTML coverage report: ./coverage-report/index.html"
    else
        print_warning "Install reportgenerator for HTML reports: dotnet tool install -g dotnet-reportgenerator-globaltool"
    fi
fi

# Exit code
if [ $UNIT_TESTS_PASSED -eq 1 ] && [ $INTEGRATION_TESTS_PASSED -eq 1 ]; then
    echo ""
    print_success "All tests passed! ✨"
    exit 0
else
    echo ""
    print_error "Some tests failed!"
    exit 1
fi
