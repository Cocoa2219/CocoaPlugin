using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using Exiled.API.Features;
using Mirror;
using UnityEngine;

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

    public static void PlayAnti207BreakSound(Vector3 pos)
    {
        NetworkServer.SendToReady(new AntiScp207.BreakMessage
        {
            SoundPos = pos
        });
    }

    private static readonly char[] _splitter = [' ', '.', ','];

    public static Player[] ParsePlayers(string text)
    {
        var players = new List<Player>();

        foreach (var part in text.Split(_splitter, StringSplitOptions.RemoveEmptyEntries))
        {
            if (Player.TryGet(part, out var player))
            {
                players.Add(player);
            }
        }

        return players.ToArray();
    }
}