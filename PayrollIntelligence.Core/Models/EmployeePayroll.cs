using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class EmployeePayroll
{
    [JsonPropertyName("Id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("EmployeeId")]
    public string? EmployeeId { get; set; }
    
    [JsonPropertyName("IsIncludedInPayroll")]
    public bool IsIncludedInPayroll { get; set; }
    
    [JsonPropertyName("EmployeeName")]
    public string? EmployeeName { get; set; }
    
    [JsonPropertyName("EmployeePosition")]
    public object? EmployeePosition { get; set; }
    
    [JsonPropertyName("EmployeeNumber")]
    public string? EmployeeNumber { get; set; }
    
    [JsonPropertyName("EmployeeWorkerStatus")]
    public int EmployeeWorkerStatus { get; set; }
    
    [JsonPropertyName("UnpaidLeaveDays")]
    public decimal UnpaidLeaveDays { get; set; }
    
    [JsonPropertyName("LeavePayDays")]
    public decimal LeavePayDays { get; set; }
    
    [JsonPropertyName("PayrollPolicyId")]
    public string? PayrollPolicyId { get; set; }
    
    [JsonPropertyName("ScheduleId")]
    public string? ScheduleId { get; set; }
    
    [JsonPropertyName("ShowAutoArrearsOption")]
    public bool ShowAutoArrearsOption { get; set; }
    
    [JsonPropertyName("ShowPreviousMonthPayrollItemsOption")]
    public bool ShowPreviousMonthPayrollItemsOption { get; set; }
    
    [JsonPropertyName("EmployeeSalaryPeriod")]
    public int EmployeeSalaryPeriod { get; set; }
    
    [JsonPropertyName("StatutoryContribution")]
    public StatutoryContribution? StatutoryContribution { get; set; }
    
    [JsonPropertyName("PayrollItems")]
    public List<PayrollItem>? PayrollItems { get; set; }
    
    [JsonPropertyName("UnpaidLeavePayrollItems")]
    public List<UnpaidLeavePayrollItem>? UnpaidLeavePayrollItems { get; set; }
    
    [JsonPropertyName("LeavePayPayrollItem")]
    public LeavePayPayrollItem? LeavePayPayrollItem { get; set; }
    
    [JsonPropertyName("Error")]
    public PayrollError? Error { get; set; }
    
    [JsonPropertyName("ApprovedEmployeeState")]
    public object? ApprovedEmployeeState { get; set; }
}
