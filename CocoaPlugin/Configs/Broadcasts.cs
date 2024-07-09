using System.Collections.Generic;
using CocoaPlugin.Configs.Broadcast;
using Exiled.API.Enums;

namespace CocoaPlugin.Configs;

public class Broadcasts
{
    public API.Broadcast VerifiedMessage { get; set; } = new("<cspace=0.05em><size=30><color=#ffc5c2>%nickname%</color>님, 환영합니다!</size></cspace>\n<cspace=0.05em><size=20><color=#ed9a95>규칙 숙지</color> 부탁드리며, <color=#a5ed95>즐거운 SCP : SL 플레이</color> 되세요!</size></cspace>", 10);

    public API.Broadcast RoundStartMessage { get; set; } = new("<cspace=0.05em><size=30>자, <color=#fcccf0>라운드가 시작되었습니다!</color></size></cspace>", 10);

    public API.Broadcast ScpTerminationMessage { get; set; } = new("<cspace=0.05em><size=30><color=%targetRoleColor%>😈 <b>%targetRoleName%</b></color>%targetRoleNameParticle% <color=%attackerRoleColor%>👤 <b>%attackerRoleName%</b></color>%attackerRoleNameParticle% 인해 격리되었습니다.</size></cspace>", 10);

    public API.Broadcast NtfSpawnMessage { get; set; } = new("<cspace=0.05em><size=30><color=#2071d4>🚁 <b>NTF<size=15> | %unitName%-%unitNumber%</size> 지원</b></color>이 도착했습니다. 격리 대기 중인 <color=#d44b42>😈 <b>SCP 개체는 총 %scpsLeft%개체</b></color>입니다.</size>\n<size=25>남은 생존자 분들은 <color=#d44b42>SCP가 모두 격리될 때까지 대기</color>하여 주십시오.</size></cspace>", 10);

    public API.Broadcast ChaosSpawnMessage { get; set; } = new("<cspace=0.05em><size=30><color=#258533>🚚 <b>혼돈의 반란 지원</b></color>이 도착했습니다.\n<size=25>남은 <color=#ff8000>D계급</color>들은 <color=#d44b42>SCP가 모두 격리될 때까지 대기</color>하여 주십시오.</size></cspace>", 10);

    public API.Broadcast HandcuffMessage { get; set; } = new("<cspace=0.05em><size=25><color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName% <b>%attackerNickname%</b></color>에게 <color=#d42b22>체포</color>되었습니다.</size></cspace>", 20);

    public API.Broadcast HandcuffedKillMessage { get; set; } = new(
        "<cspace=0.05em><size=30><color=#d44b42>🔗 <color=%targetRoleColor%>%targetRoleName%<b> %targetNickname%</b></color></color>%targetNicknameParticle% <color=%attackerRoleColor%>%attackerRoleName% <b>%attackerNickname%</b></color><size=18> | %attackerUserId%</size> 에게 <b><color=#d42b22>체포킬</color></b>을 당했습니다.</size></cspace>",
        20, 10);

    public API.Broadcast ScpSpawnMessage { get; set; } = new("<cspace=0.05em><size=25>현재</size>\n<size=35><color=#d44b42>%scpList%</color></size>\n<size=25><color=#d44b42>😈 <b>%scpCount%</b>개체의 SCP</color>가 존재합니다.</size></cspace>", 10);

    public API.Broadcast LastOneMessage { get; set; } = new("<cspace=0.05em><size=30>자신이 <color=#d44b42><color=%teamColor%>👤 <b>%teamName% 팀</color>의 마지막</b></color>이라는 불길한 예감이 듭니다...</size></cspace>", 10);

    public API.Broadcast ScpHealMessage { get; set; } = new("<cspace=0.05em><size=20>인간을 사살해 <color=#d44b42><b>💧%amount% HP를 회복</b></color>했습니다.</size></cspace>", 10);

    public API.Broadcast WarheadStartMessage { get; set; } = new("<cspace=0.05em><size=30><color=#d44b42>🚀 핵탄두 폭파 절차가 <b>시작</b></color>되었습니다. 폭발까지 <color=#d44b42><b>%amount%</b></color>초 남았습니다.\n<b><size=25><color=#d44b42>빠르게 지상으로 대피하십시오.</color></size></b></size></cspace>", 10);

    public API.Broadcast WarheadCancelMessage { get; set; } = new("<cspace=0.05em><size=30><color=#d44b42>🚀 핵탄두가 폭파 절차가 <b>중지</b></color>되었습니다.</size></cspace>", 10);

    public API.Broadcast AdministrativeBroadcastMessage { get; set; } = new("<cspace=0.05em><size=30>🔑 <color=#d44b42>관리자 공지 | <b>%nickname%</b> : </color>\n<size=40>%message%</size></size></cspace>", 10, 10);

    public Dictionary<DecontaminationState, API.Broadcast> DecontaminationMessages { get; set; } = new()
    {
        { DecontaminationState.Start, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>💀 저위험군 격리 절차</color>까지 <b><color=#32d15d>15분</color></b> 남았습니다.\n<size=20>모든 생존자 분들은 <color=#d44b42>빠르게 대피해</color> 주십시오.</size></cspace></size>", 10) },
        { DecontaminationState.Remain10Minutes, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>💀 저위험군 격리 절차</color>까지 <b><color=#32d15d>10분</color></b> 남았습니다.\n<size=20>모든 생존자 분들은 <color=#d44b42>빠르게 대피해</color> 주십시오.</size></cspace></size>", 10) },
        { DecontaminationState.Remain5Minutes, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>💀 저위험군 격리 절차</color>까지 <b><color=#e3cb2d>5분</color></b> 남았습니다.\n<size=20>모든 생존자 분들은 <color=#d44b42>빠르게 대피해</color> 주십시오.</size></cspace></size>", 10) },
        { DecontaminationState.Remain1Minute, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>💀 저위험군 격리 절차</color>까지 <b><color=#db7827>1분</color></b> 남았습니다.\n<size=20>모든 생존자 분들은 <color=#d44b42>빠르게 대피해</color> 주십시오.</size></cspace></size>", 10) },
        { DecontaminationState.Countdown, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>💀 저위험군 격리 절차</color>까지 <b><color=#e33e19>30초</color></b> 남았습니다.\n<size=23><color=#d44b42><b>모든 검문소의 문이 열렸습니다.</color> 모든 생존자 분들은 <color=#d44b42>빠르게 대피해</color> 주십시오.</b></size></cspace></size>", 10) },
        { DecontaminationState.Lockdown, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>💀 저위험군 격리 절차</color>가 <b><color=#d44b42>시작되었습니다.</color></b>", 10) }
    };

    public Dictionary<int, API.Broadcast> GeneratorMessages { get; set; } = new()
    {
        { 1, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>🔌 3개의 발전기</color> 중 <b><color=#c76d12>1개</color></b>가 활성화 되었습니다.</size></cspace>", 10) },
        { 2, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>🔌 3개의 발전기</color> 중 <b><color=#a6c712>2개</color></b>가 활성화 되었습니다.</size></cspace>", 10) },
        { 3, new API.Broadcast("<cspace=0.05em><size=30><color=#d44b42>🔌 3개의 발전기</color> 중 <b><color=#bf0000>3개</color></b>가 활성화 되었습니다.</size></cspace>", 10) },
    };

    public KillLogs KillLogs { get; set; } = new();

    public Chats Chats { get; set; } = new();
}