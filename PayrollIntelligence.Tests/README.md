# Payroll Intelligence Tests

Unit tests for the Payroll Intelligence Assistant application.

## Test Structure

```
PayrollIntelligence.Tests/
├── Helpers/
│   ├── TestDataHelper.cs          # Test data factory methods
│   └── ServiceTestHelper.cs        # Helper for creating services with test configuration
└── Services/
    ├── PayrollComparisonServiceTests.cs           # Tests for Payroll Summary feature (rule-based)
    ├── PayrollComparisonServiceHybridTests.cs     # Tests for hybrid AI+rule-based scenarios
    ├── PayrollDifferencesServiceTests.cs          # Tests for Detailed Changes feature (rule-based)
    ├── PayrollDifferencesServiceHybridTests.cs   # Tests for hybrid AI+rule-based scenarios
    ├── PayrollAnomalyServiceTests.cs              # Tests for Risk & Review feature (rule-based)
    ├── PayrollAnomalyServiceHybridTests.cs        # Tests for hybrid AI+rule-based scenarios
    ├── PayrollAnalysisServiceTests.cs             # Tests for facade service
    └── AiFallbackTests.cs                         # Tests for AI fallback scenarios
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

### Hybrid AI + Rule-Based Scenarios
- ✅ PayrollComparisonService hybrid components (metrics, notable observations)
- ✅ PayrollDifferencesService hybrid components (overview, attention items)
- ✅ PayrollAnomalyService rule-based fallback
- ✅ AI fallback when AI service is null
- ✅ Rule-based result completeness for hybrid mode
- ✅ All required fields present for hybrid integration

### AI Fallback Scenarios
- ✅ Services work correctly when AI is null
- ✅ Rule-based methods provide all required fields for hybrid mode
- ✅ Multiple historical periods handled correctly
- ✅ Graceful degradation when AI unavailable

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

## Hybrid AI + Rule-Based Testing

The test suite includes comprehensive tests for the hybrid AI + rule-based approach:

### What's Tested
1. **Rule-Based Components**: All static methods that provide rule-based analysis
2. **Hybrid Integration Points**: Fields that are used in hybrid mode (metrics, overview, attention items)
3. **Fallback Behavior**: Services gracefully fall back to rule-based when AI is unavailable
4. **Result Completeness**: All required fields are present for hybrid mode integration

### What Requires Integration Tests
For full end-to-end testing of hybrid scenarios with actual AI:
- Async methods (`ComparePeriodsAsync`, `AnalyzeKeyDifferencesAsync`, `DetectAnomaliesAsync`)
- AI service integration with real or mocked HTTP responses
- Hybrid result building methods (`BuildResultWithAiComparison`, `BuildResultWithAiChangeGroups`)
- AI exception handling and fallback

These would require:
- Mock HTTP client for AI service
- Integration test setup
- Test AI service configuration

The current unit tests ensure that:
- Rule-based components work correctly
- All fields needed for hybrid mode are present
- Services gracefully handle AI unavailability
- Fallback behavior is correct
