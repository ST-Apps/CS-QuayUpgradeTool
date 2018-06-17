using System;
using System.Collections.Generic;
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
    public class QuayUpgradeTool : MonoBehaviour
    {
        public const string SettingsFileName = "QuayUpgradeTool";

        public static QuayUpgradeTool Instance;

        public static NetTool NetTool;

        private UIComponent _quayOptionsPanel;
        private UITabstrip _toolModeBar;
        private UIButton _toolToggleButton;

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
                    QuayAIDetour.Deploy();
                }
                else
                {
                    DebugUtils.Log("Disabling quay upgrade support");
                    QuayAIDetour.Revert();
                }

                _isToolActive = value;
            }
        }

        #region Handlers

        private void UnsubscribeToUIEvents()
        {
            _toolToggleButton.eventClicked -= ToolToggleButtonOnEventClicked;
        }

        private void SubscribeToUIEvents()
        {            
            _toolToggleButton.eventClicked += ToolToggleButtonOnEventClicked;
            _toolModeBar.eventSelectedIndexChanged += ToolModeBarOnEventSelectedIndexChanged;            
        }

        private void ToolModeBarOnEventSelectedIndexChanged(UIComponent component, int value)
        {
            DebugUtils.Log($"TabStrip selection changed to {value}");
        }

        private void ToolToggleButtonOnEventClicked(UIComponent component, UIMouseEventParameter eventparam)
        {
            DebugUtils.Log("Enabling upgrade mode for quays.");            
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

                QuayAIDetour.Deploy();

                DebugUtils.Log("Adding UI components");
                _quayOptionsPanel =
                    UIUtil.FindComponent<UIComponent>("QuaysOptionPanel", null, UIUtil.FindOptions.NameContains);
                if (_quayOptionsPanel == null) return;                

                _toolModeBar = UIUtil.FindComponent<UITabstrip>("ToolMode", _quayOptionsPanel, UIUtil.FindOptions.NameContains);
                if (_toolModeBar == null || !_toolModeBar.gameObject.activeInHierarchy) return;

                var button = UIUtil.FindComponent<UICheckBox>("PRT_Parallel");
                if (button != null)
                    Destroy(button);
                _toolToggleButton = UIUtil.CreateButton(_toolModeBar, "RoadOptionUpgrade", "Upgrade quay");

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

        public void OnDestroy()
        {
            UnsubscribeToUIEvents();
            QuayAIDetour.Revert();
            IsToolActive = false;
        }

        public void Update()
        {
            // _toolToggleButton.isVisible = _toolModeBar.childCount == 3;
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