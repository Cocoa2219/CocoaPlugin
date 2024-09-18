using System;
using CommandSystem;
using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ChangeAppearance : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "이 명령어는 콘솔에서 사용할 수 없습니다.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "외모를 변경할 수 있습니다. 사용법: ca <외모>";
            return false;
        }

        if (!Enum.TryParse(arguments.At(0), true, out RoleTypeId appearance))
        {
            response = "해당 외모가 존재하지 않습니다.";
            return false;
        }

        player.ChangeAppearance(appearance);

        response = "외모를 변경했습니다.";
        return true;
    }

    public string Command { get; } = "changeappearance";
    public string[] Aliases { get; } = { "ca" };
    public string Description { get; } = "자신의 외모를 변경합니다.";
}