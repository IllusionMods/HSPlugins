using System;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;

namespace HSLRE.CustomEffects
{
    [Serializable]
    public class SMAA
    {
        public Shader shader
        {
            get
            {
                if (this.m_Shader == null)
                {
                    this.m_Shader = HSLRE.self._resources.LoadAsset<Shader>("SMAA");
                }
                return this.m_Shader;
            }
        }

        private Texture2D areaTexture
        {
            get
            {
                if (this.m_AreaTexture == null)
                {
                    this.m_AreaTexture = HSLRE.self._resources.LoadAsset<Texture2D>("AreaTex");
                }
                return this.m_AreaTexture;
            }
        }

        private Texture2D searchTexture
        {
            get
            {
                if (this.m_SearchTexture == null)
                {
                    this.m_SearchTexture = HSLRE.self._resources.LoadAsset<Texture2D>("SearchTex");
                }
                return this.m_SearchTexture;
            }
        }

        private Material material
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
        }

        public void OnDisable()
        {
            if (this.m_Material != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_Material);
            }
            if (this.m_Accumulation != null)
            {
                UnityEngine.Object.DestroyImmediate(this.m_Accumulation);
            }
            this.m_Material = null;
            this.m_Accumulation = null;
        }

        public void OnPreCull()
        {
            if (this.temporal.UseTemporal())
            {
                this.m_ProjectionMatrix = Camera.current.projectionMatrix;
                this.m_FlipFlop -= 2f * this.m_FlipFlop;
                Matrix4x4 identity = Matrix4x4.identity;
                identity.m03 = 0.25f * this.m_FlipFlop * this.temporal.fuzzSize / Camera.current.pixelWidth;
                identity.m13 = -0.25f * this.m_FlipFlop * this.temporal.fuzzSize / Camera.current.pixelHeight;
                Camera.current.projectionMatrix = identity * Camera.current.projectionMatrix;
            }
        }

        public void OnPostRender()
        {
            if (this.temporal.UseTemporal())
            {
                Camera.current.ResetProjectionMatrix();
            }
        }

        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            int pixelWidth = Camera.current.pixelWidth;
            int pixelHeight = Camera.current.pixelHeight;
            bool flag = false;
            QualitySettings qualitySettings = this.quality;
            if (this.settings.quality != QualityPreset.Custom)
            {
                qualitySettings = QualitySettings.presetQualitySettings[(int)this.settings.quality];
            }
            int edgeDetectionMethod = (int)this.settings.edgeDetectionMethod;
            int pass = 4;
            int pass2 = 5;
            int pass3 = 6;
            Matrix4x4 matrix4x = GL.GetGPUProjectionMatrix(this.m_ProjectionMatrix, true) * Camera.current.worldToCameraMatrix;
            this.material.SetTexture("_AreaTex", this.areaTexture);
            this.material.SetTexture("_SearchTex", this.searchTexture);
            this.material.SetVector("_Metrics", new Vector4(1f / pixelWidth, 1f / pixelHeight, pixelWidth, pixelHeight));
            this.material.SetVector("_Params1", new Vector4(qualitySettings.threshold, qualitySettings.depthThreshold, qualitySettings.maxSearchSteps, qualitySettings.maxDiagonalSearchSteps));
            this.material.SetVector("_Params2", new Vector2(qualitySettings.cornerRounding, qualitySettings.localContrastAdaptationFactor));
            this.material.SetMatrix("_ReprojectionMatrix", this.m_PreviousViewProjectionMatrix * Matrix4x4.Inverse(matrix4x));
            float num = (this.m_FlipFlop < 0f) ? 2f : 1f;
            this.material.SetVector("_SubsampleIndices", new Vector4(num, num, num, 0f));
            Shader.DisableKeyword("USE_PREDICATION");
            if (this.settings.edgeDetectionMethod == EdgeDetectionMethod.Depth)
            {
                Camera.current.depthTextureMode |= DepthTextureMode.Depth;
            }
            else if (this.predication.enabled)
            {
                Camera.current.depthTextureMode |= DepthTextureMode.Depth;
                Shader.EnableKeyword("USE_PREDICATION");
                this.material.SetVector("_Params3", new Vector3(this.predication.threshold, this.predication.scale, this.predication.strength));
            }
            Shader.DisableKeyword("USE_DIAG_SEARCH");
            Shader.DisableKeyword("USE_CORNER_DETECTION");
            if (qualitySettings.diagonalDetection)
            {
                Shader.EnableKeyword("USE_DIAG_SEARCH");
            }
            if (qualitySettings.cornerDetection)
            {
                Shader.EnableKeyword("USE_CORNER_DETECTION");
            }
            Shader.DisableKeyword("USE_UV_BASED_REPROJECTION");
            if (this.temporal.UseTemporal())
            {
                Shader.EnableKeyword("USE_UV_BASED_REPROJECTION");
            }
            if (this.m_Accumulation == null || this.m_Accumulation.width != pixelWidth || this.m_Accumulation.height != pixelHeight)
            {
                if (this.m_Accumulation)
                {
                    RenderTexture.ReleaseTemporary(this.m_Accumulation);
                }
                this.m_Accumulation = RenderTexture.GetTemporary(pixelWidth, pixelHeight, 0, source.format, RenderTextureReadWrite.Linear);
                this.m_Accumulation.hideFlags = HideFlags.HideAndDontSave;
                flag = true;
            }
            RenderTexture renderTexture = this.TempRT(pixelWidth, pixelHeight, source.format);
            Graphics.Blit(null, renderTexture, this.material, 0);
            Graphics.Blit(source, renderTexture, this.material, edgeDetectionMethod);
            if (this.settings.debugPass == DebugPass.Edges)
            {
                Graphics.Blit(renderTexture, destination);
            }
            else
            {
                RenderTexture renderTexture2 = this.TempRT(pixelWidth, pixelHeight, source.format);
                Graphics.Blit(null, renderTexture2, this.material, 0);
                Graphics.Blit(renderTexture, renderTexture2, this.material, pass);
                if (this.settings.debugPass == DebugPass.Weights)
                {
                    Graphics.Blit(renderTexture2, destination);
                }
                else
                {
                    this.material.SetTexture("_BlendTex", renderTexture2);
                    if (this.temporal.UseTemporal())
                    {
                        Graphics.Blit(source, renderTexture, this.material, pass2);
                        if (this.settings.debugPass == DebugPass.Accumulation)
                        {
                            Graphics.Blit(this.m_Accumulation, destination);
                        }
                        else if (!flag)
                        {
                            this.material.SetTexture("_AccumulationTex", this.m_Accumulation);
                            Graphics.Blit(renderTexture, destination, this.material, pass3);
                        }
                        else
                        {
                            Graphics.Blit(renderTexture, destination);
                        }
                        Graphics.Blit(destination, this.m_Accumulation);
                        RenderTexture.active = null;
                    }
                    else
                    {
                        Graphics.Blit(source, destination, this.material, pass2);
                    }
                }
                RenderTexture.ReleaseTemporary(renderTexture2);
            }
            RenderTexture.ReleaseTemporary(renderTexture);
            this.m_PreviousViewProjectionMatrix = matrix4x;
        }

        private RenderTexture TempRT(int width, int height, RenderTextureFormat format)
        {
            int depthBuffer = 0;
            return RenderTexture.GetTemporary(width, height, depthBuffer, format, RenderTextureReadWrite.Linear);
        }

        [TopLevelSettings]
        public GlobalSettings settings = GlobalSettings.defaultSettings;

        [SettingsGroup]
        public QualitySettings quality = QualitySettings.presetQualitySettings[2];

        [SettingsGroup]
        public PredicationSettings predication = PredicationSettings.defaultSettings;

        [SettingsGroup]
        [ExperimentalGroup]
        public TemporalSettings temporal = TemporalSettings.defaultSettings;

        private Matrix4x4 m_ProjectionMatrix;

        private Matrix4x4 m_PreviousViewProjectionMatrix;

        private float m_FlipFlop = 1f;

        private RenderTexture m_Accumulation;

        private Shader m_Shader;

        private Texture2D m_AreaTexture;

        private Texture2D m_SearchTexture;

        private Material m_Material;

        [AttributeUsage(AttributeTargets.Field)]
        public class SettingsGroup : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class TopLevelSettings : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class ExperimentalGroup : Attribute
        {
        }

        public enum DebugPass
        {
            Off,
            Edges,
            Weights,
            Accumulation
        }

        public enum QualityPreset
        {
            Low,
            Medium,
            High,
            Ultra,
            Custom
        }

        public enum EdgeDetectionMethod
        {
            Luma = 1,
            Color,
            Depth
        }

        [Serializable]
        public struct GlobalSettings
        {
            public static GlobalSettings defaultSettings
            {
                get
                {
                    return new GlobalSettings
                    {
                        debugPass = DebugPass.Off,
                        quality = QualityPreset.High,
                        edgeDetectionMethod = EdgeDetectionMethod.Color
                    };
                }
            }

            [Tooltip("Use this to fine tune your settings when working in Custom quality mode. \"Accumulation\" only works when \"Temporal Filtering\" is enabled.")]
            public DebugPass debugPass;

            [Tooltip("Low: 60% of the quality.\nMedium: 80% of the quality.\nHigh: 95% of the quality.\nUltra: 99% of the quality (overkill).")]
            public QualityPreset quality;

            [Tooltip("You've three edge detection methods to choose from: luma, color or depth.\nThey represent different quality/performance and anti-aliasing/sharpness tradeoffs, so our recommendation is for you to choose the one that best suits your particular scenario:\n\n- Depth edge detection is usually the fastest but it may miss some edges.\n- Luma edge detection is usually more expensive than depth edge detection, but catches visible edges that depth edge detection can miss.\n- Color edge detection is usually the most expensive one but catches chroma-only edges.")]
            public EdgeDetectionMethod edgeDetectionMethod;
        }

        [Serializable]
        public struct QualitySettings
        {
            [Tooltip("Enables/Disables diagonal processing.")]
            public bool diagonalDetection;

            [Tooltip("Enables/Disables corner detection. Leave this on to avoid blurry corners.")]
            public bool cornerDetection;

            [Range(0f, 0.5f)]
            [Tooltip("Specifies the threshold or sensitivity to edges. Lowering this value you will be able to detect more edges at the expense of performance.\n0.1 is a reasonable value, and allows to catch most visible edges. 0.05 is a rather overkill value, that allows to catch 'em all.")]
            public float threshold;

            [Min(0.0001f)]
            [Tooltip("Specifies the threshold for depth edge detection. Lowering this value you will be able to detect more edges at the expense of performance.")]
            public float depthThreshold;

            [Range(0f, 112f)]
            [Tooltip("Specifies the maximum steps performed in the horizontal/vertical pattern searches, at each side of the pixel.\nIn number of pixels, it's actually the double. So the maximum line length perfectly handled by, for example 16, is 64 (by perfectly, we meant that longer lines won't look as good, but still antialiased).")]
            public int maxSearchSteps;

            [Range(0f, 20f)]
            [Tooltip("Specifies the maximum steps performed in the diagonal pattern searches, at each side of the pixel. In this case we jump one pixel at time, instead of two.\nOn high-end machines it is cheap (between a 0.8x and 0.9x slower for 16 steps), but it can have a significant impact on older machines.")]
            public int maxDiagonalSearchSteps;

            [Range(0f, 100f)]
            [Tooltip("Specifies how much sharp corners will be rounded.")]
            public int cornerRounding;

            [Min(0f)]
            [Tooltip("If there is an neighbor edge that has a local contrast factor times bigger contrast than current edge, current edge will be discarded.\nThis allows to eliminate spurious crossing edges, and is based on the fact that, if there is too much contrast in a direction, that will hide perceptually contrast in the other neighbors.")]
            public float localContrastAdaptationFactor;

            public static QualitySettings[] presetQualitySettings = new QualitySettings[]
            {
                new QualitySettings
                {
                    diagonalDetection = false,
                    cornerDetection = false,
                    threshold = 0.15f,
                    depthThreshold = 0.01f,
                    maxSearchSteps = 4,
                    maxDiagonalSearchSteps = 8,
                    cornerRounding = 25,
                    localContrastAdaptationFactor = 2f
                },
                new QualitySettings
                {
                    diagonalDetection = false,
                    cornerDetection = false,
                    threshold = 0.1f,
                    depthThreshold = 0.01f,
                    maxSearchSteps = 8,
                    maxDiagonalSearchSteps = 8,
                    cornerRounding = 25,
                    localContrastAdaptationFactor = 2f
                },
                new QualitySettings
                {
                    diagonalDetection = true,
                    cornerDetection = true,
                    threshold = 0.1f,
                    depthThreshold = 0.01f,
                    maxSearchSteps = 16,
                    maxDiagonalSearchSteps = 8,
                    cornerRounding = 25,
                    localContrastAdaptationFactor = 2f
                },
                new QualitySettings
                {
                    diagonalDetection = true,
                    cornerDetection = true,
                    threshold = 0.05f,
                    depthThreshold = 0.01f,
                    maxSearchSteps = 32,
                    maxDiagonalSearchSteps = 16,
                    cornerRounding = 25,
                    localContrastAdaptationFactor = 2f
                }
            };
        }

        [Serializable]
        public struct TemporalSettings
        {
            public bool UseTemporal()
            {
                return this.enabled;
            }

            public static TemporalSettings defaultSettings
            {
                get
                {
                    return new TemporalSettings
                    {
                        enabled = false,
                        fuzzSize = 2f
                    };
                }
            }

            [Tooltip("Temporal filtering makes it possible for the SMAA algorithm to benefit from minute subpixel information available that has been accumulated over many frames.")]
            public bool enabled;

            [Range(0.5f, 10f)]
            [Tooltip("The size of the fuzz-displacement (jitter) in pixels applied to the camera's perspective projection matrix.\nUsed for 2x temporal anti-aliasing.")]
            public float fuzzSize;
        }

        [Serializable]
        public struct PredicationSettings
        {
            public static PredicationSettings defaultSettings
            {
                get
                {
                    return new PredicationSettings
                    {
                        enabled = false,
                        threshold = 0.01f,
                        scale = 2f,
                        strength = 0.4f
                    };
                }
            }

            [Tooltip("Predicated thresholding allows to better preserve texture details and to improve performance, by decreasing the number of detected edges using an additional buffer (the detph buffer).\nIt locally decreases the luma or color threshold if an edge is found in an additional buffer (so the global threshold can be higher).")]
            public bool enabled;

            [Min(0.0001f)]
            [Tooltip("Threshold to be used in the additional predication buffer.")]
            public float threshold;

            [Range(1f, 5f)]
            [Tooltip("How much to scale the global threshold used for luma or color edge detection when using predication.")]
            public float scale;

            [Range(0f, 1f)]
            [Tooltip("How much to locally decrease the threshold.")]
            public float strength;
        }
    }
}
