// using System.Diagnostics;
// using AFK;
// using Exiled.API.Features;
// using HarmonyLib;
// using MultiBroadcast.API;
// using PlayerRoles;
// using PlayerRoles.FirstPersonControl;
// using UnityEngine;
//
// namespace CocoaPlugin.Patches;
//
// [HarmonyPatch(typeof(AFKManager), nameof(AFKManager.OnUpdate))]
// public class AfkPatch
// {
//     public static bool Prefix()
//     {
//         foreach (var referenceHub in ReferenceHub.AllHubs)
//         {
//             if (AFKManager.AFKTimers.TryGetValue(referenceHub, out var stopwatch) && stopwatch.IsRunning)
//             {
//                 if (referenceHub.roleManager.CurrentRole is IAFKRole iafkrole)
//                 {
//                     if (!iafkrole.IsAFK)
//                     {
//                         if (AFKManager._constantlyCheck)
//                         {
//                             stopwatch.Restart();
//                         }
//                         else
//                         {
//                             stopwatch.Reset();
//                         }
//                     }
//                     else
//                     {
//                         if (AFKManager._kickTime - stopwatch.Elapsed.TotalSeconds <=
//                             CocoaPlugin.Instance.Config.Broadcasts.AfkMessage.Duration)
//                         {
//                             var player = Player.Get(referenceHub);
//                             var leftTime = Mathf.CeilToInt((float)(AFKManager._kickTime - stopwatch.Elapsed.TotalSeconds));
//
//                             if (player.HasBroadcast("AFK_" + player.UserId))
//                             {
//                                 player.EditBroadcast(CocoaPlugin.Instance.Config.Broadcasts.AfkMessage.Format(leftTime), "AFK_" + player.UserId);
//                                 return false;
//                             }
//
//                             player.AddBroadcast(CocoaPlugin.Instance.Config.Broadcasts.AfkMessage.Duration,
//                                 CocoaPlugin.Instance.Config.Broadcasts.AfkMessage.Format(leftTime), CocoaPlugin.Instance.Config.Broadcasts.AfkMessage.Priority, "AFK_" + player.UserId);
//                         }
//
//                         if (stopwatch.Elapsed.TotalSeconds >= AFKManager._kickTime)
//                         {
//                             stopwatch.Reset();
//
//                             BanPlayer.KickUser(referenceHub, AFKManager._kickMessage);
//                         }
//                     }
//                 }
//             }
//         }
//
//         return false;
//     }
// }
