// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection.Emit;
// using CentralAuth;
// using CocoaPlugin.API.Managers;
// using Exiled.API.Features;
// using Exiled.API.Features.Pools;
// using Exiled.Events.EventArgs.Player;
// using static HarmonyLib.AccessTools;
// using HarmonyLib;
// using Hints;
// using LiteNetLib;
// using MEC;
// using Mirror;
// using Mirror.LiteNetLib4Mirror;
//
// namespace CocoaPlugin.Patches;
//
// [HarmonyPatch(typeof(CustomLiteNetLib4MirrorTransport), nameof(CustomLiteNetLib4MirrorTransport.ProcessConnectionRequest))]
// public class ProcessConnectionRequestPatch
// {
//     public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
//     {
//         var newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);
//
//         var offset = -14;
//         var index = newInstructions.FindLastIndex(instruction =>
//             instruction.Calls(Method(typeof(CustomLiteNetLib4MirrorTransport),
//                 nameof(CustomLiteNetLib4MirrorTransport.ProcessCancellationData))));
//
//         index += offset;
//
//         var acceptLabel = generator.DefineLabel();
//
//         newInstructions[index].WithLabels(acceptLabel);
//
//         index = newInstructions.FindIndex(instruction =>
//             instruction.Calls(PropertyGetter(typeof(NetManager),
//                 nameof(NetManager.ConnectedPeersCount))));
//         offset = -1;
//
//         index += offset;
//
//         newInstructions.InsertRange(index, new[]
//         {
//             new CodeInstruction(OpCodes.Ldstr, "Authentication"),
//             new CodeInstruction(OpCodes.Ldnull),
//             new CodeInstruction(OpCodes.Call, Method(typeof(PluginAPI.Core.Log), nameof(PluginAPI.Core.Log.Info), new[] {typeof(string), typeof(string)})),
//             new CodeInstruction(OpCodes.Ldloc_S, 10),
//             new CodeInstruction(OpCodes.Call, Method(typeof(ProcessConnectionRequestPatch), nameof(Conditions))),
//             new CodeInstruction(OpCodes.Brtrue_S, acceptLabel),
//         });
//
//         for (int i = 0; i < newInstructions.Count; i++)
//         {
//             yield return newInstructions[i];
//         }
//
//         ListPool<CodeInstruction>.Pool.Return(newInstructions);
//     }
//
//     private static List<PlayerQueue> _queue = new();
//
//     public static bool Conditions(string id)
//     {
//         Log.Info($"[Queue] {id} is attempting to connect.");
//
//         if (!CocoaPlugin.Instance.Config.Queue.IsEnabled)
//         {
//             Log.Info("[Queue] Queue is disabled.");
//             return false;
//         }
//
//         if (LiteNetLib4MirrorCore.Host.ConnectedPeersCount < CustomNetworkManager.slots && !CocoaPlugin.Instance.Config.Queue.Debug)
//         {
//             Log.Info($"[Queue] {id} not added to queue due to not enough connected peers.");
//             return false;
//         }
//
//         if (ReservedSlotManager.Get(id))
//         {
//             Log.Info($"[Queue] {id} not added to queue due to being a reserved slot player.");
//             // Return false to continue original method
//             return false;
//         }
//
//         return Enqueue(id);
//     }
//
//     public static PlayerQueue Dequeue()
//     {
//         if (_queue.Count == 0)
//             return null;
//
//         var obj = _queue[0];
//         _queue.RemoveAt(0);
//         return obj;
//     }
//
//     private static bool Enqueue(string ip)
//     {
//         if (_queue.Count >= CocoaPlugin.Instance.Config.Queue.MaxQueue)
//         {
//             Log.Info($"[Queue] {ip} was rejected due to the queue being full.");
//         }
//
//         Log.Info($"[Queue] {ip} was added to the queue.");
//         _queue.Add(new PlayerQueue {UserId = ip});
//         return true;
//     }
//
//     public static void SetResponse(string id, AuthenticationResponse response)
//     {
//         var index = _queue.FindIndex(x => x.UserId == id);
//         if (index == -1)
//             return;
//
//         _queue[index] = new PlayerQueue {UserId = id, Response = response};
//     }
//
//     public static bool QueueContains(string id) => _queue.Any(x => x.UserId == id);
//
//     public static void OnRestartingRound()
//     {
//         _queue.Clear();
//     }
//
//     public static void OnLeft(LeftEventArgs ev)
//     {
//         if (_queue.Count == 0)
//         {
//             Log.Info($"[Queue] No players waiting in the queue.");
//             return;
//         }
//
//         if (QueueContains(ev.Player.UserId))
//         {
//             _queue.RemoveAll(x => x.UserId == ev.Player.UserId);
//             Log.Info($"[Queue] {ev.Player.UserId} was removed from the queue due to leaving.");
//             return;
//         }
//
//         var dequeued = Dequeue();
//
//         if (dequeued == null)
//         {
//             Log.Info($"[Queue] No players waiting in the queue.");
//             return;
//         }
//
//         dequeued.Hub.authManager.ProcessAuthenticationResponse(dequeued.Response);
//         Log.Info($"[Queue] {dequeued.UserId} was dequeued and processed.");
//     }
//
//     public static void OnJoined(JoinedEventArgs ev)
//     {
//         if (_queue.Count == 0)
//             return;
//
//         if (QueueContains(ev.Player.UserId))
//         {
//             var item = _queue.Find(x => x.UserId == ev.Player.UserId);
//
//             if (item == null)
//                 return;
//
//             item.Hub = ev.Player.ReferenceHub;
//         }
//     }
//
//     public static IEnumerator<float> QueueHint()
//     {
//         while (true)
//         {
//             if (_queue.Count == 0)
//                 yield break;
//
//             foreach (var player in _queue)
//             {
//                 if (player.Hub == null)
//                     continue;
//                 var index = _queue.IndexOf(player);
//
//                 player.Hub.hints.Show(new TextHint($"<b>지금 {_queue.Count} 중 {index + 1}번째 입장 대기열에 있습니다.</b>", null,
//                     null, 1.2f));
//             }
//
//             yield return Timing.WaitForSeconds(1f);
//         }
//     }
// }
//
// public class PlayerQueue
// {
//     public string UserId { get; set; }
//     public AuthenticationResponse Response { get; set; }
//     public ReferenceHub Hub { get; set; }
// }
//
// [HarmonyPatch(typeof(PlayerAuthenticationManager),
//     nameof(PlayerAuthenticationManager.ServerReceiveAuthenticationResponse))]
// public class ServerReceiveAuthenticationResponsePatch
// {
//     public static bool Prefix(NetworkConnection conn, AuthenticationResponse msg)
//     {
//         if (!NetworkServer.active)
//         {
//             Log.Error("ServerReceiveAuthenticationResponse called on client.");
//             return false;
//         }
//
//         if (msg.SignedAuthToken == null)
//         {
//             Log.Error("SignedAuthToken is null.");
//             return false;
//         }
//
//         msg.SignedAuthToken.TryGetToken<AuthenticationToken>("Authentication", out var authToken, out _,
//             out _);
//
//         if (authToken == null)
//         {
//             Log.Error("AuthenticationToken is null.");
//             return false;
//         }
//
//         var userId = PlayerAuthenticationManager.RemoveSalt(authToken.UserId);
//
//         if (ProcessConnectionRequestPatch.QueueContains(userId))
//         {
//             ProcessConnectionRequestPatch.SetResponse(userId, msg);
//             Log.Info($"[Queue] {userId} is in the queue, delaying connection and saving response.");
//             return false;
//         }
//
//         return true;
//     }
// }
//
// // Patches the PlayerAuthenticationManager.FixedUpdate method to check if the queue contains the player's id,
// // if it does, it will stop PlayerAuthenticationManager's timeout timer.
// [HarmonyPatch(typeof(PlayerAuthenticationManager), nameof(PlayerAuthenticationManager.FixedUpdate))]
// public class PlayerAuthManagerFixedUpdatePatch
// {
//     public static IEnumerator<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
//     {
//         var newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);
//
//         var index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ret);
//
//         var retJump = generator.DefineLabel();
//
//         newInstructions[index].WithLabels(retJump);
//
//         int offset = 3;
//
//         index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(PlayerAuthenticationManager),
//             nameof(PlayerAuthenticationManager._timeoutTimer))));
//
//         index += offset;
//
//         newInstructions.InsertRange(index, [
//             new CodeInstruction(OpCodes.Ldarg_0),
//             new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(PlayerAuthenticationManager),
//                 nameof(PlayerAuthenticationManager.UserId))),
//             new CodeInstruction(OpCodes.Call, Method(typeof(ProcessConnectionRequestPatch), nameof(ProcessConnectionRequestPatch.QueueContains))),
//             new CodeInstruction(OpCodes.Brtrue_S, retJump)
//         ]);
//
//         for (int i = 0; i < newInstructions.Count; i++)
//         {
//             yield return newInstructions[i];
//         }
//
//         ListPool<CodeInstruction>.Pool.Return(newInstructions);
//     }
// }