using System.Text.Json;
using FCG.Payments.Application.Commands.CreatePaymentTransaction;
using FCG.Payments.Domain.Events;
using FCG.Payments.Functions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FCG.Payments.Functions.Tests;

public class ConsumeOrderPlacedFunctionTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<ConsumeOrderPlacedFunction>> _mockLogger;
    private readonly ConsumeOrderPlacedFunction _function;

    public ConsumeOrderPlacedFunctionTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<ConsumeOrderPlacedFunction>>();
        _function = new ConsumeOrderPlacedFunction(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Run_WithValidOrderEvent_ShouldSendCreateCommand()
    {
        var orderEvent = new OrderPlacedEvent("ORDER-001", "USER-001", "GAME-001", 59.99m);
        var message = JsonSerializer.Serialize(orderEvent);
        var expectedResult = new CreatePaymentTransactionResult(
            Guid.NewGuid(), "ORDER-001", "USER-001", "GAME-001", 59.99m, "Created", DateTime.UtcNow);

        _mockMediator.Setup(m => m.Send(It.IsAny<CreatePaymentTransactionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        await _function.Run(message);

        _mockMediator.Verify(m => m.Send(
            It.Is<CreatePaymentTransactionCommand>(c =>
                c.PurchaseId == "ORDER-001" &&
                c.UserId == "USER-001" &&
                c.GameId == "GAME-001" &&
                c.Amount == 59.99m),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
