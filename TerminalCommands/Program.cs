using System.CommandLine;
using TerminalCommands.Commands;

namespace TerminalCommands;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Kallehauge CLI tool");

        // Name Command
        var nameCommand = new Command("name", "Asks for a name and shows it.");
        var nameStrategy = new NameCommandStrategy();
        nameStrategy.ConfigureCommand(nameCommand);
        nameCommand.SetHandler(nameStrategy.ExecuteAsync);
        rootCommand.AddCommand(nameCommand);

        // Git Delete Branch Command Setup
        var gitDeleteBranchCommand = new Command("git-delete-branch", "Interactively deletes local git branches.");
        var gitDeleteBranchStrategy = new GitDeleteBranchCommandStrategy();
        gitDeleteBranchStrategy.ConfigureCommand(gitDeleteBranchCommand);
        gitDeleteBranchCommand.SetHandler(gitDeleteBranchStrategy.ExecuteAsync);
        rootCommand.AddCommand(gitDeleteBranchCommand);

        // Parse the incoming args and invoke the handler
        return await rootCommand.InvokeAsync(args);
    }
}
