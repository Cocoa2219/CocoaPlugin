using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class SendPost // : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var text = string.Join(" ", arguments);

        if (string.IsNullOrWhiteSpace(text))
        {
            response = "You must provide a message to send.";
            return false;
        }

        var player = Player.Get(sender as CommandSender);

        var nickname = player?.Nickname ?? "Server";
        var userId = player?.UserId ?? "Server";

        API.NetworkManager.Send(new
        {
            Nickname = nickname,
            UserId = userId,
            Text = string.Join(" ", arguments)
        }, API.MessageType.Command);

        response = "Message sent.";
        return true;
    }

    public string Command { get; } = "sendpost";
    public string[] Aliases { get; } = { "sp" };
    public string Description { get; } = "Send a message to the server.";
}