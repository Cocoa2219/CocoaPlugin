using CocoaPlugin.API.Managers;
using CustomPlayerEffects;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Achievements.Combat;

public class OwMyEyes : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.OwMyEyes;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "아악... 내 눈...!";
    public override string Description { get; set; } = "섬광탄을 맞은 상태에서 적을 사살하십시오.";

    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
    {
        if (ev.Attacker == null) return;

        if (!ev.Attacker.IsEffectActive<Flashed>()) return;

        Achieve(ev.Attacker.UserId);
    }
}