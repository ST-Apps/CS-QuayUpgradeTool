using ColossalFramework;
using ColossalFramework.Math;
using QuayUpgradeTool.Redirection;
using UnityEngine;

namespace QuayUpgradeTool.Detours
{
    [TargetType(typeof(QuayAI))]
    // ReSharper disable once UnusedMember.Global
    public class QuayAIDetour : NetAI
    {

        /// <summary>
        /// This detour is only used to prevent <see cref="ToolBase.ToolErrors.InvalidShape"/> errors during upgrade.
        /// </summary>
        /// <param name="test"></param>
        /// <param name="visualize"></param>
        /// <param name="overlay"></param>
        /// <param name="autofix"></param>
        /// <param name="startPoint"></param>
        /// <param name="middlePoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="ownerBuilding"></param>
        /// <param name="ownerPosition"></param>
        /// <param name="ownerDirection"></param>
        /// <param name="productionRate"></param>
        /// <returns></returns>
        [RedirectMethod]
        public override ToolBase.ToolErrors CheckBuildPosition(bool test, bool visualize, bool overlay, bool autofix, ref NetTool.ControlPoint startPoint, ref NetTool.ControlPoint middlePoint, ref NetTool.ControlPoint endPoint, out BuildingInfo ownerBuilding, out Vector3 ownerPosition, out Vector3 ownerDirection, out int productionRate)
        {
            var toolErrors = base.CheckBuildPosition(test, visualize, overlay, autofix, ref startPoint, ref middlePoint, ref endPoint, out ownerBuilding, out ownerPosition, out ownerDirection, out productionRate);

            // HACK - we remove InvalidShape ToolError error to allow updates
            return Singleton<QuayUpgradeTool>.instance.IsToolActive ? toolErrors & ~ToolBase.ToolErrors.InvalidShape : toolErrors;
        }
    }
}