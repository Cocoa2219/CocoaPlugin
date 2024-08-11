using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using Exiled.API.Features;
using MEC;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.API.Achievements.Survival;

public class JustWasAHuman : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.JustWasAHuman;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "인간 (이었던 것)";
    public override string Description { get; set; } = "한 게임에서 5분 동안 체력의 10% 이하로 생존하십시오.";

    private CoroutineHandle _coroutine;
    private Dictionary<string, int> _times = new();

    public override void RegisterEvents()
    {
        Server.RoundStarted += OnRoundStarted;
    }

    public override void UnregisterEvents()
    {
    }

    public override void OnRoundRestarting()
    {
        Timing.KillCoroutines(_coroutine);

        _times.Clear();
    }

    public void OnRoundStarted()
    {
        _coroutine = Timing.RunCoroutine(Hurt());
    }

    private IEnumerator<float> Hurt()
    {
        while (!Round.IsEnded)
        {
            foreach (var player in Player.List.Where(x => x.IsHuman))
            {
                _times.TryAdd(player.UserId, 0);

                if (player.Health < player.MaxHealth * 0.1f)
                {
                    _times[player.UserId]++;

                    if (_times[player.UserId] >= 300)
                    {
                        Achieve(player.UserId);
                    }
                }
                else
                {
                    _times[player.UserId] = 0;
                }
            }

            yield return Timing.WaitForSeconds(1f);
        }
    }
}