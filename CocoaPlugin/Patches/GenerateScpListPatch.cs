using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CocoaPlugin.Commands;
using CommandSystem;
using Exiled.API.Features;
using HarmonyLib;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(ScpPlayerPicker), nameof(ScpPlayerPicker.GenerateList))]
public class GenerateScpListPatch
{
    public static bool Prefix(ScpTicketsLoader loader, int scpsToAssign)
    {
        ScpPlayerPicker.ScpsToSpawn.Clear();
        if (scpsToAssign <= 0) return false;
        var num = 0;
        foreach (var referenceHub in ReferenceHub.AllHubs)
            if (RoleAssigner.CheckPlayer(referenceHub) && NoScp.NoScpPlayers.All(x => x.ReferenceHub != referenceHub))
            {
                var tickets = loader.GetTickets(referenceHub, 10);
                if (tickets >= num)
                {
                    if (tickets > num) ScpPlayerPicker.ScpsToSpawn.Clear();
                    num = tickets;
                    ScpPlayerPicker.ScpsToSpawn.Add(referenceHub);
                }
            }

        if (ScpPlayerPicker.ScpsToSpawn.Count > 1)
        {
            var item = ScpPlayerPicker.ScpsToSpawn.RandomItem();
            ScpPlayerPicker.ScpsToSpawn.Clear();
            ScpPlayerPicker.ScpsToSpawn.Add(item);
        }

        scpsToAssign -= ScpPlayerPicker.ScpsToSpawn.Count;
        if (scpsToAssign <= 0) return false;
        var list = ListPool<ScpPlayerPicker.PotentialScp>.Shared.Rent();
        var num2 = 0L;
        using (var enumerator = ReferenceHub.AllHubs.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                var referenceHub2 = enumerator.Current;
                if (!ScpPlayerPicker.ScpsToSpawn.Contains(referenceHub2) && RoleAssigner.CheckPlayer(referenceHub2) &&
                    NoScp.NoScpPlayers.All(x => x.ReferenceHub != referenceHub2))
                {
                    var num3 = 1L;
                    var tickets2 = loader.GetTickets(referenceHub2, 10);
                    for (var i = 0; i < scpsToAssign; i++) num3 *= tickets2;
                    list.Add(new ScpPlayerPicker.PotentialScp
                    {
                        Player = referenceHub2,
                        Weight = num3
                    });
                    num2 += num3;
                }
            }

            goto IL_1C4;
        }

        IL_156:
        var num4 = Random.value * (double)num2;
        for (var j = 0; j < list.Count; j++)
        {
            var potentialScp = list[j];
            num4 -= potentialScp.Weight;
            if (num4 <= 0.0)
            {
                scpsToAssign--;
                ScpPlayerPicker.ScpsToSpawn.Add(potentialScp.Player);
                list.RemoveAt(j);
                num2 -= potentialScp.Weight;
                break;
            }
        }

        IL_1C4:
        if (scpsToAssign <= 0)
        {
            ListPool<ScpPlayerPicker.PotentialScp>.Shared.Return(list);
            return false;
        }

        goto IL_156;
    }
}

[HarmonyPatch(typeof(ScpSpawner), nameof(ScpSpawner.AssignScp))]
public class AssignScpPatch
{
    public static bool Prefix(List<ReferenceHub> chosenPlayers, RoleTypeId scp, List<RoleTypeId> otherScps)
    {
        ScpSpawner.ChancesBuffer.Clear();
        var num = 1;
        var num2 = 0;
        var ignored = new List<ReferenceHub>();

        foreach (var referenceHub in chosenPlayers)
        {
            var num3 = ScpSpawner.GetPreferenceOfPlayer(referenceHub, scp);

            if (num3 == -5)
            {
                ignored.Add(referenceHub);
                continue;
            }

            foreach (var scp2 in otherScps) num3 -= ScpSpawner.GetPreferenceOfPlayer(referenceHub, scp2);
            num2++;
            ScpSpawner.ChancesBuffer[referenceHub] = num3;
            num = Mathf.Min(num3, num);
        }

        // If all players have -5 pref on this scp,
        // Thinking (TODO: Get a new random SCP or just assign to a random player in the list)
        // Currently just assign to the random player in the list
        if (ignored.Count == chosenPlayers.Count)
        {
            var randomPlayer = chosenPlayers.RandomItem();
            chosenPlayers.Remove(randomPlayer);
            randomPlayer.roleManager.ServerSetRole(scp, RoleChangeReason.RoundStart);
            return false;
        }

        var num4 = 0f;
        ScpSpawner.SelectedSpawnChances.Clear();
        foreach (var keyValuePair in ScpSpawner.ChancesBuffer)
        {
            var num5 = Mathf.Pow(keyValuePair.Value - num + 1f, num2);
            ScpSpawner.SelectedSpawnChances[keyValuePair.Key] = num5;
            num4 += num5;
        }

        var num6 = num4 * Random.value;
        var num7 = 0f;
        foreach (var keyValuePair2 in ScpSpawner.SelectedSpawnChances)
        {
            num7 += keyValuePair2.Value;
            if (num7 >= num6)
            {
                var key = keyValuePair2.Key;
                chosenPlayers.Remove(key);
                key.roleManager.ServerSetRole(scp, RoleChangeReason.RoundStart);
                break;
            }
        }
        return false;
    }
}

[HarmonyPatch(typeof(ScpSpawner), nameof(ScpSpawner.SpawnScps))]
public class SpawnScpsPatch
{
    public static bool Prefix(int targetScpNumber)
    {
        ScpSpawner.EnqueuedScps.Clear();
        for (var i = 0; i < targetScpNumber; i++) ScpSpawner.EnqueuedScps.Add(ScpSpawner.NextScp);
        var chosenPlayers = ScpPlayerPicker.ChoosePlayers(targetScpNumber);
        var originalEnqueuedScps = ScpSpawner.EnqueuedScps.ToList();

        while (ScpSpawner.EnqueuedScps.Count > 0)
        {
            var enqueuedScp = ScpSpawner.EnqueuedScps[0];
            ScpSpawner.EnqueuedScps.RemoveAt(0);
            ScpSpawner.AssignScp(chosenPlayers, enqueuedScp, ScpSpawner.EnqueuedScps);
        }

        // If all players are SCPs, all players were assigned correctly
        if (chosenPlayers.All(x => x.GetTeam() == Team.SCPs)) return false;
        // If not, there is a player that has -5 pref on all SCPs
        // Assign SCPs to the players to [ScpSpawner.NextScp] and remove the player from the list

        // first reset the enqueued scps to the original list
        foreach (var t in originalEnqueuedScps)
        {
            ScpSpawner.EnqueuedScps.Add(t);
        }

        // then get the unspawned players, and assign them to the SCPs
        var unspawnedPlayers = chosenPlayers.Where(x => x.GetTeam() != Team.SCPs).ToList();

        foreach (var player in unspawnedPlayers)
        {
            player.roleManager.ServerSetRole(ScpSpawner.NextScp, RoleChangeReason.RoundStart);
            ScpSpawner.EnqueuedScps.Remove(ScpSpawner.NextScp);
        }

        return false;
    }
}

// [CommandHandler(typeof(RemoteAdminCommandHandler))]
// public class GetPrefs : ICommand
// {
//     public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
//     {
//         if (arguments.Count != 1)
//         {
//             response = "Usage: getprefs <player>";
//             return false;
//         }
//
//         var player = Player.Get(arguments.At(0));
//         if (player == null)
//         {
//             response = "Player not found";
//             return false;
//         }
//
//         var scps = Enum.GetValues(typeof(RoleTypeId)).Cast<RoleTypeId>().Where(x => x.GetTeam() == Team.SCPs);
//
//         response = "\n" + string.Join("\n", scps.Select(x => $"{x}: {ScpSpawner.GetPreferenceOfPlayer(player.ReferenceHub, x)}"));
//
//         return true;
//     }
//
//     public string Command { get; } = "getprefs";
//     public string[] Aliases { get; } = { "gp" };
//     public string Description { get; } = "Get the preferences of a player";
// }