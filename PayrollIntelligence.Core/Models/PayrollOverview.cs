using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// New detailed Detailed Changes models
public class PayrollOverview
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";
    
    [JsonPropertyName("headcount_change")]
    public string HeadcountChange { get; set; } = "";
    
    [JsonPropertyName("gross_pay_trend")]
    public string GrossPayTrend { get; set; } = "";
    
    [JsonPropertyName("net_pay_trend")]
    public string NetPayTrend { get; set; } = "";
    
    [JsonPropertyName("employer_cost_trend")]
    public string EmployerCostTrend { get; set; } = "";
}
