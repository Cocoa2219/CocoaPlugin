using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CocoaPlugin.API.Ranks;
using CocoaPlugin.Configs;
using Exiled.API.Features;

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

    private static readonly Dictionary<ExperienceType, ExperienceBase> experienceHandlers = new();

    public static void Initialize()
    {
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

        if (experienceSettings.TryGetValue(instance.Type, out var config))
        {
            instance.Config = config;
        }

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

            Ranks.Add(new Rank
            {
                Id = parts[0],
                Experience = experience
            });
        }
    }
}

public class Rank
{
    public string Id { get; set; }
    public int Experience { get; set; }

    public void Add(int experience, ExperienceType type)
    {
        ExperienceLogManager.WriteLog(new ExperienceLog
        {
            Id = Id,
            Experience = experience,
            Type = type,
            ActionType = ExperienceActionType.Add
        });

        Experience += experience;
    }

    public void Remove(int experience, ExperienceType type)
    {
        ExperienceLogManager.WriteLog(new ExperienceLog
        {
            Id = Id,
            Experience = -experience,
            Type = type,
            ActionType = ExperienceActionType.Remove
        });

        Experience -= experience;

        if (Experience < 0)
            Experience = 0;
    }

    public void Set(int experience, ExperienceType type)
    {
        ExperienceLogManager.WriteLog(new ExperienceLog
        {
            Id = Id,
            Experience = experience,
            Type = type,
            ActionType = ExperienceActionType.Set
        });

        Experience = experience;
    }
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