using Exiled.API.Interfaces;

namespace CocoaPlugin.Configs
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        public Broadcasts Broadcasts { get; set; } = new();
        public Translations Translations { get; set; } = new();
    }
}