using System.Collections.Generic;
using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Handlers;
using PlayerRoles;
using Player = Exiled.API.Features.Player;

namespace CocoaPlugin.API.Achievements.Survival;

public class BloodedItem : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.BloodedItem;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "피 묻은 장비";
    public override string Description { get; set; } = "한 게임에서 오직 시체의 아이템만을 사용하십시오.";

    private HashSet<string> _startPlayers;

    public override void RegisterEvents()
    {
        _startPlayers = [];

        Server.RoundStarted += OnRoundStarted;

        Exiled.Events.Handlers.Player.ItemAdded += OnItemAdded;
        Exiled.Events.Handlers.Player.Escaping += OnEscaping;
    }

    public override void UnregisterEvents()
    {
        Server.RoundStarted -= OnRoundStarted;

        Exiled.Events.Handlers.Player.ItemAdded -= OnItemAdded;
        Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
    }

    private void OnRoundStarted()
    {
        _startPlayers.Clear();

        foreach (var player in Player.List)
        {
            _startPlayers.Add(player.UserId);
        }
    }

    private void OnEscaping(EscapingEventArgs ev)
    {
        if (ev.Player == null) return;

        if (_startPlayers.Contains(ev.Player.UserId)) Achieve(ev.Player.UserId);
    }

    private void OnItemAdded(Exiled.Events.EventArgs.Player.ItemAddedEventArgs ev)
    {
        if (ev.Player == null) return;
        if (ev.Player.Role != RoleTypeId.Scientist && ev.Player.Role != RoleTypeId.ClassD) return;
        if (ev.Item == null) return;
        if (ev.Pickup == null) return;

        if (ev.Pickup.PreviousOwner != null)
        {
            _startPlayers.Remove(ev.Player.UserId);
        }
    }
}