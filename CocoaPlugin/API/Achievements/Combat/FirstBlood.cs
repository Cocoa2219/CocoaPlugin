using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;

namespace CocoaPlugin.API.Achievements.Combat;

public class FirstBlood : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.FirstBlood;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "퍼스트 블러드";
    public override string Description { get; set; } = "처음으로 플레이어를 처치하십시오.";

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

        Achieve(ev.Attacker.UserId);
    }
}