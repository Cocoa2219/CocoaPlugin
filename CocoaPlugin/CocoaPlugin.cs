﻿using System;
using System.Diagnostics;
using CocoaPlugin.API;
using CocoaPlugin.API.Beta;
using CocoaPlugin.API.Managers;
using CocoaPlugin.Commands;
using CocoaPlugin.Configs;
using CocoaPlugin.Handlers;
using CocoaPlugin.Patches;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using MEC;
using Map = Exiled.Events.Handlers.Map;
using Player = Exiled.Events.Handlers.Player;
using Server = Exiled.Events.Handlers.Server;
using ShootingRange = CocoaPlugin.API.Beta.ShootingRange;

namespace CocoaPlugin;

public class CocoaPlugin : Plugin<Config>
{
    public static CocoaPlugin Instance { get; private set; }

    public override string Name { get; } = "CocoaPlugin";
    public override string Author { get; } = "Cocoa";
    public override string Prefix { get; } = "CocoaPlugin";
    public override Version Version { get; } = new(1, 0, 0);

    internal PlayerEvents PlayerEvents { get; private set; }
    internal ServerEvents ServerEvents { get; private set; }
    internal MapEvents MapEvents { get; private set; }
    internal NetworkHandler NetworkHandler { get; private set; }

    private Harmony Harmony { get; set; }

    internal Store Store { get; private set; }
    internal ShootingRange ShootingRange { get; private set; }
    internal Pathfinding Pathfinding { get; private set; }

    public override void OnEnabled()
    {
        Instance = this;

        RankManager.Initialize();
        AchievementManager.Initialize();
        API.Managers.FileManager.CreateFolder();
        BadgeManager.LoadBadges();
        BadgeCooldownManager.LoadBadgeCooldowns();
        PenaltyManager.LoadPenalties();
        CheckManager.LoadChecks();
        UserManager.LoadUsers();
        ConnectionManager.LoadConnections();
        LogManager.Initialize();

        PlayerEvents = new PlayerEvents(this);
        ServerEvents = new ServerEvents(this);
        MapEvents = new MapEvents(this);
        NetworkHandler = new NetworkHandler();
        SubscribeEvents();

        Harmony = new Harmony($"cocoa.cocoaplugin-{DateTime.Now.Ticks}");
        Harmony.PatchAll();

        NetworkManager.StartListener();

        NetworkManager.SendLog(new {}, LogType.Started);

        Store = new Store();
        Store.RegisterEvents();

        ShootingRange = new ShootingRange();

        Pathfinding = new Pathfinding();
        Pathfinding.RegisterEvents();

        Server.RestartingRound += ForceRotation.OnRoundRestarting;
        Server.RestartingRound += GeneratorDistributePatch.OnRestartingRound;

        Player.Left += NoScp.OnLeft;
        Server.RestartingRound += NoScp.OnRestarting;

        Player.Left += ProcessConnectionRequestPatch.OnLeft;
        Server.RestartingRound += ProcessConnectionRequestPatch.OnRestartingRound;
        Player.Joined += ProcessConnectionRequestPatch.OnJoined;

        Timing.RunCoroutine(ProcessConnectionRequestPatch.QueueHint());

        base.OnEnabled();
    }

    private void SubscribeEvents()
    {
        PlayerEvents.SubscribeEvents();
        ServerEvents.SubscribeEvents();
        MapEvents.SubscribeEvents();
        NetworkHandler.SubscribeEvents();
    }

    public override void OnDisabled()
    {
        Player.Left -= NoScp.OnLeft;
        Server.RestartingRound -= NoScp.OnRestarting;

        Server.RestartingRound -= GeneratorDistributePatch.OnRestartingRound;
        Server.RestartingRound -= ForceRotation.OnRoundRestarting;

        Player.Left -= ProcessConnectionRequestPatch.OnLeft;
        Server.RestartingRound -= ProcessConnectionRequestPatch.OnRestartingRound;
        Player.Joined -= ProcessConnectionRequestPatch.OnJoined;

        Pathfinding.Destroy();
        Pathfinding = null;

        ShootingRange.Destroy();
        ShootingRange = null;

        Store.UnregisterEvents();
        Store = null;

        LogManager.Destroy();

        NetworkManager.SendLog(new {}, LogType.Stopped);

        NetworkManager.StopListener();

        Harmony.UnpatchAll();
        Harmony = null;

        UnsubscribeEvents();
        PlayerEvents = null;
        ServerEvents = null;
        MapEvents = null;
        NetworkHandler = null;

        Instance = null;

        base.OnDisabled();
    }

    private void UnsubscribeEvents()
    {
        PlayerEvents.UnsubscribeEvents();
        ServerEvents.UnsubscribeEvents();
        MapEvents.UnsubscribeEvents();
        NetworkHandler.UnsubscribeEvents();
    }
}