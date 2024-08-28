using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Achievements;
using AdminToys;
using CocoaPlugin.API;
using CocoaPlugin.API.Managers;
using CocoaPlugin.Configs.Broadcast;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Permissions.Extensions;
using MEC;
using Mirror;
using MultiBroadcast.API;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;
using Camera = UnityEngine.Camera;
using Config = CocoaPlugin.Configs.Config;
using LogType = CocoaPlugin.API.LogType;
using NetworkManager = CocoaPlugin.API.Managers.NetworkManager;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Server = Exiled.Events.Handlers.Server;
using Time = CocoaPlugin.API.Time;

namespace CocoaPlugin.EventHandlers;

public class PlayerEvents(CocoaPlugin plugin)
{
    private CocoaPlugin Plugin { get; } = plugin;
    private Config Config => Plugin.Config;

    private List<LeftUser> _leftUsers = [];
    private List<CoroutineHandle> _leftCoroutines = [];

    private HashSet<string> _roundStartUsers = [];

    private Dictionary<string, Stopwatch> _userStopwatches = [];

    private Dictionary<Player, (float time, Room currentRoom)> _currentRooms = [];
    private CoroutineHandle _campingCoroutine;

    internal Dictionary<string, Dictionary<string, Time>> UserTimes = [];
    internal Dictionary<string, Dictionary<string, int>> UserRoundCounts = [];

    private Timer _nextDayTimer;

    internal void SubscribeEvents()
    {
        Exiled.Events.Handlers.Player.Verified += OnVerified;
        Exiled.Events.Handlers.Player.Dying += OnDying;
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;
        Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
        Exiled.Events.Handlers.Player.Left += OnLeft;
        Exiled.Events.Handlers.Player.Handcuffing += OnHandcuffing;
        Exiled.Events.Handlers.Player.Destroying += OnDestroying;
        Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
        Exiled.Events.Handlers.Player.ReservedSlot += OnReservedSlot;

        Server.RestartingRound += OnRestartingRound;
        Server.RoundStarted += OnRoundStarted;
        Server.RoundEnded += OnRoundEnded;

        var today = TodayToString();

        UserTimes.TryAdd(today, new Dictionary<string, Time>());
        UserRoundCounts.TryAdd(today, new Dictionary<string, int>());

        var now = DateTime.Now;
        var nextDay = new DateTime(now.Year, now.Month, now.Day + 1, 0, 0, 0);

        _nextDayTimer = new Timer(OnNextDay, null, nextDay - now, TimeSpan.FromDays(1));
    }

    internal void UnsubscribeEvents()
    {
        Exiled.Events.Handlers.Player.Verified -= OnVerified;
        Exiled.Events.Handlers.Player.Dying -= OnDying;
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
        Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Exiled.Events.Handlers.Player.Handcuffing -= OnHandcuffing;
        Exiled.Events.Handlers.Player.Destroying -= OnDestroying;
        Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
        Exiled.Events.Handlers.Player.ReservedSlot -= OnReservedSlot;

        Server.RestartingRound -= OnRestartingRound;
        Server.RoundStarted -= OnRoundStarted;
        Server.RoundEnded -= OnRoundEnded;

        _nextDayTimer.Dispose();
    }

    internal void OnNextDay(object state)
    {
        var today = TodayToString();

        var flag = UserTimes.TryAdd(today, []) || UserRoundCounts.TryAdd(today, []);

        if (flag)
            foreach (var stopwatch in _userStopwatches)
            {
                stopwatch.Value.Restart();

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
        else
        {
            Timing.CallDelayed(0.1f, () =>
            {
                UserTimes.TryAdd(today, []);
                UserRoundCounts.TryAdd(today, []);

                foreach (var stopwatch in _userStopwatches)
                {
                    stopwatch.Value.Restart();

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
            });
        }
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

        foreach (var coroutine in _leftCoroutines)
        {
            Timing.KillCoroutines(coroutine);
        }

        _leftCoroutines.Clear();
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
        // ev.Player.GameObject.AddComponent<SightManager>();

        BadgeManager.RefreshBadge(ev.Player.UserId, BadgeManager.GetBadge(ev.Player.UserId));
        PenaltyManager.RefreshPenalty(ev.Player.UserId);

        if (ev.Player.IsLinked())
        {
            CheckManager.AddCheck(ev.Player, Check.Today);

            var user = ev.Player.GetUser();

            user.Update(ev.Player);

            CheckManager.SaveChecks();
        }

        var today = TodayToString();

        UserRoundCounts.TryAdd(today, []);
        UserTimes.TryAdd(today, []);

        UserRoundCounts[today].TryAdd(ev.Player.UserId, 0);
        UserTimes[today].TryAdd(ev.Player.UserId, new Time());

        _userStopwatches.TryAdd(ev.Player.UserId, new Stopwatch());
        _userStopwatches[ev.Player.UserId].Start();

        var penaltyCount = ev.Player.GetPenaltyCount();

        ev.Player.AddBroadcast(Config.Broadcasts.VerifiedMessage.Duration, Config.Broadcasts.VerifiedMessage.Format(ev.Player,
            penaltyCount == 0 ? Config.Broadcasts.VerifiedMessageText.Replace("%amount%", UserRoundCounts[today][ev.Player.UserId].ToString()).Replace("%text%", UserTimes[today][ev.Player.UserId].ToString()) : Config.Broadcasts.VerifiedPenaltyText.Replace("%amount%", penaltyCount.ToString())),
            Config.Broadcasts.VerifiedMessage.Priority);

        if (_leftUsers.Any(x => !x.IsReconnected && x.UserId == ev.Player.UserId))
        {
            var leftUser = _leftUsers.First(x => !x.IsReconnected && x.UserId == ev.Player.UserId);

            ev.Player.Role.Set(leftUser.Role, SpawnReason.Respawn, RoleSpawnFlags.All);
            ev.Player.Health = leftUser.Health;
            ev.Player.HumeShield = leftUser.HumeShield;
            ev.Player.ArtificialHealth = leftUser.ArtificalHealth;
            ev.Player.Position = leftUser.Lift?.Position ?? leftUser.Position;

            leftUser.IsReconnected = true;

            NetworkManager.SendLog(new
            {
                Nickname = leftUser.Nickname,
                CustomName = ev.Player.CustomName,
                UserId = leftUser.UserId,
                IpAddress = ev.Player.IPAddress,
            }, LogType.ReconnectSuccess);

            LogManager.WriteLog($"{leftUser.Nickname} ({leftUser.UserId} | {ev.Player.IPAddress}) 재접속 성공");

            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Reconnects.ReconnectMessage.Duration,
                Config.Reconnects.ReconnectMessage.Format(leftUser), Config.Reconnects.ReconnectMessage.Priority);
        }
    }

    internal void OnDying(DyingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        var killType = KillLogs.DamageTypeToKillType(ev.DamageHandler.Type);

        var attackerRole = ev.DamageHandler.AttackerFootprint.Role;
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

            NetworkManager.SendLog(new
            {
                PlayerNickname = ev.Player.Nickname,
                PlayerUserId = ev.Player.UserId,
                PlayerRole = ev.Player.Role.Type,
                AttackerNickname = ev.Attacker.Nickname,
                AttackerUserId = ev.Attacker.UserId,
                AttackerRole = ev.Attacker.Role.Type,
                AttackerIpAddress = ev.Attacker.IPAddress,
            }, LogType.HandcuffedKill);

            LogManager.WriteLog($"{ev.Attacker.Nickname} ({ev.Attacker.UserId} | {ev.Attacker.IPAddress}) - {ev.Attacker.Role.Type}이 {ev.Player.Nickname} ({ev.Player.UserId}) - {ev.Player.Role.Type}을(를) 체포킬");
        }
    }

    internal void OnSpawned(SpawnedEventArgs ev)
    {
        if (ev.Player.IsScp)
        {
            if (Config.Scps.ScpHealth.ContainsKey(ev.Player.Role.Type) && Config.Scps.ScpHealth[ev.Player.Role.Type] > 0)
            {
                ev.Player.MaxHealth = Config.Scps.ScpHealth[ev.Player.Role.Type];
                ev.Player.Health = Config.Scps.ScpHealth[ev.Player.Role.Type];
            }
        }

        Timing.CallDelayed(0.1f, () =>
        {
            if (!ev.Player.IsScp) return;

            if (ev.Player.HasBroadcast($"ScpSpawn_{ev.Player.UserId}"))
            {
                ev.Player.RemoveBroadcast($"ScpSpawn_{ev.Player.UserId}");
            }

            ev.Player.AddBroadcast(Config.Broadcasts.ScpSpawnMessage.Duration, Config.Broadcasts.ScpSpawnMessage.Format(Player.Get(Team.SCPs).ToList()), Config.Broadcasts.ScpSpawnMessage.Priority, $"ScpSpawn_{ev.Player.UserId}");
        });

        if (ev.Player.CameraTransform.gameObject.GetComponent<Camera>() != null) return;

        ev.Player.CameraTransform.gameObject.AddComponent<Camera>();
        ev.Player.CameraTransform.gameObject.AddComponent<ScreenCapture>();
    }

    internal void OnChangingRole(ChangingRoleEventArgs ev)
    {
        if (_currentRooms.ContainsKey(ev.Player)) _currentRooms.Remove(ev.Player);

        if (!Plugin.ServerEvents.LastOneEnabled) return;
        if (ev.Player.Role.Team == Team.Dead) return;
        if (Player.Get(ev.Player.Role.Team).Count() != 2) return;

        var team = ev.Player.Role.Team;
        var player = Player.Get(ev.Player.Role.Team).Except([ev.Player]).First();

        Timing.CallDelayed(0.1f, () =>
        {
            if (Player.Get(ev.Player.Role.Team).Count() > 1) return;

            player.AddBroadcast(Config.Broadcasts.LastOneMessage.Duration, Config.Broadcasts.LastOneMessage.Format(team), Config.Broadcasts.LastOneMessage.Priority);
        });
    }

    internal void OnLeft(LeftEventArgs ev)
    {
        if (ev.Player?.UserId == null) return;

        UserTimes.TryAdd(TodayToString(), []);

        UserTimes[TodayToString()].TryAdd(ev.Player.UserId, new Time());

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
        _leftCoroutines.Add(Timing.RunCoroutine(DestroyCoroutine(ev.Player)));
    }

    internal IEnumerator<float> DestroyCoroutine(Player player)
    {
        var count = _leftUsers.Count(x => x.UserId == player.UserId);

        if (count >= Config.Reconnects.ReconnectLimit)
        {
            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Reconnects.ReconnectLimitMessage.Duration,
                Config.Reconnects.ReconnectLimitMessage.Format(player), Config.Reconnects.ReconnectLimitMessage.Priority);

            NetworkManager.SendLog(new
            {
                Nickname = player.Nickname,
                CustomName = player.CustomName,
                UserId = player.UserId,
                IpAddress = player.IPAddress,
            }, LogType.ReconnectLimit);

            LogManager.WriteLog($"{player.Nickname} ({player.UserId} | {player.IPAddress}) 재접속 제한 초과");

            yield break;
        }

        var ip = player.IPAddress;
        var customName = player.CustomName;

        var leftUser = new LeftUser(player);

        _leftUsers.Add(leftUser);

        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Reconnects.QuitMessage.Duration,
            Config.Reconnects.QuitMessage.Format(leftUser), Config.Reconnects.QuitMessage.Priority);

        NetworkManager.SendLog(new
            {
                Nickname = player.Nickname,
                CustomName = player.CustomName,
                UserId = player.UserId,
                IpAddress = player.IPAddress,
            }, LogType.WaitingReconnect);

        LogManager.WriteLog($"{player.Nickname} ({player.UserId} | {ip}) 재접속 대기 중");

        yield return Timing.WaitForSeconds(Config.Reconnects.ReconnectTime);

        if (leftUser.IsReconnected)
        {
            yield break;
        }

        leftUser.IsReconnected = true;

        NetworkManager.SendLog(new
        {
            Nickname = leftUser.Nickname,
            CustomName = customName,
            UserId = leftUser.UserId,
            IpAddress = ip,
        }, LogType.ReconnectFailed);

        LogManager.WriteLog($"{leftUser.Nickname} ({leftUser.UserId} | {ip}) 재접속 실패");

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

    private bool IsCampImmune(Player player)
    {
        if (player == null) return true;
        if (player.IsNPC) return true;
        if (player.IsDead) return true;
        if (player.IsGodModeEnabled && Config.Camping.IgnoreGodmode) return true;
        if (player.Role.Is(out FpcRole fpcRole) && fpcRole.IsNoclipEnabled && Config.Camping.IgnoreNoclip) return true;
        return Config.Camping.ExcludedRoles.Contains(player.Role.Type) || player.CheckPermission("cocoa.camping.ignore");
    }

    private IEnumerator<float> CampingCoroutine()
    {
        while (!Round.IsEnded)
        {
            yield return Timing.WaitForSeconds(Config.Camping.CampingCheckInterval);

            foreach (var player in Player.List.Where(x => x.IsHuman && Player.Get(Team.SCPs).All(y => Vector3.Distance(x.Position, y.Position) > Config.Camping.CampingScpDistance)))
            {
                if (IsCampImmune(player)) continue;

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

    private void OnDestroying(DestroyingEventArgs ev)
    {
        if (Player.List.Where(x => x.Role.Type == RoleTypeId.Scp049).Any(scp049 =>
                scp049.Role.As<Scp049Role>().RecallingPlayer == ev.Player &&
                scp049.Role.As<Scp049Role>().IsRecalling) && !Round.IsEnded)
        {
            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.LeftWhileReviving.Duration,
                Config.Broadcasts.LeftWhileReviving.Format(ev.Player), Config.Broadcasts.LeftWhileReviving.Priority);

            NetworkManager.SendLog(new
            {
                Nickname = ev.Player.Nickname,
                CustomName = ev.Player.CustomName,
                UserId = ev.Player.UserId,
                IpAddress = ev.Player.IPAddress,
            }, LogType.LeftWhileReviving);

            LogManager.WriteLog($"{ev.Player.Nickname} ({ev.Player.UserId} | {ev.Player.IPAddress}) 소생 중 탈주");
        }

        // Object.Destroy(ev.Player.GameObject.GetComponent<SightManager>());
    }

    internal void OnReservedSlot(ReservedSlotsCheckEventArgs ev)
    {
        if (ReservedSlotManager.Get(ev.UserId))
        {
            ev.Result = ReservedSlotEventResult.AllowConnectionUnconditionally;
        }
    }

    internal void OnInteractingDoor(InteractingDoorEventArgs ev)
    {
        if (Player.List.Where(x => Vector3.Distance(ev.Player.Position, x.Position) < 30f).Any(x => x.Role.Team != ev.Player.Role.Team))
        {
            return;
        }

        Timing.RunCoroutine(DoorTrolling(ev));

        // if (ev.IsAllowed && ev.Door.Base.NetworkTargetState)
        // {
        //     if (TDoor == null) TDoor = new Dictionary<Door, Player>();
        //     if (!TDoor.ContainsKey(ev.Door))
        //     {
        //         TDoor.Add(ev.Door, ev.Player);
        //
        //         Timing.CallDelayed(4, () =>
        //         {
        //             if (TDoor.ContainsKey(ev.Door) && TDoor[ev.Door] == ev.Player)
        //             {
        //                 TDoor.Remove(ev.Door);
        //             }
        //         });
        //     }
        //     else
        //     {
        //         CheckDoorTrolling(ev.Door,ev.Player);
        //     }
        // }
        // else
        // {
        //     CheckDoorTrolling(ev.Door, ev.Player);
        // }

        // ev.Player.ChangeAppearance(RoleTypeId.Spectator);
    }

    internal IEnumerator<float> DoorTrolling(InteractingDoorEventArgs ev)
    {
        if (!ev.Door.Base.NetworkTargetState) yield break;

        var colliders = new Collider[1024];
        var players = new HashSet<Player>();

        var num = Physics.OverlapSphereNonAlloc(ev.Door.Position + new Vector3(0f, 1.2f, 0f),
            Config.Others.DoorTrollingSphereRadius, colliders);

        for (var i = 0; i < num; i++)
        {
            var player = Player.Get(colliders[i].GetComponentInParent<ReferenceHub>());

            if (player == null) continue;

            players.Add(player);
        }

        foreach (var player in players.ToList())
        {
            var curDis = Vector3.Distance(player.Position, ev.Door.Position);

            yield return Timing.WaitForSeconds(0.05f);

            if (Vector3.Distance(player.Position, ev.Door.Position) >= curDis)
            {
                players.Remove(player);
            }
        }

        var friendly = players.Where(x => x.LeadingTeam == ev.Player.LeadingTeam && x != ev.Player).ToList();

        if (friendly.Count == 0) yield break;

        var troller = ev.Player;

        if (troller.HasBroadcast("DoorTrolling"))
        {
            troller.RemoveBroadcast("DoorTrolling");
        }

        troller.AddBroadcast(Config.Others.DoorTrollingMessage.Duration, Config.Others.DoorTrollingMessage.Message, Config.Others.DoorTrollingMessage.Priority, "DoorTrolling");

        NetworkManager.SendLog(new
        {
            Nickname = troller.Nickname,
            UserId = troller.UserId,
            Targets = friendly.Select(x => new
            {
                Nickname = x.Nickname
            }),
        }, LogType.DoorTrolling);

        LogManager.WriteLog($"{troller.Nickname} ({troller.UserId}) 문트롤 - 대상: {string.Join(", ", friendly.Select(x => x.Nickname))}");
    }

    // public Dictionary<Door, Player> TDoor = new Dictionary<Door, Player>();
    //
    // private void CheckDoorTrolling(Door door, Player player)
    // {
    //     if (TDoor.ContainsKey(door))
    //     {
    //         if (TDoor[door] != player && TDoor[door].Role.Team == player.Role.Team)
    //         {
    //             if (Vector3.Distance(door.Position, player.Position) < Vector3.Distance(TDoor[door].Position, player.Position))
    //             {
    //                 // ProcessSTT.SendData($"🚷 문트롤 의심. {TDoor[door].Nickname} ({TDoor[door].UserId}) -{TDoor[door].Role} -> {player.Nickname} ({player.UserId}) - {player.Role}", HandleQueue.CommandLogChannelId);
    //                 // RankSystem.RankPointManager.AddPoint(TDoor[door].UserId, -1, ((int)TDoor[door].Role.GetFaction() + 2) % 3);
    //                 TDoor.Remove(door);
    //
    //                 Log.Info("🚷 문트롤 의심. {TDoor[door].Nickname} ({TDoor[door].UserId}) -{TDoor[door].Role} -> {player.Nickname} ({player.UserId}) - {player.Role}");
    //             }
    //         }
    //     }
    // }

    private IEnumerator<float> SendTypingText(Player player, string text, ushort time, float delay = 0.1f, byte priority = 0, string tag = "")
    {
        if (!KoreanTyperStringExtensions.ContainsTags(text))
        {
            for (var i = 1; i <= text.GetTypingLength(); i++)
            {
                player.AddBroadcast(time, text.Typing(i), priority, tag);
                yield return Timing.WaitForSeconds(delay);
            }
            yield break;
        }

        var groups = KoreanTyperStringExtensions.GroupCharactersWithinSameTag(text);

        var totalTypingLength = groups.Sum(x => x.GetTypingLength());

        var broadcast = player.AddBroadcast((ushort)(time + totalTypingLength), text, priority, tag);

        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var count = group.GetTypingLength();

            for (var j = 1; j <= count; j++)
            {
                var newText = group.Typing(j);

                var newTaggedText = KoreanTyperStringExtensions.ReplaceTaggedText(text, i, newText);

                for (var k = groups.Count - 1; k > i; k--)
                {
                    newTaggedText = KoreanTyperStringExtensions.ReplaceTaggedText(newTaggedText, k, "");
                }

                broadcast.Edit(newTaggedText);

                yield return Timing.WaitForSeconds(delay);
            }
        }
    }
}

public class ScreenCapture : MonoBehaviour
{
    private Camera _camera;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    public void CaptureScreen()
    {
        var renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                useMipMap = false,
                antiAliasing = 1
            };
        renderTexture.Create();

        _camera.targetTexture = renderTexture;

        var screenShot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);

        _camera.Render();

        var originalRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        screenShot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenShot.Apply();

        var bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(Paths.Plugins, "screenshot.png"), bytes);

        _camera.targetTexture = null;
        RenderTexture.active = originalRT;

        Destroy(renderTexture);
        Destroy(screenShot);
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class CaptureScreenCommand : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var target = Player.Get(arguments.At(0));

        if (target == null)
        {
            response = "플레이어를 찾을 수 없습니다.";
            return false;
        }

        var screenCapture = target.CameraTransform.gameObject.GetComponent<ScreenCapture>();

        if (screenCapture == null)
        {
            response = "화면 캡쳐 컴포넌트를 찾을 수 없습니다.";
            return false;
        }

        screenCapture.CaptureScreen();

        response = "화면을 캡쳐했습니다.";
        return true;
    }

    public string Command { get; } = "capturescreen";
    public string[] Aliases { get; } = { "cs" };
    public string Description { get; } = "화면을 캡쳐합니다.";
}