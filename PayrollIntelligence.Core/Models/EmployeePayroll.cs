using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class EmployeePayroll
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }
    
    [JsonPropertyName("isIncludedInPayroll")]
    public bool IsIncludedInPayroll { get; set; }
    
    [JsonPropertyName("employeeName")]
    public string? EmployeeName { get; set; }
    
    [JsonPropertyName("employeePosition")]
    public object? EmployeePosition { get; set; }
    
    [JsonPropertyName("employeeNumber")]
    public string? EmployeeNumber { get; set; }
    
    [JsonPropertyName("employeeWorkerStatus")]
    public int EmployeeWorkerStatus { get; set; }
    
    [JsonPropertyName("unpaidLeaveDays")]
    public decimal UnpaidLeaveDays { get; set; }
    
    [JsonPropertyName("leavePayDays")]
    public decimal LeavePayDays { get; set; }
    
    [JsonPropertyName("payrollPolicyId")]
    public string? PayrollPolicyId { get; set; }
    
    [JsonPropertyName("scheduleId")]
    public string? ScheduleId { get; set; }
    
    [JsonPropertyName("showAutoArrearsOption")]
    public bool ShowAutoArrearsOption { get; set; }
    
    [JsonPropertyName("showPreviousMonthPayrollItemsOption")]
    public bool ShowPreviousMonthPayrollItemsOption { get; set; }
    
    [JsonPropertyName("employeeSalaryPeriod")]
    public int EmployeeSalaryPeriod { get; set; }
    
    [JsonPropertyName("statutoryContribution")]
    public StatutoryContribution? StatutoryContribution { get; set; }
    
    [JsonPropertyName("payrollItems")]
    public List<PayrollItem>? PayrollItems { get; set; }
    
    [JsonPropertyName("unpaidLeavePayrollItems")]
    public List<UnpaidLeavePayrollItem>? UnpaidLeavePayrollItems { get; set; }
    
    [JsonPropertyName("leavePayPayrollItem")]
    public LeavePayPayrollItem? LeavePayPayrollItem { get; set; }
    
    [JsonPropertyName("error")]
    public PayrollError? Error { get; set; }
    
    [JsonPropertyName("approvedEmployeeState")]
    public object? ApprovedEmployeeState { get; set; }
}
