using Exiled.API.Features;
using PlayerRoles;
using UnityEngine;

namespace CocoaPlugin.API;

public class LeftUser
{
    public string UserId { get; set; }
    public string Nickname { get; set; }
    public RoleTypeId Role { get; set; }
    public Vector3 Position { get; set; }
    public Lift Lift { get; set; }
    public float Health { get; set; }
    public float HumeShield { get; set; }
    public float ArtificalHealth { get; set; }
}