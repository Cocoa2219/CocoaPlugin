using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class BanCommand : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var issuer = Player.Get(sender as CommandSender) ?? Server.Host;

        if (arguments.Count < 3)
        {
            response = "사용법: banplayer <플레이어 이름/아이디> <기간> [사유]";
            return false;
        }

        var target = Player.Get(arguments.At(0));
        long duration;

        try
        {
            duration = Misc.RelativeTimeToSeconds(arguments.At(1), 60);
        }
        catch (Exception)
        {
            response = "기간을 올바르게 입력해주세요.";
            return false;
        }

        if (target == null)
        {
            var flag = Misc.ValidateIpOrHostname(arguments.At(0), out var ipaddressType, false, false);
            if (!flag && !arguments.At(0).Contains("@"))
            {
                response = "Ip 주소 또는 플레이어 ID를 올바르게 입력해주세요.";
                return false;
            }

            BanHandler.IssueBan(new BanDetails
            {
                OriginalName = arguments.At(0),
                Id = arguments.At(0),
                IssuanceTime = TimeBehaviour.CurrentTimestamp(),
                Expires = TimeBehaviour.GetBanExpirationTime((uint) duration),
                Reason = string.Join(" ", arguments.Skip(2)),
                Issuer = sender.LogName
            }, flag ? BanHandler.BanType.IP : BanHandler.BanType.UserId, false);

            response = "플레이어를 차단했습니다.";
            return true;
        }

        var reason = string.Join(" ", arguments.Skip(2));

        target.Ban((int)duration, reason, issuer);
        response = $"{target.Nickname}님을 차단했습니다.";
        return true;
    }

    public string Command { get; } = "banplayer";
    public string[] Aliases { get; } = ["bp"];
    public string Description { get; } = "서버에서 플레이어를 차단합니다.";
}