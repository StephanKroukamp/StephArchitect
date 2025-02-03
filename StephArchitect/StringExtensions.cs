using System.Text.RegularExpressions;

namespace StephArchitect;

public static class StringExtensions
{
    public static string Pluralize(this string singular)
    {
        if (string.IsNullOrEmpty(singular))
            return singular;

        // Common irregular plurals
        switch (singular.ToLower())
        {
            case "person": return "people";
            case "man": return "men";
            case "woman": return "women";
            case "child": return "children";
            case "tooth": return "teeth";
            case "foot": return "feet";
            case "mouse": return "mice";
            case "goose": return "geese";
            default: break;
        }

        // Words ending in "y" preceded by a consonant -> replace "y" with "ies"
        if (Regex.IsMatch(singular, @"[^aeiou]y$", RegexOptions.IgnoreCase))
        {
            return Regex.Replace(singular, "y$", "ies", RegexOptions.IgnoreCase);
        }

        // Words ending in "s", "sh", "ch", "x", or "z" -> add "es"
        if (Regex.IsMatch(singular, @"(s|sh|ch|x|z)$", RegexOptions.IgnoreCase))
        {
            return singular + "es";
        }

        // Words ending in "f" or "fe" -> replace with "ves"
        if (Regex.IsMatch(singular, @"(f|fe)$", RegexOptions.IgnoreCase))
        {
            return Regex.Replace(singular, @"(f|fe)$", "ves", RegexOptions.IgnoreCase);
        }

        // Default: add "s"
        return singular + "s";
    }
}