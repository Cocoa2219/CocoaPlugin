using System.Collections.Generic;
using System.Reflection.Emit;
using CentralAuth;
using CocoaPlugin.API;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using UnityEngine;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(PlayerAuthenticationManager), nameof(PlayerAuthenticationManager.FinalizeAuthentication))]
public class PlayerAuthenticationManagerPatch
{
    // public static bool Prefix(PlayerAuthenticationManager __instance)
    // {
    //     if (!NetworkServer.active)
    //     {
    //         Debug.LogWarning(
    //             "[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::FinalizeAuthentication()' called when server was not active");
    //         return false;
    //     }
    //
    //     __instance.UserId = __instance.AuthenticationResponse.AuthToken.UserId;
    //     __instance.DoNotTrack = __instance.AuthenticationResponse.DoNotTrack ||
    //                             __instance.AuthenticationResponse.AuthToken.DoNotTrack;
    //
    //     var nickname = StringUtils.Base64Decode(__instance.AuthenticationResponse.AuthToken.Nickname);
    //
    //     __instance._hub.nicknameSync.UpdateNickname(
    //         nickname);
    //
    //     if (__instance.DoNotTrack)
    //         ServerLogs.AddLog(ServerLogs.Modules.Networking,
    //             __instance._hub.LoggedNameFromRefHub() + " connected from IP address " +
    //             __instance.connectionToClient.address + " sent Do Not Track signal.",
    //             ServerLogs.ServerLogType.ConnectionUpdate);


    //     ------------------------------ Changed Code ------------------------------
    //     __instance._hub.gameConsoleTransmission.SendToClient(
    //         $"\n현재 <b>Cocoa's Lab {Utility.GetServerNumber(Server.Port)}서버</b>에 접속되었습니다.\n즐거운 게임 되세요!",
    //         "white");
    //     ------------------------------ Changed Code ------------------------------


    //     if (PlayerAuthenticationManager.AllowSameAccountJoining) return false;
    //     var playerId = ReferenceHub.GetHub(__instance.gameObject).PlayerId;
    //     foreach (var referenceHub in ReferenceHub.AllHubs.Where(referenceHub => referenceHub.authManager.UserId == __instance.UserId && referenceHub.PlayerId != playerId &&
    //                  !referenceHub.isLocalPlayer))
    //     {
    //         ServerConsole.AddLog(
    //             $"Player {__instance.UserId} ({referenceHub.PlayerId}, {__instance.connectionToClient.address}) has been kicked from the server, because he has just joined the server again from IP address {__instance.connectionToClient.address}.");
    //         ServerConsole.Disconnect(referenceHub.gameObject,
    //             "Only one player instance of the same player is allowed.");
    //     }
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        var index = newInstructions.FindIndex(instruction => instruction.Calls(PropertyGetter(typeof(NicknameSync), nameof(NicknameSync.MyNick))));

        index -= 4;

        newInstructions.RemoveRange(index, 8);

        newInstructions.InsertRange(index, new[]
        {
            new CodeInstruction(OpCodes.Ldstr, "\n<b>{0}</b>님,\n현재 <b>Cocoa's Lab {1}서버</b>에 접속되었습니다.\n즐거운 게임 되세요!"),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(PlayerAuthenticationManager), nameof(PlayerAuthenticationManager._hub))),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(ReferenceHub), nameof(ReferenceHub.nicknameSync))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(NicknameSync), nameof(NicknameSync.MyNick))),
            new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Server), nameof(Server.Port))),
            new CodeInstruction(OpCodes.Call, Method(typeof(Utility), nameof(Utility.GetServerNumber), new[] { typeof(ushort) })),
            new CodeInstruction(OpCodes.Box, typeof(byte)),
            new CodeInstruction(OpCodes.Call, Method(typeof(string), nameof(string.Format), new[] { typeof(string), typeof(object), typeof(object) })),
            new CodeInstruction(OpCodes.Ldstr, "white"),
        });

        foreach (var t in newInstructions)
        {
            yield return t;
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}