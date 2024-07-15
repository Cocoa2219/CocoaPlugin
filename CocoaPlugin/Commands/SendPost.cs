using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public class SendPost : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var text = string.Join(" ", arguments);

        if (string.IsNullOrWhiteSpace(text))
        {
            response = "You must provide a message to send.";
            return false;
        }

        API.NetworkManager.Send(text, API.MessageType.Command);

        response = "Message sent.";
        return true;
    }

    public string Command { get; } = "sendpost";
    public string[] Aliases { get; } = { "sp" };
    public string Description { get; } = "Send a message to the server.";
}