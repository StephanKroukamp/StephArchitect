namespace StephArchitect;

public record Manifest
{
    public List<ManifestFile> Files { get; set; } = [];
}