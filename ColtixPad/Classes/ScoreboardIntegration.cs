using GorillaNetworking;
using ColtixPad.Pages;
using System.Collections.Generic;
using UnityEngine;

namespace ColtixPad.Classes
{
    // Attach this to the Handler GameObject — it watches the scoreboard
    // and adds an X click button next to each player name
    public class ScoreboardIntegration : MonoBehaviour
    {
        private readonly List<GameObject> xButtons = new List<GameObject>();
        private float checkInterval = 3f; // check every 3s not every 1s
        private float nextCheck;
        private int lastPlayerCount = 0;

        void Update()
        {
            if (GorillaScoreboardTotalUpdater.allScoreboardLines == null) return;

            int currentCount = GorillaScoreboardTotalUpdater.allScoreboardLines.Count;

            // Refresh if player count changed OR on interval
            bool countChanged = currentCount != lastPlayerCount;
            if (!countChanged && Time.time < nextCheck) return;

            nextCheck = Time.time + checkInterval;
            lastPlayerCount = currentCount;

            RefreshScoreboardButtons();
        }

        private void RefreshScoreboardButtons()
        {
            // Clean up old buttons whose lines no longer exist
            xButtons.RemoveAll(b => b == null);

            if (GorillaScoreboardTotalUpdater.allScoreboardLines == null) return;

            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line == null || line.linePlayer == null) continue;

                Transform lineTransform = line.transform;

                // Skip if we already added a button to this line
                if (lineTransform.Find("ColtixPad_XButton") != null) continue;

                // Create a small flat quad as the X button
                GameObject xBtn = GameObject.CreatePrimitive(PrimitiveType.Quad);
                xBtn.name = "ColtixPad_XButton";
                xBtn.transform.SetParent(lineTransform, false);
                xBtn.transform.localPosition = new Vector3(-0.45f, 0f, -0.01f);
                xBtn.transform.localRotation = Quaternion.identity;
                xBtn.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

                // Style it — use a bright X label
                Renderer rend = xBtn.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("GUI/Text Shader"));
                mat.color = new Color(1f, 0.3f, 0.3f, 0.9f);
                rend.material = mat;

                // Add the X label
                GameObject label = new GameObject("XLabel");
                label.transform.SetParent(xBtn.transform, false);
                label.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                label.transform.localScale = new Vector3(12f, 12f, 12f);
                TMPro.TextMeshPro tmp = label.AddComponent<TMPro.TextMeshPro>();
                tmp.text = "<b>×</b>";
                tmp.fontSize = 1f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = Color.white;

                // Store the player ref for when clicked
                NetPlayer netPlayer = line.linePlayer;

                // Add 2D click handler
                Button2D btn = xBtn.AddComponent<Button2D>();
                btn.OnClick += () =>
                {
                    // Find the VRRig for this player
                    foreach (VRRig rig in GorillaParent.instance.vrrigs)
                    {
                        if (rig != null && !rig.isLocal && rig.Creator == netPlayer)
                        {
                            Player.RequestTarget(rig);
                            Tablet.Instance.CurrentPage = Tablet.Page.Player;
                            break;
                        }
                    }
                };

                xButtons.Add(xBtn);
            }
        }

        void OnDestroy()
        {
            foreach (var btn in xButtons)
                if (btn != null) Destroy(btn);
            xButtons.Clear();
        }
    }
}
