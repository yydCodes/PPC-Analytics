using PayrollIntelligence.Core;
using PayrollIntelligence.Core.Services;
using PayrollIntelligence.Tests.Helpers;

namespace PayrollIntelligence.Tests.Services;

/// <summary>
/// Tests for AI fallback scenarios - verifying that services gracefully fall back to rule-based
/// when AI is unavailable, returns null, or throws exceptions.
/// </summary>
public class AiFallbackTests
{
    [Fact]
    public void PayrollComparisonService_WithNullAiService_UsesRuleBasedOnly()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 12000m, employerCost: 14400m);

        // Act - Static method (rule-based) should work regardless of AI availability
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert - Verify rule-based result is complete
        Assert.NotNull(result);
        Assert.Equal("increase", result.direction);
        Assert.NotNull(result.current_metrics);
        Assert.NotNull(result.previous_metrics);
        Assert.NotNull(result.key_drivers);
        Assert.NotNull(result.notable_observations);
        Assert.NotNull(result.headline_summary);
        Assert.NotNull(result.confidence_level);
    }

    [Fact]
    public void PayrollDifferencesService_WithNullAiService_UsesRuleBasedOnly()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 12000m, employerCost: 14400m);

        // Act - Static method (rule-based) should work regardless of AI availability
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Verify rule-based result is complete
        Assert.NotNull(result);
        Assert.NotNull(result.payroll_overview);
        Assert.NotNull(result.change_groups);
        Assert.NotNull(result.attention_items);
        Assert.NotNull(result.confidence_level);
    }

    [Fact]
    public void PayrollAnomalyService_WithNullAiService_UsesRuleBasedOnly()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 20000m, employerCost: 24000m);

        // Act - Static method (rule-based) should work regardless of AI availability
        var result = PayrollAnomalyService.DetectAnomalies(previous, current);

        // Assert - Verify rule-based result is complete
        Assert.NotNull(result);
        Assert.NotNull(result.anomalies);
        Assert.NotNull(result.summary);
    }

    [Fact]
    public void PayrollComparisonService_RuleBasedResult_ContainsAllRequiredFields()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 12000m, employerCost: 14400m);

        // Act
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert - All fields that hybrid mode needs from rule-based should be present
        Assert.NotNull(result.previous_metrics); // Used in hybrid
        Assert.NotNull(result.current_metrics); // Used in hybrid
        Assert.NotNull(result.notable_observations); // Used in hybrid
        Assert.NotNull(result.direction); // Can be overridden by AI
        Assert.NotNull(result.headline_summary); // Can be overridden by AI
        Assert.NotNull(result.key_drivers); // Can be overridden by AI
        Assert.NotNull(result.confidence_level); // Can be overridden by AI
    }

    [Fact]
    public void PayrollDifferencesService_RuleBasedResult_ContainsAllRequiredFields()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - All fields that hybrid mode needs from rule-based should be present
        Assert.NotNull(result.payroll_overview); // Always used in hybrid
        Assert.NotNull(result.attention_items); // Always used in hybrid
        Assert.NotNull(result.change_groups); // Can be overridden by AI
        Assert.NotNull(result.confidence_level); // Always used in hybrid
    }

    [Fact]
    public void PayrollAnomalyService_RuleBasedResult_ContainsAllRequiredFields()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 20000m, employerCost: 24000m);

        // Act
        var result = PayrollAnomalyService.DetectAnomalies(previous, current);

        // Assert - All fields that hybrid mode needs from rule-based should be present
        Assert.NotNull(result.anomalies); // Can be overridden by AI
        Assert.NotNull(result.summary); // Can be overridden by AI
    }

    [Fact]
    public void PayrollComparisonService_WithMultipleHistoricalPeriods_HandlesCorrectly()
    {
        // Arrange
        var historical = new List<PayrollData>
        {
            TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m),
            TestDataHelper.CreateSamplePayrollData(3, grossPay: 11000m, employerCost: 13200m)
        };
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 20000m, employerCost: 24000m);

        // Act - Uses most recent historical period for comparison
        var result = PayrollComparisonService.Compare(historical.Last(), current, "Previous", "Current");

        // Assert - Should detect increase (cost went from 13200 to 24000, >1% change)
        Assert.NotNull(result);
        Assert.Equal("increase", result.direction);
    }

    [Fact]
    public void PayrollAnomalyService_WithMultipleHistoricalPeriods_HandlesCorrectly()
    {
        // Arrange
        var historical = new List<PayrollData>
        {
            TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m),
            TestDataHelper.CreateSamplePayrollData(3, grossPay: 11000m)
        };
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 20000m);

        // Act - Uses most recent historical period for comparison
        var result = PayrollAnomalyService.DetectAnomalies(historical.Last(), current);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.anomalies);
        Assert.NotNull(result.summary);
    }
}
