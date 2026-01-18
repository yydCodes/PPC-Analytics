using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class PayrollData
{
    [JsonPropertyName("payrollId")]
    public string? PayrollId { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("status")]
    public int Status { get; set; }
    
    [JsonPropertyName("statutoryMonth")]
    public string? StatutoryMonth { get; set; }
    
    [JsonPropertyName("periodStartDate")]
    public string? PeriodStartDate { get; set; }
    
    [JsonPropertyName("periodEndDate")]
    public string? PeriodEndDate { get; set; }
    
    [JsonPropertyName("approvedDate")]
    public object? ApprovedDate { get; set; }
    
    [JsonPropertyName("totals")]
    public PayrollTotals? Totals { get; set; }
    
    [JsonPropertyName("employeePayrolls")]
    public List<EmployeePayroll>? EmployeePayrolls { get; set; }
    
    [JsonPropertyName("integrationSettingsPayrollSyncInfos")]
    public List<object>? IntegrationSettingsPayrollSyncInfos { get; set; }
    
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("items")]
    public List<EmployeePayroll>? Items { get; set; }
    
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}
