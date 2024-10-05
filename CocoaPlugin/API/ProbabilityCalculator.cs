using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandSystem;
using Exiled.API.Extensions;
using Exiled.API.Features;
using GameCore;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using UnityEngine;
using Log = Exiled.API.Features.Log;

namespace CocoaPlugin.API;

public static class ProbabilityCalculator
{
    public static Dictionary<string, Dictionary<Team, double>> GetSpawnProbability()
    {
        var players = Player.List.Where(x => RoleAssigner.CheckPlayer(x.ReferenceHub)).ToList();
        var probability = players.ToDictionary(player => player.UserId, player => new Dictionary<Team, double> { { Team.SCPs, 0 }, { Team.FoundationForces, 0 }, { Team.ClassD, 0 }, { Team.Scientists, 0 } });
        var tickets = new Dictionary<string, long>();

        using (var ticketLoader = new ScpTicketsLoader())
        {
            foreach (var player in players)
            {
                var ticket = ticketLoader.GetTickets(player.ReferenceHub, ScpPlayerPicker.DefaultTickets);
                tickets[player.UserId] = ticket;
            }
        }

        // if there are no players, return null
        if (tickets.Count == 0)
            return null;

        var teams = Utility.GetTeamCount(players.Count);

        // if there are no scps, set the probability of all players to 0
        if (teams[Team.SCPs] == 0)
        {
            foreach (var player in players)
            {
                probability[player.UserId][Team.SCPs] = 0;
            }
            goto Human;
        }

        // scp selection
        // first phase (max ticket selection)
        var maxTickets = GetMaxValue(tickets);

        foreach (var maxTicket in maxTickets)
        {
            probability[maxTicket.Key][Team.SCPs] += 1f / maxTickets.Count;
        }

        // if there is only one scp, set the probability of all players to 0
        if (teams[Team.SCPs] == 1)
        {
            foreach (var player in players.Where(player => maxTickets.All(x => x.Key != player.UserId)))
            {
                probability[player.UserId][Team.SCPs] = 0;
            }

            goto Human;
        }

        // second phase (random weight-based selection)
        var scpsToSpawn = teams[Team.SCPs];
        var totalWeight = tickets.Sum(x => Math.Pow(x.Value, scpsToSpawn));

        foreach (var player in players)
        {
            if (maxTickets.Any(x => x.Key == player.UserId))
                continue;

            var weight = (long)Math.Pow(tickets[player.UserId], scpsToSpawn);
            probability[player.UserId][Team.SCPs] += weight / totalWeight;
        }

        Human:
        // human selection
        var totalHumanWeight = new Dictionary<Team, double>
        {
            { Team.FoundationForces, 0 },
            { Team.ClassD, 0 },
            { Team.Scientists, 0 }
        };

        // first, calculate the total weight of each team
        foreach (var team in teams.Where(x => x.Key != Team.SCPs))
        {
            foreach (var player in players)
            {
                if (HumanSpawner.History.TryGetValue(player.UserId, out var roleHistory))
                {
                    totalHumanWeight[team.Key] += 1d / roleHistory.History.Count(x => RoleExtensions.GetTeam(x) == team.Key) + 1;
                }
                else
                {
                    totalHumanWeight[team.Key] += 1;
                }
            }

            // totalHumanWeight[team.Key] *= availableSlots;
        }

        // second, calculate the probability of each player
        foreach (var player in players)
        {
            var history = HumanSpawner.History.TryGetValue(player.UserId, out var roleHistory) ? roleHistory : new HumanSpawner.RoleHistory();

            foreach (var team in teams.Where(x => x.Key != Team.SCPs))
            {
                var weightOnTeam = 1d / history.History.Count(x => RoleExtensions.GetTeam(x) == team.Key) + 1;
                var notScp = 1 - probability[player.UserId][Team.SCPs];
                var weightOfTeam = teams.Where(x => x.Key != Team.SCPs).Sum(x => x.Value) == 0 ? 0 : (double)teams[team.Key] / teams.Where(x => x.Key != Team.SCPs).Sum(x => x.Value);

                // Log.Info($"Player: {player.UserId}, Team: {team.Key}, WeightOnTeam: {weightOnTeam}, NotScp: {notScp}, WeightOfTeam: {weightOfTeam}, TotalHumanWeight: {totalHumanWeight[team.Key]}");

                probability[player.UserId][team.Key] = weightOfTeam * notScp * weightOnTeam / totalHumanWeight[team.Key];
            }
        }

        foreach (var player in players)
        {
            var totalProbability = probability[player.UserId].Values.Sum();

            foreach (var team in probability[player.UserId].Keys)
            {
                // Normalize the probabilities to ensure they sum to 1
                probability[player.UserId][team] /= totalProbability;
            }
        }

        return probability;
    }

    private static List<KeyValuePair<string, long>> GetMaxValue(Dictionary<string, long> dict)
    {
        var maxValue = dict.Max(x => x.Value);

        return dict.Where(x => x.Value == maxValue).ToList();
    }
}

[CommandHandler(typeof(ClientCommandHandler))]
public class CalculatorTestCommand : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "Only players can execute this command.";
            return false;
        }

        var probability = ProbabilityCalculator.GetSpawnProbability();

        if (probability == null)
        {
            response = "No players to calculate.";
            return false;
        }

        var result = probability[player.UserId];

        response = $"SCP: {result[Team.SCPs] * 100}%\nMTF: {result[Team.FoundationForces] * 100}%\nClass-D: {result[Team.ClassD] * 100}%\nScientist: {result[Team.Scientists] * 100}%";
        return true;
    }

    public string Command { get; } = "ctest";
    public string[] Aliases { get; } = { "ct" };
    public string Description { get; }  = "Calculates the spawn probability of each player.";
}