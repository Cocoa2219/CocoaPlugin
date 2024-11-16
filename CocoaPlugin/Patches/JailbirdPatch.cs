using System.Collections.Generic;
using System.Reflection.Emit;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using HarmonyLib;
using InventorySystem.Items.Jailbird;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(JailbirdHitreg), nameof(JailbirdHitreg.DetectDestructibles))]
public class JailbirdPatch
{
    // public static bool Prefix(JailbirdHitreg __instance)
    // {
    //     var playerCameraReference = __instance._item.Owner.PlayerCameraReference;
    //     var position = playerCameraReference.position + playerCameraReference.forward * __instance._hitregOffset;
    //     JailbirdHitreg._detectionsLen = 0;
    //     var num = Physics.OverlapSphereNonAlloc(position, __instance._hitregRadius, JailbirdHitreg.DetectedColliders,
    //         JailbirdHitreg.DetectionMask);
    //     if (num == 0) return false;
    //     JailbirdHitreg.DetectedNetIds.Clear();
    //     for (var i = 0; i < num; i++)
    //     {
    //         if (JailbirdHitreg.DetectedColliders[i].TryGetComponent(out IDestructible destructible) &&
    //             (!Physics.Linecast(playerCameraReference.position, destructible.CenterOfMass, out var raycastHit,
    //                 LayerMask.GetMask("Default", "Door")) || !(raycastHit.collider != JailbirdHitreg.DetectedColliders[i])) &&
    //             JailbirdHitreg.DetectedNetIds.Add(destructible.NetworkId))
    //             JailbirdHitreg.DetectedDestructibles[JailbirdHitreg._detectionsLen++] = destructible;
    //     }
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        var index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(JailbirdHitreg), nameof(JailbirdHitreg.LinecastMask))));

        newInstructions.RemoveRange(index, 2);

        newInstructions.InsertRange(index, new[]
        {
            // new array
            new CodeInstruction(OpCodes.Ldc_I4_2),
            new CodeInstruction(OpCodes.Newarr, typeof(string)),
            // store "Default", "Door" in the array
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldstr, "Default"),
            new CodeInstruction(OpCodes.Stelem_Ref),
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ldstr, "Door"),
            new CodeInstruction(OpCodes.Stelem_Ref),
            // use the array to LayerMask.GetMask
            new CodeInstruction(OpCodes.Call, Method(typeof(LayerMask), nameof(LayerMask.GetMask), new[] { typeof(string[]) })),
        });

        foreach (var t in newInstructions)
        {
            yield return t;
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}