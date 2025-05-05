using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;

namespace TerminalCommands.Commands;

/// <summary>
/// Implements the strategy for the "git init" command.
/// Interactively configures a set of useful global Git aliases, including one for the 'cleanup' command.
/// </summary>
public class GitInitCommandStrategy : ICommandStrategy
{
    // Define the standard aliases to be configured
    private readonly Dictionary<string, string> _standardAliases = new()
    {
        { "co", "checkout" },
        { "ci", "commit" },
        { "st", "status" },
        { "br", "branch" },
        { "amend", "commit --amend" },
        { "cob", "checkout -B" },
        { "nuke", "reset --hard" }
    };

    /// <summary>
    /// Configures the command. No specific options or arguments are needed for this command.
    /// </summary>
    public void ConfigureCommand(Command command)
    {
        // This command does not require any specific options or arguments.
    }

    /// <summary>
    /// Executes the logic to add or update global Git aliases, prompting for each one.
    /// </summary>
    public Task ExecuteAsync(InvocationContext context)
    {
        Dictionary<string, string>? aliasesToConfigure = GetAliasesToConfigure();

        if (aliasesToConfigure is not null)
        {
            DisplayProposedAliases(aliasesToConfigure);

            int configuredCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            int declinedCount = 0;

            foreach (var alias in aliasesToConfigure)
            {
                ProcessSingleAlias(alias.Key, alias.Value,
                    ref configuredCount, ref skippedCount, ref errorCount, ref declinedCount);
            }

            Console.WriteLine("Global Git alias configuration complete.");
            Console.WriteLine($"Summary: {configuredCount} configured/updated, {skippedCount} skipped (already correct), {declinedCount} declined, {errorCount} failed.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Prepares the dictionary of all aliases to be configured, including the dynamic 'cleanup' alias.
    /// </summary>
    private Dictionary<string, string>? GetAliasesToConfigure()
    {
        string? executablePath = Environment.ProcessPath;
        Dictionary<string, string> aliases = new(_standardAliases);

        if (!string.IsNullOrEmpty(executablePath))
        {
            string cleanupCommand = $"!\\\"{executablePath}\\\" git cleanup";
            aliases.Add("cleanup", cleanupCommand);
            Console.WriteLine($"[INFO] Detected executable path: {executablePath}");
            Console.WriteLine($"[INFO] Will propose 'git cleanup' alias for: {cleanupCommand.Substring(1).Replace("\\\"", "")}");
        }
        else
        {
            Console.WriteLine("[WARN] Could not determine the executable path. Skipping 'git cleanup' alias configuration.");
            return null;
        }
        return aliases;
    }

    /// <summary>
    /// Displays the list of aliases that will be proposed to the user.
    /// </summary>
    private void DisplayProposedAliases(Dictionary<string, string> aliases)
    {
        Console.WriteLine("\nInitializing global Git aliases (interactive)...");
        Console.WriteLine("This will check and offer to add/update the following aliases in your global Git configuration:\n");
        foreach (var alias in aliases)
        {
            string displayCommand = alias.Value.StartsWith('!') ? alias.Value.Substring(1).Replace("\\\"", "") : $"git {alias.Value}";
            string displayKey = alias.Value.StartsWith('!') ? alias.Key : $"git {alias.Key}";
            Console.WriteLine($"  {displayKey} -> {displayCommand}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Processes a single alias: checks current config, prompts user, and configures if accepted.
    /// </summary>
    private void ProcessSingleAlias(string aliasName, string aliasCommand,
        ref int configuredCount, ref int skippedCount, ref int errorCount, ref int declinedCount)
    {
        string configKey = $"alias.{aliasName}";

        string checkArgs = $"config --global --get {configKey}";
        (string existingValue, int checkExitCode) = RunGitCommand(checkArgs);

        string expectedValue = aliasCommand.StartsWith('!')
            ? aliasCommand.Replace("\\\"", "\"")
            : aliasCommand;

        bool isCorrectlyConfigured = checkExitCode == 0 && !string.IsNullOrWhiteSpace(existingValue) && existingValue.Trim() == expectedValue;

        if (isCorrectlyConfigured)
        {
            Console.WriteLine($"[SKIP] Alias '{aliasName}' already configured correctly.");
            skippedCount++;
        }
        else
        {
            string action = (checkExitCode == 0 && !string.IsNullOrWhiteSpace(existingValue)) ? "Update" : "Add";
            string displayCommandPrompt = aliasCommand.StartsWith('!') ? aliasCommand.Substring(1).Replace("\\\"", "") : aliasCommand;
            Console.Write($"[{action}] Alias '{aliasName}' -> '{displayCommandPrompt}'? [Y/n]: ");
            string? response = Console.ReadLine();

            if (response != null && response.Trim().Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[DECLINED] Skipping alias '{aliasName}'.");
                declinedCount++;
            }
            else
            {
                ConfigureOrUpdateAlias(aliasName, aliasCommand, configKey, ref configuredCount, ref errorCount);
            }
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Executes the git config command to set or replace an alias.
    /// </summary>
    private void ConfigureOrUpdateAlias(string aliasName, string aliasCommand, string configKey,
        ref int configuredCount, ref int errorCount)
    {
        bool requiresShell = aliasCommand.StartsWith('!');
        string commandOrArgsToExecute;
        string configArgsBase = $"config --global --replace-all {configKey}";

        if (requiresShell)
        {
            string aliasValueArgument = $"'{aliasCommand}'";
            commandOrArgsToExecute = $"git {configArgsBase} {aliasValueArgument}";
        }
        else
        {
            commandOrArgsToExecute = $"{configArgsBase} \"{aliasCommand}\"";
        }

        (string output, int exitCode) = RunGitCommand(commandOrArgsToExecute, useShellExecute: requiresShell);

        if (exitCode == 0)
        {
            Console.WriteLine($"[OK] Successfully configured alias '{aliasName}'.");
            configuredCount++;
        }
        else
        {
            Console.WriteLine($"[FAIL] Failed to configure alias '{aliasName}'. Exit code: {exitCode}");
            if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine($"       Git Error: {output}");
            errorCount++;
        }
    }

    /// <summary>
    /// Helper method to run a Git command and capture its output and exit code.
    /// </summary>
    private static (string Output, int ExitCode) RunGitCommand(string commandOrArgs, bool useShellExecute = false)
    {
        try
        {
            ProcessStartInfo startInfo;
            string processFileName;
            string processArgs;

            if (useShellExecute)
            {
                processFileName = "/bin/sh";
                string commandForShell = $"\"{commandOrArgs}\"";
                processArgs = $"-c {commandForShell}";
            }
            else
            {
                processFileName = "git";
                processArgs = commandOrArgs;
            }

            startInfo = new ProcessStartInfo(processFileName, processArgs)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            using (Process? process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    return ("Failed to start process.", -1);
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                string combinedOutput = (process.ExitCode != 0)
                    ? $"{output}{Environment.NewLine}{error}".Trim()
                    : output.Trim();

                return (combinedOutput, process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while trying to run command: {ex.Message}");
            return ($"Exception: {ex.Message}", -1);
        }
    }
}
