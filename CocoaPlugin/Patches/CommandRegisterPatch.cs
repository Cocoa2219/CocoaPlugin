using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CommandSystem;
using Exiled.API.Features.Pools;
using HarmonyLib;
using static HarmonyLib.AccessTools;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(CommandHandler), nameof(CommandHandler.RegisterCommand))]
public class CommandRegisterPatch
{
    // public static bool Prefix(CommandHandler __instance, ICommand command)
    // {
    //     if (string.IsNullOrWhiteSpace(command.Command))
    //     {
    //         throw new ArgumentException("Command text of " + command.GetType().Name + " cannot be null or whitespace!");
    //     }

    //     ------------------------------ Changed Code ------------------------------
    //     __instance.Commands[command.Command] = command;
    //     ------------------------------ Changed Code ------------------------------

    //     if (command.Aliases != null)
    //     {
    //         foreach (var text in command.Aliases)
    //         {
    //             if (!string.IsNullOrWhiteSpace(text))
    //             {

    //                 ------------------------------ Changed Code ------------------------------
    //                 __instance.CommandAliases[text] = command.Command;
    //                 ------------------------------ Changed Code ------------------------------

    //             }
    //         }
    //     }
    //
    //     return false;
    // }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        var index = newInstructions.FindIndex(instruction => instruction.Calls(Method(typeof(Dictionary<string, ICommand>), nameof(Dictionary<string, ICommand>.Add))));

        newInstructions.RemoveRange(index, 1);

        newInstructions.InsertRange(index, new[]
        {
            new CodeInstruction(OpCodes.Callvirt, Method(typeof(Dictionary<string, ICommand>), "set_Item")),
        });

        index = newInstructions.FindLastIndex(instruction => instruction.Calls(Method(typeof(Dictionary<string, string>), nameof(Dictionary<string, string>.Add))));

        newInstructions.RemoveRange(index, 1);

        newInstructions.InsertRange(index, new[]
        {
            new CodeInstruction(OpCodes.Callvirt, Method(typeof(Dictionary<string, string>), "set_Item")),
        });

        foreach (var t in newInstructions)
        {
            yield return t;
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }
}