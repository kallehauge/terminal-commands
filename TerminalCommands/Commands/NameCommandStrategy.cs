using System.CommandLine;
using System.CommandLine.Invocation;

namespace TerminalCommands.Commands;

public class NameCommandStrategy : ICommandStrategy
{
    // Define the argument within the strategy
    private readonly Argument<string> _nameArgument = new Argument<string>(
        name: "name",
        description: "The name to display.");

    // Implement the configuration method
    public void ConfigureCommand(Command command)
    {
        command.AddArgument(_nameArgument);
    }

    public Task ExecuteAsync(InvocationContext context)
    {
        // Get the value of the argument from the invocation context
        var name = context.ParseResult.GetValueForArgument(_nameArgument);
        // System.CommandLine handles the error if it's missing.
        var currentDate = DateTime.Now;
        Console.WriteLine($"{Environment.NewLine}Hello, {name}, on {currentDate:d} at {currentDate:t}!");
        return Task.CompletedTask;
    }
}
