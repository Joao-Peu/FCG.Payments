using MediatR;

namespace FCG.Payments.Application.Commands.ProcessPayment;

public record ProcessPaymentCommand(
    Guid TransactionId,
    decimal Amount,
    string CorrelationId) : IRequest;
