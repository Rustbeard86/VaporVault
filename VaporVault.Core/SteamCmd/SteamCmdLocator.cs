using System.Formats.Tar;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using VaporVault.Core.SteamCmd.Infrastructure;
using VaporVault.Core.SteamCmd.Validation;

namespace VaporVault.Core.SteamCmd;

public class SteamCmdLocator(
    ILogger<SteamCmdLocator> logger,
    IFileSystem fileSystem,
    IPlatformService platformService,
    IHttpDownloadService httpDownloadService,
    ISteamCmdValidator validator,
    SteamCmdDownloadOptions? options = null)
    : ISteamCmdLocator
{
    private const long EstimatedRequiredSpace = 50 * 1024 * 1024; // 50MB minimum
    private readonly SteamCmdDownloadOptions _options = options ?? new SteamCmdDownloadOptions();
    private string? _cachedPath;

    public async Task<string> EnsureSteamCmdAvailableAsync(CancellationToken cancellationToken = default)
    {
        var baseDir = _options.InstallDirectory ?? fileSystem.Combine(AppContext.BaseDirectory, "steamcmd");
        fileSystem.CreateDirectory(baseDir);

        var exeName = GetSteamCmdExecutableName();
        var fullPath = fileSystem.Combine(baseDir, exeName);

        if (fileSystem.FileExists(fullPath) && !_options.ForceRedownload)
        {
            _cachedPath = fullPath;
            return fullPath;
        }

        await DownloadAndExtractSteamCmdAsync(baseDir, cancellationToken);

        if (!platformService.IsWindows)
            await platformService.SetExecutablePermissionsAsync(fullPath, cancellationToken);

        _cachedPath = fullPath;
        return fullPath;
    }

    string? ISteamCmdLocator.GetInstalledSteamCmdPath()
    {
        return _cachedPath;
    }

    public string? CheckForSteamCmd()
    {
        var baseDir = _options.InstallDirectory ?? fileSystem.Combine(AppContext.BaseDirectory, "steamcmd");
        var exeName = GetSteamCmdExecutableName();
        var fullPath = fileSystem.Combine(baseDir, exeName);

        if (fileSystem.FileExists(fullPath))
        {
            _cachedPath = fullPath;
            return fullPath;
        }

        return null;
    }

    private string GetSteamCmdExecutableName()
    {
        return platformService.IsWindows ? "steamcmd.exe" : "steamcmd";
    }

    private async Task DownloadAndExtractSteamCmdAsync(string baseDir, CancellationToken cancellationToken)
    {
        logger.LogInformation("SteamCMD not found. Downloading for {OS}", platformService.OSDescription);

        // Check disk space before download
        await validator.EnsureSufficientDiskSpaceAsync(baseDir, EstimatedRequiredSpace, cancellationToken);

        var url = GetSteamCmdDownloadUrl();
        var archivePath = fileSystem.Combine(baseDir, Path.GetFileName(new Uri(url).LocalPath));

        try
        {
            await httpDownloadService.DownloadFileAsync(url, archivePath, cancellationToken);
            await validator.ValidateArchiveAsync(archivePath, cancellationToken);
            await ExtractArchiveAsync(archivePath, baseDir, cancellationToken);

            var executablePath = fileSystem.Combine(baseDir, GetSteamCmdExecutableName());
            await validator.ValidateExecutableAsync(executablePath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download or extract SteamCMD");
            throw;
        }
        finally
        {
            if (fileSystem.FileExists(archivePath))
                fileSystem.DeleteFile(archivePath);
        }
    }

    private string GetSteamCmdDownloadUrl()
    {
        if (platformService.IsWindows)
            return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";

        return platformService.IsLinux
            ? "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz"
            : "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_osx.tar.gz";
    }

    private async Task ExtractArchiveAsync(string archivePath, string destinationDir,
        CancellationToken cancellationToken)
    {
        if (archivePath.EndsWith(".zip"))
        {
            using var archive = ZipFile.OpenRead(archivePath);
            foreach (var entry in archive.Entries)
            {
                var destPath = fileSystem.Combine(destinationDir, entry.FullName);
                var destDir = fileSystem.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir)) fileSystem.CreateDirectory(destDir);
                await using var entryStream = entry.Open();
                await using var destStream = fileSystem.Create(destPath);
                await entryStream.CopyToAsync(destStream, cancellationToken);
            }
        }
        else if (archivePath.EndsWith(".tar.gz"))
        {
            await ExtractTarGzAsync(archivePath, destinationDir, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Unsupported archive format for SteamCMD.");
        }
    }

    private async Task ExtractTarGzAsync(string archivePath, string destDir, CancellationToken cancellationToken)
    {
        await using var inStream = fileSystem.OpenRead(archivePath);
        await using var gzipStream = new GZipStream(inStream, CompressionMode.Decompress);

        var tarReader = new TarReader(gzipStream);
        TarEntry? entry;
        while ((entry = await tarReader.GetNextEntryAsync(cancellationToken: cancellationToken)) != null)
        {
            var outPath = fileSystem.Combine(destDir, entry.Name);
            if (entry.EntryType == TarEntryType.Directory)
            {
                fileSystem.CreateDirectory(outPath);
            }
            else
            {
                var dirPath = fileSystem.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(dirPath)) fileSystem.CreateDirectory(dirPath);
                if (entry.DataStream == null) continue;
                await using var outStream = fileSystem.Create(outPath);
                await entry.DataStream.CopyToAsync(outStream, cancellationToken);
            }
        }
    }
}