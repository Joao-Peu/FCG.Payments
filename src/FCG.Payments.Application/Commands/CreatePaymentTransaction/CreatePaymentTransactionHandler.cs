using System.Text.Json;
using FCG.Payments.Application.Messaging;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Interfaces;
using MediatR;

namespace FCG.Payments.Application.Commands.CreatePaymentTransaction;

public class CreatePaymentTransactionHandler
    : IRequestHandler<CreatePaymentTransactionCommand, CreatePaymentTransactionResult>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IMessagePublisher _publisher;

    public CreatePaymentTransactionHandler(
        IPaymentTransactionRepository repository,
        IMessagePublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<CreatePaymentTransactionResult> Handle(
        CreatePaymentTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = PaymentTransaction.Create(
            request.PurchaseId,
            request.UserId,
            request.Amount,
            request.CorrelationId,
            request.GameId);

        await _repository.AddAsync(transaction, cancellationToken);

        var envelope = new MessageEnvelope(
            "StartPaymentProcessing",
            JsonSerializer.Serialize(new
            {
                transaction.Id,
                transaction.PurchaseId,
                transaction.UserId,
                transaction.GameId,
                transaction.Amount
            }),
            request.CorrelationId,
            "");

        await _publisher.PublishAsync("payments-start", envelope, cancellationToken);

        return new CreatePaymentTransactionResult(
            transaction.Id,
            transaction.PurchaseId,
            transaction.UserId,
            transaction.GameId,
            transaction.Amount,
            transaction.Status.ToString(),
            transaction.CreatedAtUtc);
    }
}
