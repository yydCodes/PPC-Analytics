using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PayrollIntelligence.Core;

public class PayrollApiService
{
    private readonly HttpClient _httpClient;
    private readonly ApiConfiguration _apiConfig;
    private string? _bearerToken;
    private DateTime? _tokenExpiry;

    public PayrollApiService(HttpClient httpClient, IOptions<ApiConfiguration> apiConfig)
    {
        _httpClient = httpClient;
        _apiConfig = apiConfig.Value;

        // Set base address from configuration
        _httpClient.BaseAddress = new Uri(_apiConfig.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Set timeout if configured
        if (_apiConfig.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_apiConfig.TimeoutSeconds);
        }
    }

    /// <summary>
    /// Authenticates with the payroll API and obtains a bearer token
    /// </summary>
    /// <param name="clientId">Client ID for authentication</param>
    /// <param name="clientSecret">Client secret for authentication</param>
    /// <returns>True if authentication successful, false otherwise</returns>
    public async Task<bool> AuthenticateAsync(string clientId, string clientSecret)
    {
        return (await AuthenticateAsyncWithLogs(clientId, clientSecret)).IsSuccess;
    }

    /// <summary>
    /// Authenticates with the payroll API and obtains a bearer token with detailed logging
    /// </summary>
    /// <param name="clientId">Client ID for authentication</param>
    /// <param name="clientSecret">Client secret for authentication</param>
    /// <returns>ApiRequestLog containing authentication details</returns>
    public async Task<ApiRequestLog> AuthenticateAsyncWithLogs(string clientId, string clientSecret)
    {
        var log = new ApiRequestLog
        {
            Timestamp = DateTime.UtcNow,
            Method = "POST",
            Url = $"{_apiConfig.BaseUrl}/connect/token"
        };

        try
        {
            // Log the URL being called for debugging
            Console.WriteLine($"Authenticating with URL: {_apiConfig.BaseUrl}/connect/token");

            // Check if we already have a valid token
            if (!string.IsNullOrEmpty(_bearerToken) && _tokenExpiry.HasValue && _tokenExpiry.Value > DateTime.UtcNow.AddMinutes(5))
            {
                log.IsSuccess = true;
                log.ResponseBody = "Using cached token (still valid)";
                return log;
            }

            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["scope"] = "webapi",
                ["client_secret"] = clientSecret,
                ["grant_type"] = "client_credentials"
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            log.RequestBody = string.Join("&", tokenRequest.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            // Capture request headers
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                log.Headers[header.Key] = string.Join(", ", header.Value);
            }

            var startTime = DateTime.UtcNow;
            var response = await _httpClient.PostAsync("/connect/token", content);
            log.Duration = DateTime.UtcNow - startTime;

            log.ResponseStatusCode = (int)response.StatusCode;

            // Capture response headers
            var responseHeaders = new List<string>();
            foreach (var header in response.Headers)
            {
                responseHeaders.Add($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            foreach (var header in response.Content.Headers)
            {
                responseHeaders.Add($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            log.ResponseHeaders = string.Join("\n", responseHeaders);

            var responseContent = await response.Content.ReadAsStringAsync();
            log.ResponseBody = responseContent;

            if (!response.IsSuccessStatusCode)
            {
                log.IsSuccess = false;
                log.ErrorMessage = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
                return log;
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null)
            {
                log.IsSuccess = false;
                log.ErrorMessage = $"Invalid token response: unable to parse JSON. Raw response: {responseContent}";
                return log;
            }

            if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                log.IsSuccess = false;
                log.ErrorMessage = $"Invalid token response: missing access_token field. Parsed response: access_token='{tokenResponse.AccessToken}', token_type='{tokenResponse.TokenType}', expires_in={tokenResponse.ExpiresIn}";
                return log;
            }

            _bearerToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            // Set the authorization header for future requests
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenResponse.TokenType ?? "Bearer", _bearerToken);

            Console.WriteLine($"Authentication successful: Token type={tokenResponse.TokenType}, Expires in {tokenResponse.ExpiresIn} seconds");

            log.IsSuccess = true;
            return log;
        }
        catch (Exception ex)
        {
            log.IsSuccess = false;
            log.ErrorMessage = ex.Message;
            log.ResponseBody = $"Exception: {ex.Message}";
            return log;
        }
    }

    /// <summary>
    /// Retrieves payroll data for a specific year and month
    /// </summary>
    /// <param name="year">Year (e.g., 2026)</param>
    /// <param name="month">Month (1-12)</param>
    /// <returns>PayrollData object or null if failed</returns>
    public async Task<PayrollData?> GetPayrollDataAsync(int year, int month)
    {
        try
        {
            if (string.IsNullOrEmpty(_bearerToken))
            {
                throw new InvalidOperationException("Authentication required. Call AuthenticateAsync first.");
            }

            var url = $"/Payrolls/GetPayroll/{year}/{month}";
            
            // Create request with explicit Authorization header
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            
            Console.WriteLine($"Calling API: {_apiConfig.BaseUrl}{url}");
            Console.WriteLine($"Authorization: Bearer {_bearerToken.Substring(0, Math.Min(20, _bearerToken.Length))}...");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to get payroll data: {response.StatusCode} - {errorContent}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Payroll API response received: {responseContent.Length} characters");
            
            var payrollData = JsonSerializer.Deserialize<PayrollData>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return payrollData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving payroll data: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retrieves all payroll months for a specific year
    /// </summary>
    /// <param name="year">Year to get payroll data for</param>
    /// <returns>List of PayrollYearItem with month and status info</returns>
    public async Task<List<PayrollYearItem>> GetPayrollsByYearAsync(int year)
    {
        try
        {
            if (string.IsNullOrEmpty(_bearerToken))
            {
                throw new InvalidOperationException("Authentication required. Call AuthenticateAsync first.");
            }

            var url = $"/Payrolls/GetPayrolls/{year}";
            
            // Create request with explicit Authorization header
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            
            Console.WriteLine($"Calling API: {_apiConfig.BaseUrl}{url}");
            Console.WriteLine($"Authorization: Bearer {_bearerToken.Substring(0, Math.Min(20, _bearerToken.Length))}...");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to get payrolls for year {year}: {response.StatusCode} - {errorContent}");
                return new List<PayrollYearItem>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Payrolls by year API response received: {responseContent.Length} characters");
            
            // Try to parse as array first, then as object with items property
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            try
            {
                // Try parsing as direct array
                var items = JsonSerializer.Deserialize<List<PayrollYearItem>>(responseContent, options);
                return items ?? new List<PayrollYearItem>();
            }
            catch
            {
                // Try parsing as object with items property
                var result = JsonSerializer.Deserialize<PayrollYearResponse>(responseContent, options);
                return result?.items ?? new List<PayrollYearItem>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving payrolls for year {year}: {ex.Message}");
            return new List<PayrollYearItem>();
        }
    }

    /// <summary>
    /// Retrieves approved payroll months for a specific year (status = 2)
    /// </summary>
    /// <param name="year">Year to get approved payroll months for</param>
    /// <returns>List of approved months (1-12)</returns>
    public async Task<List<int>> GetApprovedPayrollMonthsAsync(int year)
    {
        var payrolls = await GetPayrollsByYearAsync(year);
        return payrolls
            .Where(p => p.status == 2)
            .Select(p => p.month)
            .OrderBy(m => m)
            .ToList();
    }

    /// <summary>
    /// Retrieves draft payroll months for a specific year (status = 0)
    /// </summary>
    /// <param name="year">Year to get draft payroll months for</param>
    /// <returns>List of draft months (1-12)</returns>
    public async Task<List<int>> GetDraftPayrollMonthsAsync(int year)
    {
        var payrolls = await GetPayrollsByYearAsync(year);
        return payrolls
            .Where(p => p.status == 0)
            .Select(p => p.month)
            .OrderBy(m => m)
            .ToList();
    }

    /// <summary>
    /// Gets the payroll status for a specific period
    /// </summary>
    /// <param name="year">Year of the payroll</param>
    /// <param name="month">Month of the payroll</param>
    /// <returns>Payroll status (0, 1, 2) or -1 if not found</returns>
    public async Task<int> GetPayrollStatusAsync(int year, int month)
    {
        var payrolls = await GetPayrollsByYearAsync(year);
        var payroll = payrolls.FirstOrDefault(p => p.month == month);
        return payroll?.status ?? -1;
    }

    /// <summary>
    /// Gets the current authentication status
    /// </summary>
    /// <returns>True if authenticated and token is valid</returns>
    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(_bearerToken) &&
               _tokenExpiry.HasValue &&
               _tokenExpiry.Value > DateTime.UtcNow.AddMinutes(1);
    }
}

internal class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}