using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class PayrollItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("typeId")]
    public string? TypeId { get; set; }
    
    [JsonPropertyName("isDeduction")]
    public bool IsDeduction { get; set; }
    
    [JsonPropertyName("isRecurring")]
    public bool IsRecurring { get; set; }
    
    [JsonPropertyName("isUnpaidLeaveApplicable")]
    public bool IsUnpaidLeaveApplicable { get; set; }
    
    [JsonPropertyName("isLeavePayApplicable")]
    public bool IsLeavePayApplicable { get; set; }
    
    [JsonPropertyName("typeName")]
    public string? TypeName { get; set; }
    
    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }
    
    [JsonPropertyName("units")]
    public decimal? Units { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }  // Made nullable - JSON can have null
    
    [JsonPropertyName("finalAmount")]
    public decimal? FinalAmount { get; set; }
}
