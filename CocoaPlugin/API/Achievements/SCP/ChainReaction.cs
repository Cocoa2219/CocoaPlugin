using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using MEC;

namespace CocoaPlugin.API.Achievements.SCP;

public class ChainReaction : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.ChainReaction;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Scp];
    public override string Name { get; set; } = "연쇄 반응";
    public override string Description { get; set; } = "SCP-079로 테슬라를 통해 동시에 5명 이상을 사살하십시오.";

    private Dictionary<string, int> _teslaDyingCount;
    private Dictionary<string, Exiled.API.Features.TeslaGate> _teslaGates;

    public override void RegisterEvents()
    {
        _teslaDyingCount = new Dictionary<string, int>();
        _teslaGates = new Dictionary<string, Exiled.API.Features.TeslaGate>();
        Exiled.Events.Handlers.Scp079.InteractingTesla += OnTriggeringTesla;
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Scp079.InteractingTesla -= OnTriggeringTesla;
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    public override void OnRoundRestarting()
    {
        _teslaDyingCount.Clear();
        _teslaGates.Clear();
    }

    private void OnTriggeringTesla(InteractingTeslaEventArgs ev)
    {
        _teslaDyingCount[ev.Player.UserId] = 0;
        _teslaGates[ev.Player.UserId] = ev.Tesla;

        Timing.CallDelayed(1f, () =>
        {
            if (!_teslaDyingCount.TryGetValue(ev.Player.UserId, out var value)) return;
            if (value >= 5)
            {
                Achieve(ev.Player.UserId);
            }

            _teslaDyingCount.Remove(ev.Player.UserId);
            _teslaGates.Remove(ev.Player.UserId);
        });
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker != null) return;

        if (ev.DamageHandler.Type != DamageType.Tesla) return;
        if (ev.Player.CurrentRoom.TeslaGate == null) return;

        var tesla = ev.Player.CurrentRoom.TeslaGate;

        if (!_teslaGates.ContainsValue(tesla)) return;

        foreach (var (userId, gate) in _teslaGates)
        {
            if (gate != tesla) continue;
            if (!_teslaDyingCount.TryGetValue(userId, out var value)) continue;
            _teslaDyingCount[userId] = value + 1;
        }
    }
}