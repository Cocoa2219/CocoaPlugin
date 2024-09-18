using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CommandSystem;
using Exiled.API.Features;
using Vector3 = UnityEngine.Vector3;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Scale : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var target = Player.Get(arguments.At(0));

        if (target == null)
        {
            response = "플레이어를 찾을 수 없습니다.";
            return false;
        }

        if (!float.TryParse(arguments.At(1), out var scale))
        {
            response = "크기는 숫자여야 합니다.";
            return false;
        }

        target.Scale = Vector3.one * scale;

        response = "크기를 변경했습니다.";
        return true;
    }

    public string Command { get; } = "scale";
    public string[] Aliases { get; } = { "s" };
    public string Description { get; } = "플레이어의 크기를 변경합니다.";
}