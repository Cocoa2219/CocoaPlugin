using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CocoaPlugin.API;
using CommandSystem;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Badge : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = "사용법: badge <add|remove|list>";
            return false;
        }

        string id;
        bool result;
        switch (arguments.At(0).ToLower())
        {
            case "add":
                if (arguments.Count < 4)
                {
                    response = "사용법: badge add <Id> <Color> <Name>";
                    return false;
                }

                id = arguments.At(1);
                var color = arguments.At(2);
                var name = string.Join(" ", arguments.Skip(3));

                result = BadgeManager.AddBadge(id, new API.Badge
                {
                    Color = color,
                    Name = name
                });

                response = result ? "칭호를 정상적으로 추가했습니다." : "칭호를 추가하지 못했습니다.";
                return result;
            case "remove":
                if (arguments.Count < 2)
                {
                    response = "사용법: badge remove <Id>";
                    return false;
                }

                id = arguments.At(1);

                result = BadgeManager.RemoveBadge(id);

                response = result ? "칭호를 정상적으로 제거했습니다." : "칭호를 제거하지 못했습니다.";
                return result;
            case "load":
                BadgeManager.LoadBadges();

                response = "칭호를 불러왔습니다.";
                return true;
            case "save":
                BadgeManager.SaveBadges();

                response = "칭호를 저장했습니다.";
                return true;
            case "list":
                BadgeManager.LoadBadges();

                var sb = new System.Text.StringBuilder();

                sb.AppendLine("\n<b>칭호 목록:</b>\n");

                foreach (var badge in BadgeManager.BadgeCache)
                {
                    sb.AppendLine(
                        $"- {badge.Key} | <color={BadgeColor.Colors[badge.Value.Color]}>{badge.Value.Name}</color>");
                }

                if (BadgeManager.BadgeCache.Count == 0)
                    sb.AppendLine("칭호가 없습니다.");

                response = sb.ToString();
                return true;
            default:
                response = "사용법: badge <add|remove|list>";
                return false;
        }
    }

    public string Command { get; } = "badge";
    public string[] Aliases { get; } = { "b" };
    public string Description { get; } = "유저의 칭호를 관리합니다.";
    public bool SanitizeResponse { get; } = false;
}