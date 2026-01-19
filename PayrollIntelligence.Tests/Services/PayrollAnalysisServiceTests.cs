using PayrollIntelligence.Core;
using PayrollIntelligence.Core.Services;
using PayrollIntelligence.Tests.Helpers;

namespace PayrollIntelligence.Tests.Services;

/// <summary>
/// Unit tests for PayrollAnalysisService facade
/// </summary>
public class PayrollAnalysisServiceTests
{
    // Note: Async methods that require API calls are integration tests, not unit tests
    // These tests focus on static methods that can be tested without API dependencies

    [Fact]
    public void AnalyzePayrollComparison_StaticMethod_WorksCorrectly()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 12000m, employerCost: 14400m);

        // Act - Use the underlying service method directly
        var result = PayrollComparisonService.Compare(previous, current, "Previous Period", "Current Period");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("increase", result.Direction);
        Assert.NotNull(result.CurrentMetrics);
        Assert.NotNull(result.PreviousMetrics);
        Assert.True(result.CurrentMetrics.EmployerCost > result.PreviousMetrics.EmployerCost);
    }

    [Fact]
    public void AnalyzeKeyDifferences_StaticMethod_WorksCorrectly()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act
        var result = PayrollAnalysisService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.PayrollOverview);
        Assert.NotNull(result.ChangeGroups);
        Assert.NotNull(result.AttentionItems);
    }

    [Fact]
    public void DetectAnomalies_StaticMethod_WorksCorrectly()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 20000m, employerCost: 24000m);

        // Act
        var result = PayrollAnalysisService.DetectAnomalies(previous, current);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Anomalies);
        Assert.NotNull(result.Summary);
    }

    [Fact]
    public void DetectAnomalies_WithMultipleHistorical_WorksCorrectly()
    {
        // Arrange
        var historical = new List<PayrollData>
        {
            TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m),
            TestDataHelper.CreateSamplePayrollData(3, grossPay: 11000m)
        };
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 20000m);

        // Act
        var result = PayrollAnalysisService.DetectAnomalies(historical, current);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Anomalies);
        Assert.NotNull(result.Summary);
    }

    [Fact]
    public void DetectAnomalies_WithEmptyHistorical_ReturnsEmptyResult()
    {
        // Arrange
        var historical = new List<PayrollData>();
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act
        var result = PayrollAnalysisService.DetectAnomalies(historical, current);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Anomalies);
        Assert.Contains("No historical data", result.Summary);
    }
}
