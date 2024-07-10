﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CocoaPlugin.Configs;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using MEC;
using MultiBroadcast.API;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.EventHandlers;

public class ServerEvents(CocoaPlugin plugin)
{
    private CocoaPlugin Plugin { get; } = plugin;
    private Config Config => Plugin.Config;

    internal bool LastOneEnabled { get; private set; }

    private Dictionary<>

    internal void SubscribeEvents()
    {
        Server.WaitingForPlayers += OnWaitingForPlayers;
        Server.RoundStarted += OnRoundStarted;
        Server.RespawningTeam += OnRespawningTeam;
    }

    internal void UnsubscribeEvents()
    {
        Server.WaitingForPlayers -= OnWaitingForPlayers;
        Server.RoundStarted -= OnRoundStarted;
        Server.RespawningTeam -= OnRespawningTeam;
    }

    internal void OnWaitingForPlayers()
    {
        LastOneEnabled = false;
    }

    internal void OnRoundStarted()
    {
        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.RoundStartMessage.Duration, Config.Broadcasts.RoundStartMessage.Message, Config.Broadcasts.RoundStartMessage.Priority);

        Timing.CallDelayed(5f, () =>
        {
            LastOneEnabled = true;
        });
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
}