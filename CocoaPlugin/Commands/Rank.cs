using System;
using System.Diagnostics.CodeAnalysis;
using CocoaPlugin.API.Managers;
using CocoaPlugin.API.Ranks;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Rank : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 2)
        {
            response = "사용법: rank <exp/level/save/load/showui>";
            return false;
        }

        var arg1 = arguments.At(0);

        switch (arg1)
        {
            case "exp":
                if (arguments.Count < 3)
                {
                    response = "사용법: rank exp <add/remove/set> <플레이어> <값> [타입]";
                    return false;
                }

                var arg2 = arguments.At(1);

                string arg3;
                int amount;
                ExperienceType type;

                switch (arg2)
                {
                    case "add":
                        arg3 = ParsePlayer(arguments.At(2));

                        if (arg3 == null)
                        {
                            response = "플레이어를 찾을 수 없습니다.";
                            return false;
                        }

                        if (!int.TryParse(arguments.At(3), out amount))
                        {
                            response = "값은 숫자여야 합니다.";
                            return false;
                        }

                        if (arguments.Count >= 4)
                        {
                            Enum.TryParse(arguments.At(4), true, out type);
                        }
                        else
                        {
                            type = ExperienceType.AdminCommand;
                        }

                        RankManager.GetExperienceHandler(type)?.Grant(arg3);

                        response = $"{arg3}에게 {amount} 경험치를 추가했습니다.";
                        return true;
                    case "remove":
                        arg3 = ParsePlayer(arguments.At(2));

                        if (arg3 == null)
                        {
                            response = "플레이어를 찾을 수 없습니다.";
                            return false;
                        }

                        if (!int.TryParse(arguments.At(3), out amount))
                        {
                            response = "값은 숫자여야 합니다.";
                            return false;
                        }

                        if (arguments.Count >= 4 && Enum.TryParse(arguments.At(4), true, out type))
                        {
                            RankManager.GetRank(arg3)?.Remove(amount, type);
                        }
                        else
                        {
                            type = ExperienceType.AdminCommand;

                            RankManager.GetRank(arg3)?.Remove(amount, type);
                        }

                        response = $"{arg3}에게 {amount} 경험치를 제거했습니다.";
                        return true;
                    case "set":
                        arg3 = ParsePlayer(arguments.At(2));

                        if (arg3 == null)
                        {
                            response = "플레이어를 찾을 수 없습니다.";
                            return false;
                        }

                        if (!int.TryParse(arguments.At(3), out amount))
                        {
                            response = "값은 숫자여야 합니다.";
                            return false;
                        }

                        if (arguments.Count >= 4 && Enum.TryParse(arguments.At(4), true, out type))
                        {
                            RankManager.GetRank(arg3)?.Set(amount, type);
                        }
                        else
                        {
                            type = ExperienceType.AdminCommand;

                            RankManager.GetRank(arg3)?.Set(amount, type);
                        }

                        response = $"{arg3}의 경험치를 {amount}로 설정했습니다.";
                        return true;
                    default:
                        response = "사용법: rank exp <add/remove/set> <플레이어> <값> [타입]";
                        return false;
                }

            case "level":
                response = "Not implemented yet.";
                return false;
            case "save":
                RankManager.SaveRanks();
                response = "랭크 데이터를 저장했습니다.";
                return true;
            case "load":
                RankManager.LoadRanks();
                response = "랭크 데이터를 불러왔습니다.";
                return true;
            case "showui":
                var player = Player.Get(sender as CommandSender);

                if (player == null)
                {
                    response = "플레이어를 찾을 수 없습니다.";
                    return false;
                }

                if (!int.TryParse(arguments.At(1), out var prev))
                {
                    response = "값은 숫자여야 합니다.";
                    return false;
                }

                if (!int.TryParse(arguments.At(2), out var next))
                {
                    response = "값은 숫자여야 합니다.";
                    return false;
                }

                var time = float.TryParse(arguments.At(3), out var t) ? t : 0.1f;

                RankManager.UpgradeBroadcast(prev, next, player, time);
                response = "UI를 표시했습니다.";
                return true;
            default:
                response = "사용법: rank <exp/level/save/load/showui>";
                return false;
        }
    }

    private string ParsePlayer(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var player = Player.Get(text);

        return player == null ? text : player.UserId;
    }

    public string Command { get; } = "rank";
    public string[] Aliases { get; } = { "r" };
    public string Description { get; } = "플레이어의 랭크를 설정합니다.";
}