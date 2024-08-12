using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.Handlers;
using MEC;

namespace CocoaPlugin.API.Achievements.Combat;

public class GhostLight : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.GhostLight;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "고스트라이트";
    public override string Description { get; set; } = "SCP-2176으로 SCP를 방 안에 가두십시오.";

    public override void RegisterEvents()
    {
        Map.ExplodingGrenade += OnExplodingGrenade;
    }

    public override void UnregisterEvents()
    {
        Map.ExplodingGrenade -= OnExplodingGrenade;
    }

    private void OnExplodingGrenade(ExplodingGrenadeEventArgs ev)
    {
        if (ev.Player == null) return;
        if (ev.Projectile.Type != ItemType.SCP2176) return;

        if (ev.Projectile.As<Scp2176Projectile>().Room.Players.Count(x => x.IsScp) >= 1)
        {
            Achieve(ev.Player.UserId);
        }
    }
}