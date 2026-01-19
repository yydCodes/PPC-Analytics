using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// Analysis result models
public class PayrollComparisonResult
{
    /// <summary>
    /// Direction of total payroll change: "increase", "decrease", or "stable"
    /// </summary>
    [JsonPropertyName("Direction")]
    public string Direction { get; set; } = "";

    /// <summary>
    /// One-sentence executive summary of the payroll change
    /// </summary>
    [JsonPropertyName("HeadlineSummary")]
    public string HeadlineSummary { get; set; } = "";

    /// <summary>
    /// Observable factors that drove the change
    /// </summary>
    [JsonPropertyName("KeyDrivers")]
    public List<string> KeyDrivers { get; set; } = new();

    /// <summary>
    /// Notable patterns that management should be aware of
    /// </summary>
    [JsonPropertyName("NotableObservations")]
    public List<string> NotableObservations { get; set; } = new();

    /// <summary>
    /// Confidence level of the analysis: "high", "medium", or "low"
    /// </summary>
    [JsonPropertyName("ConfidenceLevel")]
    public string ConfidenceLevel { get; set; } = "";

    /// <summary>
    /// Metrics comparison data for display
    /// </summary>
    [JsonPropertyName("PreviousMetrics")]
    public PayrollMetrics? PreviousMetrics { get; set; }
    
    [JsonPropertyName("CurrentMetrics")]
    public PayrollMetrics? CurrentMetrics { get; set; }
}
