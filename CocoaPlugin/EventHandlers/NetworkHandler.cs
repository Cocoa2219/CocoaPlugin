using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.API;
using CocoaPlugin.API.Managers;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using Exiled.Events.Handlers;
using MEC;

namespace CocoaPlugin.EventHandlers;

public class NetworkHandler(CocoaPlugin plugin)
{
    private CocoaPlugin Plugin { get; } = plugin;

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
    }

    private void OnVerified(VerifiedEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            Nickname = ev.Player.Nickname,
            CustomName = ev.Player.CustomName,
            UserId = ev.Player.UserId,
            IpAddress = ev.Player.IPAddress
        }, LogType.Verified);
    }

    private void OnDied(DiedEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            AttackerNickname = ev.Attacker?.Nickname,
            AttackerUserId = ev.Attacker?.UserId,
            AttackerRole = ev.Attacker?.Role.Type,
            PlayerNickname = ev.Player.Nickname,
            PlayerUserId = ev.Player.UserId,
            PlayerRole = ev.TargetOldRole,
            DamageType = ev.DamageHandler.Type
        }, LogType.Died);
    }

    private void OnLeft(LeftEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            Nickname = ev.Player.Nickname,
            CustomName = ev.Player.CustomName,
            UserId = ev.Player.UserId,
            IpAddress = ev.Player.IPAddress
        }, LogType.Left);
    }

    private void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            Team = ev.NextKnownTeam,
            PlayerCount = ev.Players.Count
        }, LogType.RespawningTeam);
    }

    private void OnRoundStarted()
    {
        NetworkManager.SendLog(new
        {
            PlayerCount = Exiled.API.Features.Player.List.Count
        }, LogType.RoundStarted);
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            LeadingTeam = ev.LeadingTeam,
        }, LogType.RoundEnded);
    }

    private void OnLocalReporting(LocalReportingEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            ReporterNickname = ev.Player.Nickname,
            ReporterUserId = ev.Player.UserId,
            ReporterRole = ev.Player.Role.Type,
            ReportedNickname = ev.Target.Nickname,
            ReportedUserId = ev.Target.UserId,
            ReportedRole = ev.Target.Role.Type,
            Reason = ev.Reason
        }, LogType.LocalReporting);
    }

    private void OnReportingCheater(ReportingCheaterEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            ReporterNickname = ev.Player.Nickname,
            ReporterUserId = ev.Player.UserId,
            ReporterRole = ev.Player.Role.Type,
            ReportedNickname = ev.Target.Nickname,
            ReportedUserId = ev.Target.UserId,
            ReportedRole = ev.Target.Role.Type,
            Reason = ev.Reason
        }, LogType.ReportingCheater);
    }

    private void OnKicked(KickedEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            Nickname = ev.Player.Nickname,
            CustomName = ev.Player.CustomName,
            UserId = ev.Player.UserId,
            IpAddress = ev.Player.IPAddress,
            Reason = ev.Reason
        }, LogType.Kicked);
    }

    private void OnBanned(BannedEventArgs ev)
    {
        NetworkManager.SendLog(new
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
        }, LogType.Banned);
    }

    private void OnWarheadStarting(StartingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        NetworkManager.SendLog(new
        {
            Nickname = ev.Player?.Nickname,
            UserId = ev.Player?.UserId,
            Role = ev.Player?.Role.Type,
            IsAuto = ev.IsAuto,
            Time = Exiled.API.Features.Warhead.RealDetonationTimer
        }, LogType.WarheadStarting);
    }

    private void OnWarheadStopping(StoppingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        NetworkManager.SendLog(new
        {
            Nickname = ev.Player?.Nickname,
            UserId = ev.Player?.UserId,
            Role = ev.Player?.Role.Type,
        }, LogType.WarheadStopping);
    }

    private void OnWarheadDetonated()
    {
        NetworkManager.SendLog(new
        {
            AliveCount = Exiled.API.Features.Player.List.Count(x => x.IsAlive),
        }, LogType.WarheadDetonated);
    }
}