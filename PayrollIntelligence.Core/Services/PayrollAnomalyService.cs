using Microsoft.Extensions.Options;

namespace PayrollIntelligence.Core.Services;

/// <summary>
/// Service for Risk & Review feature.
/// Detects unusual, risky, or inconsistent changes in payroll data.
/// Uses AI reasoning if available, otherwise falls back to rule-based detection.
/// </summary>
public class PayrollAnomalyService
{
    private readonly PayrollApiService _apiService;
    private readonly ApiConfiguration _apiConfig;
    private readonly AiReasoningService? _aiService;

    public PayrollAnomalyService(
        PayrollApiService apiService, 
        IOptions<ApiConfiguration> apiConfig,
        AiReasoningService? aiService = null)
    {
        _apiService = apiService;
        _apiConfig = apiConfig.Value;
        _aiService = aiService;
    }

    // ... existing DetectAnomaliesAsync method ...

    /// <summary>
    /// Detects anomalies by comparing current payroll against a previous period.
    /// Uses AI if available, otherwise falls back to rule-based detection.
    /// </summary>
    public async Task<AnomalyDetectionResult> DetectAnomaliesAsync(PayrollData previous, PayrollData current)
    {
        // Check if AI service is available and valid
        if (IsAiAvailable())
        {
            try
            {
                // Build context from payroll data for AI analysis
                var context = BuildAnomalyContextFromData(previous, current);
                
                // Get AI review items
                var aiReview = await _aiService!.GenerateAnomalyReviewItemsAsync(context);
                
                if (aiReview != null && aiReview.ReviewItems != null && aiReview.ReviewItems.Count > 0)
                {
                    // Convert AI review items to Anomaly format
                    var aiAnomalies = aiReview.ReviewItems.Select(item => new Anomaly
                    {
                        Category = AnomalyCategory.PayrollItems, // Default category for AI review
                        Severity = Enum.TryParse<AnomalySeverity>(item.Severity ?? "medium", true, out var severity) 
                            ? severity 
                            : AnomalySeverity.Medium,
                        Scope = Enum.TryParse<AnomalyScope>(item.Scope ?? "employee", true, out var scope) 
                            ? scope 
                            : AnomalyScope.Employee,
                        Reference = item.Reference ?? "",
                        Title = item.Title ?? "",
                        Explanation = item.Explanation ?? "",
                        ReviewSuggestion = item.ReviewSuggestion ?? ""
                    }).ToList();

                    // Generate summary
                    var summary = !string.IsNullOrWhiteSpace(aiReview.OverallAssessment)
                        ? aiReview.OverallAssessment
                        : GenerateSummary(aiAnomalies);

                    return new AnomalyDetectionResult
                    {
                        Anomalies = aiAnomalies,
                        Summary = summary
                    };
                }
            }
            catch
            {
                // If AI fails, fall through to rule-based
            }
        }
        
        // Fallback to rule-based detection
        Console.WriteLine("Falling back to rule-based detection");
        return DetectAnomalies(previous, current);
    }

    /// <summary>
    /// Checks if AI service is available and properly configured.
    /// </summary>
    private bool IsAiAvailable()
    {
        return _aiService != null;
    }

    /// <summary>
    /// Builds context from payroll data (not from detected anomalies) for AI analysis.
    /// </summary>
    private string BuildAnomalyContextFromData(PayrollData previous, PayrollData current)
    {
        var context = new System.Text.StringBuilder();
        context.AppendLine("Payroll Data Comparison:");
        context.AppendLine();

        // Add summary statistics
        var prevTotal = previous.Totals;
        var currTotal = current.Totals;
        if (prevTotal != null && currTotal != null)
        {
            context.AppendLine("Payroll Summary:");
            context.AppendLine($"Previous Period: Gross={prevTotal.Gross:N2}, Net={prevTotal.Net:N2}, Cost={prevTotal.Cost:N2}");
            context.AppendLine($"Current Period: Gross={currTotal.Gross:N2}, Net={currTotal.Net:N2}, Cost={currTotal.Cost:N2}");
            context.AppendLine($"Employee Count: Previous={previous.EmployeePayrolls?.Count ?? 0}, Current={current.EmployeePayrolls?.Count ?? 0}");
            context.AppendLine();
        }

        // Add employee-level changes that might indicate anomalies
        var prevEmployees = previous.EmployeePayrolls?
            .Where(e => !string.IsNullOrEmpty(e.EmployeeId))
            .ToDictionary(e => e.EmployeeId!, e => e) ?? new Dictionary<string, EmployeePayroll>();

        foreach (var currEmp in current.EmployeePayrolls ?? new List<EmployeePayroll>())
        {
            var empName = currEmp.EmployeeName ?? currEmp.EmployeeNumber ?? "Unknown";
            var empId = currEmp.EmployeeId ?? "";

            if (string.IsNullOrEmpty(empId) || !prevEmployees.TryGetValue(empId, out var prevEmp))
                continue;

            var changes = new List<string>();

            // Net pay vs salary consistency
            var prevNet = prevEmp.StatutoryContribution?.Net ?? 0;
            var currNet = currEmp.StatutoryContribution?.Net ?? 0;
            var prevBaseSalary = prevEmp.PayrollItems?.Where(pi => !pi.IsDeduction).Sum(pi => pi.Amount ?? 0) ?? 0;
            var currBaseSalary = currEmp.PayrollItems?.Where(pi => !pi.IsDeduction).Sum(pi => pi.Amount ?? 0) ?? 0;

            if (Math.Abs(currNet - prevNet) > 50 && Math.Abs(currBaseSalary - prevBaseSalary) < 1)
            {
                changes.Add($"Net pay changed by {Math.Abs(currNet - prevNet):N2} without base salary change");
            }

            // Leave changes
            var prevUnpaid = prevEmp.UnpaidLeavePayrollItems?.Sum(ul => ul.Amount) ?? 0;
            var currUnpaid = currEmp.UnpaidLeavePayrollItems?.Sum(ul => ul.Amount) ?? 0;
            if (Math.Abs(prevUnpaid - currUnpaid) > 10)
            {
                changes.Add($"Unpaid leave changed from {prevUnpaid:N2} to {currUnpaid:N2}");
            }

            // Statutory changes
            var prevMtd = prevEmp.StatutoryContribution?.EmployeeMtd ?? 0;
            var currMtd = currEmp.StatutoryContribution?.EmployeeMtd ?? 0;
            if (Math.Abs(currMtd - prevMtd) > 100)
            {
                changes.Add($"Tax deduction (MTD) changed by {Math.Abs(currMtd - prevMtd):N2}");
            }

            if (changes.Count > 0)
            {
                context.AppendLine($"- {empName}: {string.Join("; ", changes)}");
            }
        }

        return context.ToString();
    }

    private string BuildAnomalyContext(List<Anomaly> anomalies, PayrollData previous, PayrollData current)
    {
        var context = new System.Text.StringBuilder();
        context.AppendLine("Payroll Risk Signals Detected:");
        context.AppendLine();

        foreach (var anomaly in anomalies)
        {
            context.AppendLine($"- [{anomaly.Severity.ToString().ToUpper()}] {anomaly.Scope}: {anomaly.Reference}");
            context.AppendLine($"  Issue: {anomaly.Title}");
            context.AppendLine($"  Details: {anomaly.Explanation}");
            context.AppendLine();
        }

        // Add summary statistics
        var prevTotal = previous.Totals;
        var currTotal = current.Totals;
        if (prevTotal != null && currTotal != null)
        {
            context.AppendLine("Payroll Summary:");
            context.AppendLine($"Previous Period: Gross={prevTotal.Gross:N2}, Net={prevTotal.Net:N2}, Cost={prevTotal.Cost:N2}");
            context.AppendLine($"Current Period: Gross={currTotal.Gross:N2}, Net={currTotal.Net:N2}, Cost={currTotal.Cost:N2}");
            context.AppendLine($"Employee Count: Previous={previous.EmployeePayrolls?.Count ?? 0}, Current={current.EmployeePayrolls?.Count ?? 0}");
        }

        return context.ToString();
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
                Anomalies = new List<Anomaly>(),
                Summary = $"The selected draft payroll ({draftMonth:D2}/{draftYear}) has status {draftStatus}. Only draft payrolls (status 0) can be analyzed."
            };
        }

        // Validate comparison payroll has status 2
        var comparisonStatus = await _apiService.GetPayrollStatusAsync(comparisonYear, comparisonMonth);
        if (comparisonStatus != 2)
        {
            return new AnomalyDetectionResult
            {
                Anomalies = new List<Anomaly>(),
                Summary = $"The selected comparison payroll ({comparisonMonth:D2}/{comparisonYear}) has status {comparisonStatus}. Only approved payrolls (status 2) can be used for comparison."
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

        return await DetectAnomaliesAsync(comparison, draft);
    }

    /// <summary>
    /// Detects anomalies by comparing current payroll against a previous period.
    /// </summary>
    public static AnomalyDetectionResult DetectAnomalies(PayrollData previous, PayrollData current)
    {
        var anomalies = new List<Anomaly>();

        var prevEmployees = previous.EmployeePayrolls ?? new List<EmployeePayroll>();
        var currEmployees = current.EmployeePayrolls ?? new List<EmployeePayroll>();

        // Build lookup dictionaries by employeeId
        var prevEmployeeDict = prevEmployees
            .Where(e => !string.IsNullOrEmpty(e.EmployeeId))
            .ToDictionary(e => e.EmployeeId!, e => e);

        // ============================================
        // EMPLOYEE-LEVEL ANOMALY DETECTION
        // ============================================

        foreach (var currEmp in currEmployees)
        {
            var employeeRef = currEmp.EmployeeName ?? currEmp.EmployeeNumber ?? currEmp.EmployeeId ?? "Unknown Employee";
            
            // Find matching previous employee
            EmployeePayroll? prevEmp = null;
            if (!string.IsNullOrEmpty(currEmp.EmployeeId) && prevEmployeeDict.TryGetValue(currEmp.EmployeeId, out var found))
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
            Anomalies = anomalies,
            Summary = summary
        };
    }

    #region Employee-Level Detection Methods

    private static void DetectNetPayInconsistency(List<Anomaly> anomalies, EmployeePayroll prevEmp, EmployeePayroll currEmp, string employeeRef)
    {
        var prevNet = prevEmp.StatutoryContribution?.Net ?? 0;
        var currNet = currEmp.StatutoryContribution?.Net ?? 0;
        var prevBaseSalary = prevEmp.PayrollItems?.Where(pi => !pi.IsDeduction).Sum(pi => pi.Amount ?? 0) ?? 0;
        var currBaseSalary = currEmp.PayrollItems?.Where(pi => !pi.IsDeduction).Sum(pi => pi.Amount ?? 0) ?? 0;

        // Net pay changed but base salary didn't
        if (Math.Abs(currNet - prevNet) > 50 && Math.Abs(currBaseSalary - prevBaseSalary) < 1)
        {
            anomalies.Add(new Anomaly
            {
                Category = AnomalyCategory.PayConsistency,
                Severity = DetermineSeverity(Math.Abs(currNet - prevNet), 100, 500),
                Scope = AnomalyScope.Employee,
                Reference = employeeRef,
                Title = "Net pay changed without base salary change",
                Explanation = $"{employeeRef}'s net pay changed by RM {Math.Abs(currNet - prevNet):N2} compared to last month while the base salary remained the same, suggesting changes in deductions, leave, or statutory contributions.",
                ReviewSuggestion = "Review deductions, leave entries, or statutory contribution changes before approval."
            });
        }
    }

    private static void DetectLeaveChanges(List<Anomaly> anomalies, EmployeePayroll prevEmp, EmployeePayroll currEmp, string employeeRef)
    {
        var prevUnpaidLeaveTotal = prevEmp.UnpaidLeavePayrollItems?.Sum(ul => ul.Amount) ?? 0;
        var currUnpaidLeaveTotal = currEmp.UnpaidLeavePayrollItems?.Sum(ul => ul.Amount) ?? 0;
        var currUnpaidDays = currEmp.UnpaidLeaveDays;

        // Unpaid leave appeared this month
        if (prevUnpaidLeaveTotal == 0 && currUnpaidLeaveTotal > 0)
        {
            anomalies.Add(new Anomaly
            {
                Category = AnomalyCategory.Leave,
                Severity = AnomalySeverity.Medium,
                Scope = AnomalyScope.Employee,
                Reference = employeeRef,
                Title = "Unpaid leave appeared this month",
                Explanation = $"Unpaid leave deduction of RM {currUnpaidLeaveTotal:N2} ({currUnpaidDays} days) was present in the current payroll but not in the previous period.",
                ReviewSuggestion = "Confirm unpaid leave records and employee notification with HR."
            });
        }

        // Unpaid leave disappeared
        if (prevUnpaidLeaveTotal > 0 && currUnpaidLeaveTotal == 0)
        {
            anomalies.Add(new Anomaly
            {
                Category = AnomalyCategory.Leave,
                Severity = AnomalySeverity.Low,
                Scope = AnomalyScope.Employee,
                Reference = employeeRef,
                Title = "Unpaid leave deduction removed",
                Explanation = $"Previous unpaid leave deduction of RM {prevUnpaidLeaveTotal:N2} is no longer present in the current payroll.",
                ReviewSuggestion = "Verify that unpaid leave was correctly processed or confirm if it was reversed intentionally."
            });
        }

        // Leave pay changes
        var prevLeavePay = prevEmp.LeavePayPayrollItem?.Amount ?? 0;
        var currLeavePay = currEmp.LeavePayPayrollItem?.Amount ?? 0;

        if (prevLeavePay == 0 && currLeavePay > 0)
        {
            anomalies.Add(new Anomaly
            {
                Category = AnomalyCategory.Leave,
                Severity = AnomalySeverity.Low,
                Scope = AnomalyScope.Employee,
                Reference = employeeRef,
                Title = "Leave pay added this month",
                Explanation = $"Leave pay of RM {currLeavePay:N2} was added in the current payroll.",
                ReviewSuggestion = "Confirm leave pay calculation is correct and matches approved leave records."
            });
        }
    }

    private static void DetectPayrollItemChanges(List<Anomaly> anomalies, EmployeePayroll prevEmp, EmployeePayroll currEmp, string employeeRef)
    {
        var prevItemTypes = prevEmp.PayrollItems?.Select(pi => pi.TypeName).Where(t => t != null).ToHashSet() ?? new HashSet<string?>();
        var currItemTypes = currEmp.PayrollItems?.Select(pi => pi.TypeName).Where(t => t != null).ToHashSet() ?? new HashSet<string?>();

        var missingItems = prevItemTypes.Except(currItemTypes).ToList();
        var newItems = currItemTypes.Except(prevItemTypes).ToList();

        if (missingItems.Count > 0)
        {
            anomalies.Add(new Anomaly
            {
                Category = AnomalyCategory.PayrollItems,
                Severity = AnomalySeverity.Medium,
                Scope = AnomalyScope.Employee,
                Reference = employeeRef,
                Title = "Recurring payroll item(s) missing",
                Explanation = $"The following payroll items from last month are missing: {string.Join(", ", missingItems)}.",
                ReviewSuggestion = "Verify if items were intentionally removed or if this is a data entry issue."
            });
        }

        if (newItems.Count > 0)
        {
            // Check if these are deductions (higher scrutiny)
            var newDeductions = currEmp.PayrollItems?
                .Where(pi => newItems.Contains(pi.TypeName) && pi.IsDeduction)
                .Select(pi => pi.TypeName)
                .ToList() ?? new List<string?>();

            if (newDeductions.Count > 0)
            {
                anomalies.Add(new Anomaly
                {
                    Category = AnomalyCategory.PayrollItems,
                    Severity = AnomalySeverity.Medium,
                    Scope = AnomalyScope.Employee,
                    Reference = employeeRef,
                    Title = "New deduction(s) added",
                    Explanation = $"New deduction item(s) appeared this month: {string.Join(", ", newDeductions)}.",
                    ReviewSuggestion = "Confirm new deductions are authorized and correctly calculated."
                });
            }
        }
    }

    private static void DetectStatutoryChanges(List<Anomaly> anomalies, EmployeePayroll prevEmp, EmployeePayroll currEmp, string employeeRef)
    {
        var prevStatutory = prevEmp.StatutoryContribution;
        var currStatutory = currEmp.StatutoryContribution;
        var prevBaseSalary = prevEmp.PayrollItems?.Where(pi => !pi.IsDeduction).Sum(pi => pi.Amount ?? 0) ?? 0;
        var currBaseSalary = currEmp.PayrollItems?.Where(pi => !pi.IsDeduction).Sum(pi => pi.Amount ?? 0) ?? 0;

        if (prevStatutory == null || currStatutory == null) return;

        // EPF changes with same salary
        var epfChange = Math.Abs((currStatutory.EmployeeEpf + currStatutory.EmployerEpf) - 
                                (prevStatutory.EmployeeEpf + prevStatutory.EmployerEpf));
        if (epfChange > 50 && Math.Abs(currBaseSalary - prevBaseSalary) < 1)
        {
            anomalies.Add(new Anomaly
            {
                Category = AnomalyCategory.Statutory,
                Severity = AnomalySeverity.Medium,
                Scope = AnomalyScope.Employee,
                Reference = employeeRef,
                Title = "EPF contribution changed without salary change",
                Explanation = $"EPF contribution changed by RM {epfChange:N2} while base salary remained the same.",
                ReviewSuggestion = "Review EPF rate settings or verify if employee category changed."
            });
        }

        // MTD (tax) significant change
        var mtdChange = Math.Abs(currStatutory.EmployeeMtd - prevStatutory.EmployeeMtd);
        if (mtdChange > 100)
        {
            anomalies.Add(new Anomaly
            {
                Category = AnomalyCategory.Statutory,
                Severity = DetermineSeverity(mtdChange, 200, 500),
                Scope = AnomalyScope.Employee,
                Reference = employeeRef,
                Title = "Significant MTD (tax) change",
                Explanation = $"Monthly Tax Deduction changed by RM {mtdChange:N2} from the previous period.",
                ReviewSuggestion = "Verify tax computation settings and any bonus or allowance changes affecting taxation."
            });
        }

        // SOCSO/EIS unexpected change
        var socsoChange = Math.Abs((currStatutory.EmployeeSocso + currStatutory.EmployerSocso) -
                                  (prevStatutory.EmployeeSocso + prevStatutory.EmployerSocso));
        if (socsoChange > 20 && Math.Abs(currBaseSalary - prevBaseSalary) < 1)
        {
            anomalies.Add(new Anomaly
            {
                Category = AnomalyCategory.Statutory,
                Severity = AnomalySeverity.Low,
                Scope = AnomalyScope.Employee,
                Reference = employeeRef,
                Title = "SOCSO contribution changed unexpectedly",
                Explanation = $"SOCSO contribution changed by RM {socsoChange:N2} without corresponding salary change.",
                ReviewSuggestion = "Check if employee SOCSO category or rate was modified."
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
        var prevTotal = previous.Totals;
        var currTotal = current.Totals;

        if (prevTotal != null && currTotal != null)
        {
            // Significant total cost change
            var costChange = currTotal.Cost - prevTotal.Cost;
            var costChangePercent = prevTotal.Cost > 0 ? (costChange / prevTotal.Cost) * 100 : 0;

            if (Math.Abs(costChangePercent) > 10)
            {
                anomalies.Add(new Anomaly
                {
                    Category = AnomalyCategory.PayrollTotal,
                    Severity = DetermineSeverity(Math.Abs(costChangePercent), 15, 25),
                    Scope = AnomalyScope.Payroll,
                    Reference = "Payroll-wide",
                    Title = $"Total payroll cost {(costChange > 0 ? "increased" : "decreased")} significantly",
                    Explanation = $"Total payroll cost changed by {Math.Abs(costChangePercent):F1}% (RM {Math.Abs(costChange):N2}) compared to the previous period.",
                    ReviewSuggestion = "Review headcount changes, salary adjustments, or one-time payments contributing to this change."
                });
            }

            // Net pay total vs gross discrepancy check
            var netGrossRatioPrev = prevTotal.Gross > 0 ? prevTotal.Net / prevTotal.Gross : 0;
            var netGrossRatioCurr = currTotal.Gross > 0 ? currTotal.Net / currTotal.Gross : 0;
            var ratioChange = Math.Abs(netGrossRatioCurr - netGrossRatioPrev);

            if (ratioChange > 0.05m)
            {
                anomalies.Add(new Anomaly
                {
                    Category = AnomalyCategory.PayrollTotal,
                    Severity = AnomalySeverity.Medium,
                    Scope = AnomalyScope.Payroll,
                    Reference = "Payroll-wide",
                    Title = "Net-to-gross ratio changed significantly",
                    Explanation = $"The ratio of net pay to gross pay changed from {netGrossRatioPrev:P1} to {netGrossRatioCurr:P1}, indicating a shift in overall deductions.",
                    ReviewSuggestion = "Investigate changes in statutory rates, deductions, or allowances affecting the entire payroll."
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
                Category = AnomalyCategory.Headcount,
                Severity = Math.Abs(countDiff) > 3 ? AnomalySeverity.Medium : AnomalySeverity.Low,
                Scope = AnomalyScope.Payroll,
                Reference = "Payroll-wide",
                Title = $"Employee count {(countDiff > 0 ? "increased" : "decreased")} by {Math.Abs(countDiff)}",
                Explanation = $"The payroll now includes {currCount} employees compared to {prevCount} in the previous period.",
                ReviewSuggestion = "Verify new hires or terminations are correctly reflected in payroll."
            });
        }
    }

    #endregion

    #region Helper Methods

    private static AnomalySeverity DetermineSeverity(decimal amount, decimal mediumThreshold, decimal highThreshold)
    {
        if (amount >= highThreshold) return AnomalySeverity.High;
        if (amount >= mediumThreshold) return AnomalySeverity.Medium;
        return AnomalySeverity.Low;
    }

    private static string GenerateSummary(List<Anomaly> anomalies)
    {
        if (anomalies.Count == 0)
        {
            return "No significant anomalies detected. The current payroll appears consistent with historical patterns.";
        }

        var highCount = anomalies.Count(a => a.Severity == AnomalySeverity.High);
        var mediumCount = anomalies.Count(a => a.Severity == AnomalySeverity.Medium);
        var lowCount = anomalies.Count(a => a.Severity == AnomalySeverity.Low);

        var employeeLevel = anomalies.Count(a => a.Scope == AnomalyScope.Employee);
        var payrollLevel = anomalies.Count(a => a.Scope == AnomalyScope.Payroll);

        var categories = anomalies.Select(a => a.Category).Distinct().ToList();

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
