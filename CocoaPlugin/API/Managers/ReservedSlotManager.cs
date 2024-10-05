using System.Linq;

namespace CocoaPlugin.API.Managers;

public static class ReservedSlotManager
{
    private const string FileName = "ReservedSlots.txt";

    public static bool Get(string player)
    {
        var text = FileManager.ReadFile(FileName);

        if (string.IsNullOrWhiteSpace(text)) return false;

        var lines = text.Split('\n');

        var list = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

        return list.Contains(player);
    }
}