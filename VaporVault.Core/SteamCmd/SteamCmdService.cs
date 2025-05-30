﻿using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using VaporVault.Core.Abstractions;
using VaporVault.Core.SteamCmd.Infrastructure;

namespace VaporVault.Core.SteamCmd;

public class SteamCmdService(
    ISteamCmdLocator steamCmdLocator,
    ILogger<SteamCmdService> logger,
    IPlatformService platformService)
    : ISteamCmdService
{
    public async Task<SteamCmdResult> RunCommandAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var steamCmdPath = await steamCmdLocator.EnsureSteamCmdAvailableAsync(cancellationToken);
        logger.LogDebug("Running SteamCMD command on {OS}: {Arguments}", platformService.OSDescription, arguments);

        // Ensure executable permissions on Unix-like systems
        if (!platformService.IsWindows)
        {
            await platformService.SetExecutablePermissionsAsync(steamCmdPath, cancellationToken);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = steamCmdPath,
            Arguments = arguments + " +quit",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // Add platform-specific process configuration
        if (!platformService.IsWindows)
        {
            // On Unix-like systems, ensure PATH is preserved for Steam runtime dependencies
            startInfo.UseShellExecute = false;
            startInfo.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH");
        }

        using var process = new Process();
        process.StartInfo = startInfo;
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            logger.LogTrace("[SteamCMD] {Line}", e.Data);
            output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            logger.LogWarning("[SteamCMD Error] {Line}", e.Data);
            error.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!await WaitForProcessAsync(process, cancellationToken))
            {
                process.Kill(true);
                throw new OperationCanceledException("SteamCMD operation was canceled.");
            }

            var success = process.ExitCode == 0;
            if (!success) logger.LogError("SteamCMD exited with code {ExitCode}", process.ExitCode);

            return new SteamCmdResult(
                success,
                process.ExitCode,
                output.ToString(),
                error.ToString()
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to execute SteamCMD command");
            throw;
        }
    }

    public async Task<SteamCmdResult> DownloadDepotAsync(
        long appId,
        long depotId,
        string? manifestId = null,
        CancellationToken cancellationToken = default)
    {
        var args = new StringBuilder();
        args.Append($"+download_depot {appId} {depotId}");

        if (!string.IsNullOrEmpty(manifestId)) args.Append($" {manifestId}");

        logger.LogInformation("Downloading depot {DepotId} for app {AppId}{Manifest}",
            depotId, appId, manifestId != null ? $" (manifest: {manifestId})" : "");

        return await RunCommandAsync(args.ToString(), cancellationToken);
    }

    private static async Task<bool> WaitForProcessAsync(Process process, CancellationToken cancellationToken)
    {
        var processCompletionSource = new TaskCompletionSource<bool>();

        void ProcessExited(object? sender, EventArgs e)
        {
            processCompletionSource.TrySetResult(true);
        }

        process.EnableRaisingEvents = true;
        process.Exited += ProcessExited;

        try
        {
            await using var registration = cancellationToken.Register(() => processCompletionSource.TrySetResult(false));

            if (process.HasExited) return true;

            return await processCompletionSource.Task;
        }
        finally
        {
            process.Exited -= ProcessExited;
        }
    }
}