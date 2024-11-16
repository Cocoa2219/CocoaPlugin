using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AdminToys;
using CommandSystem;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using SLPlayerRotation;
using UnityEngine;

namespace CocoaPlugin.API.Beta;

public class Pathfinding
{
    public void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.Left += OnLeft;
        Exiled.Events.Handlers.Player.DroppingItem += OnThrowing;
        Exiled.Events.Handlers.Player.AimingDownSight += OnAimingDownSight;
    }

    public void Destroy()
    {
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Exiled.Events.Handlers.Player.DroppingItem -= OnThrowing;
        Exiled.Events.Handlers.Player.AimingDownSight -= OnAimingDownSight;
    }

    public static readonly CachedLayerMask Mask = new("Default");

    private Dictionary<Player, Path> _paths = new();

    private Dictionary<Player, Debug> _debug = new();

    private void OnShooting(ShootingEventArgs ev)
    {
        if (!_pathGuns.TryGetValue(ev.Player, out var gun)) return;
        if (ev.Item.Serial != gun) return;

        var ray = new Ray(ev.Player.CameraTransform.position + ev.Player.CameraTransform.forward * 0.2f,
            ev.Player.CameraTransform.forward);

        ev.IsAllowed = false;

        if (!Physics.Raycast(ray, out var hit, 100f, Mask)) return;

        if (!_paths.TryGetValue(ev.Player, out var path))
        {
            path = new Path();
            _paths.Add(ev.Player, path);
        }

        var point = hit.point;

        if (path.Start.HasValue)
        {
            if (path.End.HasValue)
            {
                path.Start = point;
                path.End = null;

                ev.Player.ShowHint("Start point set to " + point + ",\nEnd point: " + path.End);
            }
            else
            {
                path.End = point;

                ev.Player.ShowHint("Start point: " + path.Start + ",\nEnd point set to " + point);
            }
        }
        else
        {
            path.Start = point;

            ev.Player.ShowHint("Start point set to " + point + ",\nEnd point: " + path.End);
        }

        RefreshPrimitive(ev.Player, path.Start, path.End);
    }

    private void RefreshPrimitive(Player player, Vector3? start, Vector3? end)
    {
        if (!_debug.TryGetValue(player, out var debug))
        {
            return;
        }

        debug.StartPoint?.Destroy();
        debug.EndPoint?.Destroy();

        if (start.HasValue)
        {
            debug.StartPoint = Primitive.Create(PrimitiveType.Cube,
                start.Value,
                Vector3.zero,
                Vector3.one * 0.5f,
                true,
                Color.white);

            debug.StartPoint.Flags = PrimitiveFlags.Visible;
        }

        if (end.HasValue)
        {
            debug.EndPoint = Primitive.Create(PrimitiveType.Cube,
                end.Value,
                Vector3.zero,
                Vector3.one * 0.5f,
                true,
                Color.white);

            debug.EndPoint.Flags = PrimitiveFlags.Visible;
        }
    }

    private void OnAimingDownSight(AimingDownSightEventArgs ev)
    {
        if (!_pathGuns.TryGetValue(ev.Player, out var gun)) return;
        if (ev.Item.Serial != gun) return;
        if (!ev.AdsIn) return;

        if (_debug.TryGetValue(ev.Player, out var debug))
        {
            debug.StartPoint?.Destroy();
            debug.EndPoint?.Destroy();

            _debug.Remove(ev.Player);

            ev.Player.ShowHint("Debug mode disabled");
        }
        else
        {
            _debug.Add(ev.Player, new Debug());

            ev.Player.ShowHint("Debug mode enabled");
        }



        if (_paths.TryGetValue(ev.Player, out var path))
            RefreshPrimitive(ev.Player, path.Start, path.End);
    }

    private void OnThrowing(DroppingItemEventArgs ev)
    {
        if (!ev.IsThrown) return;
        if (!_pathGuns.TryGetValue(ev.Player, out var gun)) return;
        if (ev.Item.Serial != gun) return;

        ev.IsAllowed = false;

        if (_paths.TryGetValue(ev.Player, out var path))
        {
            StartPath(path, ev.Player);
        }
    }

    private void StartPath(Path path, Player player)
    {
        if (!path.Start.HasValue || !path.End.HasValue)
        {
            player.ShowHint("Path not set up correctly");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var sequence = path.CalculateSequence();

        stopwatch.Stop();

        if (sequence == null)
        {
            player.ShowHint("No path found");
            return;
        }

        player.ShowHint("Path found in " + stopwatch.ElapsedMilliseconds + "ms");

        if (_debug.ContainsKey(player))
        {
            foreach (var room in Room.List)
            {
                room.ResetColor();
            }

            foreach (var door in Door.List)
            {
                door.IsOpen = false;
            }

            foreach (var obj in sequence)
            {
                switch (obj)
                {
                    case Room room:
                        room.Color = Color.green;
                        break;
                    case Door door:
                        door.IsOpen = true;
                        break;
                }
            }
        }

        var npc = Npc.Spawn("Pathfinder", RoleTypeId.ClassD, true, "pathfinder@localhost", Vector3.zero);
        // npc.IsGodModeEnabled = true;

        Timing.CallDelayed(0.5f, () =>
        {
            var pathfinder = npc.GameObject.AddComponent<Pathfinder>();
            pathfinder.Initialize(npc);
            pathfinder.SetPath(path);
        });
    }

    private void OnLeft(LeftEventArgs ev)
    {
        if (_debug.ContainsKey(ev.Player))
        {
            _debug.Remove(ev.Player);
        }

        if (_pathGuns.ContainsKey(ev.Player))
        {
            _pathGuns.Remove(ev.Player);
        }
    }

    private Dictionary<Player, ushort> _pathGuns = new();

    internal void TogglePathGun(Player player)
    {
        if (_pathGuns.ContainsKey(player))
        {
            player.RemoveItem(_pathGuns[player]);
            _pathGuns.Remove(player);
        }
        else
        {
            var gun = player.AddItem(ItemType.GunCOM18);

            _pathGuns.Add(player, gun.Serial);

            player.AddItem(gun);
            player.CurrentItem = gun;
        }
    }
}

public class Path
{
    private Vector3? _start;
    private Vector3? _end;
    private Room _startRoom;
    private Room _endRoom;

    public Vector3? Start {
        get => _start;
        set
        {
            if (!value.HasValue)
            {
                _start = null;
                _startRoom = null;

                return;
            }

            _start = value.Value;
            _startRoom = Room.Get(value.Value);
        }
    }
    public Vector3? End {
        get => _end;
        set
        {
            if (!value.HasValue)
            {
                _end = null;
                _endRoom = null;

                return;
            }

            _end = value.Value;
            _endRoom = Room.Get(value.Value);
        }
    }

    public Sequence CalculateSequence()
    {
        if (_startRoom == null || _endRoom == null)
            return null;

        var list = new List<(Room room, List<object> waypoint)>();
        var visited = new HashSet<Room>();

        list.Add((_startRoom, [_startRoom]));
        visited.Add(_startRoom);

        while (list.Count > 0)
        {
            var (currentRoom, path) = list[0];
            list.RemoveAt(0);

            if (currentRoom == _endRoom)
            {
                Sequence = new Sequence
                {
                    Waypoint = path
                };
                return Sequence;
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var door in currentRoom.Doors)
            {
                var nextRoom = door.Rooms.FirstOrDefault(r => r != currentRoom);
                if (nextRoom != null && visited.Add(nextRoom))
                {
                    // var newPath = new List<Room>(path) { nextRoom };
                    // var newDoors = new List<Door>(doors) { door };
                    //
                    // list.Add((nextRoom, newPath, newDoors));

                    var newPath = new List<object>(path) { door, nextRoom };
                    list.Add((nextRoom, newPath));
                }
            }
        }

        return null;
    }

    public Sequence Sequence { get; private set; }
}

public class Sequence : IEnumerator<object>, IEnumerable<object>
{
    public List<object> Waypoint { get; set; }

    private int _index = -1;

    public void Dispose()
    {
        // ignored
    }

    public bool MoveNext()
    {
        if (_index < Waypoint.Count - 1)
        {
            _index++;
            return true;
        }

        return false;
    }

    public void Reset()
    {
        _index = -1;
    }

    public object Current
    {
        get
        {
            if (_index < 0 || _index >= Waypoint.Count)
                return null;

            return Waypoint[_index];
        }
    }

    object IEnumerator.Current => Current;

    public IEnumerator<object> GetEnumerator()
    {
        Reset();
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class Pathfind : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender);
        if (player == null)
        {
            response = "플레이어가 아니군, 그렇지 않나.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "당신, 무언가를 잊어버린 모양이야.";
            return false;
        }

        var arg = arguments.At(0);

        switch (arg)
        {
            case "gun":
                CocoaPlugin.Instance.Pathfinding.TogglePathGun(player);
                response = "앞으로 조금 어려울 테니 총을 주도록 하지...";
                return true;
            default:
                response = "그건... 처음 들어 보는 말이군.";
                return false;
        }
    }

    public string Command { get; } = "pathfind";
    public string[] Aliases { get; } = { "pf" };
    public string Description { get; } = "길잡이베길수입니다";
}

public class Jump
{
    public Room Origin { get; set; }
    public Room Destination { get; set; }
}

public class Debug
{
    public Primitive StartPoint { get; set; }
    public Primitive EndPoint { get; set; }
}

public class Pathfinder : MonoBehaviour
{
    private Npc _npc;
    private CharacterController _controller;
    private Path _path;
    private FpcMouseLook _mouseLook;

    public void Initialize(Npc npc)
    {
        if (!npc.Role.Is(out FpcRole fpcRole)) return;

        _npc = npc;
        _controller = fpcRole.FirstPersonController.FpcModule.CharController;
        _mouseLook = fpcRole.FirstPersonController.FpcModule.MouseLook;
    }

    public void SetPath(Path path)
    {
        _path = path;

        if (_path.Sequence == null) return;
        if (!_path.Start.HasValue || !_path.End.HasValue) return;

        _npc.Position = _path.Start.Value + Vector3.up;
    }

    private int _index = 0;

    private void Update()
    {
        if (_path == null)
        {
            Log.Info("Path not set");
            return;
        }
        if (_index >= _path.Sequence.Waypoint.Count)
        {
            Destroy(this);
            return;
        }

        var current = _path.Sequence.Waypoint[_index];
        var target = current switch
        {
            Room room => room.Position + Vector3.up,
            Door door => door.Position + Vector3.up,
            _ => Vector3.zero
        };

        target = new Vector3(target.x, _npc.Position.y, target.z);

        var direction = (target - _npc.Position).normalized;

        if (Physics.Raycast(_npc.Position, direction, out var hit, 0.5f, Pathfinding.Mask))
        {
            Log.Info("Raycast hit while pathfinding, trying to avoid");

            var avoid = Vector3.Cross(Vector3.up, hit.normal);

            direction = Vector3.Lerp(direction, avoid, 0.5f);
        }

        var move = direction * 5.4f;
        _controller.Move(move * UnityEngine.Time.deltaTime);

        var rotation = Quaternion.LookRotation(direction).ToClientUShorts();
        _mouseLook.ApplySyncValues(rotation.horizontal, rotation.vertical);

        if (Vector3.Distance(_npc.Position, target) < 0.2f)
        {
            if (_index >= _path.Sequence.Waypoint.Count)
            {
                Destroy(this);
            }

            Log.Info("Reached target, moving to next waypoint");
            _index++;
        }
    }
}