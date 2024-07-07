using System;
using System.Collections.Generic;
using System.Text;
using Exiled.API.Features;
using UnityEngine;

namespace CocoaPlugin.API;

public class Broadcast
{
    public Broadcast(string message, ushort duration)
    {
        Message = message;
        Duration = duration;
    }

    public Broadcast()
    {

    }

    public string Message { get; set; }
    public ushort Duration { get; set; }

    public string Format(Player player)
    {
        var sb = new StringBuilder(Message);

        sb.Replace("%nickname%", player.Nickname);
        sb.Replace("%customName%", player.CustomName);
        sb.Replace("%userId%", player.UserId);
        sb.Replace("%roleColor%", player.GetRoleColor());
        sb.Replace("%roleName%", player.GetRoleName());

        return sb.ToString();
    }

    public string Format(Player attacker, Player target)
    {
        var sb = new StringBuilder(Message);

        if (attacker != null)
        {
            sb.Replace("%attackerNickname%", attacker.Nickname);
            sb.Replace("%attackerCustomName%", attacker.CustomName);
            sb.Replace("%attackerUserId%", attacker.UserId);
            sb.Replace("%attackerRoleColor%", attacker.GetRoleColor());
            sb.Replace("%attackerRoleName%", attacker.GetRoleName());
            sb.Replace("%attackerNicknameParticle%", Divide(attacker.Nickname[^1]).jongsung == ' ' ? "가" : "이");
        }
        else
        {
            sb.Replace("%attackerNickname%", "알 수 없음");
            sb.Replace("%attackerCustomName%", "알 수 없음");
            sb.Replace("%attackerUserId%", "알 수 없음");
            sb.Replace("%attackerRoleColor%", "#737373");
            sb.Replace("%attackerRoleName%", "알 수 없음");
            sb.Replace("%attackerNicknameParticle%", "이");
        }

        sb.Replace("%targetNickname%", target.Nickname);
        sb.Replace("%targetCustomName%", target.CustomName);
        sb.Replace("%targetUserId%", target.UserId);
        sb.Replace("%targetRoleColor%", target.GetRoleColor());
        sb.Replace("%targetRoleName%", target.GetRoleName());
        sb.Replace("%targetNicknameParticle%", Divide(target.Nickname[^1]).jongsung == ' ' ? "가" : "이");

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

    private const string chosung = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
    private const string jungsung = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
    private const string jongsung = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

    private const ushort UnicodeHangeulBase = 0xAC00;
    private const ushort UnicodeHangeulLast = 0xD79F;

    public static (char chosung, char jungsung, char? jongsung) Divide(char c)
    {
        var check = Convert.ToUInt16(c);

        if (check is > UnicodeHangeulLast or < UnicodeHangeulBase) return (' ', ' ', ' ');

        var Code = check - UnicodeHangeulBase;

        var JongsungCode = Code % 28;
        Code = (Code - JongsungCode) / 28;

        var JungsungCode = Code % 21;
        Code = (Code - JungsungCode) / 21;

        var ChosungCode = Code;

        var Chosung = chosung[ChosungCode];
        var Jungsung = jungsung[JungsungCode];
        var Jongsung = jongsung[JongsungCode];

        return (Chosung, Jungsung, Jongsung);
    }
}