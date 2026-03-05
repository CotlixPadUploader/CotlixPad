using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ColtixPad.Classes
{
    /// <summary>
    /// Monitors players in the current room and attaches a gold crown to the back
    /// of the ColtixPad owner's tablet whenever they are present.
    /// The owner identity is resolved via <see cref="TrackerClient"/> which polls
    /// GET http://localhost:3000/api/owner for { "username": "...", "userId": "..." }.
    /// </summary>
    public class OwnerCrown : MonoBehaviour
    {
        public static OwnerCrown Instance { get; private set; }

        // Tracks the crown GameObject keyed by the owner's VRRig
        private readonly Dictionary<VRRig, GameObject> _crowns = new Dictionary<VRRig, GameObject>();

        // How often (seconds) to scan the room for the owner
        private const float SCAN_INTERVAL = 2f;
        private float _nextScan;

        // Whether we have already sent the "owner joined" notification this session
        private bool _ownerNotified;

        void Awake() => Instance = this;

        void Update()
        {
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + SCAN_INTERVAL;

            try { ScanRoom(); }
            catch { /* never crash the update loop */ }
        }

        private void ScanRoom()
        {
            bool inRoom = NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom;

            // If not in a room, clean up any leftover crowns
            if (!inRoom)
            {
                RemoveAllCrowns();
                _ownerNotified = false;
                return;
            }

            // Scan all active remote rigs
            foreach (VRRig rig in GorillaParent.instance.vrrigs)
            {
                if (rig == null || rig.isLocal) continue;

                if (TrackerClient.IsOwner(rig))
                {
                    if (!_crowns.ContainsKey(rig))
                    {
                        AttachCrown(rig);

                        if (!_ownerNotified)
                        {
                            _ownerNotified = true;
                            Notifications.SendNotification(
                                $"<color=gold>👑 {rig.playerNameVisible} (ColtixPad owner) is in the room!</color>",
                                8000);
                        }
                    }
                }
                else
                {
                    // Player is no longer identified as owner — remove crown if it exists
                    TryRemoveCrown(rig);
                }
            }

            // Prune crowns whose rigs have left or gone inactive
            List<VRRig> stale = new List<VRRig>();
            foreach (var kvp in _crowns)
            {
                if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy)
                    stale.Add(kvp.Key);
            }
            foreach (VRRig r in stale) TryRemoveCrown(r);

            // Reset notification flag if owner is no longer present
            if (!OwnerIsPresent()) _ownerNotified = false;
        }

        // ──────────────────────────────────────────────────────────────
        //  Crown construction
        // ──────────────────────────────────────────────────────────────

        private void AttachCrown(VRRig rig)
        {
            // Root object — parented to the owner's left hand so it tracks naturally
            GameObject crown = new GameObject("ColtixPad_OwnerCrown");
            crown.transform.SetParent(rig.leftHandTransform, false);

            // Position it on the BACK of the ColtixPad:
            // X=0 (centred), Y=+0.06 (slightly above), Z=-0.12 (behind the palm)
            // The 180° Y rotation makes it face away from the palm (i.e. readable from outside)
            crown.transform.localPosition = new Vector3(0f, 0.06f, -0.12f);
            crown.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            crown.transform.localScale    = Vector3.one * 0.018f;

            // ── Crown emoji label ──────────────────────────────────────
            TextMeshPro label = crown.AddComponent<TextMeshPro>();
            label.text      = "👑";
            label.fontSize  = 40f;
            label.alignment = TextAlignmentOptions.Center;
            label.color     = new Color32(255, 215, 0, 255); // gold
            label.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");

            // ── "Owner" sub-label ──────────────────────────────────────
            GameObject subLabelObj = new GameObject("ColtixPad_OwnerLabel");
            subLabelObj.transform.SetParent(crown.transform, false);
            subLabelObj.transform.localPosition = new Vector3(0f, -2.4f, 0f);
            subLabelObj.transform.localScale    = Vector3.one;

            TextMeshPro subLabel = subLabelObj.AddComponent<TextMeshPro>();
            subLabel.text      = "Owner";
            subLabel.fontSize  = 18f;
            subLabel.alignment = TextAlignmentOptions.Center;
            subLabel.color     = new Color32(255, 215, 0, 200); // semi-transparent gold
            subLabel.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");

            _crowns[rig] = crown;
        }

        // ──────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────

        private void TryRemoveCrown(VRRig rig)
        {
            if (!_crowns.TryGetValue(rig, out GameObject obj)) return;
            if (obj != null) Destroy(obj);
            _crowns.Remove(rig);
        }

        private void RemoveAllCrowns()
        {
            foreach (var kvp in _crowns)
                if (kvp.Value != null) Destroy(kvp.Value);
            _crowns.Clear();
        }

        private bool OwnerIsPresent()
        {
            foreach (var kvp in _crowns)
                if (kvp.Key != null && kvp.Key.gameObject.activeInHierarchy) return true;
            return false;
        }

        void OnDestroy() => RemoveAllCrowns();
    }
}
