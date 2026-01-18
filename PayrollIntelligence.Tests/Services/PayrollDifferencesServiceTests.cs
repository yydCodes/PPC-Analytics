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
        Assert.NotNull(result.change_groups);
        var leaveGroup = result.change_groups.FirstOrDefault(g => 
            g.group_title?.Contains("Leave", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(leaveGroup);
        Assert.True(leaveGroup.affected_employees?.Count > 0);
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
        Assert.NotNull(result.change_groups);
        var newEmployeeGroup = result.change_groups.FirstOrDefault(g => 
            g.group_title?.Contains("New Employees", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(newEmployeeGroup);
        Assert.True(newEmployeeGroup.affected_employees?.Count > 0);
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
        Assert.NotNull(result.attention_items);
        Assert.True(result.attention_items.Count > 0);
        var zeroPayItem = result.attention_items.FirstOrDefault(a => 
            a.issue?.Contains("zero", StringComparison.OrdinalIgnoreCase) == true);
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
        Assert.NotNull(result.attention_items);
        var errorItem = result.attention_items.FirstOrDefault(a => 
            a.issue?.Contains("warning", StringComparison.OrdinalIgnoreCase) == true);
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
        Assert.NotNull(result.change_groups);
        var removedGroup = result.change_groups.FirstOrDefault(g => 
            g.group_title?.Contains("Removed", StringComparison.OrdinalIgnoreCase) == true);
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
        Assert.NotNull(result.payroll_overview);
        Assert.NotEmpty(result.payroll_overview.summary);
        Assert.NotEmpty(result.payroll_overview.headcount_change);
        Assert.NotEmpty(result.payroll_overview.gross_pay_trend);
        Assert.NotEmpty(result.payroll_overview.employer_cost_trend);
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
        Assert.NotNull(result.confidence_level);
        Assert.Contains(result.confidence_level, new[] { "high", "medium", "low" });
    }

    [Fact]
    public void AnalyzeKeyDifferences_WithEmptyData_HandlesGracefully()
    {
        // Arrange
        var previous = new PayrollData
        {
            employeePayrolls = new List<EmployeePayroll>(),
            totals = new PayrollTotals { gross = 0, net = 0, cost = 0 }
        };
        var current = new PayrollData
        {
            employeePayrolls = new List<EmployeePayroll>(),
            totals = new PayrollTotals { gross = 0, net = 0, cost = 0 }
        };

        // Act
        var result = PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("low", result.confidence_level);
    }
}
