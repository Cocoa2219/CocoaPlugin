using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Handlers;
using MEC;

namespace CocoaPlugin.API.Achievements.Challenge;

public class Speedrun : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.Speedrun;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Challenge];
    public override string Name { get; set; } = "스피드런";
    public override string Description { get; set; } = "한 게임에서 6분 이내에 라운드를 끝내십시오.";

    private HashSet<string> _startPlayers;
    private CoroutineHandle _coroutine;
    private bool _canAchieve = true;

    public override void RegisterEvents()
    {
        _startPlayers = new HashSet<string>();

        Server.RoundStarted += OnRoundStarted;
        Server.RoundEnded += OnRoundEnded;
    }

    public override void UnregisterEvents()
    {
        Server.RoundStarted -= OnRoundStarted;
        Server.RoundEnded -= OnRoundEnded;
    }

    private void OnRoundStarted()
    {
        _startPlayers.Clear();

        foreach (var player in Exiled.API.Features.Player.List)
        {
            _startPlayers.Add(player.UserId);
        }

        _coroutine = Timing.CallDelayed(360f, () =>
        {
            _canAchieve = false;
        });
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        if (!_canAchieve) return;

        foreach (var player in _startPlayers)
        {
            Achieve(player);
        }
    }

    public override void OnRoundRestarting()
    {
        _startPlayers.Clear();
        _canAchieve = true;

        Timing.KillCoroutines(_coroutine);
    }
}