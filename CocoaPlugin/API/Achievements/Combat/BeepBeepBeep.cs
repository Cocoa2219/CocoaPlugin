using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.Events;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.Handlers;
using MEC;

namespace CocoaPlugin.API.Achievements.Combat;

public class BeepBeepBeep : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.BeepBeepBeep;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "삑... 삑.. 삑.";
    public override string Description { get; set; } = "수류탄으로 한 번에 4명 이상을 사살하십시오.";

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
        if (ev.Projectile.Type != ItemType.GrenadeHE) return;

        Timing.CallDelayed(0.1f, () =>
        {
            var targetsDied = ev.TargetsToAffect.Count(x => !x.IsAlive);

            if (targetsDied >= 4)
            {
                Achieve(ev.Player.UserId);
            }
        });
    }
}