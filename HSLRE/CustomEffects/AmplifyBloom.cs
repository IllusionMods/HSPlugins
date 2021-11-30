using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace HSLRE.CustomEffects
{
    public enum PrecisionModes
    {
        Low = 0,
        High
    }

    public enum BloomPasses
    {
        Threshold = 0,
        ThresholdMask = 1,
        AnamorphicGlare = 2,
        LensFlare0 = 3,
        LensFlare1 = 4,
        LensFlare2 = 5,
        LensFlare3 = 6,
        LensFlare4 = 7,
        LensFlare5 = 8,
        DownsampleNoWeightedAvg = 9,
        DownsampleWithKaris = 10,
        DownsampleWithoutKaris = 11,
        DownsampleWithTempFilterWithKaris = 12,
        DownsampleWithTempFilterWithoutKaris = 13,
        HorizontalBlur = 14,
        VerticalBlur = 15,
        VerticalBlurWithTempFilter = 16,
        UpscaleFirstPass = 17,
        Upscale = 18,
        WeightedAddPS1 = 19,
        WeightedAddPS2 = 20,
        WeightedAddPS3 = 21,
        WeightedAddPS4 = 22,
        WeightedAddPS5 = 23,
        WeightedAddPS6 = 24,
        WeightedAddPS7 = 25,
        WeightedAddPS8 = 26,
        BokehWeightedBlur = 27,
        BokehComposition2S = 28,
        BokehComposition3S = 29,
        BokehComposition4S = 30,
        BokehComposition5S = 31,
        BokehComposition6S = 32,
        Decode = 33,
        TotalPasses
    };

    public enum UpscaleQualityEnum
    {
        Realistic,
        Natural
    }

    public enum DebugToScreenEnum
    {
        None,
        Bloom,
        MainThreshold,
        FeaturesThreshold,
        TemporalFilter,
        BokehFilter,
        LensFlare,
        LensGlare,
        LensDirt,
        LensStarburst
    }

    public enum MainThresholdSizeEnum
    {
        Full = 0,
        Half,
        Quarter

    }

    public enum LensDirtTextureEnum
    {
        Dirt_Simple_1,
        Dirt_Simple_2,
        Dirt_Simple_3,
        Dirt_Simple_4,
        Dirt_Simple_5,
        Dirt_Simple_6,
        Dirt_Simple_7,
        Dirt_Simple_8,
        Dirt_Simple_9,
        Dirt_Simple_Color_1,
        Dirt_Simple_Color_2,
        Dirt_Simple_Color_3,
        Dirt_Simple_Color_4,
        Dirt_Simple_Color_5,
        Dirt_Simple_Color_6,
        DirtHighContrast,
        DirtLowContrast,
        Lens_Old_1,
        Lens_Old_2,
        Lens_Old_3,
        Lens_Old_4
    }

    public enum LensStarburstTextureEnum
    {
        Starburst,
        Starburst_Color_1,
        Starburst_Color_2,
        Starburst_Old_1,
        Starburst_Old_2,
        Starburst_Simple_1
    }

    [Serializable]
    [AddComponentMenu("")]
    public class AmplifyBloom
    {
        //CONSTS
        public const int MaxGhosts = 5;
        public const int MinDownscales = 1;
        public const int MaxDownscales = 6;
        public const int MaxGaussian = 8;
        private const float MaxDirtIntensity = 1;
        private const float MaxStarburstIntensity = 1;

        // SERIALIZABLE VARIABLES

        [SerializeField]
        private Texture m_maskTexture = null;
        [SerializeField]
        private RenderTexture m_targetTexture = null;

        [SerializeField]
        private bool m_showDebugMessages = true;

        [SerializeField]
        private int m_softMaxdownscales = MaxDownscales;

        [SerializeField]
        private DebugToScreenEnum m_debugToScreen = DebugToScreenEnum.None;

        [SerializeField]
        private bool m_highPrecision = false;

        [SerializeField]
        private Vector4 m_bloomRange = new Vector4(500, 1, 0, 0);

        [SerializeField]
        private float m_overallThreshold = 0.53f;

        [SerializeField]
        private Vector4 m_bloomParams = new Vector4(0.8f, 1, 1, 1); // x - overallIntensity, y - threshold, z - blur radius w - bloom scale

        [SerializeField]
        private bool m_temporalFilteringActive = false;

        [SerializeField]
        private float m_temporalFilteringValue = 0.05f;

        [SerializeField]
        private int m_bloomDownsampleCount = 6;

        [SerializeField]
        private AnimationCurve m_temporalFilteringCurve;

        [SerializeField]
        private bool m_separateFeaturesThreshold = false;

        [SerializeField]
        private float m_featuresThreshold = 0.05f;

        [SerializeField]
        private AmplifyLensFlare m_lensFlare = new AmplifyLensFlare();

        [SerializeField]
        private bool m_applyLensDirt = true;

        [SerializeField]
        private float m_lensDirtStrength = 2f;

        [SerializeField]
        private Texture m_lensDirtTexture;

        [SerializeField]
        private bool m_applyLensStardurst = true;

        [SerializeField]
        private Texture m_lensStardurstTex;

        [SerializeField]
        private float m_lensStarburstStrength = 2f;

        [SerializeField]
        private AmplifyGlare m_anamorphicGlare = new AmplifyGlare();

        [SerializeField]
        private AmplifyBokeh m_bokehFilter = new AmplifyBokeh();

        [SerializeField]
        private float[] m_upscaleWeights = new float[MaxDownscales] {0.0842f, 0.1282f, 0.1648f, 0.2197f, 0.2197f, 0.1831f};

        [SerializeField]
        private float[] m_gaussianRadius = new float[MaxDownscales] {1, 1, 1, 1, 1, 1};

        [SerializeField]
        private int[] m_gaussianSteps = new int[MaxDownscales] {1, 1, 1, 1, 1, 1};

        [SerializeField]
        private float[] m_lensDirtWeights = new float[MaxDownscales] {0.0670f, 0.1020f, 0.1311f, 0.1749f, 0.2332f, 0.3f};

        [SerializeField]
        private float[] m_lensStarburstWeights = new float[MaxDownscales] {0.0670f, 0.1020f, 0.1311f, 0.1749f, 0.2332f, 0.3f};

        [SerializeField]
        private bool[] m_downscaleSettingsFoldout = new bool[MaxDownscales] {false, false, false, false, false, false};

        [SerializeField]
        private int m_featuresSourceId = 0;

        [SerializeField]
        private UpscaleQualityEnum m_upscaleQuality = UpscaleQualityEnum.Realistic;

        [SerializeField]
        private MainThresholdSizeEnum m_mainThresholdSize = MainThresholdSizeEnum.Full;

        // Internal private variables
        private Transform m_cameraTransform;
        private Matrix4x4 m_starburstMat;

        private Shader m_bloomShader;
        private Material m_bloomMaterial;

        private Shader m_finalCompositionShader;
        private Material m_finalCompositionMaterial;

        private RenderTexture m_tempFilterBuffer;
        private Camera m_camera;
        RenderTexture[] m_tempUpscaleRTs = new RenderTexture[MaxDownscales];
        RenderTexture[] m_tempAuxDownsampleRTs = new RenderTexture[MaxDownscales];
        Vector2[] m_tempDownsamplesSizes = new Vector2[MaxDownscales];
        private List<Texture2D> m_allLensDirtTextures = new List<Texture2D>();
        private List<Texture2D> m_allLensStarburstTextures = new List<Texture2D>();

        private bool silentError = false;
        private LensDirtTextureEnum m_currentLensDirtTexture = LensDirtTextureEnum.DirtHighContrast;
        private LensStarburstTextureEnum m_currentLensStardurstTex = LensStarburstTextureEnum.Starburst;
        private Vector2 m_screenshotMultiplier = new Vector2(1, 1); 

#if TRIAL
		private Texture2D watermark = null;
#endif

        public AmplifyBloom(Camera camera)
        {
            bool nullDev = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null);
            if (nullDev)
            {

                AmplifyUtils.DebugLog("Null graphics device detected. Skipping effect silently.", LogType.Error);
                this.silentError = true;
                return;
            }

            if (!AmplifyUtils.IsInitialized)
                AmplifyUtils.InitializeIds();

            this.m_anamorphicGlare.Init();
            this.m_lensFlare.Init();

            for (int i = 0; i < MaxDownscales; i++)
            {
                this.m_tempDownsamplesSizes[i] = new Vector2(0, 0);
            }
            this.m_cameraTransform = camera.transform;
            this.m_tempFilterBuffer = null;
            this.m_starburstMat = Matrix4x4.identity;

            if (this.m_temporalFilteringCurve == null)
                this.m_temporalFilteringCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0.999f));

            this.m_bloomShader = HSLRE.self._resources.LoadAsset<Shader>("AmplifyBloom");
            if (this.m_bloomShader != null)
            {
                this.m_bloomMaterial = new Material(this.m_bloomShader);
                this.m_bloomMaterial.hideFlags = HideFlags.DontSave;
            }
            else
            {
                AmplifyUtils.DebugLog("Main Bloom shader not found", LogType.Error);
                camera.gameObject.SetActive(false);
            }

            this.m_finalCompositionShader = HSLRE.self._resources.LoadAsset<Shader>("AmplifyBloomFinal");
            if (this.m_finalCompositionShader != null)
            {
                this.m_finalCompositionMaterial = new Material(this.m_finalCompositionShader);
                if (!this.m_finalCompositionMaterial.GetTag(AmplifyUtils.ShaderModeTag, false).Equals(AmplifyUtils.ShaderModeValue))
                {
                    if (this.m_showDebugMessages)
                        AmplifyUtils.DebugLog("Amplify Bloom is running on a limited hardware and may lead to a decrease on its visual quality.", LogType.Warning);
                }
                else
                {
                    this.m_softMaxdownscales = MaxDownscales;
                }

                this.m_finalCompositionMaterial.hideFlags = HideFlags.DontSave;

                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_1"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_2"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_3"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_4"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_5"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_6"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_7"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_8"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_9"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_Color_1"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_Color_2"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_Color_3"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_Color_4"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_Color_5"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Dirt_Simple_Color_6"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("DirtHighContrast"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("DirtLowContrast"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Lens_Old_1"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Lens_Old_2"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Lens_Old_3"));
                this.m_allLensDirtTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Lens_Old_4"));

                if (this.m_lensDirtTexture == null)
                {
                    this.m_lensDirtTexture = this.m_allLensDirtTextures[(int)LensDirtTextureEnum.DirtHighContrast];
                }

                this.m_allLensStarburstTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Starburst"));
                this.m_allLensStarburstTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Starburst_Color_1"));
                this.m_allLensStarburstTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Starburst_Color_2"));
                this.m_allLensStarburstTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Starburst_Old_1"));
                this.m_allLensStarburstTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Starburst_Old_2"));
                this.m_allLensStarburstTextures.Add(HSLRE.self._resources.LoadAsset<Texture2D>("Starburst_Simple_1"));

                if (this.m_lensStardurstTex == null)
                {
                    this.m_lensStardurstTex = this.m_allLensStarburstTextures[(int)LensStarburstTextureEnum.Starburst];
                }
            }
            else
            {
                AmplifyUtils.DebugLog("Bloom Composition shader not found", LogType.Error);
                camera.gameObject.SetActive(false);
            }

            this.m_camera = camera;
            this.m_camera.depthTextureMode |= DepthTextureMode.Depth;
            this.m_lensFlare.CreateLUTexture();

#if TRIAL
			watermark = new Texture2D( 4, 4 ) { hideFlags = HideFlags.HideAndDontSave };
			watermark.LoadImage( AmplifyBloom.Watermark.ImageData );
#endif
        }

        void OnDestroy()
        {
            if (this.m_bokehFilter != null)
            {
                this.m_bokehFilter.Destroy();
                this.m_bokehFilter = null;
            }

            if (this.m_anamorphicGlare != null)
            {
                this.m_anamorphicGlare.Destroy();
                this.m_anamorphicGlare = null;
            }

            if (this.m_lensFlare != null)
            {
                this.m_lensFlare.Destroy();
                this.m_lensFlare = null;
            }

#if TRIAL
			if ( watermark != null )
			{
				DestroyImmediate( watermark );
				watermark = null;
			}
#endif
        }

        void ApplyGaussianBlur(RenderTexture renderTexture, int amount, float radius = 1.0f, bool applyTemporal = false)
        {
            if (amount == 0)
                return;

            this.m_bloomMaterial.SetFloat(AmplifyUtils.BlurRadiusId, radius);
            RenderTexture blurRT = AmplifyUtils.GetTempRenderTarget(renderTexture.width, renderTexture.height);
            for (int i = 0; i < amount; i++)
            {
                blurRT.DiscardContents();
                Graphics.Blit(renderTexture, blurRT, this.m_bloomMaterial, (int)BloomPasses.HorizontalBlur);

                if (this.m_temporalFilteringActive && applyTemporal && i == (amount - 1))
                {
                    if (this.m_tempFilterBuffer != null && this.m_temporalFilteringActive)
                    {
                        float filterVal = this.m_temporalFilteringCurve.Evaluate(this.m_temporalFilteringValue);
                        this.m_bloomMaterial.SetFloat(AmplifyUtils.TempFilterValueId, filterVal);
                        this.m_bloomMaterial.SetTexture(AmplifyUtils.AnamorphicRTS[0], this.m_tempFilterBuffer);
                        renderTexture.DiscardContents();
                        Graphics.Blit(blurRT, renderTexture, this.m_bloomMaterial, (int)BloomPasses.VerticalBlurWithTempFilter);
                    }
                    else
                    {
                        renderTexture.DiscardContents();
                        Graphics.Blit(blurRT, renderTexture, this.m_bloomMaterial, (int)BloomPasses.VerticalBlur);
                    }

                    bool createRT = false;
                    if (this.m_tempFilterBuffer != null)
                    {
                        if (this.m_tempFilterBuffer.format != renderTexture.format || this.m_tempFilterBuffer.width != renderTexture.width || this.m_tempFilterBuffer.height != renderTexture.height)
                        {
                            this.CleanTempFilterRT();
                            createRT = true;
                        }
                    }
                    else
                    {
                        createRT = true;
                    }

                    if (createRT)
                    {
                        this.CreateTempFilterRT(renderTexture);
                    }
                    this.m_tempFilterBuffer.DiscardContents();
                    Graphics.Blit(renderTexture, this.m_tempFilterBuffer);
                }
                else
                {
                    renderTexture.DiscardContents();
                    Graphics.Blit(blurRT, renderTexture, this.m_bloomMaterial, (int)BloomPasses.VerticalBlur);
                }
            }
            AmplifyUtils.ReleaseTempRenderTarget(blurRT);
        }

        void CreateTempFilterRT(RenderTexture source)
        {
            if (this.m_tempFilterBuffer != null)
            {
                this.CleanTempFilterRT();
            }

            this.m_tempFilterBuffer = new RenderTexture(source.width, source.height, 0, source.format, AmplifyUtils.CurrentReadWriteMode);
            this.m_tempFilterBuffer.filterMode = AmplifyUtils.CurrentFilterMode;
            this.m_tempFilterBuffer.wrapMode = AmplifyUtils.CurrentWrapMode;
            this.m_tempFilterBuffer.Create();
        }

        void CleanTempFilterRT()
        {
            if (this.m_tempFilterBuffer != null)
            {
                RenderTexture.active = null;
                this.m_tempFilterBuffer.Release();
                UnityEngine.Object.DestroyImmediate(this.m_tempFilterBuffer);
                this.m_tempFilterBuffer = null;
            }
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (this.silentError)
                return;

            if (!AmplifyUtils.IsInitialized)
                AmplifyUtils.InitializeIds();

            if (this.m_highPrecision)
            {
                AmplifyUtils.EnsureKeywordEnabled(this.m_bloomMaterial, AmplifyUtils.HighPrecisionKeyword, true);
                AmplifyUtils.EnsureKeywordEnabled(this.m_finalCompositionMaterial, AmplifyUtils.HighPrecisionKeyword, true);
                AmplifyUtils.CurrentRTFormat = RenderTextureFormat.DefaultHDR;
            }
            else
            {
                AmplifyUtils.EnsureKeywordEnabled(this.m_bloomMaterial, AmplifyUtils.HighPrecisionKeyword, false);
                AmplifyUtils.EnsureKeywordEnabled(this.m_finalCompositionMaterial, AmplifyUtils.HighPrecisionKeyword, false);
                AmplifyUtils.CurrentRTFormat = RenderTextureFormat.Default;
            }

            float totalCamRot = Mathf.Acos(Vector3.Dot(this.m_cameraTransform.right, Vector3.right));
            if (Vector3.Cross(this.m_cameraTransform.right, Vector3.right).y > 0)
                totalCamRot = -totalCamRot;


            RenderTexture lensFlareRT = null;
            RenderTexture lensGlareRT = null;

            if (!this.m_highPrecision)
            {
                this.m_bloomRange.y = 1 / this.m_bloomRange.x;

                this.m_bloomMaterial.SetVector(AmplifyUtils.BloomRangeId, this.m_bloomRange);
                this.m_finalCompositionMaterial.SetVector(AmplifyUtils.BloomRangeId, this.m_bloomRange);
            }
            this.m_bloomParams.y = this.m_overallThreshold;

            this.m_bloomMaterial.SetVector(AmplifyUtils.BloomParamsId, this.m_bloomParams);
            this.m_finalCompositionMaterial.SetVector(AmplifyUtils.BloomParamsId, this.m_bloomParams);

            int thresholdResDiv = 1;
            switch (this.m_mainThresholdSize)
            {
                case MainThresholdSizeEnum.Half:
                    thresholdResDiv = 2;
                    break;
                case MainThresholdSizeEnum.Quarter:
                    thresholdResDiv = 4;
                    break;
            }

            // CALCULATE THRESHOLD
            RenderTexture thresholdRT = AmplifyUtils.GetTempRenderTarget((int)(src.width / (float)thresholdResDiv / this.m_screenshotMultiplier.x), (int)(src.height / (float)thresholdResDiv / this.m_screenshotMultiplier.y));
            if (this.m_maskTexture != null)
            {
                this.m_bloomMaterial.SetTexture(AmplifyUtils.MaskTextureId, this.m_maskTexture);
                Graphics.Blit(src, thresholdRT, this.m_bloomMaterial, (int)BloomPasses.ThresholdMask);
            }
            else
            {
                Graphics.Blit(src, thresholdRT, this.m_bloomMaterial, (int)BloomPasses.Threshold);
            }

            if (this.m_debugToScreen == DebugToScreenEnum.MainThreshold)
            {
                Graphics.Blit(thresholdRT, dest, this.m_bloomMaterial, (int)BloomPasses.Decode);
                AmplifyUtils.ReleaseAllRT();
                return;
            }

            // DOWNSAMPLE
            bool applyGaussian = true;
            RenderTexture downsampleRT = thresholdRT;
            if (this.m_bloomDownsampleCount > 0)
            {
                applyGaussian = false;
                int tempW = thresholdRT.width;
                int tempH = thresholdRT.height;
                for (int i = 0; i < this.m_bloomDownsampleCount; i++)
                {
                    this.m_tempDownsamplesSizes[i].x = tempW;
                    this.m_tempDownsamplesSizes[i].y = tempH;
                    tempW = (tempW + 1) >> 1;
                    tempH = (tempH + 1) >> 1;
                    this.m_tempAuxDownsampleRTs[i] = AmplifyUtils.GetTempRenderTarget(tempW, tempH);
                    if (i == 0)
                    {
                        if (!this.m_temporalFilteringActive || this.m_gaussianSteps[i] != 0)
                        {
                            if (this.m_upscaleQuality == UpscaleQualityEnum.Realistic)
                            {
                                Graphics.Blit(downsampleRT, this.m_tempAuxDownsampleRTs[i], this.m_bloomMaterial, (int)BloomPasses.DownsampleWithKaris);
                            }
                            else
                            {
                                Graphics.Blit(downsampleRT, this.m_tempAuxDownsampleRTs[i], this.m_bloomMaterial, (int)BloomPasses.DownsampleWithoutKaris);
                            }
                        }
                        else
                        {
                            if (this.m_tempFilterBuffer != null && this.m_temporalFilteringActive)
                            {
                                float filterVal = this.m_temporalFilteringCurve.Evaluate(this.m_temporalFilteringValue);
                                this.m_bloomMaterial.SetFloat(AmplifyUtils.TempFilterValueId, filterVal);
                                this.m_bloomMaterial.SetTexture(AmplifyUtils.AnamorphicRTS[0], this.m_tempFilterBuffer);
                                if (this.m_upscaleQuality == UpscaleQualityEnum.Realistic)
                                {
                                    Graphics.Blit(downsampleRT, this.m_tempAuxDownsampleRTs[i], this.m_bloomMaterial, (int)BloomPasses.DownsampleWithTempFilterWithKaris);
                                }
                                else
                                {
                                    Graphics.Blit(downsampleRT, this.m_tempAuxDownsampleRTs[i], this.m_bloomMaterial, (int)BloomPasses.DownsampleWithTempFilterWithoutKaris);
                                }
                            }
                            else
                            {
                                if (this.m_upscaleQuality == UpscaleQualityEnum.Realistic)
                                {
                                    Graphics.Blit(downsampleRT, this.m_tempAuxDownsampleRTs[i], this.m_bloomMaterial, (int)BloomPasses.DownsampleWithKaris);
                                }
                                else
                                {
                                    Graphics.Blit(downsampleRT, this.m_tempAuxDownsampleRTs[i], this.m_bloomMaterial, (int)BloomPasses.DownsampleWithoutKaris);
                                }
                            }

                            bool createRT = false;
                            if (this.m_tempFilterBuffer != null)
                            {
                                if (this.m_tempFilterBuffer.format != this.m_tempAuxDownsampleRTs[i].format || this.m_tempFilterBuffer.width != this.m_tempAuxDownsampleRTs[i].width || this.m_tempFilterBuffer.height != this.m_tempAuxDownsampleRTs[i].height)
                                {
                                    this.CleanTempFilterRT();
                                    createRT = true;
                                }
                            }
                            else
                            {
                                createRT = true;
                            }

                            if (createRT)
                            {
                                this.CreateTempFilterRT(this.m_tempAuxDownsampleRTs[i]);
                            }

                            this.m_tempFilterBuffer.DiscardContents();
                            Graphics.Blit(this.m_tempAuxDownsampleRTs[i], this.m_tempFilterBuffer);
                            if (this.m_debugToScreen == DebugToScreenEnum.TemporalFilter)
                            {
                                Graphics.Blit(this.m_tempAuxDownsampleRTs[i], dest);
                                AmplifyUtils.ReleaseAllRT();
                                return;
                            }
                        }
                    }
                    else
                    {
                        Graphics.Blit(this.m_tempAuxDownsampleRTs[i - 1], this.m_tempAuxDownsampleRTs[i], this.m_bloomMaterial, (int)BloomPasses.DownsampleNoWeightedAvg);
                    }

                    if (this.m_gaussianSteps[i] > 0)
                    {
                        this.ApplyGaussianBlur(this.m_tempAuxDownsampleRTs[i], this.m_gaussianSteps[i], this.m_gaussianRadius[i], i == 0);
                        if (this.m_temporalFilteringActive && this.m_debugToScreen == DebugToScreenEnum.TemporalFilter)
                        {
                            Graphics.Blit(this.m_tempAuxDownsampleRTs[i], dest);
                            AmplifyUtils.ReleaseAllRT();
                            return;
                        }
                    }

                }

                downsampleRT = this.m_tempAuxDownsampleRTs[this.m_featuresSourceId];
                AmplifyUtils.ReleaseTempRenderTarget(thresholdRT);
            }

            // BOKEH FILTER
            if (this.m_bokehFilter.ApplyBokeh && this.m_bokehFilter.ApplyOnBloomSource)
            {
                this.m_bokehFilter.ApplyBokehFilter(downsampleRT, this.m_bloomMaterial);
                if (this.m_debugToScreen == DebugToScreenEnum.BokehFilter)
                {
                    Graphics.Blit(downsampleRT, dest);
                    AmplifyUtils.ReleaseAllRT();
                    return;
                }
            }

            // FEATURES THRESHOLD
            RenderTexture featuresRT = null;
            bool releaseFeaturesRT = false;
            if (this.m_separateFeaturesThreshold)
            {
                this.m_bloomParams.y = this.m_featuresThreshold;
                this.m_bloomMaterial.SetVector(AmplifyUtils.BloomParamsId, this.m_bloomParams);
                this.m_finalCompositionMaterial.SetVector(AmplifyUtils.BloomParamsId, this.m_bloomParams);
                featuresRT = AmplifyUtils.GetTempRenderTarget(downsampleRT.width, downsampleRT.height);
                releaseFeaturesRT = true;
                Graphics.Blit(downsampleRT, featuresRT, this.m_bloomMaterial, (int)BloomPasses.Threshold);
                if (this.m_debugToScreen == DebugToScreenEnum.FeaturesThreshold)
                {
                    Graphics.Blit(featuresRT, dest);
                    AmplifyUtils.ReleaseAllRT();
                    return;
                }
            }
            else
            {
                featuresRT = downsampleRT;
            }

            if (this.m_bokehFilter.ApplyBokeh && !this.m_bokehFilter.ApplyOnBloomSource)
            {
                if (!releaseFeaturesRT)
                {
                    releaseFeaturesRT = true;
                    featuresRT = AmplifyUtils.GetTempRenderTarget(downsampleRT.width, downsampleRT.height);
                    Graphics.Blit(downsampleRT, featuresRT);
                }

                this.m_bokehFilter.ApplyBokehFilter(featuresRT, this.m_bloomMaterial);
                if (this.m_debugToScreen == DebugToScreenEnum.BokehFilter)
                {
                    Graphics.Blit(featuresRT, dest);
                    AmplifyUtils.ReleaseAllRT();
                    return;
                }
            }

            // LENS FLARE
            if (this.m_lensFlare.ApplyLensFlare && this.m_debugToScreen != DebugToScreenEnum.Bloom)
            {
                lensFlareRT = this.m_lensFlare.ApplyFlare(this.m_bloomMaterial, featuresRT);
                this.ApplyGaussianBlur(lensFlareRT, this.m_lensFlare.LensFlareGaussianBlurAmount, this.m_lensFlare.LensFlareGaussianBlurRadius);
                if (this.m_debugToScreen == DebugToScreenEnum.LensFlare)
                {
                    Graphics.Blit(lensFlareRT, dest);
                    AmplifyUtils.ReleaseAllRT();
                    return;
                }
            }

            //ANAMORPHIC GLARE
            if (this.m_anamorphicGlare.ApplyLensGlare && this.m_debugToScreen != DebugToScreenEnum.Bloom)
            {
                lensGlareRT = AmplifyUtils.GetTempRenderTarget(downsampleRT.width, downsampleRT.height);

                this.m_anamorphicGlare.OnRenderImage(this.m_bloomMaterial, featuresRT, lensGlareRT, totalCamRot);
                if (this.m_debugToScreen == DebugToScreenEnum.LensGlare)
                {
                    Graphics.Blit(lensGlareRT, dest);
                    AmplifyUtils.ReleaseAllRT();
                    return;
                }
            }

            if (releaseFeaturesRT)
            {
                AmplifyUtils.ReleaseTempRenderTarget(featuresRT);
            }

            //BLUR
            if (applyGaussian)
            {
                this.ApplyGaussianBlur(downsampleRT, this.m_gaussianSteps[0], this.m_gaussianRadius[0]);
            }

            //UPSAMPLE

            if (this.m_bloomDownsampleCount > 0)
            {
                if (this.m_bloomDownsampleCount == 1)
                {
                    if (this.m_upscaleQuality == UpscaleQualityEnum.Realistic)
                    {
                        this.ApplyUpscale();
                        this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.MipResultsRTS[0], this.m_tempUpscaleRTs[0]);
                    }
                    else
                    {
                        this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.MipResultsRTS[0], this.m_tempAuxDownsampleRTs[0]);
                    }
                    this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.UpscaleWeightsStr[0], this.m_upscaleWeights[0]);
                }
                else
                {

                    if (this.m_upscaleQuality == UpscaleQualityEnum.Realistic)
                    {
                        this.ApplyUpscale();
                        for (int i = 0; i < this.m_bloomDownsampleCount; i++)
                        {
                            int id = this.m_bloomDownsampleCount - i - 1;
                            this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.MipResultsRTS[id], this.m_tempUpscaleRTs[i]);
                            this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.UpscaleWeightsStr[id], this.m_upscaleWeights[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < this.m_bloomDownsampleCount; i++)
                        {
                            int id = this.m_bloomDownsampleCount - 1 - i;
                            this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.MipResultsRTS[id], this.m_tempAuxDownsampleRTs[id]);
                            this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.UpscaleWeightsStr[id], this.m_upscaleWeights[i]);
                        }
                    }
                }
            }
            else
            {
                this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.MipResultsRTS[0], downsampleRT);
                this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.UpscaleWeightsStr[0], 1);
            }

            if (this.m_debugToScreen == DebugToScreenEnum.Bloom)
            {
                this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.SourceContributionId, 0);
                this.FinalComposition(0, 1, src, dest, 0);
                return;
            }


            // FINAL COMPOSITION
            // LENS FLARE
            if (this.m_bloomDownsampleCount > 1)
            {
                for (int i = 0; i < this.m_bloomDownsampleCount; i++)
                {
                    this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.LensDirtWeightsStr[this.m_bloomDownsampleCount - i - 1], this.m_lensDirtWeights[i]);
                    this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.LensStarburstWeightsStr[this.m_bloomDownsampleCount - i - 1], this.m_lensStarburstWeights[i]);
                }
            }
            else
            {
                this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.LensDirtWeightsStr[0], this.m_lensDirtWeights[0]);
                this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.LensStarburstWeightsStr[0], this.m_lensStarburstWeights[0]);
            }
            if (this.m_lensFlare.ApplyLensFlare)
            {
                this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.LensFlareRTId, lensFlareRT);
            }

            //LENS GLARE
            if (this.m_anamorphicGlare.ApplyLensGlare)
            {
                this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.LensGlareRTId, lensGlareRT);
            }

            // LENS DIRT
            if (this.m_applyLensDirt)
            {
                this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.LensDirtRTId, this.m_lensDirtTexture);
                this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.LensDirtStrengthId, this.m_lensDirtStrength * MaxDirtIntensity);

                if (this.m_debugToScreen == DebugToScreenEnum.LensDirt)
                {
                    this.FinalComposition(0, 0, src, dest, 2);
                    return;
                }
            }

            // LENS STARBURST
            if (this.m_applyLensStardurst)
            {
                this.m_starburstMat[0, 0] = Mathf.Cos(totalCamRot);
                this.m_starburstMat[0, 1] = -Mathf.Sin(totalCamRot);
                this.m_starburstMat[1, 0] = Mathf.Sin(totalCamRot);
                this.m_starburstMat[1, 1] = Mathf.Cos(totalCamRot);

                this.m_finalCompositionMaterial.SetMatrix(AmplifyUtils.LensFlareStarMatrixId, this.m_starburstMat);
                this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.LensFlareStarburstStrengthId, this.m_lensStarburstStrength * MaxStarburstIntensity);
                this.m_finalCompositionMaterial.SetTexture(AmplifyUtils.LensStarburstRTId, this.m_lensStardurstTex);

                if (this.m_debugToScreen == DebugToScreenEnum.LensStarburst)
                {
                    this.FinalComposition(0, 0, src, dest, 1);
                    return;
                }
            }

            if (this.m_targetTexture != null)
            {
                this.m_targetTexture.DiscardContents();
                this.FinalComposition(0, 1, src, this.m_targetTexture, -1);
                Graphics.Blit(src, dest);
            }
            else
            {
                this.FinalComposition(1, 1, src, dest, -1);
            }
        }


        void FinalComposition(float srcContribution, float upscaleContribution, RenderTexture src, RenderTexture dest, int forcePassId)
        {
            this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.SourceContributionId, srcContribution);
            this.m_finalCompositionMaterial.SetFloat(AmplifyUtils.UpscaleContributionId, upscaleContribution);

            int passCount = 0;
            if (forcePassId > -1)
            {
                passCount = forcePassId;
            }
            else
            {
                if (this.LensFlareInstance.ApplyLensFlare)
                {
                    passCount = passCount | 8;
                }

                if (this.LensGlareInstance.ApplyLensGlare)
                {
                    passCount = passCount | 4;
                }

                if (this.m_applyLensDirt)
                {
                    passCount = passCount | 2;
                }

                if (this.m_applyLensStardurst)
                {
                    passCount = passCount | 1;
                }
            }
            passCount += (this.m_bloomDownsampleCount - 1) * 16;
            Graphics.Blit(src, dest, this.m_finalCompositionMaterial, passCount);
            AmplifyUtils.ReleaseAllRT();
        }

        void ApplyUpscale()
        {
            int beginIdx = (this.m_bloomDownsampleCount - 1);
            int upscaleIdx = 0;
            for (int downscaleIdx = beginIdx; downscaleIdx > -1; downscaleIdx--)
            {
                this.m_tempUpscaleRTs[upscaleIdx] = AmplifyUtils.GetTempRenderTarget((int)this.m_tempDownsamplesSizes[downscaleIdx].x, (int)this.m_tempDownsamplesSizes[downscaleIdx].y);
                if (downscaleIdx == beginIdx)
                {
                    Graphics.Blit(this.m_tempAuxDownsampleRTs[beginIdx], this.m_tempUpscaleRTs[upscaleIdx], this.m_bloomMaterial, (int)BloomPasses.UpscaleFirstPass);
                }
                else
                {
                    this.m_bloomMaterial.SetTexture(AmplifyUtils.AnamorphicRTS[0], this.m_tempUpscaleRTs[upscaleIdx - 1]);
                    Graphics.Blit(this.m_tempAuxDownsampleRTs[downscaleIdx], this.m_tempUpscaleRTs[upscaleIdx], this.m_bloomMaterial, (int)BloomPasses.Upscale);
                }
                upscaleIdx++;
            }
        }

        public AmplifyGlare LensGlareInstance { get { return this.m_anamorphicGlare; } }

        public AmplifyBokeh BokehFilterInstance { get { return this.m_bokehFilter; } }

        public AmplifyLensFlare LensFlareInstance { get { return this.m_lensFlare; } }

        public bool ApplyLensDirt { get { return this.m_applyLensDirt; } set { this.m_applyLensDirt = value; } }

        public float LensDirtStrength { get { return this.m_lensDirtStrength; } set { this.m_lensDirtStrength = value < 0 ? 0 : value; } }

        public LensDirtTextureEnum LensDirtTexture
        {
            get { return this.m_currentLensDirtTexture; }
            set
            {
                this.m_currentLensDirtTexture = value;
                this.m_lensDirtTexture = this.m_allLensDirtTextures[(int)this.m_currentLensDirtTexture];
            }
        }

        public bool ApplyLensStardurst { get { return this.m_applyLensStardurst; } set { this.m_applyLensStardurst = value; } }

        public LensStarburstTextureEnum LensStardurstTex
        {
            get { return this.m_currentLensStardurstTex; }
            set
            {
                this.m_currentLensStardurstTex = value;
                this.m_lensStardurstTex = this.m_allLensStarburstTextures[(int)this.m_currentLensStardurstTex];
            }
        }

        public float LensStarburstStrength { get { return this.m_lensStarburstStrength; } set { this.m_lensStarburstStrength = value < 0 ? 0 : value; } }

        public PrecisionModes CurrentPrecisionMode
        {
            get
            {
                if (this.m_highPrecision)
                    return PrecisionModes.High;

                return PrecisionModes.Low;
            }
            set { this.HighPrecision = value == PrecisionModes.High; }
        }

        public bool HighPrecision
        {
            get { return this.m_highPrecision; }
            set
            {
                if (this.m_highPrecision != value)
                {
                    this.m_highPrecision = value;
                    this.CleanTempFilterRT();
                }
            }
        }

        public float BloomRange { get { return this.m_bloomRange.x; } set { this.m_bloomRange.x = value < 0 ? 0 : value; } }

        public float OverallThreshold { get { return this.m_overallThreshold; } set { this.m_overallThreshold = value < 0 ? 0 : value; } }

        public Vector4 BloomParams { get { return this.m_bloomParams; } set { this.m_bloomParams = value; } }

        public float OverallIntensity { get { return this.m_bloomParams.x; } set { this.m_bloomParams.x = value < 0 ? 0 : value; } }

        public float BloomScale { get { return this.m_bloomParams.w; } set { this.m_bloomParams.w = value < 0 ? 0 : value; } }

        public float UpscaleBlurRadius { get { return this.m_bloomParams.z; } set { this.m_bloomParams.z = value; } }

        public bool TemporalFilteringActive
        {
            get { return this.m_temporalFilteringActive; }
            set
            {
                if (this.m_temporalFilteringActive != value)
                {
                    this.CleanTempFilterRT();
                }
                this.m_temporalFilteringActive = value;
            }
        }

        public float TemporalFilteringValue { get { return this.m_temporalFilteringValue; } set { this.m_temporalFilteringValue = value; } }

        public int SoftMaxdownscales { get { return this.m_softMaxdownscales; } }
        public int BloomDownsampleCount { get { return this.m_bloomDownsampleCount; } set { this.m_bloomDownsampleCount = Mathf.Clamp(value, MinDownscales, this.m_softMaxdownscales); } }

        public int FeaturesSourceId { get { return this.m_featuresSourceId; } set { this.m_featuresSourceId = Mathf.Clamp(value, 0, this.m_bloomDownsampleCount - 1); } }

        public bool[] DownscaleSettingsFoldout { get { return this.m_downscaleSettingsFoldout; } }

        public float[] UpscaleWeights { get { return this.m_upscaleWeights; } }

        public float[] LensDirtWeights { get { return this.m_lensDirtWeights; } }

        public float[] LensStarburstWeights { get { return this.m_lensStarburstWeights; } }

        public float[] GaussianRadius { get { return this.m_gaussianRadius; } }

        public int[] GaussianSteps { get { return this.m_gaussianSteps; } }

        public AnimationCurve TemporalFilteringCurve { get { return this.m_temporalFilteringCurve; } set { this.m_temporalFilteringCurve = value; } }

        public bool SeparateFeaturesThreshold { get { return this.m_separateFeaturesThreshold; } set { this.m_separateFeaturesThreshold = value; } }

        public float FeaturesThreshold { get { return this.m_featuresThreshold; } set { this.m_featuresThreshold = value < 0 ? 0 : value; } }

        public DebugToScreenEnum DebugToScreen { get { return this.m_debugToScreen; } set { this.m_debugToScreen = value; } }

        public UpscaleQualityEnum UpscaleQuality { get { return this.m_upscaleQuality; } set { this.m_upscaleQuality = value; } }

        public bool ShowDebugMessages { get { return this.m_showDebugMessages; } set { this.m_showDebugMessages = value; } }

        public MainThresholdSizeEnum MainThresholdSize { get { return this.m_mainThresholdSize; } set { this.m_mainThresholdSize = value; } }

        public Vector2 ScreenshotMultiplier { get { return this.m_screenshotMultiplier; } set { this.m_screenshotMultiplier = value; } }

        public RenderTexture TargetTexture { get { return this.m_targetTexture; } set { this.m_targetTexture = value; } }

        public Texture MaskTexture { get { return this.m_maskTexture; } set { this.m_maskTexture = value; } }

        public bool ApplyBokehFilter { get { return this.m_bokehFilter.ApplyBokeh; } set { this.m_bokehFilter.ApplyBokeh = value; } }

        public bool ApplyLensFlare { get { return this.m_lensFlare.ApplyLensFlare; } set { this.m_lensFlare.ApplyLensFlare = value; } }

        public bool ApplyLensGlare { get { return this.m_anamorphicGlare.ApplyLensGlare; } set { this.m_anamorphicGlare.ApplyLensGlare = value; } }
#if TRIAL
		void OnGUI()
		{
			if ( !silentError && watermark != null )
				GUI.DrawTexture( new Rect( Screen.width - watermark.width - 15, Screen.height - watermark.height - 12, watermark.width, watermark.height ), watermark );
		}
#endif
    }

    [Serializable]
    public class AmplifyLensFlare : IAmplifyItem
    {
        //CONSTS
        private const int LUTTextureWidth = 256;

        //SERIALIZABLE VARIABLES
        [SerializeField]
        private float m_overallIntensity = 1f;

        [SerializeField]
        private float m_normalizedGhostIntensity = 0.8f;

        [SerializeField]
        private float m_normalizedHaloIntensity = 0.1f;

        [SerializeField]
        private bool m_applyLensFlare = true;

        [SerializeField]
        private int m_lensFlareGhostAmount = 3;

        [SerializeField]
        private Vector4 m_lensFlareGhostsParams = new Vector4(0.8f, 0.228f, 1, 4); // x - intensity y - Dispersal z - Power Factor w - Power Falloff

        [SerializeField]
        private float m_lensFlareGhostChrDistortion = 2;

        [SerializeField]
        private Gradient m_lensGradient;

        [SerializeField]
        private Texture2D m_lensFlareGradTexture;

        private Color[] m_lensFlareGradColor = new Color[LUTTextureWidth];

        [SerializeField]
        private Vector4 m_lensFlareHaloParams = new Vector4(0.1f, 0.573f, 1, 128); // x - Intensity y - Width z - Power Factor w - Power Falloff

        [SerializeField]
        private float m_lensFlareHaloChrDistortion = 1.51f;

        [SerializeField]
        private int m_lensFlareGaussianBlurAmount = 1;

        [SerializeField]
        private float m_lensFlareGaussianBlurRadius = 1f;

        public AmplifyLensFlare()
        {
            this.m_lensGradient = new Gradient();
        }

        public void Init()
        {
            if (this.m_lensGradient.alphaKeys.Length == 0 && this.m_lensGradient.colorKeys.Length == 0)
            {
                GradientColorKey[] colorKeys = {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.blue, 0.25f),
                    new GradientColorKey(Color.green, 0.5f),
                    new GradientColorKey(Color.yellow, 0.75f),
                    new GradientColorKey(Color.red, 1f)
                };
                GradientAlphaKey[] alphaKeys = {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(1f, 0.75f),
                    new GradientAlphaKey(1f, 1f)
                };
                this.m_lensGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        public void Destroy()
        {
            if (this.m_lensFlareGradTexture != null)
            {
                Object.DestroyImmediate(this.m_lensFlareGradTexture);
                this.m_lensFlareGradTexture = null;
            }
        }

        public void CreateLUTexture()
        {
            this.m_lensFlareGradTexture = new Texture2D(LUTTextureWidth, 1, TextureFormat.ARGB32, false);
            this.m_lensFlareGradTexture.filterMode = FilterMode.Bilinear;
            this.TextureFromGradient();
        }

        public RenderTexture ApplyFlare(Material material, RenderTexture source)
        {
            RenderTexture dest = AmplifyUtils.GetTempRenderTarget(source.width, source.height);
            material.SetVector(AmplifyUtils.LensFlareGhostsParamsId, this.m_lensFlareGhostsParams);
            material.SetTexture(AmplifyUtils.LensFlareLUTId, this.m_lensFlareGradTexture);
            material.SetVector(AmplifyUtils.LensFlareHaloParamsId, this.m_lensFlareHaloParams);
            material.SetFloat(AmplifyUtils.LensFlareGhostChrDistortionId, this.m_lensFlareGhostChrDistortion);
            material.SetFloat(AmplifyUtils.LensFlareHaloChrDistortionId, this.m_lensFlareHaloChrDistortion);
            Graphics.Blit(source, dest, material, (int)BloomPasses.LensFlare0 + this.m_lensFlareGhostAmount);
            return dest;
        }

        public void TextureFromGradient()
        {
            for (int i = 0; i < LUTTextureWidth; i++)
            {
                this.m_lensFlareGradColor[i] = this.m_lensGradient.Evaluate((float)i / (float)(LUTTextureWidth - 1));
            }
            this.m_lensFlareGradTexture.SetPixels(this.m_lensFlareGradColor);
            this.m_lensFlareGradTexture.Apply();
        }

        public bool ApplyLensFlare { get { return this.m_applyLensFlare; } set { this.m_applyLensFlare = value; } }

        public float OverallIntensity
        {
            get { return this.m_overallIntensity; }
            set
            {
                this.m_overallIntensity = value < 0 ? 0 : value;
                this.m_lensFlareGhostsParams.x = value * this.m_normalizedGhostIntensity;
                this.m_lensFlareHaloParams.x = value * this.m_normalizedHaloIntensity;
            }
        }
        public int LensFlareGhostAmount { get { return this.m_lensFlareGhostAmount; } set { this.m_lensFlareGhostAmount = value; } }

        public Vector4 LensFlareGhostsParams { get { return this.m_lensFlareGhostsParams; } set { this.m_lensFlareGhostsParams = value; } }

        public float LensFlareNormalizedGhostsIntensity
        {
            get { return this.m_normalizedGhostIntensity; }
            set
            {
                this.m_normalizedGhostIntensity = value < 0 ? 0 : value;
                this.m_lensFlareGhostsParams.x = this.m_overallIntensity * this.m_normalizedGhostIntensity;
            }
        }

        public float LensFlareGhostsIntensity { get { return this.m_lensFlareGhostsParams.x; } set { this.m_lensFlareGhostsParams.x = value < 0 ? 0 : value; } }

        public float LensFlareGhostsDispersal { get { return this.m_lensFlareGhostsParams.y; } set { this.m_lensFlareGhostsParams.y = value; } }

        public float LensFlareGhostsPowerFactor { get { return this.m_lensFlareGhostsParams.z; } set { this.m_lensFlareGhostsParams.z = value; } }

        public float LensFlareGhostsPowerFalloff { get { return this.m_lensFlareGhostsParams.w; } set { this.m_lensFlareGhostsParams.w = value; } }

        public Gradient LensFlareGradient { get { return this.m_lensGradient; } set { this.m_lensGradient = value; } }
        public Texture2D LensFlareGradientTexture { get { return this.m_lensFlareGradTexture; } }
        public Vector4 LensFlareHaloParams { get { return this.m_lensFlareHaloParams; } set { this.m_lensFlareHaloParams = value; } }

        public float LensFlareNormalizedHaloIntensity
        {
            get { return this.m_normalizedHaloIntensity; }
            set
            {
                this.m_normalizedHaloIntensity = value < 0 ? 0 : value;
                this.m_lensFlareHaloParams.x = this.m_overallIntensity * this.m_normalizedHaloIntensity;
            }
        }

        public float LensFlareHaloIntensity { get { return this.m_lensFlareHaloParams.x; } set { this.m_lensFlareHaloParams.x = value < 0 ? 0 : value; } }

        public float LensFlareHaloWidth { get { return this.m_lensFlareHaloParams.y; } set { this.m_lensFlareHaloParams.y = value; } }

        public float LensFlareHaloPowerFactor { get { return this.m_lensFlareHaloParams.z; } set { this.m_lensFlareHaloParams.z = value; } }

        public float LensFlareHaloPowerFalloff { get { return this.m_lensFlareHaloParams.w; } set { this.m_lensFlareHaloParams.w = value; } }

        public float LensFlareGhostChrDistortion { get { return this.m_lensFlareGhostChrDistortion; } set { this.m_lensFlareGhostChrDistortion = value; } }

        public float LensFlareHaloChrDistortion { get { return this.m_lensFlareHaloChrDistortion; } set { this.m_lensFlareHaloChrDistortion = value; } }

        public int LensFlareGaussianBlurAmount { get { return this.m_lensFlareGaussianBlurAmount; } set { this.m_lensFlareGaussianBlurAmount = value; } }

        public float LensFlareGaussianBlurRadius { get { return this.m_lensFlareGaussianBlurRadius; } set { this.m_lensFlareGaussianBlurRadius = value; } }
    }


    // Glare form library
    public enum GlareLibType
    {
        CheapLens = 0,
        CrossScreen,
        CrossScreenSpectral,
        SnowCross,
        SnowCrossSpectral,
        SunnyCross,
        SunnyCrossSpectral,
        VerticalSlits,
        HorizontalSlits,
        Custom
    };

    [Serializable]
    public class GlareDefData
    {
        public bool FoldoutValue = true;
        [SerializeField]
        private StarLibType m_starType = StarLibType.Cross;
        [SerializeField]
        private float m_starInclination = 0;
        [SerializeField]
        private float m_chromaticAberration = 0;
        [SerializeField]
        private StarDefData m_customStarData = null;

        public GlareDefData()
        {
            this.m_customStarData = new StarDefData();
        }

        public GlareDefData(StarLibType starType, float starInclination, float chromaticAberration)
        {
            this.m_starType = starType;
            this.m_starInclination = starInclination;
            this.m_chromaticAberration = chromaticAberration;
        }

        public StarLibType StarType { get { return this.m_starType; } set { this.m_starType = value; } }

        public float StarInclination { get { return this.m_starInclination; } set { this.m_starInclination = value; } }

        public float StarInclinationDeg { get { return this.m_starInclination * Mathf.Rad2Deg; } set { this.m_starInclination = value * Mathf.Deg2Rad; } }

        public float ChromaticAberration { get { return this.m_chromaticAberration; } set { this.m_chromaticAberration = value; } }

        public StarDefData CustomStarData { get { return this.m_customStarData; } set { this.m_customStarData = value; } }
    }

    [Serializable]
    public sealed class AmplifyGlare : IAmplifyItem
    {
        public const int MaxLineSamples = 8;
        public const int MaxTotalSamples = 16;
        public const int MaxStarLines = 4;
        public const int MaxPasses = 4;
        public const int MaxCustomGlare = 32;

        [SerializeField]
        private GlareDefData[] m_customGlareDef;

        [SerializeField]
        private int m_customGlareDefIdx = 0;

        [SerializeField]
        private int m_customGlareDefAmount = 0;

        [SerializeField]
        private bool m_applyGlare = true;

        [SerializeField]
        private Color _overallTint = Color.white;

        [SerializeField]
        private Gradient m_cromaticAberrationGrad;

        [SerializeField]
        private int m_glareMaxPassCount = MaxPasses;

        private StarDefData[] m_starDefArr;
        private GlareDefData[] m_glareDefArr;

        private Matrix4x4[] m_weigthsMat;
        private Matrix4x4[] m_offsetsMat;

        private Color m_whiteReference;

        private float m_aTanFoV;

        private AmplifyGlareCache m_amplifyGlareCache;

        [SerializeField]
        private int m_currentWidth;

        [SerializeField]
        private int m_currentHeight;

        [SerializeField]
        private GlareLibType m_currentGlareType = GlareLibType.CheapLens;

        [SerializeField]
        private int m_currentGlareIdx;

        [SerializeField]
        private float m_perPassDisplacement = 4;

        [SerializeField]
        private float m_intensity = 0.17f;

        [SerializeField]
        private float m_overallStreakScale = 1f;

        private bool m_isDirty = true;
        private RenderTexture[] _rtBuffer;

        public AmplifyGlare()
        {
            this.m_currentGlareIdx = (int)this.m_currentGlareType;

            this.m_cromaticAberrationGrad = new Gradient();

            this._rtBuffer = new RenderTexture[MaxStarLines * MaxPasses];

            this.m_weigthsMat = new Matrix4x4[4];
            this.m_offsetsMat = new Matrix4x4[4];

            this.m_amplifyGlareCache = new AmplifyGlareCache();

            this.m_whiteReference = new Color(0.63f, 0.63f, 0.63f, 0.0f);
            this.m_aTanFoV = Mathf.Atan(Mathf.PI / MaxLineSamples);

            this.m_starDefArr = new[]
            {
                new StarDefData(StarLibType.Cross, "Cross", 2, 4, 1.0f, 0.85f, 0.0f, 0.5f, -1.0f, 90.0f),
                new StarDefData(StarLibType.Cross_Filter, "CrossFilter", 2, 4, 1.0f, 0.95f, 0.0f, 0.5f, -1.0f, 90.0f),
                new StarDefData(StarLibType.Snow_Cross, "snowCross", 3, 4, 1.0f, 0.96f, 0.349f, 0.5f, -1.0f, -1),
                new StarDefData(StarLibType.Vertical, "Vertical", 1, 4, 1.0f, 0.96f, 0.0f, 0.0f, -1.0f, -1),
                new StarDefData(StarLibType.Sunny_Cross, "SunnyCross", 4, 4, 1.0f, 0.88f, 0.0f, 0.0f, 0.95f, 45.0f)
            };

            this.m_glareDefArr = new[]
            {
                new GlareDefData(StarLibType.Cross, 0.00f, 0.5f), //Cheap Lens
                new GlareDefData(StarLibType.Cross_Filter, 0.44f, 0.5f), //Cross Screen
                new GlareDefData(StarLibType.Cross_Filter, 1.22f, 1.5f), //Cross Screen Spectral
                new GlareDefData(StarLibType.Snow_Cross, 0.17f, 0.5f), //Snow Cross
                new GlareDefData(StarLibType.Snow_Cross, 0.70f, 1.5f), //Snow Cross Spectral
                new GlareDefData(StarLibType.Sunny_Cross, 0.00f, 0.5f), //Sunny Cross
                new GlareDefData(StarLibType.Sunny_Cross, 0.79f, 1.5f), //Sunny Cross Spectral
                new GlareDefData(StarLibType.Vertical, 1.57f, 0.5f), //Vertical Slits
                new GlareDefData(StarLibType.Vertical, 0.00f, 0.5f) //Horizontal slits
            };
        }

        public void Init()
        {
            if (this.m_cromaticAberrationGrad.alphaKeys.Length == 0 && this.m_cromaticAberrationGrad.colorKeys.Length == 0)
            {
                GradientColorKey[] colorKeys = {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.blue, 0.25f),
                    new GradientColorKey(Color.green, 0.5f),
                    new GradientColorKey(Color.yellow, 0.75f),
                    new GradientColorKey(Color.red, 1f)
                };
                GradientAlphaKey[] alphaKeys = {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(1f, 0.75f),
                    new GradientAlphaKey(1f, 1f)
                };
                this.m_cromaticAberrationGrad.SetKeys(colorKeys, alphaKeys);
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < this.m_starDefArr.Length; i++)
            {
                this.m_starDefArr[i].Destroy();
            }

            this.m_glareDefArr = null;
            this.m_weigthsMat = null;
            this.m_offsetsMat = null;

            for (int i = 0; i < this._rtBuffer.Length; i++)
            {
                if (this._rtBuffer[i] != null)
                {
                    AmplifyUtils.ReleaseTempRenderTarget(this._rtBuffer[i]);
                    this._rtBuffer[i] = null;
                }
            }
            this._rtBuffer = null;

            this.m_amplifyGlareCache.Destroy();
            this.m_amplifyGlareCache = null;
        }

        public void SetDirty()
        {
            this.m_isDirty = true;
        }

        public void OnRenderFromCache(RenderTexture source, RenderTexture dest, Material material, float glareIntensity, float cameraRotation)
        {
            // ALLOCATE RENDER TEXTURES
            for (int i = 0; i < this.m_amplifyGlareCache.TotalRT; i++)
            {
                this._rtBuffer[i] = AmplifyUtils.GetTempRenderTarget(source.width, source.height);
            }

            int rtIdx = 0;
            for (int d = 0; d < this.m_amplifyGlareCache.StarDef.StarlinesCount; d++)
            {
                for (int p = 0; p < this.m_amplifyGlareCache.CurrentPassCount; p++)
                {
                    // APPLY SHADER
                    this.UpdateMatrixesForPass(material, this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets, this.m_amplifyGlareCache.Starlines[d].Passes[p].Weights, glareIntensity, cameraRotation * this.m_amplifyGlareCache.StarDef.CameraRotInfluence);

                    //CREATED WEIGHTED TEXTURE
                    if (p == 0)
                    {
                        Graphics.Blit(source, this._rtBuffer[rtIdx], material, (int)BloomPasses.AnamorphicGlare);
                    }
                    else
                    {
                        Graphics.Blit(this._rtBuffer[rtIdx - 1], this._rtBuffer[rtIdx], material, (int)BloomPasses.AnamorphicGlare);
                    }
                    rtIdx += 1;
                }
            }

            //ADD TO MAIN RT
            for (int i = 0; i < this.m_amplifyGlareCache.StarDef.StarlinesCount; i++)
            {
                material.SetVector(AmplifyUtils.AnamorphicGlareWeightsStr[i], this.m_amplifyGlareCache.AverageWeight);
                int idx = (i + 1) * this.m_amplifyGlareCache.CurrentPassCount - 1;
                material.SetTexture(AmplifyUtils.AnamorphicRTS[i], this._rtBuffer[idx]);
            }


            int passId = (int)BloomPasses.WeightedAddPS1 + this.m_amplifyGlareCache.StarDef.StarlinesCount - 1;
            dest.DiscardContents();
            Graphics.Blit(this._rtBuffer[0], dest, material, passId);

            //RELEASE RT's
            for (rtIdx = 0; rtIdx < this._rtBuffer.Length; rtIdx++)
            {
                AmplifyUtils.ReleaseTempRenderTarget(this._rtBuffer[rtIdx]);
                this._rtBuffer[rtIdx] = null;
            }
        }

        public void UpdateMatrixesForPass(Material material, Vector4[] offsets, Vector4[] weights, float glareIntensity, float rotation)
        {
            float cosRot = Mathf.Cos(rotation);
            float sinRot = Mathf.Sin(rotation);

            for (int i = 0; i < MaxTotalSamples; i++)
            {
                int matIdx = i >> 2;
                int vecIdx = i & 3;
                this.m_offsetsMat[matIdx][vecIdx, 0] = offsets[i].x * cosRot - offsets[i].y * sinRot;
                this.m_offsetsMat[matIdx][vecIdx, 1] = offsets[i].x * sinRot + offsets[i].y * cosRot;

                this.m_weigthsMat[matIdx][vecIdx, 0] = glareIntensity * weights[i].x;
                this.m_weigthsMat[matIdx][vecIdx, 1] = glareIntensity * weights[i].y;
                this.m_weigthsMat[matIdx][vecIdx, 2] = glareIntensity * weights[i].z;
            }

            for (int i = 0; i < 4; i++)
            {
                material.SetMatrix(AmplifyUtils.AnamorphicGlareOffsetsMatStr[i], this.m_offsetsMat[i]);
                material.SetMatrix(AmplifyUtils.AnamorphicGlareWeightsMatStr[i], this.m_weigthsMat[i]);
            }
        }

        public void OnRenderImage(Material material, RenderTexture source, RenderTexture dest, float cameraRot)
        {
            //NEED TO SET DESTINATION RENDER TARGET TO COMPLETELLY BLACK SO WE CAN SUM ALL THE GLARE/STAR PASSES ON IT
            Graphics.Blit(Texture2D.blackTexture, dest);

            if (this.m_isDirty || this.m_currentWidth != source.width || this.m_currentHeight != source.height)
            {
                this.m_isDirty = false;
                this.m_currentWidth = source.width;
                this.m_currentHeight = source.height;
            }
            else
            {
                this.OnRenderFromCache(source, dest, material, this.m_intensity, cameraRot);
                return;
            }

            GlareDefData glareDef = null;
            bool validCustom = false;
            if (this.m_currentGlareType == GlareLibType.Custom)
            {
                if (this.m_customGlareDef != null && this.m_customGlareDef.Length > 0)
                {
                    glareDef = this.m_customGlareDef[this.m_customGlareDefIdx];
                    validCustom = true;
                }
                else
                {
                    glareDef = this.m_glareDefArr[0];
                }
            }
            else
            {
                glareDef = this.m_glareDefArr[this.m_currentGlareIdx];
            }

            this.m_amplifyGlareCache.GlareDef = glareDef;

            float srcW = source.width;
            float srcH = source.height;

            StarDefData starDef = (validCustom) ? glareDef.CustomStarData : this.m_starDefArr[(int)glareDef.StarType];

            this.m_amplifyGlareCache.StarDef = starDef;
            int currPassCount = (this.m_glareMaxPassCount < starDef.PassCount) ? this.m_glareMaxPassCount : starDef.PassCount;
            this.m_amplifyGlareCache.CurrentPassCount = currPassCount;
            float radOffset = glareDef.StarInclination + starDef.Inclination;

            for (int p = 0; p < this.m_glareMaxPassCount; p++)
            {
                float ratio = (float)(p + 1) / (float)this.m_glareMaxPassCount;

                for (int s = 0; s < MaxLineSamples; s++)
                {
                    Color chromaticAberrColor = this._overallTint * Color.Lerp(this.m_cromaticAberrationGrad.Evaluate((float)s / (float)(MaxLineSamples - 1)), this.m_whiteReference, ratio);
                    this.m_amplifyGlareCache.CromaticAberrationMat[p, s] = Color.Lerp(this.m_whiteReference, chromaticAberrColor, glareDef.ChromaticAberration);
                }
            }
            this.m_amplifyGlareCache.TotalRT = starDef.StarlinesCount * currPassCount;

            for (int i = 0; i < this.m_amplifyGlareCache.TotalRT; i++)
            {
                this._rtBuffer[i] = AmplifyUtils.GetTempRenderTarget(source.width, source.height);
            }

            int rtIdx = 0;
            for (int d = 0; d < starDef.StarlinesCount; d++)
            {
                StarLineData starLine = starDef.StarLinesArr[d];
                float angle = radOffset + starLine.Inclination;
                float sinAngle = Mathf.Sin(angle);
                float cosAngle = Mathf.Cos(angle);
                Vector2 vtStepUV = new Vector2();
                vtStepUV.x = cosAngle / srcW * (starLine.SampleLength * this.m_overallStreakScale);
                vtStepUV.y = sinAngle / srcH * (starLine.SampleLength * this.m_overallStreakScale);

                float attnPowScale = (this.m_aTanFoV + 0.1f) * (280.0f) / (srcW + srcH) * 1.2f;

                for (int p = 0; p < currPassCount; p++)
                {
                    for (int i = 0; i < MaxLineSamples; i++)
                    {
                        float lum = Mathf.Pow(starLine.Attenuation, attnPowScale * i);

                        this.m_amplifyGlareCache.Starlines[d].Passes[p].Weights[i] = this.m_amplifyGlareCache.CromaticAberrationMat[currPassCount - 1 - p, i] * lum * (p + 1.0f) * 0.5f;

                        // OFFSET OF SAMPLING COORDINATE
                        this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets[i].x = vtStepUV.x * i;
                        this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets[i].y = vtStepUV.y * i;
                        if (Mathf.Abs(this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets[i].x) >= 0.9f ||
                            Mathf.Abs(this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets[i].y) >= 0.9f)
                        {
                            this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets[i].x = 0.0f;
                            this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets[i].y = 0.0f;
                            this.m_amplifyGlareCache.Starlines[d].Passes[p].Weights[i] *= 0.0f;
                        }
                    }

                    // MIRROR STARLINE
                    for (int i = MaxLineSamples; i < MaxTotalSamples; i++)
                    {
                        this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets[i] = -this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets[i - MaxLineSamples];
                        this.m_amplifyGlareCache.Starlines[d].Passes[p].Weights[i] = this.m_amplifyGlareCache.Starlines[d].Passes[p].Weights[i - MaxLineSamples];
                    }

                    // APPLY SHADER
                    this.UpdateMatrixesForPass(material, this.m_amplifyGlareCache.Starlines[d].Passes[p].Offsets, this.m_amplifyGlareCache.Starlines[d].Passes[p].Weights, this.m_intensity, starDef.CameraRotInfluence * cameraRot);

                    //CREATED WEIGHTED TEXTURE
                    if (p == 0)
                    {
                        Graphics.Blit(source, this._rtBuffer[rtIdx], material, (int)BloomPasses.AnamorphicGlare);
                    }
                    else
                    {
                        Graphics.Blit(this._rtBuffer[rtIdx - 1], this._rtBuffer[rtIdx], material, (int)BloomPasses.AnamorphicGlare);
                    }

                    rtIdx += 1;
                    vtStepUV *= this.m_perPassDisplacement;
                    attnPowScale *= this.m_perPassDisplacement;
                }
            }

            //ADD TO MAIN RT
            this.m_amplifyGlareCache.AverageWeight = Vector4.one / starDef.StarlinesCount;
            for (int i = 0; i < starDef.StarlinesCount; i++)
            {
                material.SetVector(AmplifyUtils.AnamorphicGlareWeightsStr[i], this.m_amplifyGlareCache.AverageWeight);
                int idx = (i + 1) * currPassCount - 1;
                material.SetTexture(AmplifyUtils.AnamorphicRTS[i], this._rtBuffer[idx]);
            }

            int passId = (int)BloomPasses.WeightedAddPS1 + starDef.StarlinesCount - 1;
            dest.DiscardContents();
            Graphics.Blit(this._rtBuffer[0], dest, material, passId);

            //RELEASE RT's
            for (rtIdx = 0; rtIdx < this._rtBuffer.Length; rtIdx++)
            {
                AmplifyUtils.ReleaseTempRenderTarget(this._rtBuffer[rtIdx]);
                this._rtBuffer[rtIdx] = null;
            }
        }

        public GlareLibType CurrentGlare
        {
            get { return this.m_currentGlareType; }
            set
            {
                if (this.m_currentGlareType != value)
                {
                    this.m_currentGlareType = value;
                    this.m_currentGlareIdx = (int)value;
                    this.m_isDirty = true;
                }
            }
        }

        public int GlareMaxPassCount
        {
            get { return this.m_glareMaxPassCount; }
            set
            {
                this.m_glareMaxPassCount = value;
                this.m_isDirty = true;
            }
        }

        public float PerPassDisplacement
        {
            get { return this.m_perPassDisplacement; }
            set
            {
                this.m_perPassDisplacement = value;
                this.m_isDirty = true;
            }
        }

        public float Intensity
        {
            get { return this.m_intensity; }
            set
            {
                this.m_intensity = value < 0 ? 0 : value;
                this.m_isDirty = true;
            }
        }

        public Color OverallTint
        {
            get { return this._overallTint; }
            set
            {
                this._overallTint = value;
                this.m_isDirty = true;
            }
        }

        public bool ApplyLensGlare { get { return this.m_applyGlare; } set { this.m_applyGlare = value; } }

        public Gradient CromaticColorGradient
        {
            get { return this.m_cromaticAberrationGrad; }
            set
            {
                this.m_cromaticAberrationGrad = value;
                this.m_isDirty = true;
            }
        }

        public float OverallStreakScale
        {
            get { return this.m_overallStreakScale; }
            set
            {
                this.m_overallStreakScale = value;
                this.m_isDirty = true;
            }
        }

        public GlareDefData[] CustomGlareDef { get { return this.m_customGlareDef; } set { this.m_customGlareDef = value; } }

        public int CustomGlareDefIdx { get { return this.m_customGlareDefIdx; } set { this.m_customGlareDefIdx = value; } }

        public int CustomGlareDefAmount
        {
            get { return this.m_customGlareDefAmount; }
            set
            {
                if (value == this.m_customGlareDefAmount)
                {
                    return;
                }

                if (value == 0)
                {
                    this.m_customGlareDef = null;
                    this.m_customGlareDefIdx = 0;
                    this.m_customGlareDefAmount = 0;
                }
                else
                {

                    GlareDefData[] newArr = new GlareDefData[value];
                    for (int i = 0; i < value; i++)
                    {
                        if (i < this.m_customGlareDefAmount)
                        {
                            newArr[i] = this.m_customGlareDef[i];
                        }
                        else
                        {
                            newArr[i] = new GlareDefData();
                        }
                    }
                    this.m_customGlareDefIdx = Mathf.Clamp(this.m_customGlareDefIdx, 0, value - 1);
                    this.m_customGlareDef = newArr;
                    newArr = null;
                    this.m_customGlareDefAmount = value;
                }
            }
        }
    }

    public enum ApertureShape
    {
        Square,
        Hexagon,
        Octagon,
    }

    [Serializable]
    public class AmplifyBokehData
    {
        internal RenderTexture BokehRenderTexture;
        internal Vector4[] Offsets;

        public AmplifyBokehData(Vector4[] offsets)
        {
            this.Offsets = offsets;
        }

        public void Destroy()
        {
            if (this.BokehRenderTexture != null)
            {
                AmplifyUtils.ReleaseTempRenderTarget(this.BokehRenderTexture);
                this.BokehRenderTexture = null;
            }

            this.Offsets = null;
        }
    }

    [Serializable]
    public sealed class AmplifyBokeh : IAmplifyItem, ISerializationCallbackReceiver
    {
        //CONSTS
        private const int PerPassSampleCount = 8;

        //SERIALIZABLE VARIABLES
        [SerializeField]
        private bool m_isActive = false;

        [SerializeField]
        private bool m_applyOnBloomSource = false;

        [SerializeField]
        private float m_bokehSampleRadius = 0.5f;

        [SerializeField]
        private Vector4 m_bokehCameraProperties = new Vector4(0.05f, 0.018f, 1.34f, 0.18f); // x - aperture y - Focal Length z - Focal Distance w - Max CoC Diameter

        [SerializeField]
        private float m_offsetRotation = 0;

        [SerializeField]
        private ApertureShape m_apertureShape = ApertureShape.Hexagon;

        private List<AmplifyBokehData> m_bokehOffsets;

        public AmplifyBokeh()
        {
            this.m_bokehOffsets = new List<AmplifyBokehData>();
            this.CreateBokehOffsets(ApertureShape.Hexagon);
        }

        public void Destroy()
        {
            for (int i = 0; i < this.m_bokehOffsets.Count; i++)
            {
                this.m_bokehOffsets[i].Destroy();
            }
        }

        void CreateBokehOffsets(ApertureShape shape)
        {
            this.m_bokehOffsets.Clear();
            switch (shape)
            {
                case ApertureShape.Square:
                {
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation)));
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation + 90)));
                }
                    break;
                case ApertureShape.Hexagon:
                {
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation)));
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation - 75)));
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation + 75)));
                }
                    break;
                case ApertureShape.Octagon:
                {
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation)));
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation + 65)));
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation + 90)));
                    this.m_bokehOffsets.Add(new AmplifyBokehData(this.CalculateBokehSamples(PerPassSampleCount, this.m_offsetRotation + 115)));

                }
                    break;
            }
        }

        Vector4[] CalculateBokehSamples(int sampleCount, float angle)
        {
            Vector4[] bokehSamples = new Vector4[sampleCount];
            float angleRad = Mathf.Deg2Rad * angle;
            float aspectRatio = (float)Screen.width / (float)Screen.height;
            Vector4 samplePoint = new Vector4(this.m_bokehSampleRadius * Mathf.Cos(angleRad), this.m_bokehSampleRadius * Mathf.Sin(angleRad));
            samplePoint.x /= aspectRatio;
            for (int i = 0; i < sampleCount; i++)
            {
                float sampleInterp = (float)i / ((float)sampleCount - 1.0f);
                bokehSamples[i] = Vector4.Lerp(-samplePoint, samplePoint, sampleInterp);
            }
            return bokehSamples;
        }

        public void ApplyBokehFilter(RenderTexture source, Material material)
        {
            // ALLOCATE RENDER TEXTURES
            for (int i = 0; i < this.m_bokehOffsets.Count; i++)
            {
                this.m_bokehOffsets[i].BokehRenderTexture = AmplifyUtils.GetTempRenderTarget(source.width, source.height);
            }

            // SET CAMERA PARAMS AND APPLY EACH ROTATIONAL WEIGHTS
            material.SetVector(AmplifyUtils.BokehParamsId, this.m_bokehCameraProperties);

            for (int bId = 0; bId < this.m_bokehOffsets.Count; bId++)
            {
                for (int i = 0; i < PerPassSampleCount; i++)
                {
                    material.SetVector(AmplifyUtils.AnamorphicGlareWeightsStr[i], this.m_bokehOffsets[bId].Offsets[i]);
                }
                Graphics.Blit(source, this.m_bokehOffsets[bId].BokehRenderTexture, material, (int)BloomPasses.BokehWeightedBlur);
            }

            for (int bId = 0; bId < this.m_bokehOffsets.Count - 1; bId++)
            {
                material.SetTexture(AmplifyUtils.AnamorphicRTS[bId], this.m_bokehOffsets[bId].BokehRenderTexture);

            }

            // FINAL COMPOSITION
            source.DiscardContents();
            Graphics.Blit(this.m_bokehOffsets[this.m_bokehOffsets.Count - 1].BokehRenderTexture, source, material, (int)BloomPasses.BokehComposition2S + (this.m_bokehOffsets.Count - 2));

            //RELEASE RENDER TEXTURES
            for (int i = 0; i < this.m_bokehOffsets.Count; i++)
            {
                AmplifyUtils.ReleaseTempRenderTarget(this.m_bokehOffsets[i].BokehRenderTexture);
                this.m_bokehOffsets[i].BokehRenderTexture = null;
            }
        }

        public void OnAfterDeserialize()
        {
            this.CreateBokehOffsets(this.m_apertureShape);
        }

        public void OnBeforeSerialize()
        {

        }

        public ApertureShape ApertureShape
        {
            get { return this.m_apertureShape; }
            set
            {
                if (this.m_apertureShape != value)
                {
                    this.m_apertureShape = value;
                    this.CreateBokehOffsets(value);
                }
            }
        }

        public bool ApplyBokeh { get { return this.m_isActive; } set { this.m_isActive = value; } }


        public bool ApplyOnBloomSource { get { return this.m_applyOnBloomSource; } set { this.m_applyOnBloomSource = value; } }

        public float BokehSampleRadius { get { return this.m_bokehSampleRadius; } set { this.m_bokehSampleRadius = value; } }

        public float OffsetRotation { get { return this.m_offsetRotation; } set { this.m_offsetRotation = value; } }

        public Vector4 BokehCameraProperties { get { return this.m_bokehCameraProperties; } set { this.m_bokehCameraProperties = value; } }

        public float Aperture { get { return this.m_bokehCameraProperties.x; } set { this.m_bokehCameraProperties.x = value; } }

        public float FocalLength { get { return this.m_bokehCameraProperties.y; } set { this.m_bokehCameraProperties.y = value; } }

        public float FocalDistance { get { return this.m_bokehCameraProperties.z; } set { this.m_bokehCameraProperties.z = value; } }

        public float MaxCoCDiameter { get { return this.m_bokehCameraProperties.w; } set { this.m_bokehCameraProperties.w = value; } }
    }

    public enum LogType
    {
        Normal,
        Warning,
        Error
    }

    public class AmplifyUtils
    {
        public static int MaskTextureId;
        public static int BlurRadiusId;
        public static string HighPrecisionKeyword = "AB_HIGH_PRECISION";
        public static string ShaderModeTag = "Mode";
        public static string ShaderModeValue = "Full";
        public static string DebugStr = "[AmplifyBloom] ";
        public static int UpscaleContributionId;
        public static int SourceContributionId;
        public static int LensStarburstRTId;
        public static int LensDirtRTId;
        public static int LensFlareRTId;
        public static int LensGlareRTId;
        public static int[] MipResultsRTS;
        public static int[] AnamorphicRTS;
        public static int[] AnamorphicGlareWeightsMatStr;
        public static int[] AnamorphicGlareOffsetsMatStr;
        public static int[] AnamorphicGlareWeightsStr;
        public static int[] UpscaleWeightsStr;
        public static int[] LensDirtWeightsStr;
        public static int[] LensStarburstWeightsStr;
        public static int BloomRangeId;
        public static int LensDirtStrengthId;
        public static int BloomParamsId;
        public static int TempFilterValueId;
        public static int LensFlareStarMatrixId;
        public static int LensFlareStarburstStrengthId;
        public static int LensFlareGhostsParamsId;
        public static int LensFlareLUTId;
        public static int LensFlareHaloParamsId;
        public static int LensFlareGhostChrDistortionId;
        public static int LensFlareHaloChrDistortionId;
        public static int BokehParamsId = -1;
        public static RenderTextureFormat CurrentRTFormat = RenderTextureFormat.DefaultHDR;
        public static FilterMode CurrentFilterMode = FilterMode.Bilinear;
        public static TextureWrapMode CurrentWrapMode = TextureWrapMode.Clamp;
        public static RenderTextureReadWrite CurrentReadWriteMode = RenderTextureReadWrite.sRGB;
        public static bool IsInitialized = false;

        private static readonly List<RenderTexture> _allocatedRT = new List<RenderTexture>();

        public static void InitializeIds()
        {
            IsInitialized = true;
            MaskTextureId = Shader.PropertyToID("_MaskTex");

            MipResultsRTS = new[]
            {
                Shader.PropertyToID("_MipResultsRTS0"),
                Shader.PropertyToID("_MipResultsRTS1"),
                Shader.PropertyToID("_MipResultsRTS2"),
                Shader.PropertyToID("_MipResultsRTS3"),
                Shader.PropertyToID("_MipResultsRTS4"),
                Shader.PropertyToID("_MipResultsRTS5")
            };

            AnamorphicRTS = new[]
            {
                Shader.PropertyToID("_AnamorphicRTS0"),
                Shader.PropertyToID("_AnamorphicRTS1"),
                Shader.PropertyToID("_AnamorphicRTS2"),
                Shader.PropertyToID("_AnamorphicRTS3"),
                Shader.PropertyToID("_AnamorphicRTS4"),
                Shader.PropertyToID("_AnamorphicRTS5"),
                Shader.PropertyToID("_AnamorphicRTS6"),
                Shader.PropertyToID("_AnamorphicRTS7")
            };

            AnamorphicGlareWeightsMatStr = new[]
            {
                Shader.PropertyToID("_AnamorphicGlareWeightsMat0"),
                Shader.PropertyToID("_AnamorphicGlareWeightsMat1"),
                Shader.PropertyToID("_AnamorphicGlareWeightsMat2"),
                Shader.PropertyToID("_AnamorphicGlareWeightsMat3")
            };


            AnamorphicGlareOffsetsMatStr = new[]
            {
                Shader.PropertyToID("_AnamorphicGlareOffsetsMat0"),
                Shader.PropertyToID("_AnamorphicGlareOffsetsMat1"),
                Shader.PropertyToID("_AnamorphicGlareOffsetsMat2"),
                Shader.PropertyToID("_AnamorphicGlareOffsetsMat3")
            };

            AnamorphicGlareWeightsStr = new[]
            {
                Shader.PropertyToID("_AnamorphicGlareWeights0"),
                Shader.PropertyToID("_AnamorphicGlareWeights1"),
                Shader.PropertyToID("_AnamorphicGlareWeights2"),
                Shader.PropertyToID("_AnamorphicGlareWeights3"),
                Shader.PropertyToID("_AnamorphicGlareWeights4"),
                Shader.PropertyToID("_AnamorphicGlareWeights5"),
                Shader.PropertyToID("_AnamorphicGlareWeights6"),
                Shader.PropertyToID("_AnamorphicGlareWeights7"),
                Shader.PropertyToID("_AnamorphicGlareWeights8"),
                Shader.PropertyToID("_AnamorphicGlareWeights9"),
                Shader.PropertyToID("_AnamorphicGlareWeights10"),
                Shader.PropertyToID("_AnamorphicGlareWeights11"),
                Shader.PropertyToID("_AnamorphicGlareWeights12"),
                Shader.PropertyToID("_AnamorphicGlareWeights13"),
                Shader.PropertyToID("_AnamorphicGlareWeights14"),
                Shader.PropertyToID("_AnamorphicGlareWeights15")
            };

            UpscaleWeightsStr = new[]
            {
                Shader.PropertyToID("_UpscaleWeights0"),
                Shader.PropertyToID("_UpscaleWeights1"),
                Shader.PropertyToID("_UpscaleWeights2"),
                Shader.PropertyToID("_UpscaleWeights3"),
                Shader.PropertyToID("_UpscaleWeights4"),
                Shader.PropertyToID("_UpscaleWeights5"),
                Shader.PropertyToID("_UpscaleWeights6"),
                Shader.PropertyToID("_UpscaleWeights7")
            };

            LensDirtWeightsStr = new[]
            {
                Shader.PropertyToID("_LensDirtWeights0"),
                Shader.PropertyToID("_LensDirtWeights1"),
                Shader.PropertyToID("_LensDirtWeights2"),
                Shader.PropertyToID("_LensDirtWeights3"),
                Shader.PropertyToID("_LensDirtWeights4"),
                Shader.PropertyToID("_LensDirtWeights5"),
                Shader.PropertyToID("_LensDirtWeights6"),
                Shader.PropertyToID("_LensDirtWeights7")
            };

            LensStarburstWeightsStr = new[]
            {
                Shader.PropertyToID("_LensStarburstWeights0"),
                Shader.PropertyToID("_LensStarburstWeights1"),
                Shader.PropertyToID("_LensStarburstWeights2"),
                Shader.PropertyToID("_LensStarburstWeights3"),
                Shader.PropertyToID("_LensStarburstWeights4"),
                Shader.PropertyToID("_LensStarburstWeights5"),
                Shader.PropertyToID("_LensStarburstWeights6"),
                Shader.PropertyToID("_LensStarburstWeights7")
            };

            BloomRangeId = Shader.PropertyToID("_BloomRange");
            LensDirtStrengthId = Shader.PropertyToID("_LensDirtStrength");
            BloomParamsId = Shader.PropertyToID("_BloomParams");
            TempFilterValueId = Shader.PropertyToID("_TempFilterValue");
            LensFlareStarMatrixId = Shader.PropertyToID("_LensFlareStarMatrix");
            LensFlareStarburstStrengthId = Shader.PropertyToID("_LensFlareStarburstStrength");
            LensFlareGhostsParamsId = Shader.PropertyToID("_LensFlareGhostsParams");
            LensFlareLUTId = Shader.PropertyToID("_LensFlareLUT");
            LensFlareHaloParamsId = Shader.PropertyToID("_LensFlareHaloParams");
            LensFlareGhostChrDistortionId = Shader.PropertyToID("_LensFlareGhostChrDistortion");
            LensFlareHaloChrDistortionId = Shader.PropertyToID("_LensFlareHaloChrDistortion");
            BokehParamsId = Shader.PropertyToID("_BokehParams");
            BlurRadiusId = Shader.PropertyToID("_BlurRadius");
            LensStarburstRTId = Shader.PropertyToID("_LensStarburst");
            LensDirtRTId = Shader.PropertyToID("_LensDirt");
            LensFlareRTId = Shader.PropertyToID("_LensFlare");
            LensGlareRTId = Shader.PropertyToID("_LensGlare");
            SourceContributionId = Shader.PropertyToID("_SourceContribution");
            UpscaleContributionId = Shader.PropertyToID("_UpscaleContribution");
        }

        public static void DebugLog(string value, LogType type)
        {
            switch (type)
            {
                case LogType.Normal:
                    Debug.Log(DebugStr + value);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(DebugStr + value);
                    break;
                case LogType.Error:
                    Debug.LogError(DebugStr + value);
                    break;
            }
        }

        public static RenderTexture GetTempRenderTarget(int width, int height)
        {
            RenderTexture newRT = RenderTexture.GetTemporary(width, height, 0, CurrentRTFormat, CurrentReadWriteMode);
            newRT.filterMode = CurrentFilterMode;
            newRT.wrapMode = CurrentWrapMode;
            _allocatedRT.Add(newRT);
            return newRT;
        }

        public static void ReleaseTempRenderTarget(RenderTexture renderTarget)
        {
            if (renderTarget != null && _allocatedRT.Remove(renderTarget))
            {
                renderTarget.DiscardContents();
                RenderTexture.ReleaseTemporary(renderTarget);
            }
        }

        public static void ReleaseAllRT()
        {
            for (int i = 0; i < _allocatedRT.Count; i++)
            {
                _allocatedRT[i].DiscardContents();
                RenderTexture.ReleaseTemporary(_allocatedRT[i]);
            }
            _allocatedRT.Clear();
        }

        public static void EnsureKeywordEnabled(Material mat, string keyword, bool state)
        {
            if (mat != null)
            {
                if (state && !mat.IsKeywordEnabled(keyword))
                    mat.EnableKeyword(keyword);
                else if (!state && mat.IsKeywordEnabled(keyword))
                    mat.DisableKeyword(keyword);
            }
        }
    }

    interface IAmplifyItem
    {
        void Destroy();
    }

    // Star generation

    // Define each line of the star.
    [Serializable]
    public class StarLineData
    {
        [SerializeField]
        internal int PassCount;
        [SerializeField]
        internal float SampleLength;
        [SerializeField]
        internal float Attenuation;
        [SerializeField]
        internal float Inclination;
    };

    // Star form library
    public enum StarLibType
    {
        Cross = 0,
        Cross_Filter,
        Snow_Cross,
        Vertical,
        Sunny_Cross
    };

    // Simple definition of the star.
    [Serializable]
    public class StarDefData
    {
        [SerializeField]
        private StarLibType m_starType = StarLibType.Cross;
        [SerializeField]
        private string m_starName = string.Empty;
        [SerializeField]
        private int m_starlinesCount = 2;
        [SerializeField]
        private int m_passCount = 4;
        [SerializeField]
        private float m_sampleLength = 1;
        [SerializeField]
        private float m_attenuation = 0.85f;
        [SerializeField]
        private float m_inclination = 0;
        [SerializeField]
        private float m_rotation = 0;
        [SerializeField]
        private StarLineData[] m_starLinesArr = null;
        [SerializeField]
        private float m_customIncrement = 90;
        [SerializeField]
        private float m_longAttenuation = 0;

        public StarDefData()
        {
        }

        public void Destroy()
        {
            this.m_starLinesArr = null;
        }

        public StarDefData(StarLibType starType, string starName, int starLinesCount, int passCount, float sampleLength, float attenuation, float inclination, float rotation, float longAttenuation = 0, float customIncrement = -1)
        {
            this.m_starType = starType;

            this.m_starName = starName;
            this.m_passCount = passCount;
            this.m_sampleLength = sampleLength;
            this.m_attenuation = attenuation;
            this.m_starlinesCount = starLinesCount;
            this.m_inclination = inclination;
            this.m_rotation = rotation;
            this.m_customIncrement = customIncrement;
            this.m_longAttenuation = longAttenuation;
            this.CalculateStarData();
        }

        public void CalculateStarData()
        {
            if (this.m_starlinesCount == 0)
                return;

            this.m_starLinesArr = new StarLineData[this.m_starlinesCount];
            float fInc = (this.m_customIncrement > 0) ? this.m_customIncrement : (180.0f / (float)this.m_starlinesCount);
            fInc *= Mathf.Deg2Rad;
            for (int i = 0; i < this.m_starlinesCount; i++)
            {
                this.m_starLinesArr[i] = new StarLineData
                {
                    PassCount = this.m_passCount,
                    SampleLength = this.m_sampleLength
                };
                if (this.m_longAttenuation > 0)
                {
                    this.m_starLinesArr[i].Attenuation = ((i % 2) == 0) ? this.m_longAttenuation : this.m_attenuation;
                }
                else
                {
                    this.m_starLinesArr[i].Attenuation = this.m_attenuation;
                }
                this.m_starLinesArr[i].Inclination = fInc * (float)i;
            }
        }


        public StarLibType StarType { get { return this.m_starType; } set { this.m_starType = value; } }

        public string StarName { get { return this.m_starName; } set { this.m_starName = value; } }

        public int StarlinesCount
        {
            get { return this.m_starlinesCount; }
            set
            {
                this.m_starlinesCount = value;
                this.CalculateStarData();
            }
        }



        public int PassCount
        {
            get { return this.m_passCount; }
            set
            {
                this.m_passCount = value;
                this.CalculateStarData();
            }
        }


        public float SampleLength
        {
            get { return this.m_sampleLength; }
            set
            {
                this.m_sampleLength = value;
                this.CalculateStarData();
            }
        }


        public float Attenuation
        {
            get { return this.m_attenuation; }
            set
            {
                this.m_attenuation = value;
                this.CalculateStarData();
            }
        }

        public float Inclination
        {
            get { return this.m_inclination; }
            set
            {
                this.m_inclination = value;
                this.CalculateStarData();
            }
        }
        public float CameraRotInfluence { get { return this.m_rotation; } set { this.m_rotation = value; } }

        public StarLineData[] StarLinesArr { get { return this.m_starLinesArr; } }

        public float CustomIncrement
        {
            get { return this.m_customIncrement; }
            set
            {
                this.m_customIncrement = value;
                this.CalculateStarData();
            }
        }

        public float LongAttenuation
        {
            get { return this.m_longAttenuation; }
            set
            {
                this.m_longAttenuation = value;
                this.CalculateStarData();
            }
        }
    }

    [Serializable]
    public class AmplifyPassCache
    {
        [SerializeField]
        internal Vector4[] Offsets;

        [SerializeField]
        internal Vector4[] Weights;

        public AmplifyPassCache()
        {
            this.Offsets = new Vector4[16];
            this.Weights = new Vector4[16];
        }

        public void Destroy()
        {
            this.Offsets = null;
            this.Weights = null;
        }
    }

    [Serializable]
    public class AmplifyStarlineCache
    {
        [SerializeField]
        internal AmplifyPassCache[] Passes;

        public AmplifyStarlineCache()
        {
            this.Passes = new AmplifyPassCache[AmplifyGlare.MaxPasses];
            for (int i = 0; i < AmplifyGlare.MaxPasses; i++)
            {
                this.Passes[i] = new AmplifyPassCache();
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < AmplifyGlare.MaxPasses; i++)
            {
                this.Passes[i].Destroy();
            }
            this.Passes = null;
        }
    }

    [Serializable]
    public class AmplifyGlareCache
    {
        [SerializeField]
        internal AmplifyStarlineCache[] Starlines;

        [SerializeField]
        internal Vector4 AverageWeight;

        [SerializeField]
        internal Vector4[,] CromaticAberrationMat;

        [SerializeField]
        internal int TotalRT;

        [SerializeField]
        internal GlareDefData GlareDef;

        [SerializeField]
        internal StarDefData StarDef;

        [SerializeField]
        internal int CurrentPassCount;

        public AmplifyGlareCache()
        {
            this.Starlines = new AmplifyStarlineCache[AmplifyGlare.MaxStarLines];
            this.CromaticAberrationMat = new Vector4[AmplifyGlare.MaxPasses, AmplifyGlare.MaxLineSamples];
            for (int i = 0; i < AmplifyGlare.MaxStarLines; i++)
            {
                this.Starlines[i] = new AmplifyStarlineCache();
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < AmplifyGlare.MaxStarLines; i++)
            {
                this.Starlines[i].Destroy();
            }
            this.Starlines = null;
            this.CromaticAberrationMat = null;
        }
    }
}
