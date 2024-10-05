using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class UnlockGeneratorExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.UnlockGenerator;
    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.UnlockingGenerator += OnUnlockingGenerator;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.UnlockingGenerator -= OnUnlockingGenerator;
    }

    private void OnUnlockingGenerator(UnlockingGeneratorEventArgs ev)
    {
        Grant(ev.Player.UserId);
    }
}