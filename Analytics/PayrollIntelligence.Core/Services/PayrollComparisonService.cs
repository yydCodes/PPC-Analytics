using Microsoft.Extensions.Options;

namespace PayrollIntelligence.Core.Services;

/// <summary>
/// Service for Payroll Summary feature.
/// Compares total payroll costs, headcount changes, and identifies key drivers.
/// 
/// Uses these context fields for analysis:
/// - totals.net, totals.gross, totals.cost, totalCount
/// - employeePayrolls[].payrollItems[].amount (salary stability)
/// - employeePayrolls[].unpaidLeavePayrollItems
/// - employeePayrolls[].leavePayPayrollItem
/// - employeePayrolls[].error.message
/// 
/// Ignores: Individual tax math, exact statutory formulas, bank payment data
/// </summary>
public class PayrollComparisonService
{
    private readonly PayrollApiService _apiService;
    private readonly ApiConfiguration _apiConfig;

    public PayrollComparisonService(PayrollApiService apiService, IOptions<ApiConfiguration> apiConfig)
    {
        _apiService = apiService;
        _apiConfig = apiConfig.Value;
    }

    /// <summary>
    /// Analyzes payroll comparison between two periods using API data.
    /// </summary>
    public async Task<PayrollComparisonResult> ComparePeriodsAsync(
        int previousYear, int previousMonth,
        int currentYear, int currentMonth)
    {
        await EnsureAuthenticatedAsync();

        var previous = await _apiService.GetPayrollDataAsync(previousYear, previousMonth);
        var current = await _apiService.GetPayrollDataAsync(currentYear, currentMonth);

        if (previous == null || current == null)
        {
            throw new InvalidOperationException("Failed to retrieve payroll data from API");
        }

        var previousPeriodName = $"{GetMonthName(previousMonth)} {previousYear}";
        var currentPeriodName = $"{GetMonthName(currentMonth)} {currentYear}";

        return Compare(previous, current, previousPeriodName, currentPeriodName);
    }

    /// <summary>
    /// Compares two payroll periods and returns analysis result.
    /// </summary>
    public static PayrollComparisonResult Compare(
        PayrollData previous, 
        PayrollData current,
        string previousPeriodName = "Previous Period",
        string currentPeriodName = "Current Period")
    {
        var keyDrivers = new List<string>();
        var notableObservations = new List<string>();

        // ============================================
        // EXTRACT CORE METRICS (from totals)
        // ============================================
        var prevNet = previous.totals?.net ?? 0;
        var currNet = current.totals?.net ?? 0;
        var prevGross = previous.totals?.gross ?? 0;
        var currGross = current.totals?.gross ?? 0;
        var prevCost = previous.totals?.cost ?? 0;
        var currCost = current.totals?.cost ?? 0;
        
        // Use totalCount field, fallback to counting employeePayrolls
        var prevCount = previous.totalCount > 0 ? previous.totalCount : (previous.employeePayrolls?.Count ?? 0);
        var currCount = current.totalCount > 0 ? current.totalCount : (current.employeePayrolls?.Count ?? 0);

        // Calculate changes
        var costChange = currCost - prevCost;
        var grossChange = currGross - prevGross;
        var netChange = currNet - prevNet;
        var headcountChange = currCount - prevCount;

        // Determine direction based on employer cost
        var direction = DetermineDirection(costChange, prevCost);

        // ============================================
        // ANALYZE EMPLOYEE-LEVEL DATA
        // ============================================
        var employeeAnalysis = AnalyzeEmployeePayrollItems(previous, current);
        var leaveAnalysis = AnalyzeLeaveData(previous, current);
        var errorAnalysis = AnalyzePayrollErrors(previous, current);

        // ============================================
        // KEY DRIVERS ANALYSIS
        // ============================================

        // 1. Headcount impact
        if (headcountChange != 0)
        {
            if (headcountChange > 0)
            {
                keyDrivers.Add($"Headcount increased by {headcountChange} employee(s), contributing to higher payroll costs");
            }
            else
            {
                keyDrivers.Add($"Headcount decreased by {Math.Abs(headcountChange)} employee(s), reducing overall payroll costs");
            }
        }
        else if (Math.Abs(costChange) > 100)
        {
            keyDrivers.Add("Payroll costs changed while employee headcount remained unchanged");
        }

        // 2. Gross pay analysis (without headcount change)
        if (Math.Abs(grossChange) > 100 && headcountChange == 0)
        {
            var grossChangeDesc = grossChange > 0 ? "increased" : "decreased";
            keyDrivers.Add($"Gross pay {grossChangeDesc} despite stable headcount, suggesting changes in variable components");
        }

        // 3. Individual employee gross changes
        if (employeeAnalysis.employeesWithLowerGross > 0 && grossChange < 0)
        {
            keyDrivers.Add($"{employeeAnalysis.employeesWithLowerGross} employee(s) had lower gross amounts compared to the previous month");
        }
        else if (employeeAnalysis.employeesWithHigherGross > 0 && grossChange > 0)
        {
            keyDrivers.Add($"{employeeAnalysis.employeesWithHigherGross} employee(s) had higher gross amounts compared to the previous month");
        }

        // 4. Leave-related analysis
        if (leaveAnalysis.hasSignificantLeavePayChange)
        {
            if (leaveAnalysis.leavePayChange < 0)
            {
                keyDrivers.Add("Leave pay amounts decreased compared to the previous period");
            }
            else
            {
                keyDrivers.Add("Leave pay amounts increased in the current period");
            }
        }

        if (leaveAnalysis.hasSignificantUnpaidLeaveChange)
        {
            if (leaveAnalysis.unpaidLeaveChange > 0)
            {
                keyDrivers.Add($"Unpaid leave deductions increased, affecting {leaveAnalysis.employeesWithUnpaidLeave} employee(s)");
            }
            else
            {
                keyDrivers.Add("Unpaid leave deductions from the previous period were reduced or removed");
            }
        }

        // 5. Combined leave impact
        if (leaveAnalysis.hasSignificantLeavePayChange || leaveAnalysis.hasSignificantUnpaidLeaveChange)
        {
            if (!keyDrivers.Any(k => k.Contains("Leave")))
            {
                keyDrivers.Add("Leave-related adjustments present in the previous period were reduced or absent in the current period");
            }
        }

        // ============================================
        // NOTABLE OBSERVATIONS
        // ============================================

        // 1. Cost follows gross trend
        if (Math.Abs(grossChange) > 100)
        {
            var grossTrend = grossChange > 0 ? "upward" : "downward";
            var costTrend = costChange > 0 ? "upward" : "downward";
            if (grossTrend == costTrend)
            {
                notableObservations.Add($"Overall payroll cost followed the same {costTrend} trend as gross pay");
            }
        }

        // 2. Base salary stability (from payrollItems[].amount analysis)
        if (employeeAnalysis.baseSalaryStable && Math.Abs(grossChange) > 100)
        {
            notableObservations.Add("Most base salaries appear consistent, suggesting changes were driven by variable components rather than structural salary changes");
        }
        else if (!employeeAnalysis.baseSalaryStable && employeeAnalysis.employeesWithSalaryChange > 0)
        {
            notableObservations.Add($"{employeeAnalysis.employeesWithSalaryChange} employee(s) had base salary changes this period");
        }

        // 3. Net-to-gross ratio (deduction pattern indicator)
        var prevRatio = prevGross > 0 ? prevNet / prevGross : 0;
        var currRatio = currGross > 0 ? currNet / currGross : 0;
        if (Math.Abs(currRatio - prevRatio) > 0.02m)
        {
            notableObservations.Add("The proportion of net pay to gross pay shifted, indicating changes in deduction patterns");
        }

        // 4. Payroll errors/warnings (from error.message)
        if (errorAnalysis.hasErrors)
        {
            if (errorAnalysis.prevErrorCount > 0 && errorAnalysis.currErrorCount > 0)
            {
                notableObservations.Add("Payroll calculation warnings were present in both periods");
            }
            else if (errorAnalysis.currErrorCount > 0 && errorAnalysis.prevErrorCount == 0)
            {
                notableObservations.Add($"New payroll calculation warnings detected ({errorAnalysis.currErrorCount} employee(s) affected)");
            }
            else if (errorAnalysis.prevErrorCount > 0 && errorAnalysis.currErrorCount == 0)
            {
                notableObservations.Add("Payroll calculation warnings from previous period have been resolved");
            }
        }

        // 5. New hires or terminations
        if (employeeAnalysis.newEmployees > 0)
        {
            notableObservations.Add($"{employeeAnalysis.newEmployees} new employee(s) added to payroll this period");
        }
        if (employeeAnalysis.exitedEmployees > 0)
        {
            notableObservations.Add($"{employeeAnalysis.exitedEmployees} employee(s) no longer in payroll this period");
        }

        // 6. Leave pay activity
        if (leaveAnalysis.employeesWithLeavePay > 0)
        {
            notableObservations.Add($"{leaveAnalysis.employeesWithLeavePay} employee(s) received leave pay in the current period");
        }

        // Generate headline summary
        var headlineSummary = GenerateHeadlineSummary(
            direction, costChange, grossChange, headcountChange, prevCost, 
            leaveAnalysis, employeeAnalysis);

        // Determine confidence level
        var confidenceLevel = DetermineConfidenceLevel(previous, current, errorAnalysis);

        return new PayrollComparisonResult
        {
            direction = direction,
            headline_summary = headlineSummary,
            key_drivers = keyDrivers.Take(5).ToList(),
            notable_observations = notableObservations.Take(5).ToList(),
            confidence_level = confidenceLevel,
            previous_metrics = new PayrollMetrics
            {
                net_pay = prevNet,
                gross_pay = prevGross,
                employer_cost = prevCost,
                headcount = prevCount,
                period_name = previousPeriodName
            },
            current_metrics = new PayrollMetrics
            {
                net_pay = currNet,
                gross_pay = currGross,
                employer_cost = currCost,
                headcount = currCount,
                period_name = currentPeriodName
            }
        };
    }

    #region Analysis Helper Classes

    private class EmployeeAnalysisResult
    {
        public int employeesWithHigherGross { get; set; }
        public int employeesWithLowerGross { get; set; }
        public bool baseSalaryStable { get; set; }
        public int employeesWithSalaryChange { get; set; }
        public int newEmployees { get; set; }
        public int exitedEmployees { get; set; }
    }

    private class LeaveAnalysisResult
    {
        public decimal leavePayChange { get; set; }
        public decimal unpaidLeaveChange { get; set; }
        public bool hasSignificantLeavePayChange { get; set; }
        public bool hasSignificantUnpaidLeaveChange { get; set; }
        public int employeesWithLeavePay { get; set; }
        public int employeesWithUnpaidLeave { get; set; }
    }

    private class ErrorAnalysisResult
    {
        public int prevErrorCount { get; set; }
        public int currErrorCount { get; set; }
        public bool hasErrors => prevErrorCount > 0 || currErrorCount > 0;
    }

    #endregion

    #region Private Helper Methods

    private static string DetermineDirection(decimal change, decimal previousTotal)
    {
        var percentChange = previousTotal > 0 ? Math.Abs(change / previousTotal) * 100 : 0;
        
        // Consider stable if change is less than 1%
        if (percentChange < 1)
            return "stable";
        
        return change > 0 ? "increase" : "decrease";
    }

    private static string GenerateHeadlineSummary(
        string direction, 
        decimal costChange, 
        decimal grossChange, 
        int headcountChange,
        decimal previousCost,
        LeaveAnalysisResult leaveAnalysis,
        EmployeeAnalysisResult employeeAnalysis)
    {
        var percentChange = previousCost > 0 ? Math.Abs(costChange / previousCost) * 100 : 0;
        var magnitude = percentChange > 5 ? "significantly" : "slightly";

        if (direction == "stable")
        {
            return "Total payroll remained stable compared to the previous period with minimal change.";
        }

        var directionWord = direction == "increase" ? "increased" : "decreased";

        // Determine primary driver for headline
        if (headcountChange != 0)
        {
            var headcountDesc = headcountChange > 0 ? "additional employees" : "fewer employees";
            return $"Total payroll {directionWord} {magnitude} compared to last month, primarily driven by {Math.Abs(headcountChange)} {headcountDesc}.";
        }
        else if (leaveAnalysis.hasSignificantLeavePayChange || leaveAnalysis.hasSignificantUnpaidLeaveChange)
        {
            return $"Total payroll {directionWord} {magnitude} compared to last month, primarily driven by leave-related adjustments.";
        }
        else if (!employeeAnalysis.baseSalaryStable && employeeAnalysis.employeesWithSalaryChange > 0)
        {
            return $"Total payroll {directionWord} {magnitude} compared to last month, driven by salary changes for {employeeAnalysis.employeesWithSalaryChange} employee(s).";
        }
        else if (Math.Abs(grossChange) > Math.Abs(costChange) * 0.5m)
        {
            return $"Total payroll {directionWord} {magnitude} compared to last month, primarily driven by {(grossChange > 0 ? "higher" : "lower")} gross pay rather than changes in headcount.";
        }
        else
        {
            return $"Total payroll {directionWord} {magnitude} compared to last month.";
        }
    }

    /// <summary>
    /// Analyzes employee payroll items for salary stability and gross changes.
    /// Uses employeePayrolls[].payrollItems[].amount for analysis.
    /// </summary>
    private static EmployeeAnalysisResult AnalyzeEmployeePayrollItems(PayrollData previous, PayrollData current)
    {
        var prevEmployees = previous.employeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.employeeId))
            .ToDictionary(e => e.employeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();
        
        var currEmployees = current.employeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.employeeId))
            .ToDictionary(e => e.employeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        int higherGross = 0, lowerGross = 0;
        int stableBaseSalary = 0, changedBaseSalary = 0;

        foreach (var currEmp in currEmployees.Values)
        {
            if (prevEmployees.TryGetValue(currEmp.employeeId!, out var prevEmp))
            {
                // Compare gross from statutory contribution (totals)
                var prevGross = prevEmp.statutoryContribution?.gross ?? 0;
                var currGross = currEmp.statutoryContribution?.gross ?? 0;

                if (currGross > prevGross + 10) higherGross++;
                else if (currGross < prevGross - 10) lowerGross++;

                // Analyze payrollItems[].amount for base salary stability
                // Non-deduction items represent base earnings
                var prevBaseAmount = prevEmp.payrollItems?
                    .Where(p => !p.isDeduction)
                    .Sum(p => p.amount ?? 0) ?? 0;
                var currBaseAmount = currEmp.payrollItems?
                    .Where(p => !p.isDeduction)
                    .Sum(p => p.amount ?? 0) ?? 0;

                if (Math.Abs(currBaseAmount - prevBaseAmount) < 10)
                    stableBaseSalary++;
                else
                    changedBaseSalary++;
            }
        }

        var newEmployees = currEmployees.Keys.Except(prevEmployees.Keys).Count();
        var exitedEmployees = prevEmployees.Keys.Except(currEmployees.Keys).Count();

        return new EmployeeAnalysisResult
        {
            employeesWithHigherGross = higherGross,
            employeesWithLowerGross = lowerGross,
            baseSalaryStable = stableBaseSalary > changedBaseSalary,
            employeesWithSalaryChange = changedBaseSalary,
            newEmployees = newEmployees,
            exitedEmployees = exitedEmployees
        };
    }

    /// <summary>
    /// Analyzes leave-related data from employeePayrolls.
    /// Uses unpaidLeavePayrollItems and leavePayPayrollItem.
    /// </summary>
    private static LeaveAnalysisResult AnalyzeLeaveData(PayrollData previous, PayrollData current)
    {
        // Analyze leave pay (leavePayPayrollItem)
        var prevLeavePay = previous.employeePayrolls?.Sum(ep => ep.leavePayPayrollItem?.amount ?? 0) ?? 0;
        var currLeavePay = current.employeePayrolls?.Sum(ep => ep.leavePayPayrollItem?.amount ?? 0) ?? 0;
        var leavePayChange = currLeavePay - prevLeavePay;

        // Count employees with leave pay in current period
        var employeesWithLeavePay = current.employeePayrolls?
            .Count(ep => ep.leavePayPayrollItem != null && ep.leavePayPayrollItem.amount > 0) ?? 0;

        // Analyze unpaid leave (unpaidLeavePayrollItems)
        var prevUnpaidLeave = previous.employeePayrolls?
            .Sum(ep => ep.unpaidLeavePayrollItems?.Sum(ul => ul.amount) ?? 0) ?? 0;
        var currUnpaidLeave = current.employeePayrolls?
            .Sum(ep => ep.unpaidLeavePayrollItems?.Sum(ul => ul.amount) ?? 0) ?? 0;
        var unpaidLeaveChange = currUnpaidLeave - prevUnpaidLeave;

        // Count employees with unpaid leave in current period
        var employeesWithUnpaidLeave = current.employeePayrolls?
            .Count(ep => ep.unpaidLeavePayrollItems != null && ep.unpaidLeavePayrollItems.Any(ul => ul.amount > 0)) ?? 0;

        return new LeaveAnalysisResult
        {
            leavePayChange = leavePayChange,
            unpaidLeaveChange = unpaidLeaveChange,
            hasSignificantLeavePayChange = Math.Abs(leavePayChange) > 100,
            hasSignificantUnpaidLeaveChange = Math.Abs(unpaidLeaveChange) > 100,
            employeesWithLeavePay = employeesWithLeavePay,
            employeesWithUnpaidLeave = employeesWithUnpaidLeave
        };
    }

    /// <summary>
    /// Analyzes payroll errors from employeePayrolls[].error.message.
    /// </summary>
    private static ErrorAnalysisResult AnalyzePayrollErrors(PayrollData previous, PayrollData current)
    {
        var prevErrors = previous.employeePayrolls?
            .Where(ep => ep.error != null && !string.IsNullOrEmpty(ep.error.message))
            .ToList() ?? new List<EmployeePayroll>();

        var currErrors = current.employeePayrolls?
            .Where(ep => ep.error != null && !string.IsNullOrEmpty(ep.error.message))
            .ToList() ?? new List<EmployeePayroll>();

        return new ErrorAnalysisResult
        {
            prevErrorCount = prevErrors.Count,
            currErrorCount = currErrors.Count
        };
    }

    private static string DetermineConfidenceLevel(
        PayrollData previous, 
        PayrollData current,
        ErrorAnalysisResult errorAnalysis)
    {
        // Check for data presence
        var prevCount = previous.totalCount > 0 ? previous.totalCount : (previous.employeePayrolls?.Count ?? 0);
        var currCount = current.totalCount > 0 ? current.totalCount : (current.employeePayrolls?.Count ?? 0);

        if (prevCount == 0 || currCount == 0)
            return "low";

        // Check for totals completeness
        var prevHasTotals = previous.totals != null;
        var currHasTotals = current.totals != null;

        if (!prevHasTotals || !currHasTotals)
            return "medium";

        // Check for significant errors affecting confidence
        if (errorAnalysis.currErrorCount > 3 || errorAnalysis.prevErrorCount > 3)
            return "medium";

        return "high";
    }

    private static string GetMonthName(int month)
    {
        return new DateTime(2024, month, 1).ToString("MMMM");
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (!_apiConfig.IsValid())
        {
            throw new InvalidOperationException("API configuration is incomplete.");
        }

        if (!_apiService.IsAuthenticated())
        {
            var success = await _apiService.AuthenticateAsync(_apiConfig.ClientId, _apiConfig.ClientSecret);
            if (!success)
            {
                throw new InvalidOperationException("Failed to authenticate with payroll API.");
            }
        }
    }

    #endregion
}
