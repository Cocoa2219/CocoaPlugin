using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using CocoaPlugin.API;
using CocoaPlugin.API.Managers;
using CommandSystem;

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

                response = result ? "칭호를 추가했습니다." : "칭호를 추가하지 못했습니다.";
                return result;
            case "remove":
                if (arguments.Count < 2)
                {
                    response = "사용법: badge remove <id>";
                    return false;
                }

                result = BadgeManager.RemoveBadge(arguments.At(1));

                response = result ? "칭호를 제거했습니다." : "칭호를 제거하지 못했습니다.";
                return result;
            case "list":
                var sb = new StringBuilder("\n<b>칭호 목록</b>\n\n");

                foreach (var (id, badge) in BadgeManager.BadgeCache)
                    sb.AppendLine($"- <b>{id}</b> | <color={BadgeColor.Colors[badge.Color]}>{badge.Name}</color>");

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
    public bool SanitizeResponse { get; } = false;
}