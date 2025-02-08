using System.Diagnostics;
using System.Text.RegularExpressions;
using Mono.TextTemplating;
using Newtonsoft.Json;
using DiffMatchPatch;

namespace TFive;

public class FrontendProjectGenerator(string projectName, string baseOutputPath, string jsonFilePath)
{
    private readonly string _templateDirectory = SetTemplateDirectory();

    private Manifest? Manifest { get; set; }

    private List<Entity> _entities = [];
    private List<Relationship> _relationships = [];

    public async Task GenerateFromInput()
    {
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);

        var input = JsonConvert.DeserializeObject<Input>(jsonContent) ??
                    throw new Exception("No entities found in input.");

        _entities = input.Entities;
        _relationships = input.Relationships;

        Manifest = await LoadManifestFiles();

        CreateBaseStructure();

        await Task.WhenAll(
            GenerateBaseFiles(),
            GenerateInterfaces()
            );
            // GenerateDomainLayer(),
            // GenerateContractsLayer(),
            // GenerateApplicationLayer(),
            // GenerateApiLayer(),
            // GeneratePersistenceLayer(),
            // GenerateInfrastructureLayer(),
            // GenerateProgramFile(),
            // GenerateSolutionFile(),
            // GenerateGlobalJsonFile(),
            // GenerateReadmeFile());

        Manifest ??= new Manifest
        {
            Files = Directory
                .GetFiles(baseOutputPath, "*", SearchOption.AllDirectories)
                .ToList()
        };

        await File.WriteAllTextAsync(Path.Combine(baseOutputPath, "manifest.json"),
            JsonConvert.SerializeObject(Manifest, Formatting.Indented));

        PrintGenerationSummary();
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

    private void CreateBaseStructure()
    {
        var directories = new List<string>
        {
            baseOutputPath,
            Path.Combine(baseOutputPath, "src"),
            Path.Combine(baseOutputPath, "src", "app"),
            Path.Combine(baseOutputPath, "src", "assets"),
            Path.Combine(baseOutputPath, "src", "app", "shared"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "components"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "core", "interfaces"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "core", "repositories"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "core", "services")
        };

        foreach (var entity in _entities)
        {
            directories.Add(Path.Combine(baseOutputPath, "src", "app", "shared", "core", "interfaces", entity.Name.ToLower()));
        }

        foreach (var dir in directories.Where(dir => !Directory.Exists(dir)))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private async Task GenerateBaseFiles()
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "angular.tt"),
            Path.Combine(baseOutputPath, "angular.json"),
            new Dictionary<string, object>
                { { "ProjectName", projectName }});
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "package.tt"),
            Path.Combine(baseOutputPath, "package.json"),
            new Dictionary<string, object>
                { { "ProjectName", projectName }});
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "README.tt"),
            Path.Combine(baseOutputPath, "README.md"));
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "tsconfig.tt"),
            Path.Combine(baseOutputPath, "tsconfig.json"));
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "tsconfig.app.tt"),
            Path.Combine(baseOutputPath, "tsconfig.app.json"));
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "index.tt"),
            Path.Combine(baseOutputPath, "src", "index.html"),
            new Dictionary<string, object>
                { { "ProjectName", projectName }});
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "main.tt"),
            Path.Combine(baseOutputPath, "src", "main.ts"));
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "styles.tt"),
            Path.Combine(baseOutputPath, "src", "styles.scss"));
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "app.component-html.tt"),
            Path.Combine(baseOutputPath, "src", "app", "app.component.html"),
            new Dictionary<string, object>
                { { "ProjectName", projectName }});
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "app.component-html.tt"),
            Path.Combine(baseOutputPath, "src", "app", "app.component.html"));
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "app.component-scss.tt"),
            Path.Combine(baseOutputPath, "src", "app", "app.component.scss"));
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "app.component-ts.tt"),
            Path.Combine(baseOutputPath, "src", "app", "app.component.ts"),
            new Dictionary<string, object>
                { { "ProjectName", projectName }});
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "app.config.tt"),
            Path.Combine(baseOutputPath, "src", "app", "app.config.ts"));
        
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "app.routes.tt"),
            Path.Combine(baseOutputPath, "src", "app", "app.routes.ts"));
    }

    private async Task GenerateInterfaces()
    {
        foreach (var entity in _entities)
        {
            await GenerateCreateCommand(entity);
            await GenerateDeleteCommand(entity);
            await GenerateUpdateCommand(entity);
            await GenerateGetByIdCommand(entity);
            await GenerateGetQuery(entity);
        }
    }

    private async Task GenerateCreateCommand(Entity entity)
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "shared", "core", "interfaces", "interface.tt"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "core", "interfaces", entity.Name.ToLower(), $"create-{entity.Name.ToLower()}-command.interface.ts"),
            new Dictionary<string, object>
            {
                { "InterfaceName", $"Create{StringExtensions.ToTitleCase(entity.Name)}Command" },
                { "InterfaceProperties", entity.Properties.Where(x => x.Name.ToLower() != "id" ).ToList() }
            });
    }
    
    private async Task GenerateDeleteCommand(Entity entity)
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "shared", "core", "interfaces", "interface.tt"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "core", "interfaces", entity.Name.ToLower(), $"delete-{entity.Name.ToLower()}-command.interface.ts"),
            new Dictionary<string, object>
            {
                { "InterfaceName", $"Create{StringExtensions.ToTitleCase(entity.Name)}Command" },
                { "InterfaceProperties", entity.Properties.Where(x => x.Name.ToLower() == "id" ).ToList() }
            });
    }
    
    private async Task GenerateUpdateCommand(Entity entity)
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "shared", "core", "interfaces", "interface.tt"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "core", "interfaces", entity.Name.ToLower(), $"update-{entity.Name.ToLower()}-command.interface.ts"),
            new Dictionary<string, object>
            {
                { "InterfaceName", $"Create{StringExtensions.ToTitleCase(entity.Name)}Command" },
                { "InterfaceProperties", entity.Properties }
            });
    }
    
    private async Task GenerateGetByIdCommand(Entity entity)
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "shared", "core", "interfaces", "interface.tt"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "core", "interfaces", entity.Name.ToLower(), $"get-{entity.Name.ToLower()}-by-id-query.interface.ts"),
            new Dictionary<string, object>
            {
                { "InterfaceName", $"Create{StringExtensions.ToTitleCase(entity.Name)}Command" },
                { "InterfaceProperties", entity.Properties }
            });
    }
    
    private async Task GenerateGetQuery(Entity entity)
    {
        await GenerateTemplate(
            Path.Combine(_templateDirectory, "src", "app", "shared", "core", "interfaces", "interface.tt"),
            Path.Combine(baseOutputPath, "src", "app", "shared", "core", "interfaces", entity.Name.ToLower(), $"get-{entity.Name.ToLower()}-query.interface.ts"),
            new Dictionary<string, object>
            {
                { "InterfaceName", $"Create{StringExtensions.ToTitleCase(entity.Name)}Command" },
                { "InterfaceProperties", entity.Properties }
            });
    }
    
    private static string SetTemplateDirectory() =>
        Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!
            .Parent!.Parent!.Parent!.FullName, "Templates", "Frontend");
    
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