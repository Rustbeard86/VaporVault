namespace VaporVault.Core.SteamCmd.Validation;

public interface ISteamCmdValidator
{
    Task ValidateArchiveAsync(string archivePath, CancellationToken cancellationToken);
    Task ValidateExecutableAsync(string executablePath, CancellationToken cancellationToken);
    Task EnsureSufficientDiskSpaceAsync(string path, long requiredBytes, CancellationToken cancellationToken);
}