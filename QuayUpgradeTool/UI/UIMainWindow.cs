using System;
using ColossalFramework;
using ColossalFramework.UI;
using QuayUpgradeTool.UI.Base;
using UnityEngine;

namespace QuayUpgradeTool.UI
{
    public class UIMainWindow : UIPanel
    {
        private UICheckBox _toolToggleButton;
        private UIComponent _quayOptionsPanel;
        private UITabstrip _toolModeBar;        

        #region Events/Callbacks

        public event PropertyChangedEventHandler<bool> OnParallelToolToggled;

        private void UnsubscribeToUIEvents()
        {
            _toolToggleButton.eventCheckChanged -= ToolToggleButtonOnEventCheckChanged;
        }

        private void SubscribeToUIEvents()
        {
            _toolToggleButton.eventCheckChanged += ToolToggleButtonOnEventCheckChanged;
            _toolModeBar.eventSelectedIndexChanged += _toolModeBar_eventSelectedIndexChanged;
        }

        private void _toolModeBar_eventSelectedIndexChanged(UIComponent component, int value)
        {
            // When something is selected in TabStrip we need to disable the mod
            _toolToggleButton.isChecked = false;
            // ToolToggleButtonOnEventCheckChanged(component, false);
        }

        private void ToolToggleButtonOnEventCheckChanged(UIComponent component, bool value)
        {
            DebugUtils.Log("Tool toggle pressed.");
            OnParallelToolToggled?.Invoke(component, value);
        }

        #endregion

        #region Control

        public void ToggleToolCheckbox()
        {
            _toolToggleButton.isChecked = !_toolToggleButton.isChecked;
            OnParallelToolToggled?.Invoke(_toolToggleButton, _toolToggleButton.isChecked);
        }

        #endregion

        #region Unity

        public override void Start()
        {
            name = "QUT__MainWindow";
            isVisible = false;
            size = new Vector2(500, 240);
            autoFitChildrenVertically = true;

            // Add main tool button to road options panel
            if (_toolToggleButton != null) return;

            var tsBar = UIUtil.FindComponent<UIComponent>("TSBar", null, UIUtil.FindOptions.NameContains);
            if (tsBar == null) return;

            _quayOptionsPanel =
                UIUtil.FindComponent<UIComponent>("QuaysOptionPanel", null, UIUtil.FindOptions.NameContains);
            if (_quayOptionsPanel == null) return;

            _toolModeBar = UIUtil.FindComponent<UITabstrip>("ToolMode", _quayOptionsPanel, UIUtil.FindOptions.NameContains);
            if (_toolModeBar == null) return;

            var button = UIUtil.FindComponent<UICheckBox>("QUT__Parallel");
            if (button != null)
                Destroy(button);

            _toolToggleButton = UIUtil.CreateCheckBox(tsBar, "RoadOptionUpgrade", "Quay Upgrade Tool", false);
            _toolToggleButton.isVisible = false;
            _toolToggleButton.absolutePosition = new Vector3(_toolModeBar.absolutePosition.x + _toolModeBar.size.x - 36, _toolModeBar.absolutePosition.y);

            SubscribeToUIEvents();

            OnPositionChanged();
            DebugUtils.Log($"UIMainWindow created {size} | {position}");
        }

        public override void OnDestroy()
        {
            UnsubscribeToUIEvents();
            base.OnDestroy();
        }

        public override void Update()
        {
            if (QuayUpgradeTool.Instance != null)
                isVisible = QuayUpgradeTool.Instance.IsToolActive;

            if (QuayUpgradeTool.NetTool != null)
                _toolToggleButton.isVisible = QuayUpgradeTool.NetTool.enabled;      
            
            if (QuayUpgradeTool.Instance.IsToolActive)
            {
                DebugUtils.Log("Disabling buttons in TabStrip");

                foreach (var item in _toolModeBar.GetComponentsInChildren<UIButton>())
                {
                    DebugUtils.Log($"Disabling for {item.name}");

                    item.state = UIButton.ButtonState.Normal;
                }
            }

            base.Update();
        }


        #endregion
    }
}