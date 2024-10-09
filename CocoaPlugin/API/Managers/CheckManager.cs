using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using JetBrains.Annotations;

namespace CocoaPlugin.API.Managers;

public static class CheckManager
{
    private const string CheckFileName = "Checks.txt";

    private static readonly Dictionary<string, List<Check>> CheckCache = new();

    public static void AddCheck(Player player, [CanBeNull] Check check = null)
    {
        if (!Utility.IsUserIdValid(player.UserId))
            return;

        if (player.DoNotTrack)
            return;

        if (check == null)
            check = Check.Today;

        if (!CheckCache.TryGetValue(player.UserId, out var checks))
        {
            checks = [];
            CheckCache[player.UserId] = checks;
        }

        if (checks.Contains(check))
            return;

        checks.Add(check);
    }

    public static void SaveChecks()
    {
        var text = string.Join('\n', CheckCache.Select(pair => $"{pair.Key};{string.Join(',', pair.Value)}"));

        FileManager.WriteFile(CheckFileName, text);
    }

    public static void LoadChecks()
    {
        var text = FileManager.ReadFile(CheckFileName);

        if (string.IsNullOrEmpty(text))
            return;

        foreach (var line in text.Split('\n'))
        {
            var data = line.Split(';');

            if (data.Length <= 1)
                continue;

            var id = data[0];
            var checks = new List<Check>();

            foreach (var check in data[1].Split(','))
            {
                var date = check.Split('-');

                if (date.Length != 3)
                    continue;

                if (!int.TryParse(date[0], out var year) || !int.TryParse(date[1], out var month) || !int.TryParse(date[2], out var day))
                    continue;

                checks.Add(new Check
                {
                    Year = year,
                    Month = month,
                    Day = day
                });
            }

            CheckCache[id] = checks;
        }
    }

    public static int GetStreakChecks(Player player)
    {
        if (!CheckCache.TryGetValue(player.UserId, out var checks))
            return 0;

        var today = DateTime.Today;
        var dates = checks.Select(check => new DateTime(check.Year, check.Month, check.Day)).ToList();
        dates.Sort();

        var maxStreak = 0;
        var currentStreak = 0;

        for (var i = 0; i < dates.Count; i++)
        {
            if (i == 0 || (dates[i] - dates[i - 1]).Days == 1)
            {
                currentStreak++;
            }
            else
            {
                currentStreak = 1;
            }

            var streakIncludesToday = dates[i].Date == today.Date;

            if (streakIncludesToday && currentStreak > maxStreak)
            {
                maxStreak = currentStreak;
            }
        }

        return maxStreak;
    }
}

public static class PlayerCheckExtension
{
    public static void AddCheck(this Player player, Check check)
    {
        CheckManager.AddCheck(player, check);
    }

    public static int GetStreakChecks(this Player player)
    {
        return CheckManager.GetStreakChecks(player);
    }
}

public class Check
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int Day { get; init; }

    public override string ToString()
    {
        return $"{Year}-{Month:D2}-{Day:D2}";
    }

    public static bool operator ==(Check a, Check b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        if (ReferenceEquals(a, b))
            return true;

        return a.Year == b.Year && a.Month == b.Month && a.Day == b.Day;
    }

    public static bool operator !=(Check a, Check b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        return obj is Check check && Equals(check);
    }

    private bool Equals(Check other)
    {
        return Year == other.Year && Month == other.Month && Day == other.Day;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Year, Month, Day);
    }

    public static Check Today => new()
    {
        Year = DateTime.Now.Year,
        Month = DateTime.Now.Month,
        Day = DateTime.Now.Day
    };
}
