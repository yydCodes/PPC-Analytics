using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class KeyDifferencesResult
{
    // Legacy field for backward compatibility
    [JsonPropertyName("KeyDifferences")]
    public List<KeyDifference> KeyDifferences { get; set; } = new();
    
    // New detailed format
    [JsonPropertyName("PayrollOverview")]
    public PayrollOverview? PayrollOverview { get; set; }
    
    [JsonPropertyName("ChangeGroups")]
    public List<ChangeGroup> ChangeGroups { get; set; } = new();
    
    [JsonPropertyName("AttentionItems")]
    public List<AttentionItem> AttentionItems { get; set; } = new();
    
    [JsonPropertyName("ConfidenceLevel")]
    public string ConfidenceLevel { get; set; } = "";
}
