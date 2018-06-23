using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using QuayUpgradeTool.Redirection;
using UnityEngine;

namespace QuayUpgradeTool.Detours
{
    /// <summary>
    ///     Mod's core class, it executes the detour to intercept segment's creation.
    /// </summary>
    public struct QuayAIDetour
    {
        #region Detour
        private static readonly MethodInfo From = typeof(QuayAI).GetMethod("CheckBuildPosition",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly MethodInfo To =
            typeof(QuayAIDetour).GetMethod("CheckBuildPosition", BindingFlags.NonPublic | BindingFlags.Instance);

        private static RedirectCallsState _state;
        private static bool _deployed;

        public static void Deploy()
        {
            DebugUtils.Log($"Deploying with from: {From} and to: {To}");
            if (_deployed) return;
            _state = RedirectionHelper.RedirectCalls(From, To);
            _deployed = true;
        }

        public static void Revert()
        {
            if (!_deployed) return;
            RedirectionHelper.RevertRedirect(From, _state);
            _deployed = false;
        }
        #endregion

        #region Utility

        /// <summary>
        ///     This methods skips our detour by calling the original method from the game, getting the real ToolError result.
        /// </summary>
        /// <returns></returns>
        private static ToolBase.ToolErrors CheckBuildPositionOriginal(bool test, bool visualize, bool overlay, bool autofix, ref NetTool.ControlPoint startPoint, ref NetTool.ControlPoint middlePoint, ref NetTool.ControlPoint endPoint, out BuildingInfo ownerBuilding, out Vector3 ownerPosition, out Vector3 ownerDirection, out int productionRate)
        {
            Revert();

            DebugUtils.Log($"CheckBuildPositionOriginal({test}, {visualize}, {overlay}, {autofix}, {startPoint}, {middlePoint}, {endPoint})");

            var result = Singleton<QuayAI>.instance.CheckBuildPosition(test, visualize, overlay, autofix, ref startPoint, ref middlePoint, ref endPoint, out ownerBuilding, out ownerPosition, out ownerDirection, out productionRate);

            Deploy();

            return result;
        }

        #endregion

        /// <summary>
        ///     Mod's core.
        ///     We override the returned ToolError with None, so that any upgrade can be possible.
        /// </summary>

        /// <returns></returns>
        private ToolBase.ToolErrors CheckBuildPosition(bool test, bool visualize, bool overlay, bool autofix, ref NetTool.ControlPoint startPoint, ref NetTool.ControlPoint middlePoint, ref NetTool.ControlPoint endPoint, out BuildingInfo ownerBuilding, out Vector3 ownerPosition, out Vector3 ownerDirection, out int productionRate)
        {
            ownerBuilding = new BuildingInfo();
            ownerDirection = new Vector3();
            ownerPosition = new Vector3();

            var result = CheckBuildPositionOriginal(test, visualize, overlay, autofix, ref startPoint, ref middlePoint, ref endPoint, out ownerBuilding, out ownerPosition, out ownerDirection, out productionRate);

            DebugUtils.Log($"CheckBuildPosition returned {result}, we'll return {result & ~ToolBase.ToolErrors.InvalidShape}");

            return result & ~ToolBase.ToolErrors.InvalidShape;
        }
    }
}