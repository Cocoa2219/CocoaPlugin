using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;

namespace CocoaPlugin.API;

public static class AssistManager
{
    public static Dictionary<Player, List<Assist>> Assists { get; set; } = new();

    public static void OnHurt(HurtEventArgs ev)
    {
        if (ev.Attacker == null) return;

        if (!Assists.ContainsKey(ev.Player))
        {
            Assists[ev.Player] = [];
        }

        var assist = new Assist
        {
            Target = ev.Player,
            Damage = ev.Amount,
            Time = UnityEngine.Time.timeSinceLevelLoad
        };

        if (Assists.TryGetValue(ev.Attacker, out var assists))
        {
            assists.Add(assist);
        }
        else
        {
            Assists[ev.Attacker] = [assist];
        }
    }

    public static void OnDying(DyingEventArgs ev)
    {
        Assists.Remove(ev.Player);
    }

    public static void OnRestartingRound()
    {
        Assists.Clear();
    }

    public static List<Assist> GetAssists(Player player)
    {
        return Assists.TryGetValue(player, out var assists) ? assists : [];
    }

    public static bool HasAssists(Player target, Player player)
    {
        return Assists.TryGetValue(target, out var assists) && assists.Any(x => x.Target == player);
    }

    public static Assist LastAssist(Player player)
    {
        return GetAssists(player).OrderBy(x => x.Time).FirstOrDefault();
    }

    public static float GetTotalDamage(Player player)
    {
        return GetAssists(player).Sum(x => x.Damage);
    }
}

public static class PlayerAssistExtensions
{
    public static List<Assist> GetAssists(this Player player)
    {
        return AssistManager.GetAssists(player);
    }

    public static bool HasAssistsFor(this Player player, Player target)
    {
        return AssistManager.HasAssists(target, player);
    }

    public static List<Assist> GetAssistsInWindow(this Player player, float window)
    {
        return player.GetAssists().Where(x => UnityEngine.Time.timeSinceLevelLoad - x.Time <= window).ToList();
    }

    public static float GetTotalDamage(this Player player)
    {
        return AssistManager.GetTotalDamage(player);
    }
}

public struct Assist
{
    public Player Target { get; set; }
    public float Damage { get; set; }
    public float Time { get; set; }
}