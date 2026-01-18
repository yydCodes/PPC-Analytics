# Payroll Intelligence Tests

Unit tests for the Payroll Intelligence Assistant application.

## Test Structure

```
PayrollIntelligence.Tests/
├── Helpers/
│   └── TestDataHelper.cs          # Test data factory methods
└── Services/
    ├── PayrollComparisonServiceTests.cs      # Tests for Payroll Summary feature
    ├── PayrollDifferencesServiceTests.cs    # Tests for Detailed Changes feature
    ├── PayrollAnomalyServiceTests.cs        # Tests for Risk & Review feature
    └── PayrollAnalysisServiceTests.cs       # Tests for facade service
```

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run tests with detailed output
```bash
dotnet test --verbosity normal
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~PayrollComparisonServiceTests"
```

### Run tests with code coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Coverage

The test suite covers:

### PayrollComparisonService (Payroll Summary)
- ✅ Increased payroll detection
- ✅ Decreased payroll detection
- ✅ Stable payroll detection
- ✅ New employee identification
- ✅ Percentage change calculations
- ✅ Edge cases (null totals, empty lists)

### PayrollDifferencesService (Detailed Changes)
- ✅ Leave adjustment detection
- ✅ New employee detection
- ✅ Zero pay employee detection
- ✅ Payroll error detection
- ✅ Removed employee detection
- ✅ Payroll overview generation
- ✅ Confidence level assignment

### PayrollAnomalyService (Risk & Review)
- ✅ Significant cost increase detection
- ✅ Large gross pay change detection
- ✅ Zero pay employee detection
- ✅ Payroll error handling
- ✅ Normal changes (no false positives)
- ✅ Summary generation
- ✅ Edge cases

### PayrollAnalysisService (Facade)
- ✅ Static method functionality
- ✅ Delegation to specialized services
- ✅ Multiple historical periods handling
- ✅ Empty historical data handling

## Test Data Helpers

The `TestDataHelper` class provides factory methods for creating test payroll data:

- `CreateSamplePayrollData()` - Basic payroll with configurable employees and amounts
- `CreatePayrollWithLeaveAdjustments()` - Payroll with leave pay and unpaid leave
- `CreatePayrollWithNewEmployees()` - Payroll with new hires
- `CreatePayrollWithZeroPayEmployee()` - Payroll with zero-pay employee
- `CreatePayrollWithErrors()` - Payroll with calculation errors

## Dependencies

- **xUnit** - Testing framework
- **coverlet.collector** - Code coverage collection

Note: Moq and Microsoft.Extensions.Options were removed as they're not currently used. Add them back if you need to create integration tests that mock API dependencies.

## Notes

- Tests focus on static methods that don't require API dependencies
- All tests are unit tests (no external API calls)
- Test data is generated programmatically (no file dependencies)
