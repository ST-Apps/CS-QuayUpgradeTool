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

        //protected override void OnToolGUI(UnityEngine.Event e)
        //{
        //    DebugUtils.Log($"OnToolGUI - {e.type}");
        //    if (e.type == UnityEngine.EventType.MouseDown)
        //    {
        //        if (e.button != 0) return;
        //        InstanceID hoverInstance = this.m_hoverInstance;
        //        InstanceID hoverInstance2 = this.m_hoverInstance2;
        //        if (this.m_selectErrors != ToolBase.ToolErrors.None)
        //            return;
        //        InstanceType type = hoverInstance.Type;
        //        switch (type)
        //        {
        //            case InstanceType.NetNode:
        //                Singleton<SimulationManager>.instance.AddAction(this.DeleteNode(hoverInstance.NetNode));
        //                break;
        //            case InstanceType.NetSegment:
        //                Singleton<SimulationManager>.instance.AddAction(this.DeleteSegment(hoverInstance.NetSegment, hoverInstance2.NetSegment));
        //                break;
        //            case InstanceType.TransportLine:
        //                Singleton<SimulationManager>.instance.AddAction(this.DeleteLine(hoverInstance.TransportLine));
        //                break;
        //            case InstanceType.Prop:
        //                Singleton<SimulationManager>.instance.AddAction(this.DeleteProp(hoverInstance.Prop));
        //                break;
        //            case InstanceType.Tree:
        //                Singleton<SimulationManager>.instance.AddAction(this.DeleteTree(hoverInstance.Tree));
        //                break;
        //            case InstanceType.Disaster:
        //                Singleton<SimulationManager>.instance.AddAction(this.DeleteDisaster(hoverInstance.Disaster));
        //                break;
        //            default:
        //                if (type != InstanceType.Building)
        //                    break;
        //                Singleton<SimulationManager>.instance.AddAction(this.TryDeleteBuilding(hoverInstance.Building));
        //                break;
        //        }
        //    }
        //}

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
            DebugUtils.Log($"SimulationStep!");
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

            DebugUtils.Log($"SimulationStep! raycastResult = {raycastResult}");

            if (this.m_mouseRayValid && ToolBase.RayCast(input, out output))
            {
                var secondarySegment = DefaultTool.FindSecondarySegment(output.m_netSegment);

                if (secondarySegment != 0 || output.m_netSegment != 0)
                    DebugUtils.Log($"Mouse is on segment {output.m_netSegment} | {secondarySegment}");
            }
        }

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