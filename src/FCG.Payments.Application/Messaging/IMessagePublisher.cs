namespace FCG.Payments.Application.Messaging;

public record MessageEnvelope(
    string MessageType,
    string Body,
    string CorrelationId,
    string TraceParent,
    IDictionary<string, string>? Headers = null);

public interface IMessagePublisher
{
    Task PublishAsync(string topicOrQueue, MessageEnvelope envelope, CancellationToken cancellationToken = default);
}
