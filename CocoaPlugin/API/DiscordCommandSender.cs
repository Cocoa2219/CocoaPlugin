namespace CocoaPlugin.API;

public class DiscordCommandSender : CommandSender
{
    public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
    {

    }

    public override void Print(string text)
    {

    }

    public override string SenderId { get; }
    public override string Nickname { get; }
    public override ulong Permissions { get; }
    public override byte KickPower { get; }
    public override bool FullPermissions { get; }

    public DiscordCommandSender(string senderId, string nickname, ulong permissions, byte kickPower, bool fullPermissions)
    {
        SenderId = senderId;
        Nickname = nickname;
        Permissions = permissions;
        KickPower = kickPower;
        FullPermissions = fullPermissions;
    }
}