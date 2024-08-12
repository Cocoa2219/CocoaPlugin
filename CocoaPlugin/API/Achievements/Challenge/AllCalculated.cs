using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;

namespace CocoaPlugin.API.Achievements.Challenge;

public class AllCalculated : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.AllCalculated;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Challenge];
    public override string Name { get; set; } = "좋아, 모두 계산되었어!";
    public override string Description { get; set; } = "SCP로부터 공격을 받고 1 HP 이하로 생존하십시오.";

    public override void RegisterEvents()
    {
        Player.Hurting += OnHurting;
    }

    public override void UnregisterEvents()
    {
        Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player == null || ev.Attacker == null) return;
        if (!ev.Attacker.IsScp) return;

        if (ev.Player.Health - ev.Amount <= 1)
        {
            Achieve(ev.Player.UserId);
        }
    }
}