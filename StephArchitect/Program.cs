using StephArchitect;

var projectName = "Bruno";

// mac
var baseOutputPath = $"/Users/stephankroukamp/RiderProjects/{projectName}/Api";
var inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

// windows
// var baseOutputPath = @$"C:\\Projects\\{projectName}\\Api";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

var generator = new ApiProjectGenerator(projectName, baseOutputPath, inputFilePath);

await generator.GenerateFromInput();