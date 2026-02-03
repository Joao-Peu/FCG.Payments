using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FCG.Payments.Shared;
using FCG.Payments.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var conn = context.Configuration.GetValue<string>("SQL_CONNECTION") ?? "Server=localhost,1433;Database=PaymentsDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
        services.AddDbContext<PaymentsDbContext>(opts => opts.UseSqlServer(conn));
        services.AddScoped<IPaymentService, PaymentService>();
        var useInMemoryBus = context.Configuration.GetValue<bool?>("USE_INMEMORY_BUS") ?? true;
        if (useInMemoryBus)
        {
            services.AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();
            services.AddSingleton<IMessageSubscriber, InMemoryMessagePublisher>();
        }
        else
        {
            services.AddSingleton<IMessagePublisher, ServiceBusMessagePublisher>();
            services.AddSingleton<IMessageSubscriber, ServiceBusMessagePublisher>();
        }

        // OpenTelemetry (console)
        Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FCG.Payments.Functions"))
            .AddConsoleExporter()
            .Build();
    })
    .Build();

host.Run();
