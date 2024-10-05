using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class KillExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.KillArmed;

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
        if (ev.Attacker == null || ev.Player == null) return;

        if (ev.Player.IsArmed())
        {
            Grant(ev.Attacker.UserId);
        }
    }
}