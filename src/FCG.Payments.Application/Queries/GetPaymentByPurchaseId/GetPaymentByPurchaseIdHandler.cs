using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Interfaces;
using MediatR;

namespace FCG.Payments.Application.Queries.GetPaymentByPurchaseId;

public class GetPaymentByPurchaseIdHandler
    : IRequestHandler<GetPaymentByPurchaseIdQuery, PaymentTransaction?>
{
    private readonly IPaymentTransactionRepository _repository;

    public GetPaymentByPurchaseIdHandler(IPaymentTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentTransaction?> Handle(
        GetPaymentByPurchaseIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetByPurchaseIdAsync(request.PurchaseId, cancellationToken);
    }
}
