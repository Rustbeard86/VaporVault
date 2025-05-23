using System.Formats.Tar;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using VaporVault.Core.SteamCmd.Infrastructure;
using VaporVault.Core.SteamCmd.Validation;

namespace VaporVault.Core.SteamCmd;

public class SteamCmdLocator : ISteamCmdLocator
{
    private const long EstimatedRequiredSpace = 50 * 1024 * 1024; // 50MB minimum
    private readonly IFileSystem _fileSystem;
    private readonly IHttpDownloadService _httpDownloadService;
    private readonly ILogger<SteamCmdLocator> _logger;
    private readonly SteamCmdDownloadOptions _options;
    private readonly IPlatformService _platformService;
    private readonly ISteamCmdValidator _validator;
    private string? _cachedPath;

    public SteamCmdLocator(
        ILogger<SteamCmdLocator> logger,
        IFileSystem fileSystem,
        IPlatformService platformService,
        IHttpDownloadService httpDownloadService,
        ISteamCmdValidator validator,
        SteamCmdDownloadOptions? options = null)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _platformService = platformService;
        _httpDownloadService = httpDownloadService;
        _validator = validator;
        _options = options ?? new SteamCmdDownloadOptions();
    }

    public async Task<string> EnsureSteamCmdAvailableAsync(CancellationToken cancellationToken = default)
    {
        var baseDir = _options.InstallDirectory ?? _fileSystem.Combine(AppContext.BaseDirectory, "steamcmd");
        _fileSystem.CreateDirectory(baseDir);

        var exeName = GetSteamCmdExecutableName();
        var fullPath = _fileSystem.Combine(baseDir, exeName);

        if (_fileSystem.FileExists(fullPath) && !_options.ForceRedownload)
        {
            _cachedPath = fullPath;
            return fullPath;
        }

        await DownloadAndExtractSteamCmdAsync(baseDir, cancellationToken);

        if (!_platformService.IsWindows)
            await _platformService.SetExecutablePermissionsAsync(fullPath, cancellationToken);

        _cachedPath = fullPath;
        return fullPath;
    }

    string? ISteamCmdLocator.GetInstalledSteamCmdPath()
    {
        return _cachedPath;
    }

    private string GetSteamCmdExecutableName()
    {
        return _platformService.IsWindows ? "steamcmd.exe" : "steamcmd";
    }

    private async Task DownloadAndExtractSteamCmdAsync(string baseDir, CancellationToken cancellationToken)
    {
        _logger.LogInformation("SteamCMD not found. Downloading for {OS}", _platformService.OSDescription);

        // Check disk space before download
        await _validator.EnsureSufficientDiskSpaceAsync(baseDir, EstimatedRequiredSpace, cancellationToken);

        var url = GetSteamCmdDownloadUrl();
        var archivePath = _fileSystem.Combine(baseDir, Path.GetFileName(new Uri(url).LocalPath));

        try
        {
            await _httpDownloadService.DownloadFileAsync(url, archivePath, cancellationToken);
            await _validator.ValidateArchiveAsync(archivePath, cancellationToken);
            await ExtractArchiveAsync(archivePath, baseDir, cancellationToken);

            var executablePath = _fileSystem.Combine(baseDir, GetSteamCmdExecutableName());
            await _validator.ValidateExecutableAsync(executablePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download or extract SteamCMD");
            throw;
        }
        finally
        {
            if (_fileSystem.FileExists(archivePath))
                _fileSystem.DeleteFile(archivePath);
        }
    }

    private string GetSteamCmdDownloadUrl()
    {
        if (_platformService.IsWindows)
            return "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";

        return _platformService.IsLinux
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
                var destPath = _fileSystem.Combine(destinationDir, entry.FullName);
                var destDir = _fileSystem.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir)) _fileSystem.CreateDirectory(destDir);
                await using var entryStream = entry.Open();
                await using var destStream = _fileSystem.Create(destPath);
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
        await using var inStream = _fileSystem.OpenRead(archivePath);
        await using var gzipStream = new GZipStream(inStream, CompressionMode.Decompress);

        var tarReader = new TarReader(gzipStream);
        TarEntry? entry;
        while ((entry = await tarReader.GetNextEntryAsync(cancellationToken: cancellationToken)) != null)
        {
            var outPath = _fileSystem.Combine(destDir, entry.Name);
            if (entry.EntryType == TarEntryType.Directory)
            {
                _fileSystem.CreateDirectory(outPath);
            }
            else
            {
                var dirPath = _fileSystem.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(dirPath)) _fileSystem.CreateDirectory(dirPath);
                if (entry.DataStream != null)
                {
                    await using var outStream = _fileSystem.Create(outPath);
                    await entry.DataStream.CopyToAsync(outStream, cancellationToken);
                }
            }
        }
    }
}