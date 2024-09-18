using System.Collections.Generic;
using CocoaPlugin.API.Ranks;

namespace CocoaPlugin.Configs;

public class Ranks
{
    public Dictionary<ExperienceType, ExperienceConfig> ExperienceSettings { get; set; } = new()
    {
        { ExperienceType.AdminCommand, new ExperienceConfig { Experience = 0, Multiplier = 1 } },
        { ExperienceType.Example, new ExperienceConfig { Experience = 5, Multiplier = 1 } }
    };
}