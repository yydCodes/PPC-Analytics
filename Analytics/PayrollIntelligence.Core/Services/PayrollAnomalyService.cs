using Microsoft.Extensions.Options;

namespace PayrollIntelligence.Core.Services;

/// <summary>
/// Service for Risk & Review feature.
/// Detects unusual, risky, or inconsistent changes in payroll data.
/// </summary>
public class PayrollAnomalyService
{
    private readonly PayrollApiService _apiService;
    private readonly ApiConfiguration _apiConfig;

    public PayrollAnomalyService(PayrollApiService apiService, IOptions<ApiConfiguration> apiConfig)
    {
        _apiService = apiService;
        _apiConfig = apiConfig.Value;
    }

    /// <summary>
    /// Detects anomalies by comparing a draft payroll (status 0) against a selected approved payroll (status 2).
    /// </summary>
    public async Task<AnomalyDetectionResult> DetectAnomaliesAsync(
        int draftYear, int draftMonth,
        int comparisonYear, int comparisonMonth)
    {
        await EnsureAuthenticatedAsync();

        // Validate draft payroll has status 0
        var draftStatus = await _apiService.GetPayrollStatusAsync(draftYear, draftMonth);
        if (draftStatus != 0)
        {
            return new AnomalyDetectionResult
            {
                anomalies = new List<Anomaly>(),
                summary = $"The selected draft payroll ({draftMonth:D2}/{draftYear}) has status {draftStatus}. Only draft payrolls (status 0) can be analyzed."
            };
        }

        // Validate comparison payroll has status 2
        var comparisonStatus = await _apiService.GetPayrollStatusAsync(comparisonYear, comparisonMonth);
        if (comparisonStatus != 2)
        {
            return new AnomalyDetectionResult
            {
                anomalies = new List<Anomaly>(),
                summary = $"The selected comparison payroll ({comparisonMonth:D2}/{comparisonYear}) has status {comparisonStatus}. Only approved payrolls (status 2) can be used for comparison."
            };
        }

        // Load comparison (approved) payroll data
        var comparison = await _apiService.GetPayrollDataAsync(comparisonYear, comparisonMonth);
        if (comparison == null)
        {
            throw new InvalidOperationException("Failed to retrieve comparison payroll data from API");
        }

        // Load draft payroll data
        var draft = await _apiService.GetPayrollDataAsync(draftYear, draftMonth);
        if (draft == null)
        {
            throw new InvalidOperationException("Failed to retrieve draft payroll data from API");
        }

        return DetectAnomalies(comparison, draft);
    }

    /// <summary>
    /// Detects anomalies by comparing current payroll against a previous period.
    /// </summary>
    public static AnomalyDetectionResult DetectAnomalies(PayrollData previous, PayrollData current)
    {
        var anomalies = new List<Anomaly>();

        var prevEmployees = previous.employeePayrolls ?? new List<EmployeePayroll>();
        var currEmployees = current.employeePayrolls ?? new List<EmployeePayroll>();

        // Build lookup dictionaries by employeeId
        var prevEmployeeDict = prevEmployees
            .Where(e => !string.IsNullOrEmpty(e.employeeId))
            .ToDictionary(e => e.employeeId!, e => e);

        // ============================================
        // EMPLOYEE-LEVEL ANOMALY DETECTION
        // ============================================

        foreach (var currEmp in currEmployees)
        {
            var employeeRef = currEmp.employeeName ?? currEmp.employeeNumber ?? currEmp.employeeId ?? "Unknown Employee";
            
            // Find matching previous employee
            EmployeePayroll? prevEmp = null;
            if (!string.IsNullOrEmpty(currEmp.employeeId) && prevEmployeeDict.TryGetValue(currEmp.employeeId, out var found))
            {
                prevEmp = found;
            }

            if (prevEmp == null)
            {
                // New employee - not necessarily an anomaly, but note if significant
                continue;
            }

            // --- 1. NET PAY VS SALARY CONSISTENCY ---
            DetectNetPayInconsistency(anomalies, prevEmp, currEmp, employeeRef);

            // --- 2. LEAVE-RELATED CHANGES ---
            DetectLeaveChanges(anomalies, prevEmp, currEmp, employeeRef);

            // --- 3. MISSING OR UNEXPECTED PAYROLL ITEMS ---
            DetectPayrollItemChanges(anomalies, prevEmp, currEmp, employeeRef);

            // --- 4. STATUTORY CONTRIBUTION INCONSISTENCIES ---
            DetectStatutoryChanges(anomalies, prevEmp, currEmp, employeeRef);
        }

        // ============================================
        // PAYROLL-WIDE ANOMALY DETECTION
        // ============================================

        DetectPayrollWideAnomalies(anomalies, previous, current, prevEmployees, currEmployees);

        // Generate summary
        var summary = GenerateSummary(anomalies);

        return new AnomalyDetectionResult 
        { 
            anomalies = anomalies,
            summary = summary
        };
    }

    #region Employee-Level Detection Methods

    private static void DetectNetPayInconsistency(List<Anomaly> anomalies, EmployeePayroll prevEmp, EmployeePayroll currEmp, string employeeRef)
    {
        var prevNet = prevEmp.statutoryContribution?.net ?? 0;
        var currNet = currEmp.statutoryContribution?.net ?? 0;
        var prevBaseSalary = prevEmp.payrollItems?.Where(pi => !pi.isDeduction).Sum(pi => pi.amount ?? 0) ?? 0;
        var currBaseSalary = currEmp.payrollItems?.Where(pi => !pi.isDeduction).Sum(pi => pi.amount ?? 0) ?? 0;

        // Net pay changed but base salary didn't
        if (Math.Abs(currNet - prevNet) > 50 && Math.Abs(currBaseSalary - prevBaseSalary) < 1)
        {
            anomalies.Add(new Anomaly
            {
                category = "Pay Consistency",
                severity = DetermineSeverity(Math.Abs(currNet - prevNet), 100, 500),
                scope = "employee",
                reference = employeeRef,
                title = "Net pay changed without base salary change",
                explanation = $"{employeeRef}'s net pay changed by RM {Math.Abs(currNet - prevNet):N2} compared to last month while the base salary remained the same, suggesting changes in deductions, leave, or statutory contributions.",
                review_suggestion = "Review deductions, leave entries, or statutory contribution changes before approval."
            });
        }
    }

    private static void DetectLeaveChanges(List<Anomaly> anomalies, EmployeePayroll prevEmp, EmployeePayroll currEmp, string employeeRef)
    {
        var prevUnpaidLeaveTotal = prevEmp.unpaidLeavePayrollItems?.Sum(ul => ul.amount) ?? 0;
        var currUnpaidLeaveTotal = currEmp.unpaidLeavePayrollItems?.Sum(ul => ul.amount) ?? 0;
        var currUnpaidDays = currEmp.unpaidLeaveDays;

        // Unpaid leave appeared this month
        if (prevUnpaidLeaveTotal == 0 && currUnpaidLeaveTotal > 0)
        {
            anomalies.Add(new Anomaly
            {
                category = "Leave",
                severity = "medium",
                scope = "employee",
                reference = employeeRef,
                title = "Unpaid leave appeared this month",
                explanation = $"Unpaid leave deduction of RM {currUnpaidLeaveTotal:N2} ({currUnpaidDays} days) was present in the current payroll but not in the previous period.",
                review_suggestion = "Confirm unpaid leave records and employee notification with HR."
            });
        }

        // Unpaid leave disappeared
        if (prevUnpaidLeaveTotal > 0 && currUnpaidLeaveTotal == 0)
        {
            anomalies.Add(new Anomaly
            {
                category = "Leave",
                severity = "low",
                scope = "employee",
                reference = employeeRef,
                title = "Unpaid leave deduction removed",
                explanation = $"Previous unpaid leave deduction of RM {prevUnpaidLeaveTotal:N2} is no longer present in the current payroll.",
                review_suggestion = "Verify that unpaid leave was correctly processed or confirm if it was reversed intentionally."
            });
        }

        // Leave pay changes
        var prevLeavePay = prevEmp.leavePayPayrollItem?.amount ?? 0;
        var currLeavePay = currEmp.leavePayPayrollItem?.amount ?? 0;

        if (prevLeavePay == 0 && currLeavePay > 0)
        {
            anomalies.Add(new Anomaly
            {
                category = "Leave",
                severity = "low",
                scope = "employee",
                reference = employeeRef,
                title = "Leave pay added this month",
                explanation = $"Leave pay of RM {currLeavePay:N2} was added in the current payroll.",
                review_suggestion = "Confirm leave pay calculation is correct and matches approved leave records."
            });
        }
    }

    private static void DetectPayrollItemChanges(List<Anomaly> anomalies, EmployeePayroll prevEmp, EmployeePayroll currEmp, string employeeRef)
    {
        var prevItemTypes = prevEmp.payrollItems?.Select(pi => pi.typeName).Where(t => t != null).ToHashSet() ?? new HashSet<string?>();
        var currItemTypes = currEmp.payrollItems?.Select(pi => pi.typeName).Where(t => t != null).ToHashSet() ?? new HashSet<string?>();

        var missingItems = prevItemTypes.Except(currItemTypes).ToList();
        var newItems = currItemTypes.Except(prevItemTypes).ToList();

        if (missingItems.Count > 0)
        {
            anomalies.Add(new Anomaly
            {
                category = "Payroll Items",
                severity = "medium",
                scope = "employee",
                reference = employeeRef,
                title = "Recurring payroll item(s) missing",
                explanation = $"The following payroll items from last month are missing: {string.Join(", ", missingItems)}.",
                review_suggestion = "Verify if items were intentionally removed or if this is a data entry issue."
            });
        }

        if (newItems.Count > 0)
        {
            // Check if these are deductions (higher scrutiny)
            var newDeductions = currEmp.payrollItems?
                .Where(pi => newItems.Contains(pi.typeName) && pi.isDeduction)
                .Select(pi => pi.typeName)
                .ToList() ?? new List<string?>();

            if (newDeductions.Count > 0)
            {
                anomalies.Add(new Anomaly
                {
                    category = "Payroll Items",
                    severity = "medium",
                    scope = "employee",
                    reference = employeeRef,
                    title = "New deduction(s) added",
                    explanation = $"New deduction item(s) appeared this month: {string.Join(", ", newDeductions)}.",
                    review_suggestion = "Confirm new deductions are authorized and correctly calculated."
                });
            }
        }
    }

    private static void DetectStatutoryChanges(List<Anomaly> anomalies, EmployeePayroll prevEmp, EmployeePayroll currEmp, string employeeRef)
    {
        var prevStatutory = prevEmp.statutoryContribution;
        var currStatutory = currEmp.statutoryContribution;
        var prevBaseSalary = prevEmp.payrollItems?.Where(pi => !pi.isDeduction).Sum(pi => pi.amount ?? 0) ?? 0;
        var currBaseSalary = currEmp.payrollItems?.Where(pi => !pi.isDeduction).Sum(pi => pi.amount ?? 0) ?? 0;

        if (prevStatutory == null || currStatutory == null) return;

        // EPF changes with same salary
        var epfChange = Math.Abs((currStatutory.employeeEpf + currStatutory.employerEpf) - 
                                (prevStatutory.employeeEpf + prevStatutory.employerEpf));
        if (epfChange > 50 && Math.Abs(currBaseSalary - prevBaseSalary) < 1)
        {
            anomalies.Add(new Anomaly
            {
                category = "Statutory",
                severity = "medium",
                scope = "employee",
                reference = employeeRef,
                title = "EPF contribution changed without salary change",
                explanation = $"EPF contribution changed by RM {epfChange:N2} while base salary remained the same.",
                review_suggestion = "Review EPF rate settings or verify if employee category changed."
            });
        }

        // MTD (tax) significant change
        var mtdChange = Math.Abs(currStatutory.employeeMtd - prevStatutory.employeeMtd);
        if (mtdChange > 100)
        {
            anomalies.Add(new Anomaly
            {
                category = "Statutory",
                severity = DetermineSeverity(mtdChange, 200, 500),
                scope = "employee",
                reference = employeeRef,
                title = "Significant MTD (tax) change",
                explanation = $"Monthly Tax Deduction changed by RM {mtdChange:N2} from the previous period.",
                review_suggestion = "Verify tax computation settings and any bonus or allowance changes affecting taxation."
            });
        }

        // SOCSO/EIS unexpected change
        var socsoChange = Math.Abs((currStatutory.employeeSocso + currStatutory.employerSocso) -
                                  (prevStatutory.employeeSocso + prevStatutory.employerSocso));
        if (socsoChange > 20 && Math.Abs(currBaseSalary - prevBaseSalary) < 1)
        {
            anomalies.Add(new Anomaly
            {
                category = "Statutory",
                severity = "low",
                scope = "employee",
                reference = employeeRef,
                title = "SOCSO contribution changed unexpectedly",
                explanation = $"SOCSO contribution changed by RM {socsoChange:N2} without corresponding salary change.",
                review_suggestion = "Check if employee SOCSO category or rate was modified."
            });
        }
    }

    #endregion

    #region Payroll-Wide Detection Methods

    private static void DetectPayrollWideAnomalies(
        List<Anomaly> anomalies, 
        PayrollData previous, 
        PayrollData current,
        List<EmployeePayroll> prevEmployees,
        List<EmployeePayroll> currEmployees)
    {
        var prevTotal = previous.totals;
        var currTotal = current.totals;

        if (prevTotal != null && currTotal != null)
        {
            // Significant total cost change
            var costChange = currTotal.cost - prevTotal.cost;
            var costChangePercent = prevTotal.cost > 0 ? (costChange / prevTotal.cost) * 100 : 0;

            if (Math.Abs(costChangePercent) > 10)
            {
                anomalies.Add(new Anomaly
                {
                    category = "Payroll Total",
                    severity = DetermineSeverity(Math.Abs(costChangePercent), 15, 25),
                    scope = "payroll",
                    reference = "Payroll-wide",
                    title = $"Total payroll cost {(costChange > 0 ? "increased" : "decreased")} significantly",
                    explanation = $"Total payroll cost changed by {Math.Abs(costChangePercent):F1}% (RM {Math.Abs(costChange):N2}) compared to the previous period.",
                    review_suggestion = "Review headcount changes, salary adjustments, or one-time payments contributing to this change."
                });
            }

            // Net pay total vs gross discrepancy check
            var netGrossRatioPrev = prevTotal.gross > 0 ? prevTotal.net / prevTotal.gross : 0;
            var netGrossRatioCurr = currTotal.gross > 0 ? currTotal.net / currTotal.gross : 0;
            var ratioChange = Math.Abs(netGrossRatioCurr - netGrossRatioPrev);

            if (ratioChange > 0.05m)
            {
                anomalies.Add(new Anomaly
                {
                    category = "Payroll Total",
                    severity = "medium",
                    scope = "payroll",
                    reference = "Payroll-wide",
                    title = "Net-to-gross ratio changed significantly",
                    explanation = $"The ratio of net pay to gross pay changed from {netGrossRatioPrev:P1} to {netGrossRatioCurr:P1}, indicating a shift in overall deductions.",
                    review_suggestion = "Investigate changes in statutory rates, deductions, or allowances affecting the entire payroll."
                });
            }
        }

        // Employee count changes
        var prevCount = prevEmployees.Count;
        var currCount = currEmployees.Count;

        if (prevCount != currCount)
        {
            var countDiff = currCount - prevCount;
            anomalies.Add(new Anomaly
            {
                category = "Headcount",
                severity = Math.Abs(countDiff) > 3 ? "medium" : "low",
                scope = "payroll",
                reference = "Payroll-wide",
                title = $"Employee count {(countDiff > 0 ? "increased" : "decreased")} by {Math.Abs(countDiff)}",
                explanation = $"The payroll now includes {currCount} employees compared to {prevCount} in the previous period.",
                review_suggestion = "Verify new hires or terminations are correctly reflected in payroll."
            });
        }
    }

    #endregion

    #region Helper Methods

    private static string DetermineSeverity(decimal amount, decimal mediumThreshold, decimal highThreshold)
    {
        if (amount >= highThreshold) return "high";
        if (amount >= mediumThreshold) return "medium";
        return "low";
    }

    private static string GenerateSummary(List<Anomaly> anomalies)
    {
        if (anomalies.Count == 0)
        {
            return "No significant anomalies detected. The current payroll appears consistent with historical patterns.";
        }

        var highCount = anomalies.Count(a => a.severity == "high");
        var mediumCount = anomalies.Count(a => a.severity == "medium");
        var lowCount = anomalies.Count(a => a.severity == "low");

        var employeeLevel = anomalies.Count(a => a.scope == "employee");
        var payrollLevel = anomalies.Count(a => a.scope == "payroll");

        var categories = anomalies.Select(a => a.category).Distinct().ToList();

        var parts = new List<string>();

        if (highCount > 0)
            parts.Add($"{highCount} high-priority item(s)");
        if (mediumCount > 0)
            parts.Add($"{mediumCount} medium-priority item(s)");
        if (lowCount > 0)
            parts.Add($"{lowCount} low-priority item(s)");

        var prioritySummary = string.Join(", ", parts);
        var categorySummary = string.Join(", ", categories);

        return $"Found {anomalies.Count} anomalies requiring review: {prioritySummary}. " +
               $"Areas affected: {categorySummary}. " +
               $"({employeeLevel} employee-level, {payrollLevel} payroll-wide). " +
               "Please review flagged items before final approval.";
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
