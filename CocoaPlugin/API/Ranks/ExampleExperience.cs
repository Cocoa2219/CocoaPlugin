using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks;

public class ExampleExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.Example;

    public override void RegisterEvents()
    {

    }

    public override void UnregisterEvents()
    {

    }

    private void OnVerified(VerifiedEventArgs ev)
    {
        Grant(ev.Player.UserId);
    }
}