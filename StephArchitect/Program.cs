using StephArchitect;

var projectName = "Bruno";

// mac
var baseOutputPath = $"/Users/joanitanell/Documents/GitHub/{projectName}";
var inputFilePath = "/Users/joanitanell/Documents/GitHub/StephArchitect/StephArchitect/Input/example.json";

// windows
// var baseOutputPath = @$"C:\\Projects\\{projectName}\\Api";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

// var apiGenerator = new ApiProjectGenerator(projectName, $"{baseOutputPath}-API", inputFilePath);
// await apiGenerator.GenerateFromInput();

var frontendGenerator = new FrontendProjectGenerator(projectName, baseOutputPath, inputFilePath);
await frontendGenerator.GenerateFromInput();

// var mobileGenerator = new MobileProjectGenerator(projectName, $"{StringExtensions.ToSnakeCase(baseOutputPath)}-mobile", inputFilePath);
// await mobileGenerator.GenerateFromInput();