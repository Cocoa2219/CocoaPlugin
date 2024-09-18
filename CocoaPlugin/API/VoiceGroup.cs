using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles.Voice;
using VoiceChat;
using VoiceChat.Networking;

namespace CocoaPlugin.API;

public class VoiceGroup
{
    public VoiceGroup(string name)
    {
        Id = _id++;
        Name = name;

        Groups.Add(this);
    }

    public static List<VoiceGroup> Groups { get; set; } = new();
    public static Dictionary<Player, VoiceGroup> PlayerGroups { get; set; } = new();

    private static int _id;

    public int Id { get; }
    public string Name { get; set; }
    public HashSet<Player> Members { get; set; } = [];

    public bool AddMember(Player player)
    {
        GetGroup(player)?.RemoveMember(player);

        PlayerGroups[player] = this;

        return Members.Add(player);
    }

    public bool RemoveMember(Player player)
    {
        PlayerGroups.Remove(player);

        return Members.Remove(player);
    }

    public void Destroy()
    {
        foreach (var member in Members.ToList())
        {
            PlayerGroups.Remove(member);
        }

        Members.Clear();

        Groups.Remove(this);
    }

    public static VoiceGroup GetGroup(string name)
    {
        return Groups.Find(group => group.Name == name);
    }

    public static VoiceGroup GetGroup(int id)
    {
        return Groups.Find(group => group.Id == id);
    }

    public static VoiceGroup GetGroup(Player player)
    {
        return PlayerGroups.GetValueOrDefault(player);
    }

    public static VoiceGroup CreateGroup(string name)
    {
        var group = new VoiceGroup(name);

        return group;
    }

    public static void DestroyGroup(string name)
    {
        var group = GetGroup(name);

        group?.Destroy();
    }

    public static void DestroyGroup(int id)
    {
        var group = GetGroup(id);

        group?.Destroy();
    }

    private static bool IsInAnyGroup(Player player)
    {
        return PlayerGroups.ContainsKey(player);
    }

    private static bool IsInAnyGroup(ReferenceHub player)
    {
        return PlayerGroups.Keys.Any(x => x.ReferenceHub == player);
    }

    public static void OnRestartingRound()
    {
        foreach (var group in Groups)
        {
            group.Destroy();
        }

        Groups.Clear();
        PlayerGroups.Clear();

        _id = 0;
    }

    public static void OnVoiceChatting(VoiceChattingEventArgs ev)
    {
        if (!IsInAnyGroup(ev.Player))
        {
            ev.IsAllowed = false;

            var voiceChatChannel = ev.Player.VoiceModule.ValidateSend(ev.VoiceMessage.Channel);
            if (voiceChatChannel == VoiceChatChannel.None) return;
            ev.Player.VoiceModule.CurrentChannel = voiceChatChannel;
            foreach (var referenceHub in ReferenceHub.AllHubs)
                if (referenceHub.roleManager.CurrentRole is IVoiceRole voiceRole2)
                {
                    var voiceChatChannel2 =
                        voiceRole2.VoiceModule.ValidateReceive(ev.VoiceMessage.Speaker, voiceChatChannel);
                    if (voiceChatChannel2 != VoiceChatChannel.None)
                    {
                        if (IsInAnyGroup(referenceHub))
                        {
                            return;
                        }

                        var voiceMessage = new VoiceMessage
                        {
                            Speaker = ev.VoiceMessage.Speaker,
                            Data = ev.VoiceMessage.Data,
                            DataLength = ev.VoiceMessage.DataLength,
                            SpeakerNull = ev.VoiceMessage.SpeakerNull,
                            Channel = voiceChatChannel2
                        };

                        referenceHub.connectionToClient.Send(voiceMessage);
                    }
                }

            return;
        }

        ev.IsAllowed = false;

        var group = GetGroup(ev.Player);

        var newMessage = new VoiceMessage
        {
            Speaker = ev.VoiceMessage.Speaker,
            Data = ev.VoiceMessage.Data,
            DataLength = ev.VoiceMessage.DataLength,
            SpeakerNull = ev.VoiceMessage.SpeakerNull,
            Channel = VoiceChatChannel.PreGameLobby
        };

        foreach (var member in group.Members.Where(member => member != ev.Player))
        {
            member.Connection.Send(newMessage);
        }
    }
}