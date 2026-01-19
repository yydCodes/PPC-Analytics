using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class PayrollItem
{
    [JsonPropertyName("Id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("TypeId")]
    public string? TypeId { get; set; }
    
    [JsonPropertyName("IsDeduction")]
    public bool IsDeduction { get; set; }
    
    [JsonPropertyName("IsRecurring")]
    public bool IsRecurring { get; set; }
    
    [JsonPropertyName("IsUnpaidLeaveApplicable")]
    public bool IsUnpaidLeaveApplicable { get; set; }
    
    [JsonPropertyName("IsLeavePayApplicable")]
    public bool IsLeavePayApplicable { get; set; }
    
    [JsonPropertyName("TypeName")]
    public string? TypeName { get; set; }
    
    [JsonPropertyName("Rate")]
    public decimal? Rate { get; set; }
    
    [JsonPropertyName("Units")]
    public decimal? Units { get; set; }
    
    [JsonPropertyName("Amount")]
    public decimal? Amount { get; set; }  // Made nullable - JSON can have null
    
    [JsonPropertyName("FinalAmount")]
    public decimal? FinalAmount { get; set; }
}
