using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Exiled.API.Features;

namespace CocoaPlugin.API;

public static class TyperHelper
{
    public static List<string> GroupTexts(this string input)
    {
        const string pattern = @"(<[^>]+>)+(.*?)(<\/[^>]+>)";
        var matches = Regex.Matches(input, pattern);
        List<string> result = [];
        result.AddRange(from Match match in matches select match.Groups[2].Value);

        return result.ToList();
    }

    public static string ReplaceText(this string input, int index, string newText)
    {
        const string pattern = @"(<[^>]+>)+.*?(<\/[^>]+>)+";
        var matches = Regex.Matches(input, pattern);

        if (index < 0 || index >= matches.Count)
        {
            Log.Error("Index out of range. i : " + index + " Count : " + matches.Count);
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }

        var match = matches[index];
        var originalText = match.Value;

        const string openingTagPattern = @"(<[^>]+>)+";
        const string closingTagPattern = @"(</[^>]+>)+";
        var openingTagsMatch = Regex.Match(originalText, openingTagPattern);
        var closingTagsMatch = Regex.Match(originalText, closingTagPattern);

        if (!openingTagsMatch.Success || !closingTagsMatch.Success)
            throw new InvalidOperationException("Invalid tag structure.");

        var openingTags = openingTagsMatch.Value;
        var closingTags = closingTagsMatch.Value;

        var newTaggedText = $"{openingTags}{newText}{closingTags}";

        return input.Replace(originalText, newTaggedText);
    }

    private static bool ContainsTags(this string input)
    {
        return Regex.IsMatch(input, @"<[^>]+>");
    }
}