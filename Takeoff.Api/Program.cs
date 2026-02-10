using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Takeoff.Api.Options;
using Takeoff.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PeerServicesOptions>(builder.Configuration.GetSection("PeerServices"));

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton<TakeoffDataStore>();
builder.Services.AddHttpClient<EstimatorClient>(client =>
{
    var options = builder.Configuration.GetSection("PeerServices").Get<PeerServicesOptions>();
    if (!string.IsNullOrEmpty(options?.EstimatorBaseUrl)) client.BaseAddress = new Uri(options.EstimatorBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options?.HttpTimeoutSeconds ?? 10);
})
.AddTypedClient((httpClient, sp) => new EstimatorClient(httpClient, sp.GetRequiredService<ILogger<EstimatorClient>>()));

var app = builder.Build();
// Serve default files (index.html) and static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
// Ensure SPA fallback to index.html so IIS Express virtual paths serve the UI
app.MapFallbackToFile("index.html");
app.Run();