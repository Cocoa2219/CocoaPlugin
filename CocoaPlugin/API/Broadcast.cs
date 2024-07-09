using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Exiled.API.Features;
using PlayerRoles;
using UnityEngine;

namespace CocoaPlugin.API;

public class Broadcast
{
    private string _message;

    public Broadcast(string message, ushort duration, byte priority = 0, bool isEnabled = true)
    {
        _message = message;
        Duration = duration;
        Priority = priority;
        IsEnabled = isEnabled;
    }

    public Broadcast()
    {
        IsEnabled = true;
    }

    public string Message
    {
        get => IsEnabled ? _message : string.Empty;
        set => _message = value;
    }

    public ushort Duration { get; set; }
    public byte Priority { get; set; }
    public bool IsEnabled { get; set; }

    public string Format(Player player)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%nickname%", player.Nickname);
        sb.Replace("%nicknameParticle%", IsKorean(player.Nickname[^1]) ? Divide(player.Nickname[^1]).jongsung == ' ' ? "이" : "가" : "이(가)");
        sb.Replace("%customName%", player.CustomName);
        sb.Replace("%userId%", player.UserId);
        sb.Replace("%roleColor%", player.GetRoleColor());
        sb.Replace("%roleName%", player.GetRoleName());

        return sb.ToString();
    }

    public string Format(Player player, int amount, string text)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%nickname%", player.Nickname);
        sb.Replace("%nicknameParticle%", IsKorean(player.Nickname[^1]) ? Divide(player.Nickname[^1]).jongsung == ' ' ? "이" : "가" : "이(가)");
        sb.Replace("%customName%", player.CustomName);
        sb.Replace("%userId%", player.UserId);
        sb.Replace("%roleColor%", player.GetRoleColor());
        sb.Replace("%roleName%", player.GetRoleName());
        sb.Replace("%amount%", amount.ToString());
        sb.Replace("%text%", text);

        return sb.ToString();
    }

    public string Format(Player attacker, Player target, RoleTypeId? attackerRole, RoleTypeId targetRole)
    {
        var sb = new StringBuilder(Message);

        if (attacker != null)
        {
            sb.Replace("%attackerNickname%", attacker.Nickname);
            sb.Replace("%attackerCustomName%", attacker.CustomName);
            sb.Replace("%attackerUserId%", attacker.UserId);
            sb.Replace("%attackerRoleColor%", attackerRole?.GetRoleColor());
            sb.Replace("%attackerRoleName%", attackerRole?.GetRoleName());
            sb.Replace("%attackerNicknameParticle%",
                IsKorean(attacker.Nickname[^1]) ? Divide(attacker.Nickname[^1]).jongsung == ' ' ? "이" : "가" : "이(가)");
            sb.Replace("%attackerRoleNameParticle%", Divide(attackerRole.GetValueOrDefault().GetRoleName()[^1]).jongsung == ' ' ? "로" : "으로");
        }
        else
        {
            sb.Replace("%attackerNickname%", "알 수 없음");
            sb.Replace("%attackerCustomName%", "알 수 없음");
            sb.Replace("%attackerUserId%", "알 수 없음");
            sb.Replace("%attackerRoleColor%", "#737373");
            sb.Replace("%attackerRoleName%", "알 수 없음");
            sb.Replace("%attackerNicknameParticle%", "이");
            sb.Replace("%attackerRoleNameParticle%", "으로");
        }

        sb.Replace("%targetNickname%", target.Nickname);
        sb.Replace("%targetCustomName%", target.CustomName);
        sb.Replace("%targetUserId%", target.UserId);
        sb.Replace("%targetRoleColor%", targetRole.GetRoleColor());
        sb.Replace("%targetRoleName%", targetRole.GetRoleName());
        sb.Replace("%targetNicknameParticle%", IsKorean(target.Nickname[^1]) ? Divide(target.Nickname[^1]).jongsung == ' ' ? "이" : "가" : "이(가)");
        sb.Replace("%targetRoleNameParticle%", Divide(targetRole.GetRoleName()[^1]).jongsung == ' ' ? "가" : "이");

        return sb.ToString();
    }

    public string Format(int unitNumber, string unitName, int scpsLeft)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%unitNumber%", unitNumber.ToString());
        sb.Replace("%unitName%", unitName);
        sb.Replace("%scpsLeft%", scpsLeft.ToString());

        return sb.ToString();
    }

    public string Format(List<Player> scpList)
    {
        var sb = new StringBuilder(Message);

        var scpCount = scpList.Count;
        var scpString = string.Join(" / ", scpList.ConvertAll(x => x.GetRoleName()));

        sb.Replace("%scpCount%", scpCount.ToString());
        sb.Replace("%scpList%", scpString);

        return sb.ToString();
    }

    public string Format(Team team)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%teamColor%", Cocoa.Instance.Config.Translations.TeamColors[team]);
        sb.Replace("%teamName%", Cocoa.Instance.Config.Translations.TeamTranslations[team]);

        return sb.ToString();
    }

    public string Format(int amount)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%amount%", amount.ToString());

        return sb.ToString();
    }

    public string Format(float amount)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%amount%", amount.ToString(CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    public string Format(Player sender, string message)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%nickname%", sender.Nickname);
        sb.Replace("%customName%", sender.CustomName);
        sb.Replace("%roleColor%", sender.GetRoleColor());
        sb.Replace("%roleName%", sender.GetRoleName());
        sb.Replace("%message%", message);

        return sb.ToString();
    }

    public string Format(LeftUser leftUser)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%nickname%", leftUser.Nickname);
        sb.Replace("%userId%", leftUser.UserId);
        sb.Replace("%roleColor%", leftUser.Role.GetRoleColor());
        sb.Replace("%roleName%", leftUser.Role.GetRoleName());
        sb.Replace("%nicknameParticle%", IsKorean(leftUser.Nickname[^1]) ? Divide(leftUser.Nickname[^1]).jongsung == ' ' ? "이" : "가" : "이(가)");
        sb.Replace("%time%", SecondsToMinutes(Cocoa.Instance.Config.Reconnects.ReconnectTime));

        return sb.ToString();
    }

    public static string SecondsToMinutes(int seconds)
    {
        var minutes = seconds / 60;
        var remainingSeconds = seconds % 60;

        return $"{minutes}분 {remainingSeconds}초";
    }

    private const string Chosung = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
    private const string Jungsung = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
    private const string Jongsung = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

    private const ushort UnicodeHangeulBase = 0xAC00;
    private const ushort UnicodeHangeulLast = 0xD79F;

    private static bool IsKorean(char ch)
    {
        return (0xAC00 <= ch && ch <= 0xD7A3) || (0x3131 <= ch && ch <= 0x318E);
    }

    private static (char chosung, char jungsung, char? jongsung) Divide(char c)
    {
        var check = Convert.ToUInt16(c);

        if (check is > UnicodeHangeulLast or < UnicodeHangeulBase) return (' ', ' ', ' ');

        var Code = check - UnicodeHangeulBase;

        var JongsungCode = Code % 28;
        Code = (Code - JongsungCode) / 28;

        var JungsungCode = Code % 21;
        Code = (Code - JungsungCode) / 21;

        var ChosungCode = Code;

        var Chosung = Broadcast.Chosung[ChosungCode];
        var Jungsung = Broadcast.Jungsung[JungsungCode];
        var Jongsung = Broadcast.Jongsung[JongsungCode];

        return (Chosung, Jungsung, Jongsung);
    }
}