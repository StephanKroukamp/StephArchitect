namespace StephArchitect;

public record ManifestFile(string Path, string Hash, DateTimeOffset Timestamp)
{
    public string Hash { get; set; } = Hash;
}