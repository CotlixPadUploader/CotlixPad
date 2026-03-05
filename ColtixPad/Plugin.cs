using BepInEx;
using ColtixPad.Classes;
using ColtixPad.Patches;
using ColtixPad.Utilities;
using UnityEngine;

namespace ColtixPad
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Configuration Configuration;

        void Awake()
        {
            GameObject loader = new GameObject("ColtixPad");
            DontDestroyOnLoad(loader);

            Configuration = new Configuration(Config);
            PatchHandler.PatchAll();
            loader.AddComponent<Handler>();
            loader.AddComponent<TrackerClient>();
        }
    }
}
