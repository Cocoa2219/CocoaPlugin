using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using PlayerRoles.PlayableScps;
using UnityEngine;

namespace CocoaPlugin.API;

public class SightManager : MonoBehaviour
{
    private Player _player;
    private HashSet<Player> _seenPlayers;
    private Dictionary<Player, CoroutineHandle> _coroutines;

    public void Start()
    {
        _player = Player.Get(gameObject);
        _seenPlayers = [];
        _coroutines = new Dictionary<Player, CoroutineHandle>();
    }

    public void Update()
    {
        if (_player is not { IsAlive: true })
            return;

        foreach (var player in Player.List.Where(x => x != _player).ToList())
        {
            _coroutines.TryAdd(player, new CoroutineHandle());

            if (VisionInformation.IsInView(_player.ReferenceHub, player.ReferenceHub))
            {
                if (_coroutines.TryGetValue(player, out var coroutine))
                    Timing.KillCoroutines(coroutine);
                _seenPlayers.Add(player);
            }
            else
            {
                if (_seenPlayers.Contains(player))
                {
                    _coroutines[player] = Timing.CallDelayed(3f, () =>
                    {
                        _seenPlayers.Remove(player);
                        _coroutines.Remove(player);
                    });
                }
            }
        }
    }

    public bool IsSeen(Player player)
    {
        return _seenPlayers.Contains(player);
    }

    public static SightManager Get(Player player)
    {
        return player.GameObject.GetComponent<SightManager>();
    }
}