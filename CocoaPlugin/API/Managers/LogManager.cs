using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Exiled.API.Features;
using MEC;
using Random = UnityEngine.Random;

namespace CocoaPlugin.API.Managers;

public static class LogManager
{
    private const string LogFolder = "Logs";

    private const string LogFileFormat = "라운드 %id% (%start% ~ %end%).txt";

    private static readonly Regex LogIdRegex = new(@"라운드\s(\d+)\s", RegexOptions.Compiled);

    private static string CurrentRoundIdentifier { get; set; }

    private static FileStream _currentLogStream;

    private static HashSet<string> _usedIdentifiers;

    public static void Initialize()
    {
        if (!Directory.Exists(FileManager.GetPath(LogFolder)))
            Directory.CreateDirectory(FileManager.GetPath(LogFolder));

        _usedIdentifiers = GetIdentifiers();

        CurrentRoundIdentifier = GenerateNewIdentifier(CocoaPlugin.Instance.Config.Logs.IdentifierLength);

        Exiled.Events.Handlers.Server.RestartingRound += OnRestartingRound;

        _currentLogStream = File.Create(Path.Combine(FileManager.GetPath(LogFolder), LogFileFormat.Replace("%id%", CurrentRoundIdentifier).Replace("%start%", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss")).Replace("%end%", "Round In Progress")));

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
            yield return Timing.WaitForSeconds(1f);

            if (CurrentRoundIdentifier == null)
                continue;

            foreach (var player in Player.List)
            {
                if (player.HasHint) continue;

                player.ShowHint($"현재 라운드 식별자: {CurrentRoundIdentifier}", 1.2f);
            }
        }
    }

    public static void WriteLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (CurrentRoundIdentifier == null)
            return;

        if (_currentLogStream == null)
            return;

        var log = string.Empty;

        if (text.Contains(Environment.NewLine))
        {
            foreach (var line in text.Split(Environment.NewLine))
            {
                log += $"[{DateTime.Now:HH.mm.ss}] {line}";
                log += Environment.NewLine;
            }

            log = log.TrimEnd(Environment.NewLine.ToCharArray());
        }
        else
        {
            log = $"[{DateTime.Now:HH.mm.ss}] {text}";
        }

        var bytes = Encoding.UTF8.GetBytes(log + Environment.NewLine);
        _currentLogStream.Write(bytes, 0, bytes.Length);
    }

    private static void OnRestartingRound()
    {
        WriteLog($"-------END OF ROUND {CurrentRoundIdentifier}-------");

        var path = _currentLogStream.Name;

        try
        {
            _currentLogStream.Close();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to close the current log stream: {e}");
        }
        finally
        {
            _currentLogStream.Dispose();
        }

        File.Move(path, path.Replace("Round In Progress", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss")));

        CurrentRoundIdentifier = GenerateNewIdentifier(CocoaPlugin.Instance.Config.Logs.IdentifierLength);

        _currentLogStream = File.Create(Path.Combine(FileManager.GetPath(LogFolder), LogFileFormat.Replace("%id%", CurrentRoundIdentifier).Replace("%start%", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss")).Replace("%end%", "Round In Progress")));

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