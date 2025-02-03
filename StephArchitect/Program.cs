using StephArchitect;

var generator = new ProjectGenerator("Winkel");

await generator.GenerateFromJson(@"C:\\Projects\\StephArchitect\\StephArchitect\\Input\\example.json");
