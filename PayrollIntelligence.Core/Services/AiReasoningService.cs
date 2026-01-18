using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PayrollIntelligence.Core;

namespace PayrollIntelligence.Core.Services;

public class AiReasoningService
{
    private readonly HttpClient _httpClient;
    private readonly AiConfiguration _config;

    public AiReasoningService(HttpClient httpClient, IOptions<AiConfiguration> config)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    public async Task<string?> GenerateInsightAsync(string prompt, string context, string? feature = null)
    {
        if (!_config.IsValid())
            return null;

        try
        {
            // Use feature-specific prompt if provided and not empty
            string userPrompt;
            if (!string.IsNullOrWhiteSpace(feature))
            {
                var featurePrompt = feature switch
                {
                    "PayrollComparison" => _config.PayrollComparisonPrompt,
                    "PayrollDifferences" => _config.PayrollDifferencesPrompt,
                    "PayrollAnomaly" => _config.PayrollAnomalyPrompt,
                    // Add other features here as needed
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(featurePrompt))
                {
                    userPrompt = $@"Context: {context}

{featurePrompt}";
                }
                else
                {
                    userPrompt = $@"Context: {context}

Task: {prompt}

Provide a concise, actionable insight (2-3 sentences max). Focus on business implications, not technical details.";
                }
            }
            else
            {
                userPrompt = $@"Context: {context}

Task: {prompt}

Provide a concise, actionable insight (2-3 sentences max). Focus on business implications, not technical details.";
            }

            var request = new
            {
                model = _config.ModelName,
                system = _config.SystemPrompt,
                prompt = userPrompt,
                stream = false,
                options = new
                {
                    temperature = _config.Temperature
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Response?.Trim();
        }
        catch
        {
            return null; // Gracefully fallback to rule-based
        }
    }

    public async Task<List<ChangeGroup>?> GenerateChangeGroupsAsync(string context)
    {
        if (!_config.IsValid())
            return null;

        try
        {
            var response = await GenerateInsightAsync("", context, "PayrollDifferences");
            if (string.IsNullOrWhiteSpace(response))
                return null;

            // Try to extract JSON from response (may have markdown code blocks)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < jsonStart)
                return null;

            var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var result = JsonSerializer.Deserialize<ChangeGroupsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.ChangeGroups?.Select(cg => new ChangeGroup
            {
                group_title = cg.Title ?? "",
                description = cg.Description ?? "",
                affected_employees = cg.AffectedEmployees ?? new List<string>(),
                why_it_matters = cg.WhyItMatters ?? ""
            }).ToList();
        }
        catch
        {
            return null; // Gracefully fallback to rule-based
        }
    }

    private class ChangeGroupsResponse
    {
        [JsonPropertyName("change_groups")]
        public List<ChangeGroupItem>? ChangeGroups { get; set; }
    }

    private class ChangeGroupItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("affected_employees")]
        public List<string>? AffectedEmployees { get; set; }
        
        [JsonPropertyName("why_it_matters")]
        public string? WhyItMatters { get; set; }
        
        [JsonPropertyName("confidence_level")]
        public string? ConfidenceLevel { get; set; }
    }


    public async Task<PayrollComparisonAiResponse?> GeneratePayrollComparisonAsync(string context)
    {
        if (!_config.IsValid())
            return null;

        try
        {
            var response = await GenerateInsightAsync("", context, "PayrollComparison");
            if (string.IsNullOrWhiteSpace(response))
                return null;

            // Try to extract JSON from response (may have markdown code blocks)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < jsonStart)
                return null;

            var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var result = JsonSerializer.Deserialize<PayrollComparisonAiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
        catch
        {
            return null; // Gracefully fallback to rule-based
        }
    }

    public class PayrollComparisonAiResponse
    {
        [JsonPropertyName("direction")]
        public string? Direction { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("key_drivers")]
        public List<string>? KeyDrivers { get; set; }

        [JsonPropertyName("confidence_level")]
        public string? ConfidenceLevel { get; set; }
    }
    
    public async Task<string?> EnhanceExplanationAsync(string ruleBasedExplanation, string context)
    {
        var prompt = $"Enhance this payroll explanation to be more business-friendly and insightful: {ruleBasedExplanation}";
        return await GenerateInsightAsync(prompt, context);
    }

    public async Task<AnomalyReviewResponse?> GenerateAnomalyReviewItemsAsync(string context)
    {
        if (!_config.IsValid())
            return null;

        try
        {
            var response = await GenerateInsightAsync("", context, "PayrollAnomaly");
            if (string.IsNullOrWhiteSpace(response))
                return null;

            // Try to extract JSON from response (may have markdown code blocks)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < jsonStart)
                return null;

            var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var result = JsonSerializer.Deserialize<AnomalyReviewResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
        catch
        {
            return null; // Gracefully fallback to rule-based
        }
    }
    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}