using VaporVault.Core.SteamCmd;

namespace VaporVault.CLI.Commands;

public class SteamCmdCommands(ISteamCmdLocator steamCmdLocator) : ICommand
{
    public string Name => "steamcmd";
    public string Description => "Manage SteamCMD installation and configuration";

    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0 || args[0] == "status")
        {
            await ShowStatus();
            return;
        }

        switch (args[0].ToLower())
        {
            case "install":
                await Install();
                break;
            case "help":
                ShowHelp();
                break;
            default:
                Console.WriteLine($"Unknown subcommand: {args[0]}");
                ShowHelp();
                break;
        }
    }

    private Task ShowStatus()
    {
        // First check the cache
        var installedPath = steamCmdLocator.GetInstalledSteamCmdPath();

        // If not in cache, check the filesystem without triggering downloads
        if (installedPath == null)
        {
            installedPath = steamCmdLocator.CheckForSteamCmd();
        }

        Console.WriteLine("SteamCMD Status:");
        Console.WriteLine($"Installed: {(installedPath != null ? "Yes" : "No")}");
        if (installedPath != null) Console.WriteLine($"Location: {installedPath}");

        return Task.CompletedTask;
    }

    private async Task Install()
    {
        try
        {
            var path = await steamCmdLocator.EnsureSteamCmdAvailableAsync();
            var wasExisting = steamCmdLocator.GetInstalledSteamCmdPath() == path;

            if (wasExisting)
            {
                Console.WriteLine($"SteamCMD is already installed at: {path}");
            }
            else
            {
                Console.WriteLine("Installing SteamCMD...");
                Console.WriteLine($"SteamCMD installed successfully at: {path}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to install SteamCMD: {ex.Message}");
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("SteamCMD Commands:");
        Console.WriteLine("  status    - Show current SteamCMD installation status");
        Console.WriteLine("  install   - Install or update SteamCMD");
        Console.WriteLine("  help      - Show this help message");
    }
}