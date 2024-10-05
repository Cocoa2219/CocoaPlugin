using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API.Ranks.Handlers.Scp;

public class KillWithoutMicroExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.KillWithoutMicro;
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
        if (ev.Attacker is not { IsScp: true }) return;

        if (!ev.Player.HasItem(ItemType.MicroHID))
        {
            Grant(ev.Attacker.UserId);
        }
    }
}