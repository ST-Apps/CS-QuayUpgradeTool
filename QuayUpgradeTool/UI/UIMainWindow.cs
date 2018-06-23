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
        }

        private void ToolToggleButtonOnEventCheckChanged(UIComponent component, bool value)
        {
            DebugUtils.Log($"Tool toggle pressed - {value}");            
            OnParallelToolToggled?.Invoke(component, value);

            // TODO: this doesn't work for some reason.
            _toolModeBar.isEnabled = !QuayUpgradeTool.Instance.IsToolActive;
        }

        #endregion

        #region Unity

        public override void Start()
        {
            name = "QUT_MainWindow";
            isVisible = false;
            size = new Vector2(1, 1);
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

            var button = UIUtil.FindComponent<UICheckBox>("QUT_Parallel");
            if (button != null)
                Destroy(button);

            _toolToggleButton = UIUtil.CreateCheckBox(tsBar, "RoadOptionUpgrade", "Quay Upgrade Tool", false);
            _toolToggleButton.isVisible = false;
            _toolToggleButton.absolutePosition = new Vector3(_toolModeBar.absolutePosition.x + _toolModeBar.size.x - 36, _toolModeBar.absolutePosition.y);            

            SubscribeToUIEvents();

            OnPositionChanged();
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

            // TODO: this doesn't work for some reason.
            _toolModeBar.isEnabled = !QuayUpgradeTool.Instance.IsToolActive;

            base.Update();
        }


        #endregion
    }
}