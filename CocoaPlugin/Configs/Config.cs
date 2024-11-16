using System.ComponentModel;
using Exiled.API.Interfaces;
using YamlDotNet.Serialization;

namespace CocoaPlugin.Configs
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        [YamlMember(Alias = "vsr_compliant")]
        public bool VSRCompliant { get; set; } = true;

        public Scps Scps { get; set; } = new();
        public Broadcasts Broadcasts { get; set; } = new();
        public Translations Translations { get; set; } = new();
        public Reconnects Reconnects { get; set; } = new();
        public Commands Commands { get; set; } = new();
        public Camping Camping { get; set; } = new();
        public Afk Afk { get; set; } = new();
        public Spawns Spawns { get; set; } = new();
        public AutoNuke AutoNuke { get; set; } = new();
        public Network Network { get; set; } = new();
        public Others Others { get; set; } = new();
        public Achievements Achievements { get; set; } = new();
        public Logs Logs { get; set; } = new();
        public Ranks Ranks { get; set; } = new();
        public Queue Queue { get; set; } = new();
    }
}