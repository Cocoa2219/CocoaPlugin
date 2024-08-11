using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace CocoaPlugin.API.Achievements.Survival;

public class DimensionEscape : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.DimensionEscape;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "차원 탈출";
    public override string Description { get; set; } = "차원 주머니에서 10초 안에 탈출하십시오.";

    private HashSet<string> _pocketCorrodingPlayers;

    public override void RegisterEvents()
    {
        _pocketCorrodingPlayers = [];

        Exiled.Events.Handlers.Player.EscapingPocketDimension += OnEscapingPocketDimension;
        Exiled.Events.Handlers.Player.ReceivingEffect += OnRecievingEffect;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.EscapingPocketDimension -= OnEscapingPocketDimension;
        Exiled.Events.Handlers.Player.ReceivingEffect -= OnRecievingEffect;
    }

    public override void OnRoundRestarting()
    {
        _pocketCorrodingPlayers.Clear();
    }

    private void OnEscapingPocketDimension(Exiled.Events.EventArgs.Player.EscapingPocketDimensionEventArgs ev)
    {
        if (!_pocketCorrodingPlayers.Contains(ev.Player.UserId)) return;

        Achieve(ev.Player.UserId);
    }

    private void OnRecievingEffect(ReceivingEffectEventArgs ev)
    {
        if (ev.Effect.GetEffectType() != EffectType.PocketCorroding) return;

        _pocketCorrodingPlayers.Add(ev.Player.UserId);

        Timing.CallDelayed(10f, () =>
        {
            _pocketCorrodingPlayers.Remove(ev.Player.UserId);
        });
    }
}