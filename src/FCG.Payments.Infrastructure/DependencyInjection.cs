using Azure.Messaging.ServiceBus;
using FCG.Payments.Application.Messaging;
using FCG.Payments.Domain.Interfaces;
using FCG.Payments.Infrastructure.Messaging;
using FCG.Payments.Infrastructure.Persistence;
using FCG.Payments.Infrastructure.Persistence.Repositories;
using FCG.Payments.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string sqlConnectionString,
        string? serviceBusConnectionString = null,
        string? applicationInsightsConnectionString = null,
        string serviceName = "FCG.Payments")
    {
        // EF Core with SQL Server
        services.AddDbContext<PaymentsDbContext>(opts =>
            opts.UseSqlServer(sqlConnectionString, sqlOpts =>
                sqlOpts.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)));

        // Repository
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();

        // Messaging
        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));
            services.AddSingleton<IMessagePublisher, ServiceBusMessagePublisher>();
        }
        else
        {
            services.AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();
        }

        // Telemetry
        services.AddTelemetry(serviceName, applicationInsightsConnectionString);

        return services;
    }
}
