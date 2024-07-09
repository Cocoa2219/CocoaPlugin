namespace CocoaPlugin.Configs;

public class Reconnects
{
    public int ReconnectTime { get; set; } = 90;

    public int ReconnectLimit { get; set; } = 1;

    public API.Broadcast QuitMessage { get; set; } = new(
        "<cspace=0.05em><size=32><color=%roleColor%>%roleName%<b>%nickname%<size=15> | %userId% </size></b></color>%nicknameParticle% 게임 도중 <color=#d44b42>접속을 종료</color>했습니다.\n<size=25><color=#85ed79>%time% 전 재접속이 가능</color>하며, 미 접속시 해당 유저는 <color=#d44b42>제재 처리</color>되며 <color=#d44b42>대체될 예정</color>입니다.</size></size></cspace>",
        20, 10);

    public API.Broadcast ReconnectMessage { get; set; } = new(
        "<cspace=0.05em><size=32><color=%roleColor%>%roleName% <b>%nickname%</b></color>%nicknameParticle% <color=#d44b42>다시 접속했습니다.</color> </size></cspace>",
        15, 10);

    public API.Broadcast ReplaceMessage { get; set; } = new(
        "<cspace=0.05em><size=32><color=%roleColor%>%roleName%<b>%nickname%<size=15> | %userId% </size></b></color>%nicknameParticle% 제한 시간 전에 <color=#d44b42>접속하지 않아 대체</color>되었습니다.</size></cspace>",
        15, 10);

    public API.Broadcast ReplaceFailedMessage { get; set; } = new(
        "<cspace=0.05em><size=32><color=%roleColor%>%roleName%<b>%nickname%<size=15> | %userId% </size></b></color>%nicknameParticle% 제한 시간 전에 <color=#d44b42>접속하지 않았지만 관전자가 없어 대체 불가능</color>합니다.</size></cspace>",
        15, 10);
}