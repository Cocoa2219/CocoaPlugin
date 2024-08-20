﻿namespace CocoaPlugin.API;

public enum LogType
{
    None,
    Verified,
    Died,
    Left,
    RespawningTeam,
    RoundStarted,
    RoundEnded,
    Started,
    Stopped,
    LocalReporting,
    ReportingCheater,
    Kicked,
    Banned,
    WarheadStarting,
    WarheadStopping,
    WarheadDetonated,
    LeftWhileReviving,
    HandcuffedKill,
    ReconnectLimit,
    WaitingReconnect,
    ReconnectFailed,
    ReconnectSuccess,
    // ServerInfo,
    Command,
    DoorTrolling
}