using Microsoft.Extensions.Options;
using PayrollIntelligence.Core;

namespace PayrollIntelligence.Core.Services;

/// <summary>
/// Service for Detailed Changes feature.
/// Identifies meaningful differences between two payroll periods.
/// Uses AI reasoning if available, otherwise falls back to rule-based analysis.
/// </summary>
public class PayrollDifferencesService
{
    private readonly PayrollApiService _apiService;
    private readonly ApiConfiguration _apiConfig;
    private readonly AiReasoningService? _aiService;

    public PayrollDifferencesService(
        PayrollApiService apiService, 
        IOptions<ApiConfiguration> apiConfig,
        AiReasoningService? aiService = null)  // Optional AI service
    {
        _apiService = apiService;
        _apiConfig = apiConfig.Value;
        _aiService = aiService;
    }

    /// <summary>
    /// Analyzes key differences between two periods using API data.
    /// </summary>
    public async Task<KeyDifferencesResult> AnalyzeKeyDifferencesAsync(
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

        return await AnalyzeKeyDifferencesWithConditionalAIAsync(previous, current);
    }

    /// <summary>
    /// Analyzes key differences with conditional AI or rule-based reasoning.
    /// Uses AI if available, otherwise falls back to rule-based analysis.
    /// </summary>
    private async Task<KeyDifferencesResult> AnalyzeKeyDifferencesWithConditionalAIAsync(PayrollData previous, PayrollData current)
    {
        // Check if AI service is available and valid
        if (IsAiAvailable())
        {
            try
            {
                // Build context with employee-level change facts
                var context = BuildEmployeeChangeContext(previous, current);
                
                // Use AI reasoning for change groups
                var aiChangeGroups = await _aiService!.GenerateChangeGroupsAsync(context);
                
                if (aiChangeGroups != null && aiChangeGroups.Count > 0)
                {
                    // Build result with AI-generated change groups
                    // But keep rule-based attention items and overview (not covered by AI)
                    return BuildResultWithAiChangeGroups(previous, current, aiChangeGroups);
                }
            }
            catch
            {
                // If AI fails, fall through to rule-based
            }
        }
        
        // Fallback to rule-based analysis
        Console.WriteLine("Falling back to rule-based analysis");
        return AnalyzeKeyDifferences(previous, current);
    }

    /// <summary>
    /// Checks if AI service is available and properly configured.
    /// </summary>
    private bool IsAiAvailable()
    {
        return _aiService != null;
    }

    /// <summary>
    /// Builds a result using AI-generated change groups, with rule-based attention items and overview.
    /// </summary>
    private static KeyDifferencesResult BuildResultWithAiChangeGroups(
        PayrollData previous, 
        PayrollData current, 
        List<ChangeGroup> aiChangeGroups)
    {
        var result = new KeyDifferencesResult();
        
        // Use AI-generated change groups
        result.change_groups = aiChangeGroups;
        
        // Still use rule-based for attention items and overview (not covered by AI)
        var ruleBasedResult = AnalyzeKeyDifferences(previous, current);
        result.attention_items = ruleBasedResult.attention_items;
        result.payroll_overview = ruleBasedResult.payroll_overview;
        result.confidence_level = ruleBasedResult.confidence_level;
        result.key_differences = ruleBasedResult.key_differences;
        
        return result;
    }

    /// <summary>
    /// Builds a context string with employee-level change facts for AI analysis.
    /// </summary>
    private static string BuildEmployeeChangeContext(PayrollData previous, PayrollData current)
    {
        var context = new System.Text.StringBuilder();
        
        context.AppendLine("Employee-level payroll change facts:");
        context.AppendLine();

        var prevEmployees = previous.employeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.employeeId))
            .ToDictionary(e => e.employeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        var currEmployees = current.employeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.employeeId))
            .ToDictionary(e => e.employeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        // List all employees with changes
        foreach (var currEmp in current.employeePayrolls ?? new List<EmployeePayroll>())
        {
            var empName = currEmp.employeeName ?? currEmp.employeeNumber ?? "Unknown";
            var empId = currEmp.employeeId ?? "";

            if (string.IsNullOrEmpty(empId)) continue;

            // Check if new employee
            if (!prevEmployees.ContainsKey(empId))
            {
                context.AppendLine($"- {empName}: New employee added to payroll");
                continue;
            }

            var prevEmp = prevEmployees[empId];
            var changes = new List<string>();

            // Gross pay change
            var prevGross = prevEmp.statutoryContribution?.gross ?? 0;
            var currGross = currEmp.statutoryContribution?.gross ?? 0;
            var grossDiff = currGross - prevGross;
            if (Math.Abs(grossDiff) > 10)
            {
                changes.Add($"Gross pay changed from {prevGross:N2} to {currGross:N2} (difference: {grossDiff:+#;-#;0})");
            }

            // Net pay change
            var prevNet = prevEmp.statutoryContribution?.net ?? 0;
            var currNet = currEmp.statutoryContribution?.net ?? 0;
            var netDiff = currNet - prevNet;
            if (Math.Abs(netDiff) > 10)
            {
                changes.Add($"Net pay changed from {prevNet:N2} to {currNet:N2} (difference: {netDiff:+#;-#;0})");
            }

            // Leave changes
            var prevLeavePay = prevEmp.leavePayPayrollItem?.amount ?? 0;
            var currLeavePay = currEmp.leavePayPayrollItem?.amount ?? 0;
            if (Math.Abs(prevLeavePay - currLeavePay) > 10)
            {
                changes.Add($"Leave pay changed from {prevLeavePay:N2} to {currLeavePay:N2}");
            }

            var prevUnpaid = prevEmp.unpaidLeavePayrollItems?.Sum(u => u.amount) ?? 0;
            var currUnpaid = currEmp.unpaidLeavePayrollItems?.Sum(u => u.amount) ?? 0;
            if (Math.Abs(prevUnpaid - currUnpaid) > 10)
            {
                changes.Add($"Unpaid leave deduction changed from {prevUnpaid:N2} to {currUnpaid:N2}");
            }

            // MTD changes
            var prevMtd = prevEmp.statutoryContribution?.employeeMtd ?? 0;
            var currMtd = currEmp.statutoryContribution?.employeeMtd ?? 0;
            if (Math.Abs(currMtd - prevMtd) > 50)
            {
                changes.Add($"Tax deduction (MTD) changed from {prevMtd:N2} to {currMtd:N2}");
            }

            if (changes.Count > 0)
            {
                context.AppendLine($"- {empName}: {string.Join("; ", changes)}");
            }
        }

        // List removed employees
        foreach (var prevEmpId in prevEmployees.Keys)
        {
            if (!currEmployees.ContainsKey(prevEmpId))
            {
                var prevEmp = prevEmployees[prevEmpId];
                var empName = prevEmp.employeeName ?? prevEmp.employeeNumber ?? "Unknown";
                context.AppendLine($"- {empName}: Removed from payroll");
            }
        }

        return context.ToString();
    }


    /// <summary>
    /// Analyzes key differences between two payroll periods with detailed grouping.
    /// </summary>
    public static KeyDifferencesResult AnalyzeKeyDifferences(PayrollData previous, PayrollData current)
    {
        var result = new KeyDifferencesResult();
        var changeGroups = new List<ChangeGroup>();
        var attentionItems = new List<AttentionItem>();

        // ============================================
        // PAYROLL OVERVIEW
        // ============================================
        var prevGross = previous.totals?.gross ?? 0;
        var currGross = current.totals?.gross ?? 0;
        var prevNet = previous.totals?.net ?? 0;
        var currNet = current.totals?.net ?? 0;
        var prevCost = previous.totals?.cost ?? 0;
        var currCost = current.totals?.cost ?? 0;
        var prevCount = previous.employeePayrolls?.Count ?? 0;
        var currCount = current.employeePayrolls?.Count ?? 0;

        var grossChange = currGross - prevGross;
        var netChange = currNet - prevNet;
        var costChange = currCost - prevCost;
        var headcountChange = currCount - prevCount;

        // Generate summary
        var overallTrend = costChange > 100 ? "increased" : (costChange < -100 ? "decreased" : "remained stable");
        var summary = headcountChange == 0
            ? $"Overall payroll {overallTrend} compared to last month, while employee headcount remained unchanged."
            : $"Overall payroll {overallTrend} compared to last month, with headcount changing by {Math.Abs(headcountChange)}.";

        result.payroll_overview = new PayrollOverview
        {
            summary = summary,
            headcount_change = headcountChange == 0 
                ? "No change in employee count" 
                : $"Employee count {(headcountChange > 0 ? "increased" : "decreased")} by {Math.Abs(headcountChange)}",
            gross_pay_trend = GetTrendDescription("Gross pay", grossChange, prevGross),
            net_pay_trend = GetTrendDescription("Net pay", netChange, prevNet),
            employer_cost_trend = GetTrendDescription("Employer payroll cost", costChange, prevCost)
        };

        // ============================================
        // BUILD EMPLOYEE LOOKUP
        // ============================================
        var prevEmployees = previous.employeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.employeeId))
            .ToDictionary(e => e.employeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        var currEmployees = current.employeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.employeeId))
            .ToDictionary(e => e.employeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        // ============================================
        // CHANGE DETECTION BY CATEGORY
        // ============================================
        var leaveAdjustmentEmployees = new List<string>();
        var grossChangeEmployees = new List<string>();
        var newEmployees = new List<string>();
        var removedEmployees = new List<string>();
        var zeroPayEmployees = new List<(string name, string reason)>();
        var significantMtdChangeEmployees = new List<string>();

        foreach (var currEmp in current.employeePayrolls ?? new List<EmployeePayroll>())
        {
            var empName = currEmp.employeeName ?? currEmp.employeeNumber ?? "Unknown";
            var empId = currEmp.employeeId ?? "";

            // Check if new employee
            if (!string.IsNullOrEmpty(empId) && !prevEmployees.ContainsKey(empId))
            {
                newEmployees.Add(empName);
                continue;
            }

            // Find matching previous employee
            EmployeePayroll? prevEmp = null;
            if (!string.IsNullOrEmpty(empId) && prevEmployees.TryGetValue(empId, out var found))
            {
                prevEmp = found;
            }

            if (prevEmp == null) continue;

            var prevGrossEmp = prevEmp.statutoryContribution?.gross ?? 0;
            var currGrossEmp = currEmp.statutoryContribution?.gross ?? 0;
            var grossDiff = currGrossEmp - prevGrossEmp;

            // Check for zero pay
            if (currGrossEmp == 0 && prevGrossEmp > 0)
            {
                zeroPayEmployees.Add((empName, "Employee was included in payroll but recorded no salary amount in the current period."));
                continue;
            }

            // Check for leave-related changes
            var prevLeavePay = prevEmp.leavePayPayrollItem?.amount ?? 0;
            var currLeavePay = currEmp.leavePayPayrollItem?.amount ?? 0;
            var prevUnpaid = prevEmp.unpaidLeavePayrollItems?.Sum(u => u.amount) ?? 0;
            var currUnpaid = currEmp.unpaidLeavePayrollItems?.Sum(u => u.amount) ?? 0;

            if (Math.Abs(prevLeavePay - currLeavePay) > 10 || Math.Abs(prevUnpaid - currUnpaid) > 10)
            {
                leaveAdjustmentEmployees.Add(empName);
            }

            // Check for gross pay changes (not related to leave)
            else if (Math.Abs(grossDiff) > 50)
            {
                grossChangeEmployees.Add(empName);
            }

            // Check for significant MTD changes
            var prevMtd = prevEmp.statutoryContribution?.employeeMtd ?? 0;
            var currMtd = currEmp.statutoryContribution?.employeeMtd ?? 0;
            if (Math.Abs(currMtd - prevMtd) > 200)
            {
                significantMtdChangeEmployees.Add(empName);
            }
        }

        // Find removed employees
        foreach (var prevEmpId in prevEmployees.Keys)
        {
            if (!currEmployees.ContainsKey(prevEmpId))
            {
                var prevEmp = prevEmployees[prevEmpId];
                removedEmployees.Add(prevEmp.employeeName ?? prevEmp.employeeNumber ?? "Unknown");
            }
        }

        // ============================================
        // BUILD CHANGE GROUPS
        // ============================================
        if (leaveAdjustmentEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                group_title = "Leave-Related Adjustments Changed",
                description = "Some employees had unpaid leave or leave pay adjustments in one period that differ from the other period.",
                affected_employees = leaveAdjustmentEmployees.Take(5).ToList(),
                why_it_matters = "Leave-related adjustments can significantly affect monthly pay and often require confirmation with HR records."
            });
        }

        if (grossChangeEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                group_title = "Gross Pay Variations",
                description = "Several employees show changes in gross pay while base salary appears generally consistent.",
                affected_employees = grossChangeEmployees.Take(5).ToList(),
                why_it_matters = "Small changes may come from rounding, partial periods, or variable payroll components."
            });
        }

        if (newEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                group_title = "New Employees Added",
                description = $"{newEmployees.Count} new employee(s) were added to the current payroll.",
                affected_employees = newEmployees.Take(5).ToList(),
                why_it_matters = "New hires increase overall payroll costs and should be verified against onboarding records."
            });
        }

        if (removedEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                group_title = "Employees Removed from Payroll",
                description = $"{removedEmployees.Count} employee(s) from the previous payroll are not in the current period.",
                affected_employees = removedEmployees.Take(5).ToList(),
                why_it_matters = "Verify if these are expected terminations or transfers to ensure final pay was processed correctly."
            });
        }

        if (significantMtdChangeEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                group_title = "Tax Deduction Changes",
                description = "Some employees have notable changes in their monthly tax deduction (MTD).",
                affected_employees = significantMtdChangeEmployees.Take(5).ToList(),
                why_it_matters = "Large MTD changes may reflect salary adjustments, bonus payments, or tax rate updates."
            });
        }

        // ============================================
        // ATTENTION ITEMS
        // ============================================
        foreach (var (name, reason) in zeroPayEmployees)
        {
            attentionItems.Add(new AttentionItem
            {
                employee = name,
                issue = "Gross pay dropped to zero",
                reason = reason
            });
        }

        // Check for payroll errors
        foreach (var currEmp in current.employeePayrolls ?? new List<EmployeePayroll>())
        {
            if (currEmp.error != null && !string.IsNullOrEmpty(currEmp.error.message) 
                && currEmp.error.message.Contains("Error", StringComparison.OrdinalIgnoreCase))
            {
                var empName = currEmp.employeeName ?? currEmp.employeeNumber ?? "Unknown";
                // Only add if not already in attention items
                if (!attentionItems.Any(a => a.employee == empName))
                {
                    // Check if this is a new error (not present in previous)
                    EmployeePayroll? prevEmp = null;
                    if (!string.IsNullOrEmpty(currEmp.employeeId) && prevEmployees.TryGetValue(currEmp.employeeId, out var found))
                    {
                        prevEmp = found;
                    }

                    var hadErrorBefore = prevEmp?.error != null && !string.IsNullOrEmpty(prevEmp.error.message);
                    if (!hadErrorBefore)
                    {
                        attentionItems.Add(new AttentionItem
                        {
                            employee = empName,
                            issue = "New payroll calculation warning",
                            reason = "This employee has a calculation warning that was not present in the previous period."
                        });
                    }
                }
            }
        }

        // ============================================
        // DETERMINE CONFIDENCE LEVEL
        // ============================================
        var confidence = "high";
        if (prevCount == 0 || currCount == 0)
            confidence = "low";
        else if (attentionItems.Count > 3 || (previous.totals == null || current.totals == null))
            confidence = "medium";

        result.change_groups = changeGroups;
        result.attention_items = attentionItems;
        result.confidence_level = confidence;

        // Also populate legacy key_differences for backward compatibility
        result.key_differences = GenerateLegacyDifferences(previous, current);

        return result;
    }

    private static string GetTrendDescription(string metric, decimal change, decimal previousValue)
    {
        var percentChange = previousValue > 0 ? (change / previousValue) * 100 : 0;

        if (Math.Abs(percentChange) < 1)
            return $"{metric} remained relatively stable";
        
        var direction = change > 0 ? "increased" : "decreased";
        
        if (Math.Abs(percentChange) > 5)
            return $"{metric} {direction} notably across the payroll";
        
        return $"{metric} {direction} slightly compared to last period";
    }

    private static List<KeyDifference> GenerateLegacyDifferences(PayrollData previous, PayrollData current)
    {
        var differences = new List<KeyDifference>();

        var prevCost = previous.totals?.cost ?? 0;
        var currCost = current.totals?.cost ?? 0;
        var costChange = currCost - prevCost;

        if (Math.Abs(costChange) > 1000)
        {
            differences.Add(new KeyDifference
            {
                title = "Overall Payroll Costs " + (costChange > 0 ? "Increased" : "Decreased"),
                explanation = $"Total payroll costs changed by RM {Math.Abs(costChange):N0}.",
                affected_area = "Total Compensation"
            });
        }

        var prevCount = previous.employeePayrolls?.Count ?? 0;
        var currCount = current.employeePayrolls?.Count ?? 0;
        if (prevCount != currCount)
        {
            differences.Add(new KeyDifference
            {
                title = $"Headcount Changed by {Math.Abs(currCount - prevCount)}",
                explanation = $"Employee count went from {prevCount} to {currCount}.",
                affected_area = "Workforce"
            });
        }

        return differences;
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
}
