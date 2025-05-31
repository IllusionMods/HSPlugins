using UnityEngine;

namespace VideoExport
{
    public static class Styles
    {
        public static int CommonFontSize = 14;
        public static int SectionFontSize = 16;

        public static int SectionSpacing = 5;
        public static int WindowWidth = 520;

#if HONEYSELECT2 || AISHOUJO
        public static Color NormalColorOn = new Color(0.69f, 0.93f, 0.38f);
        public static Color DangerColorOn = new Color(0.83f, 0.30f, 0.21f);
#else
        public static Color NormalColorOn = new Color(0.49f, 0.69f, 0.31f);
        public static Color DangerColorOn = new Color(0.79f, 0.20f, 0.21f);
#endif


        public static GUIStyle ToggleStyle;
        public static GUIStyle TextFieldStyle;
        public static GUIStyle LabelStyle;
        public static GUIStyle CenteredLabelStyle;
        public static GUIStyle ButtonStyle;
        public static GUIStyle SectionLabelStyle;
        public static GUIStyle DangerToggleStyle;
        public static GUIStyle TooltipStyle;

        private static GUISkin _originalSkin = null;
        private static GUISkin _veSkin = null;

        public static void InitializeSkin(GUISkin originalSkin)
        {
            _originalSkin = originalSkin;
            _veSkin = Object.Instantiate(originalSkin);

            InitializeStyles();
            _veSkin.toggle = ToggleStyle;
            _veSkin.label = LabelStyle;
            _veSkin.button = ButtonStyle;
            _veSkin.textField = TextFieldStyle;
        }

        private static void InitializeStyles()
        {
            ToggleStyle = new GUIStyle(_originalSkin.toggle)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                hover = { textColor = Color.yellow },
                onHover = { textColor = Color.yellow },
                onNormal = { textColor = NormalColorOn },
                padding = new RectOffset(20, 5, 3, 3),
            };

            TextFieldStyle = new GUIStyle(_originalSkin.textField)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 3, 3),
            };

            LabelStyle = new GUIStyle(_originalSkin.label)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 3, 3),
            };

            CenteredLabelStyle = new GUIStyle(LabelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
            };

            ButtonStyle = new GUIStyle(_originalSkin.button)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleCenter,
                hover = { textColor = Color.yellow },
                onHover = { textColor = Color.yellow },
                onNormal = { textColor = NormalColorOn },
                padding = new RectOffset(5, 5, 3, 3),
            };

            SectionLabelStyle = new GUIStyle(LabelStyle)
            {
                fontSize = SectionFontSize,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };

            DangerToggleStyle = new GUIStyle(ToggleStyle)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                hover = { textColor = Color.yellow },
                onHover = { textColor = Color.yellow },
                onNormal = { textColor = DangerColorOn },
                padding = new RectOffset(20, 5, 3, 3),
            };

            TooltipStyle = new GUIStyle(_originalSkin.box)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 5, 5),
            };
            TooltipStyle.normal.background = CreateBlackTexture();
        }

        public static void BeginVESkin()
        {
            if (!_veSkin)
                InitializeSkin(GUI.skin);

            if (_veSkin)
                GUI.skin = _veSkin;
        }

        public static void EndVESkin()
        {
            if (_originalSkin)
                GUI.skin = _originalSkin;
        }

        private static Texture2D CreateBlackTexture()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0, 0, 0, 1));
            texture.Apply();
            return texture;
        }
    }
}