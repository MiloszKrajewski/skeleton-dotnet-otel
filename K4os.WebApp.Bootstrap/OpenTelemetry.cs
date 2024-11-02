using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace K4os.WebApp.Bootstrap;

public static class OpenTelemetry
{
    private const string ConfigSectionName = "OpenTelemetry";

    private const string SerilogTemplate =
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}";

    private static readonly string ApplicationName =
        (Assembly.GetEntryAssembly()?.GetName().Name).ThrowIfNull();

    public static IHostApplicationBuilder ConfigureLogging(
        this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;
        services.AddSerilog(logging => ConfigureLogging(logging, config));
        return builder;
    }

    private static OpenTelemetryConfig GetOpenTelemetryConfig(this IConfiguration config) =>
        config.GetSection(ConfigSectionName).Get<OpenTelemetryConfig>() ?? new OpenTelemetryConfig();

    private static void ConfigureLogging(LoggerConfiguration logging, IConfiguration config)
    {
        var openTelemetry = config.GetOpenTelemetryConfig();

        logging
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: SerilogTemplate)
            .WriteTo.OpenTelemetry(c => { ConfigureTelemetrySink(c, openTelemetry); });
    }

    private static void ConfigureTelemetrySink(
        BatchedOpenTelemetrySinkOptions options, OpenTelemetryConfig configuration)
    {
        options.Endpoint = configuration.TelemetryEndpoint.ToString();
        options.ResourceAttributes = new Dictionary<string, object> {
            { "service.name", ApplicationName }
        };
        options.IncludedData = 
            IncludedData.TraceIdField | 
            IncludedData.SpanIdField | 
            IncludedData.MessageTemplateMD5HashAttribute;
    }

    public static void ConfigureMetrics(
        this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;
        var openTelemetry = config.GetOpenTelemetryConfig();

        services.AddMetrics();
        services
            .AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ApplicationName))
            .WithLogging()
            .WithMetrics(
                x => x
                    .AddOtlpExporter(z => z.Endpoint = openTelemetry.TelemetryEndpoint)
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(openTelemetry.Meters));
    }

    public static void ConfigureTracing(
        this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;
        var openTelemetry = config.GetOpenTelemetryConfig();

        services
            .AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ApplicationName))
            .WithLogging()
            .WithTracing(
                x => x
                    .SetSampler<AlwaysOnSampler>()
                    .AddOtlpExporter(z => z.Endpoint = openTelemetry.TelemetryEndpoint)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(openTelemetry.Traces));
    }

    public static IHealthChecksBuilder ConfigureHealthChecks(
        this IHostApplicationBuilder builder) =>
        builder.Services.AddHealthChecks().AddCheck("default", () => HealthCheckResult.Healthy());
}
