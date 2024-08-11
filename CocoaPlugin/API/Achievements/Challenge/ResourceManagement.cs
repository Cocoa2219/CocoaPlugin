using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using MEC;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.API.Achievements.Challenge;

public class ResourceManagement : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.ResourceManagement;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Challenge];
    public override string Name { get; set; } = "자원 관리";
    public override string Description { get; set; } = "아이템을 버리거나 사용하지 않고 라운드를 끝내십시오.";

    private HashSet<string> _startPlayers;
    private CoroutineHandle _coroutine;

    public override void RegisterEvents()
    {
        _startPlayers = [];

        Server.RoundStarted += OnRoundStarted;
        Server.RoundEnded += OnRoundEnded;
    }

    public override void UnregisterEvents()
    {
        Server.RoundStarted -= OnRoundStarted;
        Server.RoundEnded -= OnRoundEnded;

        Timing.KillCoroutines(_coroutine);
    }

    private void OnRoundStarted()
    {
        _startPlayers.Clear();

        foreach (var player in Player.List)
        {
            _startPlayers.Add(player.UserId);
        }

        _coroutine = Timing.RunCoroutine(CheckItems());
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        foreach (var player in _startPlayers)
        {
            Achieve(player);
        }
    }

    public override void OnRoundRestarting()
    {
        _startPlayers.Clear();

        Timing.KillCoroutines(_coroutine);
    }

    private IEnumerator<float> CheckItems()
    {
        yield return Timing.WaitUntilTrue(() => Round.IsStarted);

        var items = new Dictionary<string, int>();
        var ammo = new Dictionary<string, Dictionary<ItemType,ushort>>();

        while (!Round.IsEnded)
        {
            foreach (var player in Player.List)
            {
                if (player == null) continue;

                items.TryAdd(player.UserId, 0);

                ammo.TryAdd(player.UserId, []);

                if (player.Items.Count < items[player.UserId])
                {
                    _startPlayers.Remove(player.UserId);
                }

                if (player.Ammo.Any(x => x.Value < ammo[player.UserId][x.Key]))
                {
                    _startPlayers.Remove(player.UserId);
                }
            }

            yield return Timing.WaitForOneFrame;
        }
    }
}