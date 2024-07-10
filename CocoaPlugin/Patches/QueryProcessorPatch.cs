using System;
using System.Linq;
using CommandSystem;
using HarmonyLib;
using PluginAPI.Events;
using RemoteAdmin;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(QueryProcessor), nameof(QueryProcessor.ProcessGameConsoleQuery))]
public class QueryProcessorPatch
{
    public static bool Prefix(QueryProcessor __instance, string query)
    {
        var arguments = query.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);
        if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandEvent(__instance._hub, arguments[0], arguments.Skip(1).ToArray())))
        {
            return false;
        }

        if (QueryProcessor.DotCommandHandler.TryGetCommand(arguments[0], out var command))
        {
            try
            {
                var success = command.Execute(arguments.Segment(1), __instance._sender, out var response);
                // if (command.SanitizeResponse)
                // {
                //     text = Misc.SanitizeRichText(text);
                // }
                if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandExecutedEvent(__instance._hub, arguments[0], arguments.Skip(1).ToArray(), success, response)))
                {
                    return false;
                }

                __instance._hub.gameConsoleTransmission.SendToClient(response,
                    success
                        ? CocoaPlugin.Instance.Config.Commands.ExecuteSuccessColor
                        : CocoaPlugin.Instance.Config.Commands.ExecuteFailColor);
            }
            catch (Exception ex)
            {
                var errorFormat = CocoaPlugin.Instance.Config.Commands.ExecuteErrorText;
                var err = errorFormat + ex;
                if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandExecutedEvent(__instance._hub, arguments[0], arguments.Skip(1).ToArray(), false, err)))
                {
                    return false;
                }
                __instance._hub.gameConsoleTransmission.SendToClient(err, CocoaPlugin.Instance.Config.Commands.ExecuteFailColor);
            }

            return false;
        }
        if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandExecutedEvent(__instance._hub, arguments[0], arguments.Skip(1).ToArray(), false, CocoaPlugin.Instance.Config.Commands.CommandNotFound)))
        {
            return false;
        }

        __instance._hub.gameConsoleTransmission.SendToClient(CocoaPlugin.Instance.Config.Commands.CommandNotFound, CocoaPlugin.Instance.Config.Commands.ExecuteFailColor);
        return false;
    }
}