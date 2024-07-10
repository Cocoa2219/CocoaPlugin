using Exiled.API.Features;
using UnityEngine;

namespace CocoaPlugin.API;

public static class PlayerExtensions
{
    public static string GetRoleColor(this Player player)
    {
        return CocoaPlugin.Instance.Config.Translations.RoleColors[player.Role.Type];
    }

    public static string GetRoleName(this Player player)
    {
        return CocoaPlugin.Instance.Config.Translations.RoleTranslations[player.Role.Type];
    }
}