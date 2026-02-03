using System.Security.Claims;
using FCG.Payments.Shared;
using FCG.Payments.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Payments.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService service, ILogger<PaymentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("{purchaseId}")]
    [Authorize]
    public async Task<IActionResult> GetStatus(string purchaseId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tx = await _service.GetByPurchaseIdAsync(purchaseId, userId);
        if (tx == null) return NotFound();
        if (tx.UserId != userId && userId != "admin") return Forbid();
        return Ok(new { tx.PurchaseId, tx.Status, tx.Amount, tx.CreatedAtUtc, tx.UpdatedAtUtc });
    }

    [HttpGet("transactions")]
    [Authorize]
    public async Task<IActionResult> Query([FromQuery] string? userId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] PaymentStatus? status)
    {
        var requester = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // basic rule: if not admin, force userId = requester
        if (requester != "admin") userId = requester;
        var res = await _service.QueryTransactionsAsync(userId, from, to, status);
        return Ok(res.Select(x => new { x.PurchaseId, x.Status, x.Amount, x.UserId, x.CreatedAtUtc, x.UpdatedAtUtc }));
    }
}