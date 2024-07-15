namespace CocoaPlugin.API;

public class Message
{
    public object Content { get; set; }
    public MessageType Type { get; set; }
}

public enum MessageType
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
    Command
}