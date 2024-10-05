using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class RecontainedExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.Recontained;
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
        if (ev.Player.IsScp)
        {
            Grant(ev.Player.UserId);
        }
    }
}