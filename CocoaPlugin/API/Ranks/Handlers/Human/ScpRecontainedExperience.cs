using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class ScpRecontainedExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.ScpRecontained;
    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying += OnScpRecontained;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Dying -= OnScpRecontained;
    }

    private void OnScpRecontained(DyingEventArgs ev)
    {
        if (!ev.Player.IsScp) return;
        if (ev.Attacker == null) return;

        foreach (var player in Player.List)
        {
            if (player == ev.Attacker) continue;

            Grant(player.UserId);
        }
    }
}