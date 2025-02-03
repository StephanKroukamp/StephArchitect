using System.Diagnostics;
using Mono.TextTemplating;
using Newtonsoft.Json;
using DiffMatchPatch;

namespace StephArchitect;

public class ProjectGenerator(string projectName)
{
    private readonly string _templateDirectory = SetTemplateDirectory();

    private string BaseOutputPath { get; } = @$"C:\\Projects\\{projectName}";

    private Manifest? Manifest { get; set; }

    private List<Entity> _entities = [];
    private List<Relationship> _relationships = [];

    public async Task GenerateFromJson(string jsonFilePath)
    {
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);

        var input = JsonConvert.DeserializeObject<Input>(jsonContent) ??
                    throw new Exception("No entities found in input.");

        _entities = input.Entities;
        _relationships = input.Relationships;

        Manifest = await LoadManifestFiles();

        CreateBaseDirectoryStructure();

        await Task.WhenAll([
            GenerateDomainLayer(),
            GenerateContractsLayer(),
            GenerateApplicationLayer(),
            GenerateApiLayer(),
            GeneratePersistenceLayer(),
            GenerateInfrastructureLayer(),
            GenerateProgramFile(),
            GenerateSolutionFile(),
            GenerateGlobalJsonFile(),
            GenerateReadmeFile()
        ]);

        await PersistManifestFiles();

        PrintGenerationSummary();

        RestoreNugetPackages();
    }

    private void RestoreNugetPackages()
    {
        var objPi = new ProcessStartInfo("dotnet", $"restore \"{BaseOutputPath}\" --verbosity quiet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        var objProcess = Process.Start(objPi);

        if (objProcess is null)
        {
            return;
        }

        var error = objProcess.StandardError.ReadToEnd();

        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine($"Error: {error}");
        }

        var output = objProcess.StandardOutput.ReadToEnd();

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine($"Output: {output}");
        }

        objProcess.WaitForExit();
    }

    private async Task<Manifest?> LoadManifestFiles()
    {
        var path = Path.Combine(BaseOutputPath, "manifest.json");

        if (!File.Exists(path))
        {
            return null;
        }

        var manifestFile = await File.ReadAllTextAsync(path);

        return JsonConvert.DeserializeObject<Manifest>(manifestFile);
    }

    private async Task PersistManifestFiles()
    {
        Manifest ??= new Manifest
        {
            Files = Directory
                .GetFiles(BaseOutputPath, "*", SearchOption.AllDirectories)
                .Select(path => new ManifestFile(path, ComputeFileHash(File.ReadAllText(path)), DateTimeOffset.Now))
                .ToList()
        };

        await File.WriteAllTextAsync(Path.Combine(BaseOutputPath, "manifest.json"),
            JsonConvert.SerializeObject(Manifest, Formatting.Indented));
    }

    private static string ComputeFileHash(string fileContent)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();

        var hashBytes =
            sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fileContent));

        return BitConverter
            .ToString(hashBytes)
            .Replace("-", "")
            .ToLower();
    }

    private async Task GenerateReadmeFile() =>
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "ReadMeTemplate.tt"),
            Path.Combine(BaseOutputPath, "Readme.md"),
            new Dictionary<string, object> { { "ProjectName", projectName } });

    private Task GenerateGlobalJsonFile() =>
        GenerateTemplate(
            Path.Combine(_templateDirectory, "GlobalJsonFileTemplate.tt"),
            Path.Combine(BaseOutputPath, "global.json"));

    private void CreateBaseDirectoryStructure()
    {
        var directories = new List<string>
        {
            BaseOutputPath,
            Path.Combine(BaseOutputPath, $"{projectName}.Domain"),
            Path.Combine(BaseOutputPath, $"{projectName}.Contracts"),
            Path.Combine(BaseOutputPath, $"{projectName}.Application"),
            Path.Combine(BaseOutputPath, $"{projectName}.Infrastructure"),
            Path.Combine(BaseOutputPath, $"{projectName}.Persistence"),
            Path.Combine(BaseOutputPath, $"{projectName}.Api"),
            Path.Combine(BaseOutputPath, $"{projectName}.Api", "Properties")
        };

        directories.AddRange(_entities.Select(entity =>
            Path.Combine(BaseOutputPath, $"{projectName}.Domain", entity.Name.Pluralize())));

        directories.AddRange(_entities.Select(entity =>
            Path.Combine(BaseOutputPath, $"{projectName}.Contracts", entity.Name.Pluralize())));

        directories.AddRange(_entities.Select(entity =>
            Path.Combine(BaseOutputPath, $"{projectName}.Application", entity.Name.Pluralize(), "Commands")));
        directories.AddRange(_entities.Select(entity =>
            Path.Combine(BaseOutputPath, $"{projectName}.Application", entity.Name.Pluralize(), "Queries")));

        directories.AddRange(_entities.Select(entity =>
            Path.Combine(BaseOutputPath, $"{projectName}.Infrastructure", entity.Name.Pluralize())));

        directories.AddRange(_entities.Select(entity =>
            Path.Combine(BaseOutputPath, $"{projectName}.Persistence", entity.Name.Pluralize())));

        directories.AddRange(_entities.Select(entity =>
            Path.Combine(BaseOutputPath, $"{projectName}.Api", entity.Name.Pluralize())));

        foreach (var dir in directories.Where(dir => !Directory.Exists(dir)))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private async Task GenerateDomainLayer()
    {
        foreach (var entity in _entities)
        {
            var path = Path.Combine(BaseOutputPath, $"{projectName}.Domain", entity.Name.Pluralize());

            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Domain", "EntityTemplate.tt"),
                Path.Combine(path, $"{entity.Name}.cs"),
                new Dictionary<string, object>
                    { { "ProjectName", projectName }, { "Entity", entity }, { "Relationships", _relationships } });
        }

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Domain", "DomainCsprojFileTemplate.tt"),
            Path.Combine(BaseOutputPath, $"{projectName}.Domain", $"{projectName}.Domain.csproj"));
    }

    private async Task GenerateContractsLayer()
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Contracts", "ContractsCsprojFileTemplate.tt"),
            Path.Combine(BaseOutputPath, $"{projectName}.Contracts", $"{projectName}.Contracts.csproj"));
    }

    private static string SetTemplateDirectory() =>
        Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!
            .Parent!.Parent!.Parent!.FullName, "Templates");

    private async Task GenerateApplicationLayer()
    {
        var path = Path.Combine(BaseOutputPath, $"{projectName}.Application");

        foreach (var entity in _entities)
        {
            await GenerateCommandsAndQueries(path, entity);
        }

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Application", "ApplicationCsprojFileTemplate.tt"),
            Path.Combine(path, $"{projectName}.Application.csproj"),
            new Dictionary<string, object> { { "ProjectName", projectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Application", "ApplicationDependencyInjectionFileTemplate.tt"),
            Path.Combine(path, "DependencyInjection.cs"),
            new Dictionary<string, object> { { "ProjectName", projectName } });
    }

    private async Task GenerateCommandsAndQueries(string path, Entity entity)
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Application", "CreateCommandTemplate.tt"),
            Path.Combine(path, entity.Name.Pluralize(), "Commands", $"Create{entity.Name}Command.cs"),
            new Dictionary<string, object> { { "ProjectName", projectName }, { "Entity", entity } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Application", "GetByIdQueryTemplate.tt"),
            Path.Combine(path, entity.Name.Pluralize(), "Queries", $"Get{entity.Name}ByIdQuery.cs"),
            new Dictionary<string, object> { { "ProjectName", projectName }, { "Entity", entity } });
    }

    private async Task GenerateApiLayer()
    {
        var path = Path.Combine(BaseOutputPath, $"{projectName}.Api");

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "ApiCsprojFileTemplate.tt"),
            Path.Combine(BaseOutputPath, $"{projectName}.Api", $"{projectName}.Api.csproj"),
            new Dictionary<string, object> { { "ProjectName", projectName } });

        foreach (var entity in _entities)
        {
            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Api", "ApiTemplate.tt"),
                Path.Combine(path, entity.Name.Pluralize(), $"{entity.Name}Endpoints.cs"),
                new Dictionary<string, object>
                {
                    { "ProjectName", projectName }, { "EntityName", entity.Name },
                    { "PluralEntityName", entity.Name.Pluralize() }
                });
        }

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "AppSettingsTemplate.tt"),
            Path.Combine(path, "appsettings.json"),
            new Dictionary<string, object> { { "ProjectName", projectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "AppSettingsTemplate.tt"),
            Path.Combine(path, "appsettings.Development.json"),
            new Dictionary<string, object> { { "ProjectName", projectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "LaunchSettingsFileTemplate.tt"),
            Path.Combine(path, "Properties", "launchSettings.json"),
            new Dictionary<string, object> { { "ProjectName", projectName } });
    }

    private async Task GeneratePersistenceLayer()
    {
        var path = Path.Combine(BaseOutputPath, $"{projectName}.Persistence");

        foreach (var entity in _entities)
        {
            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Persistence", "EntityConfigurationTemplate.tt"),
                Path.Combine(path, entity.Name.Pluralize(), $"{entity.Name}EntityConfiguration.cs"),
                new Dictionary<string, object>
                    { { "ProjectName", projectName }, { "Entity", entity }, { "Relationships", _relationships } });
        }

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Persistence", "PersistenceCsprojFileTemplate.tt"),
            Path.Combine(path, $"{projectName}.Persistence.csproj"),
            new Dictionary<string, object> { { "ProjectName", projectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Persistence", "DbContextTemplate.tt"),
            Path.Combine(path, $"{projectName}DbContext.cs"),
            new Dictionary<string, object> { { "ProjectName", projectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Persistence", "PersistenceDependencyInjectionFileTemplate.tt"),
            Path.Combine(path, "DependencyInjection.cs"),
            new Dictionary<string, object> { { "ProjectName", projectName } });
    }

    private async Task GenerateInfrastructureLayer()
    {
        var path = Path.Combine(BaseOutputPath, $"{projectName}.Infrastructure");

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Infrastructure", "InfrastructureCsprojFileTemplate.tt"),
            Path.Combine(path, $"{projectName}.Infrastructure.csproj"),
            new Dictionary<string, object> { { "ProjectName", projectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Infrastructure", "InfrastructureDependencyInjectionFileTemplate.tt"),
            Path.Combine(path, "DependencyInjection.cs"),
            new Dictionary<string, object> { { "ProjectName", projectName } });
    }

    private async Task GenerateProgramFile()
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "ProgramTemplate.tt"),
            Path.Combine(BaseOutputPath, $"{projectName}.Api", "Program.cs"),
            new Dictionary<string, object> { { "ProjectName", projectName } });
    }

    private async Task GenerateSolutionFile()
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "SolutionFileTemplate.tt"),
            Path.Combine(BaseOutputPath, $"{projectName}.sln"),
            new Dictionary<string, object> { { "ProjectName", projectName }, { "BaseOutputPath", BaseOutputPath } });
    }

    private async Task GenerateTemplate(string templatePath, string outputPath,
        Dictionary<string, object>? sessionValues = null)
    {
        var templateGenerator = new TemplateGenerator();

        var session = templateGenerator.GetOrCreateSession();

        try
        {
            var oldHash = string.Empty;

            if (Manifest is not null)
            {
                oldHash = Manifest.Files
                    .FirstOrDefault(x => x.Path == outputPath)
                    ?.Hash;
            }


            if (sessionValues != null)
            {
                foreach (var (key, value) in sessionValues)
                {
                    session.Add(key, value);
                }
            }

            var templateContent = await File.ReadAllTextAsync(templatePath);
            var parsed = templateGenerator.ParseTemplate(templatePath, templateContent);
            var settings = TemplatingEngine.GetSettings(templateGenerator, parsed);
            settings.CompilerOptions = "-nullable:enable";

            var (generatedFilename, newContent) = await templateGenerator.ProcessTemplateAsync(
                parsed, templatePath, "", outputPath, settings
            );

            if (templateGenerator.Errors.HasErrors)
            {
                foreach (var error in templateGenerator.Errors)
                {
                    Console.WriteLine($"Error: {error}");
                }

                throw new Exception("Failed to process the T4 template.");
            }

            if (!string.IsNullOrWhiteSpace(oldHash))
            {
                var newHash = ComputeFileHash(newContent);

                if (oldHash == newHash)
                {
                    return;
                }

                var oldContent = await File.ReadAllTextAsync(outputPath);

                if (outputPath.EndsWith(".sln"))
                {
                    // TODO: since the solution uses guids for the project, we need to track this in the manifest file somehow
                    return;
                }

                PrintDifferencesBetweenFiles(oldContent, newContent);

                Manifest!.Files
                    .First(x => x.Path == outputPath)
                    .Hash = newHash;
            }

            await File.WriteAllTextAsync(generatedFilename, newContent);
        }
        finally
        {
            session.Clear();
        }
    }

    private static void PrintDifferencesBetweenFiles(string text1, string text2)
    {
        var lines1 = text1.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var lines2 = text2.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        var dmp = new diff_match_patch();
        var diffs = dmp.diff_main(text1, text2);
        dmp.diff_cleanupSemantic(diffs);

        var removedLines = new HashSet<int>();
        var addedLines = new HashSet<int>();
        var modifiedLines = new Dictionary<int, (string, string)>();

        int lineIndex1 = 0, lineIndex2 = 0;
        foreach (var diff in diffs)
        {
            switch (diff.operation)
            {
                case Operation.DELETE:
                    removedLines.Add(lineIndex1++);
                    break;
                case Operation.INSERT:
                    addedLines.Add(lineIndex2++);
                    break;
                case Operation.EQUAL:
                    lineIndex1++;
                    lineIndex2++;
                    break;
            }
        }

        var maxLines = Math.Max(lines1.Length, lines2.Length);
        Console.WriteLine("Changes:");

        for (var i = 0; i < maxLines; i++)
        {
            var line1 = i < lines1.Length ? lines1[i] : string.Empty;
            var line2 = i < lines2.Length ? lines2[i] : string.Empty;

            if (removedLines.Contains(i) && !addedLines.Contains(i))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"- {line1}");
            }
            else if (addedLines.Contains(i) && !removedLines.Contains(i))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"+ {line2}");
            }
            else if (line1 != line2)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"~ {HighlightChanges(line1, line2)}");
            }
        }

        Console.ResetColor();
    }

    private static string HighlightChanges(string oldLine, string newLine)
    {
        var dmp = new diff_match_patch();
        var diffs = dmp.diff_main(oldLine, newLine);
        dmp.diff_cleanupSemantic(diffs);

        var result = "";
        foreach (var diff in diffs)
        {
            switch (diff.operation)
            {
                case Operation.DELETE:
                    result += $"\x1b[31m{diff.text}\x1b[0m"; // Red for removed text
                    break;
                case Operation.INSERT:
                    result += $"\x1b[32m{diff.text}\x1b[0m"; // Green for added text
                    break;
                case Operation.EQUAL:
                    result += diff.text;
                    break;
            }
        }

        return result;
    }

    private void PrintGenerationSummary()
    {
        Console.WriteLine($"Project generated successfully at: {BaseOutputPath}");

        PrintDirectoryTree(BaseOutputPath, string.Empty);
    }

    private static void PrintDirectoryTree(string directory, string indent)
    {
        var dirInfo = new DirectoryInfo(directory);

        List<string> ignoredDirectories = ["obj", "bin", ".idea"];

        if (ignoredDirectories.Contains(dirInfo.Name, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        Console.WriteLine($"{indent}{dirInfo.Name}/");

        foreach (var subDir in dirInfo.GetDirectories())
        {
            PrintDirectoryTree(subDir.FullName, indent + "  ");
        }

        List<string> ignoredFiles = [".dll", ".pdb", ".nuspec", "project.assets.json"];

        foreach (var file in dirInfo.GetFiles())
        {
            if (ignoredFiles.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            Console.WriteLine($"{indent}  {file.Name}");
        }
    }
}