using FCG.Payments.Shared.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using FCG.Payments.Shared;
using FCG.Payments.Shared.Models;

namespace FCG.Payments.Functions;

public class ProcessPaymentFunction
{
    private readonly IPaymentService _service;
    private readonly ILogger<ProcessPaymentFunction> _logger;

    public ProcessPaymentFunction(IPaymentService service, ILogger<ProcessPaymentFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("ProcessPaymentFunction")]
    public async Task Run([ServiceBusTrigger("payments-start", Connection = "SERVICEBUS_CONNECTION")] string message)
    {
        _logger.LogInformation("ProcessPaymentFunction triggered: {msg}", message);

        var doc = JsonDocument.Parse(message);
        var id = doc.RootElement.GetProperty("Id").GetGuid();
        var amount = doc.RootElement.GetProperty("Amount").GetDecimal();
        var correlationId = doc.RootElement.TryGetProperty("CorrelationId", out var c) ? c.GetString() ?? string.Empty : string.Empty;

        // deterministic rule: even cents -> Paid, odd cents -> Failed
        var cents = (int)((amount - Math.Floor(amount)) * 100);
        var status = (cents % 2 == 0) ? PaymentStatus.Paid : PaymentStatus.Failed;

        await _service.UpdateStatusAsync(id, status, correlationId);

        _logger.LogInformation("Processed payment {id} -> {status}", id, status);
    }
}
