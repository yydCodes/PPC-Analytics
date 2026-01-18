using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalyScope
{
    [JsonPropertyName("employee")]
    Employee,
    
    [JsonPropertyName("payroll")]
    Payroll
}
