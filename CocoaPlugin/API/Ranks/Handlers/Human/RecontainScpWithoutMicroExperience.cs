using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class RecontainScpWithoutMicroExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.RecontainScpWithMicro;
    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker == null) return;
        if (ev.DamageHandler.Type == DamageType.MicroHid) return;

        Grant(ev.Attacker.UserId);
    }
}