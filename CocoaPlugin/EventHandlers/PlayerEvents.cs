using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API;
using CocoaPlugin.Configs;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Loader;
using MEC;
using MultiBroadcast.API;
using PlayerRoles;
using UnityEngine;
using Config = CocoaPlugin.Configs.Config;

namespace CocoaPlugin.EventHandlers;

public class PlayerEvents(Cocoa plugin)
{
    private Cocoa Plugin { get; } = plugin;
    private Config Config => Plugin.Config;

    internal void SubscribeEvents()
    {
        Exiled.Events.Handlers.Player.Verified += OnVerified;
        Exiled.Events.Handlers.Player.Dying += OnDying;
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;
        Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
    }

    internal void UnsubscribeEvents()
    {
        Exiled.Events.Handlers.Player.Verified -= OnVerified;
        Exiled.Events.Handlers.Player.Dying -= OnDying;
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
        Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
    }

    internal void OnVerified(VerifiedEventArgs ev)
    {
        // ev.Player.AddBroadcast(Config.Broadcasts.VerifiedMessage.Duration, Config.Broadcasts.VerifiedMessage.Format(ev.Player));
    }

    internal void OnDying(DyingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        var killType = KillLogs.DamageTypeToKillType(ev.DamageHandler.Type);

        foreach (var dead in Player.Get(Team.Dead))
        {
            // dead.AddBroadcast(Config.Broadcasts.KillLogs.KillLog[killType].Duration, Config.Broadcasts.KillLogs.KillLog[killType].Format(ev.Attacker, ev.Player));
        }

        if (ev.Attacker == null) return;

        if (ev.Attacker.IsScp)
        {
            var amount = Random.Range(Config.Scps.ScpHealMin, Config.Scps.ScpHealMax + 1);

            ev.Attacker.Heal(amount);

            // ev.Attacker.AddBroadcast(Config.Broadcasts.ScpHealMessage.Duration, Config.Broadcasts.ScpHealMessage.Format(amount));
        }

        if (ev.Attacker == ev.Player.Cuffer) return;

        if (ev.Player.IsCuffed)
        {
            // MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.CuffedKillMessage.Duration, Config.Broadcasts.CuffedKillMessage.Format(ev.Attacker, ev.Player));
        }
    }

    internal void OnSpawned(SpawnedEventArgs ev)
    {
        Timing.CallDelayed(0.1f, () =>
        {
            if (!ev.Player.IsScp) return;

            // if (ev.Player.HasBroadcast($"ScpSpawn_{ev.Player.UserId}"))
            // {
            //     ev.Player.RemoveBroadcast($"ScpSpawn_{ev.Player.UserId}");
            // }
            //
            // ev.Player.AddBroadcast(Config.Broadcasts.ScpSpawnMessage.Duration, Config.Broadcasts.ScpSpawnMessage.Format(Player.Get(Team.SCPs).ToList()), 0, $"ScpSpawn_{ev.Player.UserId}");
        });
    }

    internal void OnChangingRole(ChangingRoleEventArgs ev)
    {
        if (!Plugin.ServerEvents.LastOneEnabled) return;
        if (ev.Player.Role.Team == Team.Dead) return;
        if (Player.Get(ev.Player.Role.Team).Count() != 2) return;

        var player = Player.Get(ev.Player.Role.Team).Except([ev.Player]).First();

        // player.AddBroadcast(Config.Broadcasts.LastOneMessage.Duration, Config.Broadcasts.LastOneMessage.Format(ev.Player.Role.Team));
    }

    internal void OnHandcuffing(HandcuffingEventArgs ev)
    {

    }

    internal void OnRemovingHandcuffing(RemovingHandcuffsEventArgs ev)
    {

    }
}