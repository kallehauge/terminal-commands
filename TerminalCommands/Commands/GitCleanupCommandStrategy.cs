using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;

namespace TerminalCommands.Commands;

/// <summary>
/// Implements the strategy for the "git cleanup" command.
/// Handles parsing options and executing the branch deletion logic.
/// </summary>
public class GitCleanupCommandStrategy : ICommandStrategy
{
    private readonly Option<bool> _forceOption = new Option<bool>(
        aliases: new[] { "--force", "-f" },
        description: "Force delete branches without prompting.");

    private readonly Option<string[]> _excludeOption = new Option<string[]>(
        name: "--exclude",
        description: "Specify branches to exclude from deletion. Can be used multiple times.")
    {
        AllowMultipleArgumentsPerToken = true // Allows --exclude branch1 branch2
    };

    private readonly Option<bool> _dryRunOption = new Option<bool>(
        name: "--dry-run",
        description: "Show which branches would be deleted without actually deleting them.");

    /// <summary>
    /// Adds the specific options for the git cleanup command.
    /// </summary>
    public void ConfigureCommand(Command command)
    {
        command.AddOption(_forceOption);
        command.AddOption(_excludeOption);
        command.AddOption(_dryRunOption);
    }

    /// <summary>
    /// Executes the git cleanup logic.
    /// </summary>
    public Task ExecuteAsync(InvocationContext context)
    {
        // Retrieve option values from the invocation context
        bool force = context.ParseResult.GetValueForOption(_forceOption);
        string[]? excludeBranches = context.ParseResult.GetValueForOption(_excludeOption);
        bool dryRun = context.ParseResult.GetValueForOption(_dryRunOption);
        // Use a HashSet for efficient lookup of excluded branches
        var excludedBranchSet = new HashSet<string>(excludeBranches ?? Array.Empty<string>());

        if (dryRun)
        {
            Console.WriteLine("*** Dry Run Mode: No branches will be deleted. ***");
        }

        // Ensure the command is run within a git repository
        if (!IsInGitRepository())
        {
            Console.WriteLine("Error: Not in a git repository.");
            context.ExitCode = 1;
            return Task.CompletedTask;
        }

        // Get the current branch name
        string currentBranch = GetGitOutput("rev-parse --abbrev-ref HEAD");
        if (string.IsNullOrWhiteSpace(currentBranch))
        {
            Console.WriteLine("Error: Could not determine the current branch.");
            context.ExitCode = 1;
            return Task.CompletedTask;
        }
        currentBranch = currentBranch.Trim();

        // Get all local branches
        string branchesOutput = GetGitOutput("for-each-ref --format=%(refname:short) refs/heads/");
        if (string.IsNullOrWhiteSpace(branchesOutput))
        {
            Console.WriteLine("No local branches found or error fetching branches.");
            return Task.CompletedTask;
        }

        List<string> branches = branchesOutput.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries).ToList();

        // Iterate through each local branch
        foreach (string? branch in branches)
        {
            string trimmedBranch = branch.Trim();

            // Skip the current branch and any explicitly excluded branches
            if (trimmedBranch == currentBranch || excludedBranchSet.Contains(trimmedBranch))
            {
                continue;
            }

            bool deleteBranch = force; // Assume deletion if --force is used

            // Prompt user if not using --force
            if (!force)
            {
                Console.Write($"{(dryRun ? "[Dry Run] Would delete" : "Delete")} branch '{trimmedBranch}'? (y/n): ");
                string? choice = Console.ReadLine()?.Trim().ToLower();
                if (choice == "y")
                {
                    deleteBranch = true;
                }
                else
                {
                    Console.WriteLine($"Skipped branch '{trimmedBranch}'.");
                    deleteBranch = false; // Ensure we don't delete if skipped
                }
            }

            // Perform deletion or dry run output
            if (deleteBranch)
            {
                if (dryRun)
                {
                    Console.WriteLine($"[Dry Run] Would delete branch '{trimmedBranch}'.");
                }
                else
                {
                    Console.WriteLine($"Deleting branch '{trimmedBranch}'...");
                    (string deleteOutput, int exitCode) = RunGitCommand($"branch -D \"{trimmedBranch}\"");
                    if (exitCode == 0)
                    {
                        Console.WriteLine($"Successfully deleted branch '{trimmedBranch}'.");
                        if (!string.IsNullOrWhiteSpace(deleteOutput)) Console.WriteLine(deleteOutput);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to delete branch '{trimmedBranch}'. Exit code: {exitCode}");
                        if (!string.IsNullOrWhiteSpace(deleteOutput)) Console.WriteLine($"Git Error: {deleteOutput}");
                    }
                }
            }
        }

        Console.WriteLine($"Branch deletion process completed{(dryRun ? " (Dry Run)" : "")}.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the current directory is within a Git repository.
    /// </summary>
    /// <returns>True if inside a Git repository, false otherwise.</returns>
    private static bool IsInGitRepository()
    {
        (string _, int exitCode) = RunGitCommand("rev-parse --is-inside-work-tree");
        return exitCode == 0;
    }

    /// <summary>
    /// Executes a Git command and returns its standard output.
    /// Returns an empty string if the command fails.
    /// </summary>
    /// <param name="args">Arguments to pass to the git command.</param>
    /// <returns>The standard output of the git command, or empty string on error.</returns>
    private static string GetGitOutput(string args)
    {
         (string output, int exitCode) = RunGitCommand(args);
         return exitCode == 0 ? output : string.Empty;
    }

    /// <summary>
    /// Helper method to run a Git command and capture its output and exit code.
    /// </summary>
    /// <param name="args">Arguments to pass to the git command.</param>
    /// <returns>A tuple containing the combined standard output/error and the exit code.</returns>
    private static (string Output, int ExitCode) RunGitCommand(string args)
    {
        try
        {
            var startInfo = new ProcessStartInfo("git", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory // Run in the current directory
            };

            using (Process? process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    return ("Failed to start git process.", -1);
                }

                // Read output and error streams
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Combine output and error if the command failed
                string combinedOutput = process.ExitCode != 0
                    ? $"{output}{Environment.NewLine}{error}".Trim()
                    : output.Trim();

                return (combinedOutput, process.ExitCode);
            }
        }
        catch (Exception ex) // Catch potential exceptions like git not being found
        {
            Console.WriteLine($"An error occurred while trying to run git: {ex.Message}");
            return ($"Exception: {ex.Message}", -1);
        }
    }
}
