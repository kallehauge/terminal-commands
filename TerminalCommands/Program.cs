using System.CommandLine;
using TerminalCommands.Commands;

namespace TerminalCommands;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Kallehauge CLI Tools - A collection of helpful command-line utilities.");

        // Register the parent "git" command
        var gitCommand = new Command("git", "Git related utility commands.");

        // Register the "git cleanup" subcommand
        var gitCleanupCommand = new Command("cleanup",
            "Cleans up local Git repository by interactively prompting to delete branches that are not the current branch.\n\n" +
            "Examples:\n" +
            "  kalle git cleanup --exclude main develop      # Interactive branch deletion, excluding 'main' and 'develop'\n" +
            "  kalle git cleanup -f                          # Force delete all local branches except the current one\n" +
            "  kalle git cleanup -f --exclude main develop   # Force delete all local branches except 'main', 'develop', and the current one\n" +
            "  kalle git cleanup --dry-run -f                # Show branches that would be deleted by the force option"
        );
        var gitCleanupStrategy = new GitCleanupCommandStrategy();
        gitCleanupStrategy.ConfigureCommand(gitCleanupCommand);
        gitCleanupCommand.SetHandler(gitCleanupStrategy.ExecuteAsync);
        gitCommand.AddCommand(gitCleanupCommand);

        // Register the "git init" subcommand
        var gitInitCommand = new Command("init",
            "Interactively configures useful global Git aliases to streamline common Git operations.\n\n" +
            "This command checks your global ~/.gitconfig file and prompts [Y/n] (default Yes) \n" +
            "before adding or updating the following aliases:\n" +
            "  git co    = git checkout\n" +
            "  git ci    = git commit\n" +
            "  git st    = git status\n" +
            "  git br    = git branch\n" +
            "  git amend = git commit --amend\n" +
            "  git cob   = git checkout -B (create or reset branch)\n" +
            "  git nuke  = git reset --hard (use with caution!)\n" +
            "  cleanup   = <path_to_this_executable> git cleanup (calls the kalle git cleanup command)\n\n" +
            "If an alias already exists with the correct command, it will be skipped automatically.\n" +
            "If an alias exists but points to a different command, you will be prompted to update it.\n\n" +
            "After running this command, you can use the configured aliases directly with the standard 'git' command.\n\n" +
            "Examples:\n" +
            "  kalle git init     # Run this once to interactively set up the aliases globally\n" +
            "  git st             # After configuration, this is equivalent to 'git status'\n" +
            "  git co my-branch   # Equivalent to 'git checkout my-branch'\n" +
            "  git ci -m \"Msg\"    # Equivalent to 'git commit -m \"Msg\"'\n" +
            "  git cleanup        # Equivalent to running '<path_to_this_executable> git cleanup' (e.g., 'kalle git cleanup')\n"
        );
        var gitInitStrategy = new GitInitCommandStrategy();
        gitInitStrategy.ConfigureCommand(gitInitCommand);
        gitInitCommand.SetHandler(gitInitStrategy.ExecuteAsync);
        gitCommand.AddCommand(gitInitCommand);

        // Add the "git" command (and its subcommands) to the root
        rootCommand.AddCommand(gitCommand);

        // Register the "mozjpeg" command
        var mozjpegCommand = new Command("mozjpeg",
            "Optimize JPEG images using mozjpeg.\n\n" +
            "Examples:\n" +
            "  kalle mozjpeg input.jpg output.jpg              # Optimize with default quality (75)\n" +
            "  kalle mozjpeg --quality 85 input.jpg output.jpg # Optimize with custom quality\n" +
            "  kalle mozjpeg --update                          # Update the mozjpeg Docker image\n\n" +
            "The command uses Docker to run mozjpeg, so make sure Docker is installed and running."
        );
        var mozjpegStrategy = new MozjpegCommandStrategy();
        mozjpegStrategy.ConfigureCommand(mozjpegCommand);
        mozjpegCommand.SetHandler(mozjpegStrategy.ExecuteAsync);
        rootCommand.AddCommand(mozjpegCommand);

        // Register the "guetzli" command
        var guetzliCommand = new Command("guetzli",
            "Optimize JPEG images using Google's Guetzli.\n\n" +
            "Examples:\n" +
            "  kalle guetzli input.jpg output.jpg                  # Optimize with default quality (95)\n" +
            "  kalle guetzli --quality 90 input.jpg output.jpg     # Optimize with custom quality\n" +
            "  kalle guetzli --memlimit 8000 input.jpg output.jpg  # Set memory limit to 8000MB\n" +
            "  kalle guetzli --verbose input.jpg output.jpg        # Show detailed progress\n" +
            "  kalle guetzli --update                              # Update the Guetzli Docker image\n\n" +
            "The command uses Docker to run Guetzli, so make sure Docker is installed and running.\n" +
            "Note: Guetzli uses a large amount of memory (about 300MB per 1MPix of input image)."
        );
        var guetzliStrategy = new GuetzliCommandStrategy();
        guetzliStrategy.ConfigureCommand(guetzliCommand);
        guetzliCommand.SetHandler(guetzliStrategy.ExecuteAsync);
        rootCommand.AddCommand(guetzliCommand);

        // Parse the command line arguments and invoke the appropriate handler
        return await rootCommand.InvokeAsync(args);
    }
}
