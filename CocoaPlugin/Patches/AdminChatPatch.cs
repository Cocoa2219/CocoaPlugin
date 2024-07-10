using System;
using System.Linq;
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
                playerCommandSender.ReferenceHub.gameConsoleTransmission.SendToClient("관리자 채팅을 사용할 권한이 없습니다!", "red");
                playerCommandSender.RaReply("관리자 채팅을 사용할 권한이 없습니다!", false, true, "");
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
            ServerConsole.AddLog("[관리자 " + sender.LogName + "] " + q, ConsoleColor.DarkYellow);
        }
        ServerLogs.AddLog(ServerLogs.Modules.Administrative, "[" + sender.LogName + "] " + q, ServerLogs.ServerLogType.AdminChat, false);
        foreach (var player in from referenceHub in ReferenceHub.AllHubs let mode = referenceHub.Mode where mode != ClientInstanceMode.Unverified && mode != ClientInstanceMode.DedicatedServer && referenceHub.serverRoles.AdminChatPerms select Player.Get(referenceHub))
        {
            player.AddBroadcast(CocoaPlugin.Instance.Config.Broadcasts.Chats.AdminChatMessage.Duration, CocoaPlugin.Instance.Config.Broadcasts.Chats.AdminChatMessage.Format(player, q));
        }

        return false;
    }
}