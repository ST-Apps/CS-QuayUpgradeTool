using ICities;

namespace QuayUpgradeTool
{
    public class ModInfo : IUserMod
    {
        private const string Version = "0.1.2";
#if DEBUG
        private const string Branch = "dev";
        public static readonly string ModName = $"[BETA] Quay Upgrade Tool {Version}-{Branch}";
#else
        public static readonly string ModName = $"Quay Road Tool {Version}";
#endif

        public string Name => ModName;

        public string Description => "This mod allows players to upgrade and reverse quays.";
    }
}