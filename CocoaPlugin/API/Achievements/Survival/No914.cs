using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using MEC;

namespace CocoaPlugin.API.Achievements.Survival;

public class No914 : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.No914;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "914? 그게 뭔데?!";
    public override string Description { get; set; } = "한 게임에서 SCP-914에 들어가지 않고 라운드를 끝내십시오.";

    private CoroutineHandle _coroutine;
    private HashSet<string> _players;
    private HashSet<string> _startPlayers;

    public override void RegisterEvents()
    {
        _players = new HashSet<string>();

        Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
        Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
        Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
    }

    private void OnRoundStarted()
    {
        _startPlayers = [];

        foreach (var player in Player.List)
        {
            _startPlayers.Add(player.UserId);
        }

        _coroutine = Timing.RunCoroutine(Check914());
    }

    private IEnumerator<float> Check914()
    {
        yield return Timing.WaitUntilTrue(() => Round.IsStarted);

        var scp914 = Room.Get(RoomType.Lcz914);

        while (!Round.IsEnded)
        {
            foreach (var pl in scp914.Players)
            {
                _players.Add(pl.UserId);
            }

            yield return Timing.WaitForSeconds(0.1f);
        }
    }

    public override void OnRoundRestarting()
    {
        _players.Clear();
        _endLock = false;

        Timing.KillCoroutines(_coroutine);
    }

    private bool _endLock;

    public void OnRoundEnded(RoundEndedEventArgs ev)
    {
        if (_endLock) return;

        _endLock = true;

        foreach (var player in _startPlayers.Where(player => !_players.Contains(player)))
        {
            Achieve(player);
        }
    }
}