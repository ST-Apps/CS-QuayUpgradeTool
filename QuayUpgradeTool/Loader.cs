﻿using ColossalFramework;
using ICities;
using UnityEngine;

namespace QuayUpgradeTool
{
    /// <summary>
    ///     Mod's launcher.
    /// </summary>
    public class Loader : LoadingExtensionBase
    {

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