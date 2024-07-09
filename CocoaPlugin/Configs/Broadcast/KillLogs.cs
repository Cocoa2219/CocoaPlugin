using System.Collections.Generic;
using Exiled.API.Enums;

namespace CocoaPlugin.Configs.Broadcast;

public class KillLogs
{
    public Dictionary<KillType, API.Broadcast> KillLog { get; set; } = new()
    {
        { KillType.Unknown , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>알 수 없는 이유로</color> 사망했습니다.</size></cspace>", 10)},
        { KillType.Human , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>🔪 살해당했습니다.</color></size></cspace>", 10)},
        { KillType.Scp , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>😈 살해당했습니다.</color></size></cspace>", 10)},
        { KillType.Warhead , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>알파 핵탄두</color>에 의해 <color=#d44b42>💣 폭파당했습니다.</color></size></cspace>", 10)},
        { KillType.Tesla , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>자동 보안 장치</color>에 의해 <color=#d44b42>⚡ 감전당했습니다.</color></size></cspace>", 10)},
        { KillType.Recontained , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>⚡ 재격리</color>되었습니다.</size></cspace>", 10)},
        { KillType.PocketDimension , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>🌀 주머니 차원에서 부식</color>되었습니다.</color></size></cspace>", 10)},
        { KillType.Decontamination , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>💀 저위험군 격리 절차</color>에 의해 <color=#d44b42>사망했습니다.</color></size></cspace>", 10)},
        { KillType.Falldown , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>⬇️ 중력</color>에 의해 <color=#d44b42>사망했습니다.</color></size></cspace>", 10)},
        { KillType.StatusEffect , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>💉 상태 효과</color>에 의해 <color=#d44b42>사망했습니다.</color></size></cspace>", 10)},
        { KillType.Hypothermia, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>☃️ 얼어 사망했습니다.</color></size></cspace>", 10)},
        { KillType.Scp018, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>%attackerNicknameParticle% 던진 <color=#d44b42>SCP-018</color>에 의해 <color=#d44b42>🎳 관통당했습니다.</color></size></cspace>", 10)},
        { KillType.ParticleDisruptor, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42><color=#29f8ff>✨</color> 증발당했습니다.</color></size></cspace>", 10)},
        { KillType.Scp207, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>💧 동맥 파열</color>로 사망했습니다.</size></cspace>", 10)},
        { KillType.MicroHid, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>⚡ 심한 전기 화상</color>을 입었습니다.</size></cspace>", 10)},
        { KillType.Explosion, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>💣 폭사했습니다.</color></size></cspace>", 10)},
        { KillType.SevereBleeding, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>💧 과다 출혈</color>로 사망했습니다.</size></cspace>", 10)},
        { KillType.Jailbird, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>💢 맞아 사망했습니다.</color></size></cspace>", 10)},
        { KillType.CardiacArrest, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>💓 심장 마비로 사망했습니다.</color></size></cspace>", 10)},
        { KillType.Laceration, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>💔 몸이 찢겨 사망했습니다.</color></size></cspace>", 10)},
        { KillType.NeckSnap, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>😈 목이 꺾여 사망했습니다.</color></size></cspace>", 10)},
        { KillType.Abrasion, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>😈 찰과상으로 사망했습니다.</color></size></cspace>", 10)},
        { KillType.Corrosion, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>😈 몸이 부식당해 사망했습니다.</color></size></cspace>", 10)},
        { KillType.Strangled, new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName%<b> %attackerNickname%</b></color>에 의해 <color=#d44b42>😈 목이 졸려 사망했습니다.</color></size></cspace>", 10)},
        { KillType.Crushed , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>⬇️ 무거운 물체에 깔려</color> 사망했습니다.</size></cspace>", 10)}
    };

    public static KillType DamageTypeToKillType(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Unknown => KillType.Unknown,
            DamageType.Falldown => KillType.Falldown,
            DamageType.Warhead => KillType.Warhead,
            DamageType.Decontamination => KillType.Decontamination,
            DamageType.Asphyxiation => KillType.StatusEffect,
            DamageType.Poison => KillType.StatusEffect,
            DamageType.Bleeding => KillType.StatusEffect,
            DamageType.Firearm => KillType.Human,
            DamageType.MicroHid => KillType.MicroHid,
            DamageType.Tesla => KillType.Tesla,
            DamageType.Scp => KillType.Scp,
            DamageType.Explosion => KillType.Explosion,
            DamageType.Scp018 => KillType.Scp018,
            DamageType.Scp207 => KillType.Scp207,
            DamageType.Recontainment => KillType.Recontained,
            DamageType.Crushed => KillType.Crushed,
            DamageType.FemurBreaker => KillType.Unknown,
            DamageType.PocketDimension => KillType.PocketDimension,
            DamageType.FriendlyFireDetector => KillType.Human,
            DamageType.SeveredHands => KillType.SevereBleeding,
            DamageType.Custom => KillType.Unknown,
            DamageType.Scp049 => KillType.CardiacArrest,
            DamageType.Scp096 => KillType.Laceration,
            DamageType.Scp173 => KillType.NeckSnap,
            DamageType.Scp939 => KillType.Laceration,
            DamageType.Scp0492 => KillType.Abrasion,
            DamageType.Scp106 => KillType.Corrosion,
            DamageType.Crossvec => KillType.Human,
            DamageType.Logicer => KillType.Human,
            DamageType.Revolver => KillType.Human,
            DamageType.Shotgun => KillType.Human,
            DamageType.AK => KillType.Human,
            DamageType.Com15 => KillType.Human,
            DamageType.Com18 => KillType.Human,
            DamageType.Fsp9 => KillType.Human,
            DamageType.E11Sr => KillType.Human,
            DamageType.Hypothermia => KillType.Hypothermia,
            DamageType.ParticleDisruptor => KillType.ParticleDisruptor,
            DamageType.CardiacArrest => KillType.CardiacArrest,
            DamageType.Com45 => KillType.Human,
            DamageType.Jailbird => KillType.Jailbird,
            DamageType.Frmg0 => KillType.Human,
            DamageType.A7 => KillType.Human,
            DamageType.Scp3114 => KillType.Abrasion,
            DamageType.Strangled => KillType.Strangled,
            DamageType.Marshmallow => KillType.Human,
            _ => KillType.Unknown
        };
    }
}

public enum KillType
{
    Unknown,
    Human,
    Scp,
    Warhead,
    Tesla,
    Recontained,
    PocketDimension,
    Decontamination,
    Falldown,
    StatusEffect,
    Hypothermia,
    Scp018,
    ParticleDisruptor,
    Scp207,
    MicroHid,
    Explosion,
    SevereBleeding,
    Jailbird,
    CardiacArrest,
    Laceration,
    NeckSnap,
    Abrasion,
    Corrosion,
    Strangled,
    Crushed
}