using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class KeyDifferencesResult
{
    // Legacy field for backward compatibility
    [JsonPropertyName("key_differences")]
    public List<KeyDifference> KeyDifferences { get; set; } = new();
    
    // New detailed format
    [JsonPropertyName("payroll_overview")]
    public PayrollOverview? PayrollOverview { get; set; }
    
    [JsonPropertyName("change_groups")]
    public List<ChangeGroup> ChangeGroups { get; set; } = new();
    
    [JsonPropertyName("attention_items")]
    public List<AttentionItem> AttentionItems { get; set; } = new();
    
    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; set; } = "";
}
