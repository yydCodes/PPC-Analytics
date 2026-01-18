using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// Payroll data models
public class PayrollTotals
{
    public decimal net { get; set; }
    public decimal cost { get; set; }
    public decimal gross { get; set; }
    public decimal employeeMtd { get; set; }
    public decimal employeeEpf { get; set; }
    public decimal employerEpf { get; set; }
    public decimal employeeEis { get; set; }
    public decimal employerEis { get; set; }
    public decimal employerHrdf { get; set; }
    public decimal employeeSocso { get; set; }
    public decimal employerSocso { get; set; }
    public decimal zakat { get; set; }
    public decimal cp38 { get; set; }
}

public class StatutoryContribution
{
    public decimal net { get; set; }
    public decimal cost { get; set; }
    public decimal gross { get; set; }
    public decimal employeeMtd { get; set; }
    public decimal employeeEpf { get; set; }
    public decimal employerEpf { get; set; }
    public decimal employeeEis { get; set; }
    public decimal employerEis { get; set; }
    public decimal employerHrdf { get; set; }
    public decimal employeeSocso { get; set; }
    public decimal employerSocso { get; set; }
    public decimal zakat { get; set; }
    public decimal cp38 { get; set; }
}

public class PayrollItem
{
    public string? id { get; set; }
    public string? typeId { get; set; }
    public bool isDeduction { get; set; }
    public bool isRecurring { get; set; }
    public bool isUnpaidLeaveApplicable { get; set; }
    public bool isLeavePayApplicable { get; set; }
    public string? typeName { get; set; }
    public decimal? rate { get; set; }
    public decimal? units { get; set; }
    public decimal? amount { get; set; }  // Made nullable - JSON can have null
    public decimal? finalAmount { get; set; }
}

public class UnpaidLeavePayrollItem
{
    public string? employeePayrollId { get; set; }
    public string? payrollItemTypeId { get; set; }
    public string? name { get; set; }
    public decimal amount { get; set; }
    public string? id { get; set; }
    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
    public DateTime? archivedAt { get; set; }
}

public class LeavePayPayrollItem
{
    public string? name { get; set; }
    public decimal amount { get; set; }
}

public class PayrollError
{
    public string? message { get; set; }
}

public class EmployeePayroll
{
    public string? id { get; set; }
    public string? employeeId { get; set; }
    public bool isIncludedInPayroll { get; set; }
    public string? employeeName { get; set; }
    public object? employeePosition { get; set; }
    public string? employeeNumber { get; set; }
    public int employeeWorkerStatus { get; set; }
    public decimal unpaidLeaveDays { get; set; }
    public decimal leavePayDays { get; set; }
    public string? payrollPolicyId { get; set; }
    public string? scheduleId { get; set; }
    public bool showAutoArrearsOption { get; set; }
    public bool showPreviousMonthPayrollItemsOption { get; set; }
    public int employeeSalaryPeriod { get; set; }
    public StatutoryContribution? statutoryContribution { get; set; }
    public List<PayrollItem>? payrollItems { get; set; }
    public List<UnpaidLeavePayrollItem>? unpaidLeavePayrollItems { get; set; }
    public LeavePayPayrollItem? leavePayPayrollItem { get; set; }
    public PayrollError? error { get; set; }
    public object? approvedEmployeeState { get; set; }
}

public class PayrollData
{
    public string? payrollId { get; set; }
    public string? name { get; set; }
    public int status { get; set; }
    public string? statutoryMonth { get; set; }
    public string? periodStartDate { get; set; }
    public string? periodEndDate { get; set; }
    public object? approvedDate { get; set; }
    public PayrollTotals? totals { get; set; }
    public List<EmployeePayroll>? employeePayrolls { get; set; }
    public List<object>? integrationSettingsPayrollSyncInfos { get; set; }
    public int totalCount { get; set; }
    public List<EmployeePayroll>? items { get; set; }
    public int totalPages { get; set; }
}

// Analysis result models
public class PayrollComparisonResult
{
    /// <summary>
    /// Direction of total payroll change: "increase", "decrease", or "stable"
    /// </summary>
    public string direction { get; set; } = "";

    /// <summary>
    /// One-sentence executive summary of the payroll change
    /// </summary>
    public string headline_summary { get; set; } = "";

    /// <summary>
    /// Observable factors that drove the change
    /// </summary>
    public List<string> key_drivers { get; set; } = new();

    /// <summary>
    /// Notable patterns that management should be aware of
    /// </summary>
    public List<string> notable_observations { get; set; } = new();

    /// <summary>
    /// Confidence level of the analysis: "high", "medium", or "low"
    /// </summary>
    public string confidence_level { get; set; } = "";

    /// <summary>
    /// Metrics comparison data for display
    /// </summary>
    public PayrollMetrics? previous_metrics { get; set; }
    public PayrollMetrics? current_metrics { get; set; }

    public string? ai_insight { get; set; }  // AI-generated deeper insight
}

public class PayrollMetrics
{
    public decimal net_pay { get; set; }
    public decimal gross_pay { get; set; }
    public decimal employer_cost { get; set; }
    public int headcount { get; set; }
    public string period_name { get; set; } = "";
}

public class KeyDifference
{
    public string title { get; set; } = "";
    public string explanation { get; set; } = "";
    public string affected_area { get; set; } = "";
}

// New detailed Detailed Changes models
public class PayrollOverview
{
    public string summary { get; set; } = "";
    public string headcount_change { get; set; } = "";
    public string gross_pay_trend { get; set; } = "";
    public string net_pay_trend { get; set; } = "";
    public string employer_cost_trend { get; set; } = "";
}

public class ChangeGroup
{
    public string group_title { get; set; } = "";
    public string description { get; set; } = "";
    public List<string> affected_employees { get; set; } = new();
    public string why_it_matters { get; set; } = "";
}

public class AttentionItem
{
    public string employee { get; set; } = "";
    public string issue { get; set; } = "";
    public string reason { get; set; } = "";
}

public class KeyDifferencesResult
{
    // Legacy field for backward compatibility
    public List<KeyDifference> key_differences { get; set; } = new();
    
    // New detailed format
    public PayrollOverview? payroll_overview { get; set; }
    public List<ChangeGroup> change_groups { get; set; } = new();
    public List<AttentionItem> attention_items { get; set; } = new();
    public string confidence_level { get; set; } = "";
    
    public string? ai_insight { get; set; }  // AI-generated insight about changes
}

public class AnomalyReviewResponse
{
    [JsonPropertyName("review_items")]
    public List<AnomalyReviewItem>? ReviewItems { get; set; }

    [JsonPropertyName("overall_assessment")]
    public string? OverallAssessment { get; set; }
}

public class AnomalyReviewItem
{
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("confidence_level")]
    public string? ConfidenceLevel { get; set; }
}

public class Anomaly
{
    public string category { get; set; } = "";           // e.g., "Pay Consistency", "Leave", "Statutory"
    public string severity { get; set; } = "";           // "low", "medium", "high"
    public string scope { get; set; } = "";              // "employee" or "payroll"
    public string reference { get; set; } = "";          // e.g., "Employee 1" or "Payroll-wide"
    public string title { get; set; } = "";              // Short description
    public string explanation { get; set; } = "";        // Business-friendly explanation
    public string review_suggestion { get; set; } = "";  // What to check before approval
}

public class AnomalyDetectionResult
{
    public List<Anomaly> anomalies { get; set; } = new();
    public string summary { get; set; } = "";            // Overall summary of findings

    public string? ai_insight { get; set; }  // AI-generated risk assessment
}

// API response for GetPayrolls/:year endpoint
public class PayrollYearItem
{
    public string? payrollId { get; set; }
    public string? name { get; set; }
    public int status { get; set; }  // 1 = approved, 0 = not approved
    public int employeesCount { get; set; }
    public string? statutoryMonth { get; set; }
    public int year { get; set; }
    public int month { get; set; }
}

// Fallback wrapper class if API returns object instead of array
public class PayrollYearResponse
{
    public List<PayrollYearItem> items { get; set; } = new();
}