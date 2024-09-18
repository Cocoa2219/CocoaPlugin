using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Firearms.BasicMessages;
using InventorySystem.Items.Firearms.Modules;
using MEC;
using RelativePositioning;
using RemoteAdmin;
using SLPlayerRotation;
using UnityEngine;

namespace CocoaPlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ForceRotation : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        Player player;
        if (!float.TryParse(arguments.At(1), out float x) || !float.TryParse(arguments.At(2), out float y) || !float.TryParse(arguments.At(3), out float z))
        {
            if (arguments.At(1) == "stop")
            {
                player = Player.Get(arguments.At(0));

                if (player == null)
                {
                    response = "플레이어를 찾을 수 없습니다.";
                    return false;
                }

                Timing.KillCoroutines("ForceRot_" + player.UserId);
                response = "플레이어의 시선 고정을 중지했습니다.";
                return true;
            }

            if (arguments.At(1) == "start")
            {
                player = Player.Get(arguments.At(0));

                if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out var hit, 1000f, LayerMask.GetMask("Default")))
                {
                    x = hit.point.x;
                    y = hit.point.y;
                    z = hit.point.z;

                    Timing.KillCoroutines("ForceRot_" + player.UserId);

                    _coroutines.Add(Timing.RunCoroutine(ForceRotationCoroutine(player, new Vector3(x, y, z)), "ForceRot_" + player.UserId));

                    response = "플레이어의 시선을 고정했습니다.";
                    return true;
                }

                response = "좌표를 찾을 수 없습니다.";
                return false;
            }

            response = "좌표는 숫자여야 합니다.";
            return false;
        }

        player = Player.Get(arguments.At(0));

        if (player == null)
        {
            response = "플레이어를 찾을 수 없습니다.";
            return false;
        }

        Timing.KillCoroutines("ForceRot_" + player.UserId);

        _coroutines.Add(Timing.RunCoroutine(ForceRotationCoroutine(player, new Vector3(x, y, z)), "ForceRot_" + player.UserId));

        response = "플레이어의 시선을 고정했습니다.";
        return true;
    }

    private static List<CoroutineHandle> _coroutines = [];

    public static void OnRoundRestarting()
    {
        foreach (var coroutine in _coroutines)
        {
            Timing.KillCoroutines(coroutine);
        }

        foreach (var coroutine in ForceRotationToPlayer._coroutines)
        {
            Timing.KillCoroutines(coroutine);
        }
    }

    private IEnumerator<float> ForceRotationCoroutine(Player player, Vector3 target)
    {
        while (true)
        {
            yield return Timing.WaitForOneFrame;

            var rotation = Quaternion.LookRotation(target - player.CameraTransform.position, Vector3.up);

            player.SetHubRotation(rotation);
        }
    }

    public static (ushort horizontal, ushort vertical) ToClientUShorts(Quaternion rotation)
    {
        const float ToHorizontal = ushort.MaxValue / 360f;
        const float ToVertical = ushort.MaxValue / 176f;

        float fixVertical = -rotation.eulerAngles.x;

        if (fixVertical < -90f)
        {
            fixVertical += 360f;
        }
        else if (fixVertical > 270f)
        {
            fixVertical -= 360f;
        }

        float horizontal = Mathf.Clamp(rotation.eulerAngles.y, 0f, 360f);
        float vertical = Mathf.Clamp(fixVertical, -88f, 88f) + 88f;

        return ((ushort)Math.Round(horizontal * ToHorizontal), (ushort)Math.Round(vertical * ToVertical));
    }

    public string Command { get; } = "forcerotation";
    public string[] Aliases { get; } = { "fr" };
    public string Description { get; } = "플레이어의 시선을 고정합니다.";
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ForceRotationToPlayer : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(arguments.At(0));
        var target = Player.Get(arguments.At(1));

        if (arguments.At(1) == "stop")
        {
            if (player == null)
            {
                response = "플레이어를 찾을 수 없습니다.";
                return false;
            }

            Timing.KillCoroutines("ForceRotPlayer_" + player.UserId);
            response = "플레이어의 시선 고정을 중지했습니다.";
            return true;
        }

        if (arguments.At(1) == "closest")
        {
            if (player == null)
            {
                response = "플레이어를 찾을 수 없습니다.";
                return false;
            }

            Timing.KillCoroutines("ForceRotPlayer_" + player.UserId);

            _coroutines.Add(Timing.RunCoroutine(ForceRotationCoroutine(player), "ForceRotPlayer_" + player.UserId));

            response = "플레이어의 시선을 고정했습니다.";
            return true;
        }

        if (player == null)
        {
            response = "플레이어를 찾을 수 없습니다.";
            return false;
        }

        if (target == null)
        {
            response = "대상 플레이어를 찾을 수 없습니다.";
            return false;
        }

        _coroutines.Add(Timing.RunCoroutine(ForceRotationCoroutine(player, target), "ForceRotPlayer_" + player.UserId));

        response = "플레이어의 시선을 고정했습니다.";
        return true;
    }

    private IEnumerator<float> ForceRotationCoroutine(Player player, Player target)
    {
        while (true)
        {
            yield return Timing.WaitForOneFrame;

            if (target.IsDead) continue;

            var rotation = Quaternion.LookRotation(target.CameraTransform.position - player.CameraTransform.position, Vector3.up);

            player.SetHubRotation(rotation);
        }
    }

    private IEnumerator<float> ForceRotationCoroutine(Player player)
    {
        while (true)
        {
            yield return Timing.WaitForOneFrame;

            var closest = Player.List.Where(x => x != player).OrderBy(x => Vector3.Distance(player.Position, x.Position)).FirstOrDefault();

            if (closest == null) continue;

            var rotation = Quaternion.LookRotation(closest.CameraTransform.position - player.CameraTransform.position, Vector3.up);

            player.SetHubRotation(rotation);
        }
    }

    public static List<CoroutineHandle> _coroutines = [];

    public string Command { get; } = "forcerotationto";
    public string[] Aliases { get; } = { "frt" };
    public string Description { get; } = "플레이어의 시선을 고정합니다.";
}