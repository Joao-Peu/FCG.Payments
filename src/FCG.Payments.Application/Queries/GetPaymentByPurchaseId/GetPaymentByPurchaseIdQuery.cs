using FCG.Payments.Domain.Entities;
using MediatR;

namespace FCG.Payments.Application.Queries.GetPaymentByPurchaseId;

public record GetPaymentByPurchaseIdQuery(
    string PurchaseId,
    string? RequesterUserId = null) : IRequest<PaymentTransaction?>;
