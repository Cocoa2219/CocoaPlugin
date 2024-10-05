using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerStatsSystem;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class ScpHumeShieldExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.ScpHumeShield;

    private Dictionary<Player, float> _shield = new();

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
        _shield.Remove(ev.Player);
    }

    private void OnHurt(HurtEventArgs ev)
    {
        if (ev.Attacker == null) return;
        if (!ev.Player.IsScp) return;

        _shield.TryAdd(ev.Player, 0);

        _shield[ev.Player] += ((StandardDamageHandler)ev.DamageHandler.Base).AbsorbedHumeDamage;

        if (_shield[ev.Player] >= 200)
        {
            Grant(ev.Attacker.UserId);
            _shield.Remove(ev.Player);
        }
    }
}