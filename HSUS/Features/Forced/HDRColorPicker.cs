
using ToolBox.Extensions;
#if HONEYSELECT
using System;
using System.Reflection;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using Studio;
using ToolBox;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS.Features
{
    public static class HDRColorPicker
    {
        [HarmonyPatch(typeof(UI_ColorInfo), "ConvertTextFromValue", new[] {typeof(UI_ColorInfo.ColorType), typeof(float)})]
        internal static class UI_ColorInfo_ConvertTextFromValue_Patches
        {
            private static bool Prefix(UI_ColorInfo.ColorType type, float value, ref string __result)
            {
                int num = 0;
                switch (type)
                {
                    case UI_ColorInfo.ColorType.Hue:
                        num = (int)Mathf.Lerp(0f, 360f, value);
                        break;
                    case UI_ColorInfo.ColorType.Value:
                        num = (int)Mathf.LerpUnclamped(0f, 100f, value);
                        break;
                    case UI_ColorInfo.ColorType.Saturation:
                    case UI_ColorInfo.ColorType.Alpha:
                        num = (int)Mathf.Lerp(0f, 100f, value);
                        break;
                    case UI_ColorInfo.ColorType.Red:
                    case UI_ColorInfo.ColorType.Green:
                    case UI_ColorInfo.ColorType.Blue:
                        num = (int)Mathf.LerpUnclamped(0f, 255f, value);
                        break;
                }
                __result = num.ToString();
                return false;
            }
        }

        [HarmonyPatch]
        internal static class UI_ColorInfo_ConvertValueFromText_Patches
        {
            private static MethodInfo TargetMethod()
            {
                return typeof(UI_ColorInfo).GetMethod("ConvertValueFromText", BindingFlags.Public | BindingFlags.Instance);
            }

            private static bool Prefix(UI_ColorInfo.ColorType type, string buf, ref bool OutOfRange, ref float __result)
            {
                OutOfRange = false;
                int num = 0;
                if (string.Empty != buf)
                {
                    num = int.Parse(buf);
                }
                __result = 0;
                switch (type)
                {
                    case UI_ColorInfo.ColorType.Hue:
                        if (!MathfEx.RangeEqualOn(0, num, 360))
                        {
                            OutOfRange = true;
                        }
                        num = Mathf.Clamp(num, 0, 360);
                        __result = num / 360f;
                        break;
                    case UI_ColorInfo.ColorType.Value:
                        if (num < 0)
                            num = 0;
                        __result = num / 100f;
                        break;
                    case UI_ColorInfo.ColorType.Saturation:
                    case UI_ColorInfo.ColorType.Alpha:
                        if (!MathfEx.RangeEqualOn(0, num, 100))
                        {
                            OutOfRange = true;
                        }
                        num = Mathf.Clamp(num, 0, 100);
                        __result = num / 100f;
                        break;
                    case UI_ColorInfo.ColorType.Red:
                    case UI_ColorInfo.ColorType.Green:
                    case UI_ColorInfo.ColorType.Blue:
                        if (num < 0)
                            num = 0;
                        __result = num / 255f;
                        break;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(UI_ColorSlider), "ChangeSliderSliderPos", new[] {typeof(int)})]
        internal static class UI_ColorSlider_ChangeSliderSliderPos_Patches
        {
            private static MethodInfo _getRateHSV;
            private static MethodInfo _getRateRGB;

            private static bool Prefix(UI_ColorSlider __instance, int index)
            {
                if (_getRateHSV == null)
                    _getRateHSV = __instance.GetType().GetMethod("GetRateHSV", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (_getRateRGB == null)
                    _getRateRGB = __instance.GetType().GetMethod("GetRateRGB", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (!MathfEx.RangeEqualOn(0, index, 3) || null == __instance.sliderSlider[index])
                {
                    return false;
                }
                if (index == 3)
                {
                    __instance.sliderSlider[3].value = __instance.GetColorAlpha();
                }
                else
                {
                    switch (__instance.sliderMode)
                    {
                        case UI_ColorSlider.SliderMode.ModeHSV:
                            float[] rateHSV = (float[])_getRateHSV.Invoke(__instance, new object[] { });
                            Slider slider = __instance.sliderSlider[index];
                            if (rateHSV[index] > slider.maxValue)
                                slider.maxValue = rateHSV[index];
                            slider.value = rateHSV[index];
                            break;
                        case UI_ColorSlider.SliderMode.ModeRGB:
                            float[] rateRGB = (float[])_getRateRGB.Invoke(__instance, new object[] { });
                            slider = __instance.sliderSlider[index];
                            if (rateRGB[index] > slider.maxValue)
                                slider.maxValue = rateRGB[index];
                            __instance.sliderSlider[index].value = rateRGB[index];
                            break;
                    }
                }
                return false;
            }
        }

        [HarmonyPatch]
        internal static class HsvColor_Ctor_Patches
        {
            internal static MethodBase TargetMethod()
            {
                return typeof(HsvColor).GetConstructor(new[] {typeof(float), typeof(float), typeof(float)});
            }

            internal static bool Prefix(HsvColor __instance, float hue, float saturation, float brightness)
            {
                if (hue < 0f || 360f < hue)
                {
                    throw new ArgumentException("hueは0~360の値です。", nameof(hue));
                }
                if (saturation < 0f || 1f < saturation)
                {
                    throw new ArgumentException("saturationは0以上1以下の値です。", nameof(saturation));
                }
                if (brightness < 0f)
                {
                    throw new ArgumentException("brightnessは0以上1以下の値です。", nameof(brightness));
                }
                __instance.H = hue;
                __instance.S = saturation;
                __instance.V = brightness;
                return false;
            }
        }


        [HarmonyPatch(typeof(UI_ColorSlider), "ChangeSliderMode", new Type[] { })]
        internal static class UI_ColorSlider_ChangeSliderMode_Patches
        {
            private static void Prefix(UI_ColorSlider __instance)
            {
                Slider.SliderEvent dummyEvent = new Slider.SliderEvent();
                foreach (Slider slider in __instance.sliderSlider)
                {
                    Slider.SliderEvent cachedEvent = slider.onValueChanged;
                    slider.onValueChanged = dummyEvent;
                    slider.maxValue = 1f;
                    slider.onValueChanged = cachedEvent;
                }
            }
        }

        [HarmonyPatch(typeof(UI_ColorSlider), "Init")]
        internal static class UI_ColorSlider_Init_Patches
        {
            private static void Postfix(UI_ColorSlider __instance)
            {
                foreach (InputField inputField in __instance.inputSlider)
                {
                    inputField.characterLimit = 0;
                }
            }
        }

        [HarmonyPatch(typeof(ColorPaletteCtrl), "set_visible", new[] {typeof(bool)})]
        internal static class ColorPaletteCtrl_set_visible_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static void Postfix(ColorPaletteCtrl __instance, bool value, UI_ColorMenu ___colorMenu)
            {
                if (value)
                {
                    __instance.ExecuteDelayed(() =>
                    {
                        Slider.SliderEvent dummyEvent = new Slider.SliderEvent();
                        foreach (Slider slider in ___colorMenu.sliderSlider)
                        {
                            Slider.SliderEvent cachedEvent = slider.onValueChanged;
                            slider.onValueChanged = dummyEvent;
                            slider.maxValue = 1f;
                            slider.onValueChanged = cachedEvent;
                        }
                        ___colorMenu.ChangeSliderMode();
                    });
                }
            }
        }
    }
}
#endif