using Exiled.API.Interfaces;

namespace CocoaPlugin.Configs
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        public Scps Scps { get; set; } = new();
        public Broadcasts Broadcasts { get; set; } = new();
        public Translations Translations { get; set; } = new();
        public Reconnects Reconnects { get; set; } = new();
        public Commands Commands { get; set; } = new();
        public Camping Camping { get; set; } = new();
        public Afk Afk { get; set; } = new();
        public Spawns Spawns { get; set; } = new();
        public AutoNuke AutoNuke { get; set; } = new();
        public Supporters Supporters { get; set; } = new();
    }
}