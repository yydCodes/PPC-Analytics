using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class UnpaidLeavePayrollItem
{
    [JsonPropertyName("employeePayrollId")]
    public string? EmployeePayrollId { get; set; }
    
    [JsonPropertyName("payrollItemTypeId")]
    public string? PayrollItemTypeId { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    [JsonPropertyName("archivedAt")]
    public DateTime? ArchivedAt { get; set; }
}
