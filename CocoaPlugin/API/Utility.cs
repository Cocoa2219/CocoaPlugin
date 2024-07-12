using System;
using System.Collections.Generic;
using System.Linq;

namespace CocoaPlugin.API;

public static class Utility
{
    public static long UnixTimeNow => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static string UnixTimeToDateTime(long unixTime)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private static readonly List<string> ValidUserIds =
    [
        "@steam",
        "@discord",
        "@northwood",
        "@localhost"
    ];

    public static bool IsUserIdValid(string id)
    {
        return ValidUserIds.Any(id.EndsWith);
    }
}