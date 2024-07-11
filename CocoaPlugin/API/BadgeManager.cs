﻿using System.Collections.Generic;
using System.Linq;

namespace CocoaPlugin.API;

public static class BadgeManager
{
    private const string BadgeFileName = "Badges.txt";
    public static Dictionary<string, Badge> BadgeCache { get; } = new();

    public static bool AddBadge(string id, Badge badge)
    {
        if (!IsUserIdValid(id) || !Badge.IsValid(badge))
            return false;

        BadgeCache[id] = badge;
        return true;
    }

    public static bool RemoveBadge(string id)
    {
        if (!IsUserIdValid(id))
            return false;

        if (!BadgeCache.ContainsKey(id))
            return false;

        BadgeCache.Remove(id);
        return true;
    }

    public static Badge GetBadge(string id)
    {
        return !IsUserIdValid(id) ? null : BadgeCache.GetValueOrDefault(id);
    }

    public static void SaveBadges()
    {
        var text = string.Join("\n", BadgeCache.Select(x => $"{x.Key};{x.Value.Name};{x.Value.Color}"));

        FileManager.WriteFile(BadgeFileName, text);
    }

    public static bool LoadBadges()
    {
        var text = FileManager.ReadFile(BadgeFileName);

        if (string.IsNullOrWhiteSpace(text))
            return false;

        BadgeCache.Clear();

        foreach (var line in text.Split('\n'))
        {
            var parts = line.Split(';');

            if (parts.Length != 3)
                continue;

            BadgeCache[parts[0]] = new Badge
            {
                Name = parts[1],
                Color = parts[2]
            };
        }

        return true;
    }

    private static readonly List<string> ValidUserIds =
    [
        "@steam",
        "@discord",
        "@northwood",
        "@localhost"
    ];

    private static bool IsUserIdValid(string id)
    {
        return ValidUserIds.Any(id.EndsWith);
    }
}

public class Badge
{
    public string Name { get; set; }
    public string Color { get; set; }

    public static bool IsValid(Badge badge)
    {
        return !string.IsNullOrWhiteSpace(badge.Name) && !string.IsNullOrWhiteSpace(badge.Color) && BadgeColor.IsValidColor(badge.Color);
    }
}

public static class BadgeColor
{
    public static readonly Dictionary<string, string> Colors = new()
    {
        { "pink", "#FF96DE" },
        { "red", "#C50000" },
        { "brown", "#944710" },
        { "silver", "#A0A0A0" },
        { "light_green", "#32CD32" },
        { "crimson", "#DC143C" },
        { "cyan", "#00B7EB" },
        { "aqua", "#00FFFF" },
        { "deep_pink", "#FF1493" },
        { "tomato", "#FF6448" },
        { "yellow", "#FAFF86" },
        { "magenta", "#FF0090" },
        { "blue_green", "#4DFFB8" },
        { "orange", "#FF9966" },
        { "lime", "#BFFF00" },
        { "green", "#228B22" },
        { "emerald", "#50C878" },
        { "carmine", "#960018" },
        { "nickel", "#727472" },
        { "mint", "#98FB98" },
        { "army_green", "#4B5320" },
        { "pumpkin", "#EE7600" }
    };

    public static bool IsValidColor(string color)
    {
        return Colors.ContainsKey(color);
    }
}
