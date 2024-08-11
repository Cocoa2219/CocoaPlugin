using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommandSystem;
using Exiled.API.Features;
using UnityEngine;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class OffsetPosition : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        Vector3 offset;
        StringBuilder sb;
        Vector3 roomRot;
        Vector3 playerRot;
        if (arguments.Count == 0)
        {
            var player = Player.Get(sender as CommandSender);

            if (player == null)
            {
                response = "You must specify a player to offset the position of.";
                return false;
            }

            if (player.CurrentRoom == null)
            {
                response = "Player is not in a room.";
                return false;
            }

            offset = player.CurrentRoom.LocalPosition(player.Position);
            sb = new System.Text.StringBuilder();

            sb.AppendLine($"\n<b>{player.Nickname}</b>'s position offset in room:");

            sb.AppendLine($"Current Room: {player.CurrentRoom.name} ({player.CurrentRoom.Type}, {player.CurrentRoom.Zone})");

            sb.AppendLine($"WX: {player.Position.x}");
            sb.AppendLine($"WY: {player.Position.y}");
            sb.AppendLine($"WZ: {player.Position.z}\n");

            sb.AppendLine($"LocX: {offset.x}");
            sb.AppendLine($"LocY: {offset.y}");
            sb.AppendLine($"LocZ: {offset.z}\n");

            playerRot = player.CameraTransform.rotation.eulerAngles;
            sb.AppendLine($"PRX: {playerRot.x}");
            sb.AppendLine($"PRY: {playerRot.y}");
            sb.AppendLine($"PRZ: {playerRot.z}\n");

            sb.AppendLine($"RPXQ: {player.CameraTransform.rotation.x}");
            sb.AppendLine($"RPYQ: {player.CameraTransform.rotation.y}");
            sb.AppendLine($"RPZQ: {player.CameraTransform.rotation.z}");
            sb.AppendLine($"RPWQ: {player.CameraTransform.rotation.w}\n");

            sb.AppendLine($"Distance: {Vector3.Distance(player.CurrentRoom.Position, player.Position)}");

            response = sb.ToString();
            return true;
        }

        var target = Player.Get(arguments.At(0));

        if (target == null)
        {
            response = "Player not found.";
            return false;
        }

        if (target.CurrentRoom == null)
        {
            response = "Player is not in a room.";
            return false;
        }

        offset = target.CurrentRoom.LocalPosition(target.Position);
        sb = new System.Text.StringBuilder();

        sb.AppendLine($"\n<b>{target.Nickname}</b>'s position offset in room:");

        sb.AppendLine($"Current Room: {target.CurrentRoom.Name}");

        sb.AppendLine($"WX: {target.Position.x}");
        sb.AppendLine($"WY: {target.Position.y}");
        sb.AppendLine($"WZ: {target.Position.z}");

        sb.AppendLine($"LocX: {offset.x}");
        sb.AppendLine($"LocY: {offset.y}");
        sb.AppendLine($"LocZ: {offset.z}\n");

        playerRot = target.CameraTransform.rotation.eulerAngles;
        sb.AppendLine($"PRX: {playerRot.x}");
        sb.AppendLine($"PRY: {playerRot.y}");
        sb.AppendLine($"PRZ: {playerRot.z}\n");

        sb.AppendLine($"RPXQ: {target.CameraTransform.rotation.x}");
        sb.AppendLine($"RPYQ: {target.CameraTransform.rotation.y}");
        sb.AppendLine($"RPZQ: {target.CameraTransform.rotation.z}");
        sb.AppendLine($"RPWQ: {target.CameraTransform.rotation.w}\n");

        sb.AppendLine($"Distance: {Vector3.Distance(target.CurrentRoom.Position, target.Position)}");

        response = sb.ToString();
        return true;
    }

    public string Command { get; } = "offsetposition";
    public string[] Aliases { get; } = { "opr" };
    public string Description { get; } = "Offset the position in a room of a player.";
    public bool SanitizeResponse { get; } = false;
}