using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class PayrollData
{
    [JsonPropertyName("PayrollId")]
    public string? PayrollId { get; set; }
    
    [JsonPropertyName("Name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("Status")]
    public int Status { get; set; }
    
    [JsonPropertyName("StatutoryMonth")]
    public string? StatutoryMonth { get; set; }
    
    [JsonPropertyName("PeriodStartDate")]
    public string? PeriodStartDate { get; set; }
    
    [JsonPropertyName("PeriodEndDate")]
    public string? PeriodEndDate { get; set; }
    
    [JsonPropertyName("ApprovedDate")]
    public object? ApprovedDate { get; set; }
    
    [JsonPropertyName("Totals")]
    public PayrollTotals? Totals { get; set; }
    
    [JsonPropertyName("EmployeePayrolls")]
    public List<EmployeePayroll>? EmployeePayrolls { get; set; }
    
    [JsonPropertyName("IntegrationSettingsPayrollSyncInfos")]
    public List<object>? IntegrationSettingsPayrollSyncInfos { get; set; }
    
    [JsonPropertyName("TotalCount")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("Items")]
    public List<EmployeePayroll>? Items { get; set; }
    
    [JsonPropertyName("TotalPages")]
    public int TotalPages { get; set; }
}
