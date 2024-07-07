using System;
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

public class ServerEvents(Cocoa plugin)
{
    private Cocoa Plugin { get; } = plugin;
    private Config Config => Plugin.Config;

    internal void SubscribeEvents()
    {
        Server.RoundStarted += OnRoundStarted;
        Server.RespawningTeam += OnRespawningTeam;
    }

    internal void UnsubscribeEvents()
    {
        Server.RoundStarted -= OnRoundStarted;
        Server.RespawningTeam -= OnRespawningTeam;
    }

    internal void OnRoundStarted()
    {
        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.RoundStartMessage.Duration, Config.Broadcasts.RoundStartMessage.Message);
    }

    internal void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        if (ev.NextKnownTeam == Respawning.SpawnableTeamType.ChaosInsurgency)
        {
            foreach (var player in Player.List.Where(player => player.LeadingTeam == LeadingTeam.ChaosInsurgency))
            {
                player.AddBroadcast(Config.Broadcasts.ChaosSpawnMessage.Duration, Config.Broadcasts.ChaosSpawnMessage.Message);
            }
        }
    }
}