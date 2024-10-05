using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class EscapeTeamExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.EscapeTeam;
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
        var team = ev.NewRole.GetTeam();
        foreach (var player in Player.Get(team))
        {
            Grant(player.UserId);
        }
    }
}