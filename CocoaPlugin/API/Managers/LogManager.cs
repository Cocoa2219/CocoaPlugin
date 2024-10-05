using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using RoundRestarting;
using Random = UnityEngine.Random;

namespace CocoaPlugin.API.Managers;

public static class LogManager
{
    public const string LogFolder = "Logs";

    private const string LogFileFormat = "라운드 %id% (%start% ~ %end%).txt";

    private static readonly Regex LogIdRegex = new(@"라운드\s(\d+)\s", RegexOptions.Compiled);

    public static string CurrentRoundIdentifier { get; set; }

    private static string _currentLogPath;

    private static HashSet<string> _usedIdentifiers;

    private static bool _isQuitting;

    public static void Initialize()
    {
        var path = FileManager.GetPath(LogFolder);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        _usedIdentifiers = GetIdentifiers();

        CurrentRoundIdentifier = GenerateNewIdentifier(CocoaPlugin.Instance.Config.Logs.IdentifierLength);

        Exiled.Events.Handlers.Server.RestartingRound += OnRestartingRound;

        var logs = Directory.GetFiles(path, "*.txt").Where(x => Path.GetFileNameWithoutExtension(x).Contains("Round In Progress"));

        foreach (var log in logs)
        {
            File.Move(log, log.Replace("Round In Progress", "Round Aborted"));
        }

        _currentLogPath = Path.Combine(path,
            LogFileFormat.Replace("%id%", CurrentRoundIdentifier)
                .Replace("%start%", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"))
                .Replace("%end%", "Round In Progress"));

        File.Create(Path.Combine(path,
            LogFileFormat.Replace("%id%", CurrentRoundIdentifier)
                .Replace("%start%", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"))
                .Replace("%end%", "Round In Progress"))).Close();

        WriteLog($"-------START OF ROUND {CurrentRoundIdentifier}-------");

        Timing.RunCoroutine(RoundIdentifierHint(), "RoundIdentifierHint");
    }

    public static void Destroy()
    {
        Exiled.Events.Handlers.Server.RestartingRound -= OnRestartingRound;

        Timing.KillCoroutines("RoundIdentifierHint");
    }

    public static IEnumerator<float> RoundIdentifierHint()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(2f);

            if (CurrentRoundIdentifier == null)
                continue;

            foreach (var player in Player.List)
            {
                if (CocoaPlugin.Instance.ShootingRange.Instances.Any(x => x.Player == player))
                    continue;

                player.ShowHint($"<align=left><size=20><voffset=1335px><color=#ffffff66>{CurrentRoundIdentifier}</color></voffset></size></align>", 3f);
            }
        }
    }

    public static void WriteLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (CurrentRoundIdentifier == null)
            return;

        if (string.IsNullOrWhiteSpace(_currentLogPath))
            return;

        if (_isQuitting)
            return;

        var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}";

        File.AppendAllText(_currentLogPath, log + Environment.NewLine);
    }

    public static void OnRestartingRound()
    {
        WriteEnd(RoundEndType.RoundRestarting);
    }

    public static void WriteEnd(RoundEndType type)
    {
        _isQuitting = true;

        switch (type)
        {
            case RoundEndType.RoundRestarting:
                WriteLog($"-------END OF ROUND {CurrentRoundIdentifier} : ROUND RESTARTING-------");
                break;
            case RoundEndType.SoftRestarting:
                WriteLog($"-------END OF ROUND {CurrentRoundIdentifier} : SOFT RESTARTING-------");
                break;
            case RoundEndType.Shutdown:
                WriteLog($"-------END OF ROUND {CurrentRoundIdentifier} : SHUTDOWN-------");
                break;
        }

        var path = _currentLogPath;

        File.Move(path, path.Replace("Round In Progress", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss")));

        if (type is RoundEndType.Shutdown or RoundEndType.SoftRestarting)
        {
            _isQuitting = false;
            return;
        }

        CurrentRoundIdentifier = GenerateNewIdentifier(CocoaPlugin.Instance.Config.Logs.IdentifierLength);

        path = FileManager.GetPath(LogFolder);

        _currentLogPath = Path.Combine(path,
            LogFileFormat.Replace("%id%", CurrentRoundIdentifier)
                .Replace("%start%", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"))
                .Replace("%end%", "Round In Progress"));

        File.Create(Path.Combine(FileManager.GetPath(LogFolder), LogFileFormat.Replace("%id%", CurrentRoundIdentifier).Replace("%start%", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss")).Replace("%end%", "Round In Progress"))).Close();

        _isQuitting = false;

        WriteLog($"-------START OF ROUND {CurrentRoundIdentifier}-------");
    }

    private static HashSet<string> GetIdentifiers()
    {
        var path = FileManager.GetPath(LogFolder);
        var logs = Directory.GetFiles(path, "*.txt");

        var identifiers = (from log in logs select Path.GetFileNameWithoutExtension(log) into name where LogIdRegex.IsMatch(name) select LogIdRegex.Match(name) into match select match.Groups[1].Value).ToHashSet();

        return identifiers;
    }

    private const string IdentifierCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private static string GenerateNewIdentifier(int length)
    {
        string code;

        do
        {
            code = CreateRandomIdentifier(length);
        } while (_usedIdentifiers.Contains(code));

        _usedIdentifiers.Add(code);
        return code;
    }

    private static string CreateRandomIdentifier(int length)
    {
        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++)
            result.Append(IdentifierCharacters[Random.Range(0, IdentifierCharacters.Length)]);

        return result.ToString();
    }
}

public enum RoundEndType
{
    RoundRestarting,
    SoftRestarting,
    Shutdown
}

[HarmonyPatch(typeof(RoundRestart), nameof(RoundRestart.ChangeLevel))]
public class ServerRestartPatch
{
    public static bool Prefix(bool noShutdownMessage)
    {
        var nextRoundAction = ServerStatic.StopNextRound;

        switch (nextRoundAction)
        {
            case ServerStatic.NextRoundAction.DoNothing:
                break;
            case ServerStatic.NextRoundAction.Restart:
                LogManager.WriteEnd(RoundEndType.SoftRestarting);
                RankManager.Destroy();
                BadgeManager.SaveBadges();
                BadgeCooldownManager.SaveBadgeCooldowns();
                PenaltyManager.SavePenalties();
                CheckManager.SaveChecks();
                UserManager.SaveUsers();
                ConnectionManager.SaveConnections();
                break;
            case ServerStatic.NextRoundAction.Shutdown:
                LogManager.WriteEnd(RoundEndType.Shutdown);
                RankManager.Destroy();
                BadgeManager.SaveBadges();
                BadgeCooldownManager.SaveBadgeCooldowns();
                PenaltyManager.SavePenalties();
                CheckManager.SaveChecks();
                UserManager.SaveUsers();
                ConnectionManager.SaveConnections();
                break;
        }

        return true;
    }
}