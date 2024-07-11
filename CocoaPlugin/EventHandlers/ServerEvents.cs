using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CocoaPlugin.API;
using CocoaPlugin.Configs;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using MultiBroadcast.API;
using PlayerRoles;
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

    internal void OnRoundStarted()
    {
        _afkCoroutine = Timing.RunCoroutine(AfkCoroutine());
        _autoNukeCoroutine = Timing.RunCoroutine(AutoNukeCoroutine());

        Timing.CallDelayed(5f, () =>
        {
            LastOneEnabled = true;
        });

        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.RoundStartMessage.Duration, Config.Broadcasts.RoundStartMessage.Message, Config.Broadcasts.RoundStartMessage.Priority);

        Timing.CallDelayed(0.1f, () =>
        {
            var random = Random.value;

            var sum = 0f;

            foreach (var (team, (role, chance)) in Config.Spawns.StartSpawnChances)
            {
                sum += chance;

                if (random <= sum)
                {
                    if (team == Respawning.SpawnableTeamType.None) break;

                    foreach (var player in Player.Get(RoleTypeId.FacilityGuard))
                    {
                        player.Role.Set(role, SpawnReason.RoundStart, RoleSpawnFlags.All);
                    }

                    break;
                }
            }
        });
    }

    private IEnumerator<float> AfkCoroutine()
    {
        while (!Round.IsEnded)
        {
            yield return Timing.WaitForSeconds(Config.Afk.AfkCheckInterval);

            foreach (var player in Player.List)
            {
                if (!_afkPlayers.ContainsKey(player))
                {
                    _afkPlayers.Add(player, (0, player.Position));
                }

                if (player.IsDead) continue;
                if (player.IsGodModeEnabled && Config.Afk.IgnoreGodmode) continue;
                if (player.Role.Is(out FpcRole fpcRole) && fpcRole.IsNoclipEnabled && Config.Afk.IgnoreNoclip) continue;
                if (Config.Afk.ExcludedRoles.Contains(player.Role.Type)) continue;

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
                    player.AddBroadcast(Config.Broadcasts.ChaosSpawnMessage.Duration, Config.Broadcasts.ChaosSpawnMessage.Message, Config.Broadcasts.ChaosSpawnMessage.Priority);
                }
            });
        }
    }

    internal void OnRestartingRound()
    {
        Timing.KillCoroutines(_afkCoroutine);
        _afkPlayers.Clear();

        BadgeManager.SaveBadges();
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