using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using PlayerRoles.PlayableScps;
using UnityEngine;

namespace CocoaPlugin.API;

public class SightManager : MonoBehaviour
{
    private Player _player;
    private HashSet<Player> _seenPlayers;

    public void Start()
    {
        _player = Player.Get(gameObject);
        _seenPlayers = [];
    }

    public void Update()
    {
        if (_player is not { IsAlive: true })
            return;

        foreach (var player in Player.List)
        {
            if (VisionInformation.IsInView(_player.ReferenceHub, player.ReferenceHub))
            {
                _seenPlayers.Add(player);
            }
            else
            {
                _seenPlayers.Remove(player);
            }
        }

        Log.Info(string.Join(", ", _seenPlayers.Select(x => x.Nickname)));
    }

    public static SightManager Get(Player player)
    {
        return player.GameObject.GetComponent<SightManager>();
    }
}