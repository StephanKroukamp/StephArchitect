using Newtonsoft.Json;
using StephArchitect;

const string inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

var jsonContent = await File.ReadAllTextAsync(inputFilePath);

var input = JsonConvert.DeserializeObject<Input>(jsonContent) ??
            throw new Exception("No entities found in input.");

var baseOutputPath = $"/Users/stephankroukamp/RiderProjects/{input.ProjectName}";

// C# Backend Api
var apiGenerator = new ApiProjectGenerator(Path.Join(baseOutputPath, $"{StringExtensions.ToSnakeCase(input.ProjectName)}-api"), input);
await apiGenerator.GenerateFromInput();

// Flutter mobile Frontend
var mobileGenerator = new MobileProjectGenerator(input.ProjectName, Path.Join(baseOutputPath, $"{StringExtensions.ToSnakeCase(input.ProjectName)}-mobile"), inputFilePath);
await mobileGenerator.GenerateFromInput();

// // Angular web Frontend
var frontendGenerator = new FrontendProjectGenerator(input.ProjectName, Path.Join(baseOutputPath, $"{StringExtensions.ToSnakeCase(input.ProjectName)}-web"), inputFilePath);
await frontendGenerator.GenerateFromInput();