using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MultiBroadcast.API;

namespace CocoaPlugin.API.Managers;

public static class PenaltyManager
{
    private const string PenaltyFileName = "Penalties.txt";

    private static Dictionary<string, List<Penalty>> PenaltyCache { get; } = new();

    public static event Action<string, Penalty> OnPenaltyAdded;
    public static event Action<string, Penalty> OnPenaltyRemoved;

    public static bool AddPenalty(string id, Penalty penalty)
    {
        if (!Utility.IsUserIdValid(id) || !penalty.IsValid())
            return false;

        if (!PenaltyCache.TryGetValue(id, out var penalties))
        {
            penalties = [];
            PenaltyCache[id] = penalties;
        }

        penalties.Add(penalty);

        OnPenaltyAdded?.Invoke(id, penalty);

        if (Player.TryGet(id, out var player))
        {
            player.AddBroadcast(CocoaPlugin.Instance.Config.Broadcasts.PenaltyAddedMessage.Duration, CocoaPlugin.Instance.Config.Broadcasts.PenaltyAddedMessage.Message, CocoaPlugin.Instance.Config.Broadcasts.PenaltyAddedMessage.Priority);
            player.SendConsoleMessage($"\n<b><color=#d44b42>벌점이 추가되었습니다.</color></b>\n\n사유 | <b>{penalty.Reason}</b>\n유효일 | <color=#d44b42><b>{Utility.UnixTimeToDateTime(penalty.Issued)}</b></color>부터 <color=#d44b42><b>{Utility.UnixTimeToDateTime(penalty.Until)}</b></color>까지 유효합니다.\n처리 관리자 | <color=#d44b42>{penalty.IssuerNickname} ({penalty.Issuer})</color>", "white");
        }

        return true;
    }

    public static int GetPenaltyCount(string id)
    {
        return PenaltyCache.TryGetValue(id, out var value) ? value.Count : 0;
    }

    public static List<Penalty> GetPenalties(string id)
    {
        return PenaltyCache.TryGetValue(id, out var value) ? value : [];
    }

    public static bool RemovePenalty(string id, int index)
    {
        if (!PenaltyCache.TryGetValue(id, out var penalties) || index < 0 || index >= penalties.Count)
            return false;

        var penalty = penalties[index];

        OnPenaltyRemoved?.Invoke(id, penalty);

        penalties.RemoveAt(index);

        return true;
    }

    public static void RefreshPenalties()
    {
        foreach (var (_, penalties) in PenaltyCache)
            penalties.RemoveAll(penalty => !penalty.IsValid());

        foreach (var id in PenaltyCache.Keys.ToList().Where(id => PenaltyCache[id].Count == 0))
        {
            PenaltyCache.Remove(id);
        }
    }

    public static void RefreshPenalty(string id)
    {
        if (!PenaltyCache.TryGetValue(id, out var penalties))
            return;

        penalties.RemoveAll(penalty => !penalty.IsValid());
    }

    public static void SavePenalties()
    {
        RefreshPenalties();

        var text = string.Join("\n", PenaltyCache.Select(x => $"{x.Key};{string.Join(",", x.Value.Select(p => $"({p.Reason}/{p.Issued}/{p.Until}/{p.Issuer}/{p.IssuerNickname})"))}"));

        FileManager.WriteFile(PenaltyFileName, text);
    }

    public static void LoadPenalties()
    {
        var text = FileManager.ReadFile(PenaltyFileName);

        if (string.IsNullOrWhiteSpace(text))
            return;

        PenaltyCache.Clear();

        foreach (var line in text.Split('\n'))
        {
            var split = line.Split(';');

            if (split.Length < 2)
                continue;

            var id = split[0];
            var penalties = (from penalty in split[1].Split(',') select penalty.Trim('(', ')').Split('/') into penaltySplit where penaltySplit.Length >= 5 select new Penalty() { Reason = penaltySplit[0], Issued = long.Parse(penaltySplit[1]), Until = long.Parse(penaltySplit[2]), Issuer = penaltySplit[3], IssuerNickname = penaltySplit[4]}).ToList();

            PenaltyCache[id] = penalties;
        }

        RefreshPenalties();
    }
}

public class Penalty
{
    public string Reason { get; init; }
    public long Issued { get; init; }
    public long Until { get; init; }
    public string Issuer { get; init; }
    public string IssuerNickname { get; init; }

    public bool IsValid()
    {
        return Until > Utility.UnixTimeNow && !string.IsNullOrWhiteSpace(Reason) && Utility.IsUserIdValid(Issuer) && !string.IsNullOrWhiteSpace(IssuerNickname);
    }
}

public static class PenaltyUserExtension
{
    public static int GetPenaltyCount(this Player player)
    {
        return PenaltyManager.GetPenaltyCount(player.UserId);
    }

    public static List<Penalty> GetPenalties(this Player player)
    {
        return PenaltyManager.GetPenalties(player.UserId);
    }
}