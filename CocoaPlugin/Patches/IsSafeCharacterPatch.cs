using HarmonyLib;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(Misc), nameof(Misc.IsSafeCharacter))]
public class IsSafeCharacterPatch
{
    public static bool Prefix(char c, ref bool __result)
    {
        __result = true;
        return false;
    }
}