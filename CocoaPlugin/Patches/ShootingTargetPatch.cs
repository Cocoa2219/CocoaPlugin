using System.Linq;
using AdminToys;
using Exiled.API.Features;
using HarmonyLib;
using PlayerStatsSystem;
using PluginAPI.Events;
using UnityEngine;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(ShootingTarget), nameof(ShootingTarget.Damage))]
public class ShootingTargetPatch
{
    public static bool Prefix(ShootingTarget __instance, float damage, DamageHandlerBase handler, Vector3 exactHit, ref bool __result)
    {
        if (handler is not AttackerDamageHandler attackerDamageHandler)
        {
            __result = false;
            return false;
        }
        var hub = attackerDamageHandler.Attacker.Hub;
        if (hub == null)
        {
            __result = false;
            return false;
        }
        var playerDamagedShootingTargetEvent = new PlayerDamagedShootingTargetEvent(hub, __instance, handler, damage);
        if (!EventManager.ExecuteEvent(playerDamagedShootingTargetEvent))
        {
            __result = false;
            return false;
        }
        damage = playerDamagedShootingTargetEvent.DamageAmount;
        var distance = Vector3.Distance(hub.transform.position, __instance._bullsEye.position);
        foreach (var referenceHub in ReferenceHub.AllHubs.Where(referenceHub => __instance._syncMode || referenceHub == hub))
            __instance.TargetRpcReceiveData(referenceHub.characterClassManager.connectionToClient, damage, distance,
                exactHit, handler);
        __result = true;
        return false;
    }
}