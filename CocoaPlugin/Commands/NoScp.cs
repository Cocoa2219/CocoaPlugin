using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CocoaPlugin.API;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using GameCore;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class NoScp : ICommand, IHelpableCommand
{
    public static HashSet<Player> NoScpPlayers { get; } = [];

    public static void OnLeft(LeftEventArgs ev)
    {
        if (NoScpPlayers.Contains(ev.Player))
        {
            NoScpPlayers.Remove(ev.Player);
        }
    }

    public static void OnRestarting()
    {
        NoScpPlayers.Clear();
    }

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "플레이어만 사용할 수 있습니다.";
            return false;
        }

        if (!Round.IsLobby)
        {
            response = "시작 전에만 사용할 수 있습니다.";
            return false;
        }

        if (NoScpPlayers.Contains(player))
        {
            NoScpPlayers.Remove(player);
            response = "취소했습니다.";
            return false;
        }

        var leftScps = Player.List.Count - NoScpPlayers.Count;

        if (leftScps <= Utility.GetTeamCount(Player.List.Count)[Team.SCPs])
        {
            response = "스폰할 SCP가 부족합니다. 더 이상 사용할 수 없습니다.";
            return false;
        }

        NoScpPlayers.Add(player);

        response = "이번 라운드에 확정적으로 SCP가 되지 않습니다.";
        return true;
    }

    public string Command { get; } = "noscp";
    public string[] Aliases { get; } = ["ns"];
    public string Description { get; } = "이번 라운드에 확정적으로 SCP가 되지 않습니다.";
}