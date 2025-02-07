using System.Diagnostics;

namespace StephArchitect;

public class EfMigrationHelper
{
    public static void RunEfMigrations(string projectName, string baseOutputPath)
    {
        string migrationCommand = 
            $"ef migrations add --project {projectName}.Persistence\\{projectName}.Persistence.csproj " +
            $"--startup-project {projectName}.Api\\{projectName}.Api.csproj " +
            $"--context {projectName}.Persistence.{projectName}DbContext " +
            $"--configuration Debug initialize_migrations --output-dir Migrations";

        string updateCommand = 
            $"ef database update --project {projectName}.Persistence\\{projectName}.Persistence.csproj " +
            $"--startup-project {projectName}.Api\\{projectName}.Api.csproj " +
            $"--context {projectName}.Persistence.{projectName}DbContext " +
            $"--configuration Debug initialize_migrations";

        ExecuteDotnetCommand(migrationCommand, baseOutputPath);
        ExecuteDotnetCommand(updateCommand, baseOutputPath);
    }

    private static void ExecuteDotnetCommand(string arguments, string workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(processStartInfo);

        if (process == null)
        {
            Console.WriteLine("Failed to start process.");
            return;
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine($"Output: {output}");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine($"Error: {error}");
        }

        process.WaitForExit();
    }
}