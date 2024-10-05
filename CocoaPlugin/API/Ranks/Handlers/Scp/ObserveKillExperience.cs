using Exiled.Events.EventArgs.Scp079;
using Exiled.Events.Handlers;
using PlayerRoles.PlayableScps.Scp079;

namespace CocoaPlugin.API.Ranks.Handlers.Scp;

public class ObserveKillExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.ObserveKill;
    public override void RegisterEvents()
    {
        Scp079.GainingExperience += OnGainingExperience;
    }

    public override void UnregisterEvents()
    {
        Scp079.GainingExperience -= OnGainingExperience;
    }

    private void OnGainingExperience(GainingExperienceEventArgs ev)
    {
        var watch = ev.GainType == Scp079HudTranslation.ExpGainTerminationWitness;

        if (!watch) return;

        Grant(ev.Player.UserId);
    }
}