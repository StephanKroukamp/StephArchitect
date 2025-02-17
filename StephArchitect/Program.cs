using StephArchitect;

var projectName = "Winkel";

// laptop
// "Server=localhost;Database=<#= ProjectName #>;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
// "Server=localhost;Database=<#= ProjectName #>_Tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
// var baseOutputPath = @$"C:\Users\StephanKroukamp\RiderProjects\{projectName}";
// var inputFilePath = @"C:\Users\StephanKroukamp\RiderProjects\StephArchitect\StephArchitect\Input\example.json";

// desktop
// "Server=STEPHAN\\SQLEXPRESS;Database=<#= ProjectName #>;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
// "Server=STEPHAN\\SQLEXPRESS;Database=<#= ProjectName #>_Tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
var baseOutputPath = @$"C:\\Projects\\{projectName}";
var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

var generator = new ProjectGenerator(projectName, baseOutputPath, inputFilePath);

await generator.GenerateFromInput();