using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;

namespace CocoaPlugin.API;

public static class UserManager
{
    private const string UserFileName = "Users.txt";

    private static List<User> UserCache { get; } = new();

    public static bool AddUser(User user)
    {
        if (UserCache.Contains(user))
            return false;

        UserCache.Add(user);

        return true;
    }

    public static bool RemoveUser(string userId)
    {
        var user = UserCache.Find(u => u.UserId == userId);

        if (user == null)
            return false;

        UserCache.Remove(user);

        return true;
    }

    public static User GetUser(string userId)
    {
        return UserCache.FirstOrDefault(u => u.UserId == userId);
    }

    public static User GetUserByDiscordId(string discordId)
    {
        return UserCache.FirstOrDefault(u => u.DiscordId == discordId);
    }

    public static bool IsUserExist(string userId)
    {
        return UserCache.Any(u => u.UserId == userId);
    }

    public static bool IsDiscordUserExist(string discordId)
    {
        return UserCache.Any(u => u.DiscordId == discordId);
    }

    public static void SaveUsers()
    {
        var text = string.Join('\n', UserCache.Select(u => $"{u.UserId};{u.DiscordId};{u.GameNickname};{u.DiscordNickname}"));

        FileManager.WriteFile(UserFileName, text);
    }

    public static void LoadUsers()
    {
        var text = FileManager.ReadFile(UserFileName);

        if (string.IsNullOrEmpty(text))
            return;

        UserCache.Clear();

        foreach (var line in text.Split('\n'))
        {
            var data = line.Split(';');

            if (data.Length != 4)
                continue;

            var user = new User
            {
                UserId = data[0],
                DiscordId = data[1],
                GameNickname = data[2],
                DiscordNickname = data[3]
            };

            UserCache.Add(user);
        }
    }
}

public class User
{
    public string UserId { get; set; }
    public string DiscordId { get; set; }
    public string GameNickname { get; set; }
    public string DiscordNickname { get; set; }

    public void Update(Player player)
    {
        GameNickname = player.Nickname;
    }
}

public static class UserPlayerExtensions
{
    public static bool IsLinked(this Player player)
    {
        return UserManager.IsUserExist(player.UserId);
    }

    public static User GetUser(this Player player)
    {
        return UserManager.GetUser(player.UserId);
    }

    public static void Link(this Player player, string discordId, string discordNickname)
    {
        if (player.IsLinked())
            return;

        var user = new User
        {
            UserId = player.UserId,
            DiscordId = discordId,
            GameNickname = player.Nickname,
            DiscordNickname = discordNickname
        };

        UserManager.AddUser(user);

        ConnectionManager.RemoveConnection(discordId);
        ConnectionManager.SaveConnections();
    }
}