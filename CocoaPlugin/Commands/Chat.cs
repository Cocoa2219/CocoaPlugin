using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;
using PlayerRoles;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class Chat : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "플레이어만 사용할 수 있습니다.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "채팅할 내용을 입력해주세요.";
            return false;
        }

        var message = string.Join(" ", arguments);

        var receivers = Player.Get(Team.SCPs);

        foreach (var receiver in receivers)
        {
            // receiver.AddBroadcast(Cocoa.Instance.Config.Broadcasts.Chats.ScpChatMessage.Duration, Cocoa.Instance.Config.Broadcasts.Chats.ScpChatMessage.Format(player, message));
        }

        response = "채팅을 전송했습니다.";
        return true;
    }

    public string Command { get; } = "chat";
    public string[] Aliases { get; } = { "c", "ㅊ" };
    public string Description { get; } = "채팅을 사용합니다.";
}