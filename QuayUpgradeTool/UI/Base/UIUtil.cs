using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ColossalFramework.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuayUpgradeTool.UI.Base
{
    public static class UIUtil
    {
        [Flags]
        public enum FindOptions
        {
            None = 0,
            NameContains = 1 << 0
        }

        private const float COLUMN_PADDING = 5f;
        private const float TEXT_FIELD_WIDTH = 35f;
        public static readonly UITextureAtlas TextureAtlas = LoadResources();

        public static UIView uiRoot;

        // some helper functions from https://github.com/bernardd/Crossings/blob/master/Crossings/UIUtils.cs
        private static void FindUIRoot()
        {
            uiRoot = null;

            foreach (var view in Object.FindObjectsOfType<UIView>())
                if (view.transform.parent == null && view.name == "UIView")
                {
                    uiRoot = view;
                    break;
                }
        }

        public static string GetTransformPath(Transform transform)
        {
            var path = transform.name;
            var t = transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }

            return path;
        }

        public static T FindComponent<T>(string name, UIComponent parent = null, FindOptions options = FindOptions.None)
            where T : UIComponent
        {
            if (uiRoot == null)
            {
                FindUIRoot();
                if (uiRoot == null) return null;
            }

            foreach (var component in Object.FindObjectsOfType<T>())
            {
                bool nameMatches;
                if ((options & FindOptions.NameContains) != 0) nameMatches = component.name.Contains(name);
                else nameMatches = component.name == name;

                if (!nameMatches) continue;

                Transform parentTransform;
                if (parent != null) parentTransform = parent.transform;
                else parentTransform = uiRoot.transform;

                var t = component.transform.parent;
                while (t != null && t != parentTransform) t = t.parent;

                if (t == null) continue;

                return component;
            }


            return null;
        }

        public static IEnumerable<T> FindComponents<T>(string name, UIComponent parent = null, FindOptions options = FindOptions.None)
            where T : UIComponent
        {
            if (uiRoot == null)
            {
                FindUIRoot();
                if (uiRoot == null) yield return null;
            }

            foreach (var component in Object.FindObjectsOfType<T>())
            {
                bool nameMatches;
                if ((options & FindOptions.NameContains) != 0) nameMatches = component.name.Contains(name);
                else nameMatches = component.name == name;

                if (!nameMatches) continue;

                Transform parentTransform;
                if (parent != null) parentTransform = parent.transform;
                else parentTransform = uiRoot.transform;

                var t = component.transform.parent;
                while (t != null && t != parentTransform) t = t.parent;

                if (t == null) continue;

                yield return component;
            }


            yield return null;
        }

        public static UICheckBox CreateCheckBox(UIComponent parent, string spriteName, string toolTip, bool value)
        {
            var checkBox = parent.AddUIComponent<UICheckBox>();
            checkBox.size = new Vector2(36, 36);

            var button = checkBox.AddUIComponent<UIButton>();
            button.name = "QUT__" + spriteName;
            button.atlas = TextureAtlas;
            button.tooltip = toolTip;
            button.relativePosition = new Vector2(0, 0);

            button.normalBgSprite = "OptionBase";
            button.hoveredBgSprite = "OptionBaseHovered";
            button.pressedBgSprite = "OptionBasePressed";
            button.disabledBgSprite = "OptionBaseDisabled";

            button.normalFgSprite = spriteName;
            button.hoveredFgSprite = spriteName + "Hovered";
            button.pressedFgSprite = spriteName + "Pressed";
            button.disabledFgSprite = spriteName + "Disabled";

            checkBox.isChecked = value;
            if (value)
            {
                button.normalBgSprite = "OptionBaseFocused";
                button.normalFgSprite = spriteName + "Focused";
            }

            checkBox.eventCheckChanged += (c, s) =>
            {
                if (s)
                {
                    button.normalBgSprite = "OptionBaseFocused";
                    button.normalFgSprite = spriteName + "Focused";
                }
                else
                {
                    button.normalBgSprite = "OptionBase";
                    button.normalFgSprite = spriteName;
                }
            };

            return checkBox;
        }

        public static void SetSprite(this UIButton button, string spriteName)
        {
            // button.atlas = TextureAtlas;

            button.normalBgSprite = "OptionBase";
            button.hoveredBgSprite = "OptionBaseHovered";
            button.pressedBgSprite = "OptionBasePressed";
            button.disabledBgSprite = "OptionBaseDisabled";
            button.focusedBgSprite = "OptionsBaseFocused";

            button.normalFgSprite = spriteName; // RoadOptionUpgrade
            button.hoveredFgSprite = spriteName + "Hovered"; // RoadOptionUpgradeHovered
            button.pressedFgSprite = spriteName + "Pressed";
            button.disabledFgSprite = spriteName + "Disabled";
            button.focusedFgSprite = spriteName + "Focused"; // RoadOptionUpgradeFocused
        }

        public static UIButton CreateButton(UIComponent parent, string spriteName, string toolTip)
        {
            var button = parent is UITabstrip ? new UIButton() : parent.AddUIComponent<UIButton>();

            // button.size = new Vector2(36, 36);
            button.name = "QUT_" + spriteName;
            button.atlas = TextureAtlas;
            button.tooltip = toolTip;
            // button.relativePosition = new Vector2(0, 0);

            button.normalBgSprite = "OptionBase";
            button.hoveredBgSprite = "OptionBaseHovered";
            button.pressedBgSprite = "OptionBasePressed";
            button.disabledBgSprite = "OptionBaseDisabled";
            button.focusedBgSprite = "OptionsBaseFocused";

            button.normalFgSprite = spriteName;
            button.hoveredFgSprite = spriteName + "Hovered";
            button.pressedFgSprite = spriteName + "Pressed";
            button.disabledFgSprite = spriteName + "Disabled";
            button.focusedFgSprite = spriteName + "Focused";

            return button;
        }

        public static void SetTextures(UIButton button, string spriteName, string toolTip)
        {
            button.name = "QUT_" + spriteName;
            button.atlas ??= TextureAtlas;
            button.tooltip = toolTip;

            button.normalBgSprite = "OptionBase";
            button.hoveredBgSprite = "OptionBaseHovered";
            button.pressedBgSprite = "OptionBasePressed";
            button.disabledBgSprite = "OptionBaseDisabled";
            button.focusedBgSprite = "OptionBaseFocused";

            button.normalFgSprite = spriteName;
            button.hoveredFgSprite = spriteName + "Hovered";
            button.pressedFgSprite = spriteName + "Pressed";
            button.disabledFgSprite = spriteName + "Disabled";
            button.focusedFgSprite = spriteName + "Focused";
        }

        public static UIMultiStateButton CreateMultiStateButton(UIComponent parent, string spriteName, string toolTip)
        {
            var button = parent.AddUIComponent<UIMultiStateButton>();
            button.size = new Vector2(36, 36);
            button.name = "QUT_" + spriteName;
            button.atlas = TextureAtlas;
            button.tooltip = toolTip;
            button.relativePosition = new Vector2(0, 0);

            //button.normalBgSprite = "OptionBase";
            //button.hoveredBgSprite = "OptionBaseHovered";
            //button.pressedBgSprite = "OptionBasePressed";
            //button.disabledBgSprite = "OptionBaseDisabled";
            //button.focusedBgSprite = "OptionsBaseFocused";

            //button.normalFgSprite = spriteName;
            //button.hoveredFgSprite = spriteName + "Hovered";
            //button.pressedFgSprite = spriteName + "Pressed";
            //button.disabledFgSprite = spriteName + "Disabled";
            //button.focusedFgSprite = spriteName + "Focused";

            return button;
        }

        private static UITextureAtlas LoadResources()
        {
            return ResourceLoader.GetAtlas("Ingame");
        }
    }
}