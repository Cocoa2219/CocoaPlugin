using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SetMaxHealth : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(arguments.At(0));

        if (player == null)
        {
            response = "플레이어를 찾을 수 없습니다.";
            return false;
        }

        if (arguments.Count != 2 || !int.TryParse(arguments.At(1), out var maxHealth))
        {
            response = "사용법: setmaxhealth <플레이어> <최대 체력>";
            return false;
        }

        player.MaxHealth = maxHealth;
        response = $"{player.Nickname}의 최대 체력을 {maxHealth}로 설정했습니다.";
        return true;
    }

    public string Command { get; } = "setmaxhealth";
    public string[] Aliases { get; } = { "mhp" };
    public string Description { get; } = "플레이어의 최대 체력을 설정합니다.";
}