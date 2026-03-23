using System.Diagnostics;
using System.Text.Json;
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
        [ServiceBusTrigger("order-placed", Connection = "SERVICEBUS_CONNECTION")] string message,
        FunctionContext context)
    {
        var sw = Stopwatch.StartNew();

        // Extract metadata from binding data
        var bindingData = context.BindingContext.BindingData;
        var messageId = bindingData.TryGetValue("MessageId", out var mid) ? mid?.ToString() ?? "unknown" : "unknown";
        var correlationId = bindingData.TryGetValue("CorrelationId", out var cid) && !string.IsNullOrEmpty(cid?.ToString())
            ? cid.ToString()!
            : Guid.NewGuid().ToString();
        var deliveryCount = bindingData.TryGetValue("DeliveryCount", out var dc) ? dc?.ToString() ?? "0" : "0";

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["MessageId"] = messageId,
            ["DeliveryCount"] = deliveryCount
        });

        _logger.LogInformation(
            "[STEP:RECEIVE] Message received. MessageId={MessageId}, DeliveryCount={DeliveryCount}, CorrelationId={CorrelationId}",
            messageId, deliveryCount, correlationId);

        if (string.IsNullOrEmpty(message))
        {
            _logger.LogError("[STEP:RECEIVE] Empty message body. MessageId={MessageId}", messageId);
            return;
        }

        _logger.LogInformation("[STEP:DESERIALIZE] Raw body: {Body}", message);

        using var activity = new Activity("process-payment");
        activity.SetParentId(correlationId);
        activity.Start();

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
            _logger.LogError("[STEP:DESERIALIZE] Failed to parse OrderPlacedEvent. Error={Error}, Body={Body}",
                ex.Message, message);
            return;
        }

        if (orderEvent is null)
        {
            _logger.LogWarning("[STEP:DESERIALIZE] Deserialized to null. Body={Body}", message);
            return;
        }

        _logger.LogInformation(
            "[STEP:DESERIALIZE] OK. OrderId={OrderId}, UserId={UserId}, GameId={GameId}, Price={Price}",
            orderEvent.OrderId, orderEvent.UserId, orderEvent.GameId, orderEvent.Price);

        var pending = await _repository.ExistsPendingAsync(orderEvent.UserId, orderEvent.GameId);
        if (pending)
        {
            _logger.LogWarning("[STEP:DUPLICATE] Payment already pending. UserId={UserId}, GameId={GameId}",
                orderEvent.UserId, orderEvent.GameId);
            return;
        }

        var cents = (int)((orderEvent.Price - Math.Floor(orderEvent.Price)) * 100);
        var status = (cents % 2 == 0) ? PaymentStatus.Approved : PaymentStatus.Rejected;

        _logger.LogInformation("[STEP:PROCESS] Payment decision. OrderId={OrderId}, Cents={Cents}, Status={Status}",
            orderEvent.OrderId, cents, status);

        var transaction = PaymentTransaction.Create(
            orderEvent.OrderId,
            orderEvent.UserId,
            orderEvent.GameId,
            orderEvent.Price,
            status);

        await _repository.AddAsync(transaction);
        _logger.LogInformation("[STEP:PERSIST] Transaction saved. TransactionId={TransactionId}", transaction.Id);

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
        _logger.LogInformation("[STEP:PUBLISH] Published to payments-processed");

        await _publisher.PublishAsync("notifications-payment-processed", envelope);
        _logger.LogInformation("[STEP:PUBLISH] Published to notifications-payment-processed");

        sw.Stop();
        _logger.LogInformation(
            "[STEP:COMPLETE] Payment processed. TransactionId={TransactionId}, OrderId={OrderId}, Status={Status}, Duration={Duration}ms",
            transaction.Id, orderEvent.OrderId, status, sw.ElapsedMilliseconds);
    }
}
