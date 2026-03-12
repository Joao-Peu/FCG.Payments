using FCG.Payments.Functions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FCG.Payments.Functions.Tests;

public class SendNotificationFunctionTests
{
    [Fact]
    public async Task Run_ShouldCompleteWithoutError()
    {
        var mockLogger = new Mock<ILogger<SendNotificationFunction>>();
        var function = new SendNotificationFunction(mockLogger.Object);

        await function.Run("{\"test\": \"message\"}");
    }
}
