using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FCG.Payments.Infrastructure.Telemetry;

public static class TelemetryConfiguration
{
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        string serviceName,
        string? applicationInsightsConnectionString = null)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddSqlClientInstrumentation();

                if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
                {
                    builder.AddAzureMonitorTraceExporter(opts =>
                        opts.ConnectionString = applicationInsightsConnectionString);
                }
            });

        return services;
    }
}
