using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using QuayUpgradeTool.Detours;
using QuayUpgradeTool.UI;
using QuayUpgradeTool.UI.Base;
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

        private NetTool.Mode _previousMode;

        private UIMainWindow _mainWindow;

        private bool _isToolActive;
        public bool IsToolActive
        {
            get => _isToolActive && NetTool.enabled;

            private set
            {
                if (IsToolActive == value) return;
                if (value)
                {
                    DebugUtils.Log("Enabling quay upgrade support");
                    Redirection.RedirectionUtil.Redirect();
                }
                else
                {
                    DebugUtils.Log("Disabling quay upgrade support");
                    Redirection.RedirectionUtil.RevertRedirects();
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
            Redirection.RedirectionUtil.RevertRedirects();
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

        private ushort _currentSegment;                

        protected override void OnToolLateUpdate()
        {
            DebugUtils.Log("OnToolLateUpdate");
            base.OnToolLateUpdate();

            this.m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            this.m_mouseRayLength = Camera.main.farClipPlane;
            this.m_mouseRayValid = /*!this.m_toolController.IsInsideUI &&*/ Cursor.visible;

            if (IsToolActive)
                SimulationStep();
        }

        public override void SimulationStep()
        {            
            base.SimulationStep();

            NetInfo info1 = NetTool.m_prefab;
            if (info1 == null)
                return;

            NetManager instance = Singleton<NetManager>.instance;
            ToolBase.RaycastInput input = new ToolBase.RaycastInput(this.m_mouseRay, this.m_mouseRayLength);
            input.m_buildObject = (PrefabInfo)info1;
            input.m_netService = new ToolBase.RaycastService(info1.m_class.m_service, info1.m_class.m_subService, info1.m_class.m_layer);
            input.m_ignoreTerrain = true;
            input.m_ignoreNodeFlags = NetNode.Flags.All;
            input.m_ignoreSegmentFlags = NetSegment.Flags.Untouchable;
            if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Transport || Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Traffic || (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.TrafficRoutes || Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Tours))
                input.m_netService.m_itemLayers |= ItemClass.Layer.MetroTunnels;
            else if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.Underground && Singleton<InfoManager>.instance.CurrentSubMode == InfoManager.SubInfoMode.Default)
                input.m_netService.m_itemLayers |= ItemClass.Layer.MetroTunnels;

            ToolBase.RaycastOutput output;
            var raycastResult = RayCast(input, out output);

            if (this.m_mouseRayValid && ToolBase.RayCast(input, out output))
            {
                var secondarySegment = DefaultTool.FindSecondarySegment(output.m_netSegment);
                _currentSegment = output.m_netSegment;
                if (_currentSegment == 0)
                    _currentSegment = secondarySegment;                
            }
        }

        protected override void OnToolGUI(Event e)
        {            
            if (IsToolActive && e.type == EventType.MouseDown && e.button == 1 && _currentSegment != 0)
            {
                // Right click means that we need to invert direction                
                var segment = NetManager.instance.m_segments.m_buffer[_currentSegment];
                var startNode = segment.m_startNode;
                var startDirection = segment.m_startDirection;
                var endNode = segment.m_endNode;
                var endDirection = segment.m_endDirection;
                var infos = segment.Info;
                var buildIndex = segment.m_buildIndex;
                var modifiedIndex = segment.m_modifiedIndex;
                var invert = segment.m_flags.IsFlagSet(NetSegment.Flags.Invert);

                DebugUtils.Log($"Inverting segment with ID {_currentSegment} with invert = {invert}");

                // TODO: check if it's working with invert true or false
                if (invert) endDirection = -startDirection;

                NetManager.instance.ReleaseSegment(_currentSegment, true);
                NetManager.instance.CreateSegment(out ushort segmentId, ref Singleton<SimulationManager>.instance.m_randomizer, infos, startNode, endNode, startDirection, endDirection, buildIndex, modifiedIndex, !invert); // TODO: !invert is probably wrong
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