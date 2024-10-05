using System.Linq;
using CocoaPlugin.API;
using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using Exiled.Events.Handlers;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace CocoaPlugin.Handlers;

public class NetworkHandler
{
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

        LogManager.WriteLog($"{ev.Player.Nickname} ({ev.Player.UserId} | {ev.Player.IPAddress}) 연결됨.");
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

        LogManager.WriteLog($"{ev.Player.Nickname} ({ev.Player.UserId} | {ev.Player.Role.Type}) 사망 - 공격자: {ev.Attacker?.Nickname} ({ev.Attacker?.UserId} | {ev.Attacker?.Role.Type}) - 타입: {ev.DamageHandler.Type}");
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

        LogManager.WriteLog($"{ev.Player.Nickname} ({ev.Player.UserId} | {ev.Player.IPAddress}) 연결 해제됨.");
    }

    private void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            Team = ev.NextKnownTeam,
            PlayerCount = ev.Players.Count
        }, LogType.RespawningTeam);

        LogManager.WriteLog($"팀 리스폰: {ev.NextKnownTeam} - 플레이어 수: {ev.Players.Count}");
    }

    private void OnRoundStarted()
    {
        NetworkManager.SendLog(new
        {
            PlayerCount = Exiled.API.Features.Player.List.Count
        }, LogType.RoundStarted);

        LogManager.WriteLog("라운드 시작됨.");
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        NetworkManager.SendLog(new
        {
            LeadingTeam = ev.LeadingTeam,
        }, LogType.RoundEnded);

        LogManager.WriteLog($"라운드 종료됨 - 승리팀: {ev.LeadingTeam}");
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

        LogManager.WriteLog($"{ev.Player.Nickname} ({ev.Player.UserId} | {ev.Player.Role.Type})이 {ev.Target.Nickname} ({ev.Target.UserId} | {ev.Target.Role.Type})을 신고함 - 사유: {ev.Reason}");
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

        LogManager.WriteLog($"{ev.Player.Nickname} ({ev.Player.UserId} | {ev.Player.Role.Type})이 {ev.Target.Nickname} ({ev.Target.UserId} | {ev.Target.Role.Type})을 제3자 프로그램으로 신고함 - 사유: {ev.Reason}");
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

        LogManager.WriteLog($"{ev.Player.Nickname} ({ev.Player.UserId} | {ev.Player.IPAddress}) 추방됨 - 사유: {ev.Reason}");
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

        var id = ev.Target != null ? ev.Target.UserId : ev.Details.Id;
        var ip = ev.Target != null ? ev.Target.IPAddress : ev.Details.Id;

        LogManager.WriteLog($"{ev.Target?.Nickname} ({id} | {ip}) {ev.Type}됨 - 발급자: {ev.Player?.Nickname} ({ev.Player?.UserId}) - 만료: {ev.Details.Expires} - 기간: {ev.Details.Expires - ev.Details.IssuanceTime} - 사유: {ev.Details.Reason} - 강제?: {ev.IsForced}");
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

        LogManager.WriteLog($"{ev.Player?.Nickname} ({ev.Player?.UserId} | {ev.Player?.Role.Type})이 핵탄두를 시작함 - 자동 핵: {ev.IsAuto} - 시간: {Exiled.API.Features.Warhead.RealDetonationTimer}초 남음");
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

        LogManager.WriteLog($"{ev.Player?.Nickname} ({ev.Player?.UserId} | {ev.Player?.Role.Type})이 핵탄두를 중지함");
    }

    private void OnWarheadDetonated()
    {
        NetworkManager.SendLog(new
        {
            AliveCount = Exiled.API.Features.Player.List.Count(x => x.IsAlive),
        }, LogType.WarheadDetonated);

        LogManager.WriteLog($"핵탄두 폭발됨 - 생존자 수: {Exiled.API.Features.Player.List.Count(x => x.IsAlive)}");
    }
}