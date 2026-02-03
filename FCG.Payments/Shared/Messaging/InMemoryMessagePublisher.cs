using System.Collections.Concurrent;

namespace FCG.Payments.Shared.Messaging;

public class InMemoryMessagePublisher : IMessagePublisher, IMessageSubscriber
{
    private readonly ConcurrentDictionary<string, List<Func<MessageEnvelope, Task>>> _subs = new();

    public Task PublishAsync(string topicOrQueue, MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (_subs.TryGetValue(topicOrQueue, out var handlers))
        {
            foreach (var h in handlers)
            {
                _ = Task.Run(() => h(envelope));
            }
        }
        return Task.CompletedTask;
    }

    public void Subscribe(string topicOrQueue, Func<MessageEnvelope, Task> handler)
    {
        var list = _subs.GetOrAdd(topicOrQueue, _ => new List<Func<MessageEnvelope, Task>>());
        list.Add(handler);
    }
}