using KKAPI.Utilities;
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
        private static readonly Texture2D _btnNormalBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        private static readonly Texture2D _btnOnNormalBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        private static readonly Texture2D _winNormalBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        private static readonly Texture2D _toggleNormalBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        private static readonly Texture2D _toggleOnNormalBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);

        public static GUIStyle ToggleStyle;
        public static GUIStyle TextFieldStyle;
        public static GUIStyle LabelStyle;
        public static GUIStyle CenteredLabelStyle;
        public static GUIStyle ButtonStyle;
        public static GUIStyle SectionLabelStyle;
        public static GUIStyle DangerToggleStyle;
        public static GUIStyle TooltipStyle;
        public static GUIStyle headerStyle;
        public static GUIStyle EmptyBoxStyle;
        public static GUIStyle ControlButton;
        public static GUIStyle ScreenshotToolButton;
        public static GUIStyle BoxStyle;
        public static GUIStyle WindowStyle;
        public static GUIStyle SmallButtonStyle;
        public static GUIStyle ProgressBar;
        public static GUIStyle WarningLabelStyle;
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
            _veSkin.window = WindowStyle;

            byte[] texData = ResourceUtils.GetEmbeddedResource("btn.png");
            LoadImage(_btnNormalBackground, texData);
            Object.DontDestroyOnLoad(_btnNormalBackground);
            texData = ResourceUtils.GetEmbeddedResource("btn_on.png");
            LoadImage(_btnOnNormalBackground, texData);
            Object.DontDestroyOnLoad(_btnOnNormalBackground);
            texData = ResourceUtils.GetEmbeddedResource("PopupWindowOff.png");
            LoadImage(_winNormalBackground, texData);
            Object.DontDestroyOnLoad(_winNormalBackground);
            texData = ResourceUtils.GetEmbeddedResource("checkbox_norm.png");
            LoadImage(_toggleNormalBackground, texData);
            Object.DontDestroyOnLoad(_toggleNormalBackground);
            texData = ResourceUtils.GetEmbeddedResource("checkbox_on.png");
            LoadImage(_toggleOnNormalBackground, texData);
            Object.DontDestroyOnLoad(_toggleOnNormalBackground);
        }
        private static void LoadImage(Texture2D texture, byte[] tex)
        {
#if (!KOIKATSU || SUNSHINE)
            ImageConversion.LoadImage(texture, tex);
#else
            texture.LoadImage(tex);
#endif
            texture.anisoLevel = 1;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
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
                margin = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(25, 0, 4, 4),
                border = new RectOffset(17, 0, 17, 0),
                fixedWidth = 0,
                fixedHeight = 0,
                stretchWidth = false,
                stretchHeight = false
            };

            ToggleStyle.normal.background = _toggleNormalBackground;
            ToggleStyle.active.background = _toggleNormalBackground;
            ToggleStyle.hover.background = _toggleNormalBackground;
            ToggleStyle.focused.background = _toggleNormalBackground;
            ToggleStyle.onNormal.background = _toggleOnNormalBackground;
            ToggleStyle.onActive.background = _toggleOnNormalBackground;
            ToggleStyle.onHover.background = _toggleOnNormalBackground;


            TextFieldStyle = new GUIStyle(GUI.skin.box)
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

            headerStyle = new GUIStyle(_originalSkin.label)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };

            CenteredLabelStyle = new GUIStyle(LabelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
            };

            ButtonStyle = new GUIStyle(_originalSkin.button)
            {
                fontSize = CommonFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                hover = { textColor = Color.yellow },
                onHover = { textColor = Color.yellow },
                onNormal = { textColor = NormalColorOn },
                border = new RectOffset(6, 6, 4, 4),
                padding = new RectOffset(6, 6, 7, 9),
                margin = new RectOffset(3, 3, 3, 3),
                stretchWidth = true,
                stretchHeight = false,
            };

            ButtonStyle.normal.background = _btnNormalBackground;
            ButtonStyle.active.background = _btnNormalBackground;
            ButtonStyle.hover.background = _btnNormalBackground;
            ButtonStyle.focused.background = _btnNormalBackground;
            ButtonStyle.onNormal.background = _btnOnNormalBackground;
            ButtonStyle.onActive.background = _btnOnNormalBackground;
            ButtonStyle.onHover.background = _btnOnNormalBackground;

            SmallButtonStyle = new GUIStyle(ButtonStyle)
            {
                fontStyle = FontStyle.Bold,
            };


            SectionLabelStyle = new GUIStyle(LabelStyle)
            {
                fontSize = CommonFontSize,
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
            };

            TooltipStyle = new GUIStyle(_originalSkin.box)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 5, 5),
            };
            TooltipStyle.normal.background = _winNormalBackground;

            BoxStyle = new GUIStyle(_originalSkin.box)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 0, 10),
                margin = new RectOffset(0, 0, 0, 0),
            };
            BoxStyle.normal.background = null;

            EmptyBoxStyle = new GUIStyle(_originalSkin.box)
            {
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 0, 10),
            };
            EmptyBoxStyle.normal.background = null;


            ControlButton = new GUIStyle(_originalSkin.button)
            {
                fontSize = CommonFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                hover = { textColor = Color.yellow },
                onHover = { textColor = Color.yellow },
                onNormal = { textColor = NormalColorOn },
            };

            ScreenshotToolButton = new GUIStyle(_originalSkin.button)
            {
                fixedHeight = 30,
                alignment = TextAnchor.MiddleCenter
            };

            WindowStyle = new GUIStyle(_originalSkin.window)
            {
                fontSize = CommonFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(10, 10, 35, 10),
                border = new RectOffset(12, 12, 26, 12),
            };
            WindowStyle.normal.background = _winNormalBackground;
            WindowStyle.onNormal.background = _winNormalBackground;

            ProgressBar = new GUIStyle 
            { 
                normal = new GUIStyleState { background = Texture2D.whiteTexture } 
            };
            
            WarningLabelStyle = new GUIStyle(LabelStyle)
            {
                normal = { textColor = Color.yellow },
                fontSize = CommonFontSize,
                alignment = TextAnchor.MiddleLeft,
            };
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