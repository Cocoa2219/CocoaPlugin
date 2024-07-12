using CustomPlayerEffects;
using HarmonyLib;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps;
using PlayerRoles.PlayableScps.Scp049;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;

namespace CocoaPlugin.Patches.ScpPatch;

[HarmonyPatch(typeof(Scp049AttackAbility), nameof(Scp049AttackAbility.ServerProcessCmd))]
public class Scp049AttackServerProcessCmdPatch
{
    public static bool Prefix(Scp049AttackAbility __instance, NetworkReader reader)
    {
        if (!__instance.Cooldown.IsReady || __instance._resurrect.IsInProgress)
        {
            return false;
        }
        __instance._target = reader.ReadReferenceHub();
        if (__instance._target == null || !__instance.IsTargetValid(__instance._target))
        {
            return false;
        }

        var effect = __instance._target.playerEffectsController.GetEffect<CardiacArrest>();
        __instance._isInstaKillAttack = effect.IsEnabled;

        var isInstaKill = effect.IsEnabled || CocoaPlugin.Instance.Config.Scps.Scp049.OneHitKill;

        __instance.Cooldown.Trigger(isInstaKill ? CocoaPlugin.Instance.Config.Scps.Scp049.FirstAttackCooldown : CocoaPlugin.Instance.Config.Scps.Scp049.SecondAttackCooldown);
        __instance._isTarget = __instance._sense.IsTarget(__instance._target);

        if (isInstaKill)
        {
            __instance._target.playerStats.DealDamage(new Scp049DamageHandler(__instance.Owner, -1f, Scp049DamageHandler.AttackType.Instakill));
        }
        else
        {
            effect.SetAttacker(__instance.Owner);
            effect.Intensity = 1;
            effect.ServerChangeDuration(CocoaPlugin.Instance.Config.Scps.Scp049.CardiacArrestDuration);
        }

        var senseAbility = __instance._sense;

        senseAbility.OnServerHit(__instance._target);

        __instance.ServerSendRpc(true);
        Hitmarker.SendHitmarkerDirectly(__instance.Owner, 1f, false);
        return false;
    }
}

[HarmonyPatch(typeof(Scp049SenseAbility), nameof(Scp049SenseAbility.ServerProcessCmd))]
public class Scp049SenseServerProcessCmdPatch
{
    public static bool Prefix(Scp049SenseAbility __instance, NetworkReader reader)
    {
        if (!__instance.Cooldown.IsReady || !__instance.Duration.IsReady)
        {
            return false;
        }
        __instance.HasTarget = false;
        __instance.Target = reader.ReadReferenceHub();
        if (__instance.Target == null)
        {
            __instance.Cooldown.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.SenseNoTargetCooldown);
            __instance.ServerSendRpc(true);
            return false;
        }
        if (!HitboxIdentity.IsEnemy(__instance.Owner, __instance.Target))
        {
            return false;
        }
        var fpcStandardRoleBase = __instance.Target.roleManager.CurrentRole as FpcStandardRoleBase;
        if (fpcStandardRoleBase == null)
        {
            return false;
        }
        var radius = fpcStandardRoleBase.FpcModule.CharController.radius;
        var cameraPosition = fpcStandardRoleBase.CameraPosition;
        if (!VisionInformation.GetVisionInformation(__instance.Owner, __instance.Owner.PlayerCameraReference, cameraPosition, radius, __instance._distanceThreshold, true, true, 0, false).IsLooking)
        {
            return false;
        }

        __instance.Duration.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.SenseDuration);
        __instance.HasTarget = true;
        __instance.ServerSendRpc(true);

        return false;
    }
}

[HarmonyPatch(typeof(Scp049SenseAbility), nameof(Scp049SenseAbility.ServerProcessKilledPlayer))]
public class Scp049SenseServerProcessKilledPlayerPatch
{
    public static bool Prefix(Scp049SenseAbility __instance, ReferenceHub hub)
    {
        if (!__instance.HasTarget || __instance.Target != hub)
        {
            return false;
        }
        __instance.DeadTargets.Add(hub);
        __instance.SpecialZombies.Add(hub);
        __instance.Cooldown.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.SenseKilledCooldown);
        __instance.HasTarget = false;
        __instance.ServerSendRpc(true);

        return false;
    }
}


[HarmonyPatch(typeof(Scp049SenseAbility), nameof(Scp049SenseAbility.ServerLoseTarget))]
public class Scp049SenseServerLoseTargetPatch
{
    public static bool Prefix(Scp049SenseAbility __instance)
    {
        __instance.HasTarget = false;
        __instance.Cooldown.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.SenseLostCooldown);
        __instance.ServerSendRpc(true);

        return false;
    }
}

[HarmonyPatch(typeof(Scp049CallAbility), nameof(Scp049CallAbility.ServerRefreshDuration))]
public class Scp049CallServerRefreshDurationPatch
{
    public static bool Prefix(Scp049CallAbility __instance)
    {
        if (!__instance._serverTriggered || !__instance.Duration.IsReady)
        {
            return false;
        }

        __instance.Cooldown.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.CallCooldown);
        __instance._serverTriggered = false;
        __instance.ServerSendRpc(true);

        return false;
    }
}

[HarmonyPatch(typeof(Scp049CallAbility), nameof(Scp049CallAbility.ServerProcessCmd))]
public class Scp049CallServerProcessCmdPatch
{
    public static bool Prefix(Scp049CallAbility __instance, NetworkReader reader)
    {
        if (__instance._serverTriggered || !__instance.Cooldown.IsReady)
        {
            return false;
        }

        __instance.Duration.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.CallDuration);
        __instance._serverTriggered = true;
        __instance.ServerSendRpc(true);

        return false;
    }
}

[HarmonyPatch(typeof(Scp049CallAbility), nameof(Scp049CallAbility.ClientProcessRpc))]
public class Scp049CallClientProcessRpcPatch
{
    public static bool Prefix(Scp049CallAbility __instance, NetworkReader reader)
    {
        __instance.Cooldown.ReadCooldown(reader);
        __instance.Duration.ReadCooldown(reader);
        if (__instance.Cooldown.Remaining >= CocoaPlugin.Instance.Config.Scps.Scp049.CallCooldown)
        {
            __instance.AbilityAudio(false);
            return false;
        }
        if (__instance.Duration.Remaining >= CocoaPlugin.Instance.Config.Scps.Scp049.CallDuration)
        {
            __instance.AbilityAudio(true);
        }

        return false;
    }
}

[HarmonyPatch(typeof(CardiacArrest), nameof(CardiacArrest.ServerUpdate))]
public class CardiacArrestPatch
{
    public static bool Prefix(CardiacArrest __instance)
    {
        __instance._timeTillTick -= Time.deltaTime;
        if (__instance._timeTillTick > 0f)
        {
            return false;
        }
        __instance._timeTillTick += __instance.TimeBetweenTicks;
        __instance.Hub.playerStats.DealDamage(new Scp049DamageHandler(__instance._attacker, CocoaPlugin.Instance.Config.Scps.Scp049.CardiacArrestDamage, Scp049DamageHandler.AttackType.CardiacArrest));
        return false;
    }
}