using System.Text.RegularExpressions;
using Mono.TextTemplating;
using Newtonsoft.Json;
using DiffMatchPatch;

namespace TFive;

public class ApiProjectGenerator(string baseOutputPath, Input Input)
{
    private readonly string _templateDirectory = SetTemplateDirectory();

    private Manifest? Manifest { get; set; }

    public async Task GenerateFromInput()
    {

        Manifest = await LoadManifestFiles();

        CreateBaseDirectoryStructure();

        await Task.WhenAll(
            GenerateDomainLayer(),
            GenerateContractsLayer(),
            GenerateApplicationLayer(),
            GenerateApiLayer(),
            GeneratePersistenceLayer(),
            GenerateInfrastructureLayer(),
            GenerateProgramFile(),
            GenerateSolutionFile(),
            GenerateGlobalJsonFile(),
            GenerateReadmeFile());

        Manifest ??= new Manifest
        {
            Files = Directory
                .GetFiles(baseOutputPath, "*", SearchOption.AllDirectories)
                .ToList()
        };

        await File.WriteAllTextAsync(Path.Combine(baseOutputPath, "manifest.json"),
            JsonConvert.SerializeObject(Manifest, Formatting.Indented));

        PrintGenerationSummary();

        DotnetCli.RestoreNugetPackages(baseOutputPath);

        // DotnetCli.AddNewMigration(Input.ProjectName, baseOutputPath);
        // DotnetCli.ApplyMigration(Input.ProjectName, baseOutputPath);
    }

    private async Task<Manifest?> LoadManifestFiles()
    {
        var path = Path.Combine(baseOutputPath, "manifest.json");

        if (!File.Exists(path))
        {
            return null;
        }

        var manifestFile = await File.ReadAllTextAsync(path);

        return JsonConvert.DeserializeObject<Manifest>(manifestFile);
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
            Path.Combine(_templateDirectory, "ReadMe.tt"),
            Path.Combine(baseOutputPath, "Readme.md"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

    private Task GenerateGlobalJsonFile() =>
        GenerateTemplate(
            Path.Combine(_templateDirectory, "GlobalJson.tt"),
            Path.Combine(baseOutputPath, "global.json"));

    private void CreateBaseDirectoryStructure()
    {
        var directories = new List<string>
        {
            baseOutputPath,
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Domain"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Contracts"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Application"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Infrastructure"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Persistence"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Api"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Api", "Properties")
        };

        directories.AddRange(Input.Entities.Select(entity =>
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Domain", entity.Name.Pluralize())));

        directories.AddRange(Input.Entities.Select(entity =>
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Contracts", entity.Name.Pluralize())));

        directories.AddRange(Input.Entities.Select(entity =>
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Application", entity.Name.Pluralize(), "Commands")));
        directories.AddRange(Input.Entities.Select(entity =>
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Application", entity.Name.Pluralize(), "Queries")));

        directories.AddRange(Input.Entities.Select(entity =>
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Infrastructure", entity.Name.Pluralize())));

        directories.AddRange(Input.Entities.Select(entity =>
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Persistence", entity.Name.Pluralize())));

        directories.AddRange(Input.Entities.Select(entity =>
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Api", entity.Name.Pluralize())));

        foreach (var dir in directories.Where(dir => !Directory.Exists(dir)))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private async Task GenerateDomainLayer()
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Domain", "IEntity.tt"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Domain", "IEntity.cs"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        foreach (var entity in Input.Entities)
        {
            var path = Path.Combine(baseOutputPath, $"{Input.ProjectName}.Domain", entity.Name.Pluralize());

            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Domain", "Entity.tt"),
                Path.Combine(path, $"{entity.Name}.cs"),
                new Dictionary<string, object>
                    { { "ProjectName", Input.ProjectName }, { "Entity", entity }, { "Relationships", Input.Relationships } });
        }

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Domain", "CsProj.tt"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Domain", $"{Input.ProjectName}.Domain.csproj"));
    }

    private async Task GenerateContractsLayer()
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Contracts", "Csproj.tt"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Contracts", $"{Input.ProjectName}.Contracts.csproj"));
    }

    private static string SetTemplateDirectory() =>
        Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!
            .Parent!.Parent!.Parent!.FullName, "Templates", "Api");

    private async Task GenerateApplicationLayer()
    {
        var path = Path.Combine(baseOutputPath, $"{Input.ProjectName}.Application");

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Application", "IRepository.tt"),
            Path.Combine(path, "IRepositoryTemplate.cs"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        foreach (var entity in Input.Entities)
        {
            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Application", "IEntityRepository.tt"),
                Path.Combine(path, entity.Name.Pluralize(), $"I{entity.Name}Repository.cs"),
                new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "Entity", entity } });

            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Application", "Commands", "CreateEntityCommand.tt"),
                Path.Combine(path, entity.Name.Pluralize(), "Commands", $"Create{entity.Name}Command.cs"),
                new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "Entity", entity } });

            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Application", "Commands", "DeleteEntityByIdCommand.tt"),
                Path.Combine(path, entity.Name.Pluralize(), "Commands", $"Delete{entity.Name}ByIdCommand.cs"),
                new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "Entity", entity } });

            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Application", "Commands", "UpdateEntityCommand.tt"),
                Path.Combine(path, entity.Name.Pluralize(), "Commands", $"Update{entity.Name}Command.cs"),
                new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "Entity", entity } });

            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Application", "Queries", "GetEntitiesQuery.tt"),
                Path.Combine(path, entity.Name.Pluralize(), "Queries", $"Get{entity.Name.Pluralize()}Query.cs"),
                new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "Entity", entity } });

            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Application", "Queries", "GetEntityByIdQuery.tt"),
                Path.Combine(path, entity.Name.Pluralize(), "Queries", $"Get{entity.Name}ByIdQuery.cs"),
                new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "Entity", entity } });
        }

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Application", "Csproj.tt"),
            Path.Combine(path, $"{Input.ProjectName}.Application.csproj"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Application", "DependencyInjection.tt"),
            Path.Combine(path, "DependencyInjection.cs"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });
    }

    private async Task GenerateApiLayer()
    {
        var path = Path.Combine(baseOutputPath, $"{Input.ProjectName}.Api");

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "Csproj.tt"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Api", $"{Input.ProjectName}.Api.csproj"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        foreach (var entity in Input.Entities)
        {
            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Api", "EntityEndpoints.tt"),
                Path.Combine(path, entity.Name.Pluralize(), $"{entity.Name}Endpoints.cs"),
                new Dictionary<string, object>
                {
                    { "ProjectName", Input.ProjectName }, { "EntityName", entity.Name },
                    { "PluralEntityName", entity.Name.Pluralize() }
                });
        }

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "Appsettings.tt"),
            Path.Combine(path, "appsettings.json"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "Appsettings.tt"),
            Path.Combine(path, "appsettings.Development.json"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "LaunchSettings.tt"),
            Path.Combine(path, "Properties", "launchSettings.json"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });
    }

    private async Task GeneratePersistenceLayer()
    {
        var path = Path.Combine(baseOutputPath, $"{Input.ProjectName}.Persistence");

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Persistence", "Repository.tt"),
            Path.Combine(path, "Repository.cs"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        foreach (var entity in Input.Entities)
        {
            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Persistence", "EntityConfiguration.tt"),
                Path.Combine(path, entity.Name.Pluralize(), $"{entity.Name}EntityConfiguration.cs"),
                new Dictionary<string, object>
                    { { "ProjectName", Input.ProjectName }, { "Entity", entity }, { "Relationships", Input.Relationships } });

            await GenerateTemplate(
                Path.Combine(_templateDirectory, "Persistence", "EntityRepository.tt"),
                Path.Combine(path, entity.Name.Pluralize(), $"{entity.Name}Repository.cs"),
                new Dictionary<string, object>
                    { { "ProjectName", Input.ProjectName }, { "Entity", entity } });
        }

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Persistence", "Csproj.tt"),
            Path.Combine(path, $"{Input.ProjectName}.Persistence.csproj"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Persistence", "DbContext.tt"),
            Path.Combine(path, $"{Input.ProjectName}DbContext.cs"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Persistence", "DependencyInjection.tt"),
            Path.Combine(path, "DependencyInjection.cs"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "Entities", Input.Entities } });
    }

    private async Task GenerateInfrastructureLayer()
    {
        var path = Path.Combine(baseOutputPath, $"{Input.ProjectName}.Infrastructure");

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Infrastructure", "Csproj.tt"),
            Path.Combine(path, $"{Input.ProjectName}.Infrastructure.csproj"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });

        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Infrastructure", "DependencyInjection.tt"),
            Path.Combine(path, "DependencyInjection.cs"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName } });
    }

    private async Task GenerateProgramFile()
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Api", "Program.tt"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.Api", "Program.cs"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "Entities", Input.Entities } });
    }

    private async Task GenerateSolutionFile()
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "Solution.tt"),
            Path.Combine(baseOutputPath, $"{Input.ProjectName}.sln"),
            new Dictionary<string, object> { { "ProjectName", Input.ProjectName }, { "BaseOutputPath", baseOutputPath } });
    }

    private async Task GenerateTemplate(string templatePath, string outputPath,
        Dictionary<string, object>? sessionValues = null)
    {
        var templateGenerator = new TemplateGenerator();

        var session = templateGenerator.GetOrCreateSession();

        try
        {
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

            var (newFilename, newCode) = await templateGenerator
                .ProcessTemplateAsync(parsed, templatePath, "", outputPath, settings);

            if (templateGenerator.Errors.HasErrors)
            {
                foreach (var error in templateGenerator.Errors)
                {
                    Console.WriteLine($"Error: {error}");
                }

                throw new Exception("Failed to process the T4 template.");
            }

            // at this point we have the newly generated code
            var oldFilePath = Manifest?.Files.FirstOrDefault(path => path == outputPath);

            // if the file was not created before, write the new file
            if (oldFilePath is null)
            {
                await File.WriteAllTextAsync(newFilename, newCode);

                return;
            }

            // TODO: since the solution uses guids for the project, we need to track this in the manifest file somehow
            if (outputPath.EndsWith(".sln"))
            {
                return;
            }

            // if the file was created before, check if the old and new hashes are the same
            var oldCode = await File.ReadAllTextAsync(oldFilePath);

            // if they are the same, no need to write new file
            if (ComputeFileHash(oldCode) == ComputeFileHash(newCode))
            {
                return;
            }

            // if there are no keep comments, write the new file
            if (!oldCode.Contains("Keep:"))
            {
                PrintDifferencesBetweenFiles(oldCode, newCode);

                await File.WriteAllTextAsync(newFilename, newCode);

                return;
            }

            // if there are keep comments, pull the keep code from the old code, merge back the code that has to be kept, write the new file
            var pattern =
                @"\s*//\s*Keep:\s*\r?\n\s*(public|private|protected|internal|static|\s)+\s*(async\s+)?\S+\s+\S+\s*\(.*?\)\s*\{.*?\}";

            var matches = Regex.Matches(oldCode, pattern, RegexOptions.Singleline)
                .Select(m => m.Value)
                .ToList();

            foreach (var match in matches)
            {
                newCode = Regex.Replace(newCode, pattern, match, RegexOptions.Singleline);
            }

            PrintDifferencesBetweenFiles(oldCode, newCode);

            await File.WriteAllTextAsync(newFilename, newCode);
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
        Console.WriteLine($"Project generated successfully at: {baseOutputPath}");

        PrintDirectoryTree(baseOutputPath, string.Empty);
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