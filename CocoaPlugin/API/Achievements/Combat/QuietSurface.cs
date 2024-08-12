using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;

namespace CocoaPlugin.API.Achievements.Combat;

public class QuietSurface : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.QuietSurface;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "고요한 천지";
    public override string Description { get; set; } = "한 지원 병력들을 다음 지원이 오기 전 모두 처치하십시오.";

    private int _respawnCount;
    private Dictionary<int, HashSet<Player>> _respawnedPlayers;
    private Dictionary<string, HashSet<Player>> _kills;

    public override void RegisterEvents()
    {

    }

    public override void UnregisterEvents()
    {

    }

    private void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        _respawnCount++;
        _respawnedPlayers.Add(_respawnCount, ev.Players.ToHashSet());
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker == null) return;

        _kills.TryAdd(ev.Attacker.UserId, _respawnedPlayers[_respawnCount]);

        _kills[ev.Attacker.UserId].Remove(ev.Player);

        if (_kills[ev.Attacker.UserId].Count == 0)
        {
            Achieve(ev.Attacker.UserId);
        }
    }
}