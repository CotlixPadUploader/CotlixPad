using HarmonyLib;
using ColtixPad.Utilities;

namespace ColtixPad.Patches
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
    public class PrioritizeVoicePatch
    {
        public static VRRig prioritizedRig;

        private static void Postfix(VRRig __instance)
        {
            if ( __instance.voiceAudio == null)
                return;

            if (prioritizedRig != null && !prioritizedRig.Active())
                prioritizedRig = null;

            __instance.voiceAudio.volume = (prioritizedRig != null && prioritizedRig == __instance) ? 1f : 0.3f;
        }
    }
}