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
    }
}