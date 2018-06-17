using System;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;

namespace QuayUpgradeTool
{
    public class ModInfo : IUserMod
    {
        public const string Version = "0.0.1";
        public const string Branch = "dev";

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
        public string Name => $"[BETA] Quay Upgrade Tool {Version} b-{Branch}";
#else
        public string Name => $"Quay Upgrade Tool {Version} b-{Branch}";
#endif

        public string Description =>
            "This mod allows players to upgrade quays in Cities: Skylines.";

    }
}