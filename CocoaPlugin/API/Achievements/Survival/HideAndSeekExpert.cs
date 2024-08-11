using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace CocoaPlugin.API.Achievements.Survival;

public class HideAndSeekExpert : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.HideAndSeekExpert;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "숨바꼭질 고수";
    public override string Description { get; set; } = "자신의 팀이 모두 죽은 상태에서 3분 동안 생존하십시오.";

    private Dictionary<string, CoroutineHandle> _coroutines = new();

    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    public override void OnRoundRestarting()
    {
        foreach (var coroutine in _coroutines)
        {
            Timing.KillCoroutines(coroutine.Value);
        }

        _coroutines.Clear();
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Player.IsScp) return;

        var team = ev.Player.Role.Team;

        if (_coroutines.ContainsKey(ev.Player.UserId))
        {
            Timing.KillCoroutines(_coroutines[ev.Player.UserId]);
            _coroutines.Remove(ev.Player.UserId);
        }

        if (Player.Get(team).Count() > 1) return;

        var last = Player.Get(team).First();

        if (_coroutines.ContainsKey(last.UserId))
        {
            Timing.KillCoroutines(_coroutines[last.UserId]);
            _coroutines.Remove(last.UserId);
        }

        _coroutines.Add(ev.Player.UserId, Timing.RunCoroutine(OnDiedCoroutine(last, team)));
    }

    private IEnumerator<float> OnDiedCoroutine(Player player, Team team)
    {
        yield return Timing.WaitForSeconds(180f);

        if (player.Role.Team != team) yield break;
        if (Player.Get(team).Count() > 1) yield break;

        Achieve(player.UserId);
    }
}