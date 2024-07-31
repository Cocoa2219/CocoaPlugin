using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CocoaPlugin.API;

public static class GradientMaker
{
    private static Color MixColors(Color color1, Color color2, float ratio)
    {
        return new Color(
            color1.r * (1 - ratio) + color2.r * ratio,
            color1.g * (1 - ratio) + color2.g * ratio,
            color1.b * (1 - ratio) + color2.b * ratio,
            color1.a * (1 - ratio) + color2.a * ratio
        );
    }

    public static string MakeGradient(string text, Dictionary<float, Color> colors)
    {
        var gradient = "";
        var length = text.Length;
        var colorKeys = colors.Keys.ToList();

        colorKeys.Sort();

        for (var i = 0; i < length; i++)
        {
            var ratio = i / (float)(length - 1);
            var lowerIndex = 0;
            var upperIndex = 1;

            for (var j = 0; j < colorKeys.Count; j++)
            {
                if (colorKeys[j] >= ratio)
                {
                    upperIndex = j;
                    lowerIndex = Mathf.Max(0, j - 1);
                    break;
                }
            }

            var color1 = colors[colorKeys[lowerIndex]];
            var color2 = colors[colorKeys[upperIndex]];

            var range = colorKeys[upperIndex] - colorKeys[lowerIndex];
            var t = range == 0 ? 0 : (ratio - colorKeys[lowerIndex]) / range;

            var mixedColor = MixColors(color1, color2, t);

            gradient += $"<color=#{ColorUtility.ToHtmlStringRGB(mixedColor)}>{text[i]}</color>";
        }

        return gradient;
    }

    private const string GradientPattern = @"<gradient=([^>]+)>(.*?)<\/gradient>";

    public static string ParseGradientTag(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var matches = Regex.Matches(text, GradientPattern);

        foreach (Match match in matches)
        {
            var colorData = match.Groups[1].Value;
            var colorPairs = colorData.Split(',');

            var colors = new Dictionary<float, Color>();

            foreach (var colorPair in colorPairs)
            {
                var colorPairData = colorPair.Split(':');

                if (colorPairData.Length != 2)
                    continue;

                if (!float.TryParse(colorPairData[0], out var ratio))
                    continue;

                if (!ColorUtility.TryParseHtmlString(colorPairData[1], out var color))
                    continue;

                colors.Add(ratio, color);
            }

            text = text.ReplaceFirstOccurrence(match.Value, MakeGradient(match.Groups[2].Value, colors));
        }

        return text;
    }

    public static string ReplaceFirstOccurrence(this string text, string search, string replace)
    {
        var index = text.IndexOf(search, StringComparison.Ordinal);

        if (index < 0)
            return text;

        return text[..index] + replace + text[(index + search.Length)..];
    }

    public static string ParseGradient(this string text)
    {
        return ParseGradientTag(text);
    }
}