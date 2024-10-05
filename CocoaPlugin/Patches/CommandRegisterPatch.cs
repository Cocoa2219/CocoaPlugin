using System;
using CommandSystem;
using HarmonyLib;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(CommandHandler), nameof(CommandHandler.RegisterCommand))]
public class CommandRegisterPatch
{
    public static bool Prefix(CommandHandler __instance, ICommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Command))
        {
            throw new ArgumentException("Command text of " + command.GetType().Name + " cannot be null or whitespace!");
        }

        __instance.Commands[command.Command] = command;

        if (command.Aliases != null)
        {
            foreach (var text in command.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    __instance.CommandAliases[text] = command.Command;
                }
            }
        }

        return false;
    }
}