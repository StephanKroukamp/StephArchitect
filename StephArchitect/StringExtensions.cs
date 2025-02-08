using System;
using System.Collections.Generic;
using System.Globalization;
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
/// <summary>
    /// Splits the input string into words based on common delimiters (space, underscore, hyphen)
    /// and also based on camelCase / PascalCase boundaries.
    /// </summary>
    public static string[] SplitWords(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<string>();
 
        // Replace underscores and hyphens with spaces so that they act as delimiters.
        input = input.Replace("_", " ").Replace("-", " ");
 
        // Split by spaces first.
        var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var words = new List<string>();
 
        // For each token, use a regex to split on word boundaries in case of camelCase or PascalCase.
        // This regex matches:
        // - sequences like "XML" or "ID"
        // - words starting with an optional uppercase letter followed by lowercase letters
        // - numbers
        var wordPattern = new Regex(@"([A-Z]+(?=$|[A-Z][a-z])|[A-Z]?[a-z]+|\d+)", RegexOptions.Compiled);
        foreach (var token in tokens)
        {
            var matches = wordPattern.Matches(token);
            foreach (Match match in matches)
            {
                words.Add(match.Value);
            }
        }
        return words.ToArray();
    }
 
    /// <summary>
    /// Converts input to PascalCase.
    /// </summary>
    public static string ToPascalCase(string input)
    {
        var words = SplitWords(input);
        for (int i = 0; i < words.Length; i++)
        {
            // Lowercase the word then convert the first letter to uppercase.
            words[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i].ToLower());
        }
        return string.Join("", words);
    }
 
    /// <summary>
    /// Converts input to camelCase.
    /// </summary>
    public static string ToCamelCase(string input)
    {
        string pascal = ToPascalCase(input);
        if (!string.IsNullOrEmpty(pascal))
        {
            // Lowercase the first character.
            return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
        }
        return pascal;
    }
 
    /// <summary>
    /// Converts input to snake_case.
    /// </summary>
    public static string ToSnakeCase(string input)
    {
        var words = SplitWords(input);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = words[i].ToLower();
        }
        return string.Join("_", words);
    }
 
    /// <summary>
    /// Converts input to kebab-case.
    /// </summary>
    public static string ToKebabCase(string input)
    {
        var words = SplitWords(input);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = words[i].ToLower();
        }
        return string.Join("-", words);
    }
 
    /// <summary>
    /// Converts input to Title Case (each word capitalized and separated by a space).
    /// </summary>
    public static string ToTitleCase(string input)
    {
        var words = SplitWords(input);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i].ToLower());
        }
        return string.Join(" ", words);
    }
}