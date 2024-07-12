using System.Collections.Generic;
using CocoaPlugin.Configs.Scp;
using PlayerRoles;

namespace CocoaPlugin.Configs;

public class Scps
{
    public int ScpHealMin { get; set; } = 10;
    public int ScpHealMax { get; set; } = 50;

    public Dictionary<RoleTypeId, int> ScpHealth { get; set; } = new()
    {
        {RoleTypeId.Scp049, -1},
        {RoleTypeId.Scp0492, -1},
        {RoleTypeId.Scp096, -1},
        {RoleTypeId.Scp106, -1},
        {RoleTypeId.Scp173, -1},
        {RoleTypeId.Scp939, -1},
        {RoleTypeId.Scp3114, -1},
    };

    public Scp049 Scp049 { get; set; } = new();
}
