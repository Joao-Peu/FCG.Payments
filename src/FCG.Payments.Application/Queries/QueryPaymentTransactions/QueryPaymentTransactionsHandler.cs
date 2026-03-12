using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Interfaces;
using MediatR;

namespace FCG.Payments.Application.Queries.QueryPaymentTransactions;

public class QueryPaymentTransactionsHandler
    : IRequestHandler<QueryPaymentTransactionsQuery, IEnumerable<PaymentTransaction>>
{
    private readonly IPaymentTransactionRepository _repository;

    public QueryPaymentTransactionsHandler(IPaymentTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaymentTransaction>> Handle(
        QueryPaymentTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.QueryAsync(
            request.UserId,
            request.From,
            request.To,
            request.Status,
            cancellationToken);
    }
}
