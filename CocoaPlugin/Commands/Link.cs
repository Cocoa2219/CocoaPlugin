using System;
using System.Diagnostics.CodeAnalysis;
using CocoaPlugin.API;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class Link : ICommand
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
            response = "사용법: link <코드>";
            return false;
        }

        if (player.IsLinked())
        {
            response = "이미 연결된 계정입니다.";
            return false;
        }

        var code = arguments.At(0);

        if (string.IsNullOrEmpty(code))
        {
            response = "사용법: link <코드>";
            return false;
        }
        ConnectionManager.LoadConnections();
        if (!ConnectionManager.HasCode(code))
        {
            response = "코드가 올바르지 않습니다.";
            return false;
        }

        var connection = ConnectionManager.GetConnection(code);

        if (connection == null)
        {
            response = "코드가 올바르지 않습니다.";
            return false;
        }

        player.Link(connection.DiscordId, connection.DiscordNickname);

        UserManager.SaveUsers();

        response = $"<b>{connection.DiscordNickname}</b> 계정이 연결되었습니다.";
        return true;
    }

    public string Command { get; } = "link";
    public string[] Aliases { get; } = ["l"];
    public string Description { get; } = "디스코드와 SL 계정을 연결하는 데 사용되는 명령어입니다.";
    public bool SanitizeResponse { get; } = false;
}