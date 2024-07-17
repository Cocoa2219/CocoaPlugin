using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using Exiled.Events.Handlers;
using MEC;

namespace CocoaPlugin.EventHandlers;

public class NetworkHandler(CocoaPlugin plugin)
{
    private CocoaPlugin Plugin { get; } = plugin;

    // private CoroutineHandle ServerInfo { get; set; }

    internal void SubscribeEvents()
    {
        Player.Verified += OnVerified;
        Player.Died += OnDied;
        Player.Left += OnLeft;
        Server.ReportingCheater += OnReportingCheater;
        Server.LocalReporting += OnLocalReporting;
        Player.Kicked += OnKicked;
        Player.Banned += OnBanned;
        Warhead.Starting += OnWarheadStarting;
        Warhead.Stopping += OnWarheadStopping;
        Warhead.Detonated += OnWarheadDetonated;
        Server.RoundStarted += OnRoundStarted;
        Server.RoundEnded += OnRoundEnded;
        Server.RespawningTeam += OnRespawningTeam;

        // ServerInfo = Timing.RunCoroutine(ServerInfoCoroutine());
    }

    internal void UnsubscribeEvents()
    {
        Player.Verified -= OnVerified;
        Player.Died -= OnDied;
        Player.Left -= OnLeft;
        Server.ReportingCheater -= OnReportingCheater;
        Server.LocalReporting -= OnLocalReporting;
        Player.Kicked -= OnKicked;
        Player.Banned -= OnBanned;
        Warhead.Starting -= OnWarheadStarting;
        Warhead.Stopping -= OnWarheadStopping;
        Warhead.Detonated -= OnWarheadDetonated;
        Server.RoundStarted -= OnRoundStarted;
        Server.RoundEnded -= OnRoundEnded;
        Server.RespawningTeam -= OnRespawningTeam;

        // Timing.KillCoroutines(ServerInfo);
    }

    // private IEnumerator<float> ServerInfoCoroutine()
    // {
    //     while (true)
    //     {
    //         NetworkManager.Send(new
    //         {
    //             PlayerCount = Exiled.API.Features.Player.List.Count,
    //             MaxPlayerCount = Exiled.API.Features.Server.MaxPlayerCount,
    //         }, MessageType.ServerInfo);
    //
    //         yield return Timing.WaitForSeconds(Plugin.Config.Network.ServerInfoInterval);
    //     }
    // }

    private void OnVerified(VerifiedEventArgs ev)
    {
        NetworkManager.Send(new
        {
            Nickname = ev.Player.Nickname,
            CustomName = ev.Player.CustomName,
            UserId = ev.Player.UserId,
            IpAddress = ev.Player.IPAddress
        }, MessageType.Verified);
    }

    private void OnDied(DiedEventArgs ev)
    {
        NetworkManager.Send(new
        {
            AttackerNickname = ev.Attacker?.Nickname,
            AttackerUserId = ev.Attacker?.UserId,
            AttackerRole = ev.Attacker?.Role.Type,
            PlayerNickname = ev.Player.Nickname,
            PlayerUserId = ev.Player.UserId,
            PlayerRole = ev.TargetOldRole,
            DamageType = ev.DamageHandler.Type
        }, MessageType.Died);
    }

    private void OnLeft(LeftEventArgs ev)
    {
        NetworkManager.Send(new
        {
            Nickname = ev.Player.Nickname,
            CustomName = ev.Player.CustomName,
            UserId = ev.Player.UserId,
            IpAddress = ev.Player.IPAddress
        }, MessageType.Left);
    }

    private void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        NetworkManager.Send(new
        {
            Team = ev.NextKnownTeam,
            PlayerCount = ev.Players.Count
        }, MessageType.RespawningTeam);
    }

    private void OnRoundStarted()
    {
        NetworkManager.Send(new
        {
            PlayerCount = Exiled.API.Features.Player.List.Count
        }, MessageType.RoundStarted);
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        NetworkManager.Send(new
        {
            LeadingTeam = ev.LeadingTeam,
        }, MessageType.RoundEnded);
    }

    private void OnLocalReporting(LocalReportingEventArgs ev)
    {
        NetworkManager.Send(new
        {
            ReporterNickname = ev.Player.Nickname,
            ReporterUserId = ev.Player.UserId,
            ReporterRole = ev.Player.Role.Type,
            ReportedNickname = ev.Target.Nickname,
            ReportedUserId = ev.Target.UserId,
            ReportedRole = ev.Target.Role.Type,
            Reason = ev.Reason
        }, MessageType.LocalReporting);
    }

    private void OnReportingCheater(ReportingCheaterEventArgs ev)
    {
        NetworkManager.Send(new
        {
            ReporterNickname = ev.Player.Nickname,
            ReporterUserId = ev.Player.UserId,
            ReporterRole = ev.Player.Role.Type,
            ReportedNickname = ev.Target.Nickname,
            ReportedUserId = ev.Target.UserId,
            ReportedRole = ev.Target.Role.Type,
            Reason = ev.Reason
        }, MessageType.ReportingCheater);
    }

    private void OnKicked(KickedEventArgs ev)
    {
        NetworkManager.Send(new
        {
            Nickname = ev.Player.Nickname,
            CustomName = ev.Player.CustomName,
            UserId = ev.Player.UserId,
            IpAddress = ev.Player.IPAddress,
            Reason = ev.Reason
        }, MessageType.Kicked);
    }

    private void OnBanned(BannedEventArgs ev)
    {
        NetworkManager.Send(new
        {
            Nickname = ev.Target?.Nickname,
            CustomName = ev.Target?.CustomName,
            UserId = ev.Target != null ? ev.Target.UserId : ev.Details.Id,
            IpAddress = ev.Target != null ? ev.Target.IPAddress : ev.Details.Id,
            IssuerNickname = ev.Player?.Nickname,
            IssuerUserId = ev.Player?.UserId,
            Until = ev.Details.Expires,
            Duration = ev.Details.Expires - ev.Details.IssuanceTime,
            Reason = ev.Details.Reason,
            Type = ev.Type,
            Forced = ev.IsForced
        }, MessageType.Banned);
    }

    private void OnWarheadStarting(StartingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        NetworkManager.Send(new
        {
            Nickname = ev.Player?.Nickname,
            UserId = ev.Player?.UserId,
            Role = ev.Player?.Role.Type,
            IsAuto = ev.IsAuto,
            Time = Exiled.API.Features.Warhead.RealDetonationTimer
        }, MessageType.WarheadStarting);
    }

    private void OnWarheadStopping(StoppingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        NetworkManager.Send(new
        {
            Nickname = ev.Player?.Nickname,
            UserId = ev.Player?.UserId,
            Role = ev.Player?.Role.Type,
        }, MessageType.WarheadStopping);
    }

    private void OnWarheadDetonated()
    {
        NetworkManager.Send(new
        {
            AliveCount = Exiled.API.Features.Player.List.Count(x => x.IsAlive),
        }, MessageType.WarheadDetonated);
    }
}