using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using Exiled.API.Features;
using GameCore;
using Mirror;
using PlayerRoles;
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

    public static byte GetServerNumber(ushort port)
    {
        if (port < 7777)
            return 0;

        return (byte)(port - 7776);
    }

    public static string RichTextToUnicode(string text)
    {
        return text.Replace('<', '\u003C').Replace('>', '\u003E');
    }

    public static Dictionary<Team, int> GetTeamCount(int playerCount)
    {
        var text = ConfigFile.ServerConfig.GetString("team_respawn_queue", "4014314031441404134041434414");
        var count = new Dictionary<Team, int>
        {
            {Team.SCPs, 0},
            {Team.FoundationForces, 0},
            {Team.ClassD, 0},
            {Team.Scientists, 0}
        };

        for (var i = 0; i < playerCount; i++)
        {
            switch (text[i % text.Length])
            {
                case '0':
                    count[Team.SCPs]++;
                    break;
                case '1':
                    count[Team.FoundationForces]++;
                    break;
                case '4':
                    count[Team.ClassD]++;
                    break;
                case '3':
                    count[Team.Scientists]++;
                    break;
            }
        }

        return count;
    }
}