using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColtixPad.Classes
{
    public class Button2D : MonoBehaviour
    {
        public event Action OnClick;

        // Static registry — no FindObjectsByType every frame
        private static readonly List<Button2D> allButtons = new List<Button2D>();
        private static Button2D masterButton;

        private static GameObject cursorObject;
        private static TMPro.TextMeshPro cursorText;
        private static float clickCooldown;
        private static AudioClip buttonSound;

        void Start()
        {
            gameObject.layer = 18;
            allButtons.Add(this);
            if (masterButton == null) masterButton = this;
            if (cursorObject == null) CreateCursor();
        }

        void OnDestroy()
        {
            allButtons.Remove(this);
            if (masterButton == this)
            {
                masterButton = null;
                foreach (var b in allButtons)
                {
                    if (b != null && b.isActiveAndEnabled) { masterButton = b; break; }
                }
                if (cursorObject != null) cursorObject.SetActive(false);
            }
        }

        private static void CreateCursor()
        {
            cursorObject = new GameObject("ColtixPad_2DCursor");
            cursorText = cursorObject.AddComponent<TMPro.TextMeshPro>();
            cursorText.text = "<b>×</b>";
            cursorText.fontSize = 0.12f;
            cursorText.alignment = TMPro.TextAlignmentOptions.Center;
            cursorText.color = Color.white;
            cursorObject.SetActive(false);
        }

        void Update()
        {
            if (masterButton != this) return;
            if (cursorObject == null) CreateCursor();

            Transform hand = GorillaTagger.Instance.rightHandTransform;
            Vector3 origin = hand.position;
            Vector3 direction = hand.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit rayHit, 5f, 1 << 18))
            {
                Button2D btn = rayHit.collider.GetComponent<Button2D>();
                if (btn != null)
                {
                    cursorObject.SetActive(true);
                    cursorObject.transform.position = rayHit.point - direction * 0.01f;
                    cursorObject.transform.rotation = Quaternion.LookRotation(-direction);
                    cursorText.color = new Color(1f, 0.4f, 0.4f, 1f);

                    bool trigger = ControllerInputPoller.TriggerFloat(UnityEngine.XR.XRNode.RightHand) > 0.7f;
                    if (trigger && Time.time > clickCooldown)
                    {
                        clickCooldown = Time.time + 0.3f;
                        GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength / 2f, GorillaTagger.Instance.tagHapticDuration / 2f);
                        buttonSound ??= Utilities.Assets.LoadAsset<AudioClip>("click");
                        if (buttonSound != null)
                        {
                            AudioSource audioSource = VRRig.LocalRig.rightHandPlayer;
                            audioSource.volume = 0.3f;
                            audioSource.PlayOneShot(buttonSound);
                        }
                        btn.OnClick?.Invoke();
                    }
                    return;
                }
            }

            cursorObject.SetActive(false);
        }
    }
}
