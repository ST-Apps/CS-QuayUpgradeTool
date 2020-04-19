﻿using System;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using QuayUpgradeTool.Redirection;
using QuayUpgradeTool.UI;
using UnityEngine;

namespace QuayUpgradeTool
{
    /// <summary>
    ///     Mod's "launcher" class.
    ///     It also acts as a "controller" to connect the mod with its UI.
    ///     TODO: Drag & drop is not as smooth as before.
    /// </summary>
    public class QuayUpgradeTool : ToolBase
    {
        public const string SettingsFileName = "QuayUpgradeTool";

        public static QuayUpgradeTool Instance;
        public static NetTool NetTool;
        public static QuayAI QuayAI;

        private bool _isToolActive;

        private UIMainWindow _mainWindow;

        private NetTool.Mode _previousMode;
        public bool IsLeftHandTraffic;

        public bool IsToolActive
        {
            get => _isToolActive && NetTool.enabled;

            private set
            {
                if (IsToolActive == value) return;
                if (value)
                {
                    DebugUtils.Log("Enabling quay upgrade support");
                    RedirectionUtil.Redirect();
                }
                else
                {
                    DebugUtils.Log("Disabling quay upgrade support");
                    RedirectionUtil.RevertRedirects();
                }

                _isToolActive = value;
            }
        }

        #region Handlers

        private void UnsubscribeToUIEvents()
        {
            _mainWindow.OnParallelToolToggled -= MainWindowOnOnParallelToolToggled;
        }

        private void SubscribeToUIEvents()
        {
            _mainWindow.OnParallelToolToggled += MainWindowOnOnParallelToolToggled;
        }

        private void MainWindowOnOnParallelToolToggled(UIComponent component, bool value)
        {
            IsToolActive = value;

            DebugUtils.Log($"Quay upgrade mode is set to {IsToolActive}");

            if (IsToolActive)
            {
                _previousMode = NetTool.m_mode;
                NetTool.m_mode = NetTool.Mode.Upgrade;
            }
            else
            {
                NetTool.m_mode = _previousMode;
            }
        }

        #endregion

        #region Unity

        public void Start()
        {
            // Find NetTool and deploy
            try
            {
                NetTool = FindObjectOfType<NetTool>();
                if (NetTool == null)
                {
                    DebugUtils.Log("Net Tool not found");
                    enabled = false;
                    return;
                }

                IsLeftHandTraffic = Singleton<SimulationManager>.instance.m_metaData.m_invertTraffic ==
                                    SimulationMetaData.MetaBool.True;

                // Main UI init
                var view = UIView.GetAView();
                _mainWindow = view.FindUIComponent<UIMainWindow>("QUT_MainWindow");
                if (_mainWindow != null)
                    Destroy(_mainWindow);

                DebugUtils.Log("Adding UI components");
                _mainWindow = view.AddUIComponent(typeof(UIMainWindow)) as UIMainWindow;

                SubscribeToUIEvents();

                DebugUtils.Log("Initialized");
            }
            catch (Exception e)
            {
                DebugUtils.Log("Start failed");
                DebugUtils.LogException(e);
                enabled = false;
            }
        }

        protected override void OnDestroy()
        {
            UnsubscribeToUIEvents();
            RedirectionUtil.RevertRedirects();
            IsToolActive = false;
        }

        private Ray m_mouseRay;
        private float m_mouseRayLength;
        private bool m_mouseRayValid;

        protected override void OnEnable()
        {
            // base.OnEnable();
        }

        #region ToolBase

        private ushort _currentSegmentId;

        protected override void OnToolLateUpdate()
        {
            DebugUtils.Log("OnToolLateUpdate");
            base.OnToolLateUpdate();

            m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            m_mouseRayLength = Camera.main.farClipPlane;
            m_mouseRayValid = /*!this.m_toolController.IsInsideUI &&*/ Cursor.visible;

            if (IsToolActive)
                SimulationStep();
        }

        public override void SimulationStep()
        {
            base.SimulationStep();

            var info1 = NetTool.m_prefab;
            if (info1 == null)
                return;

            var instance = Singleton<NetManager>.instance;
            var input = new RaycastInput(m_mouseRay, m_mouseRayLength)
            {
                m_buildObject = info1,
                m_netService = new RaycastService(info1.m_class.m_service, info1.m_class.m_subService,
                    info1.m_class.m_layer),
                m_ignoreTerrain = true,
                m_ignoreNodeFlags = NetNode.Flags.All,
                m_ignoreSegmentFlags = NetSegment.Flags.Untouchable
            };
            if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Transport ||
                Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Traffic ||
                Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.TrafficRoutes ||
                Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Tours)
                input.m_netService.m_itemLayers |= ItemClass.Layer.MetroTunnels;
            else if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Underground &&
                     Singleton<InfoManager>.instance.CurrentSubMode == InfoManager.SubInfoMode.Default)
                input.m_netService.m_itemLayers |= ItemClass.Layer.MetroTunnels;

            var raycastResult = RayCast(input, out var output);

            if (m_mouseRayValid && RayCast(input, out output))
            {
                var secondarySegment = DefaultTool.FindSecondarySegment(output.m_netSegment);
                _currentSegmentId = output.m_netSegment;
                if (_currentSegmentId == 0)
                    _currentSegmentId = secondarySegment;
            }
        }

        protected override void OnToolGUI(Event e)
        {
            if (IsToolActive && e.type == EventType.MouseDown && e.button == 1 && _currentSegmentId != 0)
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
                    NetManager.instance.UpdateSegment(_currentSegmentId);

                    // DebugUtils.Log($"Inverted {_currentSegmentId} with properties: [invert: {invert}, startNode: {startNode}, startDirection: {startDirection}, endNode: {endNode}, endDirection: {endDirection}]");
                    DebugUtils.Log(JsonUtility.ToJson(segment));
                }
                else
                {
                    // ...however it doesn't work the other way around. If invert is true and we set it to false, it'll still be true.
                    // This means that we're forced to redraw the segment from scratch, reversing directions.
                    NetManager.instance.ReleaseSegment(_currentSegmentId, true);
                    NetManager.instance.CreateSegment(out var newSegmentId,
                        ref Singleton<SimulationManager>.instance.m_randomizer,
                        infos, startNode, endNode, startDirection, endDirection, buildIndex, modifiedIndex, true);

                    var newSegment = NetManager.instance.m_segments.m_buffer[newSegmentId];
                    newSegment.m_startDirection = startDirection;
                    newSegment.m_endDirection = endDirection;

                    NetManager.instance.UpdateSegment(newSegmentId);

                    // DebugUtils.Log($"Inverted from {_currentSegmentId} to {newSegmentId} with properties: [invert: {invert} --> {newSegment.m_flags.IsFlagSet(NetSegment.Flags.Invert)}, startNode: {startNode} --> {newSegment.m_startNode}, startDirection: {startDirection}  --> {newSegment.m_startDirection}, endNode: {endNode}  --> {newSegment.m_endNode}, endDirection: {endDirection} --> {newSegment.m_endDirection}]");
                    DebugUtils.Log(JsonUtility.ToJson(newSegment));
                }
            }
        }

        #endregion

        #endregion
    }

    public class QuayUpgradeToolLoader : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            // Re-instantiate mod if recompiled after level has been loaded. Useful for UI development, but breaks actual building!
            /*if (loading.loadingComplete)
            {
                QuayUpgradeTool.Instance = new GameObject("QuayUpgradeTool").AddComponent<QuayUpgradeTool>();
            }*/
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (QuayUpgradeTool.Instance == null)
                QuayUpgradeTool.Instance = new GameObject("QuayUpgradeTool").AddComponent<QuayUpgradeTool>();
            else
                QuayUpgradeTool.Instance.Start();
        }
    }
}