using System;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;

namespace HSLRE.CustomEffects
{
    public class Bloom
    {
        public Shader shader
        {
            get
            {
                if (this.m_Shader == null)
                {
                    this.m_Shader = HSLRE.self._resources.LoadAsset<Shader>("Bloom");
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
            //if (!ImageEffectHelper.IsSupported(this.shader, true, false, this))
            //{
            //    base.enabled = false;
            //}
            this.settings.highQuality = true;
            //this.settings.antiFlicker = GraphicSetting.BloomSettings.antiFlicker;
            //this.settings.intensity = GraphicSetting.BloomSettings.intensity;
            //this.settings.radius = GraphicSetting.BloomSettings.raduis;
            //this.settings.softKnee = GraphicSetting.BloomSettings.softKnee;
            //this.settings.threshold = GraphicSetting.BloomSettings.threshold;
        }

        public void OnDisable()
        {
            if (this.m_Material != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_Material);
            }
            this.m_Material = null;
        }

        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            bool isMobilePlatform = Application.isMobilePlatform;
            int num = source.width;
            int num2 = source.height;
            if (!this.settings.highQuality)
            {
                num /= 2;
                num2 /= 2;
            }
            RenderTextureFormat format = isMobilePlatform ? RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR;
            float num3 = Mathf.Log((float)num2, 2f) + this.settings.radius - 8f;
            int num4 = (int)num3;
            int num5 = Mathf.Clamp(num4, 1, 16);
            float thresholdLinear = this.settings.thresholdLinear;
            this.material.SetFloat("_Threshold", thresholdLinear);
            float num6 = thresholdLinear * this.settings.softKnee + 1E-05f;
            Vector3 v = new Vector3(thresholdLinear - num6, num6 * 2f, 0.25f / num6);
            this.material.SetVector("_Curve", v);
            bool flag = !this.settings.highQuality && this.settings.antiFlicker;
            this.material.SetFloat("_PrefilterOffs", flag ? -0.5f : 0f);
            this.material.SetFloat("_SampleScale", 0.5f + num3 - (float)num4);
            this.material.SetFloat("_Intensity", Mathf.Max(0f, this.settings.intensity));
            RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 0, format);
            Graphics.Blit(source, temporary, this.material, this.settings.antiFlicker ? 1 : 0);
            RenderTexture renderTexture = temporary;
            for (int i = 0; i < num5; i++)
            {
                this.m_blurBuffer1[i] = RenderTexture.GetTemporary(renderTexture.width / 2, renderTexture.height / 2, 0, format);
                Graphics.Blit(renderTexture, this.m_blurBuffer1[i], this.material, (i == 0) ? (this.settings.antiFlicker ? 3 : 2) : 4);
                renderTexture = this.m_blurBuffer1[i];
            }
            for (int j = num5 - 2; j >= 0; j--)
            {
                RenderTexture renderTexture2 = this.m_blurBuffer1[j];
                this.material.SetTexture("_BaseTex", renderTexture2);
                this.m_blurBuffer2[j] = RenderTexture.GetTemporary(renderTexture2.width, renderTexture2.height, 0, format);
                Graphics.Blit(renderTexture, this.m_blurBuffer2[j], this.material, this.settings.highQuality ? 6 : 5);
                renderTexture = this.m_blurBuffer2[j];
            }
            this.material.SetTexture("_BaseTex", source);
            Graphics.Blit(renderTexture, destination, this.material, this.settings.highQuality ? 8 : 7);
            for (int k = 0; k < 16; k++)
            {
                if (this.m_blurBuffer1[k] != null)
                {
                    RenderTexture.ReleaseTemporary(this.m_blurBuffer1[k]);
                }
                if (this.m_blurBuffer2[k] != null)
                {
                    RenderTexture.ReleaseTemporary(this.m_blurBuffer2[k]);
                }
                this.m_blurBuffer1[k] = null;
                this.m_blurBuffer2[k] = null;
            }
            RenderTexture.ReleaseTemporary(temporary);
        }

        [SerializeField]
        public Bloom.Settings settings = Bloom.Settings.defaultSettings;

        [SerializeField]
        [HideInInspector]
        private Shader m_Shader;

        private Material m_Material;

        private const int kMaxIterations = 16;

        private readonly RenderTexture[] m_blurBuffer1 = new RenderTexture[16];

        private readonly RenderTexture[] m_blurBuffer2 = new RenderTexture[16];

        [Serializable]
        public struct Settings
        {
            public float thresholdGamma { get { return Mathf.Max(0f, this.threshold); } set { this.threshold = value; } }

            public float thresholdLinear { get { return Mathf.GammaToLinearSpace(this.thresholdGamma); } set { this.threshold = Mathf.LinearToGammaSpace(value); } }

            public static Bloom.Settings defaultSettings
            {
                get
                {
                    return new Bloom.Settings
                    {
                        threshold = 0.9f,
                        softKnee = 0.5f,
                        radius = 2f,
                        intensity = 0.7f,
                        highQuality = true,
                        antiFlicker = false
                    };
                }
            }

            [SerializeField]
            [Tooltip("Filters out pixels under this level of brightness.")]
            public float threshold;

            [SerializeField]
            [Range(0f, 1f)]
            [Tooltip("Makes transition between under/over-threshold gradual.")]
            public float softKnee;

            [SerializeField]
            [Range(1f, 7f)]
            [Tooltip("Changes extent of veiling effects in a screen resolution-independent fashion.")]
            public float radius;

            [SerializeField]
            [Tooltip("Blend factor of the result image.")]
            public float intensity;

            [SerializeField]
            [Tooltip("Controls filter quality and buffer resolution.")]
            public bool highQuality;

            [SerializeField]
            [Tooltip("Reduces flashing noise with an additional filter.")]
            public bool antiFlicker;
        }
    }
}
