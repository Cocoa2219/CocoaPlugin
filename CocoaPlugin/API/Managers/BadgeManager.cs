using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;

namespace CocoaPlugin.API.Managers;

public static class BadgeManager
{
    private const string BadgeFileName = "Badges.txt";

    public static Dictionary<string, Badge> BadgeCache { get; } = new();

    public static bool SetText(string id, string text)
    {
        if (!Utility.IsUserIdValid(id) || string.IsNullOrWhiteSpace(text))
            return false;

        if (!BadgeCache.ContainsKey(id))
            BadgeCache[id] = new Badge();

        BadgeCache[id].Name = text;
        return true;
    }

    public static bool SetColor(string id, string color)
    {
        if (!Utility.IsUserIdValid(id) || string.IsNullOrWhiteSpace(color) || !BadgeColor.IsValidColor(color))
            return false;

        if (!BadgeCache.ContainsKey(id))
            BadgeCache[id] = new Badge();

        BadgeCache[id].Color = color;
        return true;
    }

    public static bool AddBadge(string id, Badge badge)
    {
        if (!Utility.IsUserIdValid(id) || !Badge.IsValid(badge))
            return false;

        BadgeCache[id] = badge;

        RefreshBadge(id, badge);

        return true;
    }

    public static void RefreshBadge(string id, Badge badge)
    {
        var player = Player.Get(id);

        if (player == null || badge == null || !Badge.IsValid(badge))
            return;

        player.RankName = badge.Name;
        player.RankColor = badge.Color;
    }

    public static bool RemoveBadge(string id)
    {
        if (!Utility.IsUserIdValid(id))
            return false;

        if (!BadgeCache.ContainsKey(id))
            return false;

        BadgeCache.Remove(id);
        return true;
    }

    public static Badge GetBadge(string id)
    {
        return !Utility.IsUserIdValid(id) ? null : BadgeCache.GetValueOrDefault(id);
    }

    public static void SaveBadges()
    {
        var text = string.Join("\n", BadgeCache.Select(x => $"{x.Key};{x.Value.Name};{x.Value.Color}"));

        FileManager.WriteFile(BadgeFileName, text);
    }

    public static void LoadBadges()
    {
        var text = FileManager.ReadFile(BadgeFileName);

        if (string.IsNullOrWhiteSpace(text))
            return;

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

        foreach (var badge in BadgeCache)
        {
            RefreshBadge(badge.Key, badge.Value);
        }
    }
}

public class Badge
{
    public string Name { get; set; }
    public string Color { get; set; }

    public static bool IsValid(Badge badge)
    {
        return !string.IsNullOrWhiteSpace(badge.Name) && !string.IsNullOrWhiteSpace(badge.Color) &&
               BadgeColor.IsValidColor(badge.Color) && !badge.Name.Contains(';');
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
