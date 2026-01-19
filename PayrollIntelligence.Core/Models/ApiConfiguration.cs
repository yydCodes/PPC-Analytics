using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class ApiConfiguration
{
    [JsonPropertyName("ClientId")]
    public string ClientId { get; set; } = string.Empty;
    
    [JsonPropertyName("ClientSecret")]
    public string ClientSecret { get; set; } = string.Empty;
    
    [JsonPropertyName("BaseUrl")]
    public string BaseUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("TimeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ClientId) &&
               !string.IsNullOrWhiteSpace(ClientSecret) &&
               !string.IsNullOrWhiteSpace(BaseUrl);
    }
}
