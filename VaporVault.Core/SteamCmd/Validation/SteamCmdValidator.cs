using System.IO.Compression;
using Microsoft.Extensions.Logging;
using VaporVault.Core.SteamCmd.Exceptions;
using VaporVault.Core.SteamCmd.Infrastructure;
using VaporVault.Core.SteamCmd.Validation.Events;

namespace VaporVault.Core.SteamCmd.Validation;

public class SteamCmdValidator(
    IFileSystem fileSystem,
    IPlatformService platformService,
    IValidationEventHandler eventHandler)
    : ISteamCmdValidator
{
    private const string ComponentName = "SteamCmdValidator";

    public async Task ValidateArchiveAsync(string archivePath, CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, object>
        {
            { "ArchivePath", archivePath },
            { "ArchiveType", Path.GetExtension(archivePath) }
        };

        try
        {
            RaiseEvent("Starting archive validation", properties: properties);

            // Add explicit file existence check
            if (!fileSystem.FileExists(archivePath))
            {
                RaiseEvent("Archive file not found", LogLevel.Error, properties: properties);
                throw new SteamCmdValidationException($"Archive file not found at {archivePath}");
            }

            // Add format validation before proceeding
            if (!archivePath.EndsWith(".zip") && !archivePath.EndsWith(".tar.gz"))
            {
                RaiseEvent("Unsupported archive format", LogLevel.Error, properties: properties);
                throw new SteamCmdValidationException("Unsupported archive format for SteamCMD");
            }

            await using var stream = fileSystem.OpenRead(archivePath);
            properties["FileSize"] = stream.Length;

            if (stream.Length == 0)
            {
                RaiseEvent("Empty archive detected", LogLevel.Error, properties: properties);
                throw new SteamCmdValidationException("Downloaded archive is empty");
            }

            if (archivePath.EndsWith(".zip"))
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                properties["EntryCount"] = archive.Entries.Count;

                if (archive.Entries.Count == 0)
                {
                    RaiseEvent("Archive contains no entries", LogLevel.Error, properties: properties);
                    throw new SteamCmdValidationException("Archive contains no entries");
                }

                RaiseEvent("ZIP archive validation successful", properties: properties);
            }
            else if (archivePath.EndsWith(".tar.gz"))
            {
                await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                var buffer = new byte[1024];
                var memory = new Memory<byte>(buffer);

                var bytesRead = await gzipStream.ReadAsync(memory, cancellationToken);
                properties["InitialBytesRead"] = bytesRead;

                if (bytesRead == 0)
                {
                    RaiseEvent("Archive appears to be corrupted", LogLevel.Error, properties: properties);
                    throw new SteamCmdValidationException("Archive appears to be corrupted");
                }

                RaiseEvent("TAR.GZ archive validation successful", properties: properties);
            }
        }
        catch (Exception ex) when (ex is not SteamCmdValidationException)
        {
            RaiseEvent("Archive validation failed", LogLevel.Error, ex, properties);
            throw new SteamCmdValidationException("Failed to validate archive", ex);
        }
    }

    public async Task ValidateExecutableAsync(string executablePath, CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, object>
        {
            { "ExecutablePath", executablePath },
            { "Platform", platformService.OSDescription }
        };

        try
        {
            RaiseEvent("Starting executable validation", properties: properties);

            if (!fileSystem.FileExists(executablePath))
            {
                RaiseEvent("Executable not found", LogLevel.Error, properties: properties);
                throw new SteamCmdValidationException($"Executable not found at {executablePath}");
            }

            await using var stream = fileSystem.OpenRead(executablePath);
            properties["FileSize"] = stream.Length;

            if (stream.Length == 0)
            {
                RaiseEvent("Empty executable file", LogLevel.Error, properties: properties);
                throw new SteamCmdValidationException("Executable file is empty");
            }

            if (platformService.IsWindows)
            {
                var buffer = new byte[2];
                var memory = new Memory<byte>(buffer);

                var bytesRead = await stream.ReadAsync(memory, cancellationToken);
                properties["HeaderBytesRead"] = bytesRead;

                if (bytesRead < 2)
                {
                    RaiseEvent("File too small for executable", LogLevel.Error, properties: properties);
                    throw new SteamCmdValidationException("File is too small to be a valid executable");
                }

                if (buffer[0] != 'M' || buffer[1] != 'Z')
                {
                    RaiseEvent("Invalid executable header", LogLevel.Error, properties: properties);
                    throw new SteamCmdValidationException("File is not a valid Windows executable");
                }
            }
            else
            {
                await platformService.SetExecutablePermissionsAsync(executablePath, cancellationToken);
            }

            RaiseEvent("Executable validation successful", properties: properties);
        }
        catch (Exception ex) when (ex is not SteamCmdValidationException)
        {
            RaiseEvent("Executable validation failed", LogLevel.Error, ex, properties);
            throw new SteamCmdValidationException("Failed to validate executable", ex);
        }
    }

    public Task EnsureSufficientDiskSpaceAsync(string path, long requiredBytes, CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, object>
        {
            { "Path", path },
            { "RequiredBytes", requiredBytes }
        };

        try
        {
            RaiseEvent("Checking disk space", properties: properties);

            var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? throw new ArgumentException("Invalid path"));
            properties["AvailableBytes"] = driveInfo.AvailableFreeSpace;

            if (driveInfo.AvailableFreeSpace < requiredBytes)
            {
                RaiseEvent("Insufficient disk space", LogLevel.Error, properties: properties);
                throw new SteamCmdValidationException(
                    $"Insufficient disk space. Required: {requiredBytes / 1024 / 1024}MB, " +
                    $"Available: {driveInfo.AvailableFreeSpace / 1024 / 1024}MB");
            }

            RaiseEvent("Sufficient disk space available", properties: properties);
        }
        catch (Exception ex) when (ex is not SteamCmdValidationException)
        {
            RaiseEvent("Disk space check failed", LogLevel.Error, ex, properties);
            throw new SteamCmdValidationException("Failed to check disk space", ex);
        }

        return Task.CompletedTask;
    }

    private void RaiseEvent(string message, LogLevel level = LogLevel.Information,
        Exception? ex = null, Dictionary<string, object>? properties = null)
    {
        eventHandler.HandleEvent(new ValidationEvent
        {
            Component = ComponentName,
            Message = message,
            Level = level,
            Exception = ex,
            Properties = properties ?? new Dictionary<string, object>()
        });
    }
}