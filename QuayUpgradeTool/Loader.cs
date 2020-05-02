using ColossalFramework;
using ICities;
using UnityEngine;

namespace QuayUpgradeTool
{
    /// <summary>
    ///     Mod's launcher.
    /// </summary>
    public class Loader : LoadingExtensionBase
    {

        public override void OnCreated(ILoading loading)

        {

            // Set current game mode, we can't load some stuff if we're not in game (e.g. Map Editor)
            QuayUpgradeTool.IsInGameMode = loading.currentMode == AppMode.Game;

        }

        public override void OnReleased()
        {
            Object.DestroyImmediate(Singleton<QuayUpgradeTool>.instance);
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (!Singleton<QuayUpgradeTool>.exists)
                Singleton<QuayUpgradeTool>.Ensure();
            else
                Singleton<QuayUpgradeTool>.instance.Start();
        }
    }
}