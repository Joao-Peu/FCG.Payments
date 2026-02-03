using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Functions;

public class SendNotificationFunction
{
    private readonly ILogger<SendNotificationFunction> _logger;
    public SendNotificationFunction(ILogger<SendNotificationFunction> logger)
    {
        _logger = logger;
    }

    [Function("SendNotificationFunction")]
    public Task Run([ServiceBusTrigger("notifications", Connection = "SERVICEBUS_CONNECTION")] string message)
    {
        _logger.LogInformation("SendNotificationFunction triggered: {msg}", message);
        // simulate sending
        return Task.CompletedTask;
    }
}
