using PayrollIntelligence.Core;
using PayrollIntelligence.Core.Services;
using PayrollIntelligence.Tests.Helpers;

namespace PayrollIntelligence.Tests.Services;

/// <summary>
/// Unit tests for PayrollDifferencesService static methods
/// </summary>
public class PayrollDifferencesServiceTests
{

    [Fact]
    public void AnalyzeKeyDifferences_WithLeaveAdjustments_DetectsLeaveGroup()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreatePayrollWithLeaveAdjustments(3);

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result.ChangeGroups);
        var leaveGroup = result.ChangeGroups.FirstOrDefault(g => 
            g.GroupTitle?.Contains("Leave", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(leaveGroup);
        Assert.True(leaveGroup.AffectedEmployees?.Count > 0);
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithNewEmployees_DetectsNewEmployeeGroup()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(2);
        var current = TestDataHelper.CreatePayrollWithNewEmployees(2, 1);

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result.ChangeGroups);
        var newEmployeeGroup = result.ChangeGroups.FirstOrDefault(g => 
            g.GroupTitle?.Contains("New Employees", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(newEmployeeGroup);
        Assert.True(newEmployeeGroup.AffectedEmployees?.Count > 0);
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithZeroPayEmployee_AddsAttentionItem()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(2);
        var current = TestDataHelper.CreatePayrollWithZeroPayEmployee();

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result.AttentionItems);
        Assert.True(result.AttentionItems.Count > 0);
        var zeroPayItem = result.AttentionItems.FirstOrDefault(a => 
            a.Issue?.Contains("zero", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(zeroPayItem);
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithPayrollErrors_AddsAttentionItem()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(2);
        var current = TestDataHelper.CreatePayrollWithErrors();

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result.AttentionItems);
        var errorItem = result.AttentionItems.FirstOrDefault(a => 
            a.Issue?.Contains("warning", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(errorItem);
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithRemovedEmployees_DetectsRemovedGroup()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(2); // One less employee

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result.ChangeGroups);
        var removedGroup = result.ChangeGroups.FirstOrDefault(g => 
            g.GroupTitle?.Contains("Removed", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(removedGroup);
    }

    [Fact]
    public void AnalyzeKeyDifferences_GeneratesPayrollOverview()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 12000m, employerCost: 14400m);

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result.PayrollOverview);
        Assert.NotEmpty(result.PayrollOverview.Summary);
        Assert.NotEmpty(result.PayrollOverview.HeadcountChange);
        Assert.NotEmpty(result.PayrollOverview.GrossPayTrend);
        Assert.NotEmpty(result.PayrollOverview.EmployerCostTrend);
    }

    [Fact]
    public void AnalyzeKeyDifferences_SetsConfidenceLevel()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result.ConfidenceLevel);
        Assert.Contains(result.ConfidenceLevel, new[] { "high", "medium", "low" });
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithEmptyData_HandlesGracefully()
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

        // Assert
        Assert.NotNull(result);
        Assert.Equal("low", result.ConfidenceLevel);
    }
}
