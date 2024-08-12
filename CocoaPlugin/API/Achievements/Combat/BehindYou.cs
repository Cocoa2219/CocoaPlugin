using System.Collections.Generic;
using CocoaPlugin.API.Managers;

namespace CocoaPlugin.API.Achievements.Combat;

public class BehindYou : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.BehindYou;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "Behind You.";
    public override string Description { get; set; } = "3명의 플레이어가 자신을 감지하지 못한 상태에서 사살하십시오.";

    private Dictionary<string, int> _undetectedCount;

    public override void RegisterEvents()
    {
        _undetectedCount = new Dictionary<string, int>();
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    public override void OnRoundRestarting()
    {
        _undetectedCount.Clear();
    }

    private void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
    {
        if (ev.Attacker == null) return;

        if (SightManager.Get(ev.Player).IsSeen(ev.Attacker)) return;

        _undetectedCount.TryAdd(ev.Attacker.UserId, 0);

        _undetectedCount[ev.Attacker.UserId]++;
    }
}