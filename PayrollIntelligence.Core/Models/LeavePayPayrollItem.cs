using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class LeavePayPayrollItem
{
    [JsonPropertyName("Name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; }
}
