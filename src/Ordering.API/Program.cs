using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;


var builder = WebApplication.CreateBuilder(args);

// Get Jaeger host from environment variables (for Docker compatibility)
string jaegerHost = builder.Configuration["JAEGER_HOST"] ?? "localhost";
int jaegerPort = int.TryParse(builder.Configuration["JAEGER_PORT"], out var port) ? port : 6831;

// Configuração do OpenTelemetry
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

// Criando e registrando o Meter no DI container
var meter = new Meter("Ordering.API");
builder.Services.AddSingleton(meter);
builder.Services.AddSingleton(meter.CreateCounter<long>("order_placed_count", description: "Número total de orders."));

// Add OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
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
    })
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OrderAPI"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter() // Debugging: View traces in the console
            .AddJaegerExporter(options =>
            {
                options.AgentHost = jaegerHost;
                options.AgentPort = jaegerPort;
            });
    });

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");

orders.MapOrdersApiV1()
      .RequireAuthorization();

app.UseDefaultOpenApi();
app.Run();
