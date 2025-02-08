﻿using StephArchitect;
using System.IO;

var projectName = "Bruno";

// mac
//var baseOutputPath = $"/Users/stephankroukamp/RiderProjects/{projectName}";
//var inputFilePath = "/Users/stephankroukamp/RiderProjects/StephArchitect/StephArchitect/Input/example.json";

var baseOutputPath = $"../Output/{projectName}";
var inputFilePath = "StephArchitect/Input/example.json";

// windows
// var baseOutputPath = @$"C:\\Projects\\{projectName}\\Api";
// var inputFilePath = @"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json";

// var generator = new ApiProjectGenerator(projectName, Path.Join(baseOutputPath, "API"), inputFilePath);
// await generator.GenerateFromInput();


var mobileGenerator = new MobileProjectGenerator(projectName, Path.Join(baseOutputPath, $"{StringExtensions.ToSnakeCase(projectName)}-mobile"), inputFilePath);
await mobileGenerator.GenerateFromInput();
