using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class PayrollError
{
    [JsonPropertyName("Message")]
    public string? Message { get; set; }
}
