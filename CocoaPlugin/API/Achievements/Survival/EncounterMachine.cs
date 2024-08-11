using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using Player = Exiled.Events.Handlers.Player;
using Server = Exiled.Events.Handlers.Server;

namespace CocoaPlugin.API.Achievements.Survival;

public class EncounterMachine : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.EncounterMachine;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "기계와의 마주침";
    public override string Description { get; set; } = "SCP-079와 단 한 번도 마주치지 않고 탈출하십시오.";

    private HashSet<string> _startPlayers;
    private CoroutineHandle _coroutine;

    public override void RegisterEvents()
    {
        Player.Escaping += OnEscaping;

        _startPlayers = [];

        Server.RoundStarted += OnRoundStarted;
    }

    public override void UnregisterEvents()
    {
        Player.Escaping -= OnEscaping;

        Server.RoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted()
    {
        _startPlayers.Clear();

        foreach (var player in Exiled.API.Features.Player.List) _startPlayers.Add(player.UserId);
        _coroutine = Timing.RunCoroutine(Check079());
    }

    private void OnEscaping(EscapingEventArgs ev)
    {
        if (ev.Player == null) return;

        if (_startPlayers.Contains(ev.Player.UserId)) Achieve(ev.Player.UserId);
    }

    public override void OnRoundRestarting()
    {
        Timing.KillCoroutines(_coroutine);

        _startPlayers.Clear();
    }

    private IEnumerator<float> Check079()
    {
        while (!Round.IsEnded)
        {
            foreach (var player in Exiled.API.Features.Player.Get(RoleTypeId.Scp079))
            {
                if (player == null) continue;

                var curRoom = player.Role.As<Scp079Role>().Camera.Room;

                foreach (var curRoomPlayer in curRoom.Players) _startPlayers.Remove(curRoomPlayer.UserId);
            }

            yield return Timing.WaitForSeconds(0.1f);
        }
    }
}