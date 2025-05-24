namespace VaporVault.CLI.Commands;

internal class App(IEnumerable<ICommand> commands)
{
    public async Task RunAsync(string[] args)
    {
        Console.WriteLine("VaporVault CLI");
        Console.WriteLine("-------------");

        if (args.Length == 0 || args[0] == "help")
        {
            ShowHelp();
            return;
        }

        var command = commands.FirstOrDefault(c =>
            string.Equals(c.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (command == null)
        {
            Console.WriteLine($"Unknown command: {args[0]}");
            ShowHelp();
            return;
        }

        try
        {
            await command.ExecuteAsync(args[1..]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void ShowHelp()
    {
        Console.WriteLine("\nAvailable commands:");
        foreach (var command in commands) Console.WriteLine($"  {command.Name,-12} - {command.Description}");
        Console.WriteLine("  help         - Show this help message");
    }
}