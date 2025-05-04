using System.CommandLine;
using System.CommandLine.Invocation;

namespace TerminalCommands.Commands;

/// <summary>
/// Defines the contract for a command strategy, responsible for configuring
/// a System.CommandLine.Command and handling its execution.
/// </summary>
public interface ICommandStrategy
{
    /// <summary>
    /// Configures the command, adding arguments, options, etc.
    /// </summary>
    /// <param name="command">The command to configure.</param>
    void ConfigureCommand(Command command);

    /// <summary>
    /// Executes the command's logic using the provided invocation context.
    /// </summary>
    /// <param name="context">The invocation context containing parsed arguments and options.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(InvocationContext context);
}
