using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using MultiBroadcast.Commands.Subcommands;

namespace CocoaPlugin.API.Achievements.Survival;

public class UncomfortableCohabitation : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.UncomfortableCohabitation;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "불편한 동거";
    public override string Description { get; set; } = "SCP와 같은 방에 10초 이상 생존하십시오.";

    private Dictionary<string, int> _timeInRoom;
    private CoroutineHandle _coroutine;

    public override void RegisterEvents()
    {
        _timeInRoom = new Dictionary<string, int>();

        Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
    }

    public override void OnRoundRestarting()
    {
        _timeInRoom.Clear();

        Timing.KillCoroutines(_coroutine);
    }

    private void OnRoundStarted()
    {
        _coroutine = Timing.RunCoroutine(TimeInRoom());
    }

    private List<RoomType> _ignoredRooms =
    [
        RoomType.Lcz914,
        RoomType.Surface
    ];

    private IEnumerator<float> TimeInRoom()
    {
        while (!Round.IsEnded)
        {
            foreach (var player in Player.List.Where(x => x.IsHuman))
            {
                if (_ignoredRooms.Contains(player.CurrentRoom.Type)) continue;

                _timeInRoom.TryAdd(player.UserId, 0);

                if (Room.Get(player.CurrentRoom.Type).Players.Count(x => x.IsScp) == 0) continue;

                _timeInRoom[player.UserId]++;

                if (_timeInRoom[player.UserId] >= 100)
                {
                    Achieve(player.UserId);
                }
            }

            yield return Timing.WaitForSeconds(0.1f);
        }
    }
}