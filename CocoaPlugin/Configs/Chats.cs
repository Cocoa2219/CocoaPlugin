namespace CocoaPlugin.Configs;

public class Chats
{
    public API.Broadcast ScpChatMessage { get; set; } = new("<cspace=0.05em><size=25><color=%roleColor%>%roleName% <b>%nickname%</b> :</color> %message%</size></cspace>", 10);
}