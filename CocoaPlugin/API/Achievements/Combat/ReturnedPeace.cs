using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.Events.Handlers;
using PlayerRoles;
using Player = Exiled.API.Features.Player;

namespace CocoaPlugin.API.Achievements.Combat;

public class ReturnedPeace : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.ReturnedPeace;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Combat];
    public override string Name { get; set; } = "되찾은 평화...?";
    public override string Description { get; set; } = "모든 SCP가 격리되었을 때까지 살아남으십시오.";

    private List<string> _startPlayers;

    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;

        Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;

        Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted()
    {
        _startPlayers = Player.List.Select(x => x.UserId).ToList();
    }

    private void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
    {
        if (ev.Player.IsScp)
        {
            if (!Player.Get(Team.SCPs).Any())
            {
                foreach (var player in _startPlayers.Where(player => Player.Get(player) != null && Player.Get(player).IsAlive))
                {
                    Achieve(player);
                }
            }
        }
    }
}