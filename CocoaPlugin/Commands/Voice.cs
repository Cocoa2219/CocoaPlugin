using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using CocoaPlugin.API;
using CommandSystem;
using Exiled.API.Features;
using NorthwoodLib.Pools;
using VoiceChat;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Voice : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = "사용법: voice <group/channel/list>";
            return false;
        }

        var arg1 = arguments.At(0);

        StringBuilder sb;
        switch (arg1)
        {
            case "group":
                switch (arguments.Count)
                {
                    case < 2:
                        response = "사용법: voice group <create/destroy/add/remove/list>";
                        return false;
                    default:
                        switch (arguments.At(1))
                        {
                            case "create":
                                switch (arguments.Count)
                                {
                                    case < 3:
                                        response = "사용법: voice group create <이름>";
                                        return false;
                                    case >= 3:
                                        var name = string.Join(" ", arguments.Skip(2));

                                        var group = VoiceGroup.Create(name);

                                        response = $"음성 그룹을 생성했습니다: {group.Name} ({group.Id})";
                                        return true;
                                }
                            case "destroy":
                                switch (arguments.Count)
                                {
                                    case < 3:
                                        response = "사용법: voice group destroy <이름 / ID>";
                                        return false;
                                    case >= 3:
                                        var name = string.Join(" ", arguments.Skip(2));

                                        VoiceGroup group;
                                        if (int.TryParse(name, out var id))
                                        {
                                            group = VoiceGroup.Get(id);

                                            if (group == null)
                                            {
                                                response = "그룹을 찾을 수 없습니다.";
                                                return false;
                                            }

                                            group.Destroy();

                                            response = $"음성 그룹을 제거했습니다: {group.Name} ({group.Id})";
                                            return true;
                                        }

                                        group = VoiceGroup.Get(name);

                                        if (group == null)
                                        {
                                            response = "그룹을 찾을 수 없습니다.";
                                            return false;
                                        }

                                        group.Destroy();

                                        response = $"음성 그룹을 제거했습니다: {group.Name} ({group.Id})";
                                        return true;
                                }
                            case "add":
                                switch (arguments.Count)
                                {
                                    case < 4:
                                        response = "사용법: voice group add <그룹 이름 / ID> <플레이어>";
                                        return false;
                                    case >= 4:
                                        var name = string.Join(" ", arguments.Skip(3));

                                        var players = Utility.ParsePlayers(name);

                                        if (players.Length == 0)
                                        {
                                            response = "플레이어를 찾을 수 없습니다.";
                                            return false;
                                        }

                                        var group = int.TryParse(arguments.At(2), out var id) ? VoiceGroup.Get(id) : VoiceGroup.Get(arguments.At(2));

                                        if (group == null)
                                        {
                                            response = "그룹을 찾을 수 없습니다.";
                                            return false;
                                        }

                                        foreach (var player in players)
                                        {
                                            group.AddMember(player);
                                        }

                                        response = $"{players.Length}명의 플레이어를 {group.Name} ({group.Id}) 그룹에 추가했습니다.";
                                        return true;
                                }
                            case "remove":
                                switch (arguments.Count)
                                {
                                    case < 3:
                                        response = "사용법: voice group remove <플레이어>";
                                        return false;
                                    case >= 3:
                                        var name = arguments.At(3);

                                        if (!Player.TryGet(name, out var player))
                                        {
                                            response = "플레이어를 찾을 수 없습니다.";
                                            return false;
                                        }

                                        var group = VoiceGroup.Get(player);
                                        group?.RemoveMember(player);

                                        response = $"{player.Nickname} ({player.UserId} | {player.Id}) 을(를) {group.Name} ({group.Id}) 그룹에서 제거했습니다.";
                                        return true;
                                }
                            case "list":
                                sb = StringBuilderPool.Shared.Rent();

                                sb.Append("\n<b>음성 그룹 목록:</b>\n");

                                foreach (var group in VoiceGroup.Groups)
                                {
                                    sb.Append($"- {group.Name} ({group.Id})");

                                    foreach (var member in group.Members)
                                    {
                                        sb.Append($"\n  - {member.Nickname} ({member.UserId} | {member.Id})");
                                    }

                                    sb.Append("\n");
                                }
                                response = sb.ToString();
                                StringBuilderPool.Shared.Return(sb);
                                return true;
                            default:
                                response = "사용법: voice group <create/destroy/add/remove/list>";
                                return false;
                        }
                }
            case "channel":
                switch (arguments.Count)
                {
                    case < 2:
                        response = "사용법: voice channel <플레이어> [채널]";
                        return false;
                    case > 3:
                    {
                        if (!Player.TryGet(arguments.At(2), out var player))
                        {
                            response = "플레이어를 찾을 수 없습니다.";
                            return false;
                        }

                        if (!Enum.TryParse(arguments.At(3), true, out VoiceChatChannel channel))
                        {
                            response = "채널을 찾을 수 없습니다.";
                            return false;
                        }

                        var oldChannel = player.VoiceChannel;

                        player.VoiceChannel = channel;
                        response = $"{player.Nickname} ({player.UserId} | {player.Id}) : {oldChannel} -> {channel}";
                        return true;
                    }
                    default:
                    {
                        if (!Player.TryGet(arguments.At(2), out var player))
                        {
                            response = "플레이어를 찾을 수 없습니다.";
                            return false;
                        }

                        response = $"{player.Nickname} ({player.UserId} | {player.Id}) : {player.VoiceChannel}";
                        return true;
                    }
                }
            case "list":
                sb = StringBuilderPool.Shared.Rent();

                sb.Append("\n<b>음성 채팅 목록:</b>\n");

                foreach (var player in Player.List)
                {
                    sb.Append($"- {player.Nickname} ({player.UserId} | {player.Id}) : {player.VoiceChannel}");

                    if (player.VoiceChatMuteFlags != VcMuteFlags.None)
                    {
                        var flags = (from Enum value in Enum.GetValues(typeof(VcMuteFlags)) where player.VoiceChatMuteFlags.HasFlag(value) select value.ToString()).ToList();

                        sb.Append($" | {string.Join(", ", flags)}");
                    }

                    sb.Append("\n");
                }

                response = sb.ToString();
                StringBuilderPool.Shared.Return(sb);
                return true;
            default:
                response = "사용법: voice <group/channel/list>";
                return false;
        }
    }

    public string Command { get; } = "voice";
    public string[] Aliases { get; } = ["vc"];
    public string Description { get; } = "플레이어 음성 채팅을 관리합니다.";
}