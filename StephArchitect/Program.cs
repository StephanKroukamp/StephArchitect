using StephArchitect;
using System.IO;

var projectName = "Bruno";

// steph - mac
// var baseOutputPath = $"/Users/stephankroukamp/RiderProjects/{projectName}";
// var inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

// Danie
var baseOutputPath = $"../../../../../Output/{projectName}";
var inputFilePath = "../../../Input/example.json";

// steph - windows
// var baseOutputPath = @$"C:\\Projects\\{projectName}\\Api";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

var apiGenerator = new ApiProjectGenerator(projectName, Path.Join(baseOutputPath, $"{projectName}-API"), inputFilePath);
await apiGenerator.GenerateFromInput();


var mobileGenerator = new MobileProjectGenerator(projectName, Path.Join(baseOutputPath, $"{StringExtensions.ToSnakeCase(projectName)}-mobile"), inputFilePath);
await mobileGenerator.GenerateFromInput();
