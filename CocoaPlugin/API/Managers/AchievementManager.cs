using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.API.Managers;

public static class AchievementManager
{
    public static List<Achievement> Achievements { get; private set; }

    public const string AchievementFolder = "Achievements";
    public const string AchievementStatsFolder = "Achievements\\Stats";

    public static void Initialize()
    {
        Log.Info("Loading achievements...");

        var sw = new System.Diagnostics.Stopwatch();

        var achievements = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(Achievement).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();

        Achievements = [];

        foreach (var instance in achievements.Select(achievement => (Achievement)Activator.CreateInstance(achievement)))
        {
            Server.RestartingRound += instance.OnRoundRestarting;

            instance.RegisterEvents();
            Log.Info($"Loaded achievement: {instance.Type} / {instance.Name}");
            Achievements.Add(instance);
        }

        sw.Stop();

        Log.Info($"Loaded {Achievements.Count} achievements in {sw.ElapsedMilliseconds}ms.");

        Log.Info("Saving achievements...");
        SaveAchievements();
        Log.Info("Saved achievements.");

        Log.Info("Loading achievement stats...");
        foreach (var achievement in Achievements)
        {
            var (achievedUsers, progresses) = LoadAchievementStats(achievement);

            achievement.AchievedUsers = achievedUsers;
            if (achievement is ProgressiveAchievement progressiveAchievement)
            {
                progressiveAchievement.Progresses = progresses;
            }
        }
        Log.Info("Loaded achievement stats.");

        SaveAchievementStats();
    }

    public static void Destroy()
    {
        Log.Info("Saving achievements...");
        SaveAchievements();
        SaveAchievementStats();
        Log.Info("Saved achievements.");

        foreach (var achievement in Achievements)
        {
            Server.RestartingRound -= achievement.OnRoundRestarting;
            achievement.UnregisterEvents();
        }
    }

    public static void SaveAchievements()
    {
        var path = FileManager.GetPath(AchievementFolder);

        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
        }

        Directory.CreateDirectory(path);

        foreach (var achievement in Achievements)
        {
            var isProgressive = achievement is ProgressiveAchievement;
            var neededProgress = achievement is ProgressiveAchievement progressiveAchievement ? progressiveAchievement.NeededProgress : 0;
            var sb = new StringBuilder();

            sb.AppendLine(achievement.Type.ToString());
            sb.AppendLine(achievement.Category.ToString());
            sb.AppendLine(achievement.Name);
            sb.AppendLine(achievement.Description);
            sb.AppendLine(isProgressive.ToString());
            sb.AppendLine(neededProgress.ToString());
            sb.AppendLine(achievement.IsHidden.ToString());

            FileManager.WriteFile(Path.Combine(path, $"{achievement.Type}.txt"), sb.ToString());
        }
    }

    public static void SaveAchievementStats()
    {
        var path = FileManager.GetPath(AchievementStatsFolder);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        foreach (var achievement in Achievements)
        {
            var sb = new StringBuilder();

            sb.AppendLine("__PROGRESS__");

            if (achievement is ProgressiveAchievement progressiveAchievement)
            {
                foreach (var (userId, progress) in progressiveAchievement.Progresses)
                {
                    sb.AppendLine($"{userId};{progress}");
                }
            }

            sb.AppendLine("__ACHIEVED__");

            foreach (var (userId, achieved) in achievement.AchievedUsers)
            {
                sb.AppendLine($"{userId};{achieved}");
            }

            FileManager.WriteFile(Path.Combine(path, $"{achievement.Type}.txt"), sb.ToString());
        }
    }

    public static void SaveAchievementStat(Achievement achievement)
    {
        var path = FileManager.GetPath(Path.Combine(AchievementStatsFolder, $"{achievement.Type}.txt"));

        var sb = new StringBuilder();

        sb.AppendLine("__PROGRESS__");

        if (achievement is ProgressiveAchievement progressiveAchievement)
        {
            foreach (var (userId, progress) in progressiveAchievement.Progresses)
            {
                sb.AppendLine($"{userId};{progress}");
            }
        }

        sb.AppendLine("__ACHIEVED__");

        foreach (var (userId, achieved) in achievement.AchievedUsers)
        {
            sb.AppendLine($"{userId};{achieved}");
        }

        FileManager.WriteFile(path, sb.ToString());
    }

    public static void ResetAchievements()
    {
        foreach (var achievement in Achievements)
        {
            achievement.AchievedUsers.Clear();

            if (achievement is ProgressiveAchievement progressiveAchievement)
            {
                progressiveAchievement.Progresses.Clear();
            }
        }

        SaveAchievementStats();
    }

    public static (Dictionary<string, bool> AchievedUsers, Dictionary<string, int> Progresses) LoadAchievementStats(Achievement achievement)
    {
        var path = FileManager.GetPath(Path.Combine(AchievementStatsFolder, $"{achievement.Type}.txt"));

        if (!File.Exists(path))
        {
            return (new(), new());
        }

        var lines = File.ReadAllLines(path);

        var achievedUsers = new Dictionary<string, bool>();
        var progresses = new Dictionary<string, int>();

        var isProgress = false;
        var isAchieved = false;

        foreach (var line in lines)
        {
            switch (line)
            {
                case "__PROGRESS__":
                    isProgress = true;
                    isAchieved = false;
                    continue;
                case "__ACHIEVED__":
                    isProgress = false;
                    isAchieved = true;
                    continue;
            }

            var data = line.Split(';');

            if (isProgress)
            {
                progresses[data[0]] = int.Parse(data[1]);
            }
            else if (isAchieved)
            {
                achievedUsers[data[0]] = bool.Parse(data[1]);
            }
        }

        return (achievedUsers, progresses);
    }

    public static Achievement GetAchievement(AchievementType type)
    {
        return Achievements.FirstOrDefault(x => x.Type == type);
    }
}

public abstract class Achievement
{
    public abstract AchievementType Type { get; set; }
    public abstract Category Category { get; set; }
    public abstract string Name { get; set; }
    public abstract string Description { get; set; }
    public virtual bool IsHidden { get; set; } = false;

    public Dictionary<string, bool> AchievedUsers { get; set; } = new();

    public void Achieve(string userId)
    {
        var player = Player.Get(userId);

        if (!UserManager.IsUserExist(userId)) return;

        if (AchievedUsers.ContainsKey(userId) && AchievedUsers[userId])
            return;

        AchievedUsers[userId] = true;

        player?.SendConsoleMessage($"You've just achieved {Name}!", "white");

        AchievementManager.SaveAchievementStat(this);

        object achievement = new
        {
            Type = Type,
            Name = Name,
            UserId = userId,
            DiscordId = UserManager.GetUser(userId).DiscordId
        };

        NetworkManager.SendAchievement(achievement);
    }

    public void Revoke(string userId)
    {
        AchievedUsers[userId] = false;

        var player = Player.Get(userId);

        player?.SendConsoleMessage($"You've just lost {Name}!", "white");

        AchievementManager.SaveAchievementStat(this);
    }

    public virtual void OnRoundRestarting()
    {

    }

    public virtual void RegisterEvents()
    {

    }

    public virtual void UnregisterEvents()
    {

    }
}

public abstract class ProgressiveAchievement : Achievement
{
    public abstract int NeededProgress { get; set; }

    public Dictionary<string, int> Progresses { get; set; } = new();

    public void AddProgress(string userId, int progress = 1)
    {
        if (AchievedUsers.ContainsKey(userId) && AchievedUsers[userId])
            return;

        Progresses.TryAdd(userId, 0);

        Progresses[userId] += progress;

        var player = Player.Get(userId);

        if (Progresses[userId] >= NeededProgress)
        {
            Achieve(userId);
        }
        else
        {
            if (player != null)
            {
                player.SendConsoleMessage($"You've just made progress on {Name}! ({Progresses[userId]}/{NeededProgress})", "white");
            }
        }

        AchievementManager.SaveAchievementStat(this);
    }

    public void RemoveProgress(string userId, int progress = 1)
    {
        if (AchievedUsers.ContainsKey(userId) && AchievedUsers[userId])
        {
            AchievedUsers[userId] = false;
        }

        Progresses.TryAdd(userId, 0);

        Progresses[userId] -= progress;

        if (Progresses[userId] < 0)
        {
            Progresses[userId] = 0;
        }

        var player = Player.Get(userId);

        if (player != null)
        {
            player.
                SendConsoleMessage($"You've just lost progress on {Name}! ({Progresses[userId]}/{NeededProgress})", "white");
        }

        AchievementManager.SaveAchievementStat(this);
    }

    public void RevokeProgress(string userId)
    {
        if (AchievedUsers.ContainsKey(userId) && AchievedUsers[userId])
        {
            AchievedUsers[userId] = false;
        }

        Progresses[userId] = 0;

        AchievementManager.SaveAchievementStat(this);
    }
}

public enum AchievementType
{
    FirstBlood,
    Pacifist,
    No914,
    OneShotOneKill,
    GhostInTheShadows,
    Rich,
    WaitAlready,
    AllCalculated,
    Speedrun,
    ResourceManagement,
    HideAndSeekExpert,
    JustWasAHuman,
    BloodedItem,
    BlurryFace,
    DimensionEscape,
    UncomfortableCohabitation,
    EncounterMachine,
    ChainReaction,
    NowMyTurn,
    BeepBeepBeep,
    BehindYou,
    GhostLight,
    ReturnedPeace,
    CannotSee,
    OwMyEyes,
    FaintGuilt,
    InsurgencyCoin,
    ToTheIsekai,
    QuietSurface,
    FirstSteps,
    EscapeExpert,
    Invincible,
    BloodInMyHand,
    WetWithBlood,
    NightmareOfSky
}

public enum AchievementCategory
{
    Combat,
    Survival,
    Teamwork,
    Challenge,
    Etc,
    Scp
}

public class Category
{
    public AchievementCategory AchievementCategory { get; set; }
    public string Name { get; set; }
    public string DiscordEmojiId { get; set; }
    public string Description { get; set; }

    public static Dictionary<AchievementCategory, Category> Categories { get; } = new()
    {
        [AchievementCategory.Combat] = new Category
        {
            AchievementCategory = AchievementCategory.Combat,
            Name = "전투",
            DiscordEmojiId = "⚔️",
            Description = "전투와 관련된 업적입니다."
        },
        [AchievementCategory.Survival] = new Category
        {
            AchievementCategory = AchievementCategory.Survival,
            Name = "생존",
            DiscordEmojiId = "🏃",
            Description = "생존과 관련된 업적입니다."
        },
        [AchievementCategory.Teamwork] = new Category
        {
            AchievementCategory = AchievementCategory.Teamwork,
            Name = "팀워크",
            DiscordEmojiId = "🤝",
            Description = "팀워크와 관련된 업적입니다."
        },
        [AchievementCategory.Challenge] = new Category
        {
            AchievementCategory = AchievementCategory.Challenge,
            Name = "도전",
            DiscordEmojiId = "🏆",
            Description = "도전할 수 있는 업적입니다."
        },
        [AchievementCategory.Etc] = new Category
        {
            AchievementCategory = AchievementCategory.Etc,
            Name = "기타",
            DiscordEmojiId = "🎉",
            Description = "그 외 웃기거나 특별한 업적입니다."
        },
        [AchievementCategory.Scp] = new Category
        {
            AchievementCategory = AchievementCategory.Scp,
            Name = "SCP",
            DiscordEmojiId = "😈",
            Description = "SCP와 관련된 업적입니다."
        }
    };

    public override string ToString()
    {
        return AchievementCategory + "\n" + Name + "\n" + DiscordEmojiId + "\n" + Description;
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class AchievementCommand : ICommand
{
    public string Command { get; } = "achievement";
    public string[] Aliases { get; } = { "ach" };
    public string Description { get; } = "업적을 확인합니다.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (sender is not PlayerCommandSender commandSender)
        {
            response = "This command must be executed in-game.";
            return false;
        }

        var player = Player.Get(commandSender.SenderId);

        if (player == null)
        {
            response = "You must be in-game to use this command.";
            return false;
        }

        var subcommand = arguments.At(0);

        switch (subcommand)
        {
            case "reset":
                AchievementManager.ResetAchievements();

                response = "Achievements have been reset.";
                return true;
            default:
                response = "Usage: achievement <reset>";
                return false;
        }
    }
}
