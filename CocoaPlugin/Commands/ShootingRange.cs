using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ShootingRange : ICommand
{
    private API.Beta.ShootingRange _shootingRange { get; } = CocoaPlugin.Instance.ShootingRange;

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = "사용법: shootingrange <add/remove>";
            return false;
        }

        switch (arguments.At(0))
        {
            case "add":
                if (arguments.Count < 2)
                {
                    var player = Player.Get(sender as CommandSender);

                    if (player == null)
                    {
                        response = "플레이어가 존재하지 않습니다.";
                        return false;
                    }

                    _shootingRange.AddPlayer(player);
                    response = "플레이어를 훈련장에 추가했습니다.";
                    return true;
                }

                if (!Player.TryGet(arguments.At(1), out var target))
                {
                    response = "플레이어를 찾을 수 없습니다.";
                    return false;
                }

                _shootingRange.AddPlayer(target);

                response = "플레이어를 훈련장에 추가했습니다.";
                return true;
            case "remove":
                if (arguments.Count < 2)
                {
                    var player = Player.Get(sender as CommandSender);

                    if (player == null)
                    {
                        response = "플레이어가 존재하지 않습니다.";
                        return false;
                    }

                    _shootingRange.RemovePlayer(player);
                    response = "플레이어를 훈련장에서 제거했습니다.";
                    return true;
                }

                if (!Player.TryGet(arguments.At(1), out target))
                {
                    response = "플레이어를 찾을 수 없습니다.";
                    return false;
                }

                _shootingRange.RemovePlayer(target);
                response = "플레이어를 훈련장에서 제거했습니다.";
                return true;
            default:
                response = "사용법: shootingrange <add/remove>";
                return false;
        }
    }
    
    public string Command { get; } = "shootingrange";
    public string[] Aliases { get; } = [];
    public string Description { get; } = "훈련장 부모 명령어입니다.";
}