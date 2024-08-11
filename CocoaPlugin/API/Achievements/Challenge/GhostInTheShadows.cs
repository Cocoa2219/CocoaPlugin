using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace CocoaPlugin.API.Achievements.Challenge;

public class GhostInTheShadows : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.GhostInTheShadows;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Challenge];
    public override string Name { get; set; } = "그림자 속 유령";
    public override string Description { get; set; } = "한 게임에서 한 번도 SCP의 시야에 들어가지 않고 탈출하십시오.";

    private HashSet<string> _inSightPlayers;

    private CoroutineHandle _coroutine;

    public override void RegisterEvents()
    {
        _inSightPlayers = new HashSet<string>();

        Exiled.Events.Handlers.Player.Escaping += OnEscaping;
        Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
    }

    public override void UnregisterEvents()
    {
        Timing.KillCoroutines(_coroutine);

        Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
        Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted()
    {
        _coroutine = Timing.RunCoroutine(CheckInSight());
    }

    private IEnumerator<float> CheckInSight()
    {
        while (!Round.IsEnded)
        {
            foreach (var player in Player.Get(Side.Scp))
            {
                if (player == null) continue;

                foreach (var human in Player.List.Where(x => x.IsHuman))
                {
                    if (SightManager.Get(player).IsSeen(human))
                    {
                        _inSightPlayers.Add(human.UserId);
                    }
                }
            }

            yield return Timing.WaitForSeconds(0.1f);
        }
    }

    private void OnEscaping(EscapingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        if (!_inSightPlayers.Any())
        {
            Achieve(ev.Player.UserId);
        }
    }

    public override void OnRoundRestarting()
    {
        _inSightPlayers.Clear();

        Timing.KillCoroutines(_coroutine);
    }
}