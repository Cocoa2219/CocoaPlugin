using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandSystem;
using Exiled.API.Features;
using Exiled.API.Features.Roles;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class CheckPropertiesAndFields : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "You are not a player.";
            return false;
        }

        if (player.Role is not ISubroutinedScpRole scpRole)
        {
            response = "You are not a SCP.";
            return false;
        }

        var subroutines = scpRole.SubroutineModule.AllSubroutines;

        var sb = new StringBuilder($"<b>Subroutines: ({subroutines.Length})</b>\n");

        foreach (var subroutine in subroutines)
        {
            sb.AppendLine($"- <b>{subroutine.GetType().Name}</b>");

            var properties = subroutine.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var fields = subroutine.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            sb.AppendLine($"  Properties: ({properties.Length})");

            foreach (var t in properties)
            {
                object value;

                try
                {
                    value = t.GetValue(subroutine);
                }
                catch (Exception)
                {
                    value = "Error while getting property.";
                }

                sb.AppendLine($"    - {t.Name} = {ObjectToString(value)}");
            }

            sb.AppendLine($"  Fields: ({fields.Length})");

            foreach (var t in fields)
            {
                object value;

                try
                {
                    value = t.GetValue(subroutine);
                }
                catch (Exception)
                {
                    value = "Error while getting field.";
                }

                sb.AppendLine($"    - {t.Name} = {ObjectToString(value)}");
            }
        }

        API.Managers.FileManager.WriteFile($"{player.Role.Type} Subroutines", sb.ToString());

        response = sb.ToString();
        return true;
    }

    private string ObjectToString(object obj)
    {
        if (obj == null)
            return "null";

        switch (obj)
        {
            case Array array:
                return $"[{string.Join(", ", array.Cast<object>())}] ({array.Length})";
            case List<object> list:
                return $"[{string.Join(", ", list)}] ({list.Count})";
            case Dictionary<object, object> dictionary:
                return $"[{dictionary.Select(x => $"{x.Key} = {x.Value}").Aggregate((x, y) => $"{x}, {y}")}] ({dictionary.Count})";
            case string str:
                return "\"" + str + "\"";
            default:
                return obj.ToString();
        }
    }

    public string Command { get; } = "cpaf";
    public string[] Aliases { get; } = { "checkpropertiesandfields" };
    public string Description { get; } = "Check properties and fields of your role, if you are a SCP.";
}