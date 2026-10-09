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
        private static Texture2D windowBackground;
        private static Texture2D cardBackground;
        private static Texture2D inputBackground;
        private static Texture2D inputFocusedBackground;
        private static Texture2D primaryButtonBackground;
        private static Texture2D primaryButtonHoverBackground;
        private static Texture2D dangerBackground;
        private static Texture2D dangerHoverBackground;
        private static Texture2D secondaryBackground;
        private static Texture2D secondaryHoverBackground;
        private static Texture2D playBackground;
        private static Texture2D playHoverBackground;
        private static Texture2D creatorFrameTexture;

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
            EnsurePaletteTextures();
            style.normal.background = dangerBackground;
            style.hover.background = dangerHoverBackground;
            style.active.background = dangerBackground;

            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.active.textColor = Color.white;
            return style;
        }

        public static GUIStyle CreateSecondaryButtonStyle()
        {
            EnsurePaletteTextures();
            GUIStyle style = new GUIStyle(
                themedSkin != null ? themedSkin.button : GUI.skin.button);
            style.normal.background = secondaryBackground;
            style.focused.background = secondaryBackground;
            style.hover.background = secondaryHoverBackground;
            style.active.background = secondaryHoverBackground;
            return style;
        }

        public static GUIStyle CreatePlayButtonStyle()
        {
            EnsurePaletteTextures();
            GUIStyle style = new GUIStyle(
                themedSkin != null ? themedSkin.button : GUI.skin.button);
            style.normal.background = playBackground;
            style.focused.background = playBackground;
            style.hover.background = playHoverBackground;
            style.active.background = playHoverBackground;
            style.fontSize = 17;
            return style;
        }

        public static GUIStyle CreateCreatorWindowStyle()
        {
            GUIStyle style = new GUIStyle(
                themedSkin != null ? themedSkin.window : GUI.skin.window);
            if (creatorFrameTexture == null)
            {
                creatorFrameTexture = Resources.Load<Texture2D>(
                    "UI/CreatorPanelFrame");
            }

            if (creatorFrameTexture != null)
            {
                style.normal.background = creatorFrameTexture;
                style.border = new RectOffset(145, 145, 145, 145);
                style.padding = new RectOffset(105, 105, 96, 90);
            }

            return style;
        }

        private static void ApplyTypography(GUISkin skin)
        {
            skin.label.fontSize = 14;
            skin.label.wordWrap = true;
            skin.label.normal.textColor = new Color(0.88f, 0.93f, 0.97f, 1f);
            skin.box.fontSize = 14;
            skin.box.fontStyle = FontStyle.Normal;
            skin.box.normal.textColor = new Color(0.9f, 0.95f, 1f, 1f);
            skin.window.fontSize = 16;
            skin.window.fontStyle = FontStyle.Bold;
            skin.window.normal.textColor = Color.white;
        }

        private static void ApplyPanels(GUISkin skin)
        {
            EnsurePaletteTextures();
            skin.window.normal.background = windowBackground;
            skin.box.normal.background = cardBackground;

            skin.window.padding = new RectOffset(22, 22, 20, 20);
            skin.box.padding = new RectOffset(16, 16, 14, 14);
            skin.box.margin = new RectOffset(2, 2, 5, 5);
        }

        private static void ApplyButtons(GUISkin skin)
        {
            EnsurePaletteTextures();
            skin.button.normal.background = primaryButtonBackground;
            skin.button.focused.background = primaryButtonBackground;
            skin.button.hover.background = primaryButtonHoverBackground;
            skin.button.active.background = primaryButtonHoverBackground;
            skin.button.onNormal.background = primaryButtonHoverBackground;
            skin.button.onHover.background = primaryButtonHoverBackground;

            skin.button.fontSize = 14;
            skin.button.fontStyle = FontStyle.Bold;
            skin.button.alignment = TextAnchor.MiddleCenter;
            skin.button.normal.textColor = Color.white;
            skin.button.hover.textColor = Color.white;
            skin.button.active.textColor = Color.white;
            skin.button.padding = new RectOffset(14, 14, 9, 9);
            skin.button.margin = new RectOffset(3, 3, 4, 4);
            skin.button.contentOffset = Vector2.zero;
        }

        private static void ApplyInputs(GUISkin skin)
        {
            EnsurePaletteTextures();
            skin.textArea.normal.background = inputBackground;
            skin.textArea.focused.background = inputFocusedBackground;
            skin.textField.normal.background = inputBackground;
            skin.textField.focused.background = inputFocusedBackground;

            skin.textArea.fontSize = 15;
            skin.textArea.wordWrap = true;
            skin.textArea.normal.textColor = Color.white;
            skin.textArea.focused.textColor = Color.white;
            skin.textArea.padding = new RectOffset(14, 14, 12, 12);
            skin.textField.fontSize = 14;
            skin.textField.normal.textColor = Color.white;
            skin.textField.focused.textColor = Color.white;
            skin.textField.padding = new RectOffset(10, 10, 7, 7);
        }

        private static void EnsurePaletteTextures()
        {
            if (windowBackground != null)
            {
                return;
            }

            windowBackground = MakeTexture(
                new Color(0.025f, 0.047f, 0.075f, 0.98f));
            cardBackground = MakeTexture(
                new Color(0.055f, 0.09f, 0.13f, 0.96f));
            inputBackground = MakeTexture(
                new Color(0.018f, 0.035f, 0.058f, 1f));
            inputFocusedBackground = MakeTexture(
                new Color(0.028f, 0.095f, 0.13f, 1f));
            primaryButtonBackground = MakeTexture(
                new Color(0.04f, 0.47f, 0.63f, 1f));
            primaryButtonHoverBackground = MakeTexture(
                new Color(0.05f, 0.66f, 0.81f, 1f));
            dangerBackground = MakeTexture(
                new Color(0.67f, 0.1f, 0.18f, 1f));
            dangerHoverBackground = MakeTexture(
                new Color(0.92f, 0.2f, 0.28f, 1f));
            secondaryBackground = MakeTexture(
                new Color(0.105f, 0.16f, 0.205f, 1f));
            secondaryHoverBackground = MakeTexture(
                new Color(0.15f, 0.225f, 0.275f, 1f));
            playBackground = MakeTexture(
                new Color(0.08f, 0.51f, 0.37f, 1f));
            playHoverBackground = MakeTexture(
                new Color(0.11f, 0.68f, 0.49f, 1f));
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "RuleForge UI Color"
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }
    }
}
