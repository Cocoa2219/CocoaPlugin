using System.Linq;

namespace CocoaPlugin.API.Managers;

public static class ReservedSlotManager
{
    public static bool Get(string player)
    {
        var text = FileManager.ReadFile("ReservedSlots.txt");

        if (string.IsNullOrWhiteSpace(text)) return false;

        var lines = text.Split('\n');

        var list = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

        return list.Contains(player);
    }
}