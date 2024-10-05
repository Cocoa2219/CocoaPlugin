using System;
using System.Collections.Generic;
using CocoaPlugin.API.Ranks;

namespace CocoaPlugin.API.Managers;

public static class ExperienceLogManager
{
    public const string ExperienceLogFileName = "ExperienceLog.txt";

    public static void WriteLog(ExperienceLog log)
    {
        FileManager.AppendFile(ExperienceLogFileName, $"{log.Id};{log.Experience};{log.Type.ToString()};{log.ActionType.ToString()}\n");
    }
}

public class ExperienceLog
{
    public string Id { get; init; }
    public int Experience { get; init; }
    public ExperienceType Type { get; init; }
    public ExperienceActionType ActionType { get; init; }
}

[Flags]
public enum ExperienceActionType
{
    Add,
    Remove,
    Set
}