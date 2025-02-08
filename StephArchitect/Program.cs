using Newtonsoft.Json;
using StephArchitect;
using System.IO;

// steph - mac
var inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

// Danie
// var baseOutputPath = $"../../../../../Output/{projectName}";
// var inputFilePath = "../../../Input/example.json";

// steph - windows
// var baseOutputPath = @$"C:\\Projects\\{projectName}\\Api";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

var jsonContent = await File.ReadAllTextAsync(inputFilePath);

var input = JsonConvert.DeserializeObject<Input>(jsonContent) ??
            throw new Exception("No entities found in input.");

var baseOutputPath = $"/Users/stephankroukamp/RiderProjects/{input.ProjectName}-API";

// C# Backend Api
// var apiGenerator = new ApiProjectGenerator(baseOutputPath, input);
// await apiGenerator.GenerateFromInput();

// Flutter mobile Frontend
var mobileGenerator = new MobileProjectGenerator(input.ProjectName, Path.Join(baseOutputPath, $"{StringExtensions.ToSnakeCase(input.ProjectName)}-mobile"), inputFilePath);
await mobileGenerator.GenerateFromInput();
