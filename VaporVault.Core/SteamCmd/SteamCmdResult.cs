namespace VaporVault.Core.SteamCmd;

public class SteamCmdResult
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SteamCmdResult" /> class.
    /// </summary>
    /// <param name="success">Whether the operation completed successfully.</param>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="output">The standard output from the operation.</param>
    /// <param name="error">The standard error output from the operation.</param>
    public SteamCmdResult(bool success, int exitCode, string output, string error)
    {
        Success = success;
        ExitCode = exitCode;
        Output = output;
        Error = error;
    }

    /// <summary>
    ///     Gets whether the SteamCmd operation completed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    ///     Gets the process exit code from the SteamCmd operation.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    ///     Gets the standard output from the SteamCmd operation.
    /// </summary>
    public string Output { get; }

    /// <summary>
    ///     Gets the standard error output from the SteamCmd operation.
    /// </summary>
    public string Error { get; }

    /// <summary>
    ///     Gets a value indicating whether the operation produced any error output.
    /// </summary>
    public bool HasErrors => !string.IsNullOrWhiteSpace(Error);

    /// <summary>
    ///     Returns a string representation of the SteamCmd result.
    /// </summary>
    public override string ToString()
    {
        return $"SteamCmd {(Success ? "succeeded" : "failed")} (exit code: {ExitCode})" +
               (HasErrors ? $"\nErrors:\n{Error}" : "");
    }
}