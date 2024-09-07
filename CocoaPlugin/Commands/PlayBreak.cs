using System;
using System.Diagnostics.CodeAnalysis;
using CocoaPlugin.API;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class PlayBreak : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 3)
        {
            var pos = Player.Get(sender as CommandSender)?.Position;
            if (pos == null)
            {
                response = "플레이어가 존재하지 않습니다.";
                return false;
            }

            Utility.PlayAnti207BreakSound(pos.Value);
            response = "SCP-207? 깨짐 소리를 재생했습니다.";
            return true;
        }

        if (!float.TryParse(arguments.At(1), out var x) || !float.TryParse(arguments.At(2), out var y) || !float.TryParse(arguments.At(3), out var z))
        {
            response = "좌표는 숫자여야 합니다.";
            return false;
        }

        Utility.PlayAnti207BreakSound(new UnityEngine.Vector3(x, y, z));
        response = "SCP-207? 깨짐 소리를 재생했습니다.";
        return true;
    }

    public string Command { get; } = "playbreak";
    public string[] Aliases { get; } = { "pb" };
    public string Description { get; } = "SCP-207? 깨짐 소리를 재생합니다.";
}