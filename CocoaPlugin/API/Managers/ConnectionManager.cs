using System.Collections.Generic;
using System.Linq;

namespace CocoaPlugin.API.Managers;

public static class ConnectionManager
{
    private const string ConnectionFileName = "Connections.txt";

    private static List<Connection> ConnectionCache { get; } = new();

    public static void RemoveConnection(string discordId)
    {
        var connection = ConnectionCache.Find(c => c.DiscordId == discordId);

        if (connection == null)
            return;

        ConnectionCache.Remove(connection);
    }

    public static void LoadConnections()
    {
        var text = FileManager.ReadFile(ConnectionFileName);

        if (string.IsNullOrEmpty(text))
            return;

        ConnectionCache.Clear();

        foreach (var line in text.Split('\n'))
        {
            var data = line.Split(';');

            if (data.Length != 4)
                continue;

            ConnectionCache.Add(new Connection
            {
                Code = data[0],
                DiscordId = data[1],
                DiscordNickname = data[2],
                Until = long.Parse(data[3])
            });
        }
    }

    private static void RefreshConnections()
    {
        ConnectionCache.RemoveAll(c => !c.IsValid());
    }

    public static void SaveConnections()
    {
        RefreshConnections();

        var text = string.Join('\n', ConnectionCache.Select(c => $"{c.Code};{c.DiscordId};{c.DiscordNickname}"));

        FileManager.WriteFile(ConnectionFileName, text);
    }

    public static bool HasCode(string code)
    {
        return ConnectionCache.Any(u => u.Code == code && u.IsValid());
    }

    public static Connection GetConnection(string code)
    {
        return ConnectionCache.FirstOrDefault(u => u.Code == code && u.IsValid());
    }
}

public class Connection
{
    public string Code { get; set; }
    public string DiscordId { get; set; }
    public string DiscordNickname { get; set; }
    public long Until { get; set; }

    public bool IsValid()
    {
        return Until > Utility.UnixTimeNow;
    }
}