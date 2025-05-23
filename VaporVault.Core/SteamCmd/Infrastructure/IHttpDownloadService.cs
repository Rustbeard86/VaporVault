namespace VaporVault.Core.SteamCmd.Infrastructure;

public interface IHttpDownloadService
{
    Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken);
}