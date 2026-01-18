using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalySeverity
{
    [JsonPropertyName("low")]
    Low,
    
    [JsonPropertyName("medium")]
    Medium,
    
    [JsonPropertyName("high")]
    High
}
