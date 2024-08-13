using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MapEditorReborn.API.Features;
using MapEditorReborn.API.Features.Objects;
using MapEditorReborn.API.Features.Serializable;
using MapEditorReborn.Commands.UtilityCommands;
using MapGeneration;
using UnityEngine;
using Player = Exiled.Events.Handlers.Player;

namespace CocoaPlugin.API.Beta;

public class Minimap
{
    public Dictionary<ZoneType, RoomStruct[,]> Rooms { get; set; }

    public void OnMapGenerated()
    {
        Rooms = new Dictionary<ZoneType, RoomStruct[,]>();

        const float roomDistance = 15f; // Fixed distance between rooms

        var lczRooms = Room.Get(ZoneType.LightContainment).ToList();

        float minX = lczRooms.Min(room => room.Position.x);
        float minZ = lczRooms.Min(room => room.Position.z);

        int gridX = lczRooms.Select(room => Mathf.RoundToInt((room.Position.x - minX) / roomDistance)).Max() + 1;
        int gridZ = lczRooms.Select(room => Mathf.RoundToInt((room.Position.z - minZ) / roomDistance)).Max() + 1;

        var rooms = new RoomStruct[gridX, gridZ];

        foreach (var room in lczRooms)
        {
            int xIndex = Mathf.RoundToInt((room.Position.x - minX) / roomDistance);
            int zIndex = Mathf.RoundToInt((room.Position.z - minZ) / roomDistance);

            rooms[xIndex, zIndex] = new RoomStruct
            {
                Type = room.RoomShape,
                Name = room,
                Position = room.Position,
                Rotation = room.Rotation
            };
        }

        Rooms.Add(ZoneType.LightContainment, rooms);

        var hczRooms = Room.Get(ZoneType.HeavyContainment).ToList();

        minX = hczRooms.Min(room => room.Position.x);
        minZ = hczRooms.Min(room => room.Position.z);

        gridX = hczRooms.Select(room => Mathf.RoundToInt((room.Position.x - minX) / roomDistance)).Max() + 1;
        gridZ = hczRooms.Select(room => Mathf.RoundToInt((room.Position.z - minZ) / roomDistance)).Max() + 1;

        rooms = new RoomStruct[gridX, gridZ];

        foreach (var room in hczRooms)
        {
            int xIndex = Mathf.RoundToInt((room.Position.x - minX) / roomDistance);
            int zIndex = Mathf.RoundToInt((room.Position.z - minZ) / roomDistance);

            rooms[xIndex, zIndex] = new RoomStruct
            {
                Type = room.RoomShape,
                Name = room,
                Position = room.Position,
                Rotation = room.Rotation
            };
        }

        Rooms.Add(ZoneType.HeavyContainment, rooms);

        var ezRooms = Room.Get(ZoneType.Entrance).ToList();

        minX = ezRooms.Min(room => room.Position.x);
        minZ = ezRooms.Min(room => room.Position.z);

        gridX = ezRooms.Select(room => Mathf.RoundToInt((room.Position.x - minX) / roomDistance)).Max() + 1;
        gridZ = ezRooms.Select(room => Mathf.RoundToInt((room.Position.z - minZ) / roomDistance)).Max() + 1;

        rooms = new RoomStruct[gridX, gridZ];

        foreach (var room in ezRooms)
        {
            int xIndex = Mathf.RoundToInt((room.Position.x - minX) / roomDistance);
            int zIndex = Mathf.RoundToInt((room.Position.z - minZ) / roomDistance);

            rooms[xIndex, zIndex] = new RoomStruct
            {
                Type = room.RoomShape,
                Name = room,
                Position = room.Position,
                Rotation = room.Rotation
            };
        }

        Rooms.Add(ZoneType.Entrance, rooms);

        // Player.ChangingItem += OnChangingItem;
    }

    public Vector2 GetRoomPosition(Room room)
    {
        var rooms = Rooms[room.Zone];

        for (int x = 0; x < rooms.GetLength(0); x++)
        {
            for (int z = 0; z < rooms.GetLength(1); z++)
            {
                if (rooms[x, z].Name == room)
                {
                    return new Vector2(x, z);
                }
            }
        }

        return new Vector2(-1, -1);
    }

    public Dictionary<Exiled.API.Features.Player, Dictionary<SchematicObject, RoomStruct>> Minimaps { get; set; } = new();

    private void OnChangingItem(ChangingItemEventArgs ev)
    {
        if (ev.Item?.Type == ItemType.KeycardO5)
        {
            if (!Minimaps.ContainsKey(ev.Player)) return;

            var results = new RaycastHit[10];
            var size = Physics.RaycastNonAlloc(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward, results, 10f);

            if (size > 0)
            {
                foreach (var hit in results)
                {
                    if (hit.collider == null)
                    {
                        continue;
                    }

                    if (hit.transform.GetComponentInParent<SchematicObject>() != null)
                    {
                        var schematic = hit.transform.GetComponentInParent<SchematicObject>();

                        if (Minimaps[ev.Player].TryGetValue(schematic, out var room))
                        {
                            ev.Player.Position = room.Name.Position + Vector3.up * 1.5f;

                            ev.Player.ClearInventory();
                            ev.Player.DisableEffect(EffectType.Ensnared);

                            foreach (var s in Minimaps[ev.Player])
                            {
                                s.Key.Destroy();
                            }

                            Minimaps.Remove(ev.Player);

                            return;
                        }
                    }
                }
            }

            return;
        }

        if (ev.Item?.Type != ItemType.Adrenaline)
        {
            if (Minimaps.TryGetValue(ev.Player, out var schematics))
            {
                foreach (var schematic in schematics)
                {
                    schematic.Key.Destroy();
                }

                Minimaps.Remove(ev.Player);
            }

            ev.Player.DisableEffect(EffectType.Ensnared);

            return;
        }

        ev.Player.EnableEffect(EffectType.Ensnared);

        var roomsToShow = new List<(RoomStruct room, Vector2 gridPosition)>();

        var currentZoneRooms = Rooms[ev.Player.CurrentRoom.Zone];

        var schs = new Dictionary<SchematicObject, RoomStruct>();

        for (var x = 0; x < currentZoneRooms.GetLength(0); x++)
            for (var z = 0; z < currentZoneRooms.GetLength(1); z++)
            {
                var roomStruct = currentZoneRooms[x, z];

                var dist = Vector3.Distance(ev.Player.Position, roomStruct.Position);

                if (dist <= 30) roomsToShow.Add((roomStruct, new Vector2(x, z)));
            }

        var curRoomPos = GetRoomPosition(ev.Player.CurrentRoom);
        var curRoomIndex = new Vector3(curRoomPos.x, 0, curRoomPos.y);

        Minimaps.TryAdd(ev.Player, []);

        foreach (var (room, gridPosition) in roomsToShow)
        {
            var relativePosition = new Vector3(gridPosition.x, 0, gridPosition.y);

            relativePosition -= curRoomIndex;

            var spawnPosition = ev.Player.Position
                                + Vector3.forward * relativePosition.z * 0.5f
                                + Vector3.right * relativePosition.x * 0.5f;

            spawnPosition = new Vector3(spawnPosition.x, spawnPosition.y - 0.8f, spawnPosition.z);

            var rot = room.Rotation;

            if (room.Name?.Type == RoomType.LczClassDSpawn)
            {
                schs.Add(ObjectSpawner.SpawnSchematic(
                    new SchematicSerializable("ClassDCell"),
                    spawnPosition,
                    rot,
                    Vector3.one * 0.5f, isStatic: false), room);
            }
            else
            {
                schs.Add(ObjectSpawner.SpawnSchematic(
                    new SchematicSerializable(room.Type.ToString()),
                    spawnPosition,
                    rot,
                    Vector3.one * 0.5f, isStatic: false), room);
            }

            // Log.Info(sc.Position);
        }

        schs = schs.Where(x => x.Key != null).ToDictionary(x => x.Key, x => x.Value);

        Minimaps[ev.Player] = schs;
    }
}

public struct RoomStruct
{
    public RoomShape Type { get; set; }
    public Room Name { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
}
