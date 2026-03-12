using FCG.Payments.Application;
using FCG.Payments.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var sqlConnection = context.Configuration["SQL_CONNECTION"]
            ?? "Server=localhost,1433;Database=PaymentsDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
        var serviceBusConnection = context.Configuration["SERVICEBUS_CONNECTION"];
        var appInsightsConnection = context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        services.AddApplication();
        services.AddInfrastructure(
            sqlConnection,
            serviceBusConnection,
            appInsightsConnection,
            "FCG.Payments.Functions");
    })
    .Build();

host.Run();
