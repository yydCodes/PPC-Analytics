using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class Anomaly
{
    [JsonPropertyName("category")]
    public AnomalyCategory Category { get; set; }
    
    [JsonPropertyName("severity")]
    public AnomalySeverity Severity { get; set; }
    
    [JsonPropertyName("scope")]
    public AnomalyScope Scope { get; set; }
    
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";          // e.g., "Employee 1" or "Payroll-wide"
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";              // Short description
    
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";        // Business-friendly explanation
    
    [JsonPropertyName("review_suggestion")]
    public string ReviewSuggestion { get; set; } = "";  // What to check before approval
}
