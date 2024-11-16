using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CocoaPlugin.API;
using CocoaPlugin.API.Managers;
using CocoaPlugin.Commands;
using CocoaPlugin.Configs.Broadcast;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp330;
using Exiled.Events.EventArgs.Server;
using Exiled.Permissions.Extensions;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using MultiBroadcast.API;
using PlayerRoles;
using SLPlayerRotation;
using UnityEngine;
using Camera = UnityEngine.Camera;
using Config = CocoaPlugin.Configs.Config;
using LogType = CocoaPlugin.API.LogType;
using NetworkManager = CocoaPlugin.API.Managers.NetworkManager;
using Random = UnityEngine.Random;
using Server = Exiled.Events.Handlers.Server;
using Time = CocoaPlugin.API.Time;
// ReSharper disable RedundantAnonymousTypePropertyName

namespace CocoaPlugin.Handlers;

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
        Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting;
        Exiled.Events.Handlers.Player.Hurt += AssistManager.OnHurt;
        Exiled.Events.Handlers.Player.Dying += AssistManager.OnDying;
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.Shot += OnShot;
        Exiled.Events.Handlers.Scp330.InteractingScp330 += OnInteractingScp330;

        Server.RestartingRound += OnRestartingRound;
        Server.RoundStarted += OnRoundStarted;
        Server.RoundEnded += OnRoundEnded;

        var today = TodayToString();

        UserTimes.TryAdd(today, new Dictionary<string, Time>());
        UserRoundCounts.TryAdd(today, new Dictionary<string, int>());

        var now = DateTime.Now;
        var nextDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);

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
        Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting;
        Exiled.Events.Handlers.Player.Hurt -= AssistManager.OnHurt;
        Exiled.Events.Handlers.Player.Dying -= AssistManager.OnDying;
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.Shot -= OnShot;
        Exiled.Events.Handlers.Scp330.InteractingScp330 -= OnInteractingScp330;

        Server.RestartingRound -= OnRestartingRound;
        Server.RoundStarted -= OnRoundStarted;
        Server.RoundEnded -= OnRoundEnded;

        _nextDayTimer.Dispose();
    }

    private CandyKindID GetCandy()
    {
        var spawnTable = Config.Scps.Scp330.CandyChances;

        var total = spawnTable.Sum(x => x.Value);

        var random = Random.Range(0, total);

        foreach (var (key, value) in spawnTable)
        {
            random -= value;
            if (random <= 0)
            {
                return key;
            }
        }

        return CandyKindID.None;
    }

    internal void OnInteractingScp330(InteractingScp330EventArgs ev)
    {
        ev.Candy = GetCandy();
    }

    internal void OnShooting(ShootingEventArgs ev)
    {
        if (!ZeroAim._zeroAimPlayers.Contains(ev.Player.ReferenceHub)) return;

        // Log.Info($"{ev.Player.Nickname}'s original before shooting forward: {ev.Player.CameraTransform.forward}");
        _lastRotations[ev.Player] = ev.Player.CameraTransform.forward;
    }

    internal void OnShot(ShotEventArgs ev)
    {
        if (!ZeroAim._zeroAimPlayers.Contains(ev.Player.ReferenceHub)) return;

        // Log.Info($"{ev.Player.Nickname}'s original after shot forward: {ev.Player.CameraTransform.forward}, restoring to {_lastRotations[ev.Player]}");
        ev.Player.SetRotation(_lastRotations[ev.Player]);
    }

    private Dictionary<Player, Vector3> _lastRotations = new();

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
        VoiceGroup.OnRestartingRound();
        AssistManager.OnRestartingRound();

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

            CheckManager.AddCheck(user);
        }
    }

    internal void OnVerified(VerifiedEventArgs ev)
    {
        // ev.Player.GameObject.AddComponent<SightManager>();

        // BadgeManager.RefreshBadge(ev.Player.UserId, BadgeManager.GetBadge(ev.Player.UserId));
        PenaltyManager.RefreshPenalty(ev.Player.UserId);

        if (ev.Player.IsLinked())
        {
            var user = ev.Player.GetUser();

            user.Update(ev.Player);
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
        //
        // if (ev.Player.CameraTransform.gameObject.GetComponent<Camera>() != null) return;
        //
        // ev.Player.CameraTransform.gameObject.AddComponent<Camera>();
        //
        // if (ev.Player.CameraTransform.gameObject.GetComponent<ScreenCapture>() != null) return;
        //
        // ev.Player.CameraTransform.gameObject.AddComponent<ScreenCapture>();
        // ev.Player.CameraTransform.gameObject.GetComponent<ScreenCapture>().Initialize(ev.Player);
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

        if (leftUser.Role == RoleTypeId.Scp0492) yield break;

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
        if (Plugin.ShootingRange.Instances.Any(x => x.Player == player)) return true;
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
    }

    public class DoorTrollingEventArgs(Door door, Player player, HashSet<Player> players)
    {
        public Door Door { get; } = door;
        public Player Player { get; } = player;
        public HashSet<Player> Players { get; } = players;
    }

    public static Action<DoorTrollingEventArgs> OnDoorTrolling;

    internal IEnumerator<float> DoorTrolling(InteractingDoorEventArgs ev)
    {
        if (!ev.Door.Base.NetworkTargetState) yield break;

        var colliders = new Collider[1024];
        var players = new HashSet<Player>();

        var num = Physics.OverlapSphereNonAlloc(ev.Door.Position + new Vector3(0f, 1.2f, 0f),
            Config.Others.DoorTrollingSphereRadius, colliders, LayerMask.GetMask("Hitbox"));

        for (var i = 0; i < num; i++)
        {
            var player = Player.Get(colliders[i].GetComponentInParent<ReferenceHub>());

            if (player == null) continue;

            players.Add(player);
        }

        var playersRemoval = new HashSet<Player>(players);

        foreach (var player in players)
        {
            var curDis = Vector3.Distance(player.Position, ev.Door.Position);

            // Not using WaitForSeconds because the more players, it will not be accurate (too much delay)
            // Instead, using CallDelayed with 0.05f delay to schedule the check and pass
            Timing.CallDelayed(.05f, () =>
            {
                if (Vector3.Distance(player.Position, ev.Door.Position) >= curDis)
                {
                    playersRemoval.Remove(player);
                }
            });
        }

        // Wait for 0.05f to make sure the scheduled check is done
        yield return Timing.WaitForSeconds(0.05f);

        // Wait for more 0.01f to make sure the scheduled check is done
        yield return Timing.WaitForSeconds(0.01f);

        players = playersRemoval;

        var friendly = players.Where(x => x.LeadingTeam == ev.Player.LeadingTeam && x != ev.Player).ToHashSet();

        if (friendly.Count == 0) yield break;

        var troller = ev.Player;

        if (troller.HasBroadcast("DoorTrolling"))
        {
            troller.RemoveBroadcast("DoorTrolling");
        }

        troller.AddBroadcast(Config.Others.DoorTrollingMessage.Duration, Config.Others.DoorTrollingMessage.Message, Config.Others.DoorTrollingMessage.Priority, "DoorTrolling");

        OnDoorTrolling?.Invoke(new DoorTrollingEventArgs(ev.Door, troller, friendly));

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

    internal void OnVoiceChatting(VoiceChattingEventArgs ev)
    {
        VoiceGroup.OnVoiceChatting(ev);
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
}

public class ScreenCapture : MonoBehaviour
{
    private Camera _camera;
    private VideoEncoder _videoEncoder;

    private RenderTexture _renderTexture;
    private Texture2D _screenShot;

    public bool _isRecording;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    public void Initialize(Player player, int width = 1920, int height = 1080)
    {
        _videoEncoder = new VideoEncoder(player);

        _renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            useMipMap = true,
        };
        _renderTexture.Create();

        _screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    public void CaptureScreen(string output)
    {
        var sw = Stopwatch.StartNew();
        Log.Info("Capturing screen started | " + output);

        if (_renderTexture == null || _screenShot == null)
        {
            throw new InvalidOperationException("CaptureScreen called before InitializeCapture");
        }

        _camera.targetTexture = _renderTexture;

        _camera.Render();

        var originalRT = RenderTexture.active;
        RenderTexture.active = _renderTexture;

        _screenShot.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
        _screenShot.Apply();

        var bytes = _screenShot.EncodeToPNG();

        File.WriteAllBytes(output, bytes);

        _camera.targetTexture = null;
        RenderTexture.active = originalRT;

        sw.Stop();
        Log.Info("Capturing screen finished in " + sw.ElapsedMilliseconds + "ms" + " | " + output);
    }

    public void StartRecording()
    {
        if (_isRecording) return;

        _isRecording = true;

        Timing.RunCoroutine(Record());
    }

    private float _actualFramerate;
    private IEnumerator<float> Record()
    {
        var index = 0;
        var startTime = UnityEngine.Time.realtimeSinceStartup;

        while (_isRecording)
        {
            CaptureScreen(Path.Combine(_videoEncoder.ImageDirectory, $"image_{index}.png"));
            index++;

            yield return Timing.WaitForSeconds(1f / 60f);
        }

        var endTime = UnityEngine.Time.realtimeSinceStartup;
        var totalTime = endTime - startTime;

        _actualFramerate = index / totalTime;

        Log.Info("Captured " + index + " frames in " + totalTime + " seconds. Actual framerate: " + _actualFramerate);

        Task.Run(() => _videoEncoder.Encode(() =>
        {
            Log.Info("Encoding complete. Saved to " + _videoEncoder.VideoOutputPath);
        }, index / totalTime));
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        _isRecording = false;
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        if (_screenShot != null)
        {
            Destroy(_screenShot);
            _screenShot = null;
        }
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

        if (screenCapture._isRecording)
        {
            screenCapture.StopRecording();
            response = "녹화를 중지했습니다.";
            return true;
        }

        screenCapture.StartRecording();

        response = "녹화를 시작했습니다.";
        return true;
    }

    public string Command { get; } = "capturescreen";
    public string[] Aliases { get; } = { "cs" };
    public string Description { get; } = "화면을 캡쳐합니다.";
}