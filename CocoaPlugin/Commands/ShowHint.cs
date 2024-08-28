using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ShowHint : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = "사용법: showhint <target> <message>";
            return false;
        }

        var player = Player.Get(arguments.At(0));

        if (player == null)
        {
            response = "플레이어를 찾을 수 없습니다.";
            return false;
        }

        var message = string.Join(" ", arguments[1..]);

        player.ShowHint(message, 5);

        response = $"힌트를 표시했습니다: {message}";
        return true;
    }

    public string Command { get; } = "showhint";
    public string[] Aliases { get; } = { "hint" };
    public string Description { get; } = "힌트를 표시합니다.";
}