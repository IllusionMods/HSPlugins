using System;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;

namespace HSLRE.CustomEffects
{
    public class LensAberrations
    {
        public Shader shader
        {
            get
            {
                if (this.m_Shader == null)
                {
                    this.m_Shader = HSLRE.self._resources.LoadAsset<Shader>("LensAberrations");
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

        public void OnEnable()
        {
            //if (!ImageEffectHelper.IsSupported(this.shader, false, false, this))
            //{
            //    base.enabled = false;
            //}
            //this.distortion = GraphicSetting.MyDistortionSettings;
            //this.vignette = GraphicSetting.MyVignetteSettings;
            //this.chromaticAberration = GraphicSetting.MyChromaticAberrationSettings;
            this.m_RTU = new RenderTextureUtility();
        }

        public void OnDisable()
        {
            if (this.m_Material != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_Material);
            }
            this.m_Material = null;
            this.m_RTU.ReleaseAllTemporaryRenderTextures();
        }

        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!this.vignette.enabled && !this.chromaticAberration.enabled && !this.distortion.enabled)
            {
                Graphics.Blit(source, destination);
                return;
            }
            this.material.shaderKeywords = null;
            if (this.distortion.enabled)
            {
                float val = 1.6f * Math.Max(Mathf.Abs(this.distortion.amount), 1f);
                float num = 0.017453292f * Math.Min(160f, val);
                float y = 2f * Mathf.Tan(num * 0.5f);
                Vector4 vector = new Vector4(this.distortion.centerX, this.distortion.centerY, Mathf.Max(this.distortion.amountX, 0.0001f), Mathf.Max(this.distortion.amountY, 0.0001f));
                Vector3 v = new Vector3((this.distortion.amount >= 0f) ? num : (1f / num), y, 1f / this.distortion.scale);
                this.material.EnableKeyword((this.distortion.amount >= 0f) ? "DISTORT" : "UNDISTORT");
                this.material.SetVector("_DistCenterScale", vector);
                this.material.SetVector("_DistAmount", v);
            }
            if (this.chromaticAberration.enabled)
            {
                this.material.EnableKeyword("CHROMATIC_ABERRATION");
                Vector4 vector2 = new Vector4(this.chromaticAberration.color.r, this.chromaticAberration.color.g, this.chromaticAberration.color.b, this.chromaticAberration.amount * 0.001f);
                this.material.SetVector("_ChromaticAberration", vector2);
            }
            if (this.vignette.enabled)
            {
                this.material.SetColor("_VignetteColor", this.vignette.color);
                if (this.vignette.blur > 0f)
                {
                    int num2 = source.width / 2;
                    int num3 = source.height / 2;
                    RenderTexture temporaryRenderTexture = this.m_RTU.GetTemporaryRenderTexture(num2, num3, 0, source.format, FilterMode.Bilinear);
                    RenderTexture temporaryRenderTexture2 = this.m_RTU.GetTemporaryRenderTexture(num2, num3, 0, source.format, FilterMode.Bilinear);
                    this.material.SetVector("_BlurPass", new Vector2(1f / (float)num2, 0f));
                    Graphics.Blit(source, temporaryRenderTexture, this.material, 0);
                    if (this.distortion.enabled)
                    {
                        this.material.DisableKeyword("DISTORT");
                        this.material.DisableKeyword("UNDISTORT");
                    }
                    this.material.SetVector("_BlurPass", new Vector2(0f, 1f / (float)num3));
                    Graphics.Blit(temporaryRenderTexture, temporaryRenderTexture2, this.material, 0);
                    this.material.SetVector("_BlurPass", new Vector2(1f / (float)num2, 0f));
                    Graphics.Blit(temporaryRenderTexture2, temporaryRenderTexture, this.material, 0);
                    this.material.SetVector("_BlurPass", new Vector2(0f, 1f / (float)num3));
                    Graphics.Blit(temporaryRenderTexture, temporaryRenderTexture2, this.material, 0);
                    this.material.SetTexture("_BlurTex", temporaryRenderTexture2);
                    this.material.SetFloat("_VignetteBlur", this.vignette.blur * 3f);
                    this.material.EnableKeyword("VIGNETTE_BLUR");
                    if (this.distortion.enabled)
                    {
                        this.material.EnableKeyword((this.distortion.amount >= 0f) ? "DISTORT" : "UNDISTORT");
                    }
                }

                if (this.vignette.desaturate > 0f)
                {
                    this.material.EnableKeyword("VIGNETTE_DESAT");
                    this.material.SetFloat("_VignetteDesat", 1f - this.vignette.desaturate);
                }
                this.material.SetVector("_VignetteCenter", this.vignette.center);
                if (Mathf.Approximately(this.vignette.roundness, 1f))
                {
                    this.material.EnableKeyword("VIGNETTE_CLASSIC");
                    this.material.SetVector("_VignetteSettings", new Vector2(this.vignette.intensity, this.vignette.smoothness));
                }
                else
                {
                    this.material.EnableKeyword("VIGNETTE_FILMIC");
                    float z = (1f - this.vignette.roundness) * 6f + this.vignette.roundness;
                    this.material.SetVector("_VignetteSettings", new Vector3(this.vignette.intensity, this.vignette.smoothness, z));
                }
            }
            int pass = 0;
            if (this.vignette.enabled && this.chromaticAberration.enabled && this.distortion.enabled)
            {
                pass = 7;
            }
            else if (this.vignette.enabled && this.chromaticAberration.enabled)
            {
                pass = 5;
            }
            else if (this.vignette.enabled && this.distortion.enabled)
            {
                pass = 6;
            }
            else if (this.chromaticAberration.enabled && this.distortion.enabled)
            {
                pass = 4;
            }
            else if (this.vignette.enabled)
            {
                pass = 3;
            }
            else if (this.chromaticAberration.enabled)
            {
                pass = 1;
            }
            else if (this.distortion.enabled)
            {
                pass = 2;
            }
            Graphics.Blit(source, destination, this.material, pass);
            this.m_RTU.ReleaseAllTemporaryRenderTextures();
        }

        [LensAberrations.SettingsGroup]
        public LensAberrations.DistortionSettings distortion = LensAberrations.DistortionSettings.defaultSettings;

        [LensAberrations.SettingsGroup]
        public LensAberrations.VignetteSettings vignette = LensAberrations.VignetteSettings.defaultSettings;

        [LensAberrations.SettingsGroup]
        public LensAberrations.ChromaticAberrationSettings chromaticAberration = LensAberrations.ChromaticAberrationSettings.defaultSettings;

        [SerializeField]
        private Shader m_Shader;

        private Material m_Material;

        private RenderTextureUtility m_RTU;

        [AttributeUsage(AttributeTargets.Field)]
        public class SettingsGroup : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class AdvancedSetting : Attribute
        {
        }

        [Serializable]
        public struct DistortionSettings
        {
            public static LensAberrations.DistortionSettings defaultSettings
            {
                get
                {
                    return new LensAberrations.DistortionSettings
                    {
                        enabled = false,
                        amount = 0f,
                        centerX = 0f,
                        centerY = 0f,
                        amountX = 1f,
                        amountY = 1f,
                        scale = 1f
                    };
                }
            }

            public bool enabled;

            [Range(-100f, 100f)]
            [Tooltip("Distortion amount.")]
            public float amount;

            [Range(-1f, 1f)]
            [Tooltip("Distortion center point (X axis).")]
            public float centerX;

            [Range(-1f, 1f)]
            [Tooltip("Distortion center point (Y axis).")]
            public float centerY;

            [Range(0f, 1f)]
            [Tooltip("Amount multiplier on X axis. Set it to 0 to disable distortion on this axis.")]
            public float amountX;

            [Range(0f, 1f)]
            [Tooltip("Amount multiplier on Y axis. Set it to 0 to disable distortion on this axis.")]
            public float amountY;

            [Range(0.01f, 5f)]
            [Tooltip("Global screen scaling.")]
            public float scale;
        }

        [Serializable]
        public struct VignetteSettings
        {
            public static LensAberrations.VignetteSettings defaultSettings
            {
                get
                {
                    return new LensAberrations.VignetteSettings
                    {
                        enabled = false,
                        color = new Color(0f, 0f, 0f, 1f),
                        center = new Vector2(0.5f, 0.5f),
                        intensity = 1.4f,
                        smoothness = 0.8f,
                        roundness = 1f,
                        blur = 0f,
                        desaturate = 0f
                    };
                }
            }

            public bool enabled;

            [ColorUsage(false)]
            [Tooltip("Vignette color. Use the alpha channel for transparency.")]
            public Color color;

            [Tooltip("Sets the vignette center point (screen center is [0.5,0.5]).")]
            public Vector2 center;

            [Range(0f, 3f)]
            [Tooltip("Amount of vignetting on screen.")]
            public float intensity;

            [Range(0.01f, 3f)]
            [Tooltip("Smoothness of the vignette borders.")]
            public float smoothness;

            [LensAberrations.AdvancedSetting]
            [Range(0f, 1f)]
            [Tooltip("Lower values will make a square-ish vignette.")]
            public float roundness;

            [Range(0f, 1f)]
            [Tooltip("Blurs the corners of the screen. Leave this at 0 to disable it.")]
            public float blur;

            [Range(0f, 1f)]
            [Tooltip("Desaturate the corners of the screen. Leave this to 0 to disable it.")]
            public float desaturate;
        }

        [Serializable]
        public struct ChromaticAberrationSettings
        {
            public static LensAberrations.ChromaticAberrationSettings defaultSettings
            {
                get
                {
                    return new LensAberrations.ChromaticAberrationSettings
                    {
                        enabled = false,
                        color = Color.green,
                        amount = 0f
                    };
                }
            }

            public bool enabled;

            [ColorUsage(false)]
            [Tooltip("Channels to apply chromatic aberration to.")]
            public Color color;

            [Range(-50f, 50f)]
            [Tooltip("Amount of tangential distortion.")]
            public float amount;
        }

        private enum Pass
        {
            BlurPrePass,
            Chroma,
            Distort,
            Vignette,
            ChromaDistort,
            ChromaVignette,
            DistortVignette,
            ChromaDistortVignette
        }
    }
}
