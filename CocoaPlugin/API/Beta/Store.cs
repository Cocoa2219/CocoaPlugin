using System;
using System.Collections.Generic;
using System.Linq;
using CentralAuth;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;
using Exiled.Permissions.Extensions;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using RemoteAdmin;
using UnityEngine;
using Player = Exiled.Events.Handlers.Player;
using Random = UnityEngine.Random;

namespace CocoaPlugin.API.Beta;

public class Store
{
    public Dictionary<RoomType, List<Transform>> NpcSpawnPoints { get; set; }

    private Npc _npc;

    public Store()
    {
        var spawnpoints = new Dictionary<RoomType, List<Transform>>();

        spawnpoints.Add(RoomType.HczArmory, [new Transform()
        {
            Position = new Vector3(0.25f, 1f, 1.8f),
            Rotation = new Vector3(0f, 230f, 0f)
        }]);

        spawnpoints.Add(RoomType.Hcz939, [new Transform()
        {
            Position = new Vector3(-5.3f, 1f, 5.8f),
            Rotation = new Vector3(0f, 220f, 0f)
        }]);

        spawnpoints.Add(RoomType.HczEzCheckpointA, [new Transform()
        {
            Position = new Vector3(-4.3f, 1f, -6.4f),
            Rotation = new Vector3(0f, 0f, 0f)
        }]);

        spawnpoints.Add(RoomType.HczEzCheckpointB, [new Transform()
        {
            Position = new Vector3(-4.3f, 1f, -6.4f),
            Rotation = new Vector3(0f, 0f, 0f)
        }]);

        spawnpoints.Add(RoomType.Hcz106, [new Transform()
        {
            Position = new Vector3(17.8f, 1.2f, 3f),
            Rotation = new Vector3(0f, 134f, 0f)
        }]);

        spawnpoints.Add(RoomType.Hcz049, [new Transform()
        {
            Position = new Vector3(1.1f, 198f, 6.4f),
            Rotation = new Vector3(0f, 157f, 0f)
        }]);

        NpcSpawnPoints = spawnpoints;
    }

    public void RegisterEvents()
    {
        Player.FlippingCoin += OnFlippingCoin;
    }

    public void UnregisterEvents()
    {
        Player.FlippingCoin -= OnFlippingCoin;
    }

    public void SpawnNpc(RoomType roomType = RoomType.Unknown)
    {
        var isDefault = roomType == RoomType.Unknown;

        if (isDefault)
        {
            roomType = NpcSpawnPoints.Keys.ElementAt(Random.Range(0, NpcSpawnPoints.Keys.Count));
        }

        var spawnPoint = NpcSpawnPoints[roomType][Random.Range(0, NpcSpawnPoints[roomType].Count)];

        var globalPosition = Room.Get(roomType).WorldPosition(spawnPoint.Position);

        Log.Debug($"Spawning NPC at {globalPosition} in room {roomType}.");

        Timing.KillCoroutines("UpdateNpcRotation");
        _npc?.Destroy();

        var npc = Npc.Spawn("상점 주인", RoleTypeId.Tutorial, 0, "store@localhost", globalPosition);

        _npc = npc;

        try
        {
            // npc.ReferenceHub.authManager._privUserId = "ID_Dedicated";
            // npc.ReferenceHub.authManager.SyncedUserId = "ID_Dedicated";
            //
            // npc.ReferenceHub.authManager.InstanceMode = ClientInstanceMode.DedicatedServer;
        }
        catch (Exception)
        {
            // ignored
        }

        Timing.CallDelayed(1f, () =>
        {
            // var quat = Quaternion.Euler(spawnPoint.Rotation);
            // var mouseLook = ((IFpcRole)npc.ReferenceHub.roleManager.CurrentRole).FpcModule.MouseLook;
            // mouseLook.ApplySyncValues(ToClientUShorts(quat).horizontal, ToClientUShorts(quat).vertical);

            Timing.RunCoroutine(UpdateNpcRotation(), "UpdateNpcRotation");
        });
    }

    private IEnumerator<float> UpdateNpcRotation()
    {
        if (_npc == null) yield break;
        var mouseLook = ((IFpcRole)_npc.ReferenceHub.roleManager.CurrentRole).FpcModule.MouseLook;
        while (true)
        {
            yield return Timing.WaitForSeconds(0.1f);

            var closestPlayer = Exiled.API.Features.Player.List.OrderBy(x => Vector3.Distance(x.Position, _npc.Position)).FirstOrDefault(x => x != _npc);

            if (closestPlayer == null) continue;

            var rotation = Quaternion.LookRotation(closestPlayer.Position - _npc.Position, Vector3.up);

            mouseLook.ApplySyncValues(ToClientUShorts(rotation).horizontal, ToClientUShorts(rotation).vertical);
        }
    }

    public void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        if (ev.Player == null) return;

        if (Physics.Raycast(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward * 0.2f, out var hit, 5f))
        {
            Log.Info(hit.collider.name);

            var hitNpc = Npc.Get(hit.collider.GetComponentInParent<ReferenceHub>());

            if (hitNpc == null) return;
            if (hitNpc != _npc) return;

            ev.Item.Destroy();

            var originalItems = ev.Player.Items.Select(x => x.Clone()).ToList();

            ev.Player.ClearItems();
        }
    }

    private (ushort horizontal, ushort vertical) ToClientUShorts(Quaternion rotation)
    {
        const float ToHorizontal = ushort.MaxValue / 360f;
        const float ToVertical = ushort.MaxValue / 176f;

        var fixVertical = -rotation.eulerAngles.x;

        switch (fixVertical)
        {
            case < -90f:
                fixVertical += 360f;
                break;
            case > 270f:
                fixVertical -= 360f;
                break;
        }

        var horizontal = Mathf.Clamp(rotation.eulerAngles.y, 0f, 360f);
        var vertical = Mathf.Clamp(fixVertical, -88f, 88f) + 88f;

        return ((ushort)Math.Round(horizontal * ToHorizontal), (ushort)Math.Round(vertical * ToVertical));
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SpawnStoreCommand : ICommand
{
    public string Command { get; } = "spawnstore";
    public string[] Aliases { get; } = { "ss" };
    public string Description { get; } = "Spawns the store NPC.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (sender is not PlayerCommandSender)
        {
            response = "Only players can execute this command.";
            return false;
        }

        CocoaPlugin.Instance.Store.SpawnNpc();

        response = "Store NPC spawned.";
        return true;
    }
}

public struct Transform
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
}