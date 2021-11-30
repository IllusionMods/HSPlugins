using System;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;

namespace HSLRE.CustomEffects
{
    public class TonemappingColorGrading
    {
        public TonemappingColorGrading()
        {
            this.m_EyeAdaptation = EyeAdaptationSettings.defaultSettings;
            this.m_Tonemapping = TonemappingSettings.defaultSettings;
            this.m_ColorGrading = ColorGradingSettings.defaultSettings;
            this.m_Lut = LUTSettings.defaultSettings;
            this.m_Dirty = true;
            this.m_TonemapperDirty = true;
        }

        public void SetDirty()
        {
            this.m_Dirty = true;
        }

        public void OnEnable()
        {
            this.SetDirty();
            this.SetTonemapperDirty();
            Debug.Log("Tonemapping enabled!");
        }

        private static Color NormalizeColor(Color c)
        {
            float num = (c.r + c.g + c.b) / 3f;
            if (Mathf.Approximately(num, 0f))
            {
                return new Color(1f, 1f, 1f, 1f);
            }
            return new Color
            {
                r = c.r / num,
                g = c.g / num,
                b = c.b / num,
                a = 1f
            };
        }

        public void OnDisable()
        {
            if (this.m_Material != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_Material);
            }
            if (this.m_IdentityLut != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_IdentityLut);
            }
            if (this.m_InternalLut != null)
            {
                UnityEngine.Object.DestroyImmediate(this.internalLutRt);
            }
            if (this.m_SmallAdaptiveRt != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_SmallAdaptiveRt);
            }
            if (this.m_CurveTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_CurveTexture);
            }
            if (this.m_TonemapperCurve != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_TonemapperCurve);
            }
            this.m_Material = null;
            this.m_IdentityLut = null;
            this.m_InternalLut = null;
            this.m_SmallAdaptiveRt = null;
            this.m_CurveTexture = null;
            this.m_TonemapperCurve = null;
        }

        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            this.material.shaderKeywords = null;
            RenderTexture renderTexture = null;
            RenderTexture[] array = null;
            if (this.eyeAdaptation.enabled)
            {
                bool flag = this.CheckSmallAdaptiveRt();
                int num = (source.width >= source.height) ? source.height : source.width;
                int num2 = num | num >> 1;
                int num3 = num2 | num2 >> 2;
                int num4 = num3 | num3 >> 4;
                int num5 = num4 | num4 >> 8;
                int num6 = num5 | num5 >> 16;
                int num7 = num6 - (num6 >> 1);
                renderTexture = RenderTexture.GetTemporary(num7, num7, 0, this.m_AdaptiveRtFormat);
                Graphics.Blit(source, renderTexture);
                int num8 = (int)Mathf.Log(renderTexture.width, 2f);
                int num9 = 2;
                array = new RenderTexture[num8];
                for (int i = 0; i < num8; i++)
                {
                    array[i] = RenderTexture.GetTemporary(renderTexture.width / num9, renderTexture.width / num9, 0, this.m_AdaptiveRtFormat);
                    num9 <<= 1;
                }
                RenderTexture source2 = array[num8 - 1];
                Graphics.Blit(renderTexture, array[0], this.material, 1);
                for (int j = 0; j < num8 - 1; j++)
                {
                    Graphics.Blit(array[j], array[j + 1]);
                    source2 = array[j + 1];
                }
                this.m_SmallAdaptiveRt.MarkRestoreExpected();
                this.material.SetFloat("_AdaptationSpeed", Mathf.Max(this.eyeAdaptation.speed, 0.001f));
                Graphics.Blit(source2, this.m_SmallAdaptiveRt, this.material, (!flag) ? 2 : 3);
                this.material.SetFloat("_MiddleGrey", this.eyeAdaptation.middleGrey);
                this.material.SetFloat("_AdaptationMin", Mathf.Pow(2f, this.eyeAdaptation.min));
                this.material.SetFloat("_AdaptationMax", Mathf.Pow(2f, this.eyeAdaptation.max));
                this.material.SetTexture("_LumTex", this.m_SmallAdaptiveRt);
                this.material.EnableKeyword("ENABLE_EYE_ADAPTATION");
            }
            int num10 = 4;
            if (this.tonemapping.enabled)
            {
                if (this.tonemapping.tonemapper == Tonemapper.Curve)
                {
                    if (this.m_TonemapperDirty)
                    {
                        float num11 = 1f;
                        if (this.tonemapping.curve.length > 0)
                        {
                            num11 = this.tonemapping.curve[this.tonemapping.curve.length - 1].time;
                            for (float num12 = 0f; num12 <= 1f; num12 += 0.003921569f)
                            {
                                float num13 = this.tonemapping.curve.Evaluate(num12 * num11);
                                this.tonemapperCurve.SetPixel(Mathf.FloorToInt(num12 * 255f), 0, new Color(num13, num13, num13));
                            }
                            this.tonemapperCurve.Apply();
                        }
                        this.m_TonemapperCurveRange = 1f / num11;
                        this.m_TonemapperDirty = false;
                    }
                    this.material.SetFloat("_ToneCurveRange", this.m_TonemapperCurveRange);
                    this.material.SetTexture("_ToneCurve", this.tonemapperCurve);
                }
                else if (this.tonemapping.tonemapper == Tonemapper.Neutral)
                {
                    float num14 = this.tonemapping.neutralBlackIn * 20f + 1f;
                    float num15 = this.tonemapping.neutralBlackOut * 10f + 1f;
                    float num16 = this.tonemapping.neutralWhiteIn / 20f;
                    float num17 = 1f - this.tonemapping.neutralWhiteOut / 20f;
                    float t = num14 / num15;
                    float t2 = num16 / num17;
                    float y = Mathf.Max(0f, Mathf.LerpUnclamped(0.57f, 0.37f, t));
                    float z = Mathf.LerpUnclamped(0.01f, 0.24f, t2);
                    float w = Mathf.Max(0f, Mathf.LerpUnclamped(0.02f, 0.2f, t));
                    this.material.SetVector("_NeutralTonemapperParams1", new Vector4(0.2f, y, z, w));
                    this.material.SetVector("_NeutralTonemapperParams2", new Vector4(0.02f, 0.3f, this.tonemapping.neutralWhiteLevel, this.tonemapping.neutralWhiteClip / 10f));
                }
                this.material.SetFloat("_Exposure", this.tonemapping.exposure);
                num10 = (int)(num10 + (this.tonemapping.tonemapper + 1));
            }
            if (this.colorGrading.enabled)
            {
                if (this.m_Dirty || !this.m_InternalLut.IsCreated())
                {
                    Color c;
                    Color c2;
                    Color c3;
                    this.GenerateLiftGammaGain(out c, out c2, out c3);
                    this.GenCurveTexture();
                    this.material.SetVector("_WhiteBalance", this.GetWhiteBalance());
                    this.material.SetVector("_Lift", c);
                    this.material.SetVector("_Gamma", c2);
                    this.material.SetVector("_Gain", c3);
                    this.material.SetVector("_ContrastGainGamma", new Vector3(this.colorGrading.basics.contrast, this.colorGrading.basics.gain, 1f / this.colorGrading.basics.gamma));
                    this.material.SetFloat("_Vibrance", this.colorGrading.basics.vibrance);
                    this.material.SetVector("_HSV", new Vector4(this.colorGrading.basics.hue, this.colorGrading.basics.saturation, this.colorGrading.basics.value));
                    this.material.SetVector("_ChannelMixerRed", this.colorGrading.channelMixer.channels[0]);
                    this.material.SetVector("_ChannelMixerGreen", this.colorGrading.channelMixer.channels[1]);
                    this.material.SetVector("_ChannelMixerBlue", this.colorGrading.channelMixer.channels[2]);
                    this.material.SetTexture("_CurveTex", this.curveTexture);
                    this.internalLutRt.MarkRestoreExpected();
                    Graphics.Blit(this.identityLut, this.internalLutRt, this.material, 0);
                    this.m_Dirty = false;
                }
                this.material.EnableKeyword("ENABLE_COLOR_GRADING");
                if (this.colorGrading.useDithering)
                {
                    this.material.EnableKeyword("ENABLE_DITHERING");
                }
                this.material.SetTexture("_InternalLutTex", this.internalLutRt);
                this.material.SetVector("_InternalLutParams", new Vector3(1f / this.internalLutRt.width, 1f / this.internalLutRt.height, this.internalLutRt.height - 1f));
            }
            if (this.lut.enabled && this.lut.texture != null && this.CheckUserLut())
            {
                this.material.SetTexture("_UserLutTex", this.lut.texture);
                this.material.SetVector("_UserLutParams", new Vector4(1f / this.lut.texture.width, 1f / this.lut.texture.height, this.lut.texture.height - 1f, this.lut.contribution));
                this.material.EnableKeyword("ENABLE_USER_LUT");
            }
            Graphics.Blit(source, destination, this.material, num10);
            if (this.eyeAdaptation.enabled)
            {
                for (int k = 0; k < array.Length; k++)
                {
                    RenderTexture.ReleaseTemporary(array[k]);
                }
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        public void SetTonemapperDirty()
        {
            this.m_TonemapperDirty = true;
        }

        private static Texture2D GenerateIdentityLut(int dim)
        {
            Color[] array = new Color[dim * dim * dim];
            float num = 1f / (dim - 1f);
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int k = 0; k < dim; k++)
                    {
                        array[i + j * dim + k * dim * dim] = new Color(i * num, Mathf.Abs(k * num), j * num, 1f);
                    }
                }
            }
            Texture2D texture2D = new Texture2D(dim * dim, dim, TextureFormat.RGB24, false, true);
            texture2D.name = "Identity LUT";
            texture2D.filterMode = FilterMode.Bilinear;
            texture2D.anisoLevel = 0;
            texture2D.hideFlags = HideFlags.DontSave;
            texture2D.SetPixels(array);
            texture2D.Apply();
            return texture2D;
        }

        private float StandardIlluminantY(float x)
        {
            return 2.87f * x - 3f * x * x - 0.27509508f;
        }

        private Vector3 CIExyToLMS(float x, float y)
        {
            float num = 1f;
            float num2 = num * x / y;
            float num3 = num * (1f - x - y) / y;
            float x2 = 0.7328f * num2 + 0.4296f * num - 0.1624f * num3;
            float y2 = -0.7036f * num2 + 1.6975f * num + 0.0061f * num3;
            float z = 0.003f * num2 + 0.0136f * num + 0.9834f * num3;
            return new Vector3(x2, y2, z);
        }

        private Vector3 GetWhiteBalance()
        {
            float temperatureShift = this.colorGrading.basics.temperatureShift;
            float tint = this.colorGrading.basics.tint;
            float x = 0.31271f - temperatureShift * ((temperatureShift >= 0f) ? 0.05f : 0.1f);
            float y = this.StandardIlluminantY(x) + tint * 0.05f;
            Vector3 vector = new Vector3(0.949237f, 1.03542f, 1.08728f);
            Vector3 vector2 = this.CIExyToLMS(x, y);
            return new Vector3(vector.x / vector2.x, vector.y / vector2.y, vector.z / vector2.z);
        }

        private void GenerateLiftGammaGain(out Color lift, out Color gamma, out Color gain)
        {
            Color color = NormalizeColor(this.colorGrading.colorWheels.shadows);
            Color color2 = NormalizeColor(this.colorGrading.colorWheels.midtones);
            Color color3 = NormalizeColor(this.colorGrading.colorWheels.highlights);
            float num = (color.r + color.g + color.b) / 3f;
            float num2 = (color2.r + color2.g + color2.b) / 3f;
            float num3 = (color3.r + color3.g + color3.b) / 3f;
            float r = (color.r - num) * 0.1f;
            float g = (color.g - num) * 0.1f;
            float b = (color.b - num) * 0.1f;
            float b2 = Mathf.Pow(2f, (color2.r - num2) * 0.5f);
            float b3 = Mathf.Pow(2f, (color2.g - num2) * 0.5f);
            float b4 = Mathf.Pow(2f, (color2.b - num2) * 0.5f);
            float r2 = Mathf.Pow(2f, (color3.r - num3) * 0.5f);
            float g2 = Mathf.Pow(2f, (color3.g - num3) * 0.5f);
            float b5 = Mathf.Pow(2f, (color3.b - num3) * 0.5f);
            float r3 = 1f / Mathf.Max(0.01f, b2);
            float g3 = 1f / Mathf.Max(0.01f, b3);
            float b6 = 1f / Mathf.Max(0.01f, b4);
            lift = new Color(r, g, b);
            gamma = new Color(r3, g3, b6);
            gain = new Color(r2, g2, b5);
        }

        private void GenCurveTexture()
        {
            AnimationCurve master = this.colorGrading.curves.master;
            AnimationCurve red = this.colorGrading.curves.red;
            AnimationCurve green = this.colorGrading.curves.green;
            AnimationCurve blue = this.colorGrading.curves.blue;
            Color[] array = new Color[256];
            for (float num = 0f; num <= 1f; num += 0.003921569f)
            {
                float a = Mathf.Clamp(master.Evaluate(num), 0f, 1f);
                float r = Mathf.Clamp(red.Evaluate(num), 0f, 1f);
                float g = Mathf.Clamp(green.Evaluate(num), 0f, 1f);
                float b = Mathf.Clamp(blue.Evaluate(num), 0f, 1f);
                array[(int)Mathf.Floor(num * 255f)] = new Color(r, g, b, a);
            }
            this.curveTexture.SetPixels(array);
            this.curveTexture.Apply();
        }

        private bool CheckUserLut()
        {
            this.validUserLutSize = (this.lut.texture.height == (int)Mathf.Sqrt(this.lut.texture.width));
            return this.validUserLutSize;
        }

        private bool CheckSmallAdaptiveRt()
        {
            if (this.m_SmallAdaptiveRt != null)
            {
                return false;
            }
            this.m_AdaptiveRtFormat = RenderTextureFormat.ARGBHalf;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf))
            {
                this.m_AdaptiveRtFormat = RenderTextureFormat.RGHalf;
            }
            this.m_SmallAdaptiveRt = new RenderTexture(1, 1, 0, this.m_AdaptiveRtFormat);
            this.m_SmallAdaptiveRt.hideFlags = HideFlags.DontSave;
            return true;
        }

        public Texture2D BakeLUT()
        {
            Texture2D texture2D = new Texture2D(this.internalLutRt.width, this.internalLutRt.height, TextureFormat.RGB24, false, true);
            RenderTexture.active = this.internalLutRt;
            texture2D.ReadPixels(new Rect(0f, 0f, texture2D.width, texture2D.height), 0, 0);
            RenderTexture.active = null;
            return texture2D;
        }

        public EyeAdaptationSettings eyeAdaptation { get { return this.m_EyeAdaptation; } set { this.m_EyeAdaptation = value; } }

        public TonemappingSettings tonemapping
        {
            get { return this.m_Tonemapping; }
            set
            {
                this.m_Tonemapping = value;
                this.SetTonemapperDirty();
            }
        }

        public ColorGradingSettings colorGrading
        {
            get { return this.m_ColorGrading; }
            set
            {
                this.m_ColorGrading = value;
                this.SetDirty();
            }
        }

        public LUTSettings lut { get { return this.m_Lut; } set { this.m_Lut = value; } }

        private Texture2D identityLut
        {
            get
            {
                if (this.m_IdentityLut == null || this.m_IdentityLut.height != this.lutSize)
                {
                    UnityEngine.Object.DestroyImmediate(this.m_IdentityLut);
                    this.m_IdentityLut = GenerateIdentityLut(this.lutSize);
                }
                return this.m_IdentityLut;
            }
        }

        private RenderTexture internalLutRt
        {
            get
            {
                if (this.m_InternalLut == null || !this.m_InternalLut.IsCreated() || this.m_InternalLut.height != this.lutSize)
                {
                    UnityEngine.Object.DestroyImmediate(this.m_InternalLut);
                    this.m_InternalLut = new RenderTexture(this.lutSize * this.lutSize, this.lutSize, 0, RenderTextureFormat.ARGB32)
                    {
                        name = "Internal LUT",
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };
                }
                return this.m_InternalLut;
            }
        }

        private Texture2D curveTexture
        {
            get
            {
                if (this.m_CurveTexture == null)
                {
                    this.m_CurveTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, true)
                    {
                        name = "Curve texture",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };
                }
                return this.m_CurveTexture;
            }
        }

        private Texture2D tonemapperCurve
        {
            get
            {
                if (this.m_TonemapperCurve == null)
                {
                    TextureFormat format = TextureFormat.RGB24;
                    if (SystemInfo.SupportsTextureFormat(TextureFormat.RFloat))
                    {
                        format = TextureFormat.RFloat;
                    }
                    else if (SystemInfo.SupportsTextureFormat(TextureFormat.RHalf))
                    {
                        format = TextureFormat.RHalf;
                    }
                    this.m_TonemapperCurve = new Texture2D(256, 1, format, false, true)
                    {
                        name = "Tonemapper curve texture",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };
                }
                return this.m_TonemapperCurve;
            }
        }

        public Shader shader
        {
            get
            {
                if (this.m_Shader == null)
                {
                    this.m_Shader = HSLRE.self._resources.LoadAsset<Shader>("TonemappingColorGrading");
                }
                return this.m_Shader;
            }
        }

        public Material material
        {
            get
            {
                if (this.m_Material == null)
                {
                    this.m_Material = ImageEffectHelper.CheckShaderAndCreateMaterial(this.shader);
                }
                return this.m_Material;
            }
        }

        public bool isGammaColorSpace { get { return QualitySettings.activeColorSpace == ColorSpace.Gamma; } }

        public int lutSize { get { return (int)this.colorGrading.precision; } }

        public bool validRenderTextureFormat { get; private set; }

        public bool validUserLutSize { get; private set; }

        private bool m_Dirty;

        [SerializeField]
        [SettingsGroup]
        private EyeAdaptationSettings m_EyeAdaptation;

        [SerializeField]
        [SettingsGroup]
        private TonemappingSettings m_Tonemapping;

        [SettingsGroup]
        [SerializeField]
        private ColorGradingSettings m_ColorGrading;

        [SerializeField]
        [SettingsGroup]
        private LUTSettings m_Lut;

        private Texture2D m_IdentityLut;

        private RenderTexture m_InternalLut;

        private Texture2D m_CurveTexture;

        private Texture2D m_TonemapperCurve;

        private float m_TonemapperCurveRange;

        [SerializeField]
        private Shader m_Shader;

        private Material m_Material;

        private bool m_TonemapperDirty;

        private RenderTexture m_SmallAdaptiveRt;

        private RenderTextureFormat m_AdaptiveRtFormat;

        [AttributeUsage(AttributeTargets.Field)]
        public class SettingsGroup : Attribute
        {
        }

        public class ColorWheelGroup : PropertyAttribute
        {
            public ColorWheelGroup()
            {
            }

            public ColorWheelGroup(int minSizePerWheel, int maxSizePerWheel)
            {
                this.minSizePerWheel = minSizePerWheel;
                this.maxSizePerWheel = maxSizePerWheel;
            }

            public int minSizePerWheel = 60;

            public int maxSizePerWheel = 150;
        }

        public class IndentedGroup : PropertyAttribute
        {
        }

        public class ChannelMixer : PropertyAttribute
        {
        }

        public class Curve : PropertyAttribute
        {
            public Curve()
            {
            }

            public Curve(float r, float g, float b, float a)
            {
                this.color = new Color(r, g, b, a);
            }

            public Color color = Color.white;
        }

        [Serializable]
        public struct EyeAdaptationSettings
        {
            public static EyeAdaptationSettings defaultSettings
            {
                get
                {
                    return new EyeAdaptationSettings
                    {
                        enabled = false,
                        showDebug = false,
                        middleGrey = 0.12f,
                        min = -3f,
                        max = 3f,
                        speed = 1.5f
                    };
                }
            }

            public bool enabled;

            [Min(0f)]
            [Tooltip("Midpoint Adjustment.")]
            public float middleGrey;

            [Tooltip("The lowest possible exposure value; adjust this value to modify the brightest areas of your level.")]
            public float min;

            [Tooltip("The highest possible exposure value; adjust this value to modify the darkest areas of your level.")]
            public float max;

            [Min(0f)]
            [Tooltip("Speed of linear adaptation. Higher is faster.")]
            public float speed;

            [Tooltip("Displays a luminosity helper in the GameView.")]
            public bool showDebug;
        }

        public enum Tonemapper
        {
            ACES,
            Curve,
            Hable,
            HejlDawson,
            Photographic,
            Reinhard,
            Neutral
        }

        [Serializable]
        public struct TonemappingSettings
        {
            public static TonemappingSettings defaultSettings
            {
                get
                {
                    return new TonemappingSettings
                    {
                        enabled = true,
                        tonemapper = Tonemapper.ACES,
                        exposure = 1f,
                        curve = CurvesSettings.defaultCurve,
                        neutralBlackIn = 0.02f,
                        neutralWhiteIn = 10f,
                        neutralBlackOut = 0f,
                        neutralWhiteOut = 10f,
                        neutralWhiteLevel = 5.3f,
                        neutralWhiteClip = 10f
                    };
                }
            }

            public bool enabled;

            [Tooltip("Tonemapping technique to use. ACES is the recommended one.")]
            public Tonemapper tonemapper;

            [Tooltip("Adjusts the overall exposure of the scene.")]
            [Min(0f)]
            public float exposure;

            [Tooltip("Custom tonemapping curve.")]
            public AnimationCurve curve;

            [Range(-0.1f, 0.1f)]
            public float neutralBlackIn;

            [Range(1f, 20f)]
            public float neutralWhiteIn;

            [Range(-0.09f, 0.1f)]
            public float neutralBlackOut;

            [Range(1f, 19f)]
            public float neutralWhiteOut;

            [Range(0.1f, 20f)]
            public float neutralWhiteLevel;

            [Range(1f, 10f)]
            public float neutralWhiteClip;
        }

        [Serializable]
        public struct LUTSettings
        {
            public static LUTSettings defaultSettings
            {
                get
                {
                    return new LUTSettings
                    {
                        enabled = false,
                        texture = null,
                        contribution = 1f
                    };
                }
            }

            public bool enabled;

            [Tooltip("Custom lookup texture (strip format, e.g. 256x16).")]
            public Texture texture;

            [Tooltip("Blending factor.")]
            [Range(0f, 1f)]
            public float contribution;
        }

        [Serializable]
        public struct ColorWheelsSettings
        {
            public static ColorWheelsSettings defaultSettings
            {
                get
                {
                    return new ColorWheelsSettings
                    {
                        shadows = Color.white,
                        midtones = Color.white,
                        highlights = Color.white
                    };
                }
            }

            [ColorUsage(false)]
            public Color shadows;

            [ColorUsage(false)]
            public Color midtones;

            [ColorUsage(false)]
            public Color highlights;
        }

        [Serializable]
        public struct BasicsSettings
        {
            public static BasicsSettings defaultSettings
            {
                get
                {
                    return new BasicsSettings
                    {
                        temperatureShift = 0f,
                        tint = 0f,
                        contrast = 1f,
                        hue = 0f,
                        saturation = 1f,
                        value = 1f,
                        vibrance = 0f,
                        gain = 1f,
                        gamma = 1f
                    };
                }
            }

            [Tooltip("Sets the white balance to a custom color temperature.")]
            [Range(-2f, 2f)]
            public float temperatureShift;

            [Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
            [Range(-2f, 2f)]
            public float tint;

            [Tooltip("Shift the hue of all colors.")]
            [Space]
            [Range(-0.5f, 0.5f)]
            public float hue;

            [Range(0f, 2f)]
            [Tooltip("Pushes the intensity of all colors.")]
            public float saturation;

            [Tooltip("Adjusts the saturation so that clipping is minimized as colors approach full saturation.")]
            [Range(-1f, 1f)]
            public float vibrance;

            [Tooltip("Brightens or darkens all colors.")]
            [Range(0f, 10f)]
            public float value;

            [Range(0f, 2f)]
            [Tooltip("Expands or shrinks the overall range of tonal values.")]
            [Space]
            public float contrast;

            [Range(0.01f, 5f)]
            [Tooltip("Contrast gain curve. Controls the steepness of the curve.")]
            public float gain;

            [Range(0.01f, 5f)]
            [Tooltip("Applies a pow function to the source.")]
            public float gamma;
        }

        [Serializable]
        public struct ChannelMixerSettings
        {
            public static ChannelMixerSettings defaultSettings
            {
                get
                {
                    return new ChannelMixerSettings
                    {
                        currentChannel = 0,
                        channels = new Vector3[]
                        {
                            new Vector3(1f, 0f, 0f),
                            new Vector3(0f, 1f, 0f),
                            new Vector3(0f, 0f, 1f)
                        }
                    };
                }
            }

            public int currentChannel;

            public Vector3[] channels;
        }

        [Serializable]
        public struct CurvesSettings
        {
            public static CurvesSettings defaultSettings
            {
                get
                {
                    return new CurvesSettings
                    {
                        master = defaultCurve,
                        red = defaultCurve,
                        green = defaultCurve,
                        blue = defaultCurve
                    };
                }
            }

            public static AnimationCurve defaultCurve
            {
                get
                {
                    return new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(0f, 0f, 1f, 1f),
                        new Keyframe(1f, 1f, 1f, 1f)
                    });
                }
            }

            [Curve]
            public AnimationCurve master;

            [Curve(1f, 0f, 0f, 1f)]
            public AnimationCurve red;

            [Curve(0f, 1f, 0f, 1f)]
            public AnimationCurve green;

            [Curve(0f, 1f, 1f, 1f)]
            public AnimationCurve blue;
        }

        public enum ColorGradingPrecision
        {
            Normal = 16,
            High = 32
        }

        [Serializable]
        public struct ColorGradingSettings
        {
            public static ColorGradingSettings defaultSettings
            {
                get
                {
                    return new ColorGradingSettings
                    {
                        enabled = true,
                        useDithering = true,
                        showDebug = false,
                        precision = ColorGradingPrecision.High,
                        colorWheels = ColorWheelsSettings.defaultSettings,
                        basics = BasicsSettings.defaultSettings,
                        channelMixer = ChannelMixerSettings.defaultSettings,
                        curves = CurvesSettings.defaultSettings
                    };
                }
            }

            internal void Reset()
            {
                this.curves = CurvesSettings.defaultSettings;
            }

            public bool enabled;

            [Tooltip("Internal LUT precision. \"Normal\" is 256x16, \"High\" is 1024x32. Prefer \"Normal\" on mobile devices.")]
            public ColorGradingPrecision precision;

            [Space]
            [ColorWheelGroup]
            public ColorWheelsSettings colorWheels;

            [IndentedGroup]
            [Space]
            public BasicsSettings basics;

            [Space]
            [ChannelMixer]
            public ChannelMixerSettings channelMixer;

            [IndentedGroup]
            [Space]
            public CurvesSettings curves;

            [Tooltip("Use dithering to try and minimize color banding in dark areas.")]
            [Space]
            public bool useDithering;

            [Tooltip("Displays the generated LUT in the top left corner of the GameView.")]
            public bool showDebug;
        }

        private enum Pass
        {
            LutGen,
            AdaptationLog,
            AdaptationExpBlend,
            AdaptationExp,
            TonemappingOff,
            TonemappingACES,
            TonemappingCurve,
            TonemappingHable,
            TonemappingHejlDawson,
            TonemappingPhotographic,
            TonemappingReinhard,
            TonemappingNeutral,
            AdaptationDebug
        }
    }
}
