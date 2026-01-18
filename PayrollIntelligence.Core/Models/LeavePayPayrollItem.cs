using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class LeavePayPayrollItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}
