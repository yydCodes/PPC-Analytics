using System.Net.Http;
using Microsoft.Extensions.Options;
using PayrollIntelligence.Core;
using PayrollIntelligence.Core.Services;

namespace PayrollIntelligence.Tests.Helpers;

/// <summary>
/// Test helper for creating services with mock AI behavior
/// Since we can't easily mock AiReasoningService without an interface,
/// we'll test the hybrid scenarios by creating services with null AI (rule-based)
/// and create separate integration-style tests that would use real AI.
/// 
/// For unit testing hybrid scenarios, we'll focus on testing:
/// 1. The static rule-based methods (already covered)
/// 2. The hybrid result building methods indirectly through integration
/// 3. Fallback behavior when AI is null
/// </summary>
public static class ServiceTestHelper
{
    /// <summary>
    /// Creates a PayrollComparisonService with optional AI service
    /// </summary>
    public static PayrollComparisonService CreateComparisonService(
        AiReasoningService? aiService = null)
    {
        var httpClient = new HttpClient();
        var apiConfig = Options.Create(new ApiConfiguration
        {
            BaseUrl = "http://test-api",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        });
        var apiService = new PayrollApiService(httpClient, apiConfig);
        return new PayrollComparisonService(apiService, apiConfig, aiService);
    }

    /// <summary>
    /// Creates a PayrollDifferencesService with optional AI service
    /// </summary>
    public static PayrollDifferencesService CreateDifferencesService(
        AiReasoningService? aiService = null)
    {
        var httpClient = new HttpClient();
        var apiConfig = Options.Create(new ApiConfiguration
        {
            BaseUrl = "http://test-api",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        });
        var apiService = new PayrollApiService(httpClient, apiConfig);
        return new PayrollDifferencesService(apiService, apiConfig, aiService);
    }

    /// <summary>
    /// Creates a PayrollAnomalyService with optional AI service
    /// </summary>
    public static PayrollAnomalyService CreateAnomalyService(
        AiReasoningService? aiService = null)
    {
        var httpClient = new HttpClient();
        var apiConfig = Options.Create(new ApiConfiguration
        {
            BaseUrl = "http://test-api",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        });
        var apiService = new PayrollApiService(httpClient, apiConfig);
        return new PayrollAnomalyService(apiService, apiConfig, aiService);
    }
}
