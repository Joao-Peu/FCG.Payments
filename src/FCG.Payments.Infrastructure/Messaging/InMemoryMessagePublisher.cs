using System.Collections.Concurrent;
using FCG.Payments.Application.Messaging;

namespace FCG.Payments.Infrastructure.Messaging;

public class InMemoryMessagePublisher : IMessagePublisher
{
    private readonly ConcurrentDictionary<string, List<Func<MessageEnvelope, Task>>> _subs = new();

    public Task PublishAsync(string topicOrQueue, MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (_subs.TryGetValue(topicOrQueue, out var handlers))
        {
            foreach (var h in handlers)
            {
                _ = Task.Run(() => h(envelope), cancellationToken);
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
