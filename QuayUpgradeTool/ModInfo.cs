﻿using System;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;

namespace QuayUpgradeTool
{
    public class ModInfo : IUserMod
    {
        public const string Version = "0.0.1";
        public const string Branch = "master";

        public ModInfo()
        {
            try
            {
                // Creating setting file
                GameSettings.AddSettingsFile(new SettingsFile {fileName = QuayUpgradeTool.SettingsFileName});
            }
            catch (Exception e)
            {
                DebugUtils.Log("Could not load/create the setting file.");
                DebugUtils.LogException(e);
            }
        }

#if DEBUG
        public string Name => $"[BETA] Parallel Road Tool {Version} b-{Branch}";
#else
        public string Name => $"Parallel Road Tool {Version} b-{Branch}";
#endif

        public string Description =>
            "This mod allows players to easily draw parallel roads in Cities: Skylines. ";

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                var group = helper.AddGroup(Name) as UIHelper;
                var panel = group.self as UIPanel;

                panel.gameObject.AddComponent<OptionsKeymapping>();

                group.AddSpace(10);
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                DebugUtils.LogException(e);
            }
        }
    }
}