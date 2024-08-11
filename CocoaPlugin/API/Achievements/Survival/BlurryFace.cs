using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Scp096;
using Exiled.Events.Handlers;
using Player = Exiled.API.Features.Player;

namespace CocoaPlugin.API.Achievements.Survival;

public class BlurryFace : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.BlurryFace;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "흐릿한 얼굴";
    public override string Description { get; set; } = "SCP-096의 얼굴을 보고 생존하십시오.";

    private HashSet<string> _scp096Targets;

    public override void RegisterEvents()
    {
        Scp096.Enraging += OnScp096Enrage;
        Scp096.CalmingDown += OnScp096Calm;
    }

    public override void UnregisterEvents()
    {
        Scp096.Enraging -= OnScp096Enrage;
        Scp096.CalmingDown -= OnScp096Calm;
    }

    private void OnScp096Enrage(EnragingEventArgs ev)
    {
        _scp096Targets = ev.Scp096.Targets.Select(x => x.UserId).ToHashSet();
    }

    private void OnScp096Calm(CalmingDownEventArgs ev)
    {
        foreach (var target in _scp096Targets.Where(target => Player.Get(target) != null).Where(target => Player.Get(target).IsAlive))
        {
            Achieve(target);
        }
    }

    public override void OnRoundRestarting()
    {
        _scp096Targets.Clear();
    }
}