using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class ChangeGroup
{
    [JsonPropertyName("group_title")]
    public string GroupTitle { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    
    [JsonPropertyName("affected_employees")]
    public List<string> AffectedEmployees { get; set; } = new();
    
    [JsonPropertyName("why_it_matters")]
    public string WhyItMatters { get; set; } = "";
}
