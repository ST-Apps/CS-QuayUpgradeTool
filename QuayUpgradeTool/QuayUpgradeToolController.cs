using ColossalFramework;
using ColossalFramework.UI;
using CSUtil.Commons;
using QuayUpgradeTool.UI.Base;
using System;
using System.Linq;
using UnityEngine;

namespace QuayUpgradeTool
{
    /// <summary>
    /// This acts a main controller for the tool.
    /// Its main purpose is to build the UI and enable/disable the real tool based on the selected ToolMode in beautification bar.
    /// This means that the core tool is executed only when we're in upgrade mode, allowing to spare some FPS when the tool is not actively being used.
    /// </summary>
    public class QuayUpgradeToolController : MonoBehaviour
    {
        private readonly object _lock = new object();
        
        private bool _isBeautificationOn;
        private UIComponent _quayOptionsPanel;
        private UITabstrip _toolModeBar;
        private UIButton _toolToggleButton;

        public static bool IsInGameMode { get; set; }

        #region Handlers

        private void _toolModeBar_eventSelectedIndexChanged(UIComponent component, int value)
        {
            Log._Debug(
                $"[{nameof(QuayUpgradeToolController)}.{nameof(_toolModeBar_eventSelectedIndexChanged)}] Selected index {value}");

            if (value == 3)
            {
                Log.Info($"[{nameof(QuayUpgradeToolController)}.{nameof(_toolModeBar_eventSelectedIndexChanged)}] Enabling tool ({Singleton<QuayUpgradeTool>.exists})");

                if (!Singleton<QuayUpgradeTool>.exists)
                    Singleton<QuayUpgradeTool>.Ensure();

                Singleton<QuayUpgradeTool>.instance.enabled = true;
            } else
            {
                Log.Info($"[{nameof(QuayUpgradeToolController)}.{nameof(_toolModeBar_eventSelectedIndexChanged)}] Disabling tool");

                Singleton<QuayUpgradeTool>.instance.enabled = false;
            }
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
                    Log.Info($"[{nameof(QuayUpgradeToolController)}.{nameof(Start)}] Current mode is not game, aborting...");
                    return;
                }

                // Find NetTool and deploy
                if (ToolsModifierControl.GetTool<NetTool>() == null)
                {
                    Log.Warning($"[{nameof(QuayUpgradeToolController)}.{nameof(Start)}] Net Tool not found, can't deploy!");
                    enabled = false;
                    return;
                }

                Log.Info($"[{nameof(QuayUpgradeToolController)}.{nameof(Start)}] Loading version: {ModInfo.ModName}");
                Log._Debug($"[{nameof(QuayUpgradeToolController)}.{nameof(Start)}] Adding UI components");

                var tsBar = UIUtil.FindComponent<UISlicedSprite>("TSBar", null, UIUtil.FindOptions.NameContains);
                if (tsBar == null)
                {
                    Log.Info($"[{nameof(QuayUpgradeToolController)}.{nameof(Start)}] Couldn't find TSBar, aborting...");
                    return;
                }

                var optionsBar = UIUtil.FindComponent<UIPanel>("OptionsBar", tsBar, UIUtil.FindOptions.NameContains);
                _quayOptionsPanel = optionsBar.Find<UIPanel>("QuaysOptionPanel");
                _toolModeBar = _quayOptionsPanel.Find<UITabstrip>("ToolMode");

                var modes = _quayOptionsPanel.GetComponent<OptionPanelBase>();
                ((RoadsOptionPanel)modes).m_Modes =
                    ((RoadsOptionPanel)modes).m_Modes.Union(new[] { NetTool.Mode.Upgrade }).ToArray();

                _toolToggleButton = _toolModeBar.AddTab("Upgrade", false);
                UIUtil.SetTextures(_toolToggleButton, "RoadOptionUpgrade", "Quay Upgrade Tool");                

                Log.Info($"[{nameof(QuayUpgradeToolController)}.{nameof(Start)}] Loaded");
            }
            catch (Exception e)
            {
                Log._DebugOnlyError($"[{nameof(QuayUpgradeToolController)}.{nameof(Start)}] Loading failed");
                Log.Exception(e);

                enabled = false;
            }
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
                    $"[{nameof(QuayUpgradeToolController)}.{nameof(Update)}] Updating options panel with the new one...");

                // Replace the previous one and rebuild the UI
                _quayOptionsPanel = optionsPanel;
                _toolModeBar = _quayOptionsPanel.Find<UITabstrip>("ToolMode");
                _toolModeBar.eventSelectedIndexChanged += _toolModeBar_eventSelectedIndexChanged;

                _isBeautificationOn = true;
            }
        }

        protected void OnDestroy()
        {
            //RedirectionUtil.RevertRedirects();

            DestroyImmediate(_quayOptionsPanel);
            DestroyImmediate(_toolModeBar);
            DestroyImmediate(_toolToggleButton);

            _quayOptionsPanel = null;
            _toolModeBar = null;
            _toolToggleButton = null;
        }

        #endregion
    }
}
