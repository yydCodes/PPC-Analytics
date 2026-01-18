using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class AnomalyDetectionResult
{
    [JsonPropertyName("anomalies")]
    public List<Anomaly> Anomalies { get; set; } = new();
    
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";            // Overall summary of findings
}
