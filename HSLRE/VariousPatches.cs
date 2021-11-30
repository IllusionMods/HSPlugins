using System;
using System.IO;
using System.Reflection;
using Harmony;
using IllusionPlugin;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;
using UnityStandardAssets.ImageEffects;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using Object = UnityEngine.Object;

namespace HSLRE
{
    internal static class VariousPatches
    {
        [HarmonyPatch(typeof(DepthOfField), "OnEnable")]
        private static class DepthOfField_OnEnable_Patches
        {
            private static void Postfix(DepthOfField __instance)
            {
                __instance.highResolution = true;
            }
        }

        [HarmonyPatch(typeof(CustomTextureControl), "Initialize")]
        private static class CustomTextureControl_Initialize_Patches
        {
            private static void Prefix(ref int width, ref int height)
            {
                width = 4096;
                height = 4096;
            }
        }

        [HarmonyPatch(typeof(CustomTextureControl), "SetMainTexture", typeof(Texture))]
        private static class CustomTextureControl_SetMainTexture_Patches
        {
            private static void Prefix(Texture tex, ref RenderTexture ___createTex)
            {
                if (tex != null && ___createTex != null &&
                    (___createTex.width != tex.width || ___createTex.height != tex.height))
                {
                    int width = tex.width;
                    int height = tex.height;
                    if (Mathf.IsPowerOfTwo(width) == false)
                        width = Mathf.NextPowerOfTwo(width);
                    if (Mathf.IsPowerOfTwo(height) == false)
                        height = Mathf.NextPowerOfTwo(height);
                    width = Mathf.Clamp(width, 4096, 8192);
                    height = Mathf.Clamp(height, 4096, 8192);
                    RenderTexture newRenderTexture = new RenderTexture(width, height, 0);
                    ___createTex.Release();
                    ___createTex = newRenderTexture;
                }
            }
        }

        [HarmonyPatch(typeof(CustomTextureControl), "SetOffsetAndTiling", typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float))]
        private static class CustomTextureControl_SetOffsetAndTiling_Patches
        {
            private static void Prefix(string propertyName, ref int baseW, ref int baseH, int addW, int addH, ref float addPx, ref float addPy)
            {
                if (addPx >= 0f & addPy >= 0f & (float)addW + addPx <= 1024f & (float)addH + addPy <= 1024f)
                {
                    baseW = 1024;
                    baseH = 1024;
                }
                else
                {
                    addPx = Mathf.Abs(addPx);
                    addPy = Mathf.Abs(addPy);
                    if (addW > 4096f || addH > 4096f)
                    {
                        baseH = 8192;
                        baseW = 8192;
                    }
                    else
                    {
                        baseH = 4096;
                        baseW = 4096;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ColorCorrectionCurves), "UpdateParameters")]
        private static class ColorCorrectionCurves_UpdateParameters_Patches
        {
            private static bool Prefix(ColorCorrectionCurves __instance, ref Texture2D ___rgbChannelTex)
            {
                __instance.CheckResources();
                if (Settings.curveSettings == null ||
                    Settings.curveSettings.Curve == 0 ||
                    File.Exists(Path.Combine(Application.dataPath, "../UserData/curve/" + Settings.curveSettings.CurveName + ".dds")) == false)
                {
                    if (___rgbChannelTex.width != 2048)
                    {
                        Object.Destroy(___rgbChannelTex);
                        ___rgbChannelTex = new Texture2D(2048, 4, TextureFormat.RGBAFloat, false, true);
                        ___rgbChannelTex.wrapMode = TextureWrapMode.Clamp;
                        ___rgbChannelTex.hideFlags = HideFlags.DontSave;
                    }


                    for (int i = 0; i < 2048; i++)
                    {
                        float num = i / 2047f;
                        float num2 = Mathf.Clamp(__instance.redChannel.Evaluate(num), 0f, 1f);
                        float num3 = Mathf.Clamp(__instance.greenChannel.Evaluate(num), 0f, 1f);
                        float num4 = Mathf.Clamp(__instance.blueChannel.Evaluate(num), 0f, 1f);
                        ___rgbChannelTex.SetPixel(i, 0, new Color(num2, num2, num2));
                        ___rgbChannelTex.SetPixel(i, 1, new Color(num3, num3, num3));
                        ___rgbChannelTex.SetPixel(i, 2, new Color(num4, num4, num4));

                    }
                    ___rgbChannelTex.Apply(false);
                }
                else
                {
                    if (___rgbChannelTex.width != 1024)
                    {
                        Object.Destroy(___rgbChannelTex);
                        ___rgbChannelTex = new Texture2D(1024, 4, TextureFormat.RGBAFloat, false, true);
                        ___rgbChannelTex.wrapMode = TextureWrapMode.Clamp;
                        ___rgbChannelTex.hideFlags = HideFlags.DontSave;
                    }

                    string text = Path.Combine(Application.dataPath, "../UserData/curve/" + Settings.curveSettings.CurveName + ".dds");
                    UnityEngine.Debug.Log("Using custom curve " + text);
                    byte[] array = File.ReadAllBytes(text);
                    int num5 = 128;
                    byte[] array2 = new byte[array.Length - num5];
                    Buffer.BlockCopy(array, num5, array2, 0, array2.Length - num5);
                    ___rgbChannelTex.LoadRawTextureData(array2);
                    ___rgbChannelTex.Apply(false);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(NoiseAndGrain), "CheckResources")]
        private static class NoiseAndGrain_CheckResources_Patches
        {
            private static void Prefix(NoiseAndGrain __instance)
            {
                if (__instance.noiseShader == null)
                    __instance.noiseShader = HSLRE.self._resources.LoadAsset<Shader>("NoiseAndGrain");
                if (__instance.dx11NoiseShader == null)
                    __instance.dx11NoiseShader = HSLRE.self._resources.LoadAsset<Shader>("NoiseAndGrainDX11");
                if (__instance.noiseTexture == null)
                    __instance.noiseTexture = HSLRE.self._resources.LoadAsset<Texture2D>("NoiseAndGrain");
            }
        }

        [HarmonyPatch(typeof(BlurOptimized), "CheckResources")]
        private static class BlurOptimized_CheckResources_Patches
        {
            private static void Prefix(BlurOptimized __instance)
            {
                if (__instance.blurShader == null)
                    __instance.blurShader = HSLRE.self._resources.LoadAsset<Shader>("MobileBlur");
            }
        }

        [HarmonyPatch(typeof(CameraMotionBlur), "CheckResources")]
        private static class CameraMotionBlur_CheckResources_Patches
        {
            private static void Prefix(CameraMotionBlur __instance)
            {
                if (__instance.shader == null)
                    __instance.shader = HSLRE.self._resources.LoadAsset<Shader>("CameraMotionBlur");
                if (__instance.replacementClear == null)
                    __instance.replacementClear = HSLRE.self._resources.LoadAsset<Shader>("MotionBlurClear");
                if (__instance.dx11MotionBlurShader == null)
                    __instance.dx11MotionBlurShader = HSLRE.self._resources.LoadAsset<Shader>("CameraMotionBlurDX11");
                if (__instance.noiseTexture == null)
                    __instance.noiseTexture = HSLRE.self._resources.LoadAsset<Texture2D>("MotionBlurJitter");
            }
        }

        private static class ScrenshotFix
        {
            private static float _dofMaxBlurSize;
            private static float _noiseAndGrainSoftness;
            private static float _blurBlurSize;
            //private static float _lensGlareStreakScale;
            //private static float _lensFlareGaussianBlurRadius;
            //private static float _amplifyBloomBloomScale;
            //private static float _lensGlareIntensity;
            //private static float _lensFlareGhostChrDistortion;
            //private static float _lensFlareHaloChrDistortion;
            private static Vector2 _amplifyBloomScreenshotMultiplier;
            private static int _ssrReflectionSettingsMaxSteps;

            internal static void PreScreenshot(Vector2 multiplier, bool fixDof = true)
            {
                if (HSLRE.self.dof != null)
                {
                    _dofMaxBlurSize = HSLRE.self.dof.maxBlurSize;
                    if (HSLRE.self.fixDofForUpscaledScreenshots && fixDof)
                        HSLRE.self.dof.maxBlurSize *= multiplier.x;
                }
                if (HSLRE.self.noiseAndGrain != null)
                {
                    _noiseAndGrainSoftness = HSLRE.self.noiseAndGrain.softness;
                    if (HSLRE.self.fixNoiseAndGrainForUpscaledScreenshots)
                        HSLRE.self.noiseAndGrain.softness = (HSLRE.self.noiseAndGrain.softness + multiplier.x - 1) / multiplier.x;
                }
                if (HSLRE.self.blur != null)
                {
                    _blurBlurSize = HSLRE.self.blur.blurSize;
                    if (HSLRE.self.fixBlurForUpscaledScreenshots)
                        HSLRE.self.blur.blurSize *= multiplier.x;
                }
                if (HSLRE.self.amplifyBloom != null)
                {
                    _amplifyBloomScreenshotMultiplier = HSLRE.self.amplifyBloom.ScreenshotMultiplier;
                    if (HSLRE.self.fixAmplifyBloomForUpscaledScreenshots)
                    {
                        HSLRE.self.amplifyBloom.ScreenshotMultiplier = new Vector2(HSLRE.self.amplifyBloom.ScreenshotMultiplier.x * multiplier.x, HSLRE.self.amplifyBloom.ScreenshotMultiplier.y * multiplier.y);
                        //HSLRE.self.amplifyBloom.UpscaleBlurRadius *= multiplier;
                        //HSLRE.self.amplifyBloom.LensGlareInstance.Intensity = (float)(HSLRE.self.amplifyBloom.LensGlareInstance.Intensity * Math.Pow(multiplier, -0.1699250014));
                        //HSLRE.self.amplifyBloom.LensGlareInstance.OverallStreakScale *= multiplier;
                        //HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGaussianBlurRadius *= multiplier;
                        //HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion *= multiplier;
                        //HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion *= multiplier;
                    }
                } //_amplifyBloomBloomScale = HSLRE.self.amplifyBloom.UpscaleBlurRadius;
                //_lensGlareIntensity = HSLRE.self.amplifyBloom.LensGlareInstance.Intensity;
                //_lensGlareStreakScale = HSLRE.self.amplifyBloom.LensGlareInstance.OverallStreakScale;
                //_lensFlareGaussianBlurRadius = HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGaussianBlurRadius;
                //_lensFlareGhostChrDistortion = HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion;
                //_lensFlareHaloChrDistortion = HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion;
                if (HSLRE.self.ssr != null)
                {
                    _ssrReflectionSettingsMaxSteps = HSLRE.self.ssr.settings.reflectionSettings.maxSteps;
                    if (HSLRE.self.fixSsrForUpscaledScreenshots)
                    {
                        ScreenSpaceReflection.ReflectionSettings settings = HSLRE.self.ssr.settings.reflectionSettings;
                        settings.maxSteps = (int)(settings.maxSteps * multiplier.x);
                        HSLRE.self.ssr.settings.reflectionSettings = settings;
                    }
                }
                if (HSLRE.self.segi != null)
                    HSLRE.self.segi.halfResolution = false;
            }

            internal static void PostScreenshot()
            {
                if (HSLRE.self.dof != null)
                    HSLRE.self.dof.maxBlurSize = _dofMaxBlurSize;
                if (HSLRE.self.noiseAndGrain != null)
                    HSLRE.self.noiseAndGrain.softness = _noiseAndGrainSoftness;
                if (HSLRE.self.blur != null)
                    HSLRE.self.blur.blurSize = _blurBlurSize;
                if (HSLRE.self.amplifyBloom != null)
                    HSLRE.self.amplifyBloom.ScreenshotMultiplier = _amplifyBloomScreenshotMultiplier;
                if (HSLRE.self.ssr != null)
                {
                    ScreenSpaceReflection.ReflectionSettings settings = HSLRE.self.ssr.settings.reflectionSettings;
                    settings.maxSteps = _ssrReflectionSettingsMaxSteps;
                    HSLRE.self.ssr.settings.reflectionSettings = settings;
                }
                if (HSLRE.self.segi != null)
                    HSLRE.self.segi.halfResolution = true;
                //HSLRE.self.amplifyBloom.UpscaleBlurRadius = _amplifyBloomBloomScale;
                //HSLRE.self.amplifyBloom.LensGlareInstance.Intensity = _lensGlareIntensity;
                //HSLRE.self.amplifyBloom.LensGlareInstance.OverallStreakScale = _lensGlareStreakScale;
                //HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGaussianBlurRadius = _lensFlareGaussianBlurRadius;
                //HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion = _lensFlareGhostChrDistortion;
                //HSLRE.self.amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion = _lensFlareHaloChrDistortion;
            }
        }

        [HarmonyPatch]
        private static class HoneyShotPlugin_CaptureUsingCameras_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("HoneyShot.HoneyShotPlugin,HoneyShot") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("HoneyShot.HoneyShotPlugin,HoneyShot").GetMethod("CaptureUsingCameras", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            private static void Prefix()
            {
                ScrenshotFix.PreScreenshot(new Vector2(ModPrefs.GetInt("HoneyShot", "width", 0, false) / (float)Screen.width, ModPrefs.GetInt("HoneyShot", "height", 0, false) / (float)Screen.height));
            }

            private static void Postfix()
            {
                ScrenshotFix.PostScreenshot();
            }
        }

        [HarmonyPatch]
        private static class ScreenShot_myScreenShot_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("ScreenShot,PlayShot24ZHNeo") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("ScreenShot,PlayShot24ZHNeo").GetMethod("myScreenShot", BindingFlags.Public | BindingFlags.Instance);
            }

            private static void Prefix(int size)
            {
                ScrenshotFix.PreScreenshot(new Vector2(size, size));
            }

            private static void Postfix()
            {
                ScrenshotFix.PostScreenshot();
            }
        }

        [HarmonyPatch]
        private static class ScreenShot_myScreenShotTransparent_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("ScreenShot,PlayShot24ZHNeo") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("ScreenShot,PlayShot24ZHNeo").GetMethod("myScreenShotTransparent", BindingFlags.Public | BindingFlags.Instance);
            }

            [HarmonyBefore("com.joan6694.illusionplugins.videoexport")]
            private static void Prefix(int size)
            {
                ScrenshotFix.PreScreenshot(new Vector2(size, size));
            }

            [HarmonyAfter("com.joan6694.illusionplugins.videoexport")]
            private static void Postfix()
            {
                ScrenshotFix.PostScreenshot();
            }
        }

        [HarmonyPatch]
        private static class ScreencapMB_CaptureOpaque_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("ScreencapMB,Screencap") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("ScreencapMB,Screencap").GetMethod("CaptureOpaque", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            private static void Prefix(int ___Width, int ___Height)
            {
                ScrenshotFix.PreScreenshot(new Vector2(___Width / (float)Screen.width, ___Height / (float)Screen.height));
            }

            private static void Postfix()
            {
                ScrenshotFix.PostScreenshot();
            }
        }

        [HarmonyPatch]
        private static class VideoExport_Bitmap_OnStartRecording_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("VideoExport.ScreenshotPlugins.Bitmap,VideoExport") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("VideoExport.ScreenshotPlugins.Bitmap,VideoExport").GetMethod("OnStartRecording", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }

            private static void Prefix(object __instance)
            {
	            Vector2 currentSize = (Vector2)__instance.GetType().GetProperty("currentSize", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(__instance, null);
                ScrenshotFix.PreScreenshot(new Vector2(currentSize.x / Screen.width, currentSize.y / Screen.height));
            }
        }

        [HarmonyPatch]
        private static class VideoExport_Bitmap_OnEndRecording_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("VideoExport.ScreenshotPlugins.Bitmap,VideoExport") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("VideoExport.ScreenshotPlugins.Bitmap,VideoExport").GetMethod("OnEndRecording", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }

            private static void Postfix()
            {
                ScrenshotFix.PostScreenshot();
            }
        }

    }
}
