using Exiled.API.Features;
using PlayerRoles;
using UnityEngine;

namespace CocoaPlugin.API;

public class LeftUser(Player player)
{
    public string UserId { get; set; } = player.UserId;
    public string Nickname { get; set; } = player.Nickname;
    public RoleTypeId Role { get; set; } = player.Role.Type;
    public Vector3 Position { get; set; } = player.Position;
    public Lift Lift { get; set; } = player.Lift;
    public float Health { get; set; } = player.Health;
    public float ArtificalHealth { get; set; } = player.ArtificialHealth;
    public float HumeShield { get; set; } = player.HumeShield;
    public bool IsReconnected { get; set; } = false;

    public string GetUserId(bool ignoreVsr = false)
    {
        return CocoaPlugin.Instance.Config.VSRCompliant && !ignoreVsr ? "[EXPUNGED]" : UserId;
    }
}
