#pragma warning disable 0618

using UnityEngine;
using System;
using UnityEngine.Rendering;

namespace HSLRE.CustomEffects
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/Sonic Ether/SEGI")]
    public class SEGI
    {
        object initChecker;

        Material material;
        Camera attachedCamera;
        Transform shadowCamTransform;

        Camera shadowCam;
        GameObject shadowCamGameObject;

        [Serializable]
        public enum VoxelResolution
        {
            low = 128,
            high = 256
        }

        public VoxelResolution voxelResolution = VoxelResolution.high;

        public bool visualizeSunDepthTexture = false;
        public bool visualizeGI = false;

        public Light sun;
        public LayerMask giCullingMask = 2147483647;

        public float shadowSpaceSize = 50.0f;

        [Range(0.01f, 1.0f)]
        public float temporalBlendWeight = 0.1f;

        public bool visualizeVoxels = false;
        public int visualizeGBuffers = 0;

        public bool updateGI = true;


        public Color skyColor;

        public float voxelSpaceSize = 50.0f;

        public bool useBilateralFiltering = false;

        [Range(0, 2)]
        public int innerOcclusionLayers = 1;


        public bool halfResolution = true;
        public bool stochasticSampling = true;
        public bool infiniteBounces = false;
        public Transform followTransform;
        [Range(1, 128)]
        public int cones = 6;
        [Range(1, 32)]
        public int coneTraceSteps = 14;
        [Range(0.1f, 2.0f)]
        public float coneLength = 1.0f;
        [Range(0.5f, 6.0f)]
        public float coneWidth = 5.5f;
        [Range(0.0f, 4.0f)]
        public float occlusionStrength = 1.0f;
        [Range(0.0f, 4.0f)]
        public float nearOcclusionStrength = 0.5f;
        [Range(0.001f, 4.0f)]
        public float occlusionPower = 1.5f;
        [Range(0.0f, 4.0f)]
        public float coneTraceBias = 1.0f;
        [Range(0.0f, 4.0f)]
        public float nearLightGain = 1.0f;
        [Range(0.0f, 4.0f)]
        public float giGain = 1.0f;
        [Range(0.0f, 4.0f)]
        public float secondaryBounceGain = 1.0f;
        [Range(0.0f, 16.0f)]
        public float softSunlight = 0.0f;

        [Range(0.0f, 8.0f)]
        public float skyIntensity = 1.0f;

        public bool doReflections = true;
        [Range(12, 128)]
        public int reflectionSteps = 64;
        [Range(0.001f, 4.0f)]
        public float reflectionOcclusionPower = 1.0f;
        [Range(0.0f, 1.0f)]
        public float skyReflectionIntensity = 1.0f;



        [Range(0.1f, 4.0f)]
        public float farOcclusionStrength = 1.0f;
        [Range(0.1f, 4.0f)]
        public float farthestOcclusionStrength = 1.0f;

        [Range(3, 16)]
        public int secondaryCones = 6;
        [Range(0.1f, 4.0f)]
        public float secondaryOcclusionStrength = 1.0f;

        public bool sphericalSkylight = false;

        public bool hsStandardCustomShadersCompatibility = false;

        struct Pass
        {
            public static int DiffuseTrace = 0;
            public static int BilateralBlur = 1;
            public static int BlendWithScene = 2;
            public static int TemporalBlend = 12;
            public static int SpecularTrace = 4;
            public static int GetCameraDepthTexture = 5;
            public static int GetWorldNormals = 6;
            public static int VisualizeGI = 7;
            public static int WriteBlack = 8;
            public static int VisualizeVoxels = 10;
            public static int BilateralUpsample = 11;
        }

        public struct SystemSupported
        {
            public bool hdrTextures;
            public bool rIntTextures;
            public bool dx11;
            public bool volumeTextures;
            public bool postShader;
            public bool sunDepthShader;
            public bool voxelizationShader;
            public bool tracingShader;

            public bool fullFunctionality { get { return this.hdrTextures && this.rIntTextures && this.dx11 && this.volumeTextures && this.postShader && this.sunDepthShader && this.voxelizationShader && this.tracingShader; } }
        }

        /// <summary>
        /// Contains info on system compatibility of required hardware functionality
        /// </summary>
        public SystemSupported systemSupported;

        /// <summary>
        /// Estimates the VRAM usage of all the render textures used to render GI.
        /// </summary>
        public float vramUsage
        {
            get
            {
                long v = 0;

                if (this.sunDepthTexture != null)
                    v += this.sunDepthTexture.width * this.sunDepthTexture.height * 16;

                if (this.previousResult != null)
                    v += this.previousResult.width * this.previousResult.height * 16 * 4;

                if (this.previousDepth != null)
                    v += this.previousDepth.width * this.previousDepth.height * 32;

                if (this.intTex1 != null)
                    v += this.intTex1.width * this.intTex1.height * this.intTex1.volumeDepth * 32;

                if (this.volumeTextures != null)
                {
                    for (int i = 0; i < this.volumeTextures.Length; i++)
                    {
                        if (this.volumeTextures[i] != null)
                            v += this.volumeTextures[i].width * this.volumeTextures[i].height * this.volumeTextures[i].volumeDepth * 16 * 4;
                    }
                }

                if (this.volumeTexture1 != null)
                    v += this.volumeTexture1.width * this.volumeTexture1.height * this.volumeTexture1.volumeDepth * 16 * 4;

                if (this.volumeTextureB != null)
                    v += this.volumeTextureB.width * this.volumeTextureB.height * this.volumeTextureB.volumeDepth * 16 * 4;

                if (this.dummyVoxelTexture != null)
                    v += this.dummyVoxelTexture.width * this.dummyVoxelTexture.height * 8;

                if (this.dummyVoxelTexture2 != null)
                    v += this.dummyVoxelTexture2.width * this.dummyVoxelTexture2.height * 8;

                if (this._segiHSStandardCustom != null && this.hsStandardCustomShadersCompatibility)
                {
                    if (this._segiHSStandardCustom.GBuffer0 != null)
                        v += this._segiHSStandardCustom.GBuffer0.width * this._segiHSStandardCustom.GBuffer0.height * 32;
                    if (this._segiHSStandardCustom.GBuffer1 != null)
                        v += this._segiHSStandardCustom.GBuffer1.width * this._segiHSStandardCustom.GBuffer1.height * 32;
                    if (this._segiHSStandardCustom.GBuffer2 != null)
                        v += this._segiHSStandardCustom.GBuffer2.width * this._segiHSStandardCustom.GBuffer2.height * 32;
                    if (this._segiHSStandardCustom.Depth != null)
                        v += this._segiHSStandardCustom.Depth.width * this._segiHSStandardCustom.Depth.height * 8;
                    if (this._gBuffer0 != null)
                        v += this._gBuffer0.width * this._gBuffer0.height * 32;
                    if (this._gBuffer1 != null)
                        v += this._gBuffer1.width * this._gBuffer1.height * 32;
                    if (this._gBuffer2 != null)
                        v += this._gBuffer2.width * this._gBuffer2.height * 32;
                }

                float vram = (v / 8388608.0f);

                return vram;
            }
        }





        public bool gaussianMipFilter = false;

        int mipFilterKernel { get { return this.gaussianMipFilter ? 1 : 0; } }

        public bool voxelAA = false;

        int dummyVoxelResolution { get { return (int)this.voxelResolution * (this.voxelAA ? 2 : 1); } }

        int sunShadowResolution = 256;
        int prevSunShadowResolution;





        Shader sunDepthShader;

        float shadowSpaceDepthRatio = 10.0f;

        int frameSwitch = 0;

        RenderTexture sunDepthTexture;
        RenderTexture previousResult;
        RenderTexture previousDepth;
        RenderTexture intTex1;
        RenderTexture[] volumeTextures;
        RenderTexture volumeTexture1;
        RenderTexture volumeTextureB;

        RenderTexture activeVolume;
        RenderTexture previousActiveVolume;

        RenderTexture dummyVoxelTexture;
        RenderTexture dummyVoxelTexture2;

        bool dontTry = false;

        Shader voxelizationShader;
        Shader voxelTracingShader;

        ComputeShader clearCompute;
        ComputeShader transferInts;
        ComputeShader mipFilter;

        const int numMipLevels = 6;

        Camera voxelCamera;
        GameObject voxelCameraGO;
        GameObject leftViewPoint;
        GameObject topViewPoint;

        float voxelScaleFactor { get { return (float)this.voxelResolution / 256.0f; } }

        Vector3 voxelSpaceOrigin;
        Vector3 previousVoxelSpaceOrigin;
        Vector3 voxelSpaceOriginDelta;


        Quaternion rotationFront = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
        Quaternion rotationLeft = new Quaternion(0.0f, 0.7f, 0.0f, 0.7f);
        Quaternion rotationTop = new Quaternion(0.7f, 0.0f, 0.0f, 0.7f);

        int voxelFlipFlop = 0;


        int giRenderRes { get { return this.halfResolution ? 2 : 1; } }

        enum RenderState
        {
            Voxelize,
            Bounce
        }

        RenderState renderState = RenderState.Voxelize;

        public SEGIPreset[] _presets = new[]
        {
            new SEGIPreset()
            {
                name = "Bright",
                voxelResolution = VoxelResolution.high,
                voxelAA = true,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.13f,
                useBilateralFiltering = false,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = true,
                cones = 32,
                coneTraceSteps = 11,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.64f,
                occlusionStrength = 0.69f,
                nearOcclusionStrength = 0.6f,
                occlusionPower = 0.6f,
                nearLightGain = 0,
                giGain = 1.36f,
                secondaryBounceGain = 1.74f,
                reflectionSteps = 73,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = false,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 1,
                secondaryCones = 6,
                secondaryOcclusionStrength = 0.7f
            },
            new SEGIPreset()
            {
                name = "Ultra",
                voxelResolution = VoxelResolution.high,
                voxelAA = true,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.13f,
                useBilateralFiltering = false,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = true,
                cones = 32,
                coneTraceSteps = 11,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.64f,
                occlusionStrength = 0.69f,
                nearOcclusionStrength = 0.6f,
                occlusionPower = 1.06f,
                nearLightGain = 0,
                giGain = 1.36f,
                secondaryBounceGain = 1.45f,
                reflectionSteps = 73,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = false,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 1,
                secondaryCones = 6,
                secondaryOcclusionStrength = 1,
            },
            new SEGIPreset()
            {
                name = "High",
                voxelResolution = VoxelResolution.high,
                voxelAA = true,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.13f,
                useBilateralFiltering = true,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = true,
                cones = 20,
                coneTraceSteps = 8,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.64f,
                occlusionStrength = 0.69f,
                nearOcclusionStrength = 0.6f,
                occlusionPower = 1.06f,
                nearLightGain = 0,
                giGain = 1.36f,
                secondaryBounceGain = 1.45f,
                reflectionSteps = 73,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = false,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 1,
                secondaryCones = 6,
                secondaryOcclusionStrength = 1,
            },
            new SEGIPreset()
            {
                name = "Insane",
                voxelResolution = VoxelResolution.high,
                voxelAA = true,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.13f,
                useBilateralFiltering = false,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = true,
                cones = 39,
                coneTraceSteps = 22,
                coneLength = 1,
                coneWidth = 4.11f,
                coneTraceBias = 0.64f,
                occlusionStrength = 0.69f,
                nearOcclusionStrength = 0.6f,
                occlusionPower = 1.06f,
                nearLightGain = 0,
                giGain = 1.36f,
                secondaryBounceGain = 1.45f,
                reflectionSteps = 73,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = false,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 1,
                secondaryCones = 6,
                secondaryOcclusionStrength = 1
            },
            new SEGIPreset()
            {
                name = "Low",
                voxelResolution = VoxelResolution.low,
                voxelAA = true,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.13f,
                useBilateralFiltering = true,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = false,
                cones = 11,
                coneTraceSteps = 8,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.64f,
                occlusionStrength = 0.56f,
                nearOcclusionStrength = 0.42f,
                occlusionPower = 0.62f,
                nearLightGain = 0,
                giGain = 1.09f,
                secondaryBounceGain = 1.84f,
                reflectionSteps = 70,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = false,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 1,
                secondaryCones = 3,
                secondaryOcclusionStrength = 1,
            },
            new SEGIPreset()
            {
                name = "Medium",
                voxelResolution = VoxelResolution.low,
                voxelAA = false,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.13f,
                useBilateralFiltering = true,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = true,
                cones = 7,
                coneTraceSteps = 9,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.64f,
                occlusionStrength = 0.69f,
                nearOcclusionStrength = 0.6f,
                occlusionPower = 1.06f,
                nearLightGain = 0,
                giGain = 1.17f,
                secondaryBounceGain = 1.87f,
                reflectionSteps = 70,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = false,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 1,
                secondaryCones = 6,
                secondaryOcclusionStrength = 1
            },
            new SEGIPreset()
            {
                name = "Sponza High",
                voxelResolution = VoxelResolution.high,
                voxelAA = false,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 1,
                useBilateralFiltering = false,
                halfResolution = true,
                stochasticSampling = false,
                doReflections = true,
                cones = 18,
                coneTraceSteps = 10,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.64f,
                occlusionStrength = 0.82f,
                nearOcclusionStrength = 0.42f,
                occlusionPower = 0.71f,
                nearLightGain = 1.07f,
                giGain = 0.5f,
                secondaryBounceGain = 1.43f,
                reflectionSteps = 59,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = false,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 1,
                secondaryCones = 4,
                secondaryOcclusionStrength = 1
            },
            new SEGIPreset()
            {
                name = "Sponza Low",
                voxelResolution = VoxelResolution.low,
                voxelAA = false,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.184f,
                useBilateralFiltering = true,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = false,
                cones = 8,
                coneTraceSteps = 8,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 1.12f,
                occlusionStrength = 0.8f,
                nearOcclusionStrength = 0.39f,
                occlusionPower = 0.7f,
                nearLightGain = 0.1f,
                giGain = 0.7f,
                secondaryBounceGain = 1.56f,
                reflectionSteps = 26,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = true,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 1,
                secondaryCones = 4,
                secondaryOcclusionStrength = 1,
            },
            new SEGIPreset()
            {
                name = "Sponza Medium",
                voxelResolution = VoxelResolution.low,
                voxelAA = false,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.137f,
                useBilateralFiltering = true,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = true,
                cones = 13,
                coneTraceSteps = 8,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.63f,
                occlusionStrength = 0.66f,
                nearOcclusionStrength = 0,
                occlusionPower = 1,
                nearLightGain = 0.1f,
                giGain = 0.75f,
                secondaryBounceGain = 1.4f,
                reflectionSteps = 64,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = true,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 0.94f,
                secondaryCones = 6,
                secondaryOcclusionStrength = 1,
            },
            new SEGIPreset()
            {
                name = "Sponza Ultra",
                voxelResolution = VoxelResolution.high,
                voxelAA = false,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 0.15f,
                useBilateralFiltering = false,
                halfResolution = true,
                stochasticSampling = true,
                doReflections = true,
                cones = 31,
                coneTraceSteps = 10,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.63f,
                occlusionStrength = 0.77f,
                nearOcclusionStrength = 0,
                occlusionPower = 1,
                nearLightGain = 0.26f,
                giGain = 0.77f,
                secondaryBounceGain = 1.19f,
                reflectionSteps = 64,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = false,
                farOcclusionStrength = 0.89f,
                farthestOcclusionStrength = 0.53f,
                secondaryCones = 4,
                secondaryOcclusionStrength = 0.7f,
            },
            new SEGIPreset()
            {
                name = "Ultra Clean",
                voxelResolution = VoxelResolution.high,
                voxelAA = true,
                innerOcclusionLayers = 1,
                infiniteBounces = true,
                temporalBlendWeight = 1,
                useBilateralFiltering = false,
                halfResolution = true,
                stochasticSampling = false,
                doReflections = true,
                cones = 15,
                coneTraceSteps = 11,
                coneLength = 1,
                coneWidth = 6,
                coneTraceBias = 0.64f,
                occlusionStrength = 0.46f,
                nearOcclusionStrength = 0.24f,
                occlusionPower = 0.96f,
                nearLightGain = 0,
                giGain = 1.36f,
                secondaryBounceGain = 1.64f,
                reflectionSteps = 73,
                reflectionOcclusionPower = 1,
                skyReflectionIntensity = 1,
                gaussianMipFilter = true,
                farOcclusionStrength = 1,
                farthestOcclusionStrength = 0.62f,
                secondaryCones = 6,
                secondaryOcclusionStrength = 1,
            }
        };
        private SEGIHSStandardCustom _segiHSStandardCustom;
        private Material _mixGBufferMaterial;
        private volatile RenderTexture _gBuffer0 = null;
        private volatile RenderTexture _gBuffer1 = null;
        private volatile RenderTexture _gBuffer2 = null;
        private CommandBuffer _mixGBufferCommand;
        private Shader _getGBufferShader;
        private Material _getGBufferMaterial;

        public void ApplyPreset(int index)
        {
            this.ApplyPreset(this._presets[index]);
        }

        public void ApplyPreset(SEGIPreset preset)
        {
            this.voxelResolution = preset.voxelResolution;
            this.voxelAA = preset.voxelAA;
            this.innerOcclusionLayers = preset.innerOcclusionLayers;
            this.infiniteBounces = preset.infiniteBounces;

            this.temporalBlendWeight = preset.temporalBlendWeight;
            this.useBilateralFiltering = preset.useBilateralFiltering;
            this.halfResolution = preset.halfResolution;
            this.stochasticSampling = preset.stochasticSampling;
            this.doReflections = preset.doReflections;

            this.cones = preset.cones;
            this.coneTraceSteps = preset.coneTraceSteps;
            this.coneLength = preset.coneLength;
            this.coneWidth = preset.coneWidth;
            this.coneTraceBias = preset.coneTraceBias;
            this.occlusionStrength = preset.occlusionStrength;
            this.nearOcclusionStrength = preset.nearOcclusionStrength;
            this.occlusionPower = preset.occlusionPower;
            this.nearLightGain = preset.nearLightGain;
            this.giGain = preset.giGain;
            this.secondaryBounceGain = preset.secondaryBounceGain;

            this.reflectionSteps = preset.reflectionSteps;
            this.reflectionOcclusionPower = preset.reflectionOcclusionPower;
            this.skyReflectionIntensity = preset.skyReflectionIntensity;
            this.gaussianMipFilter = preset.gaussianMipFilter;

            this.farOcclusionStrength = preset.farOcclusionStrength;
            this.farthestOcclusionStrength = preset.farthestOcclusionStrength;
            this.secondaryCones = preset.secondaryCones;
            this.secondaryOcclusionStrength = preset.secondaryOcclusionStrength;
        }

        public SEGI(Camera camera)
        {
            this.attachedCamera = camera;
            this.attachedCamera.cullingMask = this.attachedCamera.cullingMask & ~(1 << 27);
            this.InitCheck();
        }

        void InitCheck()
        {
            if (this.initChecker == null)
            {
                this.Init();
            }
        }

        void CreateVolumeTextures()
        {
            this.volumeTextures = new RenderTexture[numMipLevels];

            for (int i = 0; i < numMipLevels; i++)
            {
                if (this.volumeTextures[i])
                {
                    this.volumeTextures[i].DiscardContents();
                    this.volumeTextures[i].Release();
                    UnityEngine.Object.DestroyImmediate(this.volumeTextures[i]);
                }
                int resolution = (int)this.voxelResolution / Mathf.RoundToInt(Mathf.Pow((float)2, (float)i));
                this.volumeTextures[i] = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                this.volumeTextures[i].isVolume = true;
                this.volumeTextures[i].volumeDepth = resolution;
                this.volumeTextures[i].enableRandomWrite = true;
                this.volumeTextures[i].filterMode = FilterMode.Bilinear;
                this.volumeTextures[i].generateMips = false;
                this.volumeTextures[i].useMipMap = false;
                this.volumeTextures[i].Create();
                this.volumeTextures[i].hideFlags = HideFlags.HideAndDontSave;
            }

            if (this.volumeTextureB)
            {
                this.volumeTextureB.DiscardContents();
                this.volumeTextureB.Release();
                UnityEngine.Object.DestroyImmediate(this.volumeTextureB);
            }
            this.volumeTextureB = new RenderTexture((int)this.voxelResolution, (int)this.voxelResolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            this.volumeTextureB.isVolume = true;
            this.volumeTextureB.volumeDepth = (int)this.voxelResolution;
            this.volumeTextureB.enableRandomWrite = true;
            this.volumeTextureB.filterMode = FilterMode.Bilinear;
            this.volumeTextureB.generateMips = false;
            this.volumeTextureB.useMipMap = false;
            this.volumeTextureB.Create();
            this.volumeTextureB.hideFlags = HideFlags.HideAndDontSave;

            if (this.volumeTexture1)
            {
                this.volumeTexture1.DiscardContents();
                this.volumeTexture1.Release();
                UnityEngine.Object.DestroyImmediate(this.volumeTexture1);
            }
            this.volumeTexture1 = new RenderTexture((int)this.voxelResolution, (int)this.voxelResolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            this.volumeTexture1.isVolume = true;
            this.volumeTexture1.volumeDepth = (int)this.voxelResolution;
            this.volumeTexture1.enableRandomWrite = true;
            this.volumeTexture1.filterMode = FilterMode.Point;
            this.volumeTexture1.generateMips = false;
            this.volumeTexture1.useMipMap = false;
            this.volumeTexture1.antiAliasing = 1;
            this.volumeTexture1.Create();
            this.volumeTexture1.hideFlags = HideFlags.HideAndDontSave;



            if (this.intTex1)
            {
                this.intTex1.DiscardContents();
                this.intTex1.Release();
                UnityEngine.Object.DestroyImmediate(this.intTex1);
            }
            this.intTex1 = new RenderTexture((int)this.voxelResolution, (int)this.voxelResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
            this.intTex1.isVolume = true;
            this.intTex1.volumeDepth = (int)this.voxelResolution;
            this.intTex1.enableRandomWrite = true;
            this.intTex1.filterMode = FilterMode.Point;
            this.intTex1.Create();
            this.intTex1.hideFlags = HideFlags.HideAndDontSave;

            this.ResizeDummyTexture();

        }

        void ResizeDummyTexture()
        {
            if (this.dummyVoxelTexture)
            {
                this.dummyVoxelTexture.DiscardContents();
                this.dummyVoxelTexture.Release();
                UnityEngine.Object.DestroyImmediate(this.dummyVoxelTexture);
            }
            this.dummyVoxelTexture = new RenderTexture(this.dummyVoxelResolution, this.dummyVoxelResolution, 0, RenderTextureFormat.R8);
            this.dummyVoxelTexture.Create();
            this.dummyVoxelTexture.hideFlags = HideFlags.HideAndDontSave;

            if (this.dummyVoxelTexture2)
            {
                this.dummyVoxelTexture2.DiscardContents();
                this.dummyVoxelTexture2.Release();
                UnityEngine.Object.DestroyImmediate(this.dummyVoxelTexture2);
            }
            this.dummyVoxelTexture2 = new RenderTexture((int)this.voxelResolution, (int)this.voxelResolution, 0, RenderTextureFormat.R8);
            this.dummyVoxelTexture2.Create();
            this.dummyVoxelTexture2.hideFlags = HideFlags.HideAndDontSave;
        }

        void Init()
        {
            this.sunDepthShader = HSLRE.self._resources.LoadAsset<Shader>("SEGIRenderSunDepth");

            this.material = new Material(HSLRE.self._resources.LoadAsset<Shader>("SEGI"));
            this.material.hideFlags = HideFlags.HideAndDontSave;
            this.attachedCamera.depthTextureMode |= DepthTextureMode.Depth;

            GameObject scgo = GameObject.Find("SEGI_SHADOWCAM");

            this.clearCompute = HSLRE.self._resources.LoadAsset<ComputeShader>("SEGIClear");
            this.transferInts = HSLRE.self._resources.LoadAsset<ComputeShader>("SEGITransferInts");
            this.mipFilter = HSLRE.self._resources.LoadAsset<ComputeShader>("SEGIMipFilter");

            if (!scgo)
            {
                this.shadowCamGameObject = new GameObject("SEGI_SHADOWCAM");
                this.shadowCam = this.shadowCamGameObject.AddComponent<Camera>();
                this.shadowCamGameObject.hideFlags = HideFlags.HideAndDontSave;

                this.shadowCam.enabled = false;
                this.shadowCam.depth = this.attachedCamera.depth - 1;
                this.shadowCam.orthographic = true;
                this.shadowCam.orthographicSize = this.shadowSpaceSize;
                this.shadowCam.clearFlags = CameraClearFlags.SolidColor;
                this.shadowCam.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                this.shadowCam.farClipPlane = this.shadowSpaceSize * 2.0f * this.shadowSpaceDepthRatio;
                this.shadowCam.cullingMask = this.giCullingMask;
                this.shadowCam.useOcclusionCulling = false;

                this.shadowCamTransform = this.shadowCamGameObject.transform;
            }
            else
            {
                this.shadowCamGameObject = scgo;
                this.shadowCam = scgo.GetComponent<Camera>();
                this.shadowCamTransform = this.shadowCamGameObject.transform;
            }

            if (this.sunDepthTexture)
            {
                this.sunDepthTexture.DiscardContents();
                this.sunDepthTexture.Release();
                UnityEngine.Object.DestroyImmediate(this.sunDepthTexture);
            }
            this.sunDepthTexture = new RenderTexture(this.sunShadowResolution, this.sunShadowResolution, 16, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            this.sunDepthTexture.wrapMode = TextureWrapMode.Clamp;
            this.sunDepthTexture.filterMode = FilterMode.Point;
            this.sunDepthTexture.Create();
            this.sunDepthTexture.hideFlags = HideFlags.HideAndDontSave;

            this.voxelizationShader = HSLRE.self._resources.LoadAsset<Shader>("SEGIVoxelizeScene");
            this.voxelTracingShader = HSLRE.self._resources.LoadAsset<Shader>("SEGITraceScene");

            this.CreateVolumeTextures();



            GameObject vcgo = GameObject.Find("SEGI_VOXEL_CAMERA");
            if (vcgo)
                UnityEngine.Object.DestroyImmediate(vcgo);

            this.voxelCameraGO = new GameObject("SEGI_VOXEL_CAMERA");
            this.voxelCameraGO.hideFlags = HideFlags.HideAndDontSave;

            this.voxelCamera = this.voxelCameraGO.AddComponent<Camera>();
            this.voxelCamera.enabled = false;
            this.voxelCamera.orthographic = true;
            this.voxelCamera.orthographicSize = this.voxelSpaceSize * 0.5f;
            this.voxelCamera.nearClipPlane = 0.0f;
            this.voxelCamera.farClipPlane = this.voxelSpaceSize;
            this.voxelCamera.depth = -2;
            this.voxelCamera.renderingPath = RenderingPath.Forward;
            this.voxelCamera.clearFlags = CameraClearFlags.Color;
            this.voxelCamera.backgroundColor = Color.black;
            this.voxelCamera.useOcclusionCulling = false;

            GameObject lvp = GameObject.Find("SEGI_LEFT_VOXEL_VIEW");
            if (lvp)
                UnityEngine.Object.DestroyImmediate(lvp);

            this.leftViewPoint = new GameObject("SEGI_LEFT_VOXEL_VIEW");
            this.leftViewPoint.hideFlags = HideFlags.HideAndDontSave;

            GameObject tvp = GameObject.Find("SEGI_TOP_VOXEL_VIEW");
            if (tvp)
                UnityEngine.Object.DestroyImmediate(tvp);

            this.topViewPoint = new GameObject("SEGI_TOP_VOXEL_VIEW");
            this.topViewPoint.hideFlags = HideFlags.HideAndDontSave;

            this.giCullingMask = LayerMask.GetMask("Default", "Water", "Chara", "Map", "MapNoShadow") | (1 << 27);

            GameObject hss = GameObject.Find("SEGI_HSSTANDARD");
            if (hss)
                UnityEngine.Object.DestroyImmediate(hss);

            hss = new GameObject("SEGI_HSSTANDARD");
            hss.transform.SetParent(this.attachedCamera.transform);
            hss.transform.localPosition = Vector3.zero;
            hss.transform.localRotation = Quaternion.identity;
            hss.transform.localScale = Vector3.one;
            this._segiHSStandardCustom = hss.AddComponent<SEGIHSStandardCustom>();
            hss.hideFlags = HideFlags.DontSave;

            this._mixGBufferMaterial = new Material(HSLRE.self._resources.LoadAsset<Shader>("MixGBuffers"));
            this._mixGBufferCommand = new CommandBuffer();
            this._mixGBufferCommand.name = "Mix GBuffer";

            this._getGBufferShader = HSLRE.self._resources.LoadAsset<Shader>("GetGBuffer");
            this._getGBufferMaterial = new Material(this._getGBufferShader);

            this.initChecker = new object();
        }

        void CheckSupport()
        {
            this.systemSupported.hdrTextures = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
            this.systemSupported.rIntTextures = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RInt);
            this.systemSupported.dx11 = SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders;
            this.systemSupported.volumeTextures = SystemInfo.supports3DTextures;

            this.systemSupported.postShader = this.material.shader.isSupported;
            this.systemSupported.sunDepthShader = this.sunDepthShader.isSupported;
            this.systemSupported.voxelizationShader = this.voxelizationShader.isSupported;
            this.systemSupported.tracingShader = this.voxelTracingShader.isSupported;

            if (!this.systemSupported.fullFunctionality)
            {
                Debug.LogWarning("SEGI is not supported on the current platform. Check for shader compile errors in SEGI/Resources");
                if (HSLRE.self.effectsDictionary.TryGetValue(this, out HSLRE.EffectData data))
                    data.enabled = false;
            }
        }

        void CleanupTexture(ref RenderTexture texture)
        {
            texture.DiscardContents();
            UnityEngine.Object.DestroyImmediate(texture);
        }

        void CleanupTextures()
        {
            this.CleanupTexture(ref this.sunDepthTexture);
            this.CleanupTexture(ref this.previousResult);
            this.CleanupTexture(ref this.previousDepth);
            this.CleanupTexture(ref this.intTex1);
            for (int i = 0; i < this.volumeTextures.Length; i++)
            {
                this.CleanupTexture(ref this.volumeTextures[i]);
            }
            this.CleanupTexture(ref this.volumeTexture1);
            this.CleanupTexture(ref this.volumeTextureB);
            this.CleanupTexture(ref this.dummyVoxelTexture);
            this.CleanupTexture(ref this.dummyVoxelTexture2);

            if (this._gBuffer0 != null)
                RenderTexture.ReleaseTemporary(this._gBuffer0);
            this._gBuffer0 = null;
            if (this._gBuffer1 != null)
                RenderTexture.ReleaseTemporary(this._gBuffer1);
            this._gBuffer1 = null;
            if (this._gBuffer2 != null)
                RenderTexture.ReleaseTemporary(this._gBuffer2);
            this._gBuffer2 = null;
        }

        void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(this.material);
            UnityEngine.Object.DestroyImmediate(this.voxelCameraGO);
            UnityEngine.Object.DestroyImmediate(this.leftViewPoint);
            UnityEngine.Object.DestroyImmediate(this.topViewPoint);
            UnityEngine.Object.DestroyImmediate(this.shadowCamGameObject);
            UnityEngine.Object.DestroyImmediate(this._segiHSStandardCustom.gameObject);
            this.initChecker = null;

            this.CleanupTextures();

            UnityEngine.Object.DestroyImmediate(this._mixGBufferMaterial);
            this.attachedCamera.RemoveCommandBuffer(CameraEvent.BeforeLighting, this._mixGBufferCommand);
        }

        void OnEnable()
        {
            this.InitCheck();
            this.ResizeRenderTextures();

            this.CheckSupport();
        }

        void OnDisable()
        {
            this.Cleanup();
        }

        void ResizeRenderTextures()
        {
            if (this.previousResult)
            {
                this.previousResult.DiscardContents();
                this.previousResult.Release();
                UnityEngine.Object.DestroyImmediate(this.previousResult);
            }

            int width = this.attachedCamera.pixelWidth == 0 ? 2 : this.attachedCamera.pixelWidth;
            int height = this.attachedCamera.pixelHeight == 0 ? 2 : this.attachedCamera.pixelHeight;

            this.previousResult = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
            this.previousResult.wrapMode = TextureWrapMode.Clamp;
            this.previousResult.filterMode = FilterMode.Bilinear;
            this.previousResult.useMipMap = true;
            this.previousResult.generateMips = true;
            this.previousResult.Create();
            this.previousResult.hideFlags = HideFlags.HideAndDontSave;

            if (this.previousDepth)
            {
                this.previousDepth.DiscardContents();
                this.previousDepth.Release();
                UnityEngine.Object.DestroyImmediate(this.previousDepth);
            }
            this.previousDepth = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            this.previousDepth.wrapMode = TextureWrapMode.Clamp;
            this.previousDepth.filterMode = FilterMode.Bilinear;
            this.previousDepth.Create();
            this.previousDepth.hideFlags = HideFlags.HideAndDontSave;
        }

        void ResizeSunShadowBuffer()
        {

            if (this.sunDepthTexture)
            {
                this.sunDepthTexture.DiscardContents();
                this.sunDepthTexture.Release();
                UnityEngine.Object.DestroyImmediate(this.sunDepthTexture);
            }
            this.sunDepthTexture = new RenderTexture(this.sunShadowResolution, this.sunShadowResolution, 16, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            this.sunDepthTexture.wrapMode = TextureWrapMode.Clamp;
            this.sunDepthTexture.filterMode = FilterMode.Point;
            this.sunDepthTexture.Create();
            this.sunDepthTexture.hideFlags = HideFlags.HideAndDontSave;
        }

        public void Update()
        {
            if (this._segiHSStandardCustom != null)
            {
                this._segiHSStandardCustom.gameObject.SetActive(this.hsStandardCustomShadersCompatibility);
            }
            if (this.dontTry)
                return;

            if (this.previousResult == null)
            {
                this.ResizeRenderTextures();
            }

            if (this.previousResult.width != this.attachedCamera.pixelWidth || this.previousResult.height != this.attachedCamera.pixelHeight)
            {
                this.ResizeRenderTextures();
            }

            if ((int)this.sunShadowResolution != this.prevSunShadowResolution)
            {
                this.ResizeSunShadowBuffer();
            }

            this.prevSunShadowResolution = (int)this.sunShadowResolution;

            if (this.volumeTextures[0].width != (int)this.voxelResolution)
            {
                this.CreateVolumeTextures();
            }

            if (this.dummyVoxelTexture.width != this.dummyVoxelResolution)
            {
                this.ResizeDummyTexture();
            }
        }

        Matrix4x4 TransformViewMatrix(Matrix4x4 mat)
        {
#if UNITY_5_5_OR_NEWER
        if (SystemInfo.usesReversedZBuffer)
        {
            mat[2, 0] = -mat[2, 0];
            mat[2, 1] = -mat[2, 1];
            mat[2, 2] = -mat[2, 2];
            mat[2, 3] = -mat[2, 3];
           // mat[3, 2] += 0.0f;
        }
#endif
            return mat;
        }

        public void OnPreRender()
        {
            this.InitCheck();

            if (this.dontTry)
                return;

            this.attachedCamera.RemoveCommandBuffer(CameraEvent.BeforeLighting, this._mixGBufferCommand);

            if (this._gBuffer0 != null)
                RenderTexture.ReleaseTemporary(this._gBuffer0);
            this._gBuffer0 = null;
            if (this._gBuffer1 != null)
                RenderTexture.ReleaseTemporary(this._gBuffer1);
            this._gBuffer1 = null;
            if (this._gBuffer2 != null)
                RenderTexture.ReleaseTemporary(this._gBuffer2);
            this._gBuffer2 = null;

            if (this.hsStandardCustomShadersCompatibility)
            {
                this._gBuffer0 = RenderTexture.GetTemporary(this.attachedCamera.pixelWidth, this.attachedCamera.pixelHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                this._gBuffer0.filterMode = FilterMode.Point;
                this._gBuffer1 = RenderTexture.GetTemporary(this.attachedCamera.pixelWidth, this.attachedCamera.pixelHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                this._gBuffer1.filterMode = FilterMode.Point;
                this._gBuffer2 = RenderTexture.GetTemporary(this.attachedCamera.pixelWidth, this.attachedCamera.pixelHeight, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
                this._gBuffer2.filterMode = FilterMode.Point;

                this._mixGBufferCommand.Clear();
                this._mixGBufferCommand.Blit(new RenderTargetIdentifier(BuiltinRenderTextureType.GBuffer0), new RenderTargetIdentifier(this._gBuffer0));
                this._mixGBufferCommand.Blit(new RenderTargetIdentifier(BuiltinRenderTextureType.GBuffer1), new RenderTargetIdentifier(this._gBuffer1));
                this._mixGBufferCommand.Blit(new RenderTargetIdentifier(BuiltinRenderTextureType.GBuffer2), new RenderTargetIdentifier(this._gBuffer2));
                this._mixGBufferMaterial.SetTexture("_GBuffer0", this._gBuffer0);
                this._mixGBufferMaterial.SetTexture("_GBuffer1", this._gBuffer1);
                this._mixGBufferMaterial.SetTexture("_GBuffer2", this._gBuffer2);
                this._mixGBufferMaterial.SetTexture("_HSSDepth", this._segiHSStandardCustom.Depth);
                this._mixGBufferCommand.Blit(new RenderTargetIdentifier(this._segiHSStandardCustom.GBuffer0), new RenderTargetIdentifier(BuiltinRenderTextureType.GBuffer0), this._mixGBufferMaterial, 0);
                this._mixGBufferCommand.Blit(new RenderTargetIdentifier(this._segiHSStandardCustom.GBuffer1), new RenderTargetIdentifier(BuiltinRenderTextureType.GBuffer1), this._mixGBufferMaterial, 1);
                this._mixGBufferCommand.Blit(new RenderTargetIdentifier(this._segiHSStandardCustom.GBuffer2), new RenderTargetIdentifier(BuiltinRenderTextureType.GBuffer2), this._mixGBufferMaterial, 2);
                this.attachedCamera.AddCommandBuffer(CameraEvent.BeforeLighting, this._mixGBufferCommand);
            }

            if (!this.updateGI)
            {
                return;
            }

            RenderTexture previousActive = RenderTexture.active;

            Shader.SetGlobalInt("SEGIVoxelAA", this.voxelAA ? 1 : 0);

            if (this.renderState == RenderState.Voxelize)
            {
                this.activeVolume = this.voxelFlipFlop == 0 ? this.volumeTextures[0] : this.volumeTextureB;
                this.previousActiveVolume = this.voxelFlipFlop == 0 ? this.volumeTextureB : this.volumeTextures[0];

                float voxelTexel = (1.0f * this.voxelSpaceSize) / (int)this.voxelResolution * 0.5f;

                float interval = this.voxelSpaceSize / 8.0f;
                Vector3 origin;
                if (this.followTransform)
                {
                    origin = this.followTransform.position;
                }
                else
                {
                    origin = this.attachedCamera.transform.position + this.attachedCamera.transform.forward * this.voxelSpaceSize / 4.0f;
                }
                this.voxelSpaceOrigin = new Vector3(Mathf.Round(origin.x / interval) * interval, Mathf.Round(origin.y / interval) * interval, Mathf.Round(origin.z / interval) * interval) + new Vector3(1.0f, 1.0f, 1.0f) * ((float)this.voxelFlipFlop * 2.0f - 1.0f) * voxelTexel * 0.0f;

                this.voxelSpaceOriginDelta = this.voxelSpaceOrigin - this.previousVoxelSpaceOrigin;
                Shader.SetGlobalVector("SEGIVoxelSpaceOriginDelta", this.voxelSpaceOriginDelta / this.voxelSpaceSize);

                this.previousVoxelSpaceOrigin = this.voxelSpaceOrigin;



                Shader.SetGlobalMatrix("WorldToGI", this.shadowCam.worldToCameraMatrix);
                Shader.SetGlobalMatrix("GIToWorld", this.shadowCam.cameraToWorldMatrix);
                Shader.SetGlobalMatrix("GIProjection", this.shadowCam.projectionMatrix);
                Shader.SetGlobalMatrix("GIProjectionInverse", this.shadowCam.projectionMatrix.inverse);
                Shader.SetGlobalMatrix("WorldToCamera", this.attachedCamera.worldToCameraMatrix);
                Shader.SetGlobalFloat("GIDepthRatio", this.shadowSpaceDepthRatio);

                Shader.SetGlobalColor("GISunColor", this.sun == null ? Color.black : new Color(Mathf.Pow(this.sun.color.r, 2.2f), Mathf.Pow(this.sun.color.g, 2.2f), Mathf.Pow(this.sun.color.b, 2.2f), Mathf.Pow(this.sun.intensity, 2.2f)));
                Shader.SetGlobalColor("SEGISkyColor", new Color(Mathf.Pow(this.skyColor.r * this.skyIntensity * 0.5f, 2.2f), Mathf.Pow(this.skyColor.g * this.skyIntensity * 0.5f, 2.2f), Mathf.Pow(this.skyColor.b * this.skyIntensity * 0.5f, 2.2f), Mathf.Pow(this.skyColor.a, 2.2f)));
                Shader.SetGlobalFloat("GIGain", this.giGain);


                if (this.sun != null)
                {
                    this.shadowCam.cullingMask = this.giCullingMask;

                    Vector3 shadowCamPosition = this.voxelSpaceOrigin + Vector3.Normalize(-this.sun.transform.forward) * this.shadowSpaceSize * 0.5f * this.shadowSpaceDepthRatio;

                    this.shadowCamTransform.position = shadowCamPosition;
                    this.shadowCamTransform.LookAt(this.voxelSpaceOrigin, Vector3.up);

                    this.shadowCam.renderingPath = RenderingPath.Forward;
                    this.shadowCam.depthTextureMode |= DepthTextureMode.None;

                    this.shadowCam.orthographicSize = this.shadowSpaceSize;
                    this.shadowCam.farClipPlane = this.shadowSpaceSize * 2.0f * this.shadowSpaceDepthRatio;


                    Graphics.SetRenderTarget(this.sunDepthTexture);
                    this.shadowCam.SetTargetBuffers(this.sunDepthTexture.colorBuffer, this.sunDepthTexture.depthBuffer);

                    this.shadowCam.RenderWithShader(this.sunDepthShader, "");

                    Shader.SetGlobalTexture("SEGISunDepth", this.sunDepthTexture);
                }

                this.voxelCamera.enabled = false;
                this.voxelCamera.orthographic = true;
                this.voxelCamera.orthographicSize = this.voxelSpaceSize * 0.5f;
                this.voxelCamera.nearClipPlane = 0.0f;
                this.voxelCamera.farClipPlane = this.voxelSpaceSize;
                this.voxelCamera.depth = -2;
                this.voxelCamera.renderingPath = RenderingPath.Forward;
                this.voxelCamera.clearFlags = CameraClearFlags.Color;
                this.voxelCamera.backgroundColor = Color.black;
                this.voxelCamera.cullingMask = this.giCullingMask;

                this.voxelFlipFlop += 1;
                this.voxelFlipFlop = this.voxelFlipFlop % 2;

                this.voxelCameraGO.transform.position = this.voxelSpaceOrigin - Vector3.forward * this.voxelSpaceSize * 0.5f;
                this.voxelCameraGO.transform.rotation = this.rotationFront;

                this.leftViewPoint.transform.position = this.voxelSpaceOrigin + Vector3.left * this.voxelSpaceSize * 0.5f;
                this.leftViewPoint.transform.rotation = this.rotationLeft;
                this.topViewPoint.transform.position = this.voxelSpaceOrigin + Vector3.up * this.voxelSpaceSize * 0.5f;
                this.topViewPoint.transform.rotation = this.rotationTop;

                Shader.SetGlobalInt("SEGIVoxelResolution", (int)this.voxelResolution);

                Shader.SetGlobalMatrix("SEGIVoxelViewFront", this.TransformViewMatrix(this.voxelCamera.transform.worldToLocalMatrix));
                Shader.SetGlobalMatrix("SEGIVoxelViewLeft", this.TransformViewMatrix(this.leftViewPoint.transform.worldToLocalMatrix));
                Shader.SetGlobalMatrix("SEGIVoxelViewTop", this.TransformViewMatrix(this.topViewPoint.transform.worldToLocalMatrix));
                Shader.SetGlobalMatrix("SEGIWorldToVoxel", this.voxelCamera.worldToCameraMatrix);
                Shader.SetGlobalMatrix("SEGIVoxelProjection", this.voxelCamera.projectionMatrix);
                Shader.SetGlobalMatrix("SEGIVoxelProjectionInverse", this.voxelCamera.projectionMatrix.inverse);



                Shader.SetGlobalFloat("SEGISecondaryBounceGain", this.infiniteBounces ? this.secondaryBounceGain : 0.0f);
                Shader.SetGlobalFloat("SEGISoftSunlight", this.softSunlight);
                Shader.SetGlobalInt("SEGISphericalSkylight", this.sphericalSkylight ? 1 : 0);
                Shader.SetGlobalInt("SEGIInnerOcclusionLayers", this.innerOcclusionLayers);

                this.clearCompute.SetTexture(0, "RG0", this.intTex1);
                //clearCompute.SetTexture(0, "BA0", ba0);
                this.clearCompute.SetInt("Res", (int)this.voxelResolution);
                this.clearCompute.Dispatch(0, (int)this.voxelResolution / 16, (int)this.voxelResolution / 16, 1);

                Matrix4x4 voxelToGIProjection = (this.shadowCam.projectionMatrix) * (this.shadowCam.worldToCameraMatrix) * (this.voxelCamera.cameraToWorldMatrix);
                Shader.SetGlobalMatrix("SEGIVoxelToGIProjection", voxelToGIProjection);
                Shader.SetGlobalVector("SEGISunlightVector", this.sun ? Vector3.Normalize(this.sun.transform.forward) : Vector3.up);


                Graphics.SetRandomWriteTarget(1, this.intTex1);
                this.voxelCamera.targetTexture = this.dummyVoxelTexture;
                this.voxelCamera.RenderWithShader(this.voxelizationShader, "");
                Graphics.ClearRandomWriteTargets();

                this.transferInts.SetTexture(0, "Result", this.activeVolume);
                this.transferInts.SetTexture(0, "PrevResult", this.previousActiveVolume);
                this.transferInts.SetTexture(0, "RG0", this.intTex1);
                this.transferInts.SetInt("VoxelAA", this.voxelAA ? 1 : 0);
                this.transferInts.SetInt("Resolution", (int)this.voxelResolution);
                this.transferInts.SetVector("VoxelOriginDelta", (this.voxelSpaceOriginDelta / this.voxelSpaceSize) * (int)this.voxelResolution);
                this.transferInts.Dispatch(0, (int)this.voxelResolution / 16, (int)this.voxelResolution / 16, 1);

                Shader.SetGlobalTexture("SEGIVolumeLevel0", this.activeVolume);

                for (int i = 0; i < numMipLevels - 1; i++)
                {
                    RenderTexture source = this.volumeTextures[i];

                    if (i == 0)
                    {
                        source = this.activeVolume;
                    }

                    int destinationRes = (int)this.voxelResolution / Mathf.RoundToInt(Mathf.Pow((float)2, (float)i + 1.0f));
                    this.mipFilter.SetInt("destinationRes", destinationRes);
                    this.mipFilter.SetTexture(this.mipFilterKernel, "Source", source);
                    this.mipFilter.SetTexture(this.mipFilterKernel, "Destination", this.volumeTextures[i + 1]);
                    this.mipFilter.Dispatch(this.mipFilterKernel, destinationRes / 8, destinationRes / 8, 1);
                    Shader.SetGlobalTexture("SEGIVolumeLevel" + (i + 1).ToString(), this.volumeTextures[i + 1]);
                }

                if (this.infiniteBounces)
                {
                    this.renderState = RenderState.Bounce;
                }
            }
            else if (this.renderState == RenderState.Bounce)
            {
                this.clearCompute.SetTexture(0, "RG0", this.intTex1);
                this.clearCompute.Dispatch(0, (int)this.voxelResolution / 16, (int)this.voxelResolution / 16, 1);

                Shader.SetGlobalInt("SEGISecondaryCones", this.secondaryCones);
                Shader.SetGlobalFloat("SEGISecondaryOcclusionStrength", this.secondaryOcclusionStrength);

                Graphics.SetRandomWriteTarget(1, this.intTex1);
                this.voxelCamera.targetTexture = this.dummyVoxelTexture2;
                this.voxelCamera.RenderWithShader(this.voxelTracingShader, "");
                Graphics.ClearRandomWriteTargets();

                this.transferInts.SetTexture(1, "Result", this.volumeTexture1);
                this.transferInts.SetTexture(1, "RG0", this.intTex1);
                this.transferInts.SetInt("Resolution", (int)this.voxelResolution);
                this.transferInts.Dispatch(1, (int)this.voxelResolution / 16, (int)this.voxelResolution / 16, 1);

                Shader.SetGlobalTexture("SEGIVolumeTexture1", this.volumeTexture1);

                this.renderState = RenderState.Voxelize;
            }
            Matrix4x4 giToVoxelProjection = this.voxelCamera.projectionMatrix * this.voxelCamera.worldToCameraMatrix * this.shadowCam.cameraToWorldMatrix;
            Shader.SetGlobalMatrix("GIToVoxelProjection", giToVoxelProjection);

            RenderTexture.active = previousActive;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (this.dontTry)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (this.visualizeGBuffers != 0)
            {
                Graphics.Blit(null, destination, this._getGBufferMaterial, this.visualizeGBuffers - 1);
                return;
            }

            Shader.SetGlobalFloat("SEGIVoxelScaleFactor", this.voxelScaleFactor);

            this.material.SetMatrix("CameraToWorld", this.attachedCamera.cameraToWorldMatrix);
            this.material.SetMatrix("WorldToCamera", this.attachedCamera.worldToCameraMatrix);
            this.material.SetMatrix("ProjectionMatrixInverse", this.attachedCamera.projectionMatrix.inverse);
            this.material.SetMatrix("ProjectionMatrix", this.attachedCamera.projectionMatrix);
            this.material.SetInt("FrameSwitch", this.frameSwitch);
            Shader.SetGlobalInt("SEGIFrameSwitch", this.frameSwitch);
            this.material.SetVector("CameraPosition", this.attachedCamera.transform.position);
            this.material.SetFloat("DeltaTime", Time.deltaTime);

            this.material.SetInt("StochasticSampling", this.stochasticSampling ? 1 : 0);
            this.material.SetInt("TraceDirections", this.cones);
            this.material.SetInt("TraceSteps", this.coneTraceSteps);
            this.material.SetFloat("TraceLength", this.coneLength);
            this.material.SetFloat("ConeSize", this.coneWidth);
            this.material.SetFloat("OcclusionStrength", this.occlusionStrength);
            this.material.SetFloat("OcclusionPower", this.occlusionPower);
            this.material.SetFloat("ConeTraceBias", this.coneTraceBias);
            this.material.SetFloat("GIGain", this.giGain);
            this.material.SetFloat("NearLightGain", this.nearLightGain);
            this.material.SetFloat("NearOcclusionStrength", this.nearOcclusionStrength);
            this.material.SetInt("DoReflections", this.doReflections ? 1 : 0);
            this.material.SetInt("HalfResolution", this.halfResolution ? 1 : 0);
            this.material.SetInt("ReflectionSteps", this.reflectionSteps);
            this.material.SetFloat("ReflectionOcclusionPower", this.reflectionOcclusionPower);
            this.material.SetFloat("SkyReflectionIntensity", this.skyReflectionIntensity);
            this.material.SetFloat("FarOcclusionStrength", this.farOcclusionStrength);
            this.material.SetFloat("FarthestOcclusionStrength", this.farthestOcclusionStrength);

            if (this.visualizeVoxels)
            {
                Graphics.Blit(source, destination, this.material, Pass.VisualizeVoxels);
                return;
            }

            RenderTexture gi1 = RenderTexture.GetTemporary(source.width / this.giRenderRes, source.height / this.giRenderRes, 0, RenderTextureFormat.ARGBHalf);
            RenderTexture gi2 = RenderTexture.GetTemporary(source.width / this.giRenderRes, source.height / this.giRenderRes, 0, RenderTextureFormat.ARGBHalf);
            RenderTexture reflections = null;

            if (this.doReflections)
            {
                reflections = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
            }

            RenderTexture currentDepth = RenderTexture.GetTemporary(source.width / this.giRenderRes, source.height / this.giRenderRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            currentDepth.filterMode = FilterMode.Point;

            RenderTexture currentNormal = RenderTexture.GetTemporary(source.width / this.giRenderRes, source.height / this.giRenderRes, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            currentNormal.filterMode = FilterMode.Point;

            Graphics.Blit(source, currentDepth, this.material, Pass.GetCameraDepthTexture);
            this.material.SetTexture("CurrentDepth", currentDepth);
            Graphics.Blit(source, currentNormal, this.material, Pass.GetWorldNormals);
            this.material.SetTexture("CurrentNormal", currentNormal);

            this.material.SetTexture("PreviousGITexture", this.previousResult);
            Shader.SetGlobalTexture("PreviousGITexture", this.previousResult);
            this.material.SetTexture("PreviousDepth", this.previousDepth);


            Graphics.Blit(source, gi2, this.material, Pass.DiffuseTrace);
            if (this.doReflections)
            {
                Graphics.Blit(source, reflections, this.material, Pass.SpecularTrace);
                this.material.SetTexture("Reflections", reflections);
            }

            this.material.SetFloat("BlendWeight", this.temporalBlendWeight);


            if (this.useBilateralFiltering)
            {
                this.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));
                Graphics.Blit(gi2, gi1, this.material, Pass.BilateralBlur);

                this.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                Graphics.Blit(gi1, gi2, this.material, Pass.BilateralBlur);

                this.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));
                Graphics.Blit(gi2, gi1, this.material, Pass.BilateralBlur);

                this.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                Graphics.Blit(gi1, gi2, this.material, Pass.BilateralBlur);
            }

            if (this.giRenderRes == 2)
            {
                RenderTexture.ReleaseTemporary(gi1);


                RenderTexture gi3 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
                RenderTexture gi4 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);

                gi2.filterMode = FilterMode.Point;
                Graphics.Blit(gi2, gi4);

                RenderTexture.ReleaseTemporary(gi2);

                gi4.filterMode = FilterMode.Point;
                gi3.filterMode = FilterMode.Point;

                this.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                Graphics.Blit(gi4, gi3, this.material, Pass.BilateralUpsample);
                this.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));

                if (this.temporalBlendWeight < 1.0f)
                {
                    Graphics.Blit(gi3, gi4);
                    Graphics.Blit(gi4, gi3, this.material, Pass.TemporalBlend);
                    Graphics.Blit(gi3, this.previousResult);
                    Graphics.Blit(source, this.previousDepth, this.material, Pass.GetCameraDepthTexture);
                }

                this.material.SetTexture("GITexture", gi3);

                Graphics.Blit(source, destination, this.material, this.visualizeGI ? Pass.VisualizeGI : Pass.BlendWithScene);

                RenderTexture.ReleaseTemporary(gi3);
                RenderTexture.ReleaseTemporary(gi4);
            }
            else
            {
                if (this.temporalBlendWeight < 1.0f)
                {
                    Graphics.Blit(gi2, gi1, this.material, Pass.TemporalBlend);
                    Graphics.Blit(gi1, this.previousResult);
                    Graphics.Blit(source, this.previousDepth, this.material, Pass.GetCameraDepthTexture);
                }

                this.material.SetTexture("GITexture", this.temporalBlendWeight < 1.0f ? gi1 : gi2);
                Graphics.Blit(source, destination, this.material, this.visualizeGI ? Pass.VisualizeGI : Pass.BlendWithScene);

                RenderTexture.ReleaseTemporary(gi1);
                RenderTexture.ReleaseTemporary(gi2);
            }

            RenderTexture.ReleaseTemporary(currentDepth);
            RenderTexture.ReleaseTemporary(currentNormal);

            if (this.visualizeSunDepthTexture)
                Graphics.Blit(this.sunDepthTexture, destination);


            if (this.doReflections)
            {
                RenderTexture.ReleaseTemporary(reflections);
            }

            this.material.SetMatrix("ProjectionPrev", this.attachedCamera.projectionMatrix);
            this.material.SetMatrix("ProjectionPrevInverse", this.attachedCamera.projectionMatrix.inverse);
            this.material.SetMatrix("WorldToCameraPrev", this.attachedCamera.worldToCameraMatrix);
            this.material.SetMatrix("CameraToWorldPrev", this.attachedCamera.cameraToWorldMatrix);
            this.material.SetVector("CameraPositionPrev", this.attachedCamera.transform.position);

            this.frameSwitch = (this.frameSwitch + 1) % (128);
        }
    }

    public class SEGIPreset
    {
        public string name;
        public SEGI.VoxelResolution voxelResolution = SEGI.VoxelResolution.high;
        public bool voxelAA = false;
        [Range(0, 2)]
        public int innerOcclusionLayers = 1;
        public bool infiniteBounces = true;

        [Range(0.01f, 1.0f)]
        public float temporalBlendWeight = 0.15f;
        public bool useBilateralFiltering = true;
        public bool halfResolution = true;
        public bool stochasticSampling = true;
        public bool doReflections = true;

        [Range(1, 128)]
        public int cones = 13;
        [Range(1, 32)]
        public int coneTraceSteps = 8;
        [Range(0.1f, 2.0f)]
        public float coneLength = 1.0f;
        [Range(0.5f, 6.0f)]
        public float coneWidth = 6.0f;
        [Range(0.0f, 4.0f)]
        public float coneTraceBias = 0.63f;
        [Range(0.0f, 4.0f)]
        public float occlusionStrength = 1.0f;
        [Range(0.0f, 4.0f)]
        public float nearOcclusionStrength = 0.0f;
        [Range(0.001f, 4.0f)]
        public float occlusionPower = 1.0f;
        [Range(0.0f, 4.0f)]
        public float nearLightGain = 1.0f;
        [Range(0.0f, 4.0f)]
        public float giGain = 1.0f;
        [Range(0.0f, 2.0f)]
        public float secondaryBounceGain = 1.0f;
        [Range(12, 128)]
        public int reflectionSteps = 64;
        [Range(0.001f, 4.0f)]
        public float reflectionOcclusionPower = 1.0f;
        [Range(0.0f, 1.0f)]
        public float skyReflectionIntensity = 1.0f;
        public bool gaussianMipFilter = false;

        [Range(0.1f, 4.0f)]
        public float farOcclusionStrength = 1.0f;
        [Range(0.1f, 4.0f)]
        public float farthestOcclusionStrength = 1.0f;

        [Range(3, 16)]
        public int secondaryCones = 6;
        [Range(0.1f, 4.0f)]
        public float secondaryOcclusionStrength = 1.0f;
    }

}
