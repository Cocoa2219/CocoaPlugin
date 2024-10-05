using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class RecontainScpWithMicroExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.RecontainScpWithMicro;
    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Attacker == null) return;
        if (ev.DamageHandler.Type != DamageType.MicroHid) return;

        Grant(ev.Attacker.UserId);
    }
}