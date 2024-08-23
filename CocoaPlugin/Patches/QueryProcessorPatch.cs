using System;
using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API;
using CocoaPlugin.API.Managers;
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

        var closestCommands = FindClosestStrings(arguments[0], QueryProcessor.DotCommandHandler.AllCommands.Select(x => x.Command).ToList());
        for (var i = 0; i < closestCommands.Count; i++)
        {
            closestCommands[i] = closestCommands[i].Insert(0, ".");
        }

        string responseText;

        if (closestCommands.Count > 0)
        {
            responseText = CocoaPlugin.Instance.Config.Commands.CommandNotFound.Replace("%similars%", $"혹시 {string.Join(", ", closestCommands)} 명령어를 찾으셨나요?").Replace("\\n", "\n");

            if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandExecutedEvent(__instance._hub, arguments[0],
                    arguments.Skip(1).ToArray(), false, responseText)))
                return false;

            __instance._hub.gameConsoleTransmission.SendToClient(responseText,
                CocoaPlugin.Instance.Config.Commands.ExecuteFailColor);
            return false;
        }

        responseText = CocoaPlugin.Instance.Config.Commands.CommandNotFound.Replace("%similars%", "");

        if (!EventManager.ExecuteEvent(new PlayerGameConsoleCommandExecutedEvent(__instance._hub, arguments[0],
                arguments.Skip(1).ToArray(), false, responseText)))
            return false;

        __instance._hub.gameConsoleTransmission.SendToClient(responseText,
            CocoaPlugin.Instance.Config.Commands.ExecuteFailColor);
        return false;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var costs = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++)
            costs[i, 0] = i;
        for (var j = 0; j <= b.Length; j++)
            costs[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = b[j - 1] == a[i - 1] ? 0 : 1;
            costs[i, j] = Math.Min(
                Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1),
                costs[i - 1, j - 1] + cost);
        }

        return costs[a.Length, b.Length];
    }

    public static List<string> FindClosestStrings(string target, List<string> list)
    {
        var closestStrings = new List<string>();
        var minDistance = int.MaxValue;

        foreach (var s in list)
        {
            var distance = LevenshteinDistance(target, s);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestStrings.Clear();
                closestStrings.Add(s);
            }
            else if (distance == minDistance)
            {
                closestStrings.Add(s);
            }
        }

        return closestStrings;
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

                if (!EventManager.ExecuteEvent(new RemoteAdminCommandExecutedEvent(sender, array2[0],
                        array2.Skip(1).ToArray(), flag, text)))
                {
                    __result = null;
                    return false;
                }

                if (!string.IsNullOrEmpty(text))
                    sender.RaReply(array2[0].ToUpperInvariant() + "#" + text, flag, true, "");

                NetworkManager.SendLog(new
                {
                    Nickname = sender.Nickname,
                    UserId = sender.SenderId,
                    Sent = q,
                    Result = text
                }, LogType.Command);

                LogManager.WriteLog($"{sender.Nickname} ({sender.SenderId}) RA 명령어 실행: {q}\n결과: {text}");

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