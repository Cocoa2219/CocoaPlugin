namespace CocoaPlugin.API;

public class BadgeRequest
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public long TextCooldown { get; set; }
    public long ColorCooldown { get; set; }
}