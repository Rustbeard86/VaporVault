namespace VaporVault.Core.SteamCmd;

public interface ISteamCmdLocator
{
    /// <summary>
    ///     Ensures SteamCMD is available and returns the path to the SteamCMD executable.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Absolute path to the SteamCMD binary.</returns>
    Task<string> EnsureSteamCmdAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the currently installed SteamCMD path if available.
    /// </summary>
    string? GetInstalledSteamCmdPath();

    /// <summary>
    ///     Checks if SteamCMD exists at the expected location without triggering downloads.
    /// </summary>
    /// <returns>The path to SteamCMD if it exists, null otherwise.</returns>
    string? CheckForSteamCmd();
}