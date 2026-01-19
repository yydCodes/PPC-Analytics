using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class PayrollMetrics
{
    [JsonPropertyName("NetPay")]
    public decimal NetPay { get; set; }
    
    [JsonPropertyName("GrossPay")]
    public decimal GrossPay { get; set; }
    
    [JsonPropertyName("EmployerCost")]
    public decimal EmployerCost { get; set; }
    
    [JsonPropertyName("Headcount")]
    public int Headcount { get; set; }
    
    [JsonPropertyName("PeriodName")]
    public string PeriodName { get; set; } = "";
}
