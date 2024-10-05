using CocoaPlugin.API.Managers;
using Exiled.API.Features;
using HarmonyLib;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(ServerRoles), nameof(ServerRoles.RefreshPermissions))]
public class RefreshPermissionPatch
{
    public static void Postfix(ServerRoles __instance)
    {
        var player = Player.Get(__instance._hub);
        var badge = BadgeManager.GetBadge(player.UserId);
        var level = RankManager.GetRank(player.UserId).Level.ToString();

        if (badge == null)
        {
            player.RankName = CocoaPlugin.Instance.Config.Ranks.BadgeFormat.Replace("%badge%", "")
                .Replace("%level%", level).Replace(" | ", "");

            player.RankColor = "white";

            return;
        }

        player.RankName = CocoaPlugin.Instance.Config.Ranks.BadgeFormat.Replace("%badge%", badge.Name)
            .Replace("%level%", level);
        player.RankColor = badge.Color;
    }
}