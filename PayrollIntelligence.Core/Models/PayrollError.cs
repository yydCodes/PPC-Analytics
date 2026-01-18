using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class PayrollError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
