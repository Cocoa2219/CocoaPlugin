using System.Linq;
using CentralAuth;
using CocoaPlugin.API;
using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using NorthwoodLib;
using UnityEngine;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(PlayerAuthenticationManager), nameof(PlayerAuthenticationManager.FinalizeAuthentication))]
public class PlayerAuthenticationManagerPatch
{
    public static bool Prefix(PlayerAuthenticationManager __instance)
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning(
                "[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::FinalizeAuthentication()' called when server was not active");
            return false;
        }

        __instance.UserId = __instance.AuthenticationResponse.AuthToken.UserId;
        __instance.DoNotTrack = __instance.AuthenticationResponse.DoNotTrack ||
                                __instance.AuthenticationResponse.AuthToken.DoNotTrack;

        var nickname = StringUtils.Base64Decode(__instance.AuthenticationResponse.AuthToken.Nickname);

        __instance._hub.nicknameSync.UpdateNickname(
            nickname);

        if (__instance.DoNotTrack)
            ServerLogs.AddLog(ServerLogs.Modules.Networking,
                __instance._hub.LoggedNameFromRefHub() + " connected from IP address " +
                __instance.connectionToClient.address + " sent Do Not Track signal.",
                ServerLogs.ServerLogType.ConnectionUpdate);
        __instance._hub.gameConsoleTransmission.SendToClient(
            $"\n현재 <b>Cocoa's Lab {Utility.GetServerNumber(Server.Port)}서버</b>에 접속되었습니다.\n즐거운 게임 되세요!",
            "white");

        if (PlayerAuthenticationManager.AllowSameAccountJoining) return false;
        var playerId = ReferenceHub.GetHub(__instance.gameObject).PlayerId;
        foreach (var referenceHub in ReferenceHub.AllHubs.Where(referenceHub => referenceHub.authManager.UserId == __instance.UserId && referenceHub.PlayerId != playerId &&
                     !referenceHub.isLocalPlayer))
        {
            ServerConsole.AddLog(
                $"Player {__instance.UserId} ({referenceHub.PlayerId}, {__instance.connectionToClient.address}) has been kicked from the server, because he has just joined the server again from IP address {__instance.connectionToClient.address}.");
            ServerConsole.Disconnect(referenceHub.gameObject,
                "Only one player instance of the same player is allowed.");
        }

        return false;
    }
}