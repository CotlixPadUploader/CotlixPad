
using BepInEx.Configuration;

namespace ColtixPad.Utilities
{
    public class Configuration
    {
        public ConfigFile _config;

        public ConfigEntry<bool> Notifications;
        public ConfigEntry<int> ThemeIndex;

        public Configuration(ConfigFile config)
        {
            _config = config;
            Load();
        }

        public void Load()
        {
            ThemeIndex = _config.Bind(
                "Settings",
                "Theme",
                0,
                "Menu theme color index"
            );

            Notifications = _config.Bind(
                "Settings",
                "Notifications",
                true,
                "Shows notifications when people with mods join"
            );
        }

        public void Save() =>
            _config.Save();
    }
}