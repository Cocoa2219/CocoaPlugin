using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using CocoaPlugin.API.Managers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;
using MultiBroadcast.Commands.Subcommands;

namespace CocoaPlugin.API.Achievements.Challenge;

public class Rich : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.Rich;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Challenge];
    public override string Name { get; set; } = "부자";
    public override string Description { get; set; } = "한 게임에서 다섯 종류 이상의 키카드를 소지하십시오.";

    public override void RegisterEvents()
    {
        Player.ItemAdded += OnPickingUpItem;
    }

    public override void UnregisterEvents()
    {
        Player.ItemAdded -= OnPickingUpItem;
    }

    private void OnPickingUpItem(ItemAddedEventArgs ev)
    {
        if (ev.Player == null) return;

        if (GetKeycardCount(ev.Player) >= 5)
        {
            Achieve(ev.Player.UserId);
        }
    }

    private int GetKeycardCount(Exiled.API.Features.Player player)
    {
        var types = new HashSet<ItemType>();

        foreach (var i in player.Items.Where(x => x.IsKeycard))
        {
            types.Add(i.Type);
        }

        return types.Count;
    }
}