using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalySeverity
{
    [JsonPropertyName("Low")]
    Low,
    
    [JsonPropertyName("Medium")]
    Medium,
    
    [JsonPropertyName("High")]
    High
}
