using PayrollIntelligence.Core;
using PayrollIntelligence.Core.Services;
using PayrollIntelligence.Tests.Helpers;

namespace PayrollIntelligence.Tests.Services;

/// <summary>
/// Tests for PayrollDifferencesService hybrid AI + rule-based scenarios
/// </summary>
public class PayrollDifferencesServiceHybridTests
{
    [Fact]
    public void AnalyzeKeyDifferences_WithLeaveAdjustments_ProvidesRuleBasedComponentsForHybrid()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreatePayrollWithLeaveAdjustments(3);

        // Act - This is the rule-based method that provides attention_items and overview for hybrid
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Verify rule-based components used in hybrid mode
        Assert.NotNull(result.PayrollOverview);
        Assert.NotNull(result.AttentionItems);
        Assert.NotNull(result.ChangeGroups);
        Assert.NotNull(result.ConfidenceLevel);
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithNewEmployees_ProvidesChangeGroupsForHybrid()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(2);
        var current = TestDataHelper.CreatePayrollWithNewEmployees(2, 1);

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Change groups can be overridden by AI in hybrid mode
        Assert.NotNull(result.ChangeGroups);
        var newEmployeeGroup = result.ChangeGroups.FirstOrDefault(g => 
            g.GroupTitle?.Contains("New Employees", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(newEmployeeGroup);
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithZeroPayEmployee_ProvidesAttentionItemsForHybrid()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(2);
        var current = TestDataHelper.CreatePayrollWithZeroPayEmployee();

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Attention items are rule-based and always used in hybrid mode
        Assert.NotNull(result.AttentionItems);
        Assert.True(result.AttentionItems.Count > 0);
        var zeroPayItem = result.AttentionItems.FirstOrDefault(a => 
            a.Issue?.Contains("zero", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(zeroPayItem);
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithPayrollErrors_ProvidesAttentionItemsForHybrid()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(2);
        var current = TestDataHelper.CreatePayrollWithErrors();

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Attention items are rule-based
        Assert.NotNull(result.AttentionItems);
        var errorItem = result.AttentionItems.FirstOrDefault(a => 
            a.Issue?.Contains("warning", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(errorItem);
    }

    [Fact]
    public void AnalyzeKeyDifferences_GeneratesPayrollOverview_ForHybrid()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 12000m, employerCost: 14400m);

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Payroll overview is rule-based and always used in hybrid mode
        Assert.NotNull(result.PayrollOverview);
        Assert.NotEmpty(result.PayrollOverview.Summary);
        Assert.NotEmpty(result.PayrollOverview.HeadcountChange);
        Assert.NotEmpty(result.PayrollOverview.GrossPayTrend);
        Assert.NotEmpty(result.PayrollOverview.EmployerCostTrend);
    }

    [Fact]
    public void AnalyzeKeyDifferences_SetsConfidenceLevel_ForHybrid()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Confidence level is rule-based and used in hybrid mode
        Assert.NotNull(result.ConfidenceLevel);
        Assert.Contains(result.ConfidenceLevel, new[] { "high", "medium", "low" });
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithRemovedEmployees_DetectsRemovedGroupForHybrid()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(2); // One less employee

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Change groups can be overridden by AI
        Assert.NotNull(result.ChangeGroups);
        var removedGroup = result.ChangeGroups.FirstOrDefault(g => 
            g.GroupTitle?.Contains("Removed", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(removedGroup);
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithEmptyData_HandlesGracefullyForHybrid()
    {
        // Arrange
        var previous = new PayrollData
        {
            EmployeePayrolls = new List<EmployeePayroll>(),
            Totals = new PayrollTotals { Gross = 0, Net = 0, Cost = 0 }
        };
        var current = new PayrollData
        {
            EmployeePayrolls = new List<EmployeePayroll>(),
            Totals = new PayrollTotals { Gross = 0, Net = 0, Cost = 0 }
        };

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert - Should handle gracefully for hybrid mode
        Assert.NotNull(result);
        Assert.Equal("low", result.ConfidenceLevel);
        Assert.NotNull(result.PayrollOverview);
        Assert.NotNull(result.ChangeGroups);
        Assert.NotNull(result.AttentionItems);
    }
}
