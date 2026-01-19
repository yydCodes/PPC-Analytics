using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// API response for GetPayrolls/:year endpoint
public class PayrollYearItem
{
    [JsonPropertyName("PayrollId")]
    public string? PayrollId { get; set; }
    
    [JsonPropertyName("Name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("Status")]
    public int Status { get; set; }  // 1 = approved, 0 = not approved
    
    [JsonPropertyName("EmployeesCount")]
    public int EmployeesCount { get; set; }
    
    [JsonPropertyName("StatutoryMonth")]
    public string? StatutoryMonth { get; set; }
    
    [JsonPropertyName("Year")]
    public int Year { get; set; }
    
    [JsonPropertyName("Month")]
    public int Month { get; set; }
}
