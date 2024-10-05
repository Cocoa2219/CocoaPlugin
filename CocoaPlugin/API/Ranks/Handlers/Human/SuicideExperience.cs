using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class SuicideExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.Suicide;
    public override void RegisterEvents()
    {
        // Here subscribe

        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker == null || ev.DamageHandler.IsSuicide || ev.Attacker == ev.Player)
        {
            Grant(ev.Player.UserId);
        }
    }
}