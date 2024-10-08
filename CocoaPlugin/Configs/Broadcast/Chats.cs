﻿namespace CocoaPlugin.Configs.Broadcast;

public class Chats
{
    public API.Broadcast ScpChatMessage { get; set; } = new("<cspace=0.05em><size=25><color=%roleColor%>😈 %roleName% <b>%nickname%</b></color> : %text%</size></cspace>", 10);

    public API.Broadcast AdminChatMessage { get; set; } = new(
        "<cspace=0.05em><size=25>\U0001F511 관리자 채팅 | <b>%nickname%</b> : %text%</size></cspace>",
        10);
}