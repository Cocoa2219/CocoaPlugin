using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using CocoaPlugin.Configs;
using CocoaPlugin.Configs.Scp;
using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using Exiled.Events.EventArgs.Scp049;
using HarmonyLib;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Subroutines;
using static HarmonyLib.AccessTools;
// ReSharper disable SuggestVarOrType_SimpleTypes

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace CocoaPlugin.Patches;

#region Scp049
[HarmonyPatch(typeof(Scp049AttackAbility), nameof(Scp049AttackAbility.ServerProcessCmd))]
public class Scp049AttackServerProcessCmdPatch
{
    // public static bool Prefix(Scp049AttackAbility __instance, NetworkReader reader)
    // {
    //     if (!__instance.Cooldown.IsReady || __instance._resurrect.IsInProgress)
    //     {
    //         return false;
    //     }
    //     __instance._target = reader.ReadReferenceHub();
    //     if (__instance._target == null || !__instance.IsTargetValid(__instance._target))
    //     {
    //         return false;
    //     }
    //
    //     var effect = __instance._target.playerEffectsController.GetEffect<CardiacArrest>();
    //     __instance._isInstaKillAttack = effect.IsEnabled;
    //
    //     var isInstaKill = effect.IsEnabled || CocoaPlugin.Instance.Config.Scps.Scp049.OneHitKill;
    //
    //     __instance.Cooldown.Trigger(isInstaKill ? CocoaPlugin.Instance.Config.Scps.Scp049.SecondAttackCooldown : CocoaPlugin.Instance.Config.Scps.Scp049.FirstAttackCooldown);
    //     __instance._isTarget = __instance._sense.IsTarget(__instance._target);
    //
    //     if (isInstaKill)
    //     {
    //         __instance._target.playerStats.DealDamage(new Scp049DamageHandler(__instance.Owner, -1f, Scp049DamageHandler.AttackType.Instakill));
    //     }
    //     else
    //     {
    //         effect.SetAttacker(__instance.Owner);
    //         effect.Intensity = 1;
    //         effect.ServerChangeDuration(CocoaPlugin.Instance.Config.Scps.Scp049.CardiacArrestDuration);
    //     }
    //
    //     var senseAbility = __instance._sense;
    //
    //     senseAbility.OnServerHit(__instance._target);
    //
    //     __instance.ServerSendRpc(true);
    //     Hitmarker.SendHitmarkerDirectly(__instance.Owner, 1f, false);
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.Calls(Method(typeof(AbilityCooldown), nameof(AbilityCooldown.Trigger))));
        index -= 3;

        LocalBuilder cooldown = generator.DeclareLocal(typeof(float));
        Label instaKillCooldownJmp = generator.DefineLabel();
        Label end = generator.DefineLabel();

        // before triggering cooldown, need to remove the original one
        newInstructions[index + 4].MoveLabelsFrom(newInstructions[index]);
        newInstructions.RemoveRange(index, 4);

        index = newInstructions.FindIndex(instruction => instruction.Calls(PropertyGetter(typeof(StatusEffectBase), nameof(StatusEffectBase.IsEnabled))));
        index += 1;

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.OneHitKill));
        configValue.Add(new CodeInstruction(OpCodes.Or));

        newInstructions.InsertRange(index, configValue);

        // after saving to _isInstaKillAttack
        index += 2;

        newInstructions.InsertRange(index, new []
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp049AttackAbility), nameof(Scp049AttackAbility._isInstaKillAttack))),
            new CodeInstruction(OpCodes.Brtrue_S, instaKillCooldownJmp),
            new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(CocoaPlugin), nameof(CocoaPlugin.Instance))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Plugin<Config>), nameof(Plugin<Config>.Config))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Config), nameof(Config.Scps))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Scps), nameof(Scps.Scp049))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Scp049), nameof(Scp049.SecondAttackCooldown))),
            new CodeInstruction(OpCodes.Br_S, end),
            new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(CocoaPlugin), nameof(CocoaPlugin.Instance))).WithLabels(instaKillCooldownJmp),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Plugin<Config>), nameof(Plugin<Config>.Config))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Config), nameof(Config.Scps))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Scps), nameof(Scps.Scp049))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Scp049), nameof(Scp049.FirstAttackCooldown))),
            new CodeInstruction(OpCodes.Stloc, cooldown).WithLabels(end),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp049AttackAbility), nameof(Scp049AttackAbility.Cooldown))),
            new CodeInstruction(OpCodes.Ldloc, cooldown),
            new CodeInstruction(OpCodes.Callvirt, Method(typeof(AbilityCooldown), nameof(AbilityCooldown.Trigger))),
        });

        index = newInstructions.FindLastIndex(instruction => instruction.Calls(PropertyGetter(typeof(StatusEffectBase), nameof(StatusEffectBase.IsEnabled))));
        index -= 1;

        newInstructions.RemoveRange(index, 2);

        newInstructions.InsertRange(index, new []
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(Scp049AttackAbility), nameof(Scp049AttackAbility._isInstaKillAttack))),
        });

        index = newInstructions.FindIndex(instruction => instruction.Calls(Method(typeof(StatusEffectBase), nameof(StatusEffectBase.ServerChangeDuration))));
        index -= 3;

        newInstructions.RemoveRange(index, 2);

        configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.CardiacArrestDuration));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp049SenseAbility), nameof(Scp049SenseAbility.ServerProcessCmd))]
public class Scp049SenseServerProcessCmdPatch
{
    // public static bool Prefix(Scp049SenseAbility __instance, NetworkReader reader)
    // {
    //     if (!__instance.Cooldown.IsReady || !__instance.Duration.IsReady)
    //     {
    //         return false;
    //     }
    //     __instance.HasTarget = false;
    //     __instance.Target = reader.ReadReferenceHub();
    //     if (__instance.Target == null)
    //     {
    //         __instance.Cooldown.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.SenseNoTargetCooldown);
    //         __instance.ServerSendRpc(true);
    //         return false;
    //     }
    //     if (!HitboxIdentity.IsEnemy(__instance.Owner, __instance.Target))
    //     {
    //         return false;
    //     }
    //     var fpcStandardRoleBase = __instance.Target.roleManager.CurrentRole as FpcStandardRoleBase;
    //     if (fpcStandardRoleBase == null)
    //     {
    //         return false;
    //     }
    //     var radius = fpcStandardRoleBase.FpcModule.CharController.radius;
    //     var cameraPosition = fpcStandardRoleBase.CameraPosition;
    //     if (!VisionInformation.GetVisionInformation(__instance.Owner, __instance.Owner.PlayerCameraReference, cameraPosition, radius, __instance._distanceThreshold, true, true, 0, false).IsLooking)
    //     {
    //         return false;
    //     }
    //
    //     __instance.Duration.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.SenseDuration);
    //     __instance.HasTarget = true;
    //     __instance.ServerSendRpc(true);
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        // foreach (var instruction in newInstructions)
        // {
        //     Log.Info($"{instruction.opcode} {instruction.operand}");
        // }

        int index = newInstructions.FindIndex(instruction => instruction.Calls(PropertyGetter(typeof(ActivatingSenseEventArgs), nameof(ActivatingSenseEventArgs.FailedCooldown))));
        int offset = -1;

        index += offset;

        newInstructions.RemoveRange(index, 3);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.SenseNoTargetCooldown));

        newInstructions.InsertRange(index, configValue);

        index = newInstructions.FindLastIndex(instruction => instruction.Calls(PropertyGetter(typeof(ActivatingSenseEventArgs), nameof(ActivatingSenseEventArgs.Duration))));

        index += offset;

        newInstructions.RemoveRange(index, 3);

        configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.SenseDuration));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp049SenseAbility), nameof(Scp049SenseAbility.ServerProcessKilledPlayer))]
public class Scp049SenseServerProcessKilledPlayerPatch
{
    // public static bool Prefix(Scp049SenseAbility __instance, ReferenceHub hub)
    // {
    //     if (!__instance.HasTarget || __instance.Target != hub)
    //     {
    //         return false;
    //     }
    //     __instance.DeadTargets.Add(hub);
    //     __instance.SpecialZombies.Add(hub);
    //     __instance.Cooldown.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.SenseKilledCooldown);
    //     __instance.HasTarget = false;
    //     __instance.ServerSendRpc(true);
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R8);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.SenseKilledCooldown));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp049SenseAbility), nameof(Scp049SenseAbility.ServerLoseTarget))]
public class Scp049SenseServerLoseTargetPatch
{
    // public static bool Prefix(Scp049SenseAbility __instance)
    // {
    //     __instance.HasTarget = false;
    //     __instance.Cooldown.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.SenseLostCooldown);
    //     __instance.ServerSendRpc(true);
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R8);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.SenseLostCooldown));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp049CallAbility), nameof(Scp049CallAbility.ServerRefreshDuration))]
public class Scp049CallServerRefreshDurationPatch
{
    // public static bool Prefix(Scp049CallAbility __instance)
    // {
    //     if (!__instance._serverTriggered || !__instance.Duration.IsReady)
    //     {
    //         return false;
    //     }
    //
    //     __instance.Cooldown.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.CallCooldown);
    //     __instance._serverTriggered = false;
    //     __instance.ServerSendRpc(true);
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R8);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.CallCooldown));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp049CallAbility), nameof(Scp049CallAbility.ServerProcessCmd))]
public class Scp049CallServerProcessCmdPatch
{
    // public static bool Prefix(Scp049CallAbility __instance, NetworkReader reader)
    // {
    //     if (__instance._serverTriggered || !__instance.Cooldown.IsReady)
    //     {
    //         return false;
    //     }
    //
    //     __instance.Duration.Trigger(CocoaPlugin.Instance.Config.Scps.Scp049.CallDuration);
    //     __instance._serverTriggered = true;
    //     __instance.ServerSendRpc(true);
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.Calls(PropertyGetter(typeof(SendingCallEventArgs), nameof(SendingCallEventArgs.Duration))));
        int offset = -1;

        index += offset;

        newInstructions.RemoveRange(index, 2);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.CallDuration));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp049CallAbility), nameof(Scp049CallAbility.ClientProcessRpc))]
public class Scp049CallClientProcessRpcPatch
{
    // public static bool Prefix(Scp049CallAbility __instance, NetworkReader reader)
    // {
    //     __instance.Cooldown.ReadCooldown(reader);
    //     __instance.Duration.ReadCooldown(reader);
    //     if (__instance.Cooldown.Remaining >= CocoaPlugin.Instance.Config.Scps.Scp049.CallCooldown)
    //     {
    //         __instance.AbilityAudio(false);
    //         return false;
    //     }
    //     if (__instance.Duration.Remaining >= CocoaPlugin.Instance.Config.Scps.Scp049.CallDuration)
    //     {
    //         __instance.AbilityAudio(true);
    //     }
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.CallCooldown));

        newInstructions.InsertRange(index, configValue);
        index = newInstructions.FindLastIndex(instruction => instruction.opcode == OpCodes.Ldc_R4);

        newInstructions.RemoveRange(index, 1);

        configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.CallDuration));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(CardiacArrest), nameof(CardiacArrest.ServerUpdate))]
public class CardiacArrestPatch
{
    // public static bool Prefix(CardiacArrest __instance)
    // {
    //     __instance._timeTillTick -= Time.deltaTime;
    //     if (__instance._timeTillTick > 0f)
    //     {
    //         return false;
    //     }
    //     __instance._timeTillTick += __instance.TimeBetweenTicks;
    //     __instance.Hub.playerStats.DealDamage(new Scp049DamageHandler(__instance._attacker, CocoaPlugin.Instance.Config.Scps.Scp049.CardiacArrestDamage, Scp049DamageHandler.AttackType.CardiacArrest));
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindLastIndex(instruction => instruction.opcode == OpCodes.Ldc_R4);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.CardiacArrestDamage));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}
#endregion Scp049

#region Scp0492
[HarmonyPatch(typeof(ZombieAttackAbility), nameof(ZombieAttackAbility.DamageAmount), MethodType.Getter)]
public class ZombieAttackDamageAmountPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.ZombieDamage));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(ZombieAttackAbility), nameof(ZombieAttackAbility.BaseCooldown), MethodType.Getter)]
public class ZombieAttackCooldownAmountPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.ZombieAttackCooldown));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(ZombieConsumeAbility), nameof(ZombieConsumeAbility.ServerComplete))]
public class ZombieConsumeServerCompletePatch
{
    // public static bool Prefix(ZombieConsumeAbility __instance)
    // {
    //     __instance._target.playerStats.HealHP(CocoaPlugin.Instance.Config.Scps.Scp049.ZombieConsumeHealAmount);
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.ZombieConsumeHealAmount));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(ZombieShieldController), nameof(ZombieShieldController.CheckDistanceTo))]
public class ZombieShieldCheckDistanceToPatch
{
    // public static bool Prefix(ZombieShieldController __instance, ReferenceHub hub)
    // {
    //     if (__instance._zombie.Owner == hub)
    //     {
    //         return false;
    //     }
    //     if (__instance._zombie.Owner.playerMovementSync.GetRealDistanceSqr(hub) > CocoaPlugin.Instance.Config.Scps.Scp049.ZombieHsRegenerationMaxDistanceSqr)
    //     {
    //         return false;
    //     }
    //     __instance._zombie.Owner.playerStats.HealHP(CocoaPlugin.Instance.Config.Scps.Scp049.ZombieConsumeHealAmount);
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.ZombieHsRegenerationMaxDistanceSqr));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(ZombieShieldController), nameof(ZombieShieldController.HsRegeneration), MethodType.Getter)]
public class ZombieShieldHsRegenerationPatch
{
    // public static bool Prefix(ZombieShieldController __instance)
    // {
    //     if (__instance._zombie.Owner.playerStats.Health >= CocoaPlugin.Instance.Config.Scps.Scp049.ZombieHumeShieldMax)
    //     {
    //         return false;
    //     }
    //     __instance._zombie.Owner.playerStats.HealHP(CocoaPlugin.Instance.Config.Scps.Scp049.ZombieHumeShieldRegeneration);
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.Calls(PropertyGetter(typeof(DynamicHumeShieldController), nameof(DynamicHumeShieldController.HsRegeneration))));
        index -= 1;

        newInstructions.RemoveRange(index, 2);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.ZombieHumeShieldRegeneration));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(ZombieShieldController), nameof(ZombieShieldController.HsMax), MethodType.Getter)]
public class ZombieShieldHsMaxPatch
{
    // public static bool Prefix(ZombieShieldController __instance)
    // {
    //     return __instance._zombie.Owner.playerStats.Health < CocoaPlugin.Instance.Config.Scps.Scp049.ZombieHumeShieldMax;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4);

        newInstructions.RemoveRange(index, 1);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp049), nameof(Scp049.ZombieHumeShieldMax));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

#endregion Scp0492

#region Scp079

[HarmonyPatch(typeof(Scp079AuxManager), nameof(Scp079AuxManager.RegenSpeed))]
public class Scp079RegenPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(Scp079AuxManager), nameof(Scp079AuxManager._regenerationPerTier))));

        index -= 1;

        newInstructions.RemoveRange(index, 2);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.AuxRegeneration));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility.GetCapacityOfTier))]
public class Scp079BlackoutRoomCapacityPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility._capacityPerTier))));

        index--;

        newInstructions.RemoveRange(index, 2);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.BlackoutRoomCapacity));

        newInstructions.InsertRange(index, configValue);

        index = newInstructions.FindLastIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility._capacityPerTier))));

        index--;

        newInstructions.RemoveRange(index, 2);

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility.AbilityCost), MethodType.Getter)]
public class Scp079BlackoutRoomCostPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility._cost))));

        // move to ldarg.0
        index--;

        // ldarg ~ ldfld
        newInstructions.RemoveRange(index, 2);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.BlackoutRoomCost));

        newInstructions.InsertRange(index, configValue);

        index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility._surfaceCost))));

        index--;

        newInstructions.RemoveRange(index, 2);

        configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.SurfaceBlackoutCost));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility.ServerProcessCmd))]
public class Scp079BlackoutRoomServerProcessCmdPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility._cooldown))));

        index -= 1;

        newInstructions.RemoveRange(index, 2);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.BlackoutRoomCooldown));

        newInstructions.InsertRange(index, configValue);

        index = newInstructions.FindLastIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutRoomAbility), nameof(Scp079BlackoutRoomAbility._blackoutDuration))));
        index -= 1;

        newInstructions.RemoveRange(index, 2);

        configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.BlackoutRoomDuration));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}

[HarmonyPatch(typeof(Scp079BlackoutZoneAbility), nameof(Scp079BlackoutZoneAbility.ServerProcessCmd))]
public class Scp079BlackoutZoneServerProcessCmdPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        int index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutZoneAbility), nameof(Scp079BlackoutZoneAbility._cooldown))));

        index -= 1;

        newInstructions.RemoveRange(index, 2);

        List<CodeInstruction> configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.BlackoutZoneCooldown));

        newInstructions.InsertRange(index, configValue);

        index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutZoneAbility), nameof(Scp079BlackoutZoneAbility._cost))));

        index -= 1;

        newInstructions.RemoveRange(index, 2);

        configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.BlackoutZoneCost));

        newInstructions.InsertRange(index, configValue);

        index = newInstructions.FindIndex(instruction => instruction.LoadsField(Field(typeof(Scp079BlackoutZoneAbility), nameof(Scp079BlackoutZoneAbility._duration))));

        index -= 1;

        newInstructions.RemoveRange(index, 2);

        configValue = ScpPatchUtility.GetConfigValue(typeof(Scp079), nameof(Scp079.BlackoutZoneDuration));

        newInstructions.InsertRange(index, configValue);

        for (var i = 0; i < newInstructions.Count; i++)
        {
            yield return newInstructions[i];
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}
#endregion Scp079

public static class ScpPatchUtility
{
    public static List<CodeInstruction> GetConfigValue(Type configType, string configName)
    {
        return
        [
            new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(CocoaPlugin), nameof(CocoaPlugin.Instance))),
            new CodeInstruction(OpCodes.Callvirt,
                PropertyGetter(typeof(Plugin<Config>), nameof(Plugin<Config>.Config))),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Config), nameof(Config.Scps))),
            new CodeInstruction(OpCodes.Callvirt,
                PropertyGetter(typeof(Scps),
                    typeof(Scps).GetProperties().First(x => x.PropertyType == configType).Name)),
            new CodeInstruction(OpCodes.Callvirt, PropertyGetter(configType, configName))
        ];
    }
}