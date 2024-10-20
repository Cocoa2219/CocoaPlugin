using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Features;
using PlayerRoles;
using UnityEngine;

namespace CocoaPlugin.API.Ranks;

public abstract class ExperienceBase
{
    public abstract ExperienceType Type { get; }
    protected internal ExperienceConfig Config { get; set; }

    public void Grant(string id, int exp = -1)
    {
        var rank = RankManager.GetRank(id);

        if (rank == null)
            return;

        exp = exp == -1 ? Config.Experience : exp;

        var amount = Mathf.RoundToInt(exp * Config.Multiplier);

        var role = Player.TryGet(id, out var player) ? player.Role.Type : RoleTypeId.None;

        switch (amount)
        {
            case 0:
                return;
            case < 0:
                rank.Remove(amount, Type, role);
                break;
            default:
                rank.Add(amount, Type, role);
                break;
        }
    }

    public abstract void RegisterEvents();
    public abstract void UnregisterEvents();
}

public class ExperienceConfig
{
    public int Experience { get; set; }
    public float Multiplier { get; set; } = 1f;
}

public enum ExperienceType
{
    None,
    KillArmed,
    AdminCommand,
    ScpHumeShield,
    ScpHealth,
    RecontainScpWithMicro,
    EscapeWhileCuff,
    Escape,
    UnlockGenerator,
    GeneratorActivated,
    ScpRecontained,
    EscapeTeam,
    KillWithoutMicro,
    KillWithMicro,
    ObserveKill,
    AssistKill,
    TeslaKill,
    TierUpgrade,
    KilledByHuman,
    KilledByScp,
    DoorTrolling,
    Suicide,
    Recontained
}

public static class PlayerExtensions
{
    public static bool IsArmed(this Player player)
    {
        return player.Items.Any(x => x.IsWeapon);
    }
}