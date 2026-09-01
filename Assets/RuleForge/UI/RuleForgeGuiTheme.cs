using UnityEngine;

namespace RuleForge.UI
{
    public static class RuleForgeGuiTheme
    {
        private static Texture2D panelTexture;
        private static Texture2D buttonTexture;
        private static Texture2D buttonHoverTexture;
        private static Texture2D dangerButtonTexture;
        private static Font uiFont;
        private static GUISkin themedSkin;

        public static void Configure(
            Texture2D panel,
            Texture2D button,
            Texture2D buttonHover,
            Texture2D dangerButton,
            Font font)
        {
            bool changed = panelTexture != panel ||
                           buttonTexture != button ||
                           buttonHoverTexture != buttonHover ||
                           dangerButtonTexture != dangerButton ||
                           uiFont != font;
            panelTexture = panel;
            buttonTexture = button;
            buttonHoverTexture = buttonHover;
            dangerButtonTexture = dangerButton;
            uiFont = font;
            if (changed)
            {
                themedSkin = null;
            }
        }

        public static GUISkin Begin()
        {
            GUISkin previous = GUI.skin;
            if (themedSkin == null)
            {
                themedSkin = Object.Instantiate(previous);
                themedSkin.hideFlags = HideFlags.DontSave;
                ApplyTypography(themedSkin);
                ApplyPanels(themedSkin);
                ApplyButtons(themedSkin);
                ApplyInputs(themedSkin);
            }

            GUI.skin = themedSkin;
            return previous;
        }

        public static void End(GUISkin previous)
        {
            if (previous != null)
            {
                GUI.skin = previous;
            }
        }

        public static GUIStyle CreateDangerButtonStyle()
        {
            GUIStyle style = new GUIStyle(
                themedSkin != null ? themedSkin.button : GUI.skin.button);
            if (dangerButtonTexture != null)
            {
                style.normal.background = dangerButtonTexture;
                style.hover.background = dangerButtonTexture;
                style.active.background = dangerButtonTexture;
            }

            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.active.textColor = Color.white;
            return style;
        }

        private static void ApplyTypography(GUISkin skin)
        {
            skin.label.fontSize = 14;
            skin.label.normal.textColor = new Color(0.9f, 0.96f, 1f, 1f);
            skin.box.fontSize = 15;
            skin.box.fontStyle = FontStyle.Bold;
            skin.box.normal.textColor = Color.white;
            skin.window.fontSize = 18;
            skin.window.fontStyle = FontStyle.Bold;
            skin.window.normal.textColor = Color.white;
        }

        private static void ApplyPanels(GUISkin skin)
        {
            if (panelTexture != null)
            {
                skin.window.normal.background = panelTexture;
                skin.box.normal.background = panelTexture;
            }

            skin.window.padding = new RectOffset(14, 14, 28, 14);
            skin.box.padding = new RectOffset(12, 12, 9, 9);
            skin.box.margin = new RectOffset(3, 3, 3, 3);
        }

        private static void ApplyButtons(GUISkin skin)
        {
            if (buttonTexture != null)
            {
                skin.button.normal.background = buttonTexture;
                skin.button.focused.background = buttonTexture;
            }

            if (buttonHoverTexture != null)
            {
                skin.button.hover.background = buttonHoverTexture;
                skin.button.active.background = buttonHoverTexture;
                skin.button.onNormal.background = buttonHoverTexture;
                skin.button.onHover.background = buttonHoverTexture;
            }

            skin.button.fontSize = 15;
            skin.button.fontStyle = FontStyle.Bold;
            skin.button.alignment = TextAnchor.MiddleCenter;
            skin.button.normal.textColor = Color.white;
            skin.button.hover.textColor = Color.white;
            skin.button.active.textColor = Color.white;
            skin.button.padding = new RectOffset(12, 12, 7, 7);
        }

        private static void ApplyInputs(GUISkin skin)
        {
            if (panelTexture != null)
            {
                skin.textArea.normal.background = panelTexture;
                skin.textArea.focused.background = panelTexture;
                skin.textField.normal.background = panelTexture;
                skin.textField.focused.background = panelTexture;
            }

            skin.textArea.fontSize = 15;
            skin.textArea.wordWrap = true;
            skin.textArea.normal.textColor = Color.white;
            skin.textArea.focused.textColor = Color.white;
            skin.textArea.padding = new RectOffset(12, 12, 10, 10);
            skin.textField.fontSize = 14;
            skin.textField.normal.textColor = Color.white;
            skin.textField.focused.textColor = Color.white;
            skin.textField.padding = new RectOffset(8, 8, 5, 5);
        }
    }
}
