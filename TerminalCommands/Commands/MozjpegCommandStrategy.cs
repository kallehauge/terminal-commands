using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;

namespace TerminalCommands.Commands;

/// <summary>
/// Implements the strategy for mozjpeg-related commands.
/// Handles both the image optimization and update commands.
/// </summary>
public class MozjpegCommandStrategy : ICommandStrategy
{
    private static readonly string[] s_validExtensions = [".jpg", ".jpeg", ".png"];

    private readonly Option<int> _qualityOption = new(
        name: "--quality",
        description: "Quality of the output image (0-100; 5-95 is recommended).",
        getDefaultValue: () => 75);

    private readonly Argument<string> _inputFileArgument = new(
        name: "input",
        description: "Input image file path.");

    private readonly Argument<string> _outputFileArgument = new(
        name: "output",
        description: "Output image file path.");

    private readonly Option<bool> _updateOption = new(
        aliases: ["--update", "-u"],
        description: "Update the mozjpeg Docker image to the latest version.");

    /// <summary>
    /// Configures the command with its options and arguments.
    /// </summary>
    public void ConfigureCommand(Command command)
    {
        command.AddOption(_qualityOption);
        command.AddOption(_updateOption);
        command.AddArgument(_inputFileArgument);
        command.AddArgument(_outputFileArgument);
    }

    /// <summary>
    /// Validates that the file has a valid image extension.
    /// </summary>
    private static bool HasValidImageExtension(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return s_validExtensions.Contains(extension);
    }

    /// <summary>
    /// Executes the mozjpeg command logic.
    /// </summary>
    public async Task ExecuteAsync(InvocationContext context)
    {
        bool update = context.ParseResult.GetValueForOption(_updateOption);

        if (update)
        {
            await UpdateDockerImageAsync();
            return;
        }

        int quality = context.ParseResult.GetValueForOption(_qualityOption);
        string? inputFile = context.ParseResult.GetValueForArgument(_inputFileArgument);
        string? outputFile = context.ParseResult.GetValueForArgument(_outputFileArgument);

        if (string.IsNullOrEmpty(inputFile) || string.IsNullOrEmpty(outputFile))
        {
            Console.WriteLine("Error: Both input and output file paths are required.");
            context.ExitCode = 1;
            return;
        }

        if (!HasValidImageExtension(inputFile))
        {
            Console.WriteLine($"Error: Input file must have one of these extensions: {string.Join(", ", s_validExtensions)}");
            context.ExitCode = 1;
            return;
        }

        if (!HasValidImageExtension(outputFile))
        {
            Console.WriteLine($"Error: Output file must have one of these extensions: {string.Join(", ", s_validExtensions)}");
            context.ExitCode = 1;
            return;
        }

        await OptimizeImageAsync(inputFile, outputFile, quality, context);
    }

    /// <summary>
    /// Updates the mozjpeg Docker image to the latest version.
    /// </summary>
    private static async Task UpdateDockerImageAsync()
    {
        Console.WriteLine("Updating mozjpeg Docker image...");
        var startInfo = new ProcessStartInfo("docker", "pull kallehauge/mozjpeg:latest")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine("Error: Failed to start docker process.");
                return;
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Successfully updated mozjpeg Docker image.");
                if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output);
            }
            else
            {
                Console.WriteLine($"Error updating Docker image. Exit code: {process.ExitCode}");
                if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine($"Docker Error: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while updating Docker image: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the mozjpeg Docker image exists locally.
    /// </summary>
    private static bool ImageExists()
    {
        var startInfo = new ProcessStartInfo("docker", "images kallehauge/mozjpeg:latest --format {{.Repository}}:{{.Tag}}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Optimizes an image using mozjpeg.
    /// </summary>
    private static async Task OptimizeImageAsync(string inputFile, string outputFile, int quality, InvocationContext context)
    {
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: Input file '{inputFile}' does not exist.");
            context.ExitCode = 1;
            return;
        }

        // Check if Docker image exists, if not, update it
        if (!ImageExists())
        {
            Console.WriteLine("kallehauge/mozjpeg Docker image not found. Updating...");
            await UpdateDockerImageAsync();
        }

        // Ensure the output directory exists
        string? outputDir = Path.GetDirectoryName(outputFile);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var startInfo = new ProcessStartInfo("/bin/sh", $"-c \"docker run -v .:/data kallehauge/mozjpeg -optimize -progressive -quality {quality} {inputFile} > {outputFile}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine("Error: Failed to start docker process.");
                context.ExitCode = 1;
                return;
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                Console.WriteLine($"Successfully optimized image: {outputFile}");
                if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output);
            }
            else
            {
                Console.WriteLine($"Error optimizing image. Exit code: {process.ExitCode}");
                if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine($"Docker Error: {error}");
                context.ExitCode = process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while optimizing image: {ex.Message}");
            context.ExitCode = 1;
        }
    }
}
