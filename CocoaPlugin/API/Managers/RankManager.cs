using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using CocoaPlugin.API.Ranks;
using CocoaPlugin.Configs;
using CommandSystem;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using UnityEngine;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.API.Managers;

public static class RankManager
{
    public const string RankFileName = "Ranks.txt";

    public static List<Rank> Ranks { get; } = [];

    public static Rank GetRank(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var rank = Ranks.Find(x => x.Id == id);

        if (rank == null)
        {
            rank = new Rank
            {
                Id = id
            };

            Ranks.Add(rank);
        }

        return rank;
    }

    public static void OnRoundEnded(RoundEndedEventArgs ev)
    {
        foreach (var rank in Ranks)
        {
            rank.OnRoundEnded(ev);
        }
    }

    public static void OnRoundRestarting()
    {
        foreach (var rank in Ranks)
        {
            rank.OnRoundRestarting();
        }

        SaveRanks();
    }

    private static readonly Dictionary<ExperienceType, ExperienceBase> experienceHandlers = new();

    public static void Initialize()
    {
        Server.RoundEnded += OnRoundEnded;
        Server.RestartingRound += OnRoundRestarting;

        LoadRanks();

        Log.Info("Loading experience handlers...");

        var sw = new Stopwatch();

        sw.Start();

        var baseType = typeof(ExperienceBase);

        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => baseType.IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface && x != baseType);

        foreach (var type in types)
        {
            Log.Info($"Loading experience handler: {type.Name}");
            CreateExperienceBaseInstance(type, CocoaPlugin.Instance.Config.Ranks.ExperienceSettings);
        }

        sw.Stop();
        Log.Info($"Loaded {experienceHandlers.Count} experience handlers in {sw.ElapsedMilliseconds}ms");
    }

    public static void Destroy()
    {
        Server.RoundEnded -= OnRoundEnded;
        Server.RestartingRound -= OnRoundRestarting;

        SaveRanks();

        foreach (var handler in experienceHandlers.Values)
        {
            handler.UnregisterEvents();
        }

        experienceHandlers.Clear();
    }

    private static void CreateExperienceBaseInstance(Type experienceBaseType,
        Dictionary<ExperienceType, ExperienceConfig> experienceSettings)
    {
        var instance = (ExperienceBase) Activator.CreateInstance(experienceBaseType);

        instance.Config = experienceSettings.TryGetValue(instance.Type, out var config) ? config : new ExperienceConfig();

        experienceHandlers[instance.Type] = instance;

        instance.RegisterEvents();
    }

    public static void SaveRanks()
    {
        var text = string.Join('\n', Ranks.ConvertAll(x => $"{x.Id};{x.Experience}"));

        FileManager.WriteFile(RankFileName, text);
    }

    public static void LoadRanks()
    {
        var text = FileManager.ReadFile(RankFileName);

        if (string.IsNullOrWhiteSpace(text))
        {
            Ranks.Clear();
            return;
        }

        Ranks.Clear();

        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            var parts = line.Split(';');

            if (parts.Length != 2)
                continue;

            if (!int.TryParse(parts[1], out var experience))
                continue;

            var rank = new Rank
            {
                Id = parts[0],
                Experience = experience
            };

            Ranks.Add(rank);
        }
    }
    
    public static ExperienceBase GetExperienceHandler(ExperienceType type)
    {
        return experienceHandlers.GetValueOrDefault(type);
    }

    public static Level GetLevel(int experience)
    {
        var level = new Level();
        var neededExperience = CocoaPlugin.Instance.Config.Ranks.NeededExperience; // Dictionary containing experience thresholds for each level type

        // Iterate through the experience thresholds to determine the level type based on experience
        foreach (var pair in neededExperience.Where(pair => experience >= pair.Value))
        {
            level.Type = pair.Key; // Assign the level type based on the threshold
        }

        // If the player has reached the highest level type (e.g., O5Council), return the max level
        if (level.Type == LevelType.O5Council)
        {
            level.Number = 0; // The top level in O5Council
            return level;
        }

        // Calculate the experience required to move between levels in the current rank
        var nextLevelTypeExperience = neededExperience[level.Type + 1]; // Experience needed for the next rank
        var currentLevelTypeExperience = neededExperience[level.Type]; // Experience threshold for the current rank
        var experienceRange = nextLevelTypeExperience - currentLevelTypeExperience; // Total experience needed for the next rank

        // Determine how much experience is needed per level number within the current rank
        var experiencePerLevel = experienceRange / CocoaPlugin.Instance.Config.Ranks.LevelNumberCount;

        for (var i = 1; i <= CocoaPlugin.Instance.Config.Ranks.LevelNumberCount; i++)
        {
            // Calculate the experience threshold for the current level number
            var levelExperience = currentLevelTypeExperience + experiencePerLevel * (CocoaPlugin.Instance.Config.Ranks.LevelNumberCount - i);

            // If the player's experience is greater than or equal to the level threshold, assign the level number
            if (experience >= levelExperience)
            {
                level.Number = (byte)i;
                break;
            }
        }

        return level;
    }

    // Method to show UI to player with broadcast
    public static void UpgradeBroadcast(int prev, int cur, Player player, float time = 0.1f)
    {
        if (prev == cur)
            return;

        if (player == null)
            return;

        var prevLevel = GetLevel(prev);
        var curLevel = GetLevel(cur);

        var levels = Level.GetLevelsBetween(prevLevel, curLevel);

        if (levels.Count == 0)
        {
            Log.Warn($"No levels found between {prevLevel} and {curLevel}");
            return;
        }

        Timing.KillCoroutines("ShowLevelUp_" + player.UserId);

        if (prev < cur)
        {
            Timing.RunCoroutine(ShowLevelUp(player, prev, cur, levels, time), "ShowLevelUp_" + player.UserId);
        }
        else
        {
            Timing.RunCoroutine(ShowLevelDown(player, cur, prev, levels, time), "ShowLevelUp_" + player.UserId);
        }
    }

    private static IEnumerator<float> ShowLevelUp(Player player, int prev, int cur, List<Level> levels, float time)
    {
        // Log.Info($"Showing level up for {player.Nickname} from {prev} to {cur}");

        var originalCur = cur;

        if (cur > CocoaPlugin.Instance.Config.Ranks.NeededExperience[LevelType.O5Council])
        {
            cur = CocoaPlugin.Instance.Config.Ranks.NeededExperience[LevelType.O5Council];
        }

        var acquiredExperience = cur - prev;
        var levelUpCount = 0;

        var finalBarCount = 0;
        int nextLevelNeededExperience;
        foreach (var level in levels)
        {
            var name = level.ToString();
            var neededExperience = level.NeededExperience;
            nextLevelNeededExperience = level.Next.NeededExperience;
            var expPerBar = (float) (nextLevelNeededExperience - neededExperience) / CocoaPlugin.Instance.Config.Ranks.BarCount;
            var isStart = level == levels.First();
            // Get starting index if level is the first in the list, calculate the bar count
            var startIndex = isStart ? expPerBar == 0 ? 0 : (prev - neededExperience) / (int) expPerBar : 0;

            for (var i = startIndex; i <= CocoaPlugin.Instance.Config.Ranks.BarCount; i++)
            {
                var currentExp = neededExperience + (int) (expPerBar * i);

                if (currentExp >= cur)
                {
                    Log.Info($"Current exp: {currentExp} > {cur}, breaking");
                    finalBarCount = i - 1;
                    break;
                }

                Log.Info($"Current exp: {currentExp} <= {cur}, continuing");

                var barText = GetBarText(CocoaPlugin.Instance.Config.Ranks.UpgradeBar, new List<(Color, int)>
                {
                    (Color.green, i),
                    (Color.red, CocoaPlugin.Instance.Config.Ranks.BarCount - i)
                });

                var broadcast = CocoaPlugin.Instance.Config.Ranks.UpgradeBroadcastFormat
                    .Replace("%rank%", name)
                    .Replace("%bar%", barText)
                    .Replace("%levelup%", levelUpCount == 0 ? "" : $" (<size=20px>▲</size> {levelUpCount})")
                    .Replace("%amount%", $"+ {acquiredExperience}");

                player.Broadcast(5, broadcast, global::Broadcast.BroadcastFlags.Normal, true);

                yield return Timing.WaitForSeconds(time);
            }

            levelUpCount++;
        }

        levelUpCount--;

        var curLevel = GetLevel(cur);

        if (curLevel.Type == LevelType.O5Council)
        {
            finalBarCount = CocoaPlugin.Instance.Config.Ranks.BarCount;
        }

        nextLevelNeededExperience = curLevel.Next.NeededExperience;

        for (var i = 1; i < finalBarCount + 1; i++)
        {
            var barText = GetBarText(CocoaPlugin.Instance.Config.Ranks.UpgradeBar, new List<(Color, int)>
            {
                (Color.yellow, i),
                (Color.green, finalBarCount - i),
                (Color.red, CocoaPlugin.Instance.Config.Ranks.BarCount - finalBarCount)
            });

            var broadcast = CocoaPlugin.Instance.Config.Ranks.UpgradeBroadcastFormat
                .Replace("%rank%", curLevel.ToString())
                .Replace("%bar%", barText)
                .Replace("%levelup%", levelUpCount == 0 ? "" : $" (<size=20px>▲</size> {levelUpCount})")
                .Replace("%amount%", $"{originalCur} / {nextLevelNeededExperience}");

            player.Broadcast(5, broadcast, global::Broadcast.BroadcastFlags.Normal, true);

            yield return Timing.WaitForSeconds(time);
        }

        if (finalBarCount == 0)
        {
            var barText = GetBarText(CocoaPlugin.Instance.Config.Ranks.UpgradeBar, new List<(Color, int)>
            {
                (Color.red, CocoaPlugin.Instance.Config.Ranks.BarCount)
            });

            var broadcast = CocoaPlugin.Instance.Config.Ranks.UpgradeBroadcastFormat
                .Replace("%rank%", curLevel.ToString())
                .Replace("%bar%", barText)
                .Replace("%levelup%", levelUpCount == 0 ? "" : $" (<size=20px>▲</size> {levelUpCount})")
                .Replace("%amount%", $"{originalCur} / {nextLevelNeededExperience}");

            player.Broadcast(5, broadcast, global::Broadcast.BroadcastFlags.Normal, true);
        }
    }

    private static IEnumerator<float> ShowLevelDown(Player player, int cur, int prev, List<Level> levels, float time)
    {
        // Log.Info($"Showing level down for {player.Nickname} from {cur} to {prev}");

        if (cur < 0)
        {
            cur = 0;
        }

        var lostExperience = prev - cur; // Experience lost
        var levelDownCount = 0;

        var finalBarCount = 0;
        int nextLevelNeededExperience;
        foreach (var level in levels)
        {
            var name = level.ToString();
            var neededExperience = level.NeededExperience;
            nextLevelNeededExperience = level.Previous.NeededExperience;
            var expPerBar = (float)(neededExperience - nextLevelNeededExperience) / CocoaPlugin.Instance.Config.Ranks.BarCount;
            var isStart = level == levels.First();

            // Reverse: start from the current bar and decrement towards the previous value
            var startIndex = isStart ? expPerBar == 0 ? CocoaPlugin.Instance.Config.Ranks.BarCount : CocoaPlugin.Instance.Config.Ranks.BarCount - (cur - neededExperience) / (int)expPerBar : CocoaPlugin.Instance.Config.Ranks.BarCount;

            for (var i = startIndex; i >= 0; i--) // Decrease the bar count
            {
                var currentExp = neededExperience + (int)(expPerBar * i);

                Log.Info($"Current exp: {neededExperience} + {expPerBar} * {i} = {currentExp}");

                if (currentExp <= cur)
                {
                    finalBarCount = i + 1;
                    break;
                }

                var barText = GetBarText(CocoaPlugin.Instance.Config.Ranks.UpgradeBar, new List<(Color, int)>
                {
                    (Color.green, i),
                    (Color.red, CocoaPlugin.Instance.Config.Ranks.BarCount - i)
                });

                var broadcast = CocoaPlugin.Instance.Config.Ranks.UpgradeBroadcastFormat
                    .Replace("%rank%", name)
                    .Replace("%bar%", barText)
                    .Replace("%levelup%", levelDownCount == 0 ? "" : $" (<size=20px>▼</size> {levelDownCount})")
                    .Replace("%amount%", $"- {lostExperience}");

                player.Broadcast(5, broadcast, global::Broadcast.BroadcastFlags.Normal, true);

                yield return Timing.WaitForSeconds(time);
            }

            levelDownCount++;
        }

        levelDownCount--;

        var curLevel = GetLevel(cur);

        nextLevelNeededExperience = curLevel.NeededExperience;

        // Final level decrease animation
        for (var i = finalBarCount; i >= 1; i--)
        {
            var barText = GetBarText(CocoaPlugin.Instance.Config.Ranks.UpgradeBar, new List<(Color, int)>
            {
                (Color.green, CocoaPlugin.Instance.Config.Ranks.BarCount - finalBarCount),
                (Color.red, i),
                (Color.yellow, finalBarCount - i)
            });

            var broadcast = CocoaPlugin.Instance.Config.Ranks.UpgradeBroadcastFormat
                .Replace("%rank%", curLevel.ToString())
                .Replace("%bar%", barText)
                .Replace("%levelup%", levelDownCount == 0 ? "" : $" (<size=20px>▼</size> {levelDownCount})")
                .Replace("%amount%", $"{cur} / {nextLevelNeededExperience}");

            player.Broadcast(5, broadcast, global::Broadcast.BroadcastFlags.Normal, true);

            yield return Timing.WaitForSeconds(time);
        }

        if (finalBarCount == 0)
        {
            var barText = GetBarText(CocoaPlugin.Instance.Config.Ranks.UpgradeBar, new List<(Color, int)>
            {
                (Color.green, CocoaPlugin.Instance.Config.Ranks.BarCount)
            });

            var broadcast = CocoaPlugin.Instance.Config.Ranks.UpgradeBroadcastFormat
                .Replace("%rank%", curLevel.ToString())
                .Replace("%bar%", barText)
                .Replace("%levelup%", levelDownCount == 0 ? "" : $" (<size=20px>▼</size> {levelDownCount})")
                .Replace("%amount%", $"{cur} / {nextLevelNeededExperience}");

            player.Broadcast(5, broadcast, global::Broadcast.BroadcastFlags.Normal, true);
        }
    }

    private static string GetBarText(char character, List<(Color color, int count)> colors)
    {
        var text = new StringBuilder();

        foreach (var (color, count) in colors)
        {
            if (count == 0)
                continue;

            text.Append($"<color={color.ToHex()}>{new string(character, count)}</color>");
        }

        return text.ToString();
    }
}

public class Rank
{
    public string Id { get; set; }
    public int Experience { get; set; }
    public Level Level => RankManager.GetLevel(Experience);

    internal bool CanGetExperience { get; set; } = true;

    private static readonly RankComparer comparer = new();

    internal readonly List<IExperienceModifier> Modifiers = [];

    public void Add(int experience, ExperienceType type, RoleTypeId role = RoleTypeId.None)
    {
        if (!CanGetExperience)
            return;

        ExperienceLogManager.WriteLog(new ExperienceLog
        {
            Id = Id,
            Experience = experience,
            Type = type,
            ActionType = ExperienceActionType.Add
        });

        experience = Modifiers.Where(modifier => modifier.ActionType.HasFlag(ExperienceActionType.Add)).Aggregate(experience, (current, modifier) => modifier.Modify(current));

        // Experience += experience;
        QueueExperience(new ExperienceAction
        {
            Role = role,
            Type = type,
            Action = ExperienceActionType.Add,
            Experience = experience
        });
    }

    public void Remove(int experience, ExperienceType type, RoleTypeId role = RoleTypeId.None)
    {
        ExperienceLogManager.WriteLog(new ExperienceLog
        {
            Id = Id,
            Experience = -experience,
            Type = type,
            ActionType = ExperienceActionType.Remove
        });

        experience = Modifiers.Where(modifier => modifier.ActionType.HasFlag(ExperienceActionType.Remove)).Aggregate(experience, (current, modifier) => modifier.Modify(current));

        // Experience -= experience;
        QueueExperience(new ExperienceAction
        {
            Role = role,
            Type = type,
            Action = ExperienceActionType.Remove,
            Experience = experience
        });
    }

    // Not Queued because it will be mostly used as admin command
    public void Set(int experience, ExperienceType type)
    {
        ExperienceLogManager.WriteLog(new ExperienceLog
        {
            Id = Id,
            Experience = experience,
            Type = type,
            ActionType = ExperienceActionType.Set
        });

        experience = Modifiers.Where(modifier => modifier.ActionType.HasFlag(ExperienceActionType.Set)).Aggregate(experience, (current, modifier) => modifier.Modify(current));

        Experience = experience;
    }

    internal void OnRoundEnded(RoundEndedEventArgs ev)
    {
        foreach (var experienceAction in ExperienceQueue)
        {
            var hasWon = RoleExtensions.GetTeam(experienceAction.Role).GetLeadingTeam() == ev.LeadingTeam;

            experienceAction.Experience = hasWon ? experienceAction.Experience : Mathf.RoundToInt(experienceAction.Experience * CocoaPlugin.Instance.Config.Ranks.LoseTeamExperienceMultiplier);
        }

        var experience = ExperienceQueue.Sum(x => x.Action == ExperienceActionType.Add ? x.Experience : -x.Experience);

        if (experience < -CocoaPlugin.Instance.Config.Ranks.MaxExperiencePerRound || experience > CocoaPlugin.Instance.Config.Ranks.MaxExperiencePerRound)
        {
            experience = experience > 0 ? CocoaPlugin.Instance.Config.Ranks.MaxExperiencePerRound : -CocoaPlugin.Instance.Config.Ranks.MaxExperiencePerRound;
        }

        Experience += experience;

        if (Experience < 0)
        {
            Experience = 0;
        }

        CanGetExperience = false;
    }

    internal void OnRoundRestarting()
    {
        foreach (var modifier in Modifiers.ToList().Where(modifier => modifier.DestroyOnRoundRestart))
        {
            Modifiers.Remove(modifier);
        }

        CanGetExperience = true;
    }

    public readonly List<ExperienceAction> ExperienceQueue = [];

    private void QueueExperience(ExperienceAction action)
    {
        ExperienceQueue.Add(action);
    }
}

public class ExperienceAction
{
    public RoleTypeId Role { get; set; }
    public ExperienceType Type { get; set; }
    public ExperienceActionType Action { get; set; }
    public int Experience { get; set; }
}

public class RankComparer : IComparer<Rank>
{
    public int Compare(Rank x, Rank y)
    {
        if (x != null)
            if (y != null)
                return x.Experience.CompareTo(y.Experience);
        return 0;
    }
}

public static class RankExtension
{
    public static Rank GetRank(this Player player)
    {
        return RankManager.GetRank(player.UserId);
    }
}

public interface IExperienceModifier
{
    ExperienceActionType ActionType { get; }
    int Modify(int experience);
    bool DestroyOnRoundRestart { get; }
}

public enum LevelType
{
    None,
    Cadet,
    FieldAgent,
    Operative,
    Sergeant,
    Lieutenant,
    Commander,
    Captain,
    Major,
    General,
    Overseer,
    O5Council
}

public record struct Level
{
    public LevelType Type { get; set; }
    public byte Number { get; set; }

    public int NeededExperience
    {
        get
        {
            if (Type == LevelType.O5Council)
                return Type.GetExperience();

            var experience = Type.GetExperience();
            var nextLevelTypeExperience = (Type + 1).GetExperience();

            var diff = nextLevelTypeExperience - experience;

            // Log.Info($"{experience} + {diff / CocoaPlugin.Instance.Config.Ranks.LevelNumberCount} * ({CocoaPlugin.Instance.Config.Ranks.LevelNumberCount} - {Number})");

            return experience + diff / CocoaPlugin.Instance.Config.Ranks.LevelNumberCount *
                (CocoaPlugin.Instance.Config.Ranks.LevelNumberCount - Number);
        }
    }

    public override string ToString()
    {
        return $"{Type.GetDisplayName()} {new string('I', Number)}".TrimEnd();
    }

    public Level Next {
        get {
            switch (Type)
            {
                case LevelType.Overseer when Number == 1:
                    return new Level { Type = LevelType.O5Council, Number = 0 };
                case LevelType.O5Council:
                    return this with { Number = 0 };
            }

            if (Number == 1)
                return new Level { Type = Type + 1, Number = (byte) CocoaPlugin.Instance.Config.Ranks.LevelNumberCount };

            return this with { Number = (byte) (Number - 1) };
        }
    }

    public Level Previous {
        get {
            if (Type == LevelType.Cadet && Number == 4)
                return this;

            if (Number == CocoaPlugin.Instance.Config.Ranks.LevelNumberCount)
                return new Level { Type = Type - 1, Number = 1 };

            return this with { Number = (byte) (Number + 1) };
        }
    }

    public static bool operator >(Level a, Level b)
    {
        if (a.Type > b.Type)
            return true;

        if (a.Type == b.Type)
            // Because levels go from 4 -> 3 -> 2 -> 1
            return a.Number < b.Number;

        return false;
    }

    public static bool operator <(Level a, Level b)
    {
        if (a.Type < b.Type)
            return true;

        if (a.Type == b.Type)
            // Because levels go from 4 -> 3 -> 2 -> 1
            return a.Number > b.Number;

        return false;
    }

    public static List<Level> GetLevelsBetween(Level a, Level b, bool includeA = true, bool includeB = true)
    {
        var levels = new List<Level>();

        // // If the levels are the same and we need to include them
        if (a == b)
        {
            if (includeA || includeB)
                levels.Add(a);

            return levels;
        }
        //
        // // If the level types are the same (same "rank"), but different level numbers
        // if (a.Type == b.Type)
        // {
        //     // If a is greater than b (progression since levels go from 4 -> 3 -> 2 -> 1)
        //     if (a.Number > b.Number)
        //     {
        //         // Include a if needed
        //         if (includeA)
        //             levels.Add(a);
        //
        //         // Add intermediate levels between a and b
        //         for (var i = a.Number - 1; i > b.Number; i--)
        //         {
        //             levels.Add(new Level { Type = a.Type, Number = (byte)i });
        //         }
        //
        //         // Include b if needed
        //     }
        //     else // If a is less than b (regression)
        //     {
        //         if (includeA)
        //             levels.Add(a);
        //
        //         for (var i = a.Number + 1; i < b.Number; i++)
        //         {
        //             levels.Add(new Level { Type = a.Type, Number = (byte)i });
        //         }
        //     }
        // }
        // else // If the levels are from different types (different "ranks")
        // {
        //     // Include a if needed
        //     if (includeA)
        //         levels.Add(a);
        //
        //     // Add levels from a's type down to the lowest level (1)
        //     for (var i = a.Number - 1; i >= 1; i--)
        //     {
        //         levels.Add(new Level { Type = a.Type, Number = (byte)i });
        //     }
        //
        //     // Iterate through all levels between a.Type and b.Type
        //     for (var currentType = a.Type + 1; currentType < b.Type; currentType++)
        //     {
        //         for (var i = CocoaPlugin.Instance.Config.Ranks.LevelNumberCount; i >= 1; i--)
        //         {
        //             levels.Add(new Level { Type = currentType, Number = (byte)i });
        //         }
        //     }
        //
        //     // Add levels from b's rank starting from the highest level down to b.Number
        //     for (var i = CocoaPlugin.Instance.Config.Ranks.LevelNumberCount; i > b.Number; i--)
        //     {
        //         levels.Add(new Level { Type = b.Type, Number = (byte)i });
        //     }
        //
        //     // Include b if needed
        // }

        if (a < b)
        {
            if (includeA)
                levels.Add(a);

            while (a < b)
            {
                Log.Info($"a: {a}, b: {b}");
                a = a.Next;
                levels.Add(a);
            }

            if (!includeB)
                levels.RemoveAt(levels.Count - 1);
        }
        else if (a > b)
        {
            if (includeA)
                levels.Add(a);

            while (a > b)
            {
                Log.Info($"a: {a}, b: {b}");
                a = a.Previous;
                levels.Add(a);
            }

            if (!includeB)
                levels.RemoveAt(levels.Count - 1);
        }

        return levels;
    }
}

public static class LevelTypeExtension
{
    public static string GetDisplayName(this LevelType type)
    {
        return type switch
        {
            LevelType.Cadet => "훈련병",
            LevelType.FieldAgent => "현장 요원",
            LevelType.Operative => "NTF 요원",
            LevelType.Sergeant => "NTF 병장",
            LevelType.Lieutenant => "NTF 중위",
            LevelType.Commander => "NTF 지휘관",
            LevelType.Captain => "NTF 대위",
            LevelType.Major => "NTF 소령",
            LevelType.General => "NTF 장군",
            LevelType.Overseer => "NTF 감독관",
            LevelType.O5Council => "O5 의회",
            _ => "미 연동"
        };
    }

    public static int GetExperience(this LevelType type)
    {
        return CocoaPlugin.Instance.Config.Ranks.NeededExperience.GetValueOrDefault(type);
    }
}

[CommandHandler(typeof(ClientCommandHandler))]
public class RankTest : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "no player found";
            return false;
        }

        var rank = player.GetRank();

        if (rank == null)
        {
            response = "No rank found.";
            return false;
        }

        var level = RankManager.GetLevel(rank.Experience);
        var experience = rank.Experience;

        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine($"lvl: {level}");
        sb.AppendLine($"exp: {experience}");

        sb.AppendLine("Applied modifiers (IExperienceModifier):");

        foreach (var modifier in rank.Modifiers)
        {
            sb.AppendLine($"{modifier.GetType().Name}");
        }

        sb.AppendLine("Experience queue (ExperienceAction):");

        foreach (var experienceAction in rank.ExperienceQueue)
        {
            sb.AppendLine($"{experienceAction.Type} {experienceAction.Action} {experienceAction.Experience}");
        }

        response = sb.ToString();
        return true;
    }

    public string Command { get; } = "rank";
    public string[] Aliases { get; } = { "r" };
    public string Description { get; } = "플레이어의 랭크를 확인합니다.";
}