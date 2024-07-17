using System;
using System.Linq;
using CocoaPlugin.API;
using CommandSystem;
using Exiled.API.Features;
using HarmonyLib;
using PluginAPI.Events;
using RemoteAdmin;
using RemoteAdmin.Communication;
using RemoteAdmin.Interfaces;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(QueryProcessor), nameof(QueryProcessor.ProcessGameConsoleQuery))]
public class QueryProcessorPatch
{
    public static bool Prefix(QueryProcessor __instance, string query)
    {
        var arguments = query.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);
        if (!EventManager.ExecuteEvent(
                new PlayerGameConsoleCommandEvent(__instance._hub, arguments[0], arguments.Skip(1).ToArray())))
            return false;

        if (QueryProcessor.DotCommandHandler.TryGetCommand(arguments[0], out var command))
        {
            try
            {
                var success = command.Execute(arguments.Segment(1), __instance._sender, out var response);
                // NW Fuck you
                // if (command.SanitizeResponse)
                // {
                //     text = Misc.SanitizeRichText(text);
                // }
                if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandExecutedEvent(__instance._hub, arguments[0],
                        arguments.Skip(1).ToArray(), success, response)))
                    return false;

                __instance._hub.gameConsoleTransmission.SendToClient(response,
                    success
                        ? CocoaPlugin.Instance.Config.Commands.ExecuteSuccessColor
                        : CocoaPlugin.Instance.Config.Commands.ExecuteFailColor);
            }
            catch (Exception ex)
            {
                var errorFormat = CocoaPlugin.Instance.Config.Commands.ExecuteErrorText;
                var err = errorFormat + ex;
                if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandExecutedEvent(__instance._hub, arguments[0],
                        arguments.Skip(1).ToArray(), false, err)))
                    return false;

                __instance._hub.gameConsoleTransmission.SendToClient(err,
                    CocoaPlugin.Instance.Config.Commands.ExecuteFailColor);
            }

            return false;
        }

        if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandExecutedEvent(__instance._hub, arguments[0],
                arguments.Skip(1).ToArray(), false, CocoaPlugin.Instance.Config.Commands.CommandNotFound)))
            return false;

        __instance._hub.gameConsoleTransmission.SendToClient(CocoaPlugin.Instance.Config.Commands.CommandNotFound,
            CocoaPlugin.Instance.Config.Commands.ExecuteFailColor);
        return false;
    }
}

[HarmonyPatch(typeof(CommandProcessor), nameof(CommandProcessor.ProcessQuery))]
public class CommandProcessorPatch
{
    public static bool Prefix(string q, CommandSender sender, ref string __result)
    {
        if (q.StartsWith("$", StringComparison.Ordinal))
        {
            var array = q.Remove(0, 1).Split(' ');
            if (array.Length <= 1)
            {
                __result = null;
                return false;
            }

            if (!int.TryParse(array[0], out var key))
            {
                __result = null;
                return false;
            }

            if (CommunicationProcessor.ServerCommunication.TryGetValue(key, out var serverCommunication))
                serverCommunication.ReceiveData(sender, string.Join(" ", array.Skip(1)));

            __result = null;
            return false;
        }

        var array2 = q.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);
        if (!EventManager.ExecuteEvent(new RemoteAdminCommandEvent(sender, array2[0],
                array2.Skip(1).ToArray())))
        {
            __result = null;
            return false;
        }

        if (CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(array2[0], out var command))
            try
            {
                var flag = command.Execute(array2.Segment(1), sender, out var text);
                if (command.SanitizeResponse) text = Misc.SanitizeRichText(text);

                if (!EventManager.ExecuteEvent(new RemoteAdminCommandExecutedEvent(sender, array2[0],
                        array2.Skip(1).ToArray(), flag, text)))
                {
                    __result = null;
                    return false;
                }

                if (!string.IsNullOrEmpty(text))
                    sender.RaReply(array2[0].ToUpperInvariant() + "#" + text, flag, true, "");

                var player = Player.Get(sender);

                if (player != null)
                    NetworkManager.Send(new
                    {
                        Nickname = player.Nickname,
                        UserId = player.UserId,
                        Sent = q,
                        Result = text
                    }, MessageType.Command);

                __result = text;
                return false;
            }
            catch (Exception ex)
            {
                var text2 = "Command execution failed! Error: " + Misc.RemoveStacktraceZeroes(ex.ToString());
                if (!EventManager.ExecuteEvent(new RemoteAdminCommandExecutedEvent(sender, array2[0],
                        array2.Skip(1).ToArray(), false, text2)))
                {
                    __result = null;
                    return false;
                }

                sender.RaReply(text2, false, true, array2[0].ToUpperInvariant() + "#" + text2);
                __result = text2;
                return false;
            }

        if (!EventManager.ExecuteEvent(new RemoteAdminCommandExecutedEvent(sender, array2[0],
                array2.Skip(1).ToArray(), false, "Unknown command!")))
        {
            __result = null;
            return false;
        }

        sender.RaReply("SYSTEM#Unknown command!", false, true, string.Empty);
        __result = "Unknown command!";
        return false;
    }
}