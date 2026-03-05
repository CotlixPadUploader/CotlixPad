using ColtixPad.Classes;
using ColtixPad.Utilities;
using TMPro;
using UnityEngine;

namespace ColtixPad.Pages
{
    public class Settings : MonoBehaviour
    {
        public void InitializePage()
        {
            Transform pageTransform = transform;

            TMPro.TextMeshPro themeLabel = pageTransform.Find("ChangeTheme/Text")?.GetComponent<TMPro.TextMeshPro>();

            void UpdateThemeLabel()
            {
                if (themeLabel == null) return;
                int idx = Plugin.Configuration.ThemeIndex.Value % Tablet.Themes.Length;
                themeLabel.SafeSetText($"Theme: {Tablet.Themes[idx].name}");
            }

            UpdateThemeLabel();

            pageTransform.Find("ChangeTheme").AddComponent<Button>().OnClick += () =>
            {
                Plugin.Configuration.ThemeIndex.Value += 1;
                Plugin.Configuration.ThemeIndex.Value %= Tablet.Themes.Length;
                Plugin.Configuration.Save();

                Tablet.Instance.ApplyTheme();
                UpdateThemeLabel();
            };

            pageTransform.Find("ToggleNotifications").AddComponent<Button>().OnClick += () =>
            {
                Plugin.Configuration.Notifications.Value = !Plugin.Configuration.Notifications.Value;
                Plugin.Configuration.Save();

                pageTransform.Find("ToggleNotifications/Text").GetComponent<TextMeshPro>().SafeSetText(Plugin.Configuration.Notifications.Value ? "Disable Notifications" : "Enable Notifications");
            };
        }
    }
}
