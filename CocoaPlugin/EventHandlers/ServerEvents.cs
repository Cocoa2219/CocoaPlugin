using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API.Managers;
using CocoaPlugin.Configs;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Permissions.Extensions;
using MEC;
using MultiBroadcast.API;
using PlayerRoles;
using Respawning;
using UnityEngine;
using Random = UnityEngine.Random;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.EventHandlers;

public class ServerEvents(CocoaPlugin plugin)
{
    private CocoaPlugin Plugin { get; } = plugin;
    private Config Config => Plugin.Config;

    internal bool LastOneEnabled { get; private set; }

    private Dictionary<Player, (float time, Vector3 position)> _afkPlayers = new();
    private CoroutineHandle _afkCoroutine;

    private CoroutineHandle _autoNukeCoroutine;
    private CoroutineHandle _autoBroadcastCoroutine;
    private CoroutineHandle _elevatorCoroutine;

    internal void SubscribeEvents()
    {
        Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;

        Server.WaitingForPlayers += OnWaitingForPlayers;
        Server.RoundStarted += OnRoundStarted;
        Server.RespawningTeam += OnRespawningTeam;
        Server.RestartingRound += OnRestartingRound;
    }

    internal void UnsubscribeEvents()
    {
        Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;

        Server.WaitingForPlayers -= OnWaitingForPlayers;
        Server.RoundStarted -= OnRoundStarted;
        Server.RespawningTeam -= OnRespawningTeam;
        Server.RestartingRound -= OnRestartingRound;
    }

    internal void OnChangingRole(ChangingRoleEventArgs ev)
    {
        if (_afkPlayers.ContainsKey(ev.Player))
            _afkPlayers.Remove(ev.Player);
    }

    internal void OnWaitingForPlayers()
    {
        LastOneEnabled = false;
    }

    // private IEnumerator<float> LookAtPlayer()
    // {
    //     while (!Round.IsEnded)
    //     {
    //         var npcs = Player.List.Where(x => x.IsNPC);
    //
    //         foreach (var npc in npcs)
    //         {
    //             var mouseLook = ((IFpcRole)npc.ReferenceHub.roleManager.CurrentRole).FpcModule.MouseLook;
    //
    //             var closestPlayer = Player.List.OrderBy(x => Vector3.Distance(x.Position, npc.Position)).FirstOrDefault(x => x != npc);
    //
    //             if (closestPlayer == null) continue;
    //
    //             var rotation = Quaternion.LookRotation(closestPlayer.Position - npc.Position, Vector3.up);
    //
    //             mouseLook.ApplySyncValues(ToClientUShorts(rotation).horizontal, ToClientUShorts(rotation).vertical);
    //         }
    //
    //         yield return Timing.WaitForSeconds(0.1f);
    //     }
    // }
    //
    // private (ushort horizontal, ushort vertical) ToClientUShorts(Quaternion rotation)
    // {
    //     const float ToHorizontal = ushort.MaxValue / 360f;
    //     const float ToVertical = ushort.MaxValue / 176f;
    //
    //     var fixVertical = -rotation.eulerAngles.x;
    //
    //     switch (fixVertical)
    //     {
    //         case < -90f:
    //             fixVertical += 360f;
    //             break;
    //         case > 270f:
    //             fixVertical -= 360f;
    //             break;
    //     }
    //
    //     var horizontal = Mathf.Clamp(rotation.eulerAngles.y, 0f, 360f);
    //     var vertical = Mathf.Clamp(fixVertical, -88f, 88f) + 88f;
    //
    //     return ((ushort)Math.Round(horizontal * ToHorizontal), (ushort)Math.Round(vertical * ToVertical));
    // }

    internal void OnRoundStarted()
    {
        _afkCoroutine = Timing.RunCoroutine(AfkCoroutine());
        _autoNukeCoroutine = Timing.RunCoroutine(AutoNukeCoroutine());
        _autoBroadcastCoroutine = Timing.RunCoroutine(AutoBroadcastCoroutine());

        Timing.CallDelayed(5f, () =>
        {
            LastOneEnabled = true;
        });

        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.RoundStartMessage.Duration, Config.Broadcasts.RoundStartMessage.ParsedMessage, Config.Broadcasts.RoundStartMessage.Priority);

        Timing.CallDelayed(0.1f, () =>
        {
            var random = Random.value;

            var sum = 0f;

            foreach (var (team, startSpawn) in Config.Spawns.StartSpawnChances)
            {
                sum += startSpawn.Chance;

                if (random <= sum)
                {
                    if (team == Respawning.SpawnableTeamType.None) break;

                    foreach (var player in Player.Get(RoleTypeId.FacilityGuard))
                    {
                        player.Role.Set(startSpawn.Role, SpawnReason.RoundStart, RoleSpawnFlags.All);
                    }

                    break;
                }
            }
        });
    }

    private IEnumerator<float> AutoBroadcastCoroutine()
    {
        var index = 0;

        while (!Round.IsEnded)
        {
            yield return Timing.WaitForSeconds(Config.Broadcasts.AutoBroadcastInterval);

            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.AutoBroadcastMessages[index].Duration, Config.Broadcasts.AutoBroadcastMessages[index].ParsedMessage, Config.Broadcasts.AutoBroadcastMessages[index].Priority);

            index = (index + 1) % Config.Broadcasts.AutoBroadcastMessages.Count;
        }
    }

    private bool IsAfkImmume(Player player)
    {
        if (player == null) return true;
        if (player.IsNPC) return true;
        if (player.IsDead) return true;
        if (player.IsGodModeEnabled && Config.Afk.IgnoreGodmode) return true;
        if (player.Role.Is(out FpcRole fpcRole) && fpcRole.IsNoclipEnabled && Config.Afk.IgnoreNoclip) return true;
        return Config.Afk.ExcludedRoles.Contains(player.Role.Type) || player.CheckPermission("cocoa.afk.ignore");
    }

    private IEnumerator<float> AfkCoroutine()
    {
        while (!Round.IsEnded)
        {
            yield return Timing.WaitForSeconds(Config.Afk.AfkCheckInterval);

            foreach (var player in Player.List)
            {
                if (IsAfkImmume(player)) continue;

                if (!_afkPlayers.ContainsKey(player))
                {
                    _afkPlayers.Add(player, (0, player.Position));
                }

                if ((_afkPlayers[player].position - player.Position).sqrMagnitude > Config.Afk.AfkSqrMagnitude)
                {
                    _afkPlayers[player] = (0, player.Position);

                    if (player.HasBroadcast($"AFK_{player.UserId}"))
                        player.RemoveBroadcast($"AFK_{player.UserId}");
                }

                if (_afkPlayers[player].time >= Config.Afk.AfkKickTime)
                {
                    _afkPlayers.Remove(player);

                    player.Kick(Config.Afk.AfkKickMessage);
                }
                else if (_afkPlayers[player].time >= Config.Afk.AfkBroadcastTime)
                {
                    if (player.HasBroadcast($"AFK_{player.UserId}"))
                        player.RemoveBroadcast($"AFK_{player.UserId}");

                    player.AddBroadcast(Config.Afk.AfkMessage.Duration, Config.Afk.AfkMessage.Format(Mathf.Ceil((Config.Afk.AfkKickTime - _afkPlayers[player].time) * 10) / 10), Config.Afk.AfkMessage.Priority, $"AFK_{player.UserId}");
                }

                _afkPlayers[player] = (_afkPlayers[player].time + Config.Afk.AfkCheckInterval, player.Position);
            }
        }
    }

    internal void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        if (ev.NextKnownTeam == Respawning.SpawnableTeamType.ChaosInsurgency)
        {
            Timing.CallDelayed(0.1f, () =>
            {
                foreach (var player in Player.List.Where(player => player.LeadingTeam == LeadingTeam.ChaosInsurgency))
                {
                    player.AddBroadcast(Config.Broadcasts.ChaosSpawnMessage.Duration, Config.Broadcasts.ChaosSpawnMessage.ParsedMessage, Config.Broadcasts.ChaosSpawnMessage.Priority);
                }
            });
        }

        Timing.KillCoroutines(_elevatorCoroutine);

        _elevatorCoroutine = Timing.RunCoroutine(ElevatorCoroutine(
            ev.NextKnownTeam == SpawnableTeamType.ChaosInsurgency ? ElevatorType.GateA : ElevatorType.GateB,
            ev.Players.Count));
    }

    private IEnumerator<float> ElevatorCoroutine(ElevatorType type, int spawnCount)
    {
        var lift = Lift.Get(type);
        var counter = 0;

        while (true)
        {
            counter++;

            if (counter >= 300) yield break;

            var players = lift.Players.ToList();

            foreach (var player in players)
            {
                if (player.HasBroadcast("Elevator"))
                {
                    player.RemoveBroadcast("Elevator");
                }

                player.AddBroadcast(Config.Broadcasts.ElevatorMessage.Duration, Config.Broadcasts.ElevatorMessage.Format(players.Count, spawnCount), Config.Broadcasts.ElevatorMessage.Priority, "Elevator");
            }

            yield return Timing.WaitForSeconds(0.1f);
        }
    }

    internal void OnRestartingRound()
    {
        Timing.KillCoroutines(_afkCoroutine);
        _afkPlayers.Clear();

        Timing.KillCoroutines(_autoNukeCoroutine);
        Timing.KillCoroutines(_autoBroadcastCoroutine);
        Timing.KillCoroutines(_elevatorCoroutine);

        BadgeManager.SaveBadges();
        BadgeCooldownManager.SaveBadgeCooldowns();
        PenaltyManager.SavePenalties();
        CheckManager.SaveChecks();
        UserManager.SaveUsers();
        ConnectionManager.SaveConnections();
    }

    private IEnumerator<float> AutoNukeCoroutine()
    {
        yield return Timing.WaitForSeconds(Config.AutoNuke.AutoNukeTimer);

        if (Warhead.IsInProgress)
        {
            Warhead.IsLocked = true;
            yield break;
        }

        Warhead.Start();
        Warhead.IsLocked = true;
    }
}