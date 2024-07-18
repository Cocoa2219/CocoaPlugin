using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using GameCore;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class NoScp : ICommand
{
    public static HashSet<Player> NoScpPlayers { get; } = [];

    public NoScp()
    {
        Exiled.Events.Handlers.Player.Left += OnLeft;
        Server.RestartingRound += OnRestarting;
    }

    private void OnLeft(LeftEventArgs ev)
    {
        if (NoScpPlayers.Contains(ev.Player))
        {
            NoScpPlayers.Remove(ev.Player);
        }
    }

    private void OnRestarting()
    {
        NoScpPlayers.Clear();

        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Server.RestartingRound -= OnRestarting;
    }

    private static int GetScpCount(int playerCount)
    {
        var text = ConfigFile.ServerConfig.GetString("team_respawn_queue", "4014314031441404134041434414");

        var scpCount = 0;

        for (var i = 0; i < playerCount; i++)
        {
            if (text[i % text.Length] == '0')
            {
                scpCount++;
            }
        }

        return scpCount;
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

        var leftScps = Player.List.Count - NoScpPlayers.Count;

        if (leftScps <= GetScpCount(Player.List.Count))
        {
            response = "스폰할 SCP가 부족합니다.";
            return false;
        }

        if (NoScpPlayers.Add(player))
        {
            response = "이번 라운드에 확정적으로 SCP가 되지 않습니다.";
            return true;
        }

        NoScpPlayers.Remove(player);
        response = "취소했습니다.";
        return true;
    }

    public string Command { get; } = "noscp";
    public string[] Aliases { get; } = ["ns"];
    public string Description { get; } = "이번 라운드에 확정적으로 SCP가 되지 않습니다.";
}