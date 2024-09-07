using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using MapEditorReborn.API.Features;
using MapEditorReborn.API.Features.Objects;
using PlayerRoles;
using UnityEngine;

namespace CocoaPlugin.API.Beta;

public class ShootingRange
{
    private const string LobbyName = "ShootingRangeLobby";

    private Vector3 _spawnPosition = new(117f, -960f, 90f);

    public HashSet<Player> Players { get; } = new();
    public Dictionary<Player, SchematicObject> PlayerRanges { get; } = new();

    public void AddPlayer(Player player)
    {
        if (!Players.Add(player)) return;

        if (!player.IsHuman) player.Role.Set(RoleTypeId.ClassD);

        player.EnableEffect(EffectType.SoundtrackMute);

        var position = _spawnPosition;

        _spawnPosition += new Vector3(0, 0, 100);

        var s = ObjectSpawner.SpawnSchematic(LobbyName, position, Quaternion.identity, isStatic: true);

        PlayerRanges.Add(player, s);

        player.Position = position;
    }

    public void RemovePlayer(Player player)
    {
        if (!Players.Contains(player)) return;

        Players.Remove(player);

        player.DisableEffect(EffectType.SoundtrackMute);

        if (PlayerRanges.TryGetValue(player, out var schematic))
        {
            schematic.Destroy();
            PlayerRanges.Remove(player);
        }
    }
}