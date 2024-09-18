using System;
using System.Linq;
using CocoaPlugin.Commands;
using CommandSystem;
using CommandSystem.Commands.Shared;
using Exiled.Permissions;
using HarmonyLib;
using NorthwoodLib.Pools;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(HelpCommand), nameof(HelpCommand.GetCommandList))]
public class GetCommandListPatch
{
    public static bool Prefix(HelpCommand __instance, ICommandHandler handler, string header, ref string __result)
    {
        if (handler is not ClientCommandHandler clientCommandHandler) return true;

        var stringBuilder = __instance._helpBuilder;

        stringBuilder.Clear();
        stringBuilder.AppendLine();
        stringBuilder.Append(header);

        foreach (var command in clientCommandHandler.AllCommands)
        {
            if (command is not IHelpableCommand) continue;

            stringBuilder.AppendLine();

            stringBuilder.Append($" - .{command.Command}");

            stringBuilder.Append(command.Aliases != null && command.Aliases.Any()
                ? $" ({string.Join(", ", command.Aliases.Select(x => $".{x}"))})"
                : string.Empty);

            stringBuilder.Append($"\n <b>|</b> {command.Description}");
        }

        __result = stringBuilder.ToString();

        return false;
    }
}

[HarmonyPatch(typeof(HelpCommand), nameof(HelpCommand.Execute))]
public class HelpCommandPatch
{
    public static bool Prefix(HelpCommand __instance, ArraySegment<string> arguments, ICommandSender sender,
        ref string response, ref bool __result)
    {
        if (__instance._commandHandler is not ClientCommandHandler clientCommandHandler)
        {
            return true;
        }

        if (arguments.Count == 0)
        {
            response = __instance.GetCommandList(clientCommandHandler, "명령어 목록:");
            __result = true;
            return false;
        }

        if (clientCommandHandler.TryGetCommand(arguments.At(0), out var command))
        {
            if (command is not IHelpableCommand helpableCommand)
            {
                response = $"{arguments.At(0)} 명령어에 대한 도움말을 찾을 수 없습니다.";
                __result = false;
                return false;
            }

            var sb = StringBuilderPool.Shared.Rent();

            sb.AppendLine();
            sb.Append($" - .{command.Command}");
            sb.Append(command.Aliases != null && command.Aliases.Any()
                ? $" ({string.Join(", ", command.Aliases.Select(x => $".{x}"))})"
                : string.Empty);

            sb.AppendLine();

            sb.Append(" <b>|</b> ");
            sb.Append(command.Description);

            response = sb.ToString();
            StringBuilderPool.Shared.Return(sb);
            __result = true;
        }
        else
        {
            response = $"{arguments.At(0)} 명령어를 찾을 수 없습니다.";
            __result = false;
        }

        return false;
    }
}