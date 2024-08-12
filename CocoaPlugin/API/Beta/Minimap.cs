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
                Position = room.Position,
                Rotation = room.Rotation.eulerAngles
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
                Position = room.Position,
                Rotation = room.Rotation.eulerAngles
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
                Rotation = room.Rotation.eulerAngles
            };
        }

        Rooms.Add(ZoneType.Entrance, rooms);

        foreach (var room in Rooms)
        {
            for (int z = 0; z < room.Value.GetLength(1); z++)
            {
                string line = "";
                for (int x = 0; x < room.Value.GetLength(0); x++)
                {
                    line += room.Value[x, z].Type switch
                    {
                        RoomShape.Endroom => room.Value[x, z].Rotation == Vector3.zero ? "E" : "F",
                        RoomShape.TShape => room.Value[x, z].Rotation == Vector3.zero ? "T" : "S",
                        RoomShape.Curve => room.Value[x, z].Rotation == Vector3.zero ? "C" : "L",
                        RoomShape.Straight => room.Value[x, z].Rotation == Vector3.zero ? "I" : "J",
                        RoomShape.XShape => "X",
                        _ => " "
                    };
                }

                Log.Info(line);
            }

            Log.Info("\n\n");
        }

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

    private void OnChangingItem(ChangingItemEventArgs ev)
    {
        if (ev.Item?.Type != ItemType.Adrenaline) return;

        ev.Player.EnableEffect(EffectType.Ensnared);

        var roomsToShow = new List<(RoomStruct room, Vector2 gridPosition)>();

        var currentZoneRooms = Rooms[ev.Player.CurrentRoom.Zone];

        int newX = 0;
        int newZ = 0;

        for (int x = 0; x < currentZoneRooms.GetLength(0); x++)
        {
            for (int z = 0; z < currentZoneRooms.GetLength(1); z++)
            {
                var roomStruct = currentZoneRooms[x, z];

                var dist = Vector3.Distance(ev.Player.Position, roomStruct.Position);

                if (dist <= 500)
                {
                    roomsToShow.Add((roomStruct, new Vector2(newX, newZ)));
                }
            }
        }

        var curRoom = roomsToShow.Find(x => x.room.Name == ev.Player.CurrentRoom);

        foreach (var (room, gridPosition) in roomsToShow)
        {
            var relativePosition = new Vector3(gridPosition.x, 0, gridPosition.y);

            var spawnPosition = ev.Player.Position
                                + Vector3.forward * relativePosition.z * 0.5f
                                + Vector3.right * relativePosition.x * 0.5f;

            var sc = ObjectSpawner.SpawnSchematic(
                new SchematicSerializable(room.Type.ToString()),
                spawnPosition,
                Quaternion.Euler(room.Rotation),
                Vector3.one * 0.5f, isStatic: false);

        }
    }
}

public struct RoomStruct
{
    public RoomShape Type { get; set; }
    public Room Name { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
}
