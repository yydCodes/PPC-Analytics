using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class KeyDifference
{
    [JsonPropertyName("Title")]
    public string Title { get; set; } = "";
    
    [JsonPropertyName("Explanation")]
    public string Explanation { get; set; } = "";
    
    [JsonPropertyName("AffectedArea")]
    public string AffectedArea { get; set; } = "";
}
