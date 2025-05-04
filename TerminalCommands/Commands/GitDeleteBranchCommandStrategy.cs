using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;

namespace TerminalCommands.Commands;

public class GitDeleteBranchCommandStrategy : ICommandStrategy
{
    public void ConfigureCommand(Command command)
    {
        // No options or arguments needed for this command
    }

    public Task ExecuteAsync(InvocationContext context)
    {
        Console.Write("This command will prompt to delete local branches. Continue? (y/n): ");
        string? confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm != "y")
        {
            Console.WriteLine("Operation cancelled.");
            return Task.CompletedTask;
        }

        if (!IsInGitRepository())
        {
            Console.WriteLine("Error: Not in a git repository.");
            context.ExitCode = 1;
            return Task.CompletedTask;
        }

        string currentBranch = GetGitOutput("rev-parse --abbrev-ref HEAD");
        if (string.IsNullOrWhiteSpace(currentBranch))
        {
            Console.WriteLine("Error: Could not determine the current branch.");
            context.ExitCode = 1;
            return Task.CompletedTask;
        }
        currentBranch = currentBranch.Trim();

        string branchesOutput = GetGitOutput("for-each-ref --format=%(refname:short) refs/heads/");
        if (string.IsNullOrWhiteSpace(branchesOutput))
        {
            Console.WriteLine("No local branches found or error fetching branches.");
            return Task.CompletedTask;
        }

        List<string> branches = branchesOutput.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries).ToList();

        foreach (string? branch in branches)
        {
            string trimmedBranch = branch.Trim();
            if (trimmedBranch == currentBranch)
            {
                continue;
            }

            Console.Write($"Delete branch '{trimmedBranch}'? (y/n): ");
            string? choice = Console.ReadLine()?.Trim().ToLower();
            if (choice == "y")
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
            else
            {
                Console.WriteLine($"Skipped branch '{trimmedBranch}'.");
            }
        }

        Console.WriteLine("Branch deletion process completed.");
        return Task.CompletedTask;
    }

    private static bool IsInGitRepository()
    {
        (string _, int exitCode) = RunGitCommand("rev-parse --is-inside-work-tree");
        return exitCode == 0;
    }

    private static string GetGitOutput(string args)
    {
         (string output, int exitCode) = RunGitCommand(args);
         return exitCode == 0 ? output : string.Empty;
    }

    private static (string Output, int ExitCode) RunGitCommand(string args)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("git", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            using (Process? process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    string combinedOutput = process.ExitCode != 0
                        ? $"{output}{Environment.NewLine}{error}".Trim()
                        : output.Trim();

                    return (combinedOutput, process.ExitCode);
                }
                else
                {
                     return ("Failed to start git process.", -1);
                }
            }
        }
        catch (Exception ex)
        {
            // Catch specific exceptions like Win32Exception if git isn't found.
            Console.WriteLine($"An error occurred while trying to run git: {ex.Message}");
            return ($"Exception: {ex.Message}", -1);
        }
    }
}
