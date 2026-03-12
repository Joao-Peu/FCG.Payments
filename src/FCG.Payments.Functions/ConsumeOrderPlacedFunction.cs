using System.Text.Json;
using FCG.Payments.Application.Commands.CreatePaymentTransaction;
using FCG.Payments.Domain.Events;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Functions;

public class ConsumeOrderPlacedFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<ConsumeOrderPlacedFunction> _logger;

    public ConsumeOrderPlacedFunction(IMediator mediator, ILogger<ConsumeOrderPlacedFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function("ConsumeOrderPlacedFunction")]
    public async Task Run(
        [ServiceBusTrigger("order-placed", Connection = "SERVICEBUS_CONNECTION")] string message)
    {
        _logger.LogInformation("ConsumeOrderPlacedFunction triggered: {msg}", message);

        var orderEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(message, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (orderEvent is null)
        {
            _logger.LogWarning("Failed to deserialize OrderPlacedEvent from message");
            return;
        }

        var command = new CreatePaymentTransactionCommand(
            orderEvent.OrderId,
            orderEvent.UserId,
            orderEvent.GameId,
            orderEvent.Price,
            Guid.NewGuid().ToString());

        var result = await _mediator.Send(command);

        _logger.LogInformation(
            "Created payment transaction {TransactionId} for order {OrderId}",
            result.Id, orderEvent.OrderId);
    }
}
