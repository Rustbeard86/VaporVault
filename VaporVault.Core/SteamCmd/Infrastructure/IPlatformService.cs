namespace VaporVault.Core.SteamCmd.Infrastructure;

public interface IPlatformService
{
    bool IsWindows { get; }
    bool IsLinux { get; }
    string OSDescription { get; }
    Task SetExecutablePermissionsAsync(string filePath, CancellationToken cancellationToken);
}