﻿using System;
using CocoaPlugin.API;
using CocoaPlugin.API.Beta;
using CocoaPlugin.API.Managers;
using CocoaPlugin.Configs;
using CocoaPlugin.EventHandlers;
using Exiled.API.Features;
using HarmonyLib;
using Map = Exiled.Events.Handlers.Map;

namespace CocoaPlugin
{
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

        public override void OnEnabled()
        {
            // PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] {GraphicsDeviceType.Direct3D11});

            Instance = this;

            AchievementManager.Initialize();
            API.Managers.FileManager.CreateFolder();
            BadgeManager.LoadBadges();
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
}
