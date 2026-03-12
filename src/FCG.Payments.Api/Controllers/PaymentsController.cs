using System.Security.Claims;
using FCG.Payments.Application.Commands.CreatePaymentTransaction;
using FCG.Payments.Application.Queries.GetPaymentByPurchaseId;
using FCG.Payments.Application.Queries.QueryPaymentTransactions;
using FCG.Payments.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Payments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var correlationId = Guid.NewGuid().ToString();

        var command = new CreatePaymentTransactionCommand(
            request.PurchaseId,
            userId,
            request.GameId,
            request.Amount,
            correlationId);

        var result = await _mediator.Send(command);

        return CreatedAtAction(nameof(GetStatus), new { purchaseId = result.PurchaseId }, result);
    }

    [HttpGet("{purchaseId}")]
    [Authorize]
    public async Task<IActionResult> GetStatus(string purchaseId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var query = new GetPaymentByPurchaseIdQuery(purchaseId, userId);
        var tx = await _mediator.Send(query);

        if (tx == null) return NotFound();
        if (tx.UserId != userId && userId != "admin") return Forbid();

        return Ok(new
        {
            tx.PurchaseId,
            Status = tx.Status.ToString(),
            tx.Amount,
            tx.GameId,
            tx.CreatedAtUtc,
            tx.UpdatedAtUtc
        });
    }

    [HttpGet("transactions")]
    [Authorize]
    public async Task<IActionResult> Query(
        [FromQuery] string? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] PaymentStatus? status)
    {
        var requester = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requester != "admin") userId = requester;

        var query = new QueryPaymentTransactionsQuery(userId, from, to, status);
        var res = await _mediator.Send(query);

        return Ok(res.Select(x => new
        {
            x.PurchaseId,
            Status = x.Status.ToString(),
            x.Amount,
            x.UserId,
            x.GameId,
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        }));
    }
}

public record CreatePaymentRequest
{
    public string PurchaseId { get; init; } = null!;
    public string? GameId { get; init; }
    public decimal Amount { get; init; }
}
