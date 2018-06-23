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

        public void OnDestroy()
        {
            UnsubscribeToUIEvents();
            Redirection.RedirectionUtil.RevertRedirects();
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