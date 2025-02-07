using StephArchitect;

var projectName = "Winkel";

// mac
var baseOutputPath = $"/Users/stephankroukamp/RiderProjects/{projectName}";
var inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

// windows
// var baseOutputPath = @$"C:\\Projects\\{projectName}";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

var generator = new ProjectGenerator(projectName, baseOutputPath, inputFilePath);

await generator.GenerateFromInput();