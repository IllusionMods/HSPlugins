using UnityEngine;

namespace HSIBL
{
    public class GUIStrings : Config.BaseSystem
    {
        public GUIStrings(string elementName):base(elementName) { }
        //public override void Init()
        //{
        //    switch (Application.systemLanguage)
        //    {
        //        case SystemLanguage.ChineseTraditional:
        //        case SystemLanguage.ChineseSimplified:
        //        case SystemLanguage.Chinese:
        //            Reset ="重置";
        //            vsync ="垂直同步";
        //            vsynctooltips ="重启游戏后生效";
        //            disable_vs_enable_0 ="禁用";
        //            disable_vs_enable_1 ="启用";
        //            Dolly_zoom ="滑动变焦";
        //            Depth_Of_View ="景深";
        //            Field_Of_View ="视场";
        //            Vignette ="渐晕";
        //            Filters ="滤镜";
        //            Chromatic_Aberration ="色差";
        //            Global_Settings ="全局设定";
        //            auto_refresh ="自动刷新";
        //            async_load ="异步读取";
        //            async_load_tootips ="开启后可以消除 Cubemap 载入时的卡顿，但会增加载入时间。";
        //            force_deferred ="强制延期着色";
        //            auto_setting ="自动最佳设置";
        //            auto_refresh_tooltips ="搜索所有的蒙皮网格渲染器并对其使用反射探针, 如果延期着色功能开启也会搜索并替换所有的皮肤着色器。可能会影响性能。";
        //            force_deferred_tooltips ="自动进入延期着色";
        //            auto_setting_tooltips ="场景切换时自动载入最佳设置。会自动禁用 DHH 画质调整模块";
        //            Tessellation ="曲面细分";
        //            Tessellation_save ="保存";
        //            Skin_Scattering ="皮肤散射";
        //            Skin_Transmission ="皮肤透射";
        //            Custom_Window ="自定义窗口";
        //            Custom_Window_Remember ="记住大小与位置";
        //            Optimal_Settings ="载入最佳设置";
        //            deferred_shading ="开启延期着色";
        //            deferred_shading_tooltips ="实验性功能。一旦开启直到场景切换前不能关闭。";
        //            Dithering ="颜色抖动";
        //            Dithering_Tooltips ="故意增加噪点使量化误差随机化，有助于防止图像出现大面积色带。";
        //            Reflection ="反射";
        //            Directional_Light ="平行光";
        //            refresh ="刷新";
        //            eye_adaptation ="自动曝光";
        //            eye_adaptation_tooltips ="根据图像亮度范围自动调整曝光值";
        //            Exposure_Value ="曝光值：(EV)";
        //            Exposure_Value_Tooltips ="以EV为单位的曝光值调整";
        //            tonemapping ="色调映射";
        //            tonemapping_tooltips ="把高动态范围的图像映射到适合屏幕显示的范围内";
        //            refresh_tooltips ="搜索所有的蒙皮网格渲染器并对其使用反射探针，如果延期着色功能开启也会搜索并替换所有的皮肤着色器。编辑人物后需要刷新。";
        //            titlebar_0 ="光照";
        //            titlebar_1 ="镜头";
        //            titlebar_2 ="视觉";
        //            titlebar_3 ="其他";
        //            Bloom ="过度曝光溢出";
        //            Bloom_Tooltip ="用于模拟真实相机成像时的瑕疵。在使用高动态范围渲染时应该把阈值设在低动态范围之上（也就是大于1）";
        //            Window_Height ="窗口高度";
        //            Window_Width ="窗口宽度";
        //            Light_Intensity ="光强：";
        //            Colorpicker_blue ="蓝色通道：";
        //            Colorpicker_green ="绿色通道：";
        //            Colorpicker_red ="红色通道：";
        //            Colorpicker_saturation ="饱和度：";
        //            Colorpicker_value ="亮度：";
        //            Colorpicker_hue ="色相：";
        //            Loading ="载入中...";
        //            Load_cubemap ="载入 Cubemap：";
        //            break;
        //        case SystemLanguage.Japanese:
        //            Reset ="リセット";
        //            disable_vs_enable_0 ="無効にする";
        //            disable_vs_enable_1 ="有効にする";
        //            refresh ="リフレッシュする";
        //            titlebar_0 ="照明";
        //            titlebar_1 ="レンズ";
        //            titlebar_2 ="視覚";
        //            titlebar_3 ="その他";
        
        //            break;
        //        default:
        //            break;
        //    }
        //}

        private static string _reflectionRefreshRateArray0 ="On Demand";
        private static string _reflectionRefreshRateArray1 ="Low";
        private static string _reflectionRefreshRateArray2 ="High";
        private static string _reflectionRefreshRateArray3 ="Extreme";
        private static string _bloomAntiflicker ="AntiFlicker";
        private static string _bloomAntiflickerTooltips ="Reduces flashing noise with an additional filter.";
        private static string _tonemapping ="Tone Mapping";
        private static string _tonemappingTooltips ="Remap HDR values of an image into a range suitable to be displayed on screen";
        private static string _tonemappingNone ="None";
        private static string _tonemappingNoneTooltips ="Not Recommended.";
        private static string _tonemappingAces ="Filmic (ACES)";
        private static string _tonemappingAcesTooltips ="Recommended. A close approximation of the reference ACES tonemapper for a more filmic look.";
        private static string _tonemappingNeutral ="Neutral";
        private static string _tonemappingNeutralTooltips ="Game Default. Only does range-remapping.";

        private static string _vsync ="VSync";
        private static string _vsynctooltips ="Apply on next run.";
        private static string _disableVsEnable0 ="Disable";
        private static string _disableVsEnable1 ="Enable";

        private static string _dofAutoFocus ="Auto Focus";
        private static string _dofAutoFocusTooltips ="Focus on camera target.";
        private static string _dofAutoCalc ="Auto Calculate";
        private static string _dofAutoCalcTooltips ="Calculate focal length from camera field-of-view";
        private static string _dofKernelSize0 ="Small";
        private static string _dofKernelSize1 ="Medium";
        private static string _dofKernelSize2 ="Large";
        private static string _dofKernelSize3 ="Very Large";
        private static string _vignetteMode ="Mode:";
        private static string _vignetteModeTooltips ="Use the \"Classic\"mode for parametric controls. Use \"Round\"to get a perfectly round vignette no matter what the aspect ratio is.";
        private static string _vignetteModeSelection0 ="Classic";
        private static string _vignetteModeSelection1 ="Round";
        private static string _filterTemperature ="Temperature";
        private static string _filterTemperatureTooltips ="Sets the white balance to a custom color temperature.";
        private static string _filterTint ="Tint";
        private static string _filterTintTooltips ="Sets the white balance to compensate for a green or magenta tint.";
        private static string _filterHue ="Hue Shift";
        private static string _filterHueTooltips ="Shift the hue of all colors.";
        private static string _filterSaturation ="Saturation";
        private static string _filterSaturationTooltips ="Pushes the intensity of all colors.";
        private static string _filterContrast ="Contrast";
        private static string _filterContrastTooltips ="Expands or shrinks the overall range of tonal values.";
        private static string _autoRefresh ="Auto Refresh";
        private static string _autoRefreshTooltips ="Automatically find all SkinnedMeshRenderer and enable ReflectionProbe usage, also replace all skin shader when deferred shading enabled. This may have performance impact.";
        private static string _asyncLoad ="Async Load";
        private static string _asyncLoadTootips ="Enable it to eliminate performance impact during cubemap loading while increasing the loading time.";
        private static string _forceDeferred ="Force Enable Experimental Features";
        private static string _forceDeferredTooltips ="Automatically enable deferred shading and tessallation.";
        private static string _autoSetting ="Auto Setting";
        private static string _autoSettingTooltips ="Auto load optimal setting at scene change. This will disable DHH graphicsetting module silently.";

        private static string _deferredShading ="Experimental Features";
        private static string _deferredShadingTooltips ="Enable deferred shading and tessallation. Once enabled there's no way to disable it until scene change.";
        private static string _refresh ="Refresh";
        private static string _titlebar0 ="Lighting";
        private static string _titlebar1 ="Lens";
        private static string _titlebar2 ="Perception";
        private static string _titlebar3 ="Misc";
        private static string _refreshTooltips ="It will enable reflection probe usage on all skinned mesh renderers, and replace shader when deferred shading is enabled. Refresh when you changed skin, face, head, nipple, clothes, etc. Or something don't look right, or when you feel like to, it's not a bad habbit.";
        private static string _eyeAdaptation ="Eye Adaptation";
        private static string _eyeAdaptationTooltips ="This effect dynamically adjusts the exposure of the image according to the range of brightness levels it contains.";
        private static string _shadowNone ="None";
        private static string _shadowHard ="Hard";
        private static string _shadowSoft ="Soft";

        private static string _tessellationPhong ="Phong";
        private static string _tessellationPhongTooltips ="Phong tessellation strength.";
        private static string _tessellationEdgelength ="Edge length";
        private static string _tessellationEdgelengthTooltips ="Length of edge to split for tessellation. Lower values result in more tessellation.";

        public static string Reset ="Reset";
        public static GUIContent Vsync = new GUIContent(_vsync, _vsynctooltips);

        public static string[] disableVsEnable = new string[] {_disableVsEnable0, _disableVsEnable1};
        public static string dithering ="Dithering";
        public static string ditheringTooltips ="Intentionally applying noise as to randomize quantization error. This prevents large-scale patterns such as color banding in images.";
        public static string depthOfView ="Depth of View";
        public static string fieldOfView ="Field of View";
        public static string vignette ="Vignette";
        public static string filters ="Filters";
        public static string chromaticAberration ="Chromatic Aberration";
        public static string globalSettings ="Global Settings";
        public static GUIContent autoRefresh = new GUIContent(_autoRefresh, _autoRefreshTooltips);
        public static GUIContent asyncLoad = new GUIContent(_asyncLoad, _asyncLoadTootips);
        public static GUIContent forceDeferred = new GUIContent(_forceDeferred, _forceDeferredTooltips);
        public static GUIContent autoSetting = new GUIContent(_autoSetting, _autoSettingTooltips);

        public static string skinScattering ="Skin Scattering";
        public static string skinTransmission ="Skin Transmission";
        public static string customWindow ="Customize Window";
        public static string customWindowRemember ="Remember size and position";
        public static string optimalSettings ="Load Optimal Settings";
        public static GUIContent deferredShading = new GUIContent(_deferredShading, _deferredShadingTooltips);
        public static GUIContent Refresh = new GUIContent(_refresh, _refreshTooltips);

        public static string reflection ="Reflection";
        public static string directionalLight ="Directional Light";
        public static string[] titlebar = new string[] {_titlebar0, _titlebar1, _titlebar2, _titlebar3};
        public static string exposureValue ="Exposure Value:";
        public static string exposureValueTooltips ="Adjusts the overall exposure of the scene in EV units. This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.";
        public static GUIContent eyeAdaptation = new GUIContent(_eyeAdaptation, _eyeAdaptationTooltips);

        public static string bloom ="Cinematic Bloom";
        public static string bloomTooltip ="Bloom is an effect used to reproduce an imaging artifact of real-world cameras. In HDR rendering a Bloom effect should only affects areas of brightness above LDR range (above 1) by setting the Threshold parameter just above this value.";
        public static string windowHeight ="Window height";
        public static string windowWidth ="Window width";
        public static string tonemappingTemperatureShift ="Temperature shift";
        public static string tonemappingTint ="Tint";
        public static string tonemappingContrast ="Contrast";
        public static string tonemappingHue ="Hue";
        public static string tonemappingSaturation ="Saturation";
        public static string tonemappingValue ="Value";
        public static string tonemappingVibrance ="Vibrance";
        public static string tonemappingGain ="Gain";
        public static string tonemappingGamma ="Gamma";
        public static string tonemappingSpecularHighlight ="Specular Highlight";
        public static string color ="Color:";
        public static string lightIntensity ="Intensity:";
        public static string Radius ="Radius:";
        public static string reflectionIntensity ="Reflection Intensity:";
        public static string lightRefresh ="Refresh";
        public static string lightRefreshTooltips ="Refresh the list to find new directional light";
        public static string skinScatteringWeightTooltips ="Weight of the scattering effect.";
        public static string skinScatteringWeight ="Weight";
        public static string skinScatteringMaskCutoffTooltips ="Threshold value above which transmission map is used to mask skin scattering. Values below act as a blend mask back to standard shading model.";
        public static string skinScatteringMaskCutoff ="Mask cutoff";
        public static string skinScatteringScaleTooltips ="Decreases scattering effect.";
        public static string skinScatteringScale ="Scale";
        public static string skinScatteringBias ="Bias";
        public static string skinScatteringBiasTooltips ="Increases scattering effect.";
        public static string skinBumpBlurTooltips ="Amount that the normals are blurred for ambient and direct diffuse lighting.";
        public static string skinBumpBlur ="Bump blur";
        public static string skybox ="Skybox";
        public static string proceduralSkybox ="Procedural Skybox";
        public static string skyboxExposure ="Skybox Exposure:";
        public static string sunSize ="Sun Size :";
        public static string atmosphereThickness ="Atmosphere Thickness:";
        public static string skyTint ="Sky Tint:";
        public static string groundColor ="Gound Color:";
        public static string ambientIntensity ="Ambient Intensity:";
        public static string skyboxRotation ="Skybox Rotation:";
        public static string reflectionProbeRefreshRate ="Reflection Probe Refresh Rate:";
        public static string reflectionProbeRefresh ="Refresh";

        public static string[] reflectionProbeRefreshRateArray = new string[]
        {
            _reflectionRefreshRateArray0,
            _reflectionRefreshRateArray1,
            _reflectionRefreshRateArray2,
            _reflectionRefreshRateArray3
        };

        public static string reflectionProbeResolution ="Reflection Probe Resolution:";
        public static string loadCubemap ="Load Cubemaps:";
        public static string loading ="Loading...";
        public static string skinTransmissionWeight ="Weight";
        public static string skinTransmissionWeightTooltips ="The global intensity of the transmission effect.";
        public static string skinTransmissionShadowWeightTooltips ="How much the light shadow attenuates the transmission effect for double-sided materials.";
        public static string skinTransmissionShadowWeight ="Shadow weight";
        public static string skinBumpDistortionTooltips ="Amount that the transmission is distorted by surface normals. A low bump distortion will result in a more even, geo-driven look, while a high bump distortion will result in a far more contrasty look.";
        public static string skinBumpDistortion ="Bump distortion";
        public static string skinTransmissionFalloffTooltips ="Controls how wide/tight the angular falloff is for the transmission effect.";
        public static string skinTransmissionFalloff ="Fall off";

        public static string tessellation ="Tessellation";
        public static GUIContent tessellationEdgelength = new GUIContent(_tessellationEdgelength, _tessellationEdgelengthTooltips);
        public static GUIContent tessellationPhong = new GUIContent(_tessellationPhong, _tessellationPhongTooltips);
        public static string tessellationSave ="Save";

        public static string bloomIntensityTooltips ="Blend factor of the result image.";
        public static string bloomIntensity ="Intensity:";
        public static string bloomThresholdTooltips ="Filters out pixels under this level of brightness.";
        public static string bloomThreshold ="Threshold:";
        public static string bloomSoftkneeTooltips ="Makes transition between under/over-threshold gradual (0 = hard threshold, 1 = soft threshold).";
        public static string bloomSoftknee ="Softknee:";
        public static string bloomRadiusTooltips ="Changes extent of veiling effects in a screen resolution-independent fashion.";
        public static string bloomRadius ="Radius:";
        public static GUIContent bloomAntiflicker = new GUIContent(_bloomAntiflicker, _bloomAntiflickerTooltips);
        public static string exposureCompensationTooltips ="Exposure bias. Use this to control the global exposure of the scene.";
        public static string exposureCompensation ="Exposure Compensation:";
        public static string histogramFilterTooltips ="These values are the lower and upper percentages of the histogram that will be used to find a stable average luminance. Values outside of this range will be discarded and won't contribute to the average luminance.";
        public static string histogramFilter ="Histogram filtering";
        public static string histogramBoundTooltips ="bound for the brightness range of the generated histogram (Log2).";
        public static string histogramBound ="Histogram bound:";
        public static string luminationRangeTooltips ="average luminance to consider for auto exposure.";
        public static string luminationRange ="Luminosity range:";
        public static string darkAdaptationSpeedTooltips ="Adaptation speed from a light to a dark environment.";
        public static string darkAdaptationSpeed ="Dark environment adatation speed:";
        public static string lightAdaptationSpeedTooltips ="Adaptation speed from a dark to a light environment.";
        public static string lightAdaptationSpeed ="Light environment adatation speed:";
        public static string[] VignetteModeSelection = new string[] {_vignetteModeSelection0, _vignetteModeSelection1};

        public static GUIContent vignetteMode = new GUIContent(_vignetteMode, _vignetteModeTooltips);
        public static string vignetteColor ="Vignette Color:";

        public static GUIContent tonemapping = new GUIContent(_tonemapping, _tonemappingTooltips);
        public static GUIContent tonemappingNone = new GUIContent(_tonemappingNone, _tonemappingNoneTooltips);
        public static GUIContent tonemappingAces = new GUIContent(_tonemappingAces, _tonemappingAcesTooltips);
        public static GUIContent tonemappingNeutral = new GUIContent(_tonemappingNeutral, _tonemappingNeutralTooltips);

        public static string tonemappingNeutralWhiteClip ="White Clip:";
        public static string tonemappingNeutralWhiteLevel ="White Level:";
        public static string tonemappingNeutralWhiteOut ="White Out:";
        public static string tonemappingNeutralWhiteIn ="White In:";
        public static string tonemappingNeutralBlackOut ="Black Out:";
        public static string tonemappingNeutralBlackIn ="Black In:";

        public static string vignetteIntensity ="Intensity:";
        public static string vignetteRoundness ="Roundness:";
        public static string vignetteSmoothness ="Smoothness:";
        public static string chromaticAberrationIntensity ="Intensity:";

        public static string dofFocalDistance ="Focal Distance:";
        public static string dofFocalDistanceTooltips ="Distance to the point of focus.";
        public static string dofApertureTooltips ="Ratio of aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is.";
        public static string dofAperture ="Aperture:";
        public static string dofFocallength ="Focal Length:";
        public static string dofFocallengthTooltips ="Distance between the lens and the film. The larger the value is, the shallower the depth of field is.";
        public static string dofKernelTooltips ="Convolution kernel size of the bokeh filter, which determines the maximum radius of bokeh. It also affects the performance (the larger the kernel is, the longer the GPU time is required).";
        public static string dofKernel ="Kernel Size:";
        public static string[] dofKernelSize = new string[] {_dofKernelSize0, _dofKernelSize1, _dofKernelSize2, _dofKernelSize3};
        public static GUIContent dofAutoCalc = new GUIContent(_dofAutoCalc, _dofAutoCalcTooltips);
        public static GUIContent dofAutoFocus = new GUIContent(_dofAutoFocus, _dofAutoFocusTooltips);

        public static GUIContent filterTemperature = new GUIContent(_filterTemperature, _filterTemperatureTooltips);
        public static GUIContent filterHue = new GUIContent(_filterHue, _filterHueTooltips);
        public static GUIContent filterTint = new GUIContent(_filterTint, _filterTintTooltips);
        public static GUIContent filterContrast = new GUIContent(_filterContrast, _filterContrastTooltips);
        public static GUIContent filterSaturation = new GUIContent(_filterSaturation, _filterSaturationTooltips);

        public static string colorpickerRed ="Red:";
        public static string colorpickerGreen ="Green:";
        public static string colorpickerBlue ="Blue:";
        public static string colorpickerHue ="Hue:";
        public static string colorpickerSaturation ="Saturation:";
        public static string colorpickerValue ="Value:";

        public static string dollyZoom ="Dolly Zoom";

        public static string shadow ="Shadow";
        public static string[] shadowSelections = new string[] {_shadowNone, _shadowHard, _shadowSoft};
        public static string shadowStrength ="Shadow strength:";
        public static string shadowBias ="Shadow bias:";

        public static string shadowNormalBias ="Shadow normal bias:";
    }
}
