using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// Analysis result models
public class PayrollComparisonResult
{
    /// <summary>
    /// Direction of total payroll change: "increase", "decrease", or "stable"
    /// </summary>
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "";

    /// <summary>
    /// One-sentence executive summary of the payroll change
    /// </summary>
    [JsonPropertyName("headline_summary")]
    public string HeadlineSummary { get; set; } = "";

    /// <summary>
    /// Observable factors that drove the change
    /// </summary>
    [JsonPropertyName("key_drivers")]
    public List<string> KeyDrivers { get; set; } = new();

    /// <summary>
    /// Notable patterns that management should be aware of
    /// </summary>
    [JsonPropertyName("notable_observations")]
    public List<string> NotableObservations { get; set; } = new();

    /// <summary>
    /// Confidence level of the analysis: "high", "medium", or "low"
    /// </summary>
    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; set; } = "";

    /// <summary>
    /// Metrics comparison data for display
    /// </summary>
    [JsonPropertyName("previous_metrics")]
    public PayrollMetrics? PreviousMetrics { get; set; }
    
    [JsonPropertyName("current_metrics")]
    public PayrollMetrics? CurrentMetrics { get; set; }
}
