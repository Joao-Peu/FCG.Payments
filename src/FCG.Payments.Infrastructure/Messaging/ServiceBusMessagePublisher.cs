using System.Collections.Concurrent;
using System.Text;
using Azure.Messaging.ServiceBus;
using FCG.Payments.Application.Messaging;

namespace FCG.Payments.Infrastructure.Messaging;

public class ServiceBusMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public ServiceBusMessagePublisher(ServiceBusClient client)
    {
        _client = client;
    }

    public async Task PublishAsync(string topicOrQueue, MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var sender = _senders.GetOrAdd(topicOrQueue, queue => _client.CreateSender(queue));

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(envelope.Body))
        {
            ContentType = "application/json",
            CorrelationId = envelope.CorrelationId,
            Subject = envelope.MessageType
        };

        message.ApplicationProperties["MessageType"] = envelope.MessageType;
        message.ApplicationProperties["TraceParent"] = envelope.TraceParent;

        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                message.ApplicationProperties[header.Key] = header.Value;
            }
        }

        await sender.SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        await _client.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
