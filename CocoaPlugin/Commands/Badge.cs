using System;
using System.Diagnostics.CodeAnalysis;
using CocoaPlugin.API;
using CommandSystem;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Badge : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = "사용법: badge <add|remove|list>";
            return false;
        }

        switch (arguments.At(0).ToLower())
        {
            case "add":


                // Add badge
                response = "칭호를 추가했습니다.";
                return true;
            case "remove":


                // Remove badge
                response = "칭호를 제거했습니다.";
                return true;
            case "list":
                var sb

                response = "칭호 목록";
                return true;
            default:
                response = "사용법: badge <add|remove|list>";
                return false;
        }
    }

    public string Command { get; } = "badge";
    public string[] Aliases { get; } = { "b" };
    public string Description { get; } = "유저의 칭호를 관리합니다.";
    public bool SanitizeResponse { get; } = false;
}