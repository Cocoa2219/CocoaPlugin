using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;

namespace CocoaPlugin.API.Ranks.Handlers.Human;

public class GeneratorActivatedExperience : ExperienceBase
{
    public override ExperienceType Type { get; } = ExperienceType.GeneratorActivated;
    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Map.GeneratorActivating += OnGeneratorActivated;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Map.GeneratorActivating -= OnGeneratorActivated;
    }

    private void OnGeneratorActivated(GeneratorActivatingEventArgs ev)
    {
        foreach (var player in Player.List.Where(x => x.IsHuman))
        {
            Grant(player.UserId);
        }
    }
}