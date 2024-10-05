using Exiled.Events.EventArgs.Scp079;
using Exiled.Events.Handlers;

namespace CocoaPlugin.API.Ranks.Handlers.Scp;

public class TierUpgradeExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.TierUpgrade;
    public override void RegisterEvents()
    {
        Scp079.GainingLevel += OnGainingLevel;
    }

    public override void UnregisterEvents()
    {
        Scp079.GainingLevel -= OnGainingLevel;
    }

    private void OnGainingLevel(GainingLevelEventArgs ev)
    {
        Grant(ev.Player.UserId, (ev.NewLevel - 1) * 10);
    }
}