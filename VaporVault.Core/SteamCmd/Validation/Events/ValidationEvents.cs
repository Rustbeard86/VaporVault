using Microsoft.Extensions.Logging;

namespace VaporVault.Core.SteamCmd.Validation.Events;

public record ValidationEvent
{
    public required string Component { get; init; }
    public required string Message { get; init; }
    public LogLevel Level { get; init; } = LogLevel.Information;
    public Exception? Exception { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public class LoggingValidationEventHandler : IValidationEventHandler
{
    private readonly ILogger _logger;

    public LoggingValidationEventHandler(ILogger logger)
    {
        _logger = logger;
    }

    public void HandleEvent(ValidationEvent evt)
    {
        var state = new Dictionary<string, object>(evt.Properties)
        {
            { "Timestamp", evt.Timestamp },
            { "Component", evt.Component }
        };

        using var scope = _logger.BeginScope(state);
        _logger.Log(
            evt.Level,
            evt.Exception,
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Component}] {Message}",
            evt.Timestamp,
            evt.Component,
            evt.Message);
    }
}