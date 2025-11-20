using Microsoft.Extensions.Logging;
using Moq;

public static class TestLoggerExtensions
{
    public static void VerifyLogWithEventId<T>(
        this Mock<ILogger<T>> loggerMock,
        LogLevel expectedLevel,
        int expectedEventId)
    {
        loggerMock.Verify(x =>
                x.Log(
                    expectedLevel,
                    It.Is<EventId>(e => e.Id == expectedEventId),
                    It.Is<It.IsAnyType>((obj, _) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((_, __) => true)
                ),
            Times.AtLeastOnce()
        );
    }
}