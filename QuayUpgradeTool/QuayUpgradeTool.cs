using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.UI;
using CSUtil.Commons;
using QuayUpgradeTool.UI;
using QuayUpgradeTool.UI.Base;
using UnityEngine;

namespace QuayUpgradeTool
{
    public class QuayUpgradeTool : ToolBase
    {
        public void Start()
        {
            try
            {
                // Find NetTool and deploy
                if (ToolsModifierControl.GetTool<NetTool>() == null)
                {
                    Log.Warning($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Net Tool not found, can't deploy!");
                    enabled = false;
                    return;
                }

                Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Loading version: {ModInfo.ModName}");
                Log._Debug($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Adding UI components");

                // Main UI init
                // AddQuayUpgradeButton();

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

                // _toolToggleButton.eventVisibilityChanged += _toolToggleButton_eventVisibilityChanged;

                Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Loaded");
            }
            catch (Exception e)
            {
                Log._DebugOnlyError($"[{nameof(QuayUpgradeTool)}.{nameof(Start)}] Loading failed");
                Log.Exception(e);

                enabled = false;
            }
        }

        private void _toolToggleButton_eventVisibilityChanged(UIComponent component, bool value)
        {
            Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(_toolToggleButton_eventVisibilityChanged)}] Visibility changed {component.parent}");
        }

        private UIButton _toolToggleButton;
        private UIComponent _quayOptionsPanel;
        private UITabstrip _toolModeBar;
        private bool _isBeautificationOn;
        private readonly object _lock = new object();

        //private void AddQuayUpgradeButton(bool beautificationPanel = false)
        //{
        //    // Add main tool button to road options panel
        //    if (false && _toolToggleButton != null) return;

        //    if (!beautificationPanel)
        //    {
        //        var tsBar = UIUtil.FindComponent<UISlicedSprite>("TSBar", null, UIUtil.FindOptions.NameContains);
        //        if (tsBar == null)
        //        {
        //            Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(AddQuayUpgradeButton)}] Couldn't find TSBar, aborting...");
        //            return;
        //        }

        //        var optionsBar = UIUtil.FindComponent<UIPanel>("OptionsBar", tsBar, UIUtil.FindOptions.NameContains);
        //        var optionsPanel = optionsBar.Find<UIPanel>("QuaysOptionPanel");
        //        var tabStrip = optionsPanel.Find<UITabstrip>("ToolMode");

        //        _quayOptionsPanel = UIUtil.FindComponent<UIComponent>("QuaysOptionPanel", tsBar, UIUtil.FindOptions.NameContains);
        //        if (_quayOptionsPanel == null)
        //        {
        //            Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(AddQuayUpgradeButton)}] Couldn't find QuaysOptionPanel, aborting...");
        //            return;
        //        }
        //    }

        //    Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(AddQuayUpgradeButton)}] Found {nameof(_quayOptionsPanel)} with name {_quayOptionsPanel.name}");

        //    _toolModeBar = UIUtil.FindComponent<UITabstrip>("ToolMode", _quayOptionsPanel, UIUtil.FindOptions.NameContains);
        //    if (_toolModeBar == null)
        //    {
        //        Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(AddQuayUpgradeButton)}] Couldn't find ToolMode, aborting...");
        //        return;
        //    }

        //    var quayTemplate = UITemplateManager.GetAsGameObject("QuaysOptionPanel");
        //    var button = quayTemplate.AddComponent<UIButton>();

        //    Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(AddQuayUpgradeButton)}] Found {nameof(_toolModeBar)} with name {_toolModeBar.name}");

        //    //var button = UIUtil.FindComponent<UIButton>("QUT_Parallel");
        //    //if (button != null)
        //    //    Destroy(button);

        //    // _toolToggleButton = UIUtil.CreateButton(_toolModeBar, "RoadOptionUpgrade", "Quay Upgrade Tool");
        //    // _toolToggleButton.isVisible = false;
        //    //_toolToggleButton.absolutePosition = new Vector3(_toolModeBar.absolutePosition.x + _toolModeBar.size.x - 36,
        //    //    _toolModeBar.absolutePosition.y);
        //    // _toolModeBar.AddTab("Quay Upgrade Tool", _toolToggleButton, false);

        //    // _toolModeBar.eventSelectedIndexChanged += _toolModeBar_eventSelectedIndexChanged;
        //}

        private void _toolModeBar_eventSelectedIndexChanged(UIComponent component, int value)
        {
            Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(_toolModeBar_eventSelectedIndexChanged)}] Selected index {value}");
        }

        protected override void OnToolLateUpdate()
        {
            if (_isBeautificationOn) return;

            // Check if a new panel was added
            var optionsPanel = UIUtil.FindComponent<UIPanel>("QuaysOptionPanel(BeautificationPanel)", null, UIUtil.FindOptions.NameContains);
            if (optionsPanel == null) return;

            lock (_lock)
            {
                if (_isBeautificationOn) return;

                Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(OnToolLateUpdate)}] Updating options panel with the new one...");

                // Replace the previous one and rebuild the UI
                _quayOptionsPanel = optionsPanel;
                _toolModeBar = _quayOptionsPanel.Find<UITabstrip>("ToolMode");
                _toolModeBar.eventSelectedIndexChanged += _toolModeBar_eventSelectedIndexChanged;

                //Destroy(_toolToggleButton);
                //_toolToggleButton = null;

                _isBeautificationOn = true;
            }
        }

        //protected override void OnToolLateUpdate()
        //{
        //    var tsBar = UIUtil.FindComponent<UIComponent>("TSBar", null, UIUtil.FindOptions.NameContains);
        //    var quayOptionsPanel = UIUtil.FindComponent<UIComponent>("QuaysOptionPanel", tsBar, UIUtil.FindOptions.NameContains);
        //    var toolModeBar = UIUtil.FindComponent<UITabstrip>("ToolMode", quayOptionsPanel, UIUtil.FindOptions.NameContains);
        //    var tmb = (UITabstrip)quayOptionsPanel.components.First(c => c is UITabstrip);

        //    Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(OnToolLateUpdate)}] {_toolModeBar.selectedIndex} | {tmb.selectedIndex} | {toolModeBar.selectedIndex}");
        //    // Log.Info($"[{nameof(QuayUpgradeTool)}.{nameof(OnToolLateUpdate)}] {_toolModeBar.Equals(toolModeBar)} | {_toolModeBar.Equals(tmb)} | {toolModeBar.Equals(tmb)}");
        //}

    }
}
