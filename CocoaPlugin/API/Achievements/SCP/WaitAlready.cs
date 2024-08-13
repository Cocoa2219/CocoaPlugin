using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;
using MEC;

namespace CocoaPlugin.API.Achievements.SCP;

public class WaitAlready : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.WaitAlready;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Scp];
    public override string Name { get; set; } = "잠깐... 벌써?!";
    public override string Description { get; set; } = "SCP로 스폰한 후 20초 이내에 플레이어를 사살하십시오.";

    private HashSet<string> _spawnedPlayers;

    public override void RegisterEvents()
    {
        _spawnedPlayers = new HashSet<string>();

        Player.Spawned += OnSpawning;
        Player.Dying += OnDying;
    }

    public override void UnregisterEvents()
    {
        Player.Spawned -= OnSpawning;
        Player.Dying -= OnDying;
    }

    private void OnSpawning(SpawnedEventArgs ev)
    {
        if (ev.Player == null) return;
        if (!ev.Player.IsScp) return;

        _spawnedPlayers.Add(ev.Player.UserId);

        Timing.CallDelayed(20f, () =>
        {
            if (_spawnedPlayers.Contains(ev.Player.UserId))
            {
                _spawnedPlayers.Remove(ev.Player.UserId);
            }
        });
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Attacker == null || ev.Player == null) return;

        if (_spawnedPlayers.Contains(ev.Attacker.UserId))
        {
            Achieve(ev.Attacker.UserId);
        }
    }

    public override void OnRoundRestarting()
    {
        _spawnedPlayers.Clear();
    }
}