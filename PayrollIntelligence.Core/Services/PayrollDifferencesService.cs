using Microsoft.Extensions.Options;

namespace PayrollIntelligence.Core.Services;

/// <summary>
/// Service for Detailed Changes feature.
/// Identifies meaningful differences between two payroll periods.
/// </summary>
public class PayrollDifferencesService
{
    private readonly PayrollApiService _apiService;
    private readonly ApiConfiguration _apiConfig;

    public PayrollDifferencesService(PayrollApiService apiService, IOptions<ApiConfiguration> apiConfig)
    {
        _apiService = apiService;
        _apiConfig = apiConfig.Value;
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

        return AnalyzeKeyDifferences(previous, current);
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
        var prevGross = previous.Totals?.Gross ?? 0;
        var currGross = current.Totals?.Gross ?? 0;
        var prevNet = previous.Totals?.Net ?? 0;
        var currNet = current.Totals?.Net ?? 0;
        var prevCost = previous.Totals?.Cost ?? 0;
        var currCost = current.Totals?.Cost ?? 0;
        var prevCount = previous.EmployeePayrolls?.Count ?? 0;
        var currCount = current.EmployeePayrolls?.Count ?? 0;

        var grossChange = currGross - prevGross;
        var netChange = currNet - prevNet;
        var costChange = currCost - prevCost;
        var headcountChange = currCount - prevCount;

        // Generate summary
        var overallTrend = costChange > 100 ? "increased" : (costChange < -100 ? "decreased" : "remained stable");
        var summary = headcountChange == 0
            ? $"Overall payroll {overallTrend} compared to last month, while employee headcount remained unchanged."
            : $"Overall payroll {overallTrend} compared to last month, with headcount changing by {Math.Abs(headcountChange)}.";

        result.PayrollOverview = new PayrollOverview
        {
            Summary = summary,
            HeadcountChange = headcountChange == 0 
                ? "No change in employee count" 
                : $"Employee count {(headcountChange > 0 ? "increased" : "decreased")} by {Math.Abs(headcountChange)}",
            GrossPayTrend = GetTrendDescription("Gross pay", grossChange, prevGross),
            NetPayTrend = GetTrendDescription("Net pay", netChange, prevNet),
            EmployerCostTrend = GetTrendDescription("Employer payroll cost", costChange, prevCost)
        };

        // ============================================
        // BUILD EMPLOYEE LOOKUP
        // ============================================
        var prevEmployees = previous.EmployeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.EmployeeId))
            .ToDictionary(e => e.EmployeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        var currEmployees = current.EmployeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.EmployeeId))
            .ToDictionary(e => e.EmployeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        // ============================================
        // CHANGE DETECTION BY CATEGORY
        // ============================================
        var leaveAdjustmentEmployees = new List<string>();
        var grossChangeEmployees = new List<string>();
        var newEmployees = new List<string>();
        var removedEmployees = new List<string>();
        var zeroPayEmployees = new List<(string name, string reason)>();
        var significantMtdChangeEmployees = new List<string>();

        foreach (var currEmp in current.EmployeePayrolls ?? new List<EmployeePayroll>())
        {
            var empName = currEmp.EmployeeName ?? currEmp.EmployeeNumber ?? "Unknown";
            var empId = currEmp.EmployeeId ?? "";

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

            var prevGrossEmp = prevEmp.StatutoryContribution?.Gross ?? 0;
            var currGrossEmp = currEmp.StatutoryContribution?.Gross ?? 0;
            var grossDiff = currGrossEmp - prevGrossEmp;

            // Check for zero pay
            if (currGrossEmp == 0 && prevGrossEmp > 0)
            {
                zeroPayEmployees.Add((empName, "Employee was included in payroll but recorded no salary amount in the current period."));
                continue;
            }

            // Check for leave-related changes
            var prevLeavePay = prevEmp.LeavePayPayrollItem?.Amount ?? 0;
            var currLeavePay = currEmp.LeavePayPayrollItem?.Amount ?? 0;
            var prevUnpaid = prevEmp.UnpaidLeavePayrollItems?.Sum(u => u.Amount) ?? 0;
            var currUnpaid = currEmp.UnpaidLeavePayrollItems?.Sum(u => u.Amount) ?? 0;

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
            var prevMtd = prevEmp.StatutoryContribution?.EmployeeMtd ?? 0;
            var currMtd = currEmp.StatutoryContribution?.EmployeeMtd ?? 0;
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
                removedEmployees.Add(prevEmp.EmployeeName ?? prevEmp.EmployeeNumber ?? "Unknown");
            }
        }

        // ============================================
        // BUILD CHANGE GROUPS
        // ============================================
        if (leaveAdjustmentEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                GroupTitle = "Leave-Related Adjustments Changed",
                Description = "Some employees had unpaid leave or leave pay adjustments in one period that differ from the other period.",
                AffectedEmployees = leaveAdjustmentEmployees.Take(5).ToList(),
                WhyItMatters = "Leave-related adjustments can significantly affect monthly pay and often require confirmation with HR records."
            });
        }

        if (grossChangeEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                GroupTitle = "Gross Pay Variations",
                Description = "Several employees show changes in gross pay while base salary appears generally consistent.",
                AffectedEmployees = grossChangeEmployees.Take(5).ToList(),
                WhyItMatters = "Small changes may come from rounding, partial periods, or variable payroll components."
            });
        }

        if (newEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                GroupTitle = "New Employees Added",
                Description = $"{newEmployees.Count} new employee(s) were added to the current payroll.",
                AffectedEmployees = newEmployees.Take(5).ToList(),
                WhyItMatters = "New hires increase overall payroll costs and should be verified against onboarding records."
            });
        }

        if (removedEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                GroupTitle = "Employees Removed from Payroll",
                Description = $"{removedEmployees.Count} employee(s) from the previous payroll are not in the current period.",
                AffectedEmployees = removedEmployees.Take(5).ToList(),
                WhyItMatters = "Verify if these are expected terminations or transfers to ensure final pay was processed correctly."
            });
        }

        if (significantMtdChangeEmployees.Count > 0)
        {
            changeGroups.Add(new ChangeGroup
            {
                GroupTitle = "Tax Deduction Changes",
                Description = "Some employees have notable changes in their monthly tax deduction (MTD).",
                AffectedEmployees = significantMtdChangeEmployees.Take(5).ToList(),
                WhyItMatters = "Large MTD changes may reflect salary adjustments, bonus payments, or tax rate updates."
            });
        }

        // ============================================
        // ATTENTION ITEMS
        // ============================================
        foreach (var (name, reason) in zeroPayEmployees)
        {
            attentionItems.Add(new AttentionItem
            {
                Employee = name,
                Issue = "Gross pay dropped to zero",
                Reason = reason
            });
        }

        // Check for payroll errors
        foreach (var currEmp in current.EmployeePayrolls ?? new List<EmployeePayroll>())
        {
            if (currEmp.Error != null && !string.IsNullOrEmpty(currEmp.Error.Message) 
                && currEmp.Error.Message.Contains("Error", StringComparison.OrdinalIgnoreCase))
            {
                var empName = currEmp.EmployeeName ?? currEmp.EmployeeNumber ?? "Unknown";
                // Only add if not already in attention items
                if (!attentionItems.Any(a => a.Employee == empName))
                {
                    // Check if this is a new error (not present in previous)
                    EmployeePayroll? prevEmp = null;
                    if (!string.IsNullOrEmpty(currEmp.EmployeeId) && prevEmployees.TryGetValue(currEmp.EmployeeId, out var found))
                    {
                        prevEmp = found;
                    }

                    var hadErrorBefore = prevEmp?.Error != null && !string.IsNullOrEmpty(prevEmp.Error.Message);
                    if (!hadErrorBefore)
                    {
                        attentionItems.Add(new AttentionItem
                        {
                            Employee = empName,
                            Issue = "New payroll calculation warning",
                            Reason = "This employee has a calculation warning that was not present in the previous period."
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
        else if (attentionItems.Count > 3 || (previous.Totals == null || current.Totals == null))
            confidence = "medium";

        result.ChangeGroups = changeGroups;
        result.AttentionItems = attentionItems;
        result.ConfidenceLevel = confidence;

        // Also populate legacy key_differences for backward compatibility
        result.KeyDifferences = GenerateLegacyDifferences(previous, current);

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

        var prevCost = previous.Totals?.Cost ?? 0;
        var currCost = current.Totals?.Cost ?? 0;
        var costChange = currCost - prevCost;

        if (Math.Abs(costChange) > 1000)
        {
            differences.Add(new KeyDifference
            {
                Title = "Overall Payroll Costs " + (costChange > 0 ? "Increased" : "Decreased"),
                Explanation = $"Total payroll costs changed by RM {Math.Abs(costChange):N0}.",
                AffectedArea = "Total Compensation"
            });
        }

        var prevCount = previous.EmployeePayrolls?.Count ?? 0;
        var currCount = current.EmployeePayrolls?.Count ?? 0;
        if (prevCount != currCount)
        {
            differences.Add(new KeyDifference
            {
                Title = $"Headcount Changed by {Math.Abs(currCount - prevCount)}",
                Explanation = $"Employee count went from {prevCount} to {currCount}.",
                AffectedArea = "Workforce"
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
