using System.Security.Claims;
using FCG.Payments.Api.Controllers;
using FCG.Payments.Application.Commands.CreatePaymentTransaction;
using FCG.Payments.Application.Queries.GetPaymentByPurchaseId;
using FCG.Payments.Application.Queries.QueryPaymentTransactions;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FCG.Payments.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<PaymentsController>> _mockLogger;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<PaymentsController>>();
        _controller = new PaymentsController(_mockMediator.Object, _mockLogger.Object);

        // Set up a default user
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "USER-001") };
        var identity = new ClaimsIdentity(claims, "Test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task CreatePayment_WithValidRequest_ShouldReturnCreatedResult()
    {
        var request = new CreatePaymentRequest
        {
            PurchaseId = "PUR-001",
            GameId = "GAME-001",
            Amount = 100.50m
        };
        var expectedResult = new CreatePaymentTransactionResult(
            Guid.NewGuid(), "PUR-001", "USER-001", "GAME-001", 100.50m, "Created", DateTime.UtcNow);

        _mockMediator.Setup(m => m.Send(It.IsAny<CreatePaymentTransactionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.CreatePayment(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task GetStatus_ExistingPurchaseId_ShouldReturnOk()
    {
        var tx = PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1");
        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentByPurchaseIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var result = await _controller.GetStatus("PUR-001");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetStatus_NonExistent_ShouldReturnNotFound()
    {
        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentByPurchaseIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        var result = await _controller.GetStatus("NONEXISTENT");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetStatus_OtherUserTransaction_ShouldReturnForbid()
    {
        var tx = PaymentTransaction.Create("PUR-001", "OTHER-USER", 100m, "corr-1");
        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentByPurchaseIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var result = await _controller.GetStatus("PUR-001");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Query_ShouldReturnOk()
    {
        var transactions = new List<PaymentTransaction>
        {
            PaymentTransaction.Create("PUR-001", "USER-001", 100m, "corr-1")
        };
        _mockMediator.Setup(m => m.Send(It.IsAny<QueryPaymentTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await _controller.Query(null, null, null, null);

        Assert.IsType<OkObjectResult>(result);
    }
}
