using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class EscapeWhileCuffExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.EscapeWhileCuff;

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
        if (!ev.Player.IsCuffed) return;

        Grant(ev.Player.Cuffer.UserId);
    }
}