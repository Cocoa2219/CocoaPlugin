using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Teleport : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = "사용법: teleport <플레이어 이름> <x> <y> <z>";
            return false;
        }

        if (!Player.TryGet(arguments.At(0), out var target))
        {
            target = Player.Get(sender as CommandSender);
        }

        if (arguments.Count < 4)
        {
            response = "사용법: teleport <플레이어 이름> <x> <y> <z>";
            return false;
        }

        if (!float.TryParse(arguments.At(1), out var x) || !float.TryParse(arguments.At(2), out var y) || !float.TryParse(arguments.At(3), out var z))
        {
            response = "좌표는 숫자여야 합니다.";
            return false;
        }

        target.Position = new UnityEngine.Vector3(x, y, z);
        response = "플레이어를 이동했습니다.";
        return true;
    }

    public string Command { get; } = "teleport";
    public string[] Aliases { get; } = { "tp" };
    public string Description { get; } = "플레이어를 특정 위치로 이동시킵니다.";
}