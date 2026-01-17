using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using PayrollIntelligence.Web.Models;
using PayrollIntelligence.Core;

namespace PayrollIntelligence.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly PayrollApiService _apiService;
    private readonly PayrollAnalysisService _analysisService;
    private readonly IOptions<ApiConfiguration> _apiConfig;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfigurationRoot _configurationRoot;

    public HomeController(
        ILogger<HomeController> logger,
        PayrollApiService apiService,
        PayrollAnalysisService analysisService,
        IOptions<ApiConfiguration> apiConfig,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _logger = logger;
        _apiService = apiService;
        _analysisService = analysisService;
        _apiConfig = apiConfig;
        _environment = environment;
        _configurationRoot = (IConfigurationRoot)configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Comparison()
    {
        // Show modal for period selection
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Comparison(int draftYear, int draftMonth, int comparisonYear, int comparisonMonth)
    {
        try
        {
            // Store selected periods in ViewBag for display
            ViewBag.DraftPeriod = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(draftMonth)} {draftYear}";
            ViewBag.ComparisonPeriod = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(comparisonMonth)} {comparisonYear}";

            // Ensure API configuration is valid
            if (!_apiConfig.Value.IsValid())
            {
                ViewBag.Error = "API settings are incomplete. Please configure Client ID, Client Secret, and Base URL in Settings before running analysis.";
                return View();
            }

            // Compare draft payroll (current) against approved payroll (previous)
            var result = await _analysisService.AnalyzePayrollComparisonAsync(comparisonYear, comparisonMonth, draftYear, draftMonth);
            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in payroll comparison");
            ViewBag.Error = $"Unable to load payroll data from the API: {ex.Message}";
            return View();
        }
    }

    [HttpGet]
    public IActionResult Differences()
    {
        // Show modal for period selection
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Differences(int draftYear, int draftMonth, int comparisonYear, int comparisonMonth)
    {
        try
        {
            // Store selected periods in ViewBag for display
            ViewBag.DraftPeriod = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(draftMonth)} {draftYear}";
            ViewBag.ComparisonPeriod = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(comparisonMonth)} {comparisonYear}";

            // Ensure API configuration is valid
            if (!_apiConfig.Value.IsValid())
            {
                ViewBag.Error = "API settings are incomplete. Please configure Client ID, Client Secret, and Base URL in Settings before running analysis.";
                return View();
            }

            // Compare draft payroll (current) against approved payroll (previous)
            var result = await _analysisService.AnalyzeKeyDifferencesAsync(comparisonYear, comparisonMonth, draftYear, draftMonth);
            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in differences analysis");
            ViewBag.Error = $"Unable to load payroll data from the API: {ex.Message}";
            return View();
        }
    }

    [HttpGet]
    public IActionResult Anomalies()
    {
        // Show modal for period selection
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Anomalies(int draftYear, int draftMonth, int comparisonYear, int comparisonMonth)
    {
        try
        {
            // Store selected periods in ViewBag for display
            ViewBag.DraftPeriod = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(draftMonth)} {draftYear}";
            ViewBag.ComparisonPeriod = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(comparisonMonth)} {comparisonYear}";

            // Ensure API configuration is valid
            if (!_apiConfig.Value.IsValid())
            {
                ViewBag.Error = "API settings are incomplete. Please configure Client ID, Client Secret, and Base URL in Settings before running analysis.";
                return View();
            }

            // Compare draft payroll (status 0) against selected approved payroll (status 2)
            var result = await _analysisService.DetectAnomaliesAsync(draftYear, draftMonth, comparisonYear, comparisonMonth);
            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in anomaly detection");
            ViewBag.Error = $"Unable to load payroll data from the API: {ex.Message}";
            return View();
        }
    }

    [HttpGet]
    public IActionResult Settings()
    {
        ViewBag.ClientId = _apiConfig.Value.ClientId;
        ViewBag.ClientSecret = _apiConfig.Value.ClientSecret;
        ViewBag.IsAuthenticated = _apiService.IsAuthenticated();
        ViewBag.ApiConfig = _apiConfig.Value;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Settings(string clientId, string clientSecret)
    {
        try
        {
            // Determine which appsettings file to update
            var appsettingsFile = _environment.IsDevelopment()
                ? Path.Combine(_environment.ContentRootPath, "appsettings.Development.json")
                : Path.Combine(_environment.ContentRootPath, "appsettings.json");

            // Read the current appsettings file
            var jsonContent = await System.IO.File.ReadAllTextAsync(appsettingsFile);
            var jsonNode = JsonNode.Parse(jsonContent);

            // Update ClientId and ClientSecret in PayrollApi section
            if (jsonNode != null && jsonNode["PayrollApi"] != null)
            {
                jsonNode["PayrollApi"]!["ClientId"] = clientId;
                jsonNode["PayrollApi"]!["ClientSecret"] = clientSecret;
            }

            // Write updated configuration back to file with proper formatting
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var updatedJson = jsonNode?.ToJsonString(options) ?? jsonContent;
            await System.IO.File.WriteAllTextAsync(appsettingsFile, updatedJson);

            // Reload configuration
            _configurationRoot.Reload();

            // Update in-memory config
            _apiConfig.Value.ClientId = clientId;
            _apiConfig.Value.ClientSecret = clientSecret;

            // Attempt to authenticate with new credentials and capture logs
            var authLog = await _apiService.AuthenticateAsyncWithLogs(clientId, clientSecret);

            if (authLog.IsSuccess)
            {
                TempData["SuccessMessage"] = "API credentials saved successfully and authentication successful!";
            }
            else
            {
                TempData["ErrorMessage"] = "Credentials saved but authentication failed. Please check Client ID and Secret.";
            }

            ViewBag.ClientId = _apiConfig.Value.ClientId;
            ViewBag.ClientSecret = _apiConfig.Value.ClientSecret;
            ViewBag.IsAuthenticated = authLog.IsSuccess;
            ViewBag.ApiConfig = _apiConfig.Value;
            ViewBag.AuthResponse = authLog.ResponseBody;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving API credentials");
            TempData["ErrorMessage"] = $"Failed to save credentials: {ex.Message}";
            
            ViewBag.ClientId = clientId;
            ViewBag.ClientSecret = clientSecret;
            ViewBag.IsAuthenticated = false;
            ViewBag.ApiConfig = _apiConfig.Value;
        }

        return View();
    }

    /// <summary>
    /// AJAX endpoint to get approved payroll months for a year
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetApprovedMonths(int year)
    {
        try
        {
            // Ensure API configuration is valid
            if (!_apiConfig.Value.IsValid())
            {
                return Json(new { success = false, message = "API settings are incomplete. Please configure in Settings." });
            }

            // Authenticate if needed
            if (!_apiService.IsAuthenticated())
            {
                var authSuccess = await _apiService.AuthenticateAsync(_apiConfig.Value.ClientId, _apiConfig.Value.ClientSecret);
                if (!authSuccess)
                {
                    return Json(new { success = false, message = "Authentication failed. Please check credentials in Settings." });
                }
            }

            // Get approved months
            var approvedMonths = await _apiService.GetApprovedPayrollMonthsAsync(year);
            
            // Convert to month names for display
            var monthsData = approvedMonths.Select(m => new
            {
                value = m,
                name = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
            }).ToList();

            return Json(new { success = true, months = monthsData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching approved months for year {Year}", year);
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// AJAX endpoint to get draft payroll months for a year (status = 0)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDraftMonths(int year)
    {
        try
        {
            // Ensure API configuration is valid
            if (!_apiConfig.Value.IsValid())
            {
                return Json(new { success = false, message = "API settings are incomplete. Please configure in Settings." });
            }

            // Authenticate if needed
            if (!_apiService.IsAuthenticated())
            {
                var authSuccess = await _apiService.AuthenticateAsync(_apiConfig.Value.ClientId, _apiConfig.Value.ClientSecret);
                if (!authSuccess)
                {
                    return Json(new { success = false, message = "Authentication failed. Please check credentials in Settings." });
                }
            }

            // Get draft months
            var draftMonths = await _apiService.GetDraftPayrollMonthsAsync(year);
            
            // Convert to month names for display
            var monthsData = draftMonths.Select(m => new
            {
                value = m,
                name = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
            }).ToList();

            return Json(new { success = true, months = monthsData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching draft months for year {Year}", year);
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// AJAX endpoint to get a quick preview of payroll analysis for the home page.
    /// Auto-detects the latest draft and approved payroll periods.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetQuickPreview()
    {
        try
        {
            // Check API configuration
            if (!_apiConfig.Value.IsValid())
            {
                return Json(new { 
                    success = false, 
                    reason = "not_configured",
                    message = "API settings are incomplete. Please configure in Settings." 
                });
            }

            // Authenticate if needed
            if (!_apiService.IsAuthenticated())
            {
                var authSuccess = await _apiService.AuthenticateAsync(_apiConfig.Value.ClientId, _apiConfig.Value.ClientSecret);
                if (!authSuccess)
                {
                    return Json(new { 
                        success = false, 
                        reason = "auth_failed",
                        message = "Authentication failed. Please check credentials in Settings." 
                    });
                }
            }

            var currentYear = DateTime.Now.Year;

            // Get first (earliest) draft month
            var draftMonths = await _apiService.GetDraftPayrollMonthsAsync(currentYear);
            if (draftMonths.Count == 0)
            {
                // Try previous year
                draftMonths = await _apiService.GetDraftPayrollMonthsAsync(currentYear - 1);
                if (draftMonths.Count == 0)
                {
                    return Json(new { 
                        success = false, 
                        reason = "no_draft",
                        message = "No draft payroll found. Create a draft payroll to see the preview." 
                    });
                }
                currentYear = currentYear - 1;
            }

            // Use the first (earliest) draft month to match the modal behavior
            var draftMonth = draftMonths.Min();
            var draftYear = currentYear;

            // Get latest approved month before the draft month
            var approvedMonths = await _apiService.GetApprovedPayrollMonthsAsync(draftYear);
            int approvedMonth, approvedYear;

            if (approvedMonths.Count > 0)
            {
                // Get the latest approved month that's before the draft month
                var earlierMonths = approvedMonths.Where(m => m < draftMonth).ToList();
                if (earlierMonths.Count > 0)
                {
                    approvedMonth = earlierMonths.Max();
                    approvedYear = draftYear;
                }
                else
                {
                    // Use previous year's latest approved
                    var prevYearApproved = await _apiService.GetApprovedPayrollMonthsAsync(draftYear - 1);
                    if (prevYearApproved.Count == 0)
                    {
                        return Json(new { 
                            success = false, 
                            reason = "no_approved",
                            message = "No approved payroll found for comparison." 
                        });
                    }
                    approvedMonth = prevYearApproved.Max();
                    approvedYear = draftYear - 1;
                }
            }
            else
            {
                // Try previous year
                var prevYearApproved = await _apiService.GetApprovedPayrollMonthsAsync(draftYear - 1);
                if (prevYearApproved.Count == 0)
                {
                    return Json(new { 
                        success = false, 
                        reason = "no_approved",
                        message = "No approved payroll found for comparison." 
                    });
                }
                approvedMonth = prevYearApproved.Max();
                approvedYear = draftYear - 1;
            }

            // Run quick analysis using KeyDifferences (lighter than full anomaly detection)
            var result = await _analysisService.AnalyzeKeyDifferencesAsync(approvedYear, approvedMonth, draftYear, draftMonth);

            // Format period names
            var draftPeriod = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(draftMonth)} {draftYear}";
            var approvedPeriod = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(approvedMonth)} {approvedYear}";

            // Build concise one-sentence summary (avoid technical terms)
            var summary = BuildConciseSummary(result);

            // Extract up to 3 key change themes (simple strings)
            var keyChanges = new List<string>();
            foreach (var group in result.change_groups?.Take(3) ?? Enumerable.Empty<PayrollIntelligence.Core.ChangeGroup>())
            {
                var count = group.affected_employees?.Count ?? 0;
                var theme = group.group_title switch
                {
                    var t when t?.Contains("Leave") == true => $"{count} employee(s) have leave-related pay adjustments",
                    var t when t?.Contains("New Employees") == true => $"{count} new team member(s) added this period",
                    var t when t?.Contains("Removed") == true => $"{count} employee(s) no longer on payroll",
                    var t when t?.Contains("Tax") == true => $"{count} employee(s) with updated deductions",
                    var t when t?.Contains("Gross") == true => $"{count} employee(s) with pay variations",
                    _ => $"{count} employee(s) affected by {group.group_title?.ToLower() ?? "changes"}"
                };
                keyChanges.Add(theme);
            }

            // Build suggested review actions (simple strings, no automatic corrections)
            var suggestedActions = new List<string>();

            if (result.attention_items?.Count > 0)
            {
                suggestedActions.Add($"Verify {result.attention_items.Count} flagged item(s) in Risk & Review");
            }

            var leaveGroup = result.change_groups?.FirstOrDefault(g => g.group_title?.Contains("Leave") == true);
            if (leaveGroup?.affected_employees?.Count > 0)
            {
                suggestedActions.Add("Confirm leave records match HR documentation");
            }

            var newHiresGroup = result.change_groups?.FirstOrDefault(g => g.group_title?.Contains("New Employees") == true);
            if (newHiresGroup?.affected_employees?.Count > 0 && suggestedActions.Count < 3)
            {
                suggestedActions.Add("Check new hire details against onboarding records");
            }

            var removedGroup = result.change_groups?.FirstOrDefault(g => g.group_title?.Contains("Removed") == true);
            if (removedGroup?.affected_employees?.Count > 0 && suggestedActions.Count < 3)
            {
                suggestedActions.Add("Ensure final payments were processed correctly");
            }

            var taxGroup = result.change_groups?.FirstOrDefault(g => g.group_title?.Contains("Tax") == true);
            if (taxGroup?.affected_employees?.Count > 0 && suggestedActions.Count < 3)
            {
                suggestedActions.Add("Review deduction changes for accuracy");
            }

            // Limit to 3 actions
            suggestedActions = suggestedActions.Take(3).ToList();

            // Build response matching the simple JSON structure
            return Json(new {
                success = true,
                draftPeriod,
                approvedPeriod,
                summary,
                key_changes = keyChanges,
                suggested_actions = suggestedActions,
                confidence_level = result.confidence_level ?? "medium",
                // Additional context for UI (not repeated in detail sections)
                attentionCount = result.attention_items?.Count ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quick preview");
            return Json(new { 
                success = false, 
                reason = "error",
                message = $"Unable to generate preview: {ex.Message}" 
            });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Builds a concise one-sentence summary of payroll movement.
    /// Avoids technical terms and keeps it simple for the dashboard preview.
    /// </summary>
    private static string BuildConciseSummary(PayrollIntelligence.Core.KeyDifferencesResult result)
    {
        var overview = result.payroll_overview;
        if (overview == null)
        {
            return "Payroll analysis complete - see details below.";
        }

        // Determine overall direction from employer cost trend
        var costTrend = overview.employer_cost_trend?.ToLower() ?? "";
        var headcount = overview.headcount_change?.ToLower() ?? "";

        var direction = costTrend switch
        {
            var t when t.Contains("increased notably") => "increased notably",
            var t when t.Contains("increased") => "increased slightly",
            var t when t.Contains("decreased notably") => "decreased notably",
            var t when t.Contains("decreased") => "decreased slightly",
            _ => "remained stable"
        };

        var headcountNote = headcount switch
        {
            var h when h.Contains("increased") => " with new additions to the team",
            var h when h.Contains("decreased") => " with fewer employees",
            _ => ""
        };

        var changeCount = result.change_groups?.Count ?? 0;
        var attentionCount = result.attention_items?.Count ?? 0;

        if (attentionCount > 0)
        {
            return $"Overall payroll {direction}{headcountNote}, with {attentionCount} item(s) requiring attention.";
        }
        else if (changeCount > 0)
        {
            return $"Overall payroll {direction}{headcountNote}, with {changeCount} category change(s) detected.";
        }
        else
        {
            return $"Overall payroll {direction}{headcountNote} compared to last approved period.";
        }
    }
}
