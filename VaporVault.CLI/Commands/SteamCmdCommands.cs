using VaporVault.Core.SteamCmd;

namespace VaporVault.CLI.Commands;

public class SteamCmdCommands : ICommand
{
    private readonly ISteamCmdLocator _steamCmdLocator;

    public SteamCmdCommands(ISteamCmdLocator steamCmdLocator)
    {
        _steamCmdLocator = steamCmdLocator;
    }

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

    private async Task ShowStatus()
    {
        var installedPath = _steamCmdLocator.GetInstalledSteamCmdPath();
        Console.WriteLine("SteamCMD Status:");
        Console.WriteLine($"Installed: {(installedPath != null ? "Yes" : "No")}");
        if (installedPath != null) Console.WriteLine($"Location: {installedPath}");
    }

    private async Task Install()
    {
        Console.WriteLine("Installing/Updating SteamCMD...");
        try
        {
            var path = await _steamCmdLocator.EnsureSteamCmdAvailableAsync();
            Console.WriteLine($"SteamCMD installed successfully at: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to install SteamCMD: {ex.Message}");
        }
    }

    private void ShowHelp()
    {
        Console.WriteLine("SteamCMD Commands:");
        Console.WriteLine("  status    - Show current SteamCMD installation status");
        Console.WriteLine("  install   - Install or update SteamCMD");
        Console.WriteLine("  help      - Show this help message");
    }
}