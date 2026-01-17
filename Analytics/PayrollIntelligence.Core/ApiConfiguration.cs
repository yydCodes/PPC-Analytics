namespace PayrollIntelligence.Core;

public class ApiConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ClientId) &&
               !string.IsNullOrWhiteSpace(ClientSecret) &&
               !string.IsNullOrWhiteSpace(BaseUrl);
    }
}

public class ApiRequestLog
{
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? RequestBody { get; set; }
    public int ResponseStatusCode { get; set; }
    public string ResponseHeaders { get; set; } = string.Empty;
    public string ResponseBody { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}