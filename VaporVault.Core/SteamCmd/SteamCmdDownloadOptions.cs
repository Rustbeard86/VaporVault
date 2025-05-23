namespace VaporVault.Core.SteamCmd;

public class SteamCmdDownloadOptions
{
    public string? InstallDirectory { get; set; }
    public bool ForceRedownload { get; set; } = false;
}