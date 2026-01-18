using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class AttentionItem
{
    [JsonPropertyName("employee")]
    public string Employee { get; set; } = "";
    
    [JsonPropertyName("issue")]
    public string Issue { get; set; } = "";
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";
}
