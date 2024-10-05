using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class KilledByHumanExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.KilledByHuman;

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

        if (ev.Attacker.IsHuman)
        {
            Grant(ev.Player.UserId);
        }
    }
}