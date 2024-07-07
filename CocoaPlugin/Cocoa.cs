﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using CentralAuth;
using CocoaPlugin.Configs;
using CocoaPlugin.EventHandlers;
using CommandSystem;
using Exiled.API.Extensions;
using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using MultiBroadcast.API;
using NorthwoodLib.Pools;
using PlayerRoles;
using RemoteAdmin.Communication;

namespace CocoaPlugin
{
    public class Cocoa : Plugin<Config>
    {
        public static Cocoa Instance { get; private set; }

        public override string Name { get; } = "CocoaPlugin";
        public override string Author { get; } = "Cocoa";
        public override string Prefix { get; } = "CocoaPlugin";
        public override Version Version { get; } = new(1, 0, 0);

        internal PlayerEvents PlayerEvents { get; private set; }
        internal ServerEvents ServerEvents { get; private set; }
        internal MapEvents MapEvents { get; private set; }

        private Harmony Harmony { get; set; }

        public override void OnEnabled()
        {
            Instance = this;

            PlayerEvents = new PlayerEvents(this);
            ServerEvents = new ServerEvents(this);
            MapEvents = new MapEvents(this);
            SubscribeEvents();

            Harmony = new Harmony($"cocoa.cocoaplugin-{DateTime.Now.Ticks}");
            Harmony.PatchAll();

            base.OnEnabled();
        }

        private void SubscribeEvents()
        {
            PlayerEvents.SubscribeEvents();
            ServerEvents.SubscribeEvents();
            MapEvents.SubscribeEvents();
        }

        public override void OnDisabled()
        {
            Harmony.UnpatchAll();
            Harmony = null;

            UnsubscribeEvents();
            PlayerEvents = null;
            ServerEvents = null;
            MapEvents = null;

            Instance = null;

            base.OnDisabled();
        }

        private void UnsubscribeEvents()
        {
            PlayerEvents.UnsubscribeEvents();
            ServerEvents.UnsubscribeEvents();
            MapEvents.UnsubscribeEvents();
        }
    }
}
