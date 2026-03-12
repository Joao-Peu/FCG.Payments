using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using MediatR;

namespace FCG.Payments.Application.Queries.QueryPaymentTransactions;

public record QueryPaymentTransactionsQuery(
    string? UserId = null,
    DateTime? From = null,
    DateTime? To = null,
    PaymentStatus? Status = null) : IRequest<IEnumerable<PaymentTransaction>>;
