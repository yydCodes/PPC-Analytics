using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class AnomalyDetectionResult
{
    [JsonPropertyName("Anomalies")]
    public List<Anomaly> Anomalies { get; set; } = new();
    
    [JsonPropertyName("Summary")]
    public string Summary { get; set; } = "";            // Overall summary of findings
}
