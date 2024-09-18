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

        var assist = Assists[ev.Player].FirstOrDefault(x => x.Player == ev.Attacker);

        if (assist == null)
        {
            assist = new Assist
            {
                Player = ev.Attacker,
                Damages = [ev.Amount],
                LastDamage = UnityEngine.Time.timeSinceLevelLoad
            };

            Assists[ev.Player].Add(assist);
        }
        else
        {
            assist.Damages.Add(ev.Amount);
            assist.LastDamage = UnityEngine.Time.timeSinceLevelLoad;
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
        return Assists.TryGetValue(target, out var assists) && assists.Any(x => x.Player == player);
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
        return player.GetAssists().Where(x => UnityEngine.Time.timeSinceLevelLoad - x.LastDamage <= window).ToList();
    }
}

public class Assist
{
    public Player Player { get; set; }
    public List<float> Damages { get; set; } = new();
    public float TotalDamage => Damages.Sum();
    public float LastDamage { get; set; }
}