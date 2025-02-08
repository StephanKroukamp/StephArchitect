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

var apiGenerator = new ApiProjectGenerator($"/Users/stephankroukamp/RiderProjects/{input.ProjectName}-API", input);
await apiGenerator.GenerateFromInput();


var mobileGenerator = new MobileProjectGenerator(projectName, Path.Join(baseOutputPath, $"{StringExtensions.ToSnakeCase(projectName)}-mobile"), inputFilePath);
await mobileGenerator.GenerateFromInput();
