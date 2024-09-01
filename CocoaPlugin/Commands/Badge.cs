using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using CocoaPlugin.API;
using CocoaPlugin.API.Managers;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Badge : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = "사용법: badge <add|remove|list|save|load>";
            return false;
        }

        bool result;
        switch (arguments.At(0).ToLower())
        {
            case "add":
                if (arguments.Count < 4)
                {
                    response = "사용법: badge add <id> <color> [name]";
                    return false;
                }

                result = BadgeManager.AddBadge(arguments.At(1), new API.Managers.Badge()
                {
                    Color = arguments.At(2),
                    Name = string.Join(" ", arguments.Skip(3))
                });

                BadgeManager.SaveBadges();

                response = result ? "칭호를 추가했습니다." : "칭호를 추가하지 못했습니다.";
                return result;
            case "remove":
                if (arguments.Count < 2)
                {
                    response = "사용법: badge remove <id>";
                    return false;
                }

                result = BadgeManager.RemoveBadge(arguments.At(1));

                BadgeManager.SaveBadges();

                response = result ? "칭호를 제거했습니다." : "칭호를 제거하지 못했습니다.";
                return result;
            case "list":
                var sb = new StringBuilder("\n<b>칭호 목록</b>\n\n");

                foreach (var (id, badge) in BadgeManager.BadgeCache)
                    sb.AppendLine($"- <b>{id}</b> | <color={API.Managers.BadgeColor.Colors[badge.Color]}>{badge.Name}</color>");

                response = sb.ToString();
                return true;
            case "save":
                BadgeManager.SaveBadges();
                response = "칭호를 저장했습니다.";
                return true;
            case "load":
                BadgeManager.LoadBadges();
                response = "칭호를 불러왔습니다.";
                return true;
            default:
                response = "사용법: badge <add|remove|list|save|load>";
                return false;
        }
    }

    public string Command { get; } = "badge";
    public string[] Aliases { get; } = ["b"];
    public string Description { get; } = "유저의 칭호를 관리합니다.";
}

[CommandHandler(typeof(ClientCommandHandler))]
public class BadgeText : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "플레이어만 사용할 수 있습니다.";
            return false;
        }

        var cooldown = BadgeCooldownManager.GetBadgeCooldown(player.UserId);

        if (cooldown == null)
        {
            response = "권한이 없습니다.";
            return false;
        }

        if (cooldown.TextCooldown < 0)
        {
            response = "권한이 없습니다.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "사용법: badgetext <텍스트>";
            return false;
        }

        if (Utility.UnixTimeNow < cooldown.TextCooldown)
        {
            response = $"\n텍스트를 바꿀 수 있을 때까지 {TimeSpanToString(TimeSpan.FromSeconds(cooldown.TextCooldown - Utility.UnixTimeNow))} 남았습니다.";
            return false;
        }

        BadgeManager.SetText(player.UserId, string.Join(" ", arguments));
        BadgeManager.RefreshBadge(player.UserId, BadgeManager.GetBadge(player.UserId));

        BadgeCooldownManager.SetTextCooldown(player.UserId, Utility.UnixTimeNow + 60 * 60 * 24 * 7);

        response = "텍스트를 바꿨습니다.";
        return true;
    }

    private string TimeSpanToString(TimeSpan timeSpan)
    {
        var sb = new StringBuilder();

        if (timeSpan.Days > 0)
            sb.Append($"{timeSpan.Days}일 ");

        if (timeSpan.Hours > 0)
            sb.Append($"{timeSpan.Hours}시간 ");

        if (timeSpan.Minutes > 0)
            sb.Append($"{timeSpan.Minutes}분 ");

        if (timeSpan.Seconds > 0)
            sb.Append($"{timeSpan.Seconds}초 ");

        sb.Remove(sb.Length - 1, 1);

        return sb.ToString();
    }

    public string Command { get; } = "badgetext";
    public string[] Aliases { get; } = ["bt"];
    public string Description { get; } = "칭호의 텍스트를 바꿉니다.";
}

[CommandHandler(typeof(ClientCommandHandler))]
public class BadgeColor : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "플레이어만 사용할 수 있습니다.";
            return false;
        }

        var cooldown = BadgeCooldownManager.GetBadgeCooldown(player.UserId);

        if (cooldown == null)
        {
            response = "권한이 없습니다.";
            return false;
        }

        if (cooldown.ColorCooldown < 0)
        {
            response = "권한이 없습니다.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "사용법: badgecolor <색깔>";
            return false;
        }

        if (Utility.UnixTimeNow < cooldown.ColorCooldown)
        {
            response = $"\n색을 바꿀 수 있을 때까지 {TimeSpanToString(TimeSpan.FromSeconds(cooldown.TextCooldown - Utility.UnixTimeNow))} 남았습니다.";
            return false;
        }

        if (!API.Managers.BadgeColor.IsValidColor(string.Join(" ", arguments)))
        {
            response = API.Managers.BadgeColor.Colors.Aggregate("\n색이 유효하지 않습니다. 가능한 색은:", (current, color) => current + $"\n- <color={color.Value}>{color.Key}</color>");
            return false;
        }

        BadgeManager.SetColor(player.UserId, string.Join(" ", arguments));
        BadgeManager.RefreshBadge(player.UserId, BadgeManager.GetBadge(player.UserId));

        BadgeCooldownManager.SetColorCooldown(player.UserId, Utility.UnixTimeNow + 60 * 60 * 24 * 3);

        response = "색을 바꿨습니다.";
        return true;
    }

    private string TimeSpanToString(TimeSpan timeSpan)
    {
        var sb = new StringBuilder();

        if (timeSpan.Days > 0)
            sb.Append($"{timeSpan.Days}일 ");

        if (timeSpan.Hours > 0)
            sb.Append($"{timeSpan.Hours}시간 ");

        if (timeSpan.Minutes > 0)
            sb.Append($"{timeSpan.Minutes}분 ");

        if (timeSpan.Seconds > 0)
            sb.Append($"{timeSpan.Seconds}초 ");

        sb.Remove(sb.Length - 1, 1);

        return sb.ToString();
    }

    public string Command { get; } = "badgecolor";
    public string[] Aliases { get; } = ["bc"];
    public string Description { get; } = "칭호의 색을 바꿉니다.";
}