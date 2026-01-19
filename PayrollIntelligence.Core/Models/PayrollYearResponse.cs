using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// Fallback wrapper class if API returns object instead of array
public class PayrollYearResponse
{
    [JsonPropertyName("Items")]
    public List<PayrollYearItem> Items { get; set; } = new();
}
