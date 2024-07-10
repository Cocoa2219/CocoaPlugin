using System.Collections.Generic;
using PlayerRoles;
using Respawning;

namespace CocoaPlugin.Configs;

public class Spawns
{
    public Dictionary<SpawnableTeamType, (RoleTypeId role, float chance)> StartSpawnChances { get; set; } = new()
    {
        {SpawnableTeamType.ChaosInsurgency, (RoleTypeId.ChaosRifleman, 0.2f)},
        {SpawnableTeamType.NineTailedFox, (RoleTypeId.NtfPrivate, 0.2f)},
        { SpawnableTeamType.None , (RoleTypeId.None, 0.6f)}
    };
}