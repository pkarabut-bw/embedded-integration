using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Estimator.StateManagement;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure base URL for Takeoff
var takeoffBase = builder.Configuration["Takeoff:BaseUrl"];

// Register HttpClient factory and configure named client
builder.Services.AddHttpClient("takeoff", client => {
    client.BaseAddress = new Uri(takeoffBase);
});

// Register StateService as singleton using IHttpClientFactory
builder.Services.AddSingleton<StateService>(sp => {
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("takeoff");
    var logger = sp.GetRequiredService<ILogger<StateService>>();
    return new StateService(client, logger);
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Serve default file 'state-viewer.html' from wwwroot as the app root
var defaultFilesOptions = new Microsoft.AspNetCore.Builder.DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("state-viewer.html");
app.UseDefaultFiles(defaultFilesOptions);

// Serve static files (frontend viewer)
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Estimator API V1");
        // keep Swagger at /swagger so it does not override the root default file
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
