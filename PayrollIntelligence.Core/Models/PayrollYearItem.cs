using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// API response for GetPayrolls/:year endpoint
public class PayrollYearItem
{
    [JsonPropertyName("payrollId")]
    public string? PayrollId { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("status")]
    public int Status { get; set; }  // 1 = approved, 0 = not approved
    
    [JsonPropertyName("employeesCount")]
    public int EmployeesCount { get; set; }
    
    [JsonPropertyName("statutoryMonth")]
    public string? StatutoryMonth { get; set; }
    
    [JsonPropertyName("year")]
    public int Year { get; set; }
    
    [JsonPropertyName("month")]
    public int Month { get; set; }
}
