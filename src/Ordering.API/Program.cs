using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using eShop.Ordering.API.Telemetry; // Import the new metrics class
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;

var builder = WebApplication.CreateBuilder(args);

// Get Jaeger host from environment variables (for Docker compatibility)
string jaegerHost = builder.Configuration["JAEGER_HOST"] ?? "localhost";
int jaegerPort = int.TryParse(builder.Configuration["JAEGER_PORT"], out var port) ? port : 6831;


builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

builder.Services.AddSingleton(TelemetryMetrics.OrderPlacedCounter);
builder.Services.AddSingleton(TelemetryMetrics.TotalRevenueCounter);
builder.Services.AddSingleton(TelemetryMetrics.CanceledOrdersCounter);
builder.Services.AddSingleton(TelemetryMetrics.ActiveOrdersGauge);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OrderAPI"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddSource("eShop.Ordering.CreateOrderCommandHandler") // Register the activity source
            .AddJaegerExporter(options =>
            {
                options.AgentHost = jaegerHost;
                options.AgentPort = jaegerPort;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Ordering.API")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });
    });

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");

orders.MapOrdersApiV1()
      .RequireAuthorization();

app.UseDefaultOpenApi();
app.Run();
