using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CocoaPlugin.API;
using CocoaPlugin.Configs;
using CocoaPlugin.EventHandlers;
using Exiled.API.Features;
using HarmonyLib;
using Newtonsoft.Json;
using RemoteAdmin;

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

        public override async void OnEnabled()
        {
            Instance = this;

            API.FileManager.CreateFolder();
            BadgeManager.LoadBadges();
            PenaltyManager.LoadPenalties();
            CheckManager.LoadChecks();
            UserManager.LoadUsers();
            ConnectionManager.LoadConnections();

            PlayerEvents = new PlayerEvents(this);
            ServerEvents = new ServerEvents(this);
            MapEvents = new MapEvents(this);
            NetworkHandler = new NetworkHandler(this);
            SubscribeEvents();

            Harmony = new Harmony($"cocoa.cocoaplugin-{DateTime.Now.Ticks}");
            Harmony.PatchAll();

            NetworkManager.StartListener();

            NetworkManager.Send(new {}, MessageType.Started);

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
            NetworkManager.Send(new {}, MessageType.Stopped);

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
