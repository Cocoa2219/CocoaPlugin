using System.Collections.Generic;
using System.Linq;

namespace CocoaPlugin.API.Managers;

public class BadgeCooldownManager
{
    private const string BadgeFileName = "BadgeCooldowns.txt";

    public static Dictionary<string, BadgeCooldown> BadgeCooldownCache { get; } = new();

    public static bool SetCooldown(string id, long textCooldown, long colorCooldown)
    {
        return SetTextCooldown(id, textCooldown) && SetColorCooldown(id, colorCooldown);
    }

    public static bool SetTextCooldown(string id, long cooldown)
    {
        if (!Utility.IsUserIdValid(id))
            return false;

        if (!BadgeCooldownCache.ContainsKey(id))
            BadgeCooldownCache[id] = new BadgeCooldown();

        BadgeCooldownCache[id].TextCooldown = cooldown;
        return true;
    }

    public static bool SetColorCooldown(string id, long cooldown)
    {
        if (!Utility.IsUserIdValid(id))
            return false;

        if (!BadgeCooldownCache.ContainsKey(id))
            BadgeCooldownCache[id] = new BadgeCooldown();

        BadgeCooldownCache[id].ColorCooldown = cooldown;
        return true;
    }

    public static bool RemoveBadgeCooldown(string id)
    {
        if (!Utility.IsUserIdValid(id))
            return false;

        if (!BadgeCooldownCache.ContainsKey(id))
            return false;

        BadgeCooldownCache.Remove(id);
        return true;
    }

    public static BadgeCooldown GetBadgeCooldown(string id)
    {
        return !Utility.IsUserIdValid(id) ? null : BadgeCooldownCache.GetValueOrDefault(id);
    }

    public static void SaveBadgeCooldowns()
    {
        var text = string.Join("\n", BadgeCooldownCache.Select(x => $"{x.Key};{x.Value.TextCooldown};{x.Value.ColorCooldown}"));

        FileManager.WriteFile(BadgeFileName, text);
    }

    public static void LoadBadgeCooldowns()
    {
        var text = FileManager.ReadFile(BadgeFileName);

        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (var line in text.Split('\n'))
        {
            var split = line.Split(';');

            if (split.Length != 3)
                continue;

            if (!long.TryParse(split[1], out var textCooldown) || !long.TryParse(split[2], out var colorCooldown))
                continue;

            BadgeCooldownCache[split[0]] = new BadgeCooldown
            {
                TextCooldown = textCooldown,
                ColorCooldown = colorCooldown
            };
        }
    }
}

public class BadgeCooldown
{
    public long TextCooldown { get; set; }
    public long ColorCooldown { get; set; }
}