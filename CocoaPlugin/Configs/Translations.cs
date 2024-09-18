using System.Collections.Generic;
using PlayerRoles;

namespace CocoaPlugin.Configs;

public class Translations
{
    public Dictionary<RoleTypeId, string> RoleTranslations { get; set; } = new()
    {
        { RoleTypeId.None, "없음" },
        { RoleTypeId.Scp173, "SCP-173" },
        { RoleTypeId.ClassD, "D계급" },
        { RoleTypeId.Spectator, "관전자" },
        { RoleTypeId.Scp106, "SCP-106" },
        { RoleTypeId.NtfSpecialist, "NTF 상등병" },
        { RoleTypeId.Scp049, "SCP-049" },
        { RoleTypeId.Scientist, "과학자" },
        { RoleTypeId.Scp079, "SCP-079" },
        { RoleTypeId.ChaosConscript, "반란 징집병" },
        { RoleTypeId.Scp096, "SCP-096" },
        { RoleTypeId.Scp0492, "SCP-049-2" },
        { RoleTypeId.NtfSergeant, "NTF 병장" },
        { RoleTypeId.NtfCaptain, "NTF 대위" },
        { RoleTypeId.NtfPrivate, "NTF 이등병" },
        { RoleTypeId.Tutorial, "튜토리얼" },
        { RoleTypeId.FacilityGuard, "시설 경비" },
        { RoleTypeId.Scp939, "SCP-939" },
        { RoleTypeId.CustomRole, "특수 역할" },
        { RoleTypeId.ChaosRifleman, "반란 소총수" },
        { RoleTypeId.ChaosMarauder, "반란 약탈자" },
        { RoleTypeId.ChaosRepressor, "반란 압제자" },
        { RoleTypeId.Overwatch, "오버워치" },
        { RoleTypeId.Filmmaker, "필름메이커" },
        { RoleTypeId.Scp3114, "SCP-3114" }
    };

    public Dictionary<RoleTypeId, string> RoleColors { get; set; } = new()
    {
        { RoleTypeId.None, "#ffffff" },
        { RoleTypeId.Scp173, "#d44b42" },
        { RoleTypeId.ClassD, "#ff8000" },
        { RoleTypeId.Spectator, "#737373" },
        { RoleTypeId.Scp106, "#d44b42" },
        { RoleTypeId.NtfSpecialist, "#0096FF" },
        { RoleTypeId.Scp049, "#d44b42" },
        { RoleTypeId.Scientist, "#FFFF7C" },
        { RoleTypeId.Scp079, "#d44b42" },
        { RoleTypeId.ChaosConscript, "#258533" },
        { RoleTypeId.Scp096, "#d44b42" },
        { RoleTypeId.Scp0492, "#d44b42" },
        { RoleTypeId.NtfSergeant, "#0096FF" },
        { RoleTypeId.NtfCaptain, "#0096FF" },
        { RoleTypeId.NtfPrivate, "#0096FF" },
        { RoleTypeId.Tutorial, "#FF00B0" },
        { RoleTypeId.FacilityGuard, "#556278" },
        { RoleTypeId.Scp939, "#d44b42" },
        { RoleTypeId.CustomRole, "#ffffff" },
        { RoleTypeId.ChaosRifleman, "#258533" },
        { RoleTypeId.ChaosMarauder, "#258533" },
        { RoleTypeId.ChaosRepressor, "#258533" },
        { RoleTypeId.Overwatch, "#FFFFFF" },
        { RoleTypeId.Filmmaker, "#0D0D0D" },
        { RoleTypeId.Scp3114, "#d44b42" }
    };

    public Dictionary<Team, string> TeamTranslations { get; set; } = new()
    {
        { Team.SCPs, "SCP" },
        { Team.FoundationForces, "NTF" },
        { Team.ChaosInsurgency, "혼돈의 반란" },
        { Team.Scientists, "과학자" },
        { Team.ClassD, "D계급" },
        { Team.Dead, "관전자" },
        { Team.OtherAlive , "특수"}
    };

    public Dictionary<Team, string> TeamColors { get; set; } = new()
    {
        { Team.SCPs, "#d44b42" },
        { Team.FoundationForces, "#0096FF" },
        { Team.ChaosInsurgency, "#258533" },
        { Team.Scientists, "#FFFF7C" },
        { Team.ClassD, "#ff8000" },
        { Team.Dead, "#737373" },
        { Team.OtherAlive, "#ffffff" }
    };
}