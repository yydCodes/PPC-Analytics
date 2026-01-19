using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// New detailed Detailed Changes models
public class PayrollOverview
{
    [JsonPropertyName("Summary")]
    public string Summary { get; set; } = "";
    
    [JsonPropertyName("HeadcountChange")]
    public string HeadcountChange { get; set; } = "";
    
    [JsonPropertyName("GrossPayTrend")]
    public string GrossPayTrend { get; set; } = "";
    
    [JsonPropertyName("NetPayTrend")]
    public string NetPayTrend { get; set; } = "";
    
    [JsonPropertyName("EmployerCostTrend")]
    public string EmployerCostTrend { get; set; } = "";
}
