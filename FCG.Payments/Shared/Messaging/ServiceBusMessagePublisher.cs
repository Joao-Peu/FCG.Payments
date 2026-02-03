namespace FCG.Payments.Shared.Messaging;

public class ServiceBusMessagePublisher : IMessagePublisher, IMessageSubscriber
{
    // Placeholder - real implementation would use Azure.Messaging.ServiceBus
    public Task PublishAsync(string topicOrQueue, MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        // TODO: real Service Bus code
        Console.WriteLine($"[ServiceBus] Publish to {topicOrQueue}: {envelope.MessageType} - Corr:{envelope.CorrelationId}");
        return Task.CompletedTask;
    }

    public void Subscribe(string topicOrQueue, Func<MessageEnvelope, Task> handler)
    {
        // Not implemented in placeholder
        throw new NotImplementedException();
    }
}