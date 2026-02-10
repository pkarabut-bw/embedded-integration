using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Estimator.Api.Options;
using Estimator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PeerServicesOptions>(builder.Configuration.GetSection("PeerServices"));

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton<EstimatorDataStore>();
builder.Services.AddHttpClient<TakeoffClient>(client =>
{
    var options = builder.Configuration.GetSection("PeerServices").Get<PeerServicesOptions>();
    if (!string.IsNullOrEmpty(options?.TakeoffBaseUrl)) client.BaseAddress = new Uri(options.TakeoffBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options?.HttpTimeoutSeconds ?? 10);
})
.AddTypedClient((httpClient, sp) => new TakeoffClient(httpClient, sp.GetRequiredService<ILogger<TakeoffClient>>()));

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();