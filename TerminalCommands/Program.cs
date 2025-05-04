using System.CommandLine;
using TerminalCommands.Commands;

namespace TerminalCommands;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Kallehauge CLI tool");

        // Register the parent "git" command
        var gitCommand = new Command("git", "Git related utility commands.");

        // Register the "git cleanup" subcommand
        var gitCleanupCommand = new Command("cleanup",
            "Cleans up local Git repository by interactively prompting to delete branches that are not the current branch.\n\n" +
            "Examples:\n" +
            "  kalle git cleanup -f                          # Force delete all branches except the one you've checked out\n" +
            "  kalle git cleanup --exclude main develop      # Interactive branch deletion while excluding certain branches from the list" +
            "  kalle git cleanup -f --exclude main develop   # Force delete all branches except the ones in the \"exclude\" option\n" +
            "  kalle git cleanup --dry-run -f                # Show branches that would be deleted\n"
        );
        var gitCleanupStrategy = new GitCleanupCommandStrategy();
        gitCleanupStrategy.ConfigureCommand(gitCleanupCommand);
        gitCleanupCommand.SetHandler(gitCleanupStrategy.ExecuteAsync);
        gitCommand.AddCommand(gitCleanupCommand);

        // Add the git command (and its subcommands) to the root
        rootCommand.AddCommand(gitCommand);

        // Parse the command line arguments and invoke the appropriate handler
        return await rootCommand.InvokeAsync(args);
    }
}
