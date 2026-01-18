using Microsoft.Extensions.Options;
using PayrollIntelligence.Core.Services;

namespace PayrollIntelligence.Core;

/// <summary>
/// Facade service that provides access to all payroll analysis features.
/// Delegates to specialized services for each feature.
/// </summary>
public class PayrollAnalysisService
{
    private readonly PayrollComparisonService? _comparisonService;
    private readonly PayrollDifferencesService? _differencesService;
    private readonly PayrollAnomalyService? _anomalyService;
    private readonly PayrollApiService? _apiService;
    private readonly ApiConfiguration? _apiConfig;

    /// <summary>
    /// Default constructor for backward compatibility with static methods.
    /// </summary>
    public PayrollAnalysisService()
    {
    }

    /// <summary>
    /// Constructor with all required dependencies.
    /// </summary>
    public PayrollAnalysisService(
        PayrollApiService apiService,
        IOptions<ApiConfiguration> apiConfig,
        PayrollComparisonService comparisonService,
        PayrollDifferencesService differencesService,
        PayrollAnomalyService anomalyService)
    {
        _apiService = apiService;
        _apiConfig = apiConfig.Value;
        _comparisonService = comparisonService;
        _differencesService = differencesService;
        _anomalyService = anomalyService;
    }

    #region Payroll Summary

    /// <summary>
    /// Analyzes payroll comparison between two periods using API data.
    /// </summary>
    public async Task<PayrollComparisonResult> AnalyzePayrollComparisonAsync(
        int previousYear, int previousMonth,
        int currentYear, int currentMonth)
    {
        if (_comparisonService == null)
        {
            throw new InvalidOperationException("Comparison service not configured.");
        }

        return await _comparisonService.ComparePeriodsAsync(
            previousYear, previousMonth,
            currentYear, currentMonth);
    }

    /// <summary>
    /// Static method for comparing two payroll periods.
    /// </summary>
    public static PayrollComparisonResult AnalyzePayrollComparison(
        PayrollData previous, 
        PayrollData current,
        string previousPeriodName = "Previous Period",
        string currentPeriodName = "Current Period")
    {
        return PayrollComparisonService.Compare(previous, current, previousPeriodName, currentPeriodName);
    }

    #endregion

    #region Detailed Changes (Key Differences)

    /// <summary>
    /// Analyzes key differences between two periods using API data.
    /// </summary>
    public async Task<KeyDifferencesResult> AnalyzeKeyDifferencesAsync(
        int previousYear, int previousMonth,
        int currentYear, int currentMonth)
    {
        if (_differencesService == null)
        {
            throw new InvalidOperationException("Differences service not configured.");
        }

        return await _differencesService.AnalyzeKeyDifferencesAsync(
            previousYear, previousMonth,
            currentYear, currentMonth);
    }

    /// <summary>
    /// Static method for analyzing key differences between two payroll periods.
    /// </summary>
    public static KeyDifferencesResult AnalyzeKeyDifferences(PayrollData previous, PayrollData current)
    {
        return PayrollDifferencesService.AnalyzeKeyDifferences(previous, current);
    }

    #endregion

    #region Risk & Review

    /// <summary>
    /// Detects anomalies by comparing a draft payroll against an approved payroll.
    /// </summary>
    public async Task<AnomalyDetectionResult> DetectAnomaliesAsync(
        int draftYear, int draftMonth,
        int comparisonYear, int comparisonMonth)
    {
        if (_anomalyService == null)
        {
            throw new InvalidOperationException("Anomaly service not configured.");
        }

        return await _anomalyService.DetectAnomaliesAsync(
            draftYear, draftMonth,
            comparisonYear, comparisonMonth);
    }

    /// <summary>
    /// Static method for detecting anomalies between two payroll periods.
    /// </summary>
    public static AnomalyDetectionResult DetectAnomalies(PayrollData historical, PayrollData current)
    {
        return PayrollAnomalyService.DetectAnomalies(historical, current);
    }

    /// <summary>
    /// Static method for detecting anomalies with multiple historical periods.
    /// </summary>
    public static AnomalyDetectionResult DetectAnomalies(List<PayrollData> historical, PayrollData current)
    {
        if (historical.Count == 0)
        {
            return new AnomalyDetectionResult 
            { 
                Anomalies = new List<Anomaly>(),
                Summary = "No historical data available for comparison."
            };
        }

        // Use the most recent historical period for comparison
        var previous = historical.Last();
        return PayrollAnomalyService.DetectAnomalies(previous, current);
    }

    #endregion

    #region Private Methods

    private async Task EnsureAuthenticatedAsync()
    {
        if (_apiService == null)
        {
            throw new InvalidOperationException("API service not configured.");
        }

        if (_apiConfig == null || !_apiConfig.IsValid())
        {
            throw new InvalidOperationException("API configuration is incomplete.");
        }

        if (!_apiService.IsAuthenticated())
        {
            var success = await _apiService.AuthenticateAsync(_apiConfig.ClientId, _apiConfig.ClientSecret);
            if (!success)
            {
                throw new InvalidOperationException("Failed to authenticate with payroll API.");
            }
        }
    }

    #endregion
}
