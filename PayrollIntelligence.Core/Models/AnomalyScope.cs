using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalyScope
{
    [JsonPropertyName("Employee")]
    Employee,
    
    [JsonPropertyName("Payroll")]
    Payroll
}
