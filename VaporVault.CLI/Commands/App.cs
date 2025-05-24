using VaporVault.CLI.Commands;

namespace VaporVault.CLI;

internal class App
{
    private readonly IEnumerable<ICommand> _commands;

    public App(IEnumerable<ICommand> commands)
    {
        _commands = commands;
    }

    public async Task RunAsync(string[] args)
    {
        Console.WriteLine("VaporVault CLI");
        Console.WriteLine("-------------");

        if (args.Length == 0 || args[0] == "help")
        {
            ShowHelp();
            return;
        }

        var command = _commands.FirstOrDefault(c => c.Name == args[0].ToLower());
        if (command == null)
        {
            Console.WriteLine($"Unknown command: {args[0]}");
            ShowHelp();
            return;
        }

        try
        {
            await command.ExecuteAsync(args.Skip(1).ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void ShowHelp()
    {
        Console.WriteLine("\nAvailable commands:");
        foreach (var command in _commands) Console.WriteLine($"  {command.Name,-12} - {command.Description}");
        Console.WriteLine("  help         - Show this help message");
    }
}