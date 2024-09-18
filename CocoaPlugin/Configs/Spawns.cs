using System.Collections.Generic;
using PlayerRoles;
using Respawning;

namespace CocoaPlugin.Configs;

public class StartSpawn
{
    public RoleTypeId Role { get; set; }
    public float Chance { get; set; }
}

public class Spawns
{
    public Dictionary<SpawnableTeamType, StartSpawn> StartSpawnChances { get; set; } = new()
    {
        {SpawnableTeamType.ChaosInsurgency, new StartSpawn() {Role = RoleTypeId.ChaosRifleman, Chance = 0.2f}},
        {SpawnableTeamType.NineTailedFox, new StartSpawn() {Role = RoleTypeId.NtfPrivate, Chance = 0.2f}},
    };
}