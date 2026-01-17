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

// Register individual feature services
builder.Services.AddScoped<PayrollComparisonService>();
builder.Services.AddScoped<PayrollDifferencesService>();
builder.Services.AddScoped<PayrollAnomalyService>();

// Register the facade service (uses the individual services)
builder.Services.AddScoped<PayrollAnalysisService>();

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
