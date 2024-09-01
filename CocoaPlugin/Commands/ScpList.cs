using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CocoaPlugin.API;
using CommandSystem;
using Exiled.API.Features;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class ScpList : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get((CommandSender)sender);

        if (player == null)
        {
            response = "플레이어만 이 명령어를 사용할 수 있습니다.";
            return false;
        }

        if (!player.IsScp)
        {
            response = "SCP만 이 명령어를 사용할 수 있습니다.";
            return false;
        }

        var scps = Player.List.Where(x => x.IsScp).OrderBy(x => x.Role.Type.ToString()).ToList();
        var sb = new System.Text.StringBuilder($"\n<color=white>현재 SCP 목록 ({scps.Count}개체 존재):</color>\n\n");

        foreach (var scp in scps)
        {
            sb.AppendLine(scp.HasCustomName ? $"<color=white><color={scp.GetRoleColor()}>{scp.GetRoleName()}</color> | {scp.DisplayNickname} ({scp.Nickname}) | {scp.UserId}</color>" : $"<color=white><color={scp.GetRoleColor()}>{scp.GetRoleName()}</color> | {scp.Nickname} | {scp.UserId}</color>");
        }

        response = sb.ToString();
        return true;
    }

    public string Command { get; } = "scplist";
    public string[] Aliases { get; } = { "ㄴ체ㅣㅑㄴㅅ" };
    public string Description { get; } = "현재 SCP 목록을 가져옵니다.";
}