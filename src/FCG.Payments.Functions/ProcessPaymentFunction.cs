using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FCG.Payments.Application.Messaging;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Events;
using FCG.Payments.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Functions;

public class ProcessPaymentFunction
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<ProcessPaymentFunction> _logger;

    public ProcessPaymentFunction(
        IPaymentTransactionRepository repository,
        IMessagePublisher publisher,
        ILogger<ProcessPaymentFunction> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    [Function("ProcessPaymentFunction")]
    public async Task Run(
        [ServiceBusTrigger("order-placed", Connection = "SERVICEBUS_CONNECTION")] ServiceBusReceivedMessage sbMessage)
    {
        var correlationId = sbMessage.CorrelationId ?? Guid.NewGuid().ToString();
        var message = sbMessage.Body.ToString();

        using var activity = new Activity("process-payment");
        activity.SetParentId(correlationId);
        activity.Start();

        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        _logger.LogInformation("ProcessPaymentFunction triggered with CorrelationId {CorrelationId}: {msg}",
            correlationId, message);

        OrderPlacedEvent? orderEvent;
        try
        {
            orderEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse message as OrderPlacedEvent");
            return;
        }

        if (orderEvent is null)
        {
            _logger.LogWarning("Failed to deserialize OrderPlacedEvent from message");
            return;
        }

        var pending = await _repository.ExistsPendingAsync(orderEvent.UserId, orderEvent.GameId);
        if (pending)
        {
            _logger.LogWarning("Payment for user {UserId} and game {GameId} already pending",
                orderEvent.UserId, orderEvent.GameId);
            return;
        }

        var cents = (int)((orderEvent.Price - Math.Floor(orderEvent.Price)) * 100);
        var status = (cents % 2 == 0) ? PaymentStatus.Approved : PaymentStatus.Rejected;

        _logger.LogInformation("Payment for order {OrderId}: {Status}", orderEvent.OrderId, status);

        var transaction = PaymentTransaction.Create(
            orderEvent.OrderId,
            orderEvent.UserId,
            orderEvent.GameId,
            orderEvent.Price,
            status);

        await _repository.AddAsync(transaction);

        var processedEvent = new PaymentProcessedEvent(
            transaction.PurchaseId,
            transaction.UserId,
            transaction.GameId,
            transaction.Amount,
            status.ToString());

        var envelope = new MessageEnvelope(
            "PaymentProcessed",
            JsonSerializer.Serialize(processedEvent),
            correlationId,
            Activity.Current?.Id ?? "");

        await _publisher.PublishAsync("payments-processed", envelope);
        await _publisher.PublishAsync("notifications-payment-processed", envelope);

        _logger.LogInformation("Payment {TransactionId} for order {OrderId} -> {Status} CorrelationId {CorrelationId}",
            transaction.Id, orderEvent.OrderId, status, correlationId);
    }
}
