using CocoaPlugin.Handlers;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class DoorTrollingExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.DoorTrolling;
    public override void RegisterEvents()
    {
        PlayerEvents.OnDoorTrolling += OnDoorTrolling;
    }

    public override void UnregisterEvents()
    {
        PlayerEvents.OnDoorTrolling -= OnDoorTrolling;
    }

    private void OnDoorTrolling(PlayerEvents.DoorTrollingEventArgs ev)
    {
        Grant(ev.Player.UserId);
    }
}