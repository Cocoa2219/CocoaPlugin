using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using CocoaPlugin.API;
using CocoaPlugin.API.Managers;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class PenaltyClient : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "플레이어가 존재하지 않습니다.";
            return false;
        }

        var color = player.GetPenaltyCount() == 0 ? "#ffffff" : "#d44b42";

        var sb = new StringBuilder($"\n<color={color}>현재 <b>{player.GetPenaltyCount()}</b>점의 벌점이 있습니다.</color>\n\n");

        foreach (var penalty in player.GetPenalties())
            sb.AppendLine($"사유 | <b>{penalty.Reason}</b>\n유효일 | <color=#d44b42><b>{Utility.UnixTimeToDateTime(penalty.Issued)}</b></color>부터 <color=#d44b42><b>{Utility.UnixTimeToDateTime(penalty.Until)}</b></color>까지 유효합니다.\n처리 관리자 | <color=#d44b42>{penalty.IssuerNickname} ({penalty.Issuer})</color>\n");

        if (player.GetPenaltyCount() == 0)
            sb.AppendLine("- 깨끗하네요, 잘 하셨어요!");

        sb.Remove(sb.Length - 1, 1);

        response = sb.ToString();
        return true;
    }

    public string Command { get; } = "penalty";
    public string[] Aliases { get; } = ["pen", "p"];
    public string Description { get; } = "자신의 벌점을 확인합니다.";
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class PenaltyRemoteAdmin : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "플레이어가 존재하지 않습니다.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "사용법: penalty <add|remove|list|save|load>";
            return false;
        }

        bool result;
        switch (arguments.At(0).ToLower())
        {
            case "add":
                if (arguments.Count < 4)
                {
                    response = "사용법: penalty add <id> <until> [reason]";
                    return false;
                }

                long time;

                try
                {
                    time = Misc.RelativeTimeToSeconds(arguments.At(2), 60);
                }
                catch (Exception)
                {
                    response = "유효일이 올바르지 않습니다.";
                    return false;
                }

                result = PenaltyManager.AddPenalty(arguments.At(1), new Penalty
                {
                    Until = Utility.UnixTimeNow + time,
                    Issued = Utility.UnixTimeNow,
                    Reason = string.Join(" ", arguments.Skip(3)),
                    Issuer = player.UserId,
                    IssuerNickname = player.Nickname
                });

                response = result ? "벌점을 추가했습니다." : "벌점을 추가하지 못했습니다.";
                return result;
            case "remove":
                if (arguments.Count < 3)
                {
                    response = "사용법: badge remove <id> <index>";
                    return false;
                }

                if (!int.TryParse(arguments.At(2), out var index))
                {
                    response = "색인이 올바르지 않습니다.";
                    return false;
                }

                result = PenaltyManager.RemovePenalty(arguments.At(1), index - 1);

                response = result ? "벌점을 제거했습니다." : "벌점을 제거하지 못했습니다.";
                return result;
            case "list":
                if (arguments.Count < 2)
                {
                    response = "사용법: penalty list <id>";
                    return false;
                }

                var penalties = PenaltyManager.GetPenalties(arguments.At(1));

                var sb = new StringBuilder($"\n<b>{arguments.At(1)}의 벌점 목록 ({PenaltyManager.GetPenaltyCount(arguments.At(1))}개의 벌점)</b>\n\n");

                for (var i = 0; i < penalties.Count; i++)
                {
                    var penalty = penalties[i];
                    sb.AppendLine($"- <b>{i + 1}</b>번 벌점\n  사유 | <b>{penalty.Reason}</b>\n  유효일 | <color=#ffffff><b>{Utility.UnixTimeToDateTime(penalty.Issued)}</b></color>부터 <color=#ffffff><b>{Utility.UnixTimeToDateTime(penalty.Until)}</b></color>까지 유효합니다.\n  처리 관리자 | <color=#ffffff>{penalty.Issuer}</color> | <color=#ffffff>{penalty.IssuerNickname}</color>");

                    if (i != penalties.Count - 1)
                        sb.AppendLine();
                }

                if (penalties.Count == 0)
                    sb.AppendLine("- 벌점이 없습니다.");

                response = sb.ToString();
                return true;
            case "save":
                PenaltyManager.SavePenalties();
                response = "벌점을 저장했습니다.";
                return true;
            case "load":
                PenaltyManager.LoadPenalties();
                response = "벌점을 불러왔습니다.";
                return true;
            default:
                response = "사용법: penalty <add|remove|list|save|load>";
                return false;
        }
    }

    public string Command { get; } = "penalty";
    public string[] Aliases { get; } = ["pen", "p"];
    public string Description { get; } = "벌점을 관리합니다.";
}