namespace VaporVault.Core.SteamCmd.Infrastructure;

public class HttpDownloadService : IHttpDownloadService
{
    private readonly HttpClient _httpClient;

    public HttpDownloadService()
    {
        _httpClient = new HttpClient();
    }

    public async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
        await using var fs = File.Create(destinationPath);
        await stream.CopyToAsync(fs, cancellationToken);
    }
}