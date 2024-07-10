using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CocoaPlugin.API;
using CocoaPlugin.Configs.Broadcast;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using GameCore;
using MEC;
using MultiBroadcast.API;
using PlayerRoles;
using UnityEngine;
using Config = CocoaPlugin.Configs.Config;
using Random = UnityEngine.Random;
using Server = Exiled.Events.Handlers.Server;
using Time = CocoaPlugin.API.Time;

namespace CocoaPlugin.EventHandlers;

public class PlayerEvents(CocoaPlugin plugin)
{
    private CocoaPlugin Plugin { get; } = plugin;
    private Config Config => Plugin.Config;

    private List<LeftUser> _leftUsers = [];

    private HashSet<string> _roundStartUsers = [];

    private Dictionary<string, Stopwatch> _userStopwatches = [];

    private Dictionary<Player, (float time, Room currentRoom)> _currentRooms = [];
    private CoroutineHandle _campingCoroutine;

    internal Dictionary<string, Dictionary<string, Time>> UserTimes = [];
    internal Dictionary<string, Dictionary<string, int>> UserRoundCounts = [];

    internal void SubscribeEvents()
    {
        Exiled.Events.Handlers.Player.Verified += OnVerified;
        Exiled.Events.Handlers.Player.Dying += OnDying;
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;
        Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
        Exiled.Events.Handlers.Player.Left += OnLeft;
        Exiled.Events.Handlers.Player.Handcuffing += OnHandcuffing;

        Server.RestartingRound += OnRestartingRound;
        Server.RoundStarted += OnRoundStarted;
        Server.RoundEnded += OnRoundEnded;
    }

    internal void UnsubscribeEvents()
    {
        Exiled.Events.Handlers.Player.Verified -= OnVerified;
        Exiled.Events.Handlers.Player.Dying -= OnDying;
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
        Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Exiled.Events.Handlers.Player.Handcuffing -= OnHandcuffing;

        Server.RestartingRound -= OnRestartingRound;
        Server.RoundStarted -= OnRoundStarted;
        Server.RoundEnded -= OnRoundEnded;
    }

    internal void OnRestartingRound()
    {
        var today = TodayToString();

        UserTimes.TryAdd(today, []);

        foreach (var stopwatch in _userStopwatches)
        {
            stopwatch.Value.Stop();

            var time = new Time
            {
                Hours = stopwatch.Value.Elapsed.Hours,
                Minutes = stopwatch.Value.Elapsed.Minutes,
                Seconds = stopwatch.Value.Elapsed.Seconds
            };

            if (!UserTimes[today].TryAdd(stopwatch.Key, time))
            {
                UserTimes[today][stopwatch.Key] += time;
            }
        }

        _leftUsers.Clear();
        _userStopwatches.Clear();

        Timing.KillCoroutines(_campingCoroutine);
        _currentRooms.Clear();
    }

    internal void OnRoundStarted()
    {
        _roundStartUsers = Player.List.Select(x => x.UserId).ToHashSet();

        _campingCoroutine = Timing.RunCoroutine(CampingCoroutine());
    }

    internal string TodayToString()
    {
        var today = DateTime.Now;

        return $"{today.Year}-{today.Month}-{today.Day}";
    }

    internal void OnRoundEnded(RoundEndedEventArgs ev)
    {
        var today = TodayToString();

        UserRoundCounts.TryAdd(today, []);

        foreach (var user in Player.List)
        {
            if (!_roundStartUsers.Contains(user.UserId)) continue;

            UserRoundCounts[today].TryAdd(user.UserId, 0);
            UserRoundCounts[today][user.UserId]++;
        }
    }

    internal void OnVerified(VerifiedEventArgs ev)
    {
        var today = TodayToString();

        UserRoundCounts.TryAdd(today, []);
        UserTimes.TryAdd(today, []);

        UserRoundCounts[today].TryAdd(ev.Player.UserId, 0);
        UserTimes[today].TryAdd(ev.Player.UserId, new Time());

        _userStopwatches.TryAdd(ev.Player.UserId, new Stopwatch());
        _userStopwatches[ev.Player.UserId].Start();

        ev.Player.AddBroadcast(Config.Broadcasts.VerifiedMessage.Duration, Config.Broadcasts.VerifiedMessage.Format(ev.Player, UserRoundCounts[today][ev.Player.UserId], UserTimes[today][ev.Player.UserId].ToString()), Config.Broadcasts.VerifiedMessage.Priority);

        if (_leftUsers.Any(x => !x.IsReconnected && x.UserId == ev.Player.UserId))
        {
            var leftUser = _leftUsers.First(x => !x.IsReconnected && x.UserId == ev.Player.UserId);

            ev.Player.Role.Set(leftUser.Role, SpawnReason.Respawn, RoleSpawnFlags.All);
            ev.Player.Health = leftUser.Health;
            ev.Player.HumeShield = leftUser.HumeShield;
            ev.Player.ArtificialHealth = leftUser.ArtificalHealth;
            ev.Player.Position = leftUser.Lift?.Position ?? leftUser.Position;

            leftUser.IsReconnected = true;

            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Reconnects.ReconnectMessage.Duration,
                Config.Reconnects.ReconnectMessage.Format(leftUser), Config.Reconnects.ReconnectMessage.Priority);
        }
    }

    internal void OnDying(DyingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        var killType = KillLogs.DamageTypeToKillType(ev.DamageHandler.Type);

        var attackerRole = ev.Attacker?.Role.Type;
        var targetRole = ev.Player.Role.Type;

        Timing.CallDelayed(0.1f, () =>
        {
            foreach (var dead in Player.Get(Team.Dead))
            {
                dead.AddBroadcast(Config.Broadcasts.KillLogs.KillLog[killType].Duration,
                    Config.Broadcasts.KillLogs.KillLog[killType].Format(ev.Attacker, ev.Player, attackerRole, targetRole), Config.Broadcasts.KillLogs.KillLog[killType].Priority);
            }
        });

        if (ev.Attacker == null) return;

        if (ev.Attacker.IsScp)
        {
            var amount = Random.Range(Config.Scps.ScpHealMin, Config.Scps.ScpHealMax + 1);

            ev.Attacker.Health += amount;

            ev.Attacker.AddBroadcast(Config.Broadcasts.ScpHealMessage.Duration, Config.Broadcasts.ScpHealMessage.Format(amount), Config.Broadcasts.ScpHealMessage.Priority);
        }

        if (ev.Attacker == ev.Player.Cuffer) return;

        if (ev.Player.IsCuffed)
        {
            var cufferRole = ev.Player.Cuffer.Role.Type;
            var cuffedRole = ev.Player.Role.Type;

            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.HandcuffedKillMessage.Duration, Config.Broadcasts.HandcuffedKillMessage.Format(ev.Attacker, ev.Player, cufferRole, cuffedRole), Config.Broadcasts.HandcuffedKillMessage.Priority);
        }
    }

    internal void OnSpawned(SpawnedEventArgs ev)
    {
        Timing.CallDelayed(0.1f, () =>
        {
            if (!ev.Player.IsScp) return;

            if (ev.Player.HasBroadcast($"ScpSpawn_{ev.Player.UserId}"))
            {
                ev.Player.RemoveBroadcast($"ScpSpawn_{ev.Player.UserId}");
            }

            ev.Player.AddBroadcast(Config.Broadcasts.ScpSpawnMessage.Duration, Config.Broadcasts.ScpSpawnMessage.Format(Player.Get(Team.SCPs).ToList()), Config.Broadcasts.ScpSpawnMessage.Priority, $"ScpSpawn_{ev.Player.UserId}");
        });
    }

    internal void OnChangingRole(ChangingRoleEventArgs ev)
    {
        if (_currentRooms.ContainsKey(ev.Player)) _currentRooms.Remove(ev.Player);

        if (!Plugin.ServerEvents.LastOneEnabled) return;
        if (ev.Player.Role.Team == Team.Dead) return;
        if (Player.Get(ev.Player.Role.Team).Count() != 2) return;

        var player = Player.Get(ev.Player.Role.Team).Except([ev.Player]).First();

        Timing.CallDelayed(0.1f, () =>
        {
            if (Player.Get(ev.Player.Role.Team).Count() > 1) return;

            player.AddBroadcast(Config.Broadcasts.LastOneMessage.Duration, Config.Broadcasts.LastOneMessage.Format(ev.Player.Role.Team), Config.Broadcasts.LastOneMessage.Priority);
        });
    }

    internal void OnLeft(LeftEventArgs ev)
    {
        if (_userStopwatches.ContainsKey(ev.Player.UserId))
        {
            _userStopwatches[ev.Player.UserId].Stop();

            UserTimes[TodayToString()][ev.Player.UserId] += new Time
            {
                Hours = _userStopwatches[ev.Player.UserId].Elapsed.Hours,
                Minutes = _userStopwatches[ev.Player.UserId].Elapsed.Minutes,
                Seconds = _userStopwatches[ev.Player.UserId].Elapsed.Seconds
            };
        }

        if (!Round.InProgress) return;

        if (!ev.Player.IsScp) return;
        Timing.RunCoroutine(DestroyCoroutine(ev.Player));
    }

    internal IEnumerator<float> DestroyCoroutine(Player player)
    {
        var count = _leftUsers.Count(x => x.UserId == player.UserId);

        if (count >= Config.Reconnects.ReconnectLimit)
        {
            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Reconnects.ReconnectLimitMessage.Duration,
                Config.Reconnects.ReconnectLimitMessage.Format(player), Config.Reconnects.ReconnectLimitMessage.Priority);
            yield break;
        }

        var leftUser = new LeftUser(player);

        _leftUsers.Add(leftUser);

        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Reconnects.QuitMessage.Duration,
            Config.Reconnects.QuitMessage.Format(leftUser), Config.Reconnects.QuitMessage.Priority);

        yield return Timing.WaitForSeconds(Config.Reconnects.ReconnectTime);

        if (leftUser.IsReconnected)
        {
            yield break;
        }

        leftUser.IsReconnected = true;

        if (!Player.Get(RoleTypeId.Spectator).Any())
        {
            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Reconnects.ReplaceFailedMessage.Duration,
                Config.Reconnects.ReplaceFailedMessage.Format(leftUser), Config.Reconnects.ReplaceFailedMessage.Priority);

            yield break;
        }

        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Reconnects.ReplaceMessage.Duration,
            Config.Reconnects.ReplaceMessage.Format(leftUser), Config.Reconnects.ReplaceMessage.Priority);

        var target = Player.Get(RoleTypeId.Spectator).GetRandomValue();

        target.Role.Set(leftUser.Role, SpawnReason.Respawn, RoleSpawnFlags.All);
        target.Health = leftUser.Health;
        target.HumeShield = leftUser.HumeShield;
        target.ArtificialHealth = leftUser.ArtificalHealth;
        target.Position = leftUser.Lift?.Position ?? leftUser.Position;
    }

    internal void OnHandcuffing(HandcuffingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        ev.Target.AddBroadcast(Config.Broadcasts.HandcuffMessage.Duration, Config.Broadcasts.HandcuffMessage.Format(ev.Player, ev.Target, ev.Player.Role.Type, ev.Target.Role.Type), Config.Broadcasts.HandcuffMessage.Priority);
        ev.Player.AddBroadcast(Config.Broadcasts.HandcuffMessage.Duration, Config.Broadcasts.HandcuffMessage.Format(ev.Player, ev.Target, ev.Player.Role.Type, ev.Target.Role.Type), Config.Broadcasts.HandcuffMessage.Priority);
    }

    private IEnumerator<float> CampingCoroutine()
    {
        while (!Round.IsEnded)
        {
            yield return Timing.WaitForSeconds(Config.Camping.CampingCheckInterval);

            foreach (var player in Player.List.Where(x => x.IsHuman && Player.Get(Team.SCPs).All(y => Vector3.Distance(x.Position, y.Position) > Config.Camping.CampingScpDistance)))
            {
                if (!_currentRooms.ContainsKey(player)) _currentRooms.Add(player, (0, player.CurrentRoom));

                if (_currentRooms[player].currentRoom != player.CurrentRoom)
                {
                    _currentRooms[player] = (_currentRooms[player].time - Config.Camping.CampingCheckInterval * Config.Camping.CampingTimeToleranceMultiplier, player.CurrentRoom);

                    if (_currentRooms[player].time < 0)
                    {
                        _currentRooms[player] = (0, player.CurrentRoom);
                    }
                }
                else
                {
                    _currentRooms[player] = (_currentRooms[player].time + Config.Camping.CampingCheckInterval, player.CurrentRoom);
                }

                if (_currentRooms[player].time >= Config.Camping.CampingTime && _currentRooms[player].time % Config.Camping.CampingMessageFrequency == 0)
                {
                    player.AddBroadcast(Config.Camping.CampingMessage.Duration, Config.Camping.CampingMessage.Format(player), Config.Camping.CampingMessage.Priority);
                }
            }
        }
    }
}