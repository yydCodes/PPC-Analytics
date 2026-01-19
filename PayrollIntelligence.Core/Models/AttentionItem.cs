using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class AttentionItem
{
    [JsonPropertyName("Employee")]
    public string Employee { get; set; } = "";
    
    [JsonPropertyName("Issue")]
    public string Issue { get; set; } = "";
    
    [JsonPropertyName("Reason")]
    public string Reason { get; set; } = "";
}
