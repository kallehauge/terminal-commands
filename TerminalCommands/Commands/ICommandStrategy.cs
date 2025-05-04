using System.CommandLine;
using System.CommandLine.Invocation;

namespace TerminalCommands.Commands;

public interface ICommandStrategy
{
    void ConfigureCommand(Command command);
    Task ExecuteAsync(InvocationContext context);
}
