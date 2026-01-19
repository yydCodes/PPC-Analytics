using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

public class ApiRequestLog
{
    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("Method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("Url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("Headers")]
    public Dictionary<string, string> Headers { get; set; } = new();
    
    [JsonPropertyName("RequestBody")]
    public string? RequestBody { get; set; }
    
    [JsonPropertyName("ResponseStatusCode")]
    public int ResponseStatusCode { get; set; }
    
    [JsonPropertyName("ResponseHeaders")]
    public string ResponseHeaders { get; set; } = string.Empty;
    
    [JsonPropertyName("ResponseBody")]
    public string ResponseBody { get; set; } = string.Empty;
    
    [JsonPropertyName("Duration")]
    public TimeSpan Duration { get; set; }
    
    [JsonPropertyName("IsSuccess")]
    public bool IsSuccess { get; set; }
    
    [JsonPropertyName("ErrorMessage")]
    public string? ErrorMessage { get; set; }
}
