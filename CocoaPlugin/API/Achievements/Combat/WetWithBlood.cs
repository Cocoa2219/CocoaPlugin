using CocoaPlugin.API.Managers;

namespace CocoaPlugin.API.Achievements.Combat;

public class WetWithBlood : ProgressiveAchievement
{
    public override AchievementType Type { get; set; } = AchievementType.WetWithBlood;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "피로 흥건한 옷";
    public override string Description { get; set; } = "총 100번 적을 사살하십시오.";
    public override int NeededProgress { get; set; } = 100;

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
        if (ev.Attacker.IsScp) return;

        AddProgress(ev.Attacker.UserId);
    }
}