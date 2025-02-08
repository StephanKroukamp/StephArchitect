using Newtonsoft.Json;
using StephArchitect;
using System.IO;

// steph - mac
var inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

// mac
//var baseOutputPath = $"/Users/stephankroukamp/RiderProjects/{projectName}";
//var inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

var baseOutputPath = $"../../Output/{projectName}";
var inputFilePath = "Input/example.json";

// windows
// var baseOutputPath = @$"C:\\Projects\\{projectName}\\Api";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

var apiGenerator = new ApiProjectGenerator(projectName, $"{baseOutputPath}-API", inputFilePath);
await apiGenerator.GenerateFromInput();

// Flutter mobile Frontend
var mobileGenerator = new MobileProjectGenerator(input.ProjectName, Path.Join(baseOutputPath, $"{StringExtensions.ToSnakeCase(input.ProjectName)}-mobile"), inputFilePath);
await mobileGenerator.GenerateFromInput();

var baseOutputPath = $"/Users/joanitanell/Documents/GitHub/{projectName}";
var inputFilePath = "/Users/joanitanell/Documents/GitHub/StephArchitect/StephArchitect/Input/example.json";

var frontendGenerator = new FrontendProjectGenerator(projectName, baseOutputPath, inputFilePath);
await frontendGenerator.GenerateFromInput();
