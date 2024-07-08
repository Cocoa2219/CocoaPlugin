using PlayerRoles;

namespace CocoaPlugin.API;

public static class RoleTypeIdExtensions
{
    public static string GetRoleColor(this RoleTypeId role)
    {
        return Cocoa.Instance.Config.Translations.RoleColors[role];
    }

    public static string GetRoleName(this RoleTypeId role)
    {
        return Cocoa.Instance.Config.Translations.RoleTranslations[role];
    }
}