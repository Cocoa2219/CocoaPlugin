using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using CocoaPlugin.API.Ranks;

namespace CocoaPlugin.Configs;

public class Ranks
{
    public Dictionary<ExperienceType, ExperienceConfig> ExperienceSettings { get; set; } = new()
    {
        { ExperienceType.KillArmed, new ExperienceConfig { Experience = 20, Multiplier = 1 } },
        { ExperienceType.AdminCommand, new ExperienceConfig { Experience = 0, Multiplier = 1 } }, // Fixed to 0
        { ExperienceType.ScpHumeShield, new ExperienceConfig { Experience = 10, Multiplier = 1 } },
        { ExperienceType.ScpHealth, new ExperienceConfig { Experience = 10, Multiplier = 1 } },
        { ExperienceType.RecontainScpWithMicro, new ExperienceConfig { Experience = 20, Multiplier = 1 } },
        { ExperienceType.EscapeWhileCuff, new ExperienceConfig { Experience = 5, Multiplier = 1 } },
        { ExperienceType.Escape, new ExperienceConfig { Experience = 25, Multiplier = 1 } },
        { ExperienceType.UnlockGenerator, new ExperienceConfig { Experience = 5, Multiplier = 1 } },
        { ExperienceType.GeneratorActivated, new ExperienceConfig { Experience = 5, Multiplier = 1 } },
        { ExperienceType.ScpRecontained, new ExperienceConfig { Experience = 5, Multiplier = 1 } },
        { ExperienceType.EscapeTeam, new ExperienceConfig { Experience = 5, Multiplier = 1 } },
        { ExperienceType.KillWithoutMicro, new ExperienceConfig { Experience = 10, Multiplier = 1 } },
        { ExperienceType.KillWithMicro, new ExperienceConfig { Experience = 10, Multiplier = 1 } },
        { ExperienceType.ObserveKill, new ExperienceConfig { Experience = 5, Multiplier = 1 } },
        { ExperienceType.AssistKill, new ExperienceConfig { Experience = 10, Multiplier = 1 } },
        { ExperienceType.TeslaKill, new ExperienceConfig { Experience = 10, Multiplier = 1 } },
        { ExperienceType.TierUpgrade, new ExperienceConfig { Experience = 0, Multiplier = 1 } },
        { ExperienceType.KilledByHuman, new ExperienceConfig { Experience = -10, Multiplier = 1 } },
        { ExperienceType.KilledByScp, new ExperienceConfig { Experience = -5, Multiplier = 1 } },
        { ExperienceType.DoorTrolling, new ExperienceConfig { Experience = -5, Multiplier = 1 } },
        { ExperienceType.Suicide, new ExperienceConfig { Experience = -10, Multiplier = 1 } },
        { ExperienceType.Recontained, new ExperienceConfig { Experience = -50, Multiplier = 1 } },
    };

    public Dictionary<LevelType, int> NeededExperience { get; set; } = new()
    {
        { LevelType.Cadet, 0 },
        { LevelType.FieldAgent , 400},
        { LevelType.Operative, 800 },
        { LevelType.Sergeant, 1200 },
        { LevelType.Lieutenant, 1600 },
        { LevelType.Commander, 2000 },
        { LevelType.Captain, 2400 },
        { LevelType.Major, 2800 },
        { LevelType.General, 3200 },
        { LevelType.Overseer, 3600 },
        { LevelType.O5Council, 4000 }
    };

    public int LevelNumberCount { get; set; } = 4;

    public string BadgeFormat { get; set; } = "%badge% | %level%";

    public int MaxExperiencePerRound { get; set; } = 100;

    public float LoseTeamExperienceMultiplier { get; set; } = 0.7f;

    public string UpgradeBroadcastFormat { get; set; } =
        "<line-height=40px><cspace=0.05em><size=25px><b>%rank%</b> (%levelup%)</size>\n<size=20px><mspace=4px>bar%</mspace>   (%amount%)</size></cspace></line-height>";

    public char UpgradeBar { get; set; } = '⎯';
    public int BarCount { get; set; } = 100;
}