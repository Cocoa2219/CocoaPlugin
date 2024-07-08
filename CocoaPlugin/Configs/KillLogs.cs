using System;
using System.Collections.Generic;
using Exiled.API.Enums;

namespace CocoaPlugin.Configs;

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
        { KillType.Falldown , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>🍎 중력</color>에 의해 <color=#d44b42>사망했습니다.</color></size></cspace>", 10)},
        { KillType.StatusEffect , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>💉 상태 효과</color>에 의해 <color=#d44b42>사망했습니다.</color></size></cspace>", 10)},
        { KillType.Crushed , new API.Broadcast("<cspace=0.05em><size=24><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=#d44b42>⬇️ 무거운 물체에 깔려</color> 사망했습니다.</size></cspace>", 10)}
    };

    public static KillType DamageTypeToKillType(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Unknown:
                return KillType.Unknown;
            case DamageType.Falldown:
                return KillType.Falldown;
            case DamageType.Warhead:
                return KillType.Warhead;
            case DamageType.Decontamination:
                return KillType.Decontamination;
            case DamageType.Asphyxiation:
                return KillType.StatusEffect;
            case DamageType.Poison:
                return KillType.StatusEffect;
            case DamageType.Bleeding:
                return KillType.StatusEffect;
            case DamageType.Firearm:
                return KillType.Human;
            case DamageType.MicroHid:
                return KillType.Human;
            case DamageType.Tesla:
                return KillType.Tesla;
            case DamageType.Scp:
                return KillType.Scp;
            case DamageType.Explosion:
                return KillType.Human;
            case DamageType.Scp018:
                return KillType.Human;
            case DamageType.Scp207:
                return KillType.StatusEffect;
            case DamageType.Recontainment:
                return KillType.Recontained;
            case DamageType.Crushed:
                return KillType.Crushed;
            case DamageType.FemurBreaker:
                return KillType.Unknown;
            case DamageType.PocketDimension:
                return KillType.PocketDimension;
            case DamageType.FriendlyFireDetector:
                return KillType.Human;
            case DamageType.SeveredHands:
                return KillType.Human;
            case DamageType.Custom:
                return KillType.Unknown;
            case DamageType.Scp049:
                return KillType.Scp;
            case DamageType.Scp096:
                return KillType.Scp;
            case DamageType.Scp173:
                return KillType.Scp;
            case DamageType.Scp939:
                return KillType.Scp;
            case DamageType.Scp0492:
                return KillType.Scp;
            case DamageType.Scp106:
                return KillType.Scp;
            case DamageType.Crossvec:
                return KillType.Human;
            case DamageType.Logicer:
                return KillType.Human;
            case DamageType.Revolver:
                return KillType.Human;
            case DamageType.Shotgun:
                return KillType.Human;
            case DamageType.AK:
                return KillType.Human;
            case DamageType.Com15:
                return KillType.Human;
            case DamageType.Com18:
                return KillType.Human;
            case DamageType.Fsp9:
                return KillType.Human;
            case DamageType.E11Sr:
                return KillType.Human;
            case DamageType.Hypothermia:
                return KillType.StatusEffect;
            case DamageType.ParticleDisruptor:
                return KillType.Human;
            case DamageType.CardiacArrest:
                return KillType.Scp;
            case DamageType.Com45:
                return KillType.Human;
            case DamageType.Jailbird:
                return KillType.Human;
            case DamageType.Frmg0:
                return KillType.Human;
            case DamageType.A7:
                return KillType.Human;
            case DamageType.Scp3114:
                return KillType.Scp;
            case DamageType.Strangled:
                return KillType.Scp;
            case DamageType.Marshmallow:
                return KillType.Human;
            default:
                return KillType.Unknown;
        }
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
    Crushed
}