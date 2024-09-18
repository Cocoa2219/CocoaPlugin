using CocoaPlugin.API.Managers;
using UnityEngine;

namespace CocoaPlugin.API.Ranks;

public abstract class ExperienceBase
{
    public abstract ExperienceType Type { get; }
    protected internal ExperienceConfig Config { get; set; }

    public virtual void Grant(string id)
    {
        var rank = RankManager.GetRank(id);

        if (rank == null)
            return;

        var amount = Mathf.RoundToInt(Config.Experience * Config.Multiplier);

        switch (amount)
        {
            case 0:
                return;
            case < 0:
                rank.Remove(amount, Type);
                break;
            default:
                rank.Add(amount, Type);
                break;
        }
    }

    public abstract void RegisterEvents();
    public abstract void UnregisterEvents();
}

public class ExperienceConfig
{
    public int Experience { get; set; }
    public float Multiplier { get; set; }
}

public enum ExperienceType
{
    AdminCommand,
    Example
}