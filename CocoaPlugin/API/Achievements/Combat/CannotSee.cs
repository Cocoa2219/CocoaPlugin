using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;

namespace CocoaPlugin.API.Achievements.Combat;

public class CannotSee : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.CannotSee;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "한 치 앞도 보이지 않아";
    public override string Description { get; set; } = "불이 꺼진 방에서 적을 사살하십시오.";

    public override void RegisterEvents()
    {
        Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker == null) return;

        if (ev.Player.CurrentRoom != ev.Attacker.CurrentRoom) return;

        if (!ev.Player.CurrentRoom.AreLightsOff) return;

        if (ev.Player.CurrentRoom.Players.Any(x => x.HasFlashlightModuleEnabled)) return;
        if (ev.Player.CurrentRoom.Players.Any(x => x.CurrentItem is Flashlight && x.CurrentItem.As<Flashlight>().IsEmittingLight)) return;

        Achieve(ev.Attacker.UserId);
    }
}