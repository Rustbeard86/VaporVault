using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VaporVault.Core.SteamCmd.Infrastructure;

public class PlatformService : IPlatformService
{
    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public string OSDescription => RuntimeInformation.OSDescription;

    public async Task SetExecutablePermissionsAsync(string filePath, CancellationToken cancellationToken)
    {
        if (IsWindows) return;

        var proc = Process.Start("chmod", $"+x \"{filePath}\"");
        await proc.WaitForExitAsync(cancellationToken);
    }
}