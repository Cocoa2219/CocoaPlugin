using System;
using System.Collections.Generic;
using System.Linq;
using CocoaPlugin.Commands;
using HarmonyLib;
using NorthwoodLib.Pools;
using PlayerRoles.RoleAssign;

namespace CocoaPlugin.Patches;

[HarmonyPatch(typeof(ScpPlayerPicker), nameof(ScpPlayerPicker.GenerateList))]
public class ScpSpawnPatch
{
    public static bool Prefix(ScpTicketsLoader loader, int scpsToAssign)
    {
		ScpPlayerPicker.ScpsToSpawn.Clear();
		if (scpsToAssign <= 0)
		{
			return false;
		}
		int num = 0;
		foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
		{
			if (RoleAssigner.CheckPlayer(referenceHub) && NoScp.NoScpPlayers.All(x => x.ReferenceHub != referenceHub))
			{
				int tickets = loader.GetTickets(referenceHub, 10);
				if (tickets >= num)
				{
					if (tickets > num)
					{
						ScpPlayerPicker.ScpsToSpawn.Clear();
					}
					num = tickets;
					ScpPlayerPicker.ScpsToSpawn.Add(referenceHub);
				}
			}
		}
		if (ScpPlayerPicker.ScpsToSpawn.Count > 1)
		{
			ReferenceHub item = ScpPlayerPicker.ScpsToSpawn.RandomItem();
			ScpPlayerPicker.ScpsToSpawn.Clear();
			ScpPlayerPicker.ScpsToSpawn.Add(item);
		}
		scpsToAssign -= ScpPlayerPicker.ScpsToSpawn.Count;
		if (scpsToAssign <= 0)
		{
			return false;
		}
		List<ScpPlayerPicker.PotentialScp> list = ListPool<ScpPlayerPicker.PotentialScp>.Shared.Rent();
		long num2 = 0L;
		using (HashSet<ReferenceHub>.Enumerator enumerator = ReferenceHub.AllHubs.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				ReferenceHub referenceHub2 = enumerator.Current;
				if (!ScpPlayerPicker.ScpsToSpawn.Contains(referenceHub2) && RoleAssigner.CheckPlayer(referenceHub2) && NoScp.NoScpPlayers.All(x => x.ReferenceHub != referenceHub2))
				{
					long num3 = 1L;
					int tickets2 = loader.GetTickets(referenceHub2, 10);
					for (int i = 0; i < scpsToAssign; i++)
					{
						num3 *= (long)tickets2;
					}
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
		var num4 = (double)UnityEngine.Random.value * (double)num2;
		for (var j = 0; j < list.Count; j++)
		{
			ScpPlayerPicker.PotentialScp potentialScp = list[j];
			num4 -= (double)potentialScp.Weight;
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