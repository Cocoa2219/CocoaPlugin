using System;
using CentralAuth;
using Exiled.API.Features;
using HarmonyLib;
using MultiBroadcast.API;
using RemoteAdmin;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(CommandProcessor), nameof(CommandProcessor.ProcessAdminChat))]
public class AdminChatPatch
{
    public static bool Prefix(string q, CommandSender sender)
    {
        if (!CommandProcessor.CheckPermissions(sender, "Admin Chat", PlayerPermissions.AdminChat, string.Empty, true))
        {
            if (sender is PlayerCommandSender playerCommandSender)
            {
                playerCommandSender.ReferenceHub.gameConsoleTransmission.SendToClient("You don't have permissions to access Admin Chat!", "red");
                playerCommandSender.RaReply("You don't have permissions to access Admin Chat!", false, true, "");
            }

            return false;
        }
        if (string.IsNullOrWhiteSpace(q.Replace("@", string.Empty)))
        {
            return false;
        }
        if (q.Length > 2000)
        {
            var text = q;
            const int length = 2000 - 0;
            q = text[..length] + "...";
        }
        if (ServerStatic.IsDedicated)
        {
            ServerConsole.AddLog("[AC " + sender.LogName + "] " + q, ConsoleColor.DarkYellow);
        }
        ServerLogs.AddLog(ServerLogs.Modules.Administrative, "[" + sender.LogName + "] " + q, ServerLogs.ServerLogType.AdminChat, false);
        foreach (var referenceHub in ReferenceHub.AllHubs)
        {
            var mode = referenceHub.Mode;
            if (mode != ClientInstanceMode.Unverified && mode != ClientInstanceMode.DedicatedServer && referenceHub.serverRoles.AdminChatPerms)
            {
                var player = Player.Get(referenceHub);

                player.AddBroadcast(Cocoa.Instance.Config.Broadcasts.Chats.AdminChatMessage.Duration, Cocoa.Instance.Config.Broadcasts.Chats.AdminChatMessage.Format(player, q));
            }
        }

        return false;
    }
}