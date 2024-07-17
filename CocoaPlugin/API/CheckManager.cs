using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CocoaPlugin.API;

public static class CheckManager
{
    private const string CheckFileName = "Checks.txt";

    private static readonly Dictionary<string, List<Check>> CheckCache = new();

    public static bool AddCheck(string id, Check check)
    {
        if (!Utility.IsUserIdValid(id))
            return false;

        if (!CheckCache.TryGetValue(id, out var checks))
        {
            checks = [];
            CheckCache[id] = checks;
        }

        if (checks.Contains(check))
            return false;

        checks.Add(check);
        return true;
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
}

public class Check
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }

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

    public static Check Today => new()
    {
        Year = DateTime.Now.Year,
        Month = DateTime.Now.Month,
        Day = DateTime.Now.Day
    };
}
