using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;
using GameCore;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ReloadPastebin : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        foreach (var player in Player.List)
        {
            var ccm = player.ReferenceHub.characterClassManager;

            ccm.NetworkPastebin = ConfigFile.ServerConfig.GetString("serverinfo_pastebin_id", "");
        }

        response = "Reloaded the server's pastebin.";
        return true;
    }

    public string Command { get; } = "reloadpastebin";
    public string[] Aliases { get; } = { "rpb" };
    public string Description { get; } = "Reloads the server's pastebin.";
}