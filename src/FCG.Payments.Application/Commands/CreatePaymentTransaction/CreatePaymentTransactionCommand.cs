using MediatR;

namespace FCG.Payments.Application.Commands.CreatePaymentTransaction;

public record CreatePaymentTransactionCommand(
    string PurchaseId,
    string UserId,
    string? GameId,
    decimal Amount,
    string CorrelationId) : IRequest<CreatePaymentTransactionResult>;

public record CreatePaymentTransactionResult(
    Guid Id,
    string PurchaseId,
    string UserId,
    string? GameId,
    decimal Amount,
    string Status,
    DateTime CreatedAtUtc);
