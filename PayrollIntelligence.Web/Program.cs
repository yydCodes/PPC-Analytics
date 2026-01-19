using Microsoft.Extensions.Options;
using PayrollIntelligence.Core;
using PayrollIntelligence.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure API settings
builder.Services.Configure<ApiConfiguration>(
    builder.Configuration.GetSection("PayrollApi"));

// Register HTTP client and API service
builder.Services.AddHttpClient<PayrollApiService>();

// Add AI configuration
builder.Services.Configure<AiConfiguration>(
    builder.Configuration.GetSection("AiService"));

// Register AI service (optional - will be null if not configured)
builder.Services.AddHttpClient<AiReasoningService>();
builder.Services.AddScoped<AiReasoningService>();

// Update service registrations to inject AI service
builder.Services.AddScoped<PayrollComparisonService>(sp =>
{
    var apiService = sp.GetRequiredService<PayrollApiService>();
    var apiConfig = sp.GetRequiredService<IOptions<ApiConfiguration>>();
    var aiService = sp.GetService<AiReasoningService>();
    return new PayrollComparisonService(apiService, apiConfig, aiService);
});

// Register individual feature services with AI support
builder.Services.AddScoped<PayrollDifferencesService>(sp =>
{
    var apiService = sp.GetRequiredService<PayrollApiService>();
    var apiConfig = sp.GetRequiredService<IOptions<ApiConfiguration>>();
    var aiService = sp.GetService<AiReasoningService>();
    return new PayrollDifferencesService(apiService, apiConfig, aiService);
});

// Register individual feature services with AI support
builder.Services.AddScoped<PayrollAnomalyService>(sp =>
{
    var apiService = sp.GetRequiredService<PayrollApiService>();
    var apiConfig = sp.GetRequiredService<IOptions<ApiConfiguration>>();
    var aiService = sp.GetService<AiReasoningService>();
    return new PayrollAnomalyService(apiService, apiConfig, aiService);
});

// Register the facade service (uses the individual services)
builder.Services.AddScoped<PayrollAnalysisService>(sp =>
{
    var apiService = sp.GetRequiredService<PayrollApiService>();
    var apiConfig = sp.GetRequiredService<IOptions<ApiConfiguration>>();
    var comparisonService = sp.GetRequiredService<PayrollComparisonService>();
    var differencesService = sp.GetRequiredService<PayrollDifferencesService>();
    var anomalyService = sp.GetRequiredService<PayrollAnomalyService>();
    return new PayrollAnalysisService(apiService, apiConfig, comparisonService, differencesService, anomalyService);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
