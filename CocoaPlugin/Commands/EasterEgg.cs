using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Extensions;
using MultiBroadcast.Commands.Subcommands;
using RemoteAdmin;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public class EasterEgg : ICommand
{
    private List<string> _codes = new()
    {
        "20240710",
        "2024710"
    };

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (sender is not PlayerCommandSender commandSender)
        {
            response = "이 명령어는 콘솔에서 사용할 수 없습니다.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "이스터 에그를 찾긴 찾으셨는데... 코드를 아직 못 찾으셨네요. 힌트는 디스코드 서버에 있습니다.";
            return false;
        }

        var code = string.Join(" ", arguments);

        code = code.RemoveSpaces().Replace(".", "").Replace("/", "");

        if (!_codes.Contains(code))
        {
            response = "이스터 에그를 찾긴 찾으셨는데... 코드를 아직 못 찾으셨네요. 힌트는 디스코드 서버에 있습니다.";
            return false;
        }

        response = $"\n이스터 에그를 찾으셨군요!\nCocoa에게 DM으로 보내면... 좋은 일이 일어날지도...? :)\n{commandSender.SenderId}";
        return true;
    }

    public string Command { get; } = "easteregg";
    public string[] Aliases { get; } = [];
    public string Description { get; } = "이스터 에그를 찾아보세요!";
}