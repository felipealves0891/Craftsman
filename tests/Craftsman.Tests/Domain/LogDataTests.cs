using Craftsman.Domain;

namespace Craftsman.Tests.Domain;

public sealed class LogDataTests
{
    [Fact]
    public void Constructor_preserves_request_correlation_and_safe_summary()
    {
        var summary = new { OrderId = Guid.NewGuid(), Operation = "ManualOrder.Edit" };

        using var log = new LogData(
            "POST",
            "/ManualOrders/Edit",
            new Dictionary<string, string>(),
            summary,
            "trace-123");

        Assert.Equal("trace-123", log.CorrelationId);
        Assert.Same(summary, log.RequestBody);
    }

    [Fact]
    public void Dispose_does_not_replace_terminal_error_with_success()
    {
        using var log = new LogData(
            "POST",
            "/ManualOrders/Edit",
            new Dictionary<string, string>(),
            new { OrderId = Guid.NewGuid() },
            "trace-123");

        log.FinishLogWithError(new InvalidOperationException("failure"));
        log.Dispose();

        Assert.Equal("Error", log.Status);
        Assert.NotNull(log.Error);
        Assert.Null(log.ResponseBody);
        Assert.True(log.EleapsedTime >= 0);
    }

    [Fact]
    public void Terminal_log_cannot_be_overwritten()
    {
        using var log = new LogData(
            "POST",
            "/ManualOrders/Edit",
            new Dictionary<string, string>(),
            new { OrderId = Guid.NewGuid() },
            "trace-123");

        log.FinishLogWithSuccess("saved");
        log.FinishLogWithError(new InvalidOperationException("late failure"));

        Assert.Equal("Success", log.Status);
        Assert.Equal("saved", log.ResponseBody);
        Assert.Null(log.Error);
    }
}
