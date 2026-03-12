using System.Text.Json;
using FCG.Payments.Application.Commands.ProcessPayment;
using FCG.Payments.Functions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FCG.Payments.Functions.Tests;

public class ProcessPaymentFunctionTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<ProcessPaymentFunction>> _mockLogger;
    private readonly ProcessPaymentFunction _function;

    public ProcessPaymentFunctionTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<ProcessPaymentFunction>>();
        _function = new ProcessPaymentFunction(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Run_WithValidMessage_ShouldSendProcessCommand()
    {
        var txId = Guid.NewGuid();
        var message = JsonSerializer.Serialize(new { Id = txId, Amount = 100.50m, CorrelationId = "corr-1" });

        await _function.Run(message);

        _mockMediator.Verify(m => m.Send(
            It.Is<ProcessPaymentCommand>(c =>
                c.TransactionId == txId &&
                c.Amount == 100.50m &&
                c.CorrelationId == "corr-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
