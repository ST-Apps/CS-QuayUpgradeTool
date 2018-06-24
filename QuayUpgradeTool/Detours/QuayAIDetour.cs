using ColossalFramework;
using ColossalFramework.Math;
using QuayUpgradeTool.Redirection;
using UnityEngine;

namespace QuayUpgradeTool.Detours
{
    [TargetType(typeof(QuayAI))]
    public class QuayAIDetour : NetAI
    {
        /// <summary>
        /// <see cref="https://github.com/bloodypenguin/Skylines-QuayAnarchy/blob/master/QuayAnarchy/Detours/QuayAIDetour.cs"/>
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
            ToolBase.ToolErrors toolErrors = base.CheckBuildPosition(test, visualize, overlay, autofix, ref startPoint, ref middlePoint, ref endPoint, out ownerBuilding, out ownerPosition, out ownerDirection, out productionRate);
            // TODO: rimuovere fino alla return
            NetManager instance1 = Singleton<NetManager>.instance;
            TerrainManager instance2 = Singleton<TerrainManager>.instance;
            if ((int)startPoint.m_node != 0)
            {
                NetInfo info = instance1.m_nodes.m_buffer[(int)startPoint.m_node].Info;
                if (info.m_class.m_subService != this.m_info.m_class.m_subService || info.m_class.m_level != this.m_info.m_class.m_level)
                {
                    toolErrors |= ToolBase.ToolErrors.InvalidShape;
                }
                else
                {
                    float y = instance1.m_nodes.m_buffer[(int)startPoint.m_node].m_position.y;
                    startPoint.m_elevation = y - startPoint.m_position.y;
                    startPoint.m_position.y = y;
                    if (instance1.m_nodes.m_buffer[(int)startPoint.m_node].CountSegments() > 1)
                        toolErrors |= ToolBase.ToolErrors.InvalidShape;
                }
            }
            else if ((int)startPoint.m_segment != 0)
            {
                toolErrors |= ToolBase.ToolErrors.InvalidShape;
            }
            else
            {
                float y = startPoint.m_position.y;
                Vector3 vector3 = new Vector3(middlePoint.m_direction.z, 0.0f, -middlePoint.m_direction.x);
                float num = Mathf.Max(Mathf.Max(y, Singleton<TerrainManager>.instance.SampleRawHeightSmooth(startPoint.m_position + vector3 * (this.m_info.m_halfWidth + 8f))), Singleton<TerrainManager>.instance.SampleRawHeightSmooth(startPoint.m_position - vector3 * (this.m_info.m_halfWidth + 8f)));
                startPoint.m_elevation = num - startPoint.m_position.y;
                startPoint.m_position.y = num;
            }
            if ((int)endPoint.m_node != 0)
            {
                NetInfo info = instance1.m_nodes.m_buffer[(int)endPoint.m_node].Info;
                if (info.m_class.m_subService != this.m_info.m_class.m_subService || info.m_class.m_level != this.m_info.m_class.m_level)
                {
                    toolErrors |= ToolBase.ToolErrors.InvalidShape;
                }
                else
                {
                    float y = instance1.m_nodes.m_buffer[(int)endPoint.m_node].m_position.y;
                    endPoint.m_elevation = y - endPoint.m_position.y;
                    endPoint.m_position.y = y;
                    if (instance1.m_nodes.m_buffer[(int)endPoint.m_node].CountSegments() > 1)
                        toolErrors |= ToolBase.ToolErrors.InvalidShape;
                }
            }
            else if ((int)endPoint.m_segment != 0)
            {
                toolErrors |= ToolBase.ToolErrors.InvalidShape;
            }
            else
            {
                float y = endPoint.m_position.y;
                Vector3 vector3 = new Vector3(endPoint.m_direction.z, 0.0f, -endPoint.m_direction.x);
                float num = Mathf.Max(Mathf.Max(y, Singleton<TerrainManager>.instance.SampleRawHeightSmooth(endPoint.m_position + vector3 * (this.m_info.m_halfWidth + 8f))), Singleton<TerrainManager>.instance.SampleRawHeightSmooth(endPoint.m_position - vector3 * (this.m_info.m_halfWidth + 8f)));
                endPoint.m_elevation = num - endPoint.m_position.y;
                endPoint.m_position.y = num;
            }
            middlePoint.m_elevation = (float)(((double)startPoint.m_elevation + (double)endPoint.m_elevation) * 0.5);
            middlePoint.m_position.y = (float)(((double)startPoint.m_position.y + (double)endPoint.m_position.y) * 0.5);
            Vector3 middlePos1;
            Vector3 middlePos2;
            NetSegment.CalculateMiddlePoints(startPoint.m_position, middlePoint.m_direction, endPoint.m_position, -endPoint.m_direction, true, true, out middlePos1, out middlePos2);
            Bezier2 bezier2;
            bezier2.a = VectorUtils.XZ(startPoint.m_position);
            bezier2.b = VectorUtils.XZ(middlePos1);
            bezier2.c = VectorUtils.XZ(middlePos2);
            bezier2.d = VectorUtils.XZ(endPoint.m_position);
            int num1 = Mathf.CeilToInt(Vector2.Distance(bezier2.a, bezier2.d) * 0.005f) + 3;
            Vector2 vector2_1 = new Vector2(middlePoint.m_direction.z, -middlePoint.m_direction.x);
            Segment2 segment1;
            segment1.a = bezier2.a + vector2_1 * this.m_info.m_halfWidth;
            Segment2 segment2;
            segment2.a = bezier2.a - vector2_1 * this.m_info.m_halfWidth;
            //begin mod
            //end mod
            for (int index = 1; index <= num1; ++index)
            {
                Vector2 vector2_2 = bezier2.Position((float)index / (float)num1);
                Vector2 vector2_3 = bezier2.Tangent((float)index / (float)num1);
                vector2_3 = new Vector2(vector2_3.y, -vector2_3.x).normalized;
                segment1.b = vector2_2 + vector2_3 * this.m_info.m_halfWidth;
                segment2.b = vector2_2 - vector2_3 * this.m_info.m_halfWidth;
                //begin mod
                //end mod
                segment1.a = segment1.b;
                segment2.a = segment2.b;
            }
            //begin mod
            //end mod
            // HACK - we remove any InvalidShape error to allow updates
            return toolErrors & ~ToolBase.ToolErrors.InvalidShape;
        }

        [RedirectMethod]
        public override NetInfo GetInfo(float minElevation, float maxElevation, float length, bool incoming, bool outgoing, bool curved, bool enableDouble, ref ToolBase.ToolErrors errors)
        {
            //begin mod
            //end mod
            return this.m_info;
        }
    }
}