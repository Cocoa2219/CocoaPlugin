using System;
using System.Diagnostics.CodeAnalysis;
using CocoaPlugin.API;
using CommandSystem;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class NetworkManagerCommand: ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = "사용법: networkmanager <true/false>";
            return false;
        }

        if (!bool.TryParse(arguments.At(0), out var value))
        {
            response = "true 또는 false를 입력해주세요.";
            return false;
        }

        response = $"네트워크 관리자를 {(value ? "활성화" : "비활성화")}했습니다.";
        NetworkManager.CanSend = value;

        return true;
    }

    public string Command { get; } = "networkmanager";
    public string[] Aliases { get; } = ["nm"];
    public string Description { get; } = "네트워크 관리자 명령어입니다.";
}