using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class ChangeGroup
{
    [JsonPropertyName("GroupTitle")]
    public string GroupTitle { get; set; } = "";
    
    [JsonPropertyName("Description")]
    public string Description { get; set; } = "";
    
    [JsonPropertyName("AffectedEmployees")]
    public List<string> AffectedEmployees { get; set; } = new();
    
    [JsonPropertyName("WhyItMatters")]
    public string WhyItMatters { get; set; } = "";
}
