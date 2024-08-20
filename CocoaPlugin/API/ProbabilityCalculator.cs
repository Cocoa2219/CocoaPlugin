using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using GameCore;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using UnityEngine;

namespace CocoaPlugin.API;

public static class ProbabilityCalculator
{
    public static string SpawnQueue => ConfigFile.ServerConfig.GetString("team_respawn_queue", "4014314031441404134041434414");

    public static Dictionary<Team, int> GetTeamCount(int count)
    {
        var teams = new Dictionary<Team, int>();

        for (var i = 0; i < count; i++)
        {
            var team = (Team)int.Parse(SpawnQueue[i % SpawnQueue.Length].ToString());
            if (!teams.TryAdd(team, 1))
                teams[team]++;
        }

        return teams;
    }

    public static List<Team> GetTeamList(int count)
    {
        var teams = new List<Team>();

        for (var i = 0; i < count; i++)
        {
            var team = (Team)int.Parse(SpawnQueue[i % SpawnQueue.Length].ToString());
            teams.Add(team);
        }

        return teams;
    }

    // public static Probabilty CalculateProbability()
    // {
    //     var scpCount = GetTeamCount(Player.List.Count)[Team.SCPs];
    //
    //     var probabilities = new Dictionary<Player, Dictionary<Team, double>>();
    //     var tickets = new Dictionary<Player, int>();
    //     var players = Player.List.ToHashSet();
    //
    //     foreach (var player in players)
    //     {
    //         var scpTickets = GetTickets(player);
    //         tickets.Add(player, scpTickets);
    //     }
    //
    //     var scpProbabilities = GetScpProbabilty(tickets, scpCount);
    //
    //
    // }

    public static Dictionary<Player, double> GetScpProbabilty(Dictionary<Player, int> tickets, int scpCount)
    {
        var probabilities = new Dictionary<Player, double>();
        var players = tickets.Keys.ToHashSet();

        if (tickets.Count == 0)
            return probabilities;

        var highestTickets = GetHighestTickets(tickets);

        foreach (var player in highestTickets)
        {
            probabilities.Add(player, 1.0 / highestTickets.Count);
            players.Remove(player);
        }

        scpCount--;

        if (scpCount == 0)
            return probabilities;

        var sumTickets = players.Sum(x => (long)Mathf.Pow(tickets[x], scpCount));

        foreach (var player in players)
        {
            probabilities.Add(player, Mathf.Pow(tickets[player], scpCount) / sumTickets);
        }

        return probabilities;
    }

    // public static Dictionary<Player, double> GetHumanProbability()
    // {
    //     var probabilities = new Dictionary<Player, double>();
    //     var teams = GetTeamList(Player.List.Count);
    //
    //     foreach (var player in Player.List)
    //     {
    //         var history = GetHistory(player);
    //
    //
    //
    //     }
    // }

    public static List<Team> GetHistory(Player player)
    {
        var userId = player.UserId;

        return !HumanSpawner.History.TryGetValue(userId, out var history) ? [] : RoleTypeIdToTeam(history.History.ToList());
    }

    public static List<Team> RoleTypeIdToTeam(List<RoleTypeId> roleTypeIds)
    {
        return roleTypeIds.Select(x => x.GetTeam()).ToList();
    }

    public static HashSet<Player> GetHighestTickets(Dictionary<Player, int> tickets)
    {
        var max = tickets.Max(x => x.Value);
        return tickets.Where(x => x.Value == max).Select(x => x.Key).ToHashSet();
    }

    public static int GetTickets(Player player)
    {
        using var ticketLoader = new ScpTicketsLoader();
        return ticketLoader.GetTickets(player.ReferenceHub, 10);
    }
}

public class Probabilty
{
    public Dictionary<Player, Dictionary<Team, double>> Probabilities { get; set; }
}