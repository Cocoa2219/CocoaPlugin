using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class EscapeExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.Escape;
    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Escaping += OnEscaping;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Escaping += OnEscaping;
    }

    private void OnEscaping(EscapingEventArgs ev)
    {
        Grant(ev.Player.UserId);
    }
}