namespace CocoaPlugin.API;

public class DiscordCommand(string command, string username)
{
    public string Command { get; set; } = command;
    public string Username { get; set; } = username;
}