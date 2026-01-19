using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class Anomaly
{
    [JsonPropertyName("Category")]
    public AnomalyCategory Category { get; set; }
    
    [JsonPropertyName("Severity")]
    public AnomalySeverity Severity { get; set; }
    
    [JsonPropertyName("Scope")]
    public AnomalyScope Scope { get; set; }
    
    [JsonPropertyName("Reference")]
    public string Reference { get; set; } = "";          // e.g., "Employee 1" or "Payroll-wide"
    
    [JsonPropertyName("Title")]
    public string Title { get; set; } = "";              // Short description
    
    [JsonPropertyName("Explanation")]
    public string Explanation { get; set; } = "";        // Business-friendly explanation
    
    [JsonPropertyName("ReviewSuggestion")]
    public string ReviewSuggestion { get; set; } = "";  // What to check before approval
}
