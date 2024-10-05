using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerStatsSystem;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class ScpHealthExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.ScpHealth;

    private Dictionary<Player, float> _health = new();

    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
        Exiled.Events.Handlers.Player.Hurt += OnHurt;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Hurt -= OnHurt;
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        _health.Remove(ev.Player);
    }

    private void OnHurt(HurtEventArgs ev)
    {
        if (ev.Attacker == null) return;

        if (!ev.Player.IsScp) return;

        _health.TryAdd(ev.Player, 0);

        _health[ev.Player] += ((StandardDamageHandler)ev.DamageHandler.Base).DealtHealthDamage;
    }
}