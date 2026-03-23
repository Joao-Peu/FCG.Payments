using FCG.Payments.Application;
using FCG.Payments.Infrastructure;
using FCG.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .UseSerilog((context, loggerConfig) => loggerConfig
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ServiceName", "FCG.Payments.Functions")
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.Conditional(
                _ => !string.IsNullOrEmpty(context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]),
                wt => wt.ApplicationInsights(
                    context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"],
                    new TraceTelemetryConverter())))
        .ConfigureServices((context, services) =>
        {
            var sqlConnection = context.Configuration["SQL_CONNECTION"]
                ?? "Server=localhost,1433;Database=PaymentsDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
            var serviceBusConnection = context.Configuration["SERVICEBUS_CONNECTION"];

            services.AddApplicationInsightsTelemetryWorkerService();

            services.AddApplication();
            services.AddInfrastructure(sqlConnection, serviceBusConnection);
        })
        .Build();

    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        db.Database.EnsureCreated();
        Log.Information("Database ensured created successfully");
    }

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal("Application terminated unexpectedly: {Message}", ex.Message);
}
finally
{
    Log.CloseAndFlush();
}
