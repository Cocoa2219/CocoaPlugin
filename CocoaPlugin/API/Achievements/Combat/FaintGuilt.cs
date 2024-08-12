using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;
using PlayerRoles;

namespace CocoaPlugin.API.Achievements.Combat;

public class FaintGuilt : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.FaintGuilt;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "희미한 죄책감";
    public override string Description { get; set; } = "시설 경비로 D계급을 5명 이상 처치하세요.";

    private Dictionary<string, int> _kills;

    public void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker == null) return;
        if (ev.Attacker.Role != RoleTypeId.FacilityGuard) return;

        _kills.TryAdd(ev.Attacker.UserId, 0);

        _kills[ev.Attacker.UserId]++;

        if (_kills[ev.Attacker.UserId] >= 5)
            Achieve(ev.Attacker.UserId);
    }

    public override void RegisterEvents()
    {
        _kills = new Dictionary<string, int>();

        Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Player.Dying -= OnDying;
    }

    public override void OnRoundRestarting()
    {
        _kills.Clear();
    }
}