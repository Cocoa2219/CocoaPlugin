using System.Linq;
using CocoaPlugin.Configs;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Warhead;
using MEC;
using MultiBroadcast.API;
using Map = Exiled.Events.Handlers.Map;
using Warhead = Exiled.Events.Handlers.Warhead;

namespace CocoaPlugin.EventHandlers;

public class MapEvents(Cocoa plugin)
{
    private Cocoa Plugin { get; } = plugin;
    private Config Config => Plugin.Config;

    internal void SubscribeEvents()
    {
        Map.GeneratorActivating += OnGeneratorActivating;
        Map.AnnouncingDecontamination += OnAnnouncingDecontamination;
        Map.AnnouncingScpTermination += OnAnnouncingScpTermination;
        Map.AnnouncingNtfEntrance += OnAnnouncingNtfEntrance;
        Warhead.Starting += OnWarheadStarting;
        Warhead.Stopping += OnWarheadStopping;
    }

    internal void UnsubscribeEvents()
    {
        Map.GeneratorActivating -= OnGeneratorActivating;
        Map.AnnouncingDecontamination += OnAnnouncingDecontamination;
        Map.AnnouncingScpTermination -= OnAnnouncingScpTermination;
        Map.AnnouncingNtfEntrance -= OnAnnouncingNtfEntrance;
        Warhead.Starting -= OnWarheadStarting;
        Warhead.Stopping -= OnWarheadStopping;
    }

    internal void OnAnnouncingDecontamination(AnnouncingDecontaminationEventArgs ev)
    {
        if (Config.Broadcasts.DecontaminationMessages.TryGetValue(ev.State, out var value))
        {
            foreach (var player in Player.List.Where(x => x.Zone == ZoneType.LightContainment))
            {
                player.AddBroadcast(value.Duration, value.Message, value.Priority);
            }
        }
    }

    internal void OnAnnouncingScpTermination(AnnouncingScpTerminationEventArgs ev)
    {
        var attackerRole = ev.Attacker?.Role.Type;
        var targetRole = ev.Player.Role.Type;

        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.ScpTerminationMessage.Duration,
        Config.Broadcasts.ScpTerminationMessage.Format(ev.Attacker, ev.Player, attackerRole, targetRole), Config.Broadcasts.ScpTerminationMessage.Priority);
    }

    internal void OnAnnouncingNtfEntrance(AnnouncingNtfEntranceEventArgs ev)
    {
        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.NtfSpawnMessage.Duration,
            Config.Broadcasts.NtfSpawnMessage.Format(ev.UnitNumber, ev.UnitName, ev.ScpsLeft), Config.Broadcasts.NtfSpawnMessage.Priority);
    }

    internal void OnGeneratorActivating(GeneratorActivatingEventArgs ev)
    {
        Timing.CallDelayed(0.1f, () =>
        {
            if (Config.Broadcasts.GeneratorMessages.TryGetValue(Generator.List.Count(x => x.IsEngaged), out var value))
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(value.Duration, value.Message, value.Priority);
            }
        });
    }

    internal void OnWarheadStarting(StartingEventArgs ev)
    {
        foreach (var player in Player.List)
        {
            if (player.HasBroadcast("WarheadStop"))
                player.RemoveBroadcast("WarheadStop");

            if (player.HasBroadcast("WarheadStart"))
                player.RemoveBroadcast("WarheadStart");
        }

        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.WarheadStartMessage.Duration, Config.Broadcasts.WarheadStartMessage.Format(Exiled.API.Features.Warhead.RealDetonationTimer), Config.Broadcasts.WarheadStartMessage.Priority, "WarheadStart");
    }

    internal void OnWarheadStopping(StoppingEventArgs ev)
    {
        if (!ev.IsAllowed || Exiled.API.Features.Warhead.IsLocked) return;

        foreach (var player in Player.List)
        {
            if (player.HasBroadcast("WarheadStop"))
                player.RemoveBroadcast("WarheadStop");

            if (player.HasBroadcast("WarheadStart"))
                player.RemoveBroadcast("WarheadStart");
        }

        MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(Config.Broadcasts.WarheadCancelMessage.Duration, Config.Broadcasts.WarheadCancelMessage.Message, Config.Broadcasts.WarheadCancelMessage.Priority, "WarheadStop");
    }
}