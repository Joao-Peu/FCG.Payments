using System.Text.Json;
using FCG.Payments.Application.Commands.ProcessPayment;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Functions;

public class ProcessPaymentFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessPaymentFunction> _logger;

    public ProcessPaymentFunction(IMediator mediator, ILogger<ProcessPaymentFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function("ProcessPaymentFunction")]
    public async Task Run(
        [ServiceBusTrigger("payments-start", Connection = "SERVICEBUS_CONNECTION")] string message)
    {
        _logger.LogInformation("ProcessPaymentFunction triggered: {msg}", message);

        var doc = JsonDocument.Parse(message);
        var id = doc.RootElement.GetProperty("Id").GetGuid();
        var amount = doc.RootElement.GetProperty("Amount").GetDecimal();
        var correlationId = doc.RootElement.TryGetProperty("CorrelationId", out var c)
            ? c.GetString() ?? string.Empty
            : string.Empty;

        var command = new ProcessPaymentCommand(id, amount, correlationId);
        await _mediator.Send(command);

        _logger.LogInformation("Processed payment {id}", id);
    }
}
