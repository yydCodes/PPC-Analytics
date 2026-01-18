using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class KeyDifference
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";
    
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";
    
    [JsonPropertyName("affected_area")]
    public string AffectedArea { get; set; } = "";
}
