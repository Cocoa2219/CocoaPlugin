using System.Collections.Generic;
using PlayerRoles;

namespace CocoaPlugin.Configs;

public class Afk
{
    public API.Broadcast AfkMessage { get; set; } =
        new(
            "<cspace=0.05em><size=30><color=#d44b42>💤 플레이어가 비활성 상태임</color>을 확인했습니다.\n<size=25>아무런 입력이 없는 경우 <color=#d44b42>%amount%초 뒤 추방됩니다.</color></size></size></cspace>",
            30, 10);

    public float AfkCheckInterval { get; set; } = 1f;

    public float AfkSqrMagnitude { get; set; } = 1f;

    public List<RoleTypeId> ExcludedRoles { get; set; } = [];

    public bool IgnoreGodmode { get; set; } = true;

    public bool IgnoreNoclip { get; set; } = true;
}