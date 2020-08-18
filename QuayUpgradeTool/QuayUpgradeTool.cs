using System;
using ColossalFramework;
using ColossalFramework.UI;
using CSUtil.Commons;
using QuayUpgradeTool.Redirection;
using UnityEngine;

namespace QuayUpgradeTool
{
    public class QuayUpgradeTool : ToolBase
    {
        private bool _canUpdate;

        public bool IsToolActive => _canUpdate && ToolsModifierControl.toolController.CurrentTool is NetTool;

        #region Unity

        protected override void OnEnable()
        {
            Log._Debug($"[{nameof(QuayUpgradeTool)}.{nameof(OnEnable)}] Enabling redirects.");

            RedirectionUtil.Redirect();
        }

        protected override void OnDisable()
        {
            Log._Debug($"[{nameof(QuayUpgradeTool)}.{nameof(OnEnable)}] Disabling redirects.");

            RedirectionUtil.RevertRedirects();
        }

        #endregion

        #region ToolBase

        private Ray m_mouseRay;
        private float m_mouseRayLength;
        private bool m_mouseRayValid;

        private ushort _currentSegmentId;

        protected override void OnToolLateUpdate()
        {
            if (!enabled) return;

            m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            m_mouseRayLength = Camera.main.farClipPlane;
            m_mouseRayValid = /*!this.m_toolController.IsInsideUI &&*/ Cursor.visible;

            if (ToolsModifierControl.toolController.CurrentTool is NetTool)
                SimulationStep();
        }

        public override void SimulationStep()
        {
            if (!enabled) return;

            base.SimulationStep();

            var currentInfo = ToolsModifierControl.GetTool<NetTool>().m_prefab;

            if (currentInfo == null || !(currentInfo.m_netAI is QuayAI))
            {
                // Prevent executing when we're not in Quay mode
                Log._Debug($"[{nameof(QuayUpgradeTool)}.{nameof(SimulationStep)}] Skipping because segment is not a quay ({currentInfo?.m_netAI})");

                _canUpdate = false;
                return;
            }

            var input = new RaycastInput(m_mouseRay, m_mouseRayLength)
            {
                m_buildObject = currentInfo,
                m_netService = new RaycastService(currentInfo.m_class.m_service, currentInfo.m_class.m_subService,
                    currentInfo.m_class.m_layer),
                m_ignoreTerrain = true,
                m_ignoreNodeFlags = NetNode.Flags.All,
                m_ignoreSegmentFlags = NetSegment.Flags.Untouchable
            };

            if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Transport ||
                Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Traffic ||
                Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.TrafficRoutes ||
                Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Tours ||
                Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Underground &&
                Singleton<InfoManager>.instance.CurrentSubMode == InfoManager.SubInfoMode.Default)
                input.m_netService.m_itemLayers |= ItemClass.Layer.MetroTunnels;

            if (!m_mouseRayValid || !RayCast(input, out var output))
            {
                // We're not on a valid segment so we can't update
                _canUpdate = false;
                return;
            }

            if (output.m_netSegment == _currentSegmentId)
            {
                // Same segment as before, no need to go on
                _canUpdate = true;
                return;
            }

            _currentSegmentId = output.m_netSegment;

            if (_currentSegmentId == 0)
                _currentSegmentId = DefaultTool.FindSecondarySegment(output.m_netSegment);

            _canUpdate = true;

            Log._Debug($"[{nameof(QuayUpgradeTool)}.{nameof(SimulationStep)}] Ray-cast detected a new segment with id {_currentSegmentId}");
        }

        protected override void OnToolGUI(Event e)
        {
            if (!enabled) return;

            if (!IsToolActive
                || e.type != EventType.MouseDown
                || e.button != 1
                || _currentSegmentId == 0) return;

            try
            {
                // Right click means that we need to invert direction                
                var segment = NetManager.instance.m_segments.m_buffer[_currentSegmentId];
                var invert = segment.m_flags.IsFlagSet(NetSegment.Flags.Invert);
                var startNode = segment.m_startNode;
                var startDirection = segment.m_startDirection;
                var endNode = segment.m_endNode;
                var endDirection = segment.m_endDirection;
                var infos = segment.Info;
                var buildIndex = segment.m_buildIndex;
                var modifiedIndex = segment.m_modifiedIndex;

                if (!invert)
                {
                    // If invert is false, we only need to set it to true to reverse segment's direction ...
                    NetManager.instance.m_segments.m_buffer[_currentSegmentId].m_flags |= NetSegment.Flags.Invert;
                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        NetManager.instance.UpdateSegment(_currentSegmentId);
                    });
                }
                else
                {
                    // ...however it doesn't work the other way around. If invert is true and we set it to false, it'll still be true.
                    // This means that we're forced to redraw the segment from scratch, reversing directions.
                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        NetManager.instance.ReleaseSegment(_currentSegmentId, true);

                        NetManager.instance.CreateSegment(out _,
                            ref Singleton<SimulationManager>.instance.m_randomizer,
                            infos, startNode, endNode, startDirection, endDirection, buildIndex, modifiedIndex, true);
                    });
                }

                Log.Info(
                    $"[{nameof(QuayUpgradeTool)}.{nameof(OnToolGUI)}] Inverted segment {_currentSegmentId} ({nameof(invert)} is {invert})");
            }
            catch (Exception ex)
            {
                Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(OnToolGUI)}] Failed for segment {_currentSegmentId}");
                Log.Exception(ex);
            }
        }

        #endregion
    }
}