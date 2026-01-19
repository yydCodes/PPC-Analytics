using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class UnpaidLeavePayrollItem
{
    [JsonPropertyName("EmployeePayrollId")]
    public string? EmployeePayrollId { get; set; }
    
    [JsonPropertyName("PayrollItemTypeId")]
    public string? PayrollItemTypeId { get; set; }
    
    [JsonPropertyName("Name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("Id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("CreatedAt")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonPropertyName("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    [JsonPropertyName("ArchivedAt")]
    public DateTime? ArchivedAt { get; set; }
}
