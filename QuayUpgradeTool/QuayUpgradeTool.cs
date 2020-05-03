using System;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using CSUtil.Commons;
using QuayUpgradeTool.Redirection;
using QuayUpgradeTool.UI.Base;
using UnityEngine;

namespace QuayUpgradeTool
{
    public class QuayUpgradeTool : ToolBase
    {
        private readonly object _lock = new object();

        private bool _canUpdate;
        private bool _isBeautificationOn;
        private UIComponent _quayOptionsPanel;
        private UITabstrip _toolModeBar;
        private UIButton _toolToggleButton;

        public bool IsToolActive => _canUpdate && ToolsModifierControl.toolController.CurrentTool is NetTool;

        public static bool IsInGameMode { get; set; }

        #region Handlers

        private void _toolModeBar_eventSelectedIndexChanged(UIComponent component, int value)
        {
            Log._Debug(
                $"[{nameof(QuayUpgradeTool)}.{nameof(_toolModeBar_eventSelectedIndexChanged)}] Selected index {value}");
        }

        #endregion

        #region Unity

        public void Start()
        {
            try
            {
                if (!IsInGameMode)
                {
                    // Quays are not supported in map editor so we don't have to start
                    Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Current mode is not game, aborting...");
                    return;
                }

                // Find NetTool and deploy
                if (ToolsModifierControl.GetTool<NetTool>() == null)
                {
                    Log.Warning($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Net Tool not found, can't deploy!");
                    enabled = false;
                    return;
                }

                Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Loading version: {ModInfo.ModName}");
                Log._Debug($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Adding UI components");

                var tsBar = UIUtil.FindComponent<UISlicedSprite>("TSBar", null, UIUtil.FindOptions.NameContains);
                if (tsBar == null)
                {
                    Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Couldn't find TSBar, aborting...");
                    return;
                }

                var optionsBar = UIUtil.FindComponent<UIPanel>("OptionsBar", tsBar, UIUtil.FindOptions.NameContains);
                _quayOptionsPanel = optionsBar.Find<UIPanel>("QuaysOptionPanel");
                _toolModeBar = _quayOptionsPanel.Find<UITabstrip>("ToolMode");

                var modes = _quayOptionsPanel.GetComponent<OptionPanelBase>();
                ((RoadsOptionPanel) modes).m_Modes =
                    ((RoadsOptionPanel) modes).m_Modes.Union(new[] {NetTool.Mode.Upgrade}).ToArray();

                _toolToggleButton = _toolModeBar.AddTab("Upgrade", false);
                UIUtil.SetTextures(_toolToggleButton, "RoadOptionUpgrade", "Quay Upgrade Tool");

                RedirectionUtil.Redirect();

                Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Loaded");
            }
            catch (Exception e)
            {
                Log._DebugOnlyError($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Loading failed");
                Log.Exception(e);

                enabled = false;
            }
        }

        protected override void OnDestroy()
        {
            RedirectionUtil.RevertRedirects();

            _toolModeBar.eventSelectedIndexChanged -= _toolModeBar_eventSelectedIndexChanged;
            _isBeautificationOn = false;

            DestroyImmediate(_quayOptionsPanel);
            DestroyImmediate(_toolModeBar);
            DestroyImmediate(_toolToggleButton);

            _quayOptionsPanel = null;
            _toolModeBar = null;
            _toolToggleButton = null;
        }

        protected void Update()
        {
            if (_isBeautificationOn || !(ToolsModifierControl.toolController.CurrentTool is NetTool)) return;

            // Check if a new panel was added
            var optionsPanel = UIUtil.FindComponent<UIPanel>("QuaysOptionPanel(BeautificationPanel)", null,
                UIUtil.FindOptions.NameContains);
            if (optionsPanel == null) return;

            lock (_lock)
            {
                if (_isBeautificationOn) return;

                Log.Info(
                    $"[{nameof(QuayUpgradeTool)}.{nameof(OnToolLateUpdate)}] Updating options panel with the new one...");

                // Replace the previous one and rebuild the UI
                _quayOptionsPanel = optionsPanel;
                _toolModeBar = _quayOptionsPanel.Find<UITabstrip>("ToolMode");
                _toolModeBar.eventSelectedIndexChanged += _toolModeBar_eventSelectedIndexChanged;

                _isBeautificationOn = true;
            }
        }

        #endregion

        #region ToolBase

        private Ray m_mouseRay;
        private float m_mouseRayLength;
        private bool m_mouseRayValid;

        private ushort _currentSegmentId;

        protected override void OnToolLateUpdate()
        {
            base.OnToolLateUpdate();

            m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            m_mouseRayLength = Camera.main.farClipPlane;
            m_mouseRayValid = /*!this.m_toolController.IsInsideUI &&*/ Cursor.visible;

            if (ToolsModifierControl.toolController.CurrentTool is NetTool)
                SimulationStep();
        }

        public override void SimulationStep()
        {
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