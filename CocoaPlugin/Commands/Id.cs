using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using RemoteAdmin;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class Id : ICommand, IHelpableCommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (sender is not PlayerCommandSender s)
        {
            response = "플레이어만 사용할 수 있는 명령어입니다.";
            return false;
        }

        response = $"\n당신의 유저 ID는 {s.SenderId} 입니다.";
        return true;
    }

    public string Command { get; } = "id";
    public string[] Aliases { get; } = { "i" };
    public string Description { get; } = "자신의 유저 ID를 확인합니다.";
}