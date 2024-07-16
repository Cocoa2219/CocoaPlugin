namespace CocoaPlugin.API;

public class BanCommand(string issuerId, string userId, string ipAddress, long from, long until, string reason)
{
    public string IssuerId { get; set; } = issuerId;
    public string UserId { get; set; } = userId;
    public string IpAddress { get; set; } = ipAddress;
    public long From { get; set; } = from;
    public long Until { get; set; } = until;
    public string Reason { get; set; } = reason;
}