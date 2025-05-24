namespace VaporVault.CLI.Commands;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    Task ExecuteAsync(string[] args);
}