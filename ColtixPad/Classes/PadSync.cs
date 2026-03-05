using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace ColtixPad.Classes
{
    /// <summary>
    /// Syncs the ColtixPad theme and open/close state to other players via Photon Custom Properties.
    /// Only spawns pads for players who have ColtixPad installed (detected via CPTheme property).
    /// </summary>
    public class PadSync : MonoBehaviourPunCallbacks
    {
        public const string PROP_THEME = "CPTheme"; // int  — theme index
        public const string PROP_OPEN  = "CPOpen";  // bool — whether the pad is open

        public static PadSync Instance { get; private set; }

        private readonly Dictionary<int, GameObject> _remotePads = new Dictionary<int, GameObject>();

        void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            BroadcastLocalTheme();
            BroadcastLocalOpen(false); // always start closed

            if (PhotonNetwork.InRoom)
            {
                foreach (Player player in PhotonNetwork.PlayerListOthers)
                    TrySpawnRemotePad(player);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Broadcast current theme index to the room.</summary>
        public static void BroadcastLocalTheme()
        {
            if (!PhotonNetwork.InRoom) return;
            int idx = Plugin.Configuration.ThemeIndex.Value % Tablet.Themes.Length;
            Hashtable props = new Hashtable { { PROP_THEME, idx } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        /// <summary>Broadcast whether our pad is currently open or closed.</summary>
        public static void BroadcastLocalOpen(bool isOpen)
        {
            if (!PhotonNetwork.InRoom) return;
            Hashtable props = new Hashtable { { PROP_OPEN, isOpen } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        // ── Photon Callbacks ─────────────────────────────────────────────────

        public override void OnJoinedRoom()
        {
            BroadcastLocalTheme();
            BroadcastLocalOpen(Tablet.Instance != null && Tablet.Instance.ui.activeSelf);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            TrySpawnRemotePad(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            DestroyRemotePad(otherPlayer.ActorNumber);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer.IsLocal) return;

            // If CPTheme just appeared or changed
            if (changedProps.ContainsKey(PROP_THEME))
            {
                if (_remotePads.TryGetValue(targetPlayer.ActorNumber, out GameObject existingPad))
                    ApplyThemeToPad(existingPad, (int)changedProps[PROP_THEME]);
                else
                    TrySpawnRemotePad(targetPlayer); // first time seeing CPTheme — spawn now
            }

            // If CPOpen changed, show or hide the remote pad's UI
            if (changedProps.ContainsKey(PROP_OPEN))
            {
                if (_remotePads.TryGetValue(targetPlayer.ActorNumber, out GameObject pad))
                {
                    bool open = (bool)changedProps[PROP_OPEN];
                    SetRemotePadVisible(pad, open);
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void TrySpawnRemotePad(Player player)
        {
            if (player.IsLocal) return;
            if (_remotePads.ContainsKey(player.ActorNumber)) return;

            // Only spawn for players who have ColtixPad — CPTheme must be present
            if (!player.CustomProperties.ContainsKey(PROP_THEME)) return;

            VRRig rig = GetRigForPlayer(player);
            if (rig == null) return;

            GameObject pad = Utilities.Assets.LoadObject<GameObject>("CheckUI");
            if (pad == null) return;

            pad.transform.SetParent(rig.leftHandTransform.parent, false);

            // Disable all buttons — remote pads are display only
            foreach (Button btn in pad.GetComponentsInChildren<Button>(true))
                btn.enabled = false;

            Transform main = pad.transform.Find("Main");
            if (main != null)
            {
                // Hide page content — only show the shell + sidebar buttons
                main.Find("Room")?.gameObject.SetActive(false);
                main.Find("Player")?.gameObject.SetActive(false);
                main.Find("Media")?.gameObject.SetActive(false);
                main.Find("Settings")?.gameObject.SetActive(false);
            }

            // Apply theme
            if (player.CustomProperties.TryGetValue(PROP_THEME, out object themeVal))
                ApplyThemeToPad(pad, (int)themeVal);

            // Set initial open/close state
            bool isOpen = player.CustomProperties.TryGetValue(PROP_OPEN, out object openVal) && (bool)openVal;
            SetRemotePadVisible(pad, isOpen);

            _remotePads[player.ActorNumber] = pad;
        }

        private void SetRemotePadVisible(GameObject pad, bool visible)
        {
            Transform main = pad.transform.Find("Main");
            if (main != null) main.gameObject.SetActive(visible);
        }

        private void DestroyRemotePad(int actorNumber)
        {
            if (_remotePads.TryGetValue(actorNumber, out GameObject pad))
            {
                if (pad != null) Destroy(pad);
                _remotePads.Remove(actorNumber);
            }
        }

        private void ApplyThemeToPad(GameObject pad, int themeIndex)
        {
            int idx = themeIndex % Tablet.Themes.Length;
            var theme = Tablet.Themes[idx];

            Transform main = pad.transform.Find("Main");
            if (main == null) return;

            Renderer bgRenderer  = main.Find("Background")?.GetComponent<Renderer>();
            if (bgRenderer  != null) bgRenderer.material.color  = theme.bg;

            Renderer btnRenderer = main.Find("Sidebar/Room")?.GetComponent<Renderer>();
            if (btnRenderer != null) btnRenderer.material.color = theme.btn;
        }

        private VRRig GetRigForPlayer(Player player)
        {
            foreach (VRRig rig in FindObjectsOfType<VRRig>())
            {
                try
                {
                    if (rig.Creator != null && rig.Creator.ActorNumber == player.ActorNumber)
                        return rig;
                }
                catch { }
            }
            return null;
        }
    }
}
