using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using Utils.NonAllocLINQ;

namespace CocoaPlugin.API.Achievements.Combat;

public class NowMyTurn : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.NowMyTurn;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "이제 내 차례다!";
    public override string Description { get; set; } = "자신을 죽인 SCP를 격리하십시오.";

    private Dictionary<string, HashSet<string>> _killedByScp;

    public override void RegisterEvents()
    {
        _killedByScp = new Dictionary<string, HashSet<string>>();

        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
    {
        if (ev.Attacker == null) return;

        if (ev.Attacker.IsScp)
        {
            if (!_killedByScp.ContainsKey(ev.Player.UserId))
            {
                _killedByScp[ev.Player.UserId] = new HashSet<string>();
            }

            _killedByScp[ev.Player.UserId].Add(ev.Attacker.UserId);
        }
        else
        {
            if (_killedByScp.Any(x => x.Value.Contains(ev.Player.UserId)))
            {
                Achieve(ev.Attacker.UserId);
            }
        }
    }
}