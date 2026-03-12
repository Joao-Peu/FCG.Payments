using System.Text.Json;
using FCG.Payments.Application.Messaging;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Events;
using FCG.Payments.Domain.Interfaces;
using MediatR;

namespace FCG.Payments.Application.Commands.ProcessPayment;

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IMessagePublisher _publisher;

    public ProcessPaymentHandler(
        IPaymentTransactionRepository repository,
        IMessagePublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null) return;

        // Deterministic rule: even cents -> Paid, odd cents -> Failed
        var cents = (int)((request.Amount - Math.Floor(request.Amount)) * 100);
        var newStatus = (cents % 2 == 0) ? PaymentStatus.Paid : PaymentStatus.Failed;

        transaction.UpdateStatus(newStatus, request.CorrelationId);
        await _repository.UpdateAsync(transaction, cancellationToken);

        // Publish PaymentProcessedEvent to payments-processed queue
        var processedEvent = new PaymentProcessedEvent(
            transaction.PurchaseId,
            transaction.UserId,
            transaction.GameId ?? string.Empty,
            transaction.Amount,
            newStatus.ToString());

        var processedEnvelope = new MessageEnvelope(
            "PaymentProcessed",
            JsonSerializer.Serialize(processedEvent),
            transaction.CorrelationId ?? string.Empty,
            "");

        await _publisher.PublishAsync("payments-processed", processedEnvelope, cancellationToken);

        // Publish notification
        var notifyEnvelope = new MessageEnvelope(
            "NotificationRequested",
            JsonSerializer.Serialize(new
            {
                transaction.UserId,
                transaction.PurchaseId,
                Status = newStatus.ToString(),
                transaction.CorrelationId
            }),
            transaction.CorrelationId ?? string.Empty,
            "");

        await _publisher.PublishAsync("notifications", notifyEnvelope, cancellationToken);
    }
}
