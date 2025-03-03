using StephArchitect;

var projectName = "Winkel";

// laptop
// "Server=localhost;Database=<#= ProjectName #>;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
// "Server=localhost;Database=<#= ProjectName #>_Tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
var baseOutputPath = @$"C:\Users\StephanKroukamp\RiderProjects\{projectName}";
var inputFilePath = @"C:\Users\StephanKroukamp\RiderProjects\StephArchitect\StephArchitect\Input\example.json";
var databaseConnectionString = $"Server=localhost;Database={projectName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
var testDatabaseConnectionString = $"Server=localhost;Database={projectName}_tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

// desktop
// "Server=STEPHAN\\SQLEXPRESS;Database=<#= ProjectName #>;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
// "Server=STEPHAN\\SQLEXPRESS;Database=<#= ProjectName #>_Tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
// var baseOutputPath = @$"C:\\Projects\\{projectName}";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";
// var databaseConnectionString = $"Server=STEPHAN\\\\SQLEXPRESS;Database={projectName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
// var testDatabaseConnectionString = $"Server=STEPHAN\\\\SQLEXPRESS;Database={projectName}_tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

var generator = new ProjectGenerator(projectName, baseOutputPath, inputFilePath, databaseConnectionString, testDatabaseConnectionString);

await generator.GenerateFromInput();