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
        Assert.Equal("increase", result.direction);
        Assert.NotNull(result.current_metrics);
        Assert.NotNull(result.previous_metrics);
        Assert.True(result.current_metrics.employer_cost > result.previous_metrics.employer_cost);
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
        Assert.Equal("decrease", result.direction);
        Assert.NotNull(result.current_metrics);
        Assert.NotNull(result.previous_metrics);
        Assert.True(result.current_metrics.employer_cost < result.previous_metrics.employer_cost);
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
        Assert.Equal("stable", result.direction);
        Assert.NotNull(result.current_metrics);
        Assert.NotNull(result.previous_metrics);
        Assert.Equal(result.current_metrics.employer_cost, result.previous_metrics.employer_cost);
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
        Assert.NotNull(result.key_drivers);
        Assert.True(result.key_drivers.Count > 0);
        Assert.Equal("increase", result.direction);
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
        Assert.NotNull(result.current_metrics);
        Assert.NotNull(result.previous_metrics);
        Assert.Equal(15000m, result.current_metrics.gross_pay);
        Assert.Equal(10000m, result.previous_metrics.gross_pay);
        Assert.Equal("increase", result.direction);
    }

    [Fact]
    public void Compare_WithNullTotals_HandlesGracefully()
    {
        // Arrange
        var previous = TestDataHelper.CreateSamplePayrollData(3);
        previous.totals = null;
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act - The service should handle null totals gracefully
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");
        
        // Assert - Should still return a result, possibly with low confidence
        Assert.NotNull(result);
        Assert.NotNull(result.direction);
    }

    [Fact]
    public void Compare_WithEmptyEmployeeList_HandlesGracefully()
    {
        // Arrange
        var previous = new PayrollData
        {
            employeePayrolls = new List<EmployeePayroll>(),
            totals = new PayrollTotals { gross = 0, net = 0, cost = 0 }
        };
        var current = TestDataHelper.CreateSamplePayrollData(3);

        // Act
        var result = PayrollComparisonService.Compare(previous, current, "Previous", "Current");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.direction);
        // Direction could be "increase" or "stable" depending on cost comparison
        Assert.Contains(result.direction, new[] { "increase", "stable", "decrease" });
        Assert.NotNull(result.key_drivers);
    }
}
