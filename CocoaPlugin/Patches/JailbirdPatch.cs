using Exiled.API.Features;
using Exiled.API.Features.Toys;
using HarmonyLib;
using InventorySystem.Items.Jailbird;
using MEC;
using UnityEngine;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(JailbirdHitreg), nameof(JailbirdHitreg.DetectDestructibles))]
public class JailbirdPatch
{
    public static bool Prefix(JailbirdHitreg __instance)
    {
        var playerCameraReference = __instance._item.Owner.PlayerCameraReference;
        var position = playerCameraReference.position + playerCameraReference.forward * __instance._hitregOffset;
        JailbirdHitreg._detectionsLen = 0;
        var num = Physics.OverlapSphereNonAlloc(position, __instance._hitregRadius, JailbirdHitreg.DetectedColliders,
            JailbirdHitreg.DetectionMask);
        if (num == 0) return false;
        JailbirdHitreg.DetectedNetIds.Clear();
        for (var i = 0; i < num; i++)
        {
            if (JailbirdHitreg.DetectedColliders[i].TryGetComponent(out IDestructible destructible) &&
                (!Physics.Linecast(playerCameraReference.position, destructible.CenterOfMass, out var raycastHit,
                    LayerMask.GetMask("Default", "Door")) || !(raycastHit.collider != JailbirdHitreg.DetectedColliders[i])) &&
                JailbirdHitreg.DetectedNetIds.Add(destructible.NetworkId))
                JailbirdHitreg.DetectedDestructibles[JailbirdHitreg._detectionsLen++] = destructible;
        }

        return false;
    }
}