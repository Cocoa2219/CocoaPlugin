using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.Loader;
using NorthwoodLib.Pools;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Plugins : ICommand, IHelpableCommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var plugins = Loader.Plugins;
        var dependencies = Loader.Dependencies;

        var sb = StringBuilderPool.Shared.Rent();

        sb.Append("\n");

        sb.AppendLine("Loaded plugins: (" + plugins.Count + ")");

        foreach (var plugin in plugins)
        {
            sb.AppendLine($"- {plugin.Assembly.GetName().Name} v{plugin.Assembly.GetName().Version}");
            sb.AppendLine($"  - Author: {plugin.Author}");
            sb.AppendLine($"  - Priority: {plugin.Priority}");
            sb.AppendLine($"  - Required EXILED version: {plugin.RequiredExiledVersion}");
        }

        sb.AppendLine("\nLoaded dependencies: (" + dependencies.Count + ")");

        foreach (var dependency in dependencies)
        {
            sb.AppendLine($"- {dependency.GetName().Name} v{dependency.GetName().Version}");
        }

        response = sb.ToString().TrimEnd('\n');
        return true;
    }

    public string Command { get; } = "plugins";
    public string[] Aliases { get; } = { "pl" };
    public string Description { get; } = "모든 플러그인의 상태를 확인합니다.";
}