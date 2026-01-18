using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnomalyCategory
{
    [JsonPropertyName("Pay Consistency")]
    PayConsistency,
    
    [JsonPropertyName("Leave")]
    Leave,
    
    [JsonPropertyName("Payroll Items")]
    PayrollItems,
    
    [JsonPropertyName("Statutory")]
    Statutory,
    
    [JsonPropertyName("Payroll Total")]
    PayrollTotal,
    
    [JsonPropertyName("Headcount")]
    Headcount
}
