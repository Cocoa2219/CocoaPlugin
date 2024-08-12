using CocoaPlugin.API.Managers;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Achievements.Survival;

public class ToTheIsekai : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.ToTheIsekai;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "이세카이로!";
    public override string Description { get; set; } = "지상에서 트럭에 치이십시오.";

    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker != null) return;
        if (ev.DamageHandler.Type != DamageType.Crushed) return;
        if (ev.Player.Zone != ZoneType.Surface) return;

        Achieve(ev.Player.UserId);
    }
}