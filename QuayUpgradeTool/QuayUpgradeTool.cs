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

        private UICheckBox _toolToggleButton;

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

        }

        private void SubscribeToUIEvents()
        {

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
                var tsBar = UIUtil.FindComponent<UIComponent>("TSBar", null, UIUtil.FindOptions.NameContains);
                if (tsBar == null || !tsBar.gameObject.activeInHierarchy) return;
                var button = UIUtil.FindComponent<UICheckBox>("PRT_Parallel");
                if (button != null)
                    Destroy(button);
                _toolToggleButton = UIUtil.CreateCheckBox(tsBar, "Parallel", "Upgrade", false);
                _toolToggleButton.relativePosition = new Vector3(424, -6);

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