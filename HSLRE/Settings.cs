using System;
using System.Collections.Generic;
using Utility.Xml;
using Config;
using UnityEngine;
using HSLRE.CustomEffects;
using System.IO;
using ToolBox;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CinematicEffects;
using UnityStandardAssets.ImageEffects;
using Bloom = HSLRE.CustomEffects.Bloom;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using TonemappingColorGrading = HSLRE.CustomEffects.TonemappingColorGrading;

namespace HSLRE
{
    public static class Settings
    {
        public class AmplifyBloomSetting : BaseSystem
        {
            public AmplifyBloomSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = false;
                this.UpscaleQuality = "Realistic";
                this.MainThresholdSize = "Full";
                this.CurrentPrecisionMode = "Low";
                this.BloomRange = 500;
                this.OverallIntensity = 0.8f;
                this.OverallThreshold = 0.53f;
                this.BloomScale = 1f;
                this.UpscaleBlurRadius = 1f;
                this.TemporalFilteringActive = false;
                this.TemporalFilteringValue = 0.05f;
                this.SeparateFeaturesThreshold = false;
                this.FeaturesThreshold = 0.05f;
                this.ApplyLensDirt = true;
                this.LensDirtStrength = 2;
                this.LensDirtTexture = "DirtHighContrast";
                this.ApplyLensStardurst = true;
                this.LensStarburstStrength = 2;
                this.LensStardurstTex = "Starburst";
                this.BokehFilterInstanceApplyBokeh = false;
                this.BokehFilterInstanceApplyOnBloomSource = false;
                this.BokehFilterInstanceApertureShape = "Hexagon";
                this.BokehFilterInstanceOffsetRotation = 0;
                this.BokehFilterInstanceBokehSampleRadius = 0.5f;
                this.BokehFilterInstanceAperture = 0.05f;
                this.BokehFilterInstanceFocalLength = 0.018f;
                this.BokehFilterInstanceFocalDistance = 1.34f;
                this.BokehFilterInstanceMaxCoCDiameter = 0.18f;
                this.LensFlareInstanceApplyLensFlare = true;
                this.LensFlareInstanceOverallIntensity = 1;
                this.LensFlareInstanceLensFlareGaussianBlurAmount = 1;
                this.LensFlareInstanceLensFlareNormalizedGhostsIntensity = 0.8f;
                this.LensFlareInstanceLensFlareGhostAmount = 3;
                this.LensFlareInstanceLensFlareGhostsDispersal = 0.228f;
                this.LensFlareInstanceLensFlareGhostChrDistortion = 2;
                this.LensFlareInstanceLensFlareGhostsPowerFactor = 1;
                this.LensFlareInstanceLensFlareGhostsPowerFalloff = 4;
                this.LensFlareInstanceLensFlareNormalizedHaloIntensity = 0.1f;
                this.LensFlareInstanceLensFlareHaloWidth = 0.573f;
                this.LensFlareInstanceLensFlareHaloChrDistortion = 1.51f;
                this.LensFlareInstanceLensFlareHaloPowerFactor = 1;
                this.LensFlareInstanceLensFlareHaloPowerFalloff = 128;
                this.LensGlareInstanceApplyLensGlare = true;
                this.LensGlareInstanceIntensity = 0.17f;
                this.LensGlareInstanceOverallStreakScale = 1;
                this.LensGlareInstanceOverallTint = Color.white;
                this.LensGlareInstanceCurrentGlare = "CheapLens";
                this.LensGlareInstancePerPassDisplacement = 4;
                this.LensGlareInstanceGlareMaxPassCount = 4;
                this.fixUpscaledScreenshots = true;
            }

            public bool enabled;
            public int LensGlareInstanceGlareMaxPassCount;
            public float LensGlareInstancePerPassDisplacement;
            public string LensGlareInstanceCurrentGlare;
            public Color LensGlareInstanceOverallTint;
            public float LensGlareInstanceOverallStreakScale;
            public float LensGlareInstanceIntensity;
            public bool LensGlareInstanceApplyLensGlare;
            public float LensFlareInstanceLensFlareHaloPowerFalloff;
            public float LensFlareInstanceLensFlareHaloPowerFactor;
            public float LensFlareInstanceLensFlareHaloChrDistortion;
            public float LensFlareInstanceLensFlareHaloWidth;
            public float LensFlareInstanceLensFlareNormalizedHaloIntensity;
            public float LensFlareInstanceLensFlareGhostsPowerFalloff;
            public float LensFlareInstanceLensFlareGhostsPowerFactor;
            public float LensFlareInstanceLensFlareGhostChrDistortion;
            public float LensFlareInstanceLensFlareGhostsDispersal;
            public int LensFlareInstanceLensFlareGhostAmount;
            public float LensFlareInstanceLensFlareNormalizedGhostsIntensity;
            public int LensFlareInstanceLensFlareGaussianBlurAmount;
            public float LensFlareInstanceOverallIntensity;
            public bool LensFlareInstanceApplyLensFlare;
            public float BokehFilterInstanceMaxCoCDiameter;
            public float BokehFilterInstanceFocalDistance;
            public float BokehFilterInstanceFocalLength;
            public float BokehFilterInstanceAperture;
            public float BokehFilterInstanceBokehSampleRadius;
            public float BokehFilterInstanceOffsetRotation;
            public string BokehFilterInstanceApertureShape;
            public bool BokehFilterInstanceApplyOnBloomSource;
            public bool BokehFilterInstanceApplyBokeh;
            public string LensStardurstTex;
            public float LensStarburstStrength;
            public bool ApplyLensStardurst;
            public string LensDirtTexture;
            public float LensDirtStrength;
            public bool ApplyLensDirt;
            public float FeaturesThreshold;
            public bool SeparateFeaturesThreshold;
            public float TemporalFilteringValue;
            public bool TemporalFilteringActive;
            public float UpscaleBlurRadius;
            public float BloomScale;
            public float OverallThreshold;
            public float OverallIntensity;
            public float BloomRange;
            public string CurrentPrecisionMode;
            public string MainThresholdSize;
            public string UpscaleQuality;
            public bool fixUpscaledScreenshots;
        }
        public class AntialiasingSetting : BaseSystem
        {
            public AntialiasingSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = false;
                this.mode = AAMode.FXAA3Console;
                this.showGeneratedNormals = false;
                this.offsetScale = 0.2f;
                this.blurRadius = 18f;
                this.edgeThresholdMin = 0.05f;
                this.edgeThreshold = 0.2f;
                this.edgeSharpness = 4f;
                this.dlaaSharp = false;
            }

            public bool enabled = false;
            public AAMode mode;
            public bool showGeneratedNormals;
            public float offsetScale;
            public float blurRadius;
            public float edgeThresholdMin;
            public float edgeThreshold;
            public float edgeSharpness;
            public bool dlaaSharp;
        }
        public class SMAASetting : BaseSystem
        {
            public SMAASetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = true;
                this.quality = 3;
                this.edgeDetectionMethod = 2;
                this.diagonalDetection = true;
                this.cornerDetection = true;
                this.threshold = 0.05f;
                this.depthThreshold = 0.01f;
                this.maxSearchSteps = 32;
                this.maxDiagonalSearchSteps = 16;
                this.cornerRounding = 25;
                this.localContrastAdaptationFactor = 2f;
                this.predication = false;
                this.predicationThreshold = 0.01f;
                this.predicationScale = 2f;
                this.predicationStrength = 0.4f;
                this.temporal = false;
                this.temporalFuzzSize = 2f;

            }

            public bool enabled;
            public int quality;
            public int edgeDetectionMethod;
            public bool diagonalDetection;
            public bool cornerDetection;
            public float threshold;
            public float depthThreshold;
            public int maxSearchSteps;
            public int maxDiagonalSearchSteps;
            public int cornerRounding;
            public float localContrastAdaptationFactor;
            public bool predication;
            public float predicationThreshold;
            public float predicationScale;
            public float predicationStrength;
            public bool temporal;
            public float temporalFuzzSize;
        }
        public class BasicSetting : BaseSystem
        {
            public BasicSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.msaa = 8;
                this.CameraFOV = 23f;
                this.DisableFog = false;
                this.CharaMakerReform = false;
                this.CharaMakerBackgroundType = (int)CameraClearFlags.Skybox;
                this.CharaMakerBackground = new Color(0.192157f, 0.301961f, 0.47451f, 1f);
            }

            public int msaa;
            public float CameraFOV;
            public bool CharaMakerReform;
            public int CharaMakerBackgroundType;
            public Color CharaMakerBackground;
            public bool DisableFog;
        }
        public class BloomSetting : BaseSystem
        {
            public BloomSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = true;
                this.threshold = 0.9f;
                this.softKnee = 0.5f;
                this.raduis = 2f;
                this.intensity = 0.7f;
                this.antiFlicker = false;
            }

            public bool enabled;

            public float intensity;

            public float threshold;

            public float softKnee;

            public float raduis;

            public bool antiFlicker;
        }
        public class BloomAndFlaresSetting : BaseSystem
        {
            public BloomAndFlaresSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.BloomEnabled = false;
                this.Bloomintensity = 0.42f;
                this.BloomThreshold = 0.33f;
                this.BloomBlurSpread = 0.8f;
                this.BloomScreenBlendMode = "Screen";
                this.BloomHdr = "Auto";
                this.BloomBlurIterations = 2;
                this.BloomUseSrcAlphaAsMask = 0f;
                this.BloomLensflares = false;
                this.BloomLensflareMode = "Anamorphic";
                this.BloomLensflareIntensity = 1f;
                this.BloomLensflareThreshold = 0.3f;
                this.BloomLensFlareStretchWidth = 3.5f;
                this.BloomLensFlareBlurIterations = 2;
                this.BloomFlareColorA = new Color32(102, 102, 204, 191);
                this.BloomFlareColorB = new Color32(102, 204, 204, 191);
                this.BloomFlareColorC = new Color32(204, 102, 204, 191);
                this.BloomFlareColorD = new Color32(204, 102, 0, 191);
            }

            public bool BloomEnabled;
            public float Bloomintensity;
            public float BloomThreshold;
            public float BloomBlurSpread;
            public string BloomScreenBlendMode;
            public string BloomHdr;
            public int BloomBlurIterations;
            public float BloomUseSrcAlphaAsMask;
            public bool BloomLensflares;
            public string BloomLensflareMode;
            public float BloomLensflareIntensity;
            public float BloomLensflareThreshold;
            public float BloomLensFlareStretchWidth;
            public int BloomLensFlareBlurIterations;
            public Color BloomFlareColorA;
            public Color BloomFlareColorB;
            public Color BloomFlareColorC;
            public Color BloomFlareColorD;
        }
        public class CurveSetting : BaseSystem
        {
            public CurveSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.Enabled = false;
                this.Curve = 1;
                this.CurveName = "SampleCurve2";
                this.CurveSaturation = 1f;
            }

            public bool Enabled;

            public int Curve;

            public string CurveName;

            public float CurveSaturation;
        }
        public class DepthOfFieldSetting : BaseSystem
        {
            public DepthOfFieldSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = true;
                this.focalLength = 10f;
                this.focalSize = 1f;
                this.aperture = 0.7f;
                this.maxBlurSize = 2f;
                this.highResolution = true;
                this.blurType = "DiscBlur";
                this.blurSampleCount = "High";
                this.nearBlur = false;
                this.foregroundOverlap = 1;
                this.dx11BokehThreshold = 0.5f;
                this.dx11SpawnHeuristic = 0.0875f;
                this.dx11BokehScale = 1.2f;
                this.dx11BokehIntensity = 2.5f;
                this.fixUpscaledScreenshots = true;
            }

            public bool enabled;
            public float focalLength;
            public float focalSize;
            public float aperture;
            public float maxBlurSize;
            public bool highResolution;
            public string blurType;
            public string blurSampleCount;
            public bool nearBlur;
            public float foregroundOverlap;
            public float dx11BokehThreshold;
            public float dx11SpawnHeuristic;
            public float dx11BokehScale;
            public float dx11BokehIntensity;
            public bool fixUpscaledScreenshots;

        }
        public class LensAberrationSetting : BaseSystem
        {
            public LensAberrationSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.DistortionEnabled = false;
                this.DistortionAmount = 0f;
                this.DistortionAmountX = 1f;
                this.DistortionAmountY = 1f;
                this.DistortionScale = 1f;
                this.VignetteEnabled = false;
                this.VignetteBlur = 0f;
                this.VignetteIntensity = 1.4f;
                this.VignetteSmoothness = 0.8f;
                this.VignetteRoundness = 1f;
                this.VignetteDesaturate = 0f;
                this.VignetteColor = Color.black;
                this.ChromaticAberrationEnabled = false;
                this.ChromaticAberrationAmount = 0f;
                this.ChromaticColor = Color.green;
            }

            public float DistortionAmount;

            public float DistortionScale;

            public float DistortionAmountX;

            public float DistortionAmountY;

            public bool DistortionEnabled;

            public bool VignetteEnabled;

            public float VignetteBlur;

            public Color VignetteColor;

            public float VignetteIntensity;

            public float VignetteSmoothness;

            public float VignetteRoundness;

            public float VignetteDesaturate;

            public Color ChromaticColor;

            public bool ChromaticAberrationEnabled;

            public float ChromaticAberrationAmount;
        }

        public class NoiseAndGrainSetting : BaseSystem
        {
            public NoiseAndGrainSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = false;
                this.intensityMultiplier = 0.25f;
                this.generalIntensity = 0.5f;
                this.blackIntensity = 1f;
                this.whiteIntensity = 1f;
                this.midGrey = 0.2f;
                this.dx11Grain = false;
                this.softness = 0;
                this.monochrome = false;
                this.intensities = new Vector3(1f, 1f, 1f);
                this.tiling = new Vector3(64f, 64f, 64f);
                this.monochromeTiling = 64f;
                this.filterMode = "Bilinear";
                this.fixUpscaledScreenshots = true;
            }

            public bool enabled;
            public bool dx11Grain;
            public bool monochrome;
            public float intensityMultiplier;
            public float generalIntensity;
            public float blackIntensity;
            public float whiteIntensity;
            public float midGrey;
            public Vector3 intensities;
            public string filterMode;
            public float softness;
            public float monochromeTiling;
            public Vector3 tiling;
            public bool fixUpscaledScreenshots;
        }
        public class Preset : BaseSystem
        {
            public Preset(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.ImageEffectPreset = "Default";
                this.ShadowPreset = "Default";
                this.StylePreset = "Default";
                this.CommonPreset = "Default";
            }

            public string ImageEffectPreset;
            public string ShadowPreset;
            public string StylePreset;
            public string CommonPreset;
        }
        public class ShadowSetting : BaseSystem
        {
            public ShadowSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.ShadowDistance = 20f;
                this.ShadowProjection = 0;
                this.ShadowCascades = 4;
                this.ShadowCascade2Split = 0.3333333f;
                this.ShadowCascade4Split_x = 0.06666667f;
                this.ShadowCascade4Split_y = 0.2f;
                this.ShadowCascade4Split_z = 0.4666667f;
                this.ShadowNearPlaneOffset = 2f;
            }

            public float ShadowDistance;

            public int ShadowProjection;

            public int ShadowCascades;

            public float ShadowCascade2Split;

            public float ShadowCascade4Split_x;

            public float ShadowCascade4Split_y;

            public float ShadowCascade4Split_z;

            public float ShadowNearPlaneOffset;
        }

        public class SEGISetting : BaseSystem
        {
            public SEGISetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = false;
                this.voxelResolution = "high";
                this.voxelAA = true;
                this.innerOcclusionLayers = 1;
                this.gaussianMipFilter = true;
                this.voxelSpaceSize = 12f;
                this.shadowSpaceSize = 12f;
                this.updateGI = true;
                this.infiniteBounces = true;
                this.hsStandardCustomShadersCompatibility = false;

                this.softSunlight = 0f;
                this.skyColor = Color.white;
                this.skyIntensity = 1f;
                this.sphericalSkylight = false;

                this.temporalBlendWeight = 1f;
                this.useBilateralFiltering = false;
                this.halfResolution = true;
                this.stochasticSampling = false;

                this.cones = 15;
                this.coneTraceSteps = 11;
                this.coneLength = 1f;
                this.coneWidth = 6f;
                this.coneTraceBias = 0.640f;
                this.occlusionStrength = 0.460f;
                this.nearOcclusionStrength = 0.240f;
                this.farOcclusionStrength = 1f;
                this.farthestOcclusionStrength = 0.620f;
                this.occlusionPower = 0.960f;
                this.nearLightGain = 0f;
                this.giGain = 1.360f;

                this.secondaryBounceGain = 1.640f;
                this.secondaryCones = 6;
                this.secondaryOcclusionStrength = 1;

                this.doReflections = true;
                this.reflectionSteps = 73;
                this.reflectionOcclusionPower = 1;
                this.skyReflectionIntensity = 1;
            }

            public bool enabled;
            public string voxelResolution;
            public bool voxelAA;
            public int innerOcclusionLayers;
            public bool gaussianMipFilter;
            public float voxelSpaceSize;
            public float shadowSpaceSize;
            public bool updateGI;
            public bool infiniteBounces;
            public bool hsStandardCustomShadersCompatibility;
            public float softSunlight;
            public Color skyColor;
            public float skyIntensity;
            public bool sphericalSkylight;
            public float temporalBlendWeight;
            public bool useBilateralFiltering;
            public bool halfResolution;
            public bool stochasticSampling;
            public int cones;
            public int coneTraceSteps;
            public float coneLength;
            public float coneWidth;
            public float coneTraceBias;
            public float occlusionStrength;
            public float nearOcclusionStrength;
            public float farOcclusionStrength;
            public float farthestOcclusionStrength;
            public float occlusionPower;
            public float nearLightGain;
            public float giGain;
            public float secondaryBounceGain;
            public int secondaryCones;
            public float secondaryOcclusionStrength;
            public bool doReflections;
            public int reflectionSteps;
            public float reflectionOcclusionPower;
            public float skyReflectionIntensity;
        }
        public class SSAOSetting : BaseSystem
        {
            public SSAOSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.SSAOEnabled = true;
                this.SSAOBias = 0.1f;
                this.SSAOBlurBilateralThreshold = 0.1f;
                this.SSAOBlurPasses = 1;
                this.SSAOCutoffDistance = 50f;
                this.SSAOfalloffDistance = 5f;
                this.SSAOLumContribution = 0.5f;
                this.SSAODistance = 0.560f;
                this.SSAORadius = 0.084f;
                this.SSAOSamples = "Medium";
                this.SSAOIntensity = 1.27f;
                this.SSAOUseHighPrecisionDepthMap = false;
                this.SSAODownsampling = 1;
                this.SSAOBlur = "Bilateral";
                this.SSAOBlurDownsampling = false;
                this.SSAOOcclusionColor = new Color32(23, 11, 11, 255);
            }

            public bool SSAOEnabled;
            public string SSAOSamples;
            public float SSAOBias;
            public float SSAOIntensity;
            public float SSAORadius;
            public float SSAOLumContribution;
            public float SSAODistance;
            public float SSAOCutoffDistance;
            public float SSAOfalloffDistance;
            public int SSAOBlurPasses;
            public float SSAOBlurBilateralThreshold;
            public bool SSAOUseHighPrecisionDepthMap;
            public int SSAODownsampling;
            public string SSAOBlur;
            public bool SSAOBlurDownsampling;
            public Color SSAOOcclusionColor;
        }
        public class SSRSetting : BaseSystem
        {
            public SSRSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.SSRenabled = true;
                this.SSRscreenEdgeFading = 0.396f;
                this.SSRmaxDistance = 465.0f;
                this.SSRfadeDistance = 262.0f;
                this.SSRreflectionMultiplier = 1.470f;
                this.SSRenableHDR = true;
                this.SSRadditiveReflection = false;
                this.SSRmaxSteps = 219;
                this.SSRrayStepSize = 3;
                this.SSRwidthModifier = 1.2f;
                this.SSRsmoothFallbackThreshold = 0.2f;
                this.SSRdistanceBlur = 1f;
                this.SSRfresnelFade = 0.2f;
                this.SSRfresnelFadePower = 2f;
                this.SSRsmoothFallbackDistance = 0.042f;
                this.SSRuseTemporalConfidence = true;
                this.SSRtemporalFilterStrength = 0.05f;
                this.SSRtreatBackfaceHitAsMiss = false;
                this.SSRallowBackwardsRays = false;
                this.SSRtraceBehindObjects = true;
                this.SSRhighQualitySharpReflections = true;
                this.SSRtraceEverywhere = false;
                this.SSRresolution = 2;
                this.SSRbilateralUpsample = true;
                this.SSRimproveCorners = true;
                this.SSRreduceBanding = true;
                this.SSRhighlightSuppression = false;
                this.SSRdebugMode = 0;
            }

            public bool SSRenabled;

            public float SSRscreenEdgeFading;

            public float SSRmaxDistance;

            public float SSRfadeDistance;

            public float SSRreflectionMultiplier;

            public bool SSRenableHDR;

            public bool SSRadditiveReflection;

            public int SSRmaxSteps;

            public int SSRrayStepSize;

            public float SSRwidthModifier;

            public float SSRsmoothFallbackThreshold;

            public float SSRdistanceBlur;

            public float SSRfresnelFade;

            public float SSRfresnelFadePower;

            public float SSRsmoothFallbackDistance;

            public bool SSRuseTemporalConfidence;

            public float SSRtemporalFilterStrength;

            public bool SSRtreatBackfaceHitAsMiss;

            public bool SSRallowBackwardsRays;

            public bool SSRtraceBehindObjects;

            public bool SSRhighQualitySharpReflections;

            public bool SSRtraceEverywhere;

            public int SSRresolution;

            public bool SSRbilateralUpsample;

            public bool SSRimproveCorners;

            public bool SSRreduceBanding;

            public bool SSRhighlightSuppression;

            public int SSRdebugMode;
        }
        public class SunShaftsSetting : BaseSystem
        {
            public SunShaftsSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = false;
                this.resolution = "Normal";
                this.screenBlendMode = "Screen";
                this.radialBlurIterations = 2;
                this.sunColor = Color.white;
                this.sunThreshold = Color.gray;
                this.sunShaftBlurRadius = 4;
                this.sunShaftIntensity = 3f;
                this.maxRadius = 0.825f;
                this.useDepthTexture = true;
            }

            public bool enabled;
            public string resolution;
            public string screenBlendMode;
            public int radialBlurIterations;
            public Color sunColor;
            public Color sunThreshold;
            public float sunShaftBlurRadius;
            public float sunShaftIntensity;
            public float maxRadius;
            public bool useDepthTexture;
        }
        public class ToneMapSetting : BaseSystem
        {
            public ToneMapSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.tonemapper = 1;
                this.exposure = 1f;
                this.Lut = false;
                this.LutName = "Neutral";
                this.LutContribution = 1f;
                this.temperatureShift = 0f;
                this.tint = 0f;
                this.contrast = 1f;
                this.hue = 0f;
                this.saturation = 1f;
                this.value = 1f;
                this.vibrance = 0f;
                this.gain = 1f;
                this.gamma = 1f;
                this.useDithering = true;
            }

            public int tonemapper;

            public float exposure;

            public bool Lut;

            public string LutName;

            public float LutContribution;

            public float tint;

            public float contrast;

            public float temperatureShift;

            public float hue;

            public float saturation;

            public float value;

            public float vibrance;

            public float gain;

            public float gamma;
            public bool useDithering;
        }
        public class VignetteSetting : BaseSystem
        {
            public VignetteSetting(string elementName) : base(elementName)
            {
            }

            public override void Init()
            {
                this.enabled = false;
                this.mode = VignetteAndChromaticAberration.AberrationMode.Simple;
                this.intensity = 0.036f;
                this.chromaticAberration = 0.2f;
                this.axialAberration = 0.5f;
                this.blur = 0f;
                this.blurSpread = 0.75f;
                this.luminanceDependency = 0.25f;
                this.blurDistance = 2.5f;
            }

            public bool enabled;
            public VignetteAndChromaticAberration.AberrationMode mode;
            public float intensity;
            public float chromaticAberration;
            public float axialAberration;
            public float blur;
            public float blurSpread;
            public float luminanceDependency;
            public float blurDistance;
        }

        public static BasicSetting basicSettings;
        public static Preset studioPresets;
        public static Preset gamePresets;

        public static SMAASetting smaaSettings;
        public static BloomSetting bloomSettings;
        public static ToneMapSetting tonemappingSettings;
        public static LensAberrationSetting lensAberrationSettings;

        public static BloomAndFlaresSetting bloomAndFlaresSettings;
        public static CurveSetting curveSettings;
        public static AntialiasingSetting antialiasingSettings;
        public static VignetteSetting vignetteSettings;

        public static SSAOSetting ssaoSettings;
        public static SSRSetting ssrSettings;
        public static ShadowSetting shadowSettings;
        public static DepthOfFieldSetting depthOfFieldSettings;
        public static SunShaftsSetting sunShaftsSettings;
        public static NoiseAndGrainSetting noiseAndGrainSettings;
        public static SEGISetting segiSettings;
        public static AmplifyBloomSetting amplifyBloomSettings;

        private static Control _xmlGraphicsSetting;
        private static Control _xmlImageEffect;
        private static Control _xmlStyle;
        private static Control _xmlShadow;
        private static Control _xmlCommon;

        public static void Load()
        {
            basicSettings = new BasicSetting("BasicConfig");
            gamePresets = new Preset("GamePresets");
            studioPresets = new Preset("StudioPresets");
            _xmlGraphicsSetting = new Control("GraphicSetting", "Config.xml", "HoneySelect", new List<Data>
            {
                basicSettings,
                gamePresets,
                studioPresets
            });
            _xmlGraphicsSetting.Read();

            if ((CameraClearFlags)basicSettings.CharaMakerBackgroundType != CameraClearFlags.Skybox && (CameraClearFlags)basicSettings.CharaMakerBackgroundType != CameraClearFlags.Color)
                basicSettings.CharaMakerBackgroundType = (int)CameraClearFlags.Skybox;
            QualitySettings.antiAliasing = basicSettings.msaa;
            if (HSLRE.self.binary == Binary.Game &&
                (SceneManager.GetActiveScene().buildIndex == 21 || SceneManager.GetActiveScene().buildIndex == 22) &&
                !basicSettings.CharaMakerReform &&
                QualitySettings.antiAliasing == 0)
                QualitySettings.antiAliasing = 2;
            string imageEffectPreset;
            string shadowPreset;
            string stylePreset;
            string commonPreset;
            switch (Application.productName)
            {
                case "HoneySelect":
                case "Honey Select Unlimited":
                    imageEffectPreset = gamePresets.ImageEffectPreset;
                    shadowPreset = gamePresets.ShadowPreset;
                    stylePreset = gamePresets.StylePreset;
                    commonPreset = gamePresets.CommonPreset;
                    break;
                case "StudioNEO":
                    imageEffectPreset = studioPresets.ImageEffectPreset;
                    shadowPreset = studioPresets.ShadowPreset;
                    stylePreset = studioPresets.StylePreset;
                    commonPreset = studioPresets.CommonPreset;
                    break;
                default:
                    Console.WriteLine("Product Name is " + Application.productName);
                    return;
            }

            //LRE only settings
            {
                lensAberrationSettings = new LensAberrationSetting("LensAberration");
                bloomSettings = new BloomSetting("Bloom");
                tonemappingSettings = new ToneMapSetting("ToneMapping");
                smaaSettings = new SMAASetting("SMAA");
                _xmlImageEffect = new Control("GraphicSetting/ImageEffect", imageEffectPreset + ".xml", "Config", new List<Data>
                {
                    lensAberrationSettings,
                    bloomSettings,
                    tonemappingSettings,
                    smaaSettings
                });
                _xmlImageEffect.Read();

                if (HSLRE.self.tonemapping != null)
                {
                    TonemappingColorGrading.TonemappingSettings localTonemappingSettings = new TonemappingColorGrading.TonemappingSettings
                    {
                        enabled = true,
                        tonemapper = TonemappingColorGrading.Tonemapper.ACES,
                        exposure = tonemappingSettings.exposure,
                        curve = TonemappingColorGrading.CurvesSettings.defaultCurve,
                        neutralBlackIn = 0.02f,
                        neutralWhiteIn = 10f,
                        neutralBlackOut = 0f,
                        neutralWhiteOut = 10f,
                        neutralWhiteLevel = 5.3f,
                        neutralWhiteClip = 10f
                    };
                    switch (tonemappingSettings.tonemapper)
                    {
                        case 0:
                            localTonemappingSettings.enabled = false;
                            break;
                        case 1:
                            localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.ACES;
                            break;
                        case 2:
                            localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.Hable;
                            break;
                        case 3:
                            localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.HejlDawson;
                            break;
                        case 4:
                            localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.Photographic;
                            break;
                        case 5:
                            localTonemappingSettings.tonemapper = TonemappingColorGrading.Tonemapper.Reinhard;
                            break;
                    }
                    HSLRE.self.tonemapping.tonemapping = localTonemappingSettings;
                    TonemappingColorGrading.ColorGradingSettings localColorGradingSettings = TonemappingColorGrading.ColorGradingSettings.defaultSettings;
                    localColorGradingSettings.useDithering = tonemappingSettings.useDithering;
                    localColorGradingSettings.basics = new TonemappingColorGrading.BasicsSettings
                    {
                        temperatureShift = tonemappingSettings.temperatureShift,
                        tint = tonemappingSettings.tint,
                        contrast = tonemappingSettings.contrast,
                        hue = tonemappingSettings.hue,
                        saturation = tonemappingSettings.saturation,
                        value = tonemappingSettings.value,
                        vibrance = tonemappingSettings.vibrance,
                        gain = tonemappingSettings.gain,
                        gamma = tonemappingSettings.gamma,
                    };
                    HSLRE.self.tonemapping.colorGrading = localColorGradingSettings;

                    Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false, true);
                    string path = Path.Combine(Application.dataPath, "../UserData/LUT/" + tonemappingSettings.LutName + ".png");
                    if (File.Exists(path))
                    {
                        byte[] data = File.ReadAllBytes(path);
                        texture2D.LoadImage(data);
                        texture2D.filterMode = FilterMode.Trilinear;
                        texture2D.anisoLevel = 0;
                        texture2D.wrapMode = TextureWrapMode.Repeat;
                    }
                    else
                    {
                        tonemappingSettings.Lut = false;
                    }
                    HSLRE.self.tonemapping.lut = new TonemappingColorGrading.LUTSettings
                    {
                        enabled = tonemappingSettings.Lut,
                        texture = texture2D,
                        contribution = tonemappingSettings.LutContribution
                    };
                }

                if (HSLRE.self.cinematicBloom != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.cinematicBloom].enabled = bloomSettings.enabled;

                    HSLRE.self.cinematicBloom.settings = new Bloom.Settings()
                    {
                        antiFlicker = bloomSettings.antiFlicker,
                        highQuality = true,
                        intensity = bloomSettings.intensity,
                        radius = bloomSettings.raduis,
                        softKnee = bloomSettings.softKnee,
                        threshold = bloomSettings.threshold
                    };

                }

                if (HSLRE.self.lensAberrations != null)
                {
                    HSLRE.self.lensAberrations.distortion = new LensAberrations.DistortionSettings
                    {
                        enabled = lensAberrationSettings.DistortionEnabled,
                        amount = lensAberrationSettings.DistortionAmount,
                        centerX = 0f,
                        centerY = 0f,
                        amountX = lensAberrationSettings.DistortionAmountX,
                        amountY = lensAberrationSettings.DistortionAmountY,
                        scale = lensAberrationSettings.DistortionScale
                    };
                    HSLRE.self.lensAberrations.vignette = new LensAberrations.VignetteSettings
                    {
                        enabled = lensAberrationSettings.VignetteEnabled,
                        color = lensAberrationSettings.VignetteColor,
                        center = new Vector2(0.5f, 0.5f),
                        intensity = lensAberrationSettings.VignetteIntensity,
                        smoothness = lensAberrationSettings.VignetteSmoothness,
                        roundness = lensAberrationSettings.VignetteRoundness,
                        blur = lensAberrationSettings.VignetteBlur,
                        desaturate = lensAberrationSettings.VignetteDesaturate
                    };
                    HSLRE.self.lensAberrations.chromaticAberration = new LensAberrations.ChromaticAberrationSettings
                    {
                        enabled = lensAberrationSettings.ChromaticAberrationEnabled,
                        color = lensAberrationSettings.ChromaticColor,
                        amount = lensAberrationSettings.ChromaticAberrationAmount
                    };
                }

                if (HSLRE.self.smaa != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.smaa].enabled = smaaSettings.enabled;
                    HSLRE.self.smaa.settings = new SMAA.GlobalSettings()
                    {
                        debugPass = SMAA.DebugPass.Off,
                        edgeDetectionMethod = (SMAA.EdgeDetectionMethod)smaaSettings.edgeDetectionMethod,
                        quality = (SMAA.QualityPreset)smaaSettings.quality
                    };
                    HSLRE.self.smaa.predication = new SMAA.PredicationSettings()
                    {
                        enabled = smaaSettings.predication,
                        scale = smaaSettings.predicationScale,
                        strength = smaaSettings.predicationStrength,
                        threshold = smaaSettings.threshold
                    };
                    HSLRE.self.smaa.temporal = new SMAA.TemporalSettings()
                    {
                        enabled = smaaSettings.temporal,
                        fuzzSize = smaaSettings.temporalFuzzSize
                    };
                    HSLRE.self.smaa.quality = new SMAA.QualitySettings()
                    {
                        cornerDetection = smaaSettings.cornerDetection,
                        cornerRounding = smaaSettings.cornerRounding,
                        depthThreshold = smaaSettings.depthThreshold,
                        diagonalDetection = smaaSettings.diagonalDetection,
                        localContrastAdaptationFactor = smaaSettings.localContrastAdaptationFactor,
                        maxDiagonalSearchSteps = smaaSettings.maxDiagonalSearchSteps,
                        maxSearchSteps = smaaSettings.maxSearchSteps,
                        threshold = smaaSettings.threshold
                    };
                }

            }
            
            //4K only settings
            {
                bloomAndFlaresSettings = new BloomAndFlaresSetting("Bloom");
                curveSettings = new CurveSetting("ColorCorrectionCurve");
                antialiasingSettings = new AntialiasingSetting("Antialiasing");
                vignetteSettings = new VignetteSetting("VignetteAndChromaticAberration");
                _xmlStyle = new Control("GraphicSetting/Style", stylePreset + ".xml", "Config", new List<Data>
                {
                    bloomAndFlaresSettings,
                    curveSettings,
                    antialiasingSettings,
                    vignetteSettings
                });
                _xmlStyle.Read();

                if (HSLRE.self.bloomAndFlares != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.bloomAndFlares].enabled = bloomAndFlaresSettings.BloomEnabled;
                    HSLRE.self.bloomAndFlares.bloomIntensity = bloomAndFlaresSettings.Bloomintensity;
                    if (Studio.Studio.Instance != null)
                        Studio.Studio.Instance.sceneInfo.bloomIntensity = HSLRE.self.bloomAndFlares.bloomIntensity;
                    HSLRE.self.bloomAndFlares.sepBlurSpread = bloomAndFlaresSettings.BloomBlurSpread;
                    if (Studio.Studio.Instance != null)
                        Studio.Studio.Instance.sceneInfo.bloomBlur = HSLRE.self.bloomAndFlares.sepBlurSpread;
                    HSLRE.self.bloomAndFlares.bloomThreshold = bloomAndFlaresSettings.BloomThreshold;

                    switch (bloomAndFlaresSettings.BloomScreenBlendMode)
                    {
                        case "Screen":
                            HSLRE.self.bloomAndFlares.screenBlendMode = BloomScreenBlendMode.Screen;
                            break;
                        case "Add":
                            HSLRE.self.bloomAndFlares.screenBlendMode = BloomScreenBlendMode.Add;
                            break;
                        default:
                            HSLRE.self.bloomAndFlares.screenBlendMode = BloomScreenBlendMode.Screen;
                            bloomAndFlaresSettings.BloomScreenBlendMode = "Screen";
                            break;
                    }
                    switch (bloomAndFlaresSettings.BloomHdr)
                    {
                        case "Auto":
                            HSLRE.self.bloomAndFlares.hdr = HDRBloomMode.Auto;
                            break;
                        case "On":
                            HSLRE.self.bloomAndFlares.hdr = HDRBloomMode.On;
                            break;
                        case "Off":
                            HSLRE.self.bloomAndFlares.hdr = HDRBloomMode.Off;
                            break;
                        default:
                            HSLRE.self.bloomAndFlares.hdr = HDRBloomMode.Auto;
                            bloomAndFlaresSettings.BloomHdr = "Auto";
                            break;
                    }

                    HSLRE.self.bloomAndFlares.bloomBlurIterations = bloomAndFlaresSettings.BloomBlurIterations;
                    HSLRE.self.bloomAndFlares.useSrcAlphaAsMask = bloomAndFlaresSettings.BloomUseSrcAlphaAsMask;

                    HSLRE.self.bloomAndFlares.lensflares = bloomAndFlaresSettings.BloomLensflares;
                    switch (bloomAndFlaresSettings.BloomLensflareMode)
                    {
                        case "Ghosting":
                            HSLRE.self.bloomAndFlares.lensflareMode = LensflareStyle34.Ghosting;
                            break;
                        case "Anamorphic":
                            HSLRE.self.bloomAndFlares.lensflareMode = LensflareStyle34.Anamorphic;
                            break;
                        case "Combined":
                            HSLRE.self.bloomAndFlares.lensflareMode = LensflareStyle34.Combined;
                            break;
                        default:
                            HSLRE.self.bloomAndFlares.lensflareMode = LensflareStyle34.Anamorphic;
                            bloomAndFlaresSettings.BloomLensflareMode = "Anamorphic";
                            break;
                    }
                    HSLRE.self.bloomAndFlares.lensflareIntensity = bloomAndFlaresSettings.BloomLensflareIntensity;
                    HSLRE.self.bloomAndFlares.lensflareThreshold = bloomAndFlaresSettings.BloomLensflareThreshold;
                    HSLRE.self.bloomAndFlares.hollyStretchWidth = bloomAndFlaresSettings.BloomLensFlareStretchWidth;
                    HSLRE.self.bloomAndFlares.hollywoodFlareBlurIterations = bloomAndFlaresSettings.BloomLensFlareBlurIterations;
                    HSLRE.self.bloomAndFlares.flareColorA = bloomAndFlaresSettings.BloomFlareColorA;
                    HSLRE.self.bloomAndFlares.flareColorB = bloomAndFlaresSettings.BloomFlareColorB;
                    HSLRE.self.bloomAndFlares.flareColorC = bloomAndFlaresSettings.BloomFlareColorC;
                    HSLRE.self.bloomAndFlares.flareColorD = bloomAndFlaresSettings.BloomFlareColorD;
                }

                if (HSLRE.self.ccc != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.ccc].enabled = curveSettings.Enabled;
                    HSLRE.self.ccc.saturation = curveSettings.CurveSaturation;
                    HSLRE.self.ccc.UpdateParameters();
                }

                if (HSLRE.self.antialiasing != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.antialiasing].enabled = antialiasingSettings.enabled;
                    HSLRE.self.antialiasing.mode = antialiasingSettings.mode;
                    HSLRE.self.antialiasing.showGeneratedNormals = antialiasingSettings.showGeneratedNormals;
                    HSLRE.self.antialiasing.offsetScale = antialiasingSettings.offsetScale;
                    HSLRE.self.antialiasing.blurRadius = antialiasingSettings.blurRadius;
                    HSLRE.self.antialiasing.edgeThresholdMin = antialiasingSettings.edgeThresholdMin;
                    HSLRE.self.antialiasing.edgeThreshold = antialiasingSettings.edgeThreshold;
                    HSLRE.self.antialiasing.edgeSharpness = antialiasingSettings.edgeSharpness;
                    HSLRE.self.antialiasing.dlaaSharp = antialiasingSettings.dlaaSharp;
                }

                if (HSLRE.self.vignette != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.vignette].enabled = vignetteSettings.enabled;
                    HSLRE.self.vignette.mode = vignetteSettings.mode;
                    HSLRE.self.vignette.intensity = vignetteSettings.intensity;
                    if (Studio.Studio.Instance != null)
                        Studio.Studio.Instance.sceneInfo.vignetteVignetting = HSLRE.self.vignette.intensity;
                    HSLRE.self.vignette.chromaticAberration = vignetteSettings.chromaticAberration;
                    HSLRE.self.vignette.axialAberration = vignetteSettings.axialAberration;
                    HSLRE.self.vignette.blur = vignetteSettings.blur;
                    HSLRE.self.vignette.blurSpread = vignetteSettings.blurSpread;
                    HSLRE.self.vignette.luminanceDependency = vignetteSettings.luminanceDependency;
                    HSLRE.self.vignette.blurDistance = vignetteSettings.blurDistance;
                }
            }

            //Common settings
            {
                ssaoSettings = new SSAOSetting("SSAO");
                ssrSettings = new SSRSetting("SSR");
                depthOfFieldSettings = new DepthOfFieldSetting("DepthOfField");
                sunShaftsSettings = new SunShaftsSetting("SunShafts");
                noiseAndGrainSettings = new NoiseAndGrainSetting("NoiseAndGrain");
                segiSettings = new SEGISetting("SEGI");
                amplifyBloomSettings = new AmplifyBloomSetting("AmplifyBloom");
                _xmlCommon = new Control("GraphicSetting/Common", commonPreset + ".xml", "Config", new List<Data>
                {
                    ssaoSettings,
                    ssrSettings,
                    depthOfFieldSettings,
                    sunShaftsSettings,
                    noiseAndGrainSettings,
                    segiSettings,
                    amplifyBloomSettings
                });
                _xmlCommon.Read();

                if (HSLRE.self.ssao != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.ssao].enabled = ssaoSettings.SSAOEnabled;
                    HSLRE.self.ssao.Bias = ssaoSettings.SSAOBias;
                    HSLRE.self.ssao.BlurBilateralThreshold = ssaoSettings.SSAOBlurBilateralThreshold;
                    HSLRE.self.ssao.BlurPasses = ssaoSettings.SSAOBlurPasses;
                    HSLRE.self.ssao.CutoffDistance = ssaoSettings.SSAOCutoffDistance;
                    HSLRE.self.ssao.CutoffFalloff = ssaoSettings.SSAOfalloffDistance;
                    HSLRE.self.ssao.LumContribution = ssaoSettings.SSAOLumContribution;
                    HSLRE.self.ssao.Distance = ssaoSettings.SSAODistance;
                    HSLRE.self.ssao.Radius = ssaoSettings.SSAORadius;
                    switch (ssaoSettings.SSAOSamples)
                    {
                        case "VeryLow":
                            HSLRE.self.ssao.Samples = SSAOPro.SampleCount.VeryLow;
                            break;
                        case "Low":
                            HSLRE.self.ssao.Samples = SSAOPro.SampleCount.Low;
                            break;
                        case "Medium":
                            HSLRE.self.ssao.Samples = SSAOPro.SampleCount.Medium;
                            break;
                        case "High":
                            HSLRE.self.ssao.Samples = SSAOPro.SampleCount.High;
                            break;
                        case "Ultra":
                            HSLRE.self.ssao.Samples = SSAOPro.SampleCount.Ultra;
                            break;
                        default:
                            HSLRE.self.ssao.Samples = SSAOPro.SampleCount.Ultra;
                            ssaoSettings.SSAOSamples = "Ultra";
                            break;
                    }
                    HSLRE.self.ssao.Intensity = ssaoSettings.SSAOIntensity;
                    if (Studio.Studio.Instance != null)
                        Studio.Studio.Instance.sceneInfo.ssaoIntensity = HSLRE.self.ssao.Intensity;
                    HSLRE.self.ssao.UseHighPrecisionDepthMap = ssaoSettings.SSAOUseHighPrecisionDepthMap;
                    HSLRE.self.ssao.Downsampling = ssaoSettings.SSAODownsampling;
                    switch (ssaoSettings.SSAOBlur)
                    {
                        case "None":
                            HSLRE.self.ssao.Blur = SSAOPro.BlurMode.None;
                            break;
                        case "Gaussian":
                            HSLRE.self.ssao.Blur = SSAOPro.BlurMode.Gaussian;
                            break;
                        case "Bilateral":
                            HSLRE.self.ssao.Blur = SSAOPro.BlurMode.Bilateral;
                            break;
                        case "HighQualityBilateral":
                            HSLRE.self.ssao.Blur = SSAOPro.BlurMode.HighQualityBilateral;
                            break;
                        default:
                            HSLRE.self.ssao.Blur = SSAOPro.BlurMode.Bilateral;
                            ssaoSettings.SSAOBlur = "Bilateral";
                            break;
                    }
                    HSLRE.self.ssao.BlurDownsampling = ssaoSettings.SSAOBlurDownsampling;
                    HSLRE.self.ssao.OcclusionColor = ssaoSettings.SSAOOcclusionColor;
                    if (Studio.Studio.Instance != null)
                        Studio.Studio.Instance.sceneInfo.ssaoColor.SetDiffuseRGBA(HSLRE.self.ssao.OcclusionColor);
                }

                if (HSLRE.self.ssr != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.ssr].enabled = true;
                    HSLRE.self.effectsDictionary[HSLRE.self.ssr].enabled = ssrSettings.SSRenabled;
                    HSLRE.self.ssr.settings = new ScreenSpaceReflection.SSRSettings
                    {
                        basicSettings = new ScreenSpaceReflection.BasicSettings
                        {
                            screenEdgeFading = ssrSettings.SSRscreenEdgeFading,
                            maxDistance = ssrSettings.SSRmaxDistance,
                            fadeDistance = ssrSettings.SSRfadeDistance,
                            reflectionMultiplier = ssrSettings.SSRreflectionMultiplier,
                            enableHDR = ssrSettings.SSRenableHDR,
                            additiveReflection = ssrSettings.SSRadditiveReflection
                        },
                        reflectionSettings = new ScreenSpaceReflection.ReflectionSettings
                        {
                            maxSteps = ssrSettings.SSRmaxSteps,
                            rayStepSize = ssrSettings.SSRrayStepSize,
                            widthModifier = ssrSettings.SSRwidthModifier,
                            smoothFallbackThreshold = ssrSettings.SSRsmoothFallbackThreshold,
                            distanceBlur = ssrSettings.SSRdistanceBlur,
                            fresnelFade = ssrSettings.SSRfresnelFade,
                            fresnelFadePower = ssrSettings.SSRfresnelFadePower,
                            smoothFallbackDistance = ssrSettings.SSRsmoothFallbackDistance
                        },
                        advancedSettings = new ScreenSpaceReflection.AdvancedSettings
                        {
                            useTemporalConfidence = ssrSettings.SSRuseTemporalConfidence,
                            temporalFilterStrength = ssrSettings.SSRtemporalFilterStrength,
                            treatBackfaceHitAsMiss = ssrSettings.SSRtreatBackfaceHitAsMiss,
                            allowBackwardsRays = ssrSettings.SSRallowBackwardsRays,
                            traceBehindObjects = ssrSettings.SSRtraceBehindObjects,
                            highQualitySharpReflections = ssrSettings.SSRhighQualitySharpReflections,
                            traceEverywhere = ssrSettings.SSRtraceEverywhere,
                            resolution = (ScreenSpaceReflection.SSRResolution)ssrSettings.SSRresolution,
                            bilateralUpsample = ssrSettings.SSRbilateralUpsample,
                            improveCorners = ssrSettings.SSRimproveCorners,
                            reduceBanding = ssrSettings.SSRreduceBanding,
                            highlightSuppression = ssrSettings.SSRhighlightSuppression
                        },
                        debugSettings = new ScreenSpaceReflection.DebugSettings
                        {
                            debugMode = (ScreenSpaceReflection.SSRDebugMode)ssrSettings.SSRdebugMode
                        }
                    };
                }

                if (HSLRE.self.dof != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.dof].enabled = depthOfFieldSettings.enabled;
                    HSLRE.self.dof.focalLength = depthOfFieldSettings.focalLength;
                    HSLRE.self.dof.focalSize = depthOfFieldSettings.focalSize;
                    if (Studio.Studio.Instance != null)
                        Studio.Studio.Instance.sceneInfo.depthFocalSize = HSLRE.self.dof.focalSize;
                    HSLRE.self.dof.aperture = depthOfFieldSettings.aperture;
                    if (Studio.Studio.Instance != null)
                        Studio.Studio.Instance.sceneInfo.depthAperture = HSLRE.self.dof.aperture;
                    HSLRE.self.dof.maxBlurSize = depthOfFieldSettings.maxBlurSize;
                    HSLRE.self.dof.highResolution = depthOfFieldSettings.highResolution;
                    switch (depthOfFieldSettings.blurType)
                    {
                        case "DiscBlur":
                            HSLRE.self.dof.blurType = DepthOfField.BlurType.DiscBlur;
                            break;
                        case "DX11":
                            HSLRE.self.dof.blurType = DepthOfField.BlurType.DX11;
                            break;
                        default:
                            HSLRE.self.dof.blurType = DepthOfField.BlurType.DiscBlur;
                            depthOfFieldSettings.blurType = "DiscBlur";
                            break;
                    }
                    switch (depthOfFieldSettings.blurSampleCount)
                    {
                        case "Low":
                            HSLRE.self.dof.blurSampleCount = DepthOfField.BlurSampleCount.Low;
                            break;
                        case "Medium":
                            HSLRE.self.dof.blurSampleCount = DepthOfField.BlurSampleCount.Medium;
                            break;
                        case "High":
                            HSLRE.self.dof.blurSampleCount = DepthOfField.BlurSampleCount.High;
                            break;
                        default:
                            HSLRE.self.dof.blurSampleCount = DepthOfField.BlurSampleCount.High;
                            depthOfFieldSettings.blurSampleCount = "High";
                            break;
                    }
                    HSLRE.self.dof.nearBlur = depthOfFieldSettings.nearBlur;
                    HSLRE.self.dof.foregroundOverlap = depthOfFieldSettings.foregroundOverlap;
                    HSLRE.self.dof.dx11BokehThreshold = depthOfFieldSettings.dx11BokehThreshold;
                    HSLRE.self.dof.dx11SpawnHeuristic = depthOfFieldSettings.dx11SpawnHeuristic;
                    HSLRE.self.dof.dx11BokehScale = depthOfFieldSettings.dx11BokehScale;
                    HSLRE.self.dof.dx11BokehIntensity = depthOfFieldSettings.dx11BokehIntensity;
                    HSLRE.self.fixDofForUpscaledScreenshots = depthOfFieldSettings.fixUpscaledScreenshots;
                }

                if (HSLRE.self.sunShafts != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.sunShafts].enabled = sunShaftsSettings.enabled;
                    switch (sunShaftsSettings.resolution)
                    {
                        case "Low":
                            HSLRE.self.sunShafts.resolution = SunShafts.SunShaftsResolution.Low;
                            break;
                        case "Normal":
                            HSLRE.self.sunShafts.resolution = SunShafts.SunShaftsResolution.Normal;
                            break;
                        case "High":
                            HSLRE.self.sunShafts.resolution = SunShafts.SunShaftsResolution.High;
                            break;
                        default:
                            HSLRE.self.sunShafts.resolution = SunShafts.SunShaftsResolution.Normal;
                            sunShaftsSettings.resolution = "Normal";
                            break;
                    }
                    switch (sunShaftsSettings.screenBlendMode)
                    {
                        case "Screen":
                            HSLRE.self.sunShafts.screenBlendMode = SunShafts.ShaftsScreenBlendMode.Screen;
                            break;
                        case "Add":
                            HSLRE.self.sunShafts.screenBlendMode = SunShafts.ShaftsScreenBlendMode.Add;
                            break;
                        default:
                            HSLRE.self.sunShafts.screenBlendMode = SunShafts.ShaftsScreenBlendMode.Screen;
                            sunShaftsSettings.screenBlendMode = "Screen";
                            break;
                    }
                    HSLRE.self.sunShafts.radialBlurIterations = sunShaftsSettings.radialBlurIterations;
                    HSLRE.self.sunShafts.sunColor = sunShaftsSettings.sunColor;
                    HSLRE.self.sunShafts.sunThreshold = sunShaftsSettings.sunThreshold;
                    HSLRE.self.sunShafts.sunShaftBlurRadius = sunShaftsSettings.sunShaftBlurRadius;
                    HSLRE.self.sunShafts.sunShaftIntensity = sunShaftsSettings.sunShaftIntensity;
                    HSLRE.self.sunShafts.maxRadius = sunShaftsSettings.sunShaftBlurRadius;
                    HSLRE.self.sunShafts.useDepthTexture = sunShaftsSettings.useDepthTexture;
                }

                if (HSLRE.self.noiseAndGrain != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.noiseAndGrain].enabled = noiseAndGrainSettings.enabled;
                    HSLRE.self.noiseAndGrain.dx11Grain = noiseAndGrainSettings.dx11Grain;
                    HSLRE.self.noiseAndGrain.monochrome = noiseAndGrainSettings.monochrome;
                    HSLRE.self.noiseAndGrain.intensityMultiplier = noiseAndGrainSettings.intensityMultiplier;
                    HSLRE.self.noiseAndGrain.generalIntensity = noiseAndGrainSettings.generalIntensity;
                    HSLRE.self.noiseAndGrain.blackIntensity = noiseAndGrainSettings.blackIntensity;
                    HSLRE.self.noiseAndGrain.whiteIntensity = noiseAndGrainSettings.whiteIntensity;
                    HSLRE.self.noiseAndGrain.midGrey = noiseAndGrainSettings.midGrey;
                    HSLRE.self.noiseAndGrain.intensities = noiseAndGrainSettings.intensities;
                    switch (noiseAndGrainSettings.filterMode)
                    {
                        case "Point":
                            HSLRE.self.noiseAndGrain.filterMode = FilterMode.Point;
                            break;
                        case "Bilinear":
                            HSLRE.self.noiseAndGrain.filterMode = FilterMode.Bilinear;
                            break;
                        case "Trilinear":
                            HSLRE.self.noiseAndGrain.filterMode = FilterMode.Trilinear;
                            break;
                        default:
                            HSLRE.self.noiseAndGrain.filterMode = FilterMode.Bilinear;
                            noiseAndGrainSettings.filterMode = "Bilinear";
                            break;
                    }
                    HSLRE.self.noiseAndGrain.softness = noiseAndGrainSettings.softness;
                    HSLRE.self.noiseAndGrain.monochromeTiling = noiseAndGrainSettings.monochromeTiling;
                    HSLRE.self.noiseAndGrain.tiling = noiseAndGrainSettings.tiling;
                    HSLRE.self.fixNoiseAndGrainForUpscaledScreenshots = noiseAndGrainSettings.fixUpscaledScreenshots;
                }

                if (HSLRE.self.segi != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.segi].enabled = segiSettings.enabled;
                    switch (segiSettings.voxelResolution)
                    {
                        case "low":
                            HSLRE.self.segi.voxelResolution = SEGI.VoxelResolution.low;
                            break;
                        case "high":
                            HSLRE.self.segi.voxelResolution = SEGI.VoxelResolution.high;
                            break;
                        default:
                            HSLRE.self.segi.voxelResolution = SEGI.VoxelResolution.high;
                            segiSettings.voxelResolution = "high";
                            break;
                    }
                    HSLRE.self.segi.voxelAA = segiSettings.voxelAA;
                    HSLRE.self.segi.innerOcclusionLayers = segiSettings.innerOcclusionLayers;
                    HSLRE.self.segi.gaussianMipFilter = segiSettings.gaussianMipFilter;
                    HSLRE.self.segi.voxelSpaceSize = segiSettings.voxelSpaceSize;
                    HSLRE.self.segi.shadowSpaceSize = segiSettings.shadowSpaceSize;
                    HSLRE.self.segi.updateGI = segiSettings.updateGI;
                    HSLRE.self.segi.infiniteBounces = segiSettings.infiniteBounces;
                    HSLRE.self.segi.hsStandardCustomShadersCompatibility = segiSettings.hsStandardCustomShadersCompatibility;

                    HSLRE.self.segi.softSunlight = segiSettings.softSunlight;
                    HSLRE.self.segi.skyColor = segiSettings.skyColor;
                    HSLRE.self.segi.skyIntensity = segiSettings.skyIntensity;
                    HSLRE.self.segi.sphericalSkylight = segiSettings.sphericalSkylight;

                    HSLRE.self.segi.temporalBlendWeight = segiSettings.temporalBlendWeight;
                    HSLRE.self.segi.useBilateralFiltering = segiSettings.useBilateralFiltering;
                    HSLRE.self.segi.halfResolution = segiSettings.halfResolution;
                    HSLRE.self.segi.stochasticSampling = segiSettings.stochasticSampling;

                    HSLRE.self.segi.cones = segiSettings.cones;
                    HSLRE.self.segi.coneTraceSteps = segiSettings.coneTraceSteps;
                    HSLRE.self.segi.coneLength = segiSettings.coneLength;
                    HSLRE.self.segi.coneWidth = segiSettings.coneWidth;
                    HSLRE.self.segi.coneTraceBias = segiSettings.coneTraceBias;
                    HSLRE.self.segi.occlusionStrength = segiSettings.farOcclusionStrength;
                    HSLRE.self.segi.nearOcclusionStrength = segiSettings.nearOcclusionStrength;
                    HSLRE.self.segi.farOcclusionStrength = segiSettings.farOcclusionStrength;
                    HSLRE.self.segi.farthestOcclusionStrength = segiSettings.farthestOcclusionStrength;
                    HSLRE.self.segi.occlusionPower = segiSettings.occlusionPower;
                    HSLRE.self.segi.nearLightGain = segiSettings.nearLightGain;
                    HSLRE.self.segi.giGain = segiSettings.giGain;

                    HSLRE.self.segi.secondaryBounceGain = segiSettings.secondaryBounceGain;
                    HSLRE.self.segi.secondaryCones = segiSettings.secondaryCones;
                    HSLRE.self.segi.secondaryOcclusionStrength = segiSettings.secondaryOcclusionStrength;

                    HSLRE.self.segi.doReflections = segiSettings.doReflections;
                    HSLRE.self.segi.reflectionSteps = segiSettings.reflectionSteps;
                    HSLRE.self.segi.reflectionOcclusionPower = segiSettings.reflectionOcclusionPower;
                    HSLRE.self.segi.skyReflectionIntensity = segiSettings.skyReflectionIntensity;
                }

                if (HSLRE.self.amplifyBloom != null)
                {
                    HSLRE.self.effectsDictionary[HSLRE.self.amplifyBloom].enabled = amplifyBloomSettings.enabled;
                    if (EnumTryParse(amplifyBloomSettings.UpscaleQuality, out UpscaleQualityEnum newUpscaleQualityEnum))
                        HSLRE.self.amplifyBloom.UpscaleQuality = newUpscaleQualityEnum;
                    if (EnumTryParse(amplifyBloomSettings.MainThresholdSize, out MainThresholdSizeEnum newMainThresholdSizeEnum))
                        HSLRE.self.amplifyBloom.MainThresholdSize = newMainThresholdSizeEnum;
                    if (EnumTryParse(amplifyBloomSettings.CurrentPrecisionMode, out PrecisionModes newPrecisionModes))
                        HSLRE.self.amplifyBloom.CurrentPrecisionMode = newPrecisionModes;
                    HSLRE.self.amplifyBloom.BloomRange = amplifyBloomSettings.BloomRange;
                    HSLRE.self.amplifyBloom.OverallIntensity = amplifyBloomSettings.OverallIntensity;
                    HSLRE.self.amplifyBloom.OverallThreshold = amplifyBloomSettings.OverallThreshold;
                    HSLRE.self.amplifyBloom.UpscaleBlurRadius = amplifyBloomSettings.UpscaleBlurRadius;
                    HSLRE.self.amplifyBloom.TemporalFilteringActive = amplifyBloomSettings.TemporalFilteringActive;
                    HSLRE.self.amplifyBloom.TemporalFilteringValue = amplifyBloomSettings.TemporalFilteringValue;
                    HSLRE.self.amplifyBloom.SeparateFeaturesThreshold = amplifyBloomSettings.SeparateFeaturesThreshold;
                    HSLRE.self.amplifyBloom.FeaturesThreshold = amplifyBloomSettings.FeaturesThreshold;
                    HSLRE.self.amplifyBloom.ApplyLensDirt = amplifyBloomSettings.ApplyLensDirt;
                    HSLRE.self.amplifyBloom.LensDirtStrength = amplifyBloomSettings.LensDirtStrength;
                    if (EnumTryParse(amplifyBloomSettings.LensDirtTexture, out LensDirtTextureEnum newLensDirtTextureEnum))
                        HSLRE.self.amplifyBloom.LensDirtTexture = newLensDirtTextureEnum;
                    HSLRE.self.amplifyBloom.ApplyLensStardurst = amplifyBloomSettings.ApplyLensStardurst;
                    HSLRE.self.amplifyBloom.LensStarburstStrength = amplifyBloomSettings.LensStarburstStrength;
                    if (EnumTryParse(amplifyBloomSettings.LensStardurstTex, out LensStarburstTextureEnum newLensStarburstTextureEnum))
                        HSLRE.self.amplifyBloom.LensStardurstTex = newLensStarburstTextureEnum;
                    HSLRE.self.amplifyBloom.BokehFilterInstance.ApplyBokeh = amplifyBloomSettings.BokehFilterInstanceApplyBokeh;
                    HSLRE.self.amplifyBloom.BokehFilterInstance.ApplyOnBloomSource = amplifyBloomSettings.BokehFilterInstanceApplyOnBloomSource;
                    if (EnumTryParse(amplifyBloomSettings.BokehFilterInstanceApertureShape, out ApertureShape newApertureShape))
                        HSLRE.self.amplifyBloom.BokehFilterInstance.ApertureShape = newApertureShape;
                    HSLRE.self.amplifyBloom.BokehFilterInstance.OffsetRotation = amplifyBloomSettings.BokehFilterInstanceOffsetRotation;
                    HSLRE.self.amplifyBloom.BokehFilterInstance.BokehSampleRadius = amplifyBloomSettings.BokehFilterInstanceBokehSampleRadius;
                    HSLRE.self.amplifyBloom.BokehFilterInstance.Aperture = amplifyBloomSettings.BokehFilterInstanceAperture;
                    HSLRE.self.amplifyBloom.BokehFilterInstance.FocalLength = amplifyBloomSettings.BokehFilterInstanceFocalLength;
                    HSLRE.self.amplifyBloom.BokehFilterInstance.FocalDistance = amplifyBloomSettings.BokehFilterInstanceFocalDistance;
                    HSLRE.self.amplifyBloom.BokehFilterInstance.MaxCoCDiameter = amplifyBloomSettings.BokehFilterInstanceMaxCoCDiameter;
                    HSLRE.self.amplifyBloom.LensFlareInstance.ApplyLensFlare = amplifyBloomSettings.LensFlareInstanceApplyLensFlare;
                    HSLRE.self.amplifyBloom.LensFlareInstance.OverallIntensity = amplifyBloomSettings.LensFlareInstanceOverallIntensity;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount = amplifyBloomSettings.LensFlareInstanceLensFlareGaussianBlurAmount;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity = amplifyBloomSettings.LensFlareInstanceLensFlareNormalizedGhostsIntensity;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostAmount = amplifyBloomSettings.LensFlareInstanceLensFlareGhostAmount;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal = amplifyBloomSettings.LensFlareInstanceLensFlareGhostsDispersal;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion = amplifyBloomSettings.LensFlareInstanceLensFlareGhostChrDistortion;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor = amplifyBloomSettings.LensFlareInstanceLensFlareGhostsPowerFactor;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff = amplifyBloomSettings.LensFlareInstanceLensFlareGhostsPowerFalloff;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity = amplifyBloomSettings.LensFlareInstanceLensFlareNormalizedHaloIntensity;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloWidth = amplifyBloomSettings.LensFlareInstanceLensFlareHaloWidth;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion = amplifyBloomSettings.LensFlareInstanceLensFlareHaloChrDistortion;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor = amplifyBloomSettings.LensFlareInstanceLensFlareHaloPowerFactor;
                    HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff = amplifyBloomSettings.LensFlareInstanceLensFlareHaloPowerFalloff;
                    HSLRE.self.amplifyBloom.LensGlareInstance.ApplyLensGlare = amplifyBloomSettings.LensGlareInstanceApplyLensGlare;
                    HSLRE.self.amplifyBloom.LensGlareInstance.Intensity = amplifyBloomSettings.LensGlareInstanceIntensity;
                    HSLRE.self.amplifyBloom.LensGlareInstance.OverallStreakScale = amplifyBloomSettings.LensGlareInstanceOverallStreakScale;
                    HSLRE.self.amplifyBloom.LensGlareInstance.OverallTint = amplifyBloomSettings.LensGlareInstanceOverallTint;
                    if (EnumTryParse(amplifyBloomSettings.LensGlareInstanceCurrentGlare, out GlareLibType newGlareLibType))
                        HSLRE.self.amplifyBloom.LensGlareInstance.CurrentGlare = newGlareLibType;
                    HSLRE.self.amplifyBloom.LensGlareInstance.PerPassDisplacement = amplifyBloomSettings.LensGlareInstancePerPassDisplacement;
                    HSLRE.self.amplifyBloom.LensGlareInstance.GlareMaxPassCount = amplifyBloomSettings.LensGlareInstanceGlareMaxPassCount;
                    HSLRE.self.fixAmplifyBloomForUpscaledScreenshots = amplifyBloomSettings.fixUpscaledScreenshots;
                }
            }

            {
                shadowSettings = new ShadowSetting("Shadow");
                _xmlShadow = new Control("GraphicSetting/Shadow", shadowPreset + ".xml", "Config", new List<Data>
                {
                    shadowSettings
                });
                _xmlShadow.Read();
                if (shadowSettings.ShadowProjection == 0)
                    QualitySettings.shadowProjection = ShadowProjection.CloseFit;
                else if (shadowSettings.ShadowProjection == 1)
                    QualitySettings.shadowProjection = ShadowProjection.StableFit;
                QualitySettings.shadowNearPlaneOffset = shadowSettings.ShadowNearPlaneOffset;
                QualitySettings.shadowDistance = shadowSettings.ShadowDistance;
                QualitySettings.shadowCascades = shadowSettings.ShadowCascades;
                QualitySettings.shadowCascade2Split = shadowSettings.ShadowCascade2Split;
                QualitySettings.shadowCascade4Split = new Vector3(shadowSettings.ShadowCascade4Split_x, shadowSettings.ShadowCascade4Split_y, shadowSettings.ShadowCascade4Split_z);
            }
        }

        public static void Save()
        {
            if (_xmlGraphicsSetting != null)
                _xmlGraphicsSetting.Write();
            if (_xmlImageEffect != null)
                _xmlImageEffect.Write();
            if (_xmlShadow != null)
                _xmlShadow.Write();
            if (_xmlStyle != null)
                _xmlStyle.Write();
            if (_xmlCommon != null)
                _xmlCommon.Write();
        }

        private static bool EnumTryParse<T>(string value, out T result)
        {
            try
            {
                result = (T)Enum.Parse(typeof(T), value);
                return true;
            }
            catch (Exception)
            {
                result = default(T);
                return false;
            }
        }
    }
}
