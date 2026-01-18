using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class PayrollMetrics
{
    [JsonPropertyName("net_pay")]
    public decimal NetPay { get; set; }
    
    [JsonPropertyName("gross_pay")]
    public decimal GrossPay { get; set; }
    
    [JsonPropertyName("employer_cost")]
    public decimal EmployerCost { get; set; }
    
    [JsonPropertyName("headcount")]
    public int Headcount { get; set; }
    
    [JsonPropertyName("period_name")]
    public string PeriodName { get; set; } = "";
}
