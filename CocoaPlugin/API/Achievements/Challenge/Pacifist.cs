using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Handlers;
using MultiBroadcast.Commands.Subcommands;

namespace CocoaPlugin.API.Achievements.Challenge;

public class Pacifist : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.Pacifist;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Challenge];
    public override string Name { get; set; } = "평화주의자";
    public override string Description { get; set; } = "한 게임에서 아무에게 피해를 입히지 않고 라운드를 끝내십시오.";

    private List<string> _attackedPlayers;
    private List<string> _startPlayers;

    public override void RegisterEvents()
    {
        _attackedPlayers = new List<string>();

        Player.Hurting += OnHurting;
        Player.Dying += OnDying;
        Server.RoundEnded += OnRoundEnded;
        Server.RoundStarted += OnRoundStarted;
    }

    public override void UnregisterEvents()
    {
        Player.Hurting -= OnHurting;
        Player.Dying -= OnDying;
        Server.RoundEnded -= OnRoundEnded;
        Server.RoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted()
    {
        _startPlayers = [];

        foreach (var player in Exiled.API.Features.Player.List)
        {
            _startPlayers.Add(player.UserId);
        }
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker == null || ev.Player == null) return;

        if (!_attackedPlayers.Contains(ev.Attacker.UserId))
            _attackedPlayers.Add(ev.Attacker.UserId);
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker == null) return;

        if (!_attackedPlayers.Contains(ev.Attacker.UserId))
            Achieve(ev.Attacker.UserId);
    }

    private bool _endLock;

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        if (_endLock) return;

        _endLock = true;

        foreach (var player in _startPlayers.Where(player => !_attackedPlayers.Contains(player)))
        {
            Achieve(player);
        }
    }

    public override void OnRoundRestarting()
    {
        _attackedPlayers.Clear();
        _startPlayers.Clear();
        _endLock = false;
    }
}