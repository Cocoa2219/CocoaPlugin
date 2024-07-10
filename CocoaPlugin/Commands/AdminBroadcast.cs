using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using MultiBroadcast.API;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class AdminBroadcastClient : ICommand, IHiddenCommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (!player.CheckPermission(PlayerPermissions.Broadcasting))
        {
            response = "이 명령어를 사용할 권한이 없습니다.";
            return false;
        }

        var message = CocoaPlugin.Instance.Config.Broadcasts.AdministrativeBroadcastMessage.Format(player, string.Join(" ", arguments));

        foreach (var receiver in Player.List)
        {
            receiver.AddBroadcast(CocoaPlugin.Instance.Config.Broadcasts.AdministrativeBroadcastMessage.Duration, message, CocoaPlugin.Instance.Config.Broadcasts.AdministrativeBroadcastMessage.Priority);
        }

        response = "관리자 브로드캐스트를 전송했습니다.";
        return true;
    }

    public string Command { get; } = "broadcast";
    public string[] Aliases { get; } = ["bc"];
    public string Description { get; } = "관리자 브로드캐스트를 사용합니다.";
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class AdminBroadcastRemoteAdmin : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (!player.CheckPermission(PlayerPermissions.Broadcasting))
        {
            response = "이 명령어를 사용할 권한이 없습니다.";
            return false;
        }

        var message = CocoaPlugin.Instance.Config.Broadcasts.AdministrativeBroadcastMessage.Format(player, string.Join(" ", arguments));

        foreach (var receiver in Player.List)
        {
            receiver.AddBroadcast(CocoaPlugin.Instance.Config.Broadcasts.AdministrativeBroadcastMessage.Duration, message, CocoaPlugin.Instance.Config.Broadcasts.AdministrativeBroadcastMessage.Priority);
        }

        response = "관리자 브로드캐스트를 전송했습니다.";
        return true;
    }

    public string Command { get; } = "adminbroadcast";
    public string[] Aliases { get; } = ["abc"];
    public string Description { get; } = "관리자 브로드캐스트를 사용합니다.";
}