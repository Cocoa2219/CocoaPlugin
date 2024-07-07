using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Loader;
using MEC;
using MultiBroadcast.API;
using PlayerRoles;
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
    }

    internal void UnsubscribeEvents()
    {
        Exiled.Events.Handlers.Player.Verified -= OnVerified;
        Exiled.Events.Handlers.Player.Dying -= OnDying;
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
    }

    internal void OnVerified(VerifiedEventArgs ev)
    {
        ev.Player.AddBroadcast(Config.Broadcasts.VerifiedMessage.Duration, Config.Broadcasts.VerifiedMessage.Format(ev.Player));
    }

    internal void OnDying(DyingEventArgs ev)
    {
        if (!ev.IsAllowed) return;
        if (ev.Attacker == null) return;
        if (ev.Attacker == ev.Player.Cuffer) return;

        if (ev.Player.IsCuffed)
        {
            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.CuffedKillMessage.Duration, Config.Broadcasts.CuffedKillMessage.Format(ev.Attacker, ev.Player));
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

            ev.Player.AddBroadcast(Config.Broadcasts.ScpSpawnMessage.Duration, Config.Broadcasts.ScpSpawnMessage.Format(Player.Get(Team.SCPs).ToList()), 0, $"ScpSpawn_{ev.Player.UserId}");
        });
    }
}