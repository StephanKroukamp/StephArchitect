using StephArchitect;

var projectName = "Bruno";

// steph - mac
// var baseOutputPath = $"/Users/stephankroukamp/RiderProjects/{projectName}";
// var inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

// Danie
var baseOutputPath = $"../../Output/{projectName}";
var inputFilePath = "Input/example.json";

// steph - windows
// var baseOutputPath = @$"C:\\Projects\\{projectName}\\Api";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

var apiGenerator = new ApiProjectGenerator(projectName, $"{baseOutputPath}-API", inputFilePath);
await apiGenerator.GenerateFromInput();

// var mobileGenerator = new MobileProjectGenerator(projectName, $"{StringExtensions.ToSnakeCase(baseOutputPath)}-mobile", inputFilePath);
// await mobileGenerator.GenerateFromInput();