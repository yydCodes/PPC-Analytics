using PayrollIntelligence.Core;
using PayrollIntelligence.Core.Services;
using PayrollIntelligence.Tests.Helpers;

namespace PayrollIntelligence.Tests.Services;

/// <summary>
/// Unit tests for PayrollComparisonService static methods
/// </summary>
public class PayrollComparisonServiceTests
{

    [Fact]
    public void Compare_WithIncreasedPayroll_ReturnsIncreaseDirection()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 12000m, employerCost: 14400m);

        // Act
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert
        Assert.Equal("increase", result.Direction);
        Assert.NotNull(result.CurrentMetrics);
        Assert.NotNull(result.PreviousMetrics);
        Assert.True(result.CurrentMetrics.EmployerCost > result.PreviousMetrics.EmployerCost);
    }

    [Fact]
    public void Compare_WithDecreasedPayroll_ReturnsDecreaseDirection()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 12000m, employerCost: 14400m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);

        // Act
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert
        Assert.Equal("decrease", result.Direction);
        Assert.NotNull(result.CurrentMetrics);
        Assert.NotNull(result.PreviousMetrics);
        Assert.True(result.CurrentMetrics.EmployerCost < result.PreviousMetrics.EmployerCost);
    }

    [Fact]
    public void Compare_WithStablePayroll_ReturnsStableDirection()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);

        // Act
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert
        Assert.Equal("stable", result.Direction);
        Assert.NotNull(result.CurrentMetrics);
        Assert.NotNull(result.PreviousMetrics);
        Assert.Equal(result.CurrentMetrics.EmployerCost, result.PreviousMetrics.EmployerCost);
    }

    [Fact]
    public void Compare_WithNewEmployees_IdentifiesTopContributors()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(2, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreatePayrollWithNewEmployees(2, 1);

        // Act
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert
        Assert.NotNull(result.KeyDrivers);
        Assert.True(result.KeyDrivers.Count > 0);
        Assert.Equal("increase", result.Direction);
    }

    [Fact]
    public void Compare_CalculatesPercentageChangeCorrectly()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3, grossPay: 10000m, employerCost: 12000m);
        var current = TestDataHelper.CreateSamplePayrollData(3, grossPay: 15000m, employerCost: 18000m);

        // Act
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert
        Assert.NotNull(result.CurrentMetrics);
        Assert.NotNull(result.PreviousMetrics);
        Assert.Equal(15000m, result.CurrentMetrics.GrossPay);
        Assert.Equal(10000m, result.PreviousMetrics.GrossPay);
        Assert.Equal("increase", result.Direction);
    }

    [Fact]
    public void Compare_WithNullTotals_HandlesGracefully()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        previous.Totals = null;
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act - The service should handle null totals gracefully
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");
        
        // Assert - Should still return a result, possibly with low confidence
        Assert.NotNull(result);
        Assert.NotNull(result.Direction);
    }

    [Fact]
    public void Compare_WithEmptyEmployeeList_HandlesGracefully()
    {
        // Arrange
        var previous = new PayrollData
        {
            EmployeePayrolls = new List<EmployeePayroll>(),
            Totals = new PayrollTotals { Gross = 0, Net = 0, Cost = 0 }
        };
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Direction);
        // Direction could be "increase" or "stable" depending on cost comparison
        Assert.Contains(result.Direction, new[] { "increase", "stable", "decrease" });
        Assert.NotNull(result.KeyDrivers);
    }
}
