using CocoaPlugin.API.Managers;
using Exiled.API.Features;
using HarmonyLib;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(ServerRoles), nameof(ServerRoles.RefreshPermissions))]
public class RefreshPermissionPatch
{
    [HarmonyPostfix]
    public static void Postfix(ServerRoles __instance)
    {
        var player = Player.Get(__instance._hub);
        var badge = BadgeManager.GetBadge(player.UserId);
        if (badge == default) return;

        player.RankName = badge.Name;
        player.RankColor = badge.Color;
    }
}