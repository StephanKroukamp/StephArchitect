using System.Diagnostics;

namespace TFive;

public static class DotnetCli
{
    public static void RestoreNugetPackages(string baseOutputPath)
    {
        var restoreCommand = $"restore \"{baseOutputPath}\" --verbosity quiet";

        ExecuteDotnetCommand(restoreCommand, baseOutputPath);
    }
    
    public static void AddNewMigration(string projectName, string baseOutputPath)
    {
        var migrationCommand =
            $"ef migrations add --project {projectName}.Persistence\\{projectName}.Persistence.csproj " +
            $"--startup-project {projectName}.Api\\{projectName}.Api.csproj " +
            $"--context {projectName}.Persistence.{projectName}DbContext " +
            $"--configuration Debug initialize_migrations --output-dir Migrations";


        ExecuteDotnetCommand(migrationCommand, baseOutputPath);
    }

    public static void ApplyMigration(string projectName, string baseOutputPath)
    {
        var updateCommand =
            $"ef database update --project {projectName}.Persistence\\{projectName}.Persistence.csproj " +
            $"--startup-project {projectName}.Api\\{projectName}.Api.csproj " +
            $"--context {projectName}.Persistence.{projectName}DbContext " +
            $"--configuration Debug initialize_migrations";

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

        if (process is null)
        {
            Console.WriteLine("Failed to start process.");
            
            return;
        }

        var output = process.StandardOutput.ReadToEnd();
        
        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine($"Output: {output}");
        }

        var error = process.StandardError.ReadToEnd();
        
        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine($"Error: {error}");
        }

        process.WaitForExit();
    }
}