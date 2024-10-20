using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using HarmonyLib;
using MapGeneration.Distributors;
using UnityEngine;
using static HarmonyLib.AccessTools;
using Log = PluginAPI.Core.Log;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(StructureDistributor), nameof(StructureDistributor.TryGetNextStructure))]
public class GeneratorDistributePatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

        var index = newInstructions.FindIndex(instruction => instruction.Calls(Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), new[] { typeof(int), typeof(int) })));

        index -= 4;

        newInstructions.RemoveRange(index, 6);

        newInstructions.InsertRange(index, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(StructureDistributor), nameof(StructureDistributor.Settings))),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(SpawnablesDistributorSettings), nameof(SpawnablesDistributorSettings.SpawnableStructures))),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldind_I4),
            new CodeInstruction(OpCodes.Ldelem_Ref),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(SpawnableStructure), nameof(SpawnableStructure.StructureType))),
            new CodeInstruction(OpCodes.Call, Method(typeof(GeneratorDistributePatch), nameof(GetStructureSpawnpoint)))
        });

        foreach (var t in newInstructions)
        {
            yield return t;
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }

    private static List<StructureSpawnpoint> _spawnedGenerators = [];

    public static StructureSpawnpoint GetStructureSpawnpoint(List<StructureSpawnpoint> spawnpoints, StructureType structureType)
    {
        if (structureType != StructureType.Scp079Generator) return spawnpoints[UnityEngine.Random.Range(0, spawnpoints.Count)];

        if (_spawnedGenerators.Count == 0)
        {
            var generator = spawnpoints[UnityEngine.Random.Range(0, spawnpoints.Count)];

            _spawnedGenerators.Add(generator);
            return generator;
        }

        var farthestPoint = FindFarthestPoint(_spawnedGenerators, spawnpoints);

        _spawnedGenerators.Add(farthestPoint);

        return farthestPoint;
    }

    private static StructureSpawnpoint FindFarthestPoint(List<StructureSpawnpoint> listA,
        List<StructureSpawnpoint> listB)
    {
        if (listA == null || listB == null)
        {
            return null;
        }

        if (listA.Count == 0 || listB.Count == 0)
        {
            return null;
        }

        StructureSpawnpoint farthestSpawnpoint = null;
        var maxMinDistance = -Mathf.Infinity;

        foreach (var pointB in listB)
        {
            if (pointB == null || pointB.transform == null)
            {
                continue;
            }

            var positionB = pointB.transform.position;
            var minDistance = (from pointA in listA where pointA != null && pointA.transform != null select pointA.transform.position into positionA select Vector3.Distance(positionB, positionA)).Prepend(Mathf.Infinity).Min();

            if (minDistance > maxMinDistance)
            {
                maxMinDistance = minDistance;
                farthestSpawnpoint = pointB;
            }
        }

        return farthestSpawnpoint;
    }
}