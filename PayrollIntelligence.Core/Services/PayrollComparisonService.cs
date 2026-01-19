using Microsoft.Extensions.Options;

namespace PayrollIntelligence.Core.Services;

/// <summary>
/// Service for Payroll Summary feature.
/// Compares total payroll costs, headcount changes, and identifies key drivers.
/// Uses AI reasoning if available, otherwise falls back to rule-based analysis.
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
    private readonly AiReasoningService? _aiService;

    public PayrollComparisonService(
        PayrollApiService apiService, 
        IOptions<ApiConfiguration> apiConfig,
        AiReasoningService? aiService = null)  // Optional AI service
    {
        _apiService = apiService;
        _apiConfig = apiConfig.Value;
        _aiService = aiService;
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

        return await ComparePeriodsWithConditionalAiAsync(previous, current, previousPeriodName, currentPeriodName);
    }

    /// <summary>
    /// Compares payroll periods with conditional AI or rule-based reasoning.
    /// Uses AI if available, otherwise falls back to rule-based analysis.
    /// </summary>
    private async Task<PayrollComparisonResult> ComparePeriodsWithConditionalAiAsync(
        PayrollData previous, 
        PayrollData current,
        string previousPeriodName,
        string currentPeriodName)
    {
        // Check if AI service is available and valid
        if (IsAiAvailable())
        {
            try
            {
                // Build context from raw payroll data
                var context = BuildComparisonContext(previous, current, previousPeriodName, currentPeriodName);
                
                // Use AI reasoning for comparison analysis
                var aiResponse = await _aiService!.GeneratePayrollComparisonAsync(context);
                
                if (aiResponse != null && !string.IsNullOrWhiteSpace(aiResponse.Direction))
                {
                    // Build result with AI-generated analysis
                    // But keep rule-based for metrics and notable_observations (not covered by AI)
                    return BuildResultWithAiComparison(previous, current, previousPeriodName, currentPeriodName, aiResponse);
                }
            }
            catch
            {
                // If AI fails, fall through to rule-based
            }
        }
        
        // Fallback to rule-based analysis
        Console.WriteLine("Falling back to rule-based analysis");
        return Compare(previous, current, previousPeriodName, currentPeriodName);
    }

    /// <summary>
    /// Checks if AI service is available and properly configured.
    /// </summary>
    private bool IsAiAvailable()
    {
        return _aiService != null;
    }

    /// <summary>
    /// Builds a result using AI-generated comparison analysis, with rule-based metrics and notable observations.
    /// </summary>
    private PayrollComparisonResult BuildResultWithAiComparison(
        PayrollData previous,
        PayrollData current,
        string previousPeriodName,
        string currentPeriodName,
        AiReasoningService.PayrollComparisonAiResponse aiResponse)
    {
        // Get rule-based result for metrics and notable_observations
        var ruleBasedResult = Compare(previous, current, previousPeriodName, currentPeriodName);
        
        // Use AI-generated fields
        var result = new PayrollComparisonResult
        {
            Direction = aiResponse.Direction?.ToLower() ?? ruleBasedResult.Direction,
            HeadlineSummary = aiResponse.Summary ?? ruleBasedResult.HeadlineSummary,
            KeyDrivers = aiResponse.KeyDrivers ?? ruleBasedResult.KeyDrivers,
            ConfidenceLevel = aiResponse.ConfidenceLevel?.ToLower() ?? ruleBasedResult.ConfidenceLevel,
            
            // Keep rule-based for these (not generated by AI)
            NotableObservations = ruleBasedResult.NotableObservations,
            PreviousMetrics = ruleBasedResult.PreviousMetrics,
            CurrentMetrics = ruleBasedResult.CurrentMetrics
        };
        
        return result;
    }

    /// <summary>
    /// Builds a context string from raw payroll data for AI analysis.
    /// </summary>
    private static string BuildComparisonContext(
        PayrollData previous, 
        PayrollData current,
        string previousPeriodName,
        string currentPeriodName)
    {
        var prevNet = previous.Totals?.Net ?? 0;
        var currNet = current.Totals?.Net ?? 0;
        var prevGross = previous.Totals?.Gross ?? 0;
        var currGross = current.Totals?.Gross ?? 0;
        var prevCost = previous.Totals?.Cost ?? 0;
        var currCost = current.Totals?.Cost ?? 0;
        var prevCount = previous.TotalCount > 0 ? previous.TotalCount : (previous.EmployeePayrolls?.Count ?? 0);
        var currCount = current.TotalCount > 0 ? current.TotalCount : (current.EmployeePayrolls?.Count ?? 0);

        var context = new System.Text.StringBuilder();
        context.AppendLine($"Previous Period ({previousPeriodName}):");
        context.AppendLine($"  Net Pay: {prevNet:C}");
        context.AppendLine($"  Gross Pay: {prevGross:C}");
        context.AppendLine($"  Employer Cost: {prevCost:C}");
        context.AppendLine($"  Headcount: {prevCount}");
        context.AppendLine();
        context.AppendLine($"Current Period ({currentPeriodName}):");
        context.AppendLine($"  Net Pay: {currNet:C}");
        context.AppendLine($"  Gross Pay: {currGross:C}");
        context.AppendLine($"  Employer Cost: {currCost:C}");
        context.AppendLine($"  Headcount: {currCount}");
        context.AppendLine();
        
        // Add change calculations
        var costChange = currCost - prevCost;
        var grossChange = currGross - prevGross;
        var netChange = currNet - prevNet;
        var headcountChange = currCount - prevCount;
        
        context.AppendLine("Changes:");
        context.AppendLine($"  Cost Change: {costChange:+#;-#;0:C} ({((prevCost > 0 ? (costChange / prevCost) * 100 : 0)):+#;-#;0.0}%)");
        context.AppendLine($"  Gross Change: {grossChange:+#;-#;0:C}");
        context.AppendLine($"  Net Change: {netChange:+#;-#;0:C}");
        context.AppendLine($"  Headcount Change: {headcountChange:+#;-#;0}");

        return context.ToString();
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
        var prevNet = previous.Totals?.Net ?? 0;
        var currNet = current.Totals?.Net ?? 0;
        var prevGross = previous.Totals?.Gross ?? 0;
        var currGross = current.Totals?.Gross ?? 0;
        var prevCost = previous.Totals?.Cost ?? 0;
        var currCost = current.Totals?.Cost ?? 0;
        
        // Use totalCount field, fallback to counting employeePayrolls
        var prevCount = previous.TotalCount > 0 ? previous.TotalCount : (previous.EmployeePayrolls?.Count ?? 0);
        var currCount = current.TotalCount > 0 ? current.TotalCount : (current.EmployeePayrolls?.Count ?? 0);

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
            keyDrivers.Add(headcountChange > 0
                ? $"Headcount increased by {headcountChange} employee(s), contributing to higher payroll costs"
                : $"Headcount decreased by {Math.Abs(headcountChange)} employee(s), reducing overall payroll costs");
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
        if (employeeAnalysis.EmployeesWithLowerGross > 0 && grossChange < 0)
        {
            keyDrivers.Add($"{employeeAnalysis.EmployeesWithLowerGross} employee(s) had lower gross amounts compared to the previous month");
        }
        else if (employeeAnalysis.EmployeesWithHigherGross > 0 && grossChange > 0)
        {
            keyDrivers.Add($"{employeeAnalysis.EmployeesWithHigherGross} employee(s) had higher gross amounts compared to the previous month");
        }

        // 4. Leave-related analysis
        if (leaveAnalysis.HasSignificantLeavePayChange)
        {
            keyDrivers.Add(leaveAnalysis.LeavePayChange < 0
                ? "Leave pay amounts decreased compared to the previous period"
                : "Leave pay amounts increased in the current period");
        }

        if (leaveAnalysis.HasSignificantUnpaidLeaveChange)
        {
            keyDrivers.Add(leaveAnalysis.UnpaidLeaveChange > 0
                ? $"Unpaid leave deductions increased, affecting {leaveAnalysis.EmployeesWithUnpaidLeave} employee(s)"
                : "Unpaid leave deductions from the previous period were reduced or removed");
        }

        // 5. Combined leave impact
        if (leaveAnalysis.HasSignificantLeavePayChange || leaveAnalysis.HasSignificantUnpaidLeaveChange)
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
        if (employeeAnalysis.BaseSalaryStable && Math.Abs(grossChange) > 100)
        {
            notableObservations.Add("Most base salaries appear consistent, suggesting changes were driven by variable components rather than structural salary changes");
        }
        else if (employeeAnalysis is { BaseSalaryStable: false, EmployeesWithSalaryChange: > 0 })
        {
            notableObservations.Add($"{employeeAnalysis.EmployeesWithSalaryChange} employee(s) had base salary changes this period");
        }

        // 3. Net-to-gross ratio (deduction pattern indicator)
        var prevRatio = prevGross > 0 ? prevNet / prevGross : 0;
        var currRatio = currGross > 0 ? currNet / currGross : 0;
        if (Math.Abs(currRatio - prevRatio) > 0.02m)
        {
            notableObservations.Add("The proportion of net pay to gross pay shifted, indicating changes in deduction patterns");
        }

        // 4. Payroll errors/warnings (from error.message)
        if (errorAnalysis.HasErrors)
        {
            if (errorAnalysis is { PrevErrorCount: > 0, CurrErrorCount: > 0 })
            {
                notableObservations.Add("Payroll calculation warnings were present in both periods");
            }
            else if (errorAnalysis.CurrErrorCount > 0 && errorAnalysis.PrevErrorCount == 0)
            {
                notableObservations.Add($"New payroll calculation warnings detected ({errorAnalysis.CurrErrorCount} employee(s) affected)");
            }
            else if (errorAnalysis.PrevErrorCount > 0 && errorAnalysis.CurrErrorCount == 0)
            {
                notableObservations.Add("Payroll calculation warnings from previous period have been resolved");
            }
        }

        // 5. New hires or terminations
        if (employeeAnalysis.NewEmployees > 0)
        {
            notableObservations.Add($"{employeeAnalysis.NewEmployees} new employee(s) added to payroll this period");
        }
        if (employeeAnalysis.ExitedEmployees > 0)
        {
            notableObservations.Add($"{employeeAnalysis.ExitedEmployees} employee(s) no longer in payroll this period");
        }

        // 6. Leave pay activity
        if (leaveAnalysis.EmployeesWithLeavePay > 0)
        {
            notableObservations.Add($"{leaveAnalysis.EmployeesWithLeavePay} employee(s) received leave pay in the current period");
        }

        // Generate headline summary
        var headlineSummary = GenerateHeadlineSummary(
            direction, costChange, grossChange, headcountChange, prevCost, 
            leaveAnalysis, employeeAnalysis);

        // Determine confidence level
        var confidenceLevel = DetermineConfidenceLevel(previous, current, errorAnalysis);

        return new PayrollComparisonResult
        {
            Direction = direction,
            HeadlineSummary = headlineSummary,
            KeyDrivers = keyDrivers.Take(5).ToList(),
            NotableObservations = notableObservations.Take(5).ToList(),
            ConfidenceLevel = confidenceLevel,
            PreviousMetrics = new PayrollMetrics
            {
                NetPay = prevNet,
                GrossPay = prevGross,
                EmployerCost = prevCost,
                Headcount = prevCount,
                PeriodName = previousPeriodName
            },
            CurrentMetrics = new PayrollMetrics
            {
                NetPay = currNet,
                GrossPay = currGross,
                EmployerCost = currCost,
                Headcount = currCount,
                PeriodName = currentPeriodName
            }
        };
    }

    #region Analysis Helper Classes

    private class EmployeeAnalysisResult
    {
        public int EmployeesWithHigherGross { get; set; }
        public int EmployeesWithLowerGross { get; set; }
        public bool BaseSalaryStable { get; set; }
        public int EmployeesWithSalaryChange { get; set; }
        public int NewEmployees { get; set; }
        public int ExitedEmployees { get; set; }
    }

    private class LeaveAnalysisResult
    {
        public decimal LeavePayChange { get; set; }
        public decimal UnpaidLeaveChange { get; set; }
        public bool HasSignificantLeavePayChange { get; set; }
        public bool HasSignificantUnpaidLeaveChange { get; set; }
        public int EmployeesWithLeavePay { get; set; }
        public int EmployeesWithUnpaidLeave { get; set; }
    }

    private class ErrorAnalysisResult
    {
        public int PrevErrorCount { get; set; }
        public int CurrErrorCount { get; set; }
        public bool HasErrors => PrevErrorCount > 0 || CurrErrorCount > 0;
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
        else if (leaveAnalysis.HasSignificantLeavePayChange || leaveAnalysis.HasSignificantUnpaidLeaveChange)
        {
            return $"Total payroll {directionWord} {magnitude} compared to last month, primarily driven by leave-related adjustments.";
        }
        else if (!employeeAnalysis.BaseSalaryStable && employeeAnalysis.EmployeesWithSalaryChange > 0)
        {
            return $"Total payroll {directionWord} {magnitude} compared to last month, driven by salary changes for {employeeAnalysis.EmployeesWithSalaryChange} employee(s).";
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
        var prevEmployees = previous.EmployeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.EmployeeId))
            .ToDictionary(e => e.EmployeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();
        
        var currEmployees = current.EmployeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.EmployeeId))
            .ToDictionary(e => e.EmployeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        int higherGross = 0, lowerGross = 0;
        int stableBaseSalary = 0, changedBaseSalary = 0;

        foreach (var currEmp in currEmployees.Values)
        {
            if (prevEmployees.TryGetValue(currEmp.EmployeeId!, out var prevEmp))
            {
                // Compare gross from statutory contribution (totals)
                var prevGross = prevEmp.StatutoryContribution?.Gross ?? 0;
                var currGross = currEmp.StatutoryContribution?.Gross ?? 0;

                if (currGross > prevGross + 10) higherGross++;
                else if (currGross < prevGross - 10) lowerGross++;

                // Analyze payrollItems[].amount for base salary stability
                // Non-deduction items represent base earnings
                var prevBaseAmount = prevEmp.PayrollItems?
                    .Where(p => !p.IsDeduction)
                    .Sum(p => p.Amount ?? 0) ?? 0;
                var currBaseAmount = currEmp.PayrollItems?
                    .Where(p => !p.IsDeduction)
                    .Sum(p => p.Amount ?? 0) ?? 0;

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
            EmployeesWithHigherGross = higherGross,
            EmployeesWithLowerGross = lowerGross,
            BaseSalaryStable = stableBaseSalary > changedBaseSalary,
            EmployeesWithSalaryChange = changedBaseSalary,
            NewEmployees = newEmployees,
            ExitedEmployees = exitedEmployees
        };
    }

    /// <summary>
    /// Analyzes leave-related data from employeePayrolls.
    /// Uses unpaidLeavePayrollItems and leavePayPayrollItem.
    /// </summary>
    private static LeaveAnalysisResult AnalyzeLeaveData(PayrollData previous, PayrollData current)
    {
        // Analyze leave pay (leavePayPayrollItem)
        var prevLeavePay = previous.EmployeePayrolls?.Sum(ep => ep.LeavePayPayrollItem?.Amount ?? 0) ?? 0;
        var currLeavePay = current.EmployeePayrolls?.Sum(ep => ep.LeavePayPayrollItem?.Amount ?? 0) ?? 0;
        var leavePayChange = currLeavePay - prevLeavePay;

        // Count employees with leave pay in current period
        var employeesWithLeavePay = current.EmployeePayrolls?
            .Count(ep => ep.LeavePayPayrollItem != null && ep.LeavePayPayrollItem.Amount > 0) ?? 0;

        // Analyze unpaid leave (unpaidLeavePayrollItems)
        var prevUnpaidLeave = previous.EmployeePayrolls?
            .Sum(ep => ep.UnpaidLeavePayrollItems?.Sum(ul => ul.Amount) ?? 0) ?? 0;
        var currUnpaidLeave = current.EmployeePayrolls?
            .Sum(ep => ep.UnpaidLeavePayrollItems?.Sum(ul => ul.Amount) ?? 0) ?? 0;
        var unpaidLeaveChange = currUnpaidLeave - prevUnpaidLeave;

        // Count employees with unpaid leave in current period
        var employeesWithUnpaidLeave = current.EmployeePayrolls?
            .Count(ep => ep.UnpaidLeavePayrollItems != null && ep.UnpaidLeavePayrollItems.Any(ul => ul.Amount > 0)) ?? 0;

        return new LeaveAnalysisResult
        {
            LeavePayChange = leavePayChange,
            UnpaidLeaveChange = unpaidLeaveChange,
            HasSignificantLeavePayChange = Math.Abs(leavePayChange) > 100,
            HasSignificantUnpaidLeaveChange = Math.Abs(unpaidLeaveChange) > 100,
            EmployeesWithLeavePay = employeesWithLeavePay,
            EmployeesWithUnpaidLeave = employeesWithUnpaidLeave
        };
    }

    /// <summary>
    /// Analyzes payroll errors from employeePayrolls[].error.message.
    /// </summary>
    private static ErrorAnalysisResult AnalyzePayrollErrors(PayrollData previous, PayrollData current)
    {
        var prevErrors = previous.EmployeePayrolls?
            .Where(ep => ep.Error != null && !string.IsNullOrEmpty(ep.Error.Message))
            .ToList() ?? new List<EmployeePayroll>();

        var currErrors = current.EmployeePayrolls?
            .Where(ep => ep.Error != null && !string.IsNullOrEmpty(ep.Error.Message))
            .ToList() ?? new List<EmployeePayroll>();

        return new ErrorAnalysisResult
        {
            PrevErrorCount = prevErrors.Count,
            CurrErrorCount = currErrors.Count
        };
    }

    private static string DetermineConfidenceLevel(
        PayrollData previous, 
        PayrollData current,
        ErrorAnalysisResult errorAnalysis)
    {
        // Check for data presence
        var prevCount = previous.TotalCount > 0 ? previous.TotalCount : (previous.EmployeePayrolls?.Count ?? 0);
        var currCount = current.TotalCount > 0 ? current.TotalCount : (current.EmployeePayrolls?.Count ?? 0);

        if (prevCount == 0 || currCount == 0)
            return "low";

        // Check for totals completeness
        var prevHasTotals = previous.Totals != null;
        var currHasTotals = current.Totals != null;

        if (!prevHasTotals || !currHasTotals)
            return "medium";

        // Check for significant errors affecting confidence
        if (errorAnalysis.CurrErrorCount > 3 || errorAnalysis.PrevErrorCount > 3)
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
