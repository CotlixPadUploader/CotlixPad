using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ColtixPad.Classes
{
    public class Tablet : MonoBehaviour
    {
        public static void InitializeTablet()
        {
            GameObject tablet = Utilities.Assets.LoadObject<GameObject>("CheckUI");
            tablet.transform.SetParent(VRRig.LocalRig.leftHandTransform.parent, false);
            tablet.AddComponent<Tablet>();
        }

        public enum Page
        {
            None,
            Room,
            Player,
            Media,
            Settings
        }

        public GameObject mainObject;
        private Dictionary<Page, GameObject> pageObjects;

        private Page _currentPage = Page.None;
        public Page CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage == value) return;
                _currentPage = value;

                foreach (var page in pageObjects)
                    page.Value.SetActive(page.Key == _currentPage);
            }
        }

        public static Tablet Instance { get; private set; }

        public GameObject ui;
        public Material backgroundMaterial;
        public Material buttonMaterial;

        public bool muted;
        private Color _lastDynamicColor = Color.clear;

        public void Start()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            gameObject.AddComponent<Rigidbody>().isKinematic = true;

            Transform uiTransform = transform.Find("Main");
            ui = uiTransform.gameObject;

            uiTransform.Find("Background/Title").GetComponent<TextMeshPro>().text = $"ColtixPad\n{PluginInfo.Version}";

            // Override credits/disclaimer text from original asset
            foreach (TextMeshPro tmp in GetComponentsInChildren<TextMeshPro>(true))
            {
                if (tmp.text != null && (
                    tmp.text.Contains("crimsoncauldron") ||
                    tmp.text.Contains("condone") ||
                    tmp.text.Contains("iiDk-the-actual") ||
                    tmp.text.Contains("LibrePad does not")))
                {
                    tmp.text = "This is a Official fork of LibrePad\nwe will try updating as soon as possible\nand right as gtag updates happen";
                }
            }

            backgroundMaterial = uiTransform.Find("Background").GetComponent<Renderer>().sharedMaterial;
            buttonMaterial = uiTransform.Find("Sidebar/Room").GetComponent<Renderer>().sharedMaterial;

            ApplyTheme();

            pageObjects = new Dictionary<Page, GameObject>
            {
                { Page.Room, uiTransform.Find("Room").gameObject },
                { Page.Player, uiTransform.Find("Player").gameObject },
                { Page.Media, uiTransform.Find("Media").gameObject },
                { Page.Settings, uiTransform.Find("Settings").gameObject }
            };

            uiTransform.Find("Room").AddComponent<Pages.Room>().InitializePage();
            uiTransform.Find("Player").AddComponent<Pages.Player>().InitializePage();
            uiTransform.Find("Media").AddComponent<Pages.Media>().InitializePage();
            uiTransform.Find("Settings").AddComponent<Pages.Settings>().InitializePage();

            uiTransform.Find("Sidebar/Room").AddComponent<Button>().OnClick += () => CurrentPage = Page.Room;
            uiTransform.Find("Sidebar/Player").AddComponent<Button>().OnClick += () => CurrentPage = Page.Player;
            uiTransform.Find("Sidebar/Media").AddComponent<Button>().OnClick += () => CurrentPage = Page.Media;
            uiTransform.Find("Sidebar/Microphone").AddComponent<Button>().OnClick += () =>
            {
                muted = !muted;

                uiTransform.Find("Sidebar/Microphone/Muted").gameObject.SetActive(muted);
                uiTransform.Find("Sidebar/Microphone/Unmuted").gameObject.SetActive(!muted);
            };
            uiTransform.Find("Sidebar/Settings").AddComponent<Button>().OnClick += () => CurrentPage = Page.Settings;

            CurrentPage = Page.Room;
            ui.SetActive(false);
        }

        private bool previousYButton;
        public void Update()
        {
            bool yButton = ControllerInputPoller.instance.leftControllerSecondaryButton;

            if (yButton && !previousYButton)
            {
                bool nowOpen = !ui.activeSelf;
                ui.SetActive(nowOpen);

                // Tell other ColtixPad players whether our pad is open or closed
                PadSync.BroadcastLocalOpen(nowOpen);
            }

            previousYButton = yButton;

            if (GorillaTagger.Instance.myRecorder != null)
                GorillaTagger.Instance.myRecorder.TransmitEnabled = !muted;

            // Keep "My Color" theme in sync with the player's current gorilla color (no broadcast spam)
            if (Plugin.Configuration.ThemeIndex.Value % Themes.Length == 0
                && backgroundMaterial != null && VRRig.LocalRig != null)
            {
                Color c = VRRig.LocalRig.playerColor;
                if (_lastDynamicColor != c)
                {
                    _lastDynamicColor = c;
                    backgroundMaterial.color = new Color32(
                        (byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
                    buttonMaterial.color = new Color32(
                        (byte)(c.r * 160), (byte)(c.g * 160), (byte)(c.b * 160), 255);
                }
            }
        }

        public static readonly (string name, Color32 bg, Color32 btn)[] Themes = new[]
        {
            // Index 0: dynamic — matches your in-game gorilla color
            ("My Color",     new Color32(128, 128, 128, 255), new Color32(80,  80,  80,  255)),
            // Original 8
            ("Red",          new Color32(195, 69,  78,  255), new Color32(99,  31,  34,  255)),
            ("Orange",       new Color32(193, 127, 69,  255), new Color32(142, 86,  37,  255)),
            ("Yellow",       new Color32(193, 183, 69,  255), new Color32(142, 133, 37,  255)),
            ("Green",        new Color32(90,  193, 69,  255), new Color32(56,  140, 37,  255)),
            ("Sky Blue",     new Color32(68,  141, 191, 255), new Color32(37,  104, 140, 255)),
            ("Blue",         new Color32(68,  68,  191, 255), new Color32(37,  37,  137, 255)),
            ("Purple",       new Color32(113, 68,  191, 255), new Color32(74,  37,  137, 255)),
            ("Pink",         new Color32(191, 68,  158, 255), new Color32(137, 37,  109, 255)),
            // New colors
            ("Crimson",      new Color32(180, 20,  40,  255), new Color32(110, 10,  20,  255)),
            ("Coral",        new Color32(220, 100, 80,  255), new Color32(160, 60,  45,  255)),
            ("Peach",        new Color32(230, 160, 120, 255), new Color32(180, 110, 75,  255)),
            ("Gold",         new Color32(212, 175, 55,  255), new Color32(150, 120, 25,  255)),
            ("Lime",         new Color32(130, 210, 50,  255), new Color32(80,  150, 25,  255)),
            ("Mint",         new Color32(80,  200, 150, 255), new Color32(40,  140, 100, 255)),
            ("Teal",         new Color32(40,  170, 160, 255), new Color32(20,  115, 110, 255)),
            ("Cyan",         new Color32(40,  200, 220, 255), new Color32(20,  140, 160, 255)),
            ("Ocean",        new Color32(30,  100, 180, 255), new Color32(15,  60,  130, 255)),
            ("Navy",         new Color32(25,  40,  120, 255), new Color32(12,  20,  80,  255)),
            ("Indigo",       new Color32(75,  50,  180, 255), new Color32(45,  25,  130, 255)),
            ("Violet",       new Color32(150, 60,  210, 255), new Color32(100, 30,  155, 255)),
            ("Magenta",      new Color32(210, 50,  180, 255), new Color32(150, 25,  125, 255)),
            ("Hot Pink",     new Color32(230, 60,  130, 255), new Color32(170, 30,  85,  255)),
            ("Rose",         new Color32(210, 100, 130, 255), new Color32(155, 55,  80,  255)),
            ("Lavender",     new Color32(160, 130, 210, 255), new Color32(110, 80,  160, 255)),
            ("Slate",        new Color32(90,  110, 140, 255), new Color32(55,  70,  95,  255)),
            ("Steel",        new Color32(120, 140, 160, 255), new Color32(75,  95,  115, 255)),
            ("Silver",       new Color32(180, 185, 190, 255), new Color32(120, 125, 130, 255)),
            ("White",        new Color32(230, 230, 230, 255), new Color32(160, 160, 160, 255)),
            ("Charcoal",     new Color32(70,  70,  75,  255), new Color32(35,  35,  40,  255)),
            ("Obsidian",     new Color32(30,  30,  35,  255), new Color32(12,  12,  18,  255)),
            ("Mocha",        new Color32(140, 95,  65,  255), new Color32(90,  55,  30,  255)),
            ("Forest",       new Color32(40,  100, 55,  255), new Color32(20,  60,  30,  255)),
            ("Olive",        new Color32(110, 120, 40,  255), new Color32(70,  80,  20,  255)),
            ("Sand",         new Color32(210, 190, 140, 255), new Color32(160, 140, 85,  255)),
            ("Blush",        new Color32(230, 170, 170, 255), new Color32(180, 115, 115, 255)),
            ("Bubblegum",    new Color32(240, 130, 180, 255), new Color32(190, 75,  130, 255)),
            ("Ice",          new Color32(175, 220, 235, 255), new Color32(115, 165, 185, 255)),
            ("Aqua",         new Color32(50,  210, 200, 255), new Color32(25,  150, 145, 255)),
            ("Toxic",        new Color32(140, 230, 30,  255), new Color32(90,  165, 15,  255)),
            ("Neon Orange",  new Color32(255, 130, 20,  255), new Color32(195, 80,  5,   255)),
            ("Neon Pink",    new Color32(255, 50,  180, 255), new Color32(195, 20,  130, 255)),
            ("Neon Green",   new Color32(50,  255, 100, 255), new Color32(20,  190, 60,  255)),
            ("Neon Blue",    new Color32(30,  180, 255, 255), new Color32(10,  120, 200, 255)),
            ("Sunset",       new Color32(220, 100, 50,  255), new Color32(160, 55,  20,  255)),
            ("Dusk",         new Color32(120, 70,  140, 255), new Color32(75,  35,  95,  255)),
            ("Aurora",       new Color32(60,  180, 140, 255), new Color32(30,  120, 90,  255)),
            ("Cherry",       new Color32(190, 30,  70,  255), new Color32(130, 12,  40,  255)),
            ("Grape",        new Color32(130, 50,  160, 255), new Color32(85,  22,  110, 255)),
            ("Sapphire",     new Color32(20,  80,  200, 255), new Color32(10,  45,  145, 255)),
            ("Emerald",      new Color32(25,  155, 85,  255), new Color32(12,  100, 50,  255)),
            ("Ruby",         new Color32(200, 25,  50,  255), new Color32(140, 10,  25,  255)),
            // +50 extra colors
            ("Tangerine",    new Color32(240, 110, 30,  255), new Color32(185, 65,  10,  255)),
            ("Pumpkin",      new Color32(215, 95,  15,  255), new Color32(160, 55,  5,   255)),
            ("Amber",        new Color32(230, 165, 10,  255), new Color32(175, 115, 5,   255)),
            ("Honey",        new Color32(235, 185, 50,  255), new Color32(180, 135, 20,  255)),
            ("Lemon",        new Color32(245, 230, 60,  255), new Color32(190, 175, 25,  255)),
            ("Chartreuse",   new Color32(155, 225, 25,  255), new Color32(100, 165, 10,  255)),
            ("Fern",         new Color32(65,  165, 75,  255), new Color32(30,  110, 40,  255)),
            ("Sage",         new Color32(120, 165, 115, 255), new Color32(75,  115, 70,  255)),
            ("Moss",         new Color32(80,  120, 55,  255), new Color32(45,  75,  25,  255)),
            ("Jungle",       new Color32(30,  85,  40,  255), new Color32(12,  50,  18,  255)),
            ("Pine",         new Color32(20,  70,  50,  255), new Color32(8,   40,  25,  255)),
            ("Spruce",       new Color32(40,  90,  70,  255), new Color32(18,  55,  40,  255)),
            ("Seafoam",      new Color32(85,  210, 175, 255), new Color32(45,  155, 125, 255)),
            ("Turquoise",    new Color32(40,  195, 185, 255), new Color32(18,  140, 135, 255)),
            ("Glacier",      new Color32(135, 200, 220, 255), new Color32(80,  145, 170, 255)),
            ("Arctic",       new Color32(180, 225, 240, 255), new Color32(120, 165, 185, 255)),
            ("Powder",       new Color32(175, 210, 240, 255), new Color32(115, 150, 185, 255)),
            ("Cornflower",   new Color32(100, 145, 235, 255), new Color32(55,  90,  180, 255)),
            ("Periwinkle",   new Color32(130, 130, 220, 255), new Color32(80,  80,  165, 255)),
            ("Cobalt",       new Color32(15,  55,  185, 255), new Color32(8,   28,  135, 255)),
            ("Royal",        new Color32(55,  35,  170, 255), new Color32(28,  15,  120, 255)),
            ("Plum",         new Color32(140, 60,  130, 255), new Color32(90,  28,  85,  255)),
            ("Orchid",       new Color32(185, 85,  185, 255), new Color32(130, 45,  130, 255)),
            ("Wisteria",     new Color32(165, 120, 200, 255), new Color32(115, 70,  150, 255)),
            ("Lilac",        new Color32(195, 160, 215, 255), new Color32(140, 105, 160, 255)),
            ("Thistle",      new Color32(210, 175, 210, 255), new Color32(155, 120, 155, 255)),
            ("Mauve",        new Color32(180, 130, 155, 255), new Color32(125, 80,  105, 255)),
            ("Dusty Rose",   new Color32(200, 145, 150, 255), new Color32(145, 90,  95,  255)),
            ("Salmon",       new Color32(235, 135, 115, 255), new Color32(180, 80,  65,  255)),
            ("Terracotta",   new Color32(195, 105, 75,  255), new Color32(140, 60,  38,  255)),
            ("Rust",         new Color32(175, 65,  30,  255), new Color32(120, 32,  10,  255)),
            ("Brick",        new Color32(165, 55,  40,  255), new Color32(110, 25,  15,  255)),
            ("Maroon",       new Color32(130, 20,  35,  255), new Color32(80,  8,   15,  255)),
            ("Wine",         new Color32(115, 15,  45,  255), new Color32(70,  5,   20,  255)),
            ("Burgundy",     new Color32(100, 10,  30,  255), new Color32(60,  3,   12,  255)),
            ("Espresso",     new Color32(90,  50,  30,  255), new Color32(50,  22,  10,  255)),
            ("Walnut",       new Color32(110, 70,  40,  255), new Color32(65,  35,  15,  255)),
            ("Caramel",      new Color32(185, 130, 70,  255), new Color32(130, 80,  30,  255)),
            ("Butterscotch", new Color32(215, 165, 80,  255), new Color32(160, 110, 35,  255)),
            ("Cream",        new Color32(240, 225, 185, 255), new Color32(185, 170, 130, 255)),
            ("Ivory",        new Color32(240, 235, 215, 255), new Color32(185, 180, 160, 255)),
            ("Pearl",        new Color32(235, 232, 228, 255), new Color32(180, 175, 170, 255)),
            ("Ash",          new Color32(160, 160, 158, 255), new Color32(105, 105, 103, 255)),
            ("Graphite",     new Color32(80,  82,  85,  255), new Color32(40,  42,  45,  255)),
            ("Midnight",     new Color32(15,  15,  40,  255), new Color32(5,   5,   20,  255)),
            ("Eclipse",      new Color32(25,  10,  45,  255), new Color32(12,  4,   25,  255)),
            ("Void",         new Color32(10,  8,   20,  255), new Color32(3,   2,   8,   255)),
            ("Neon Yellow",  new Color32(220, 255, 30,  255), new Color32(160, 195, 10,  255)),
            ("Neon Purple",  new Color32(180, 30,  255, 255), new Color32(120, 10,  195, 255)),
            ("Neon Red",     new Color32(255, 30,  60,  255), new Color32(195, 10,  30,  255)),
        };

        public void ApplyTheme()
        {
            int idx = Plugin.Configuration.ThemeIndex.Value % Themes.Length;

            if (idx == 0 && VRRig.LocalRig != null)
            {
                // Use the player's actual in-game gorilla color
                Color c = VRRig.LocalRig.playerColor;
                backgroundMaterial.color = new Color32(
                    (byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
                buttonMaterial.color = new Color32(
                    (byte)(c.r * 160), (byte)(c.g * 160), (byte)(c.b * 160), 255);
            }
            else
            {
                backgroundMaterial.color = Themes[idx].bg;
                buttonMaterial.color = Themes[idx].btn;
            }

            // Broadcast the new theme to other players in the room
            PadSync.BroadcastLocalTheme();
        }
    }
}
