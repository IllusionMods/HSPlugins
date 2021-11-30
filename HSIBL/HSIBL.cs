using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.CinematicEffects;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using HSLRE;
using HSLRE.CustomEffects;
using IllusionPlugin;
using IllusionUtility.GetUtility;
using Studio;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine.Rendering;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using Object = UnityEngine.Object;

namespace HSIBL
{
	public class HSIBL : MonoBehaviour
	{
		#region Types
		private enum DOFFocusType
		{
			Crosshair,
			Object,
			Manual
		}
		#endregion

		#region Fields
		private readonly ProceduralSkyboxManager _proceduralSkybox = new ProceduralSkyboxManager();
		private readonly SkyboxManager _skybox = new SkyboxManager();
		public GameObject probeGameObject = new GameObject("RealtimeReflectionProbe");
		private ProceduralSkyboxParams _tempProceduralSkyboxParams;
		private SkyboxParams _tempSkyboxParams;
		private GameObject _lightsObj;
		private Quaternion _lightsObjDefaultRotation;
		private ReflectionProbe _probeComponent;
		private readonly int[] _possibleReflectionProbeResolutions = { 64, 128, 256, 512, 1024, 2048 };
		private string[] _possibleReflectionProbeResolutionsNames;
		private string[] _cubeMapFileNames;
		private int _reflectionProbeResolution;
		private SortedList<int, string> _layers;
		private CharFemale _cMfemale;
		public bool cameraCtrlOff;
		private Camera _subCamera;
		private FolderAssist _cubemapFolder;
		private const string _presetFolder = "Plugins\\HSIBL\\Presets\\";
		private readonly Dictionary<object, Action<HSLRE.HSLRE.EffectData, int>> _effectsModules = new Dictionary<object, Action<HSLRE.HSLRE.EffectData, int>>();
		private readonly Dictionary<object, string> _effectsPrettyNames = new Dictionary<object, string>();
		private HSLRE.HSLRE.EffectType _effectTypeDisplay = HSLRE.HSLRE.EffectType.FourKDiffuse | HSLRE.HSLRE.EffectType.LRE;
		private readonly string[] _effectTypeDisplayNames = { "4K", "LRE", "Both" };
		private KeyCode _shortcut = KeyCode.F5;
		private RectTransform _imguiBackground;

		private static ushort _errorcode = 0;
		private Light _backDirectionalLight;
		private Light _frontDirectionalLight;
		private Light _frontDirectionalMapLight;
		private bool _cubemapLoaded = false;
		private bool _hideSkybox = false;

		private bool _tonemappingEnabled = true;
		private HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper _toneMapper = HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.ACES;
		private float _ev;
		private float _eyeSpeed;
		private bool _eyeEnabled = false;
		private float _eyeMiddleGrey;
		private float _eyeMin;
		private float _eyeMax;

		private HSLRE.CustomEffects.TonemappingColorGrading _toneMappingManager;
		private HSLRE.CustomEffects.Bloom _cinematicBloom;
		private LensAberrations _lensManager;

		private float _previousAmbientIntensity = 1f;

		private bool _environmentUpdateFlag = false;

		private static bool _showUI = false;

		private float _charaRotate = 0f;
		private Vector3 _rotateValue;
		private bool _autoRotate = false;
		private float _autoRotateSpeed = 0.2f;
		private int _selectedCubemap;
		private int _previousSelectedCubeMap;
		private const int _uniqueId = ('H' << 24) | ('I' << 16) | ('B' << 8) | 'L';
		private int _reflectionProbeRefreshRate;

		private bool _windowdragflag = false;
		private int _tabMenu;
		private bool _frontLightAnchor = true;
		private bool _backLightAnchor = true;
		private Vector3 _frontRotate;
		private Vector3 _backRotate;
		private bool _isLoading = false;

		private SSAOPro _ssao;
		private string[] _possibleSSAOSampleCountNames;
		private string[] _possibleSSAOBlurModeNames;

		private SunShafts _sunShafts;
		private string[] _possibleSunShaftsResolutionNames;
		private string[] _possibleShaftsScreenBlendModeNames;

		private DepthOfField _depthOfField;
		private string[] _possibleBlurSampleCountNames;
		private string[] _possibleDOFBlurTypeNames;
		private Transform _depthOfFieldOriginalFocusPoint;
		private DOFFocusType _focusType = DOFFocusType.Crosshair;
		private string[] _possibleDOFFocusTypeNames;
		private ObjectCtrlInfo _focussedObject;

		private ScreenSpaceReflection _ssr;
		private string[] _possibleSSRResolutionNames;
		private string[] _possibleSSRDebugModeNames;

		private VolumetricLightRenderer _volumetricLights;
		private string[] _possibleVolumetricLightsResolutionNames;

		private SMAA _smaa;
		private string[] _possibleSMAADebugPassNames;
		private string[] _possibleSMAAQualityPresetNames;
		private string[] _possibleSMAAEdgeDetectionMethodNames;

		private BloomAndFlares _bloomAndFlares;
		private string[] _possibleScreenBlendModeNames;
		private string[] _possibleHDRBloomModeNames;
		private string[] _possibleLensFlareStyle34Names;

		private ColorCorrectionCurves _colorCorrectionCurves;

		private VignetteAndChromaticAberration _vignette;
		private string[] _possibleAberrationModeNames;

		private Antialiasing _antialiasing;
		private string[] _possibleAAModeNames;

		private NoiseAndGrain _noiseAndGrain;
		private string[] _possibleFilterModeNames;

		private CameraMotionBlur _motionBlur;
		private string[] _possibleMotionBlurFilterNames;

		private AfterImage _afterImage;

		private SEGI _segi;
		private string[] _possibleVoxelResolutionNames;
		private string[] _possiblePresetNames;
		private int _selectedSegiPreset = 10;

		private AmplifyBloom _amplifyBloom;
		private string[] _possibleUpscaleQualityEnumNames;
		private string[] _possibleMainThresholdSizeEnumNames;
		private string[] _possiblePrecisionModesNames;
		private string[] _possibleDebugToScreenEnumNames;
		private string[] _possibleApertureShapeNames;
		private string[] _possibleGlareLibTypeNames;
		private string[] _possibleLensDirtTextureEnumNames;
		private string[] _possibleLensStarburstTextureEnumNames;

		private BlurOptimized _blur;
		private string[] _possibleBlurTypeNames;

		private Quaternion _frontLightDefaultRotation;
		private Quaternion _backLightDefaultRotation;
		private Material _originalSkybox;
		private AmbientMode _originalAmbientMode;
		private DefaultReflectionMode _originalDefaultReflectionMode;
		private string _currentTooltip = "";
		private Vector2 _presetsScroll;
		private string _presetName = "";
		private bool _removePresetMode;
		private string[] _presets = new string[0];
		internal static bool _isStudio = false;
		private AmbientMode _oldAmbientMode;
		#endregion

		#region Accessors
		private readonly Func<float> _getWindowHeight = () => ModPrefs.GetFloat("HSIBL", "Window.height", 600);
		private readonly Func<float> _getWindowWidth = () => ModPrefs.GetFloat("HSIBL", "Window.width", 600);
		private string _defaultCharaMakerCubemap;
		private int _lastObjectCount = -1;
		private Object _lastSelectedNode;
		private VolumetricLight _currentVolumetricLight;
		#endregion

		#region Unity Methods
		private void Awake()
		{
			Camera.main.hdr = true;

			Console.WriteLine("HSIBL is Loaded");
			Console.WriteLine("----------------");

			this._lightsObj = GameObject.Find("Lights");
			if (this._lightsObj != null)
				this._lightsObjDefaultRotation = this._lightsObj.transform.localRotation;
			this._probeComponent = this.probeGameObject.AddComponent<ReflectionProbe>();
			this._probeComponent.mode = ReflectionProbeMode.Realtime;
			this._probeComponent.resolution = 512;
			this._probeComponent.hdr = true;
			this._probeComponent.intensity = 1f;
			this._probeComponent.type = ReflectionProbeType.Cube;
			this._probeComponent.clearFlags = ReflectionProbeClearFlags.Skybox;
			this._probeComponent.size = new Vector3(1000, 1000, 1000);
			this.probeGameObject.transform.position = new Vector3(0, 2, 0);
			this._probeComponent.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
			this._probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
			this._probeComponent.nearClipPlane = 1.01f;
			this._probeComponent.cullingMask &= ~(1 << 27); //SEGI GI Blockers
			this._probeComponent.cullingMask &= ~(1 << 26); //Suimono FX
			this._probeComponent.cullingMask &= ~(1 << 25); //Suimono Depth
			this._probeComponent.cullingMask &= ~(1 << 24); //Suimono Water

			this._layers = new SortedList<int, string>(32);
			for (int i = 0; i < 32; i++)
			{
				string name = LayerMask.LayerToName(i);
				if (string.IsNullOrEmpty(name) == false)
					this._layers.Add(i, name);
			}
			this._layers.Add(24, "Suimono");

			this._possibleReflectionProbeResolutionsNames = this._possibleReflectionProbeResolutions.Select(e => e.ToString()).ToArray();
			this._possibleSSAOSampleCountNames = Enum.GetNames(typeof(SSAOPro.SampleCount));
			this._possibleSSAOBlurModeNames = Enum.GetNames(typeof(SSAOPro.BlurMode));
			this._possibleSunShaftsResolutionNames = Enum.GetNames(typeof(SunShafts.SunShaftsResolution));
			this._possibleShaftsScreenBlendModeNames = Enum.GetNames(typeof(SunShafts.ShaftsScreenBlendMode));
			this._possibleBlurSampleCountNames = Enum.GetNames(typeof(DepthOfField.BlurSampleCount));
			this._possibleDOFFocusTypeNames = Enum.GetNames(typeof(DOFFocusType));
			this._possibleDOFBlurTypeNames = Enum.GetNames(typeof(DepthOfField.BlurType));
			this._possibleSSRResolutionNames = Enum.GetNames(typeof(ScreenSpaceReflection.SSRResolution));
			this._possibleSSRDebugModeNames = Enum.GetNames(typeof(ScreenSpaceReflection.SSRDebugMode));
			this._possibleVolumetricLightsResolutionNames = Enum.GetNames(typeof(VolumetricLightRenderer.VolumetricResolution));
			this._possibleSMAADebugPassNames = Enum.GetNames(typeof(SMAA.DebugPass));
			this._possibleSMAAQualityPresetNames = Enum.GetNames(typeof(SMAA.QualityPreset));
			this._possibleSMAAEdgeDetectionMethodNames = Enum.GetNames(typeof(SMAA.EdgeDetectionMethod));
			this._possibleScreenBlendModeNames = Enum.GetNames(typeof(BloomScreenBlendMode));
			this._possibleHDRBloomModeNames = Enum.GetNames(typeof(HDRBloomMode));
			this._possibleLensFlareStyle34Names = Enum.GetNames(typeof(LensflareStyle34));
			this._possibleAberrationModeNames = Enum.GetNames(typeof(VignetteAndChromaticAberration.AberrationMode));
			this._possibleAAModeNames = Enum.GetNames(typeof(AAMode));
			this._possibleFilterModeNames = Enum.GetNames(typeof(FilterMode));
			this._possibleMotionBlurFilterNames = Enum.GetNames(typeof(CameraMotionBlur.MotionBlurFilter));
			this._possibleVoxelResolutionNames = Enum.GetNames(typeof(SEGI.VoxelResolution));
			this._possibleUpscaleQualityEnumNames = Enum.GetNames(typeof(UpscaleQualityEnum));
			this._possibleMainThresholdSizeEnumNames = Enum.GetNames(typeof(MainThresholdSizeEnum));
			this._possiblePrecisionModesNames = Enum.GetNames(typeof(PrecisionModes));
			this._possibleDebugToScreenEnumNames = Enum.GetNames(typeof(DebugToScreenEnum));
			this._possibleApertureShapeNames = Enum.GetNames(typeof(ApertureShape));
			this._possibleGlareLibTypeNames = Enum.GetNames(typeof(GlareLibType)).ToList().Take((int)(GlareLibType.Custom - 1)).ToArray();
			this._possibleLensDirtTextureEnumNames = Enum.GetNames(typeof(LensDirtTextureEnum));
			this._possibleLensStarburstTextureEnumNames = Enum.GetNames(typeof(LensStarburstTextureEnum));
			this._possibleBlurTypeNames = Enum.GetNames(typeof(BlurOptimized.BlurType));
			if (HSIBLPlugin._self.binary == Binary.Studio)
			{
				_isStudio = true;
				HSExtSave.HSExtSave.RegisterHandler("hsibl", null, null, this.OnSceneLoad, null, this.OnSceneSave, null, null);
			}

			this._effectTypeDisplay = (HSLRE.HSLRE.EffectType)ModPrefs.GetInt("HSIBL", "effectTypeFilter", (int)this._effectTypeDisplay, true);
			this._defaultCharaMakerCubemap = ModPrefs.GetString("HSIBL", "defaultCharaMakerCubemap", "", true);
			try
			{
				this._shortcut = (KeyCode)Enum.Parse(typeof(KeyCode), ModPrefs.GetString("HSIBL", "shortcut", this._shortcut.ToString(), true));
			}
			catch (Exception) { }
			this._imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI(HSIBLPlugin._self.binary == Binary.Game);
		}

		private IEnumerator Start()
		{
			yield return null;
			yield return null;
			yield return null;
			this._originalSkybox = RenderSettings.skybox;
			this._originalAmbientMode = RenderSettings.ambientMode;
			this._originalDefaultReflectionMode = RenderSettings.defaultReflectionMode;
			yield return new WaitUntil(() => HSLRE.HSLRE.self.init);

			this._ssao = HSLRE.HSLRE.self.ssao;
			if (this._ssao != null)
				this._effectsModules.Add(this._ssao, this.SSAOModule);

			this._sunShafts = HSLRE.HSLRE.self.sunShafts;
			if (this._sunShafts != null)
				this._effectsModules.Add(this._sunShafts, this.SunShaftsModule);

			this._depthOfField = HSLRE.HSLRE.self.dof;
			if (this._depthOfField != null)
			{
				this._effectsModules.Add(this._depthOfField, this.DepthOfFieldModule);
				this._depthOfFieldOriginalFocusPoint = this._depthOfField.focalTransform;
			}
			this._ssr = HSLRE.HSLRE.self.ssr;
			if (this._ssr != null)
				this._effectsModules.Add(this._ssr, this.SSRModule);

			this._volumetricLights = HSLRE.HSLRE.self.volumetricLights;
			if (this._volumetricLights != null)
				this._effectsModules.Add(this._volumetricLights, this.VolumetricLightsModule);

			this._smaa = HSLRE.HSLRE.self.smaa;
			if (this._smaa != null)
				this._effectsModules.Add(this._smaa, this.SMAAModule);

			this._cinematicBloom = HSLRE.HSLRE.self.cinematicBloom;
			if (this._cinematicBloom != null)
				this._effectsModules.Add(this._cinematicBloom, this.BloomModule);

			this._toneMappingManager = HSLRE.HSLRE.self.tonemapping;
			if (this._toneMappingManager != null)
				this._effectsModules.Add(this._toneMappingManager, this.ToneMappingColorGradingModule);

			this._lensManager = HSLRE.HSLRE.self.lensAberrations;
			if (this._lensManager != null)
				this._effectsModules.Add(this._lensManager, this.LensModule);

			this._bloomAndFlares = HSLRE.HSLRE.self.bloomAndFlares;
			if (this._bloomAndFlares != null)
				this._effectsModules.Add(this._bloomAndFlares, this.BloomAndFlaresModule);

			this._colorCorrectionCurves = HSLRE.HSLRE.self.ccc;
			if (this._colorCorrectionCurves != null)
				this._effectsModules.Add(this._colorCorrectionCurves, this.ColorCorrectionCurvesModule);

			this._vignette = HSLRE.HSLRE.self.vignette;
			if (this._vignette != null)
				this._effectsModules.Add(this._vignette, this.VignetteAndChromaticAberrationModule);

			this._antialiasing = HSLRE.HSLRE.self.antialiasing;
			if (this._antialiasing != null)
				this._effectsModules.Add(this._antialiasing, this.AntialiasingModule);

			this._noiseAndGrain = HSLRE.HSLRE.self.noiseAndGrain;
			if (this._noiseAndGrain != null)
				this._effectsModules.Add(this._noiseAndGrain, this.NoiseAndGrainModule);

			this._motionBlur = HSLRE.HSLRE.self.motionBlur;
			if (this._motionBlur != null)
				this._effectsModules.Add(this._motionBlur, this.MotionBlurModule);

			this._afterImage = HSLRE.HSLRE.self.afterImage;
			if (this._afterImage != null)
				this._effectsModules.Add(this._afterImage, this.AfterImageModule);

			this._segi = HSLRE.HSLRE.self.segi;
			if (this._segi != null)
				this._effectsModules.Add(this._segi, this.SEGIModule);

			this._amplifyBloom = HSLRE.HSLRE.self.amplifyBloom;
			if (this._amplifyBloom != null)
				this._effectsModules.Add(this._amplifyBloom, this.AmplifyBloomModule);

			this._blur = HSLRE.HSLRE.self.blur;
			if (this._blur != null)
				this._effectsModules.Add(this._blur, this.BlurModule);

			if (_isStudio == false && HSLRE.Settings.basicSettings.CharaMakerReform && (HSIBLPlugin._self.level == 21 || HSIBLPlugin._self.level == 22))
				this._selectedCubemap = this.LoadCubemap(this._defaultCharaMakerCubemap);

			if (this._segi != null)
			{
				Light selected = null;
				if (_isStudio)
				{
					foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
					{
						OCILight light = pair.Value as OCILight;
						if (light != null && light.light.type == LightType.Directional && (selected == null || selected.intensity < light.light.intensity))
							selected = light.light;
					}
				}
				else
				{
					foreach (Light light in GameObject.FindObjectsOfType<Light>())
					{
						if (light.type == LightType.Directional && (selected == null || selected.intensity < light.intensity))
							selected = light;
					}
				}
				this._segi.sun = selected;

				this._possiblePresetNames = this._segi._presets.Select(e => e.name).ToArray();
			}

			this._ev = Mathf.Log(HSLRE.Settings.tonemappingSettings.exposure, 2);
		}

		private void OnEnable()
		{
			if (!this._proceduralSkybox.Proceduralsky)
			{
				this._proceduralSkybox.Init();
			}
			this._tempProceduralSkyboxParams = this._proceduralSkybox.skyboxparams;
			this.StopAllCoroutines();
			this.StartCoroutine(this.EndOfFrame());
			if (_isStudio)
			{
				this._subCamera = GameObject.Find("Camera").GetComponent<Camera>();
				this._subCamera.fieldOfView = Camera.main.fieldOfView;
				UIUtils.windowRect = new Rect(Screen.width * 0.7f, Screen.height * 0.65f, Screen.width * 0.34f, Screen.height * 0.45f);
			}
			else if (HSIBLPlugin._self.level == 21 || HSIBLPlugin._self.level == 22)
			{

				UIUtils.windowRect = new Rect(Screen.width * 0.22f, Screen.height * 0.64f, Screen.width * 0.34f, Screen.height * 0.45f);
				this._cMfemale = Singleton<Character>.Instance.GetFemale(0);
				if (this._cMfemale)
				{
					this._rotateValue = this._cMfemale.GetRotation();
					this.StartCoroutine(this.RotateCharater());
				}
			}
			else
			{
				UIUtils.windowRect = new Rect(0f, Screen.height * 0.3f, Screen.width * 0.35f, Screen.height * 0.45f);
			}

			this._frontDirectionalLight = Camera.main.transform.FindLoop("DirectionalFront").GetComponent<Light>();
			this._backDirectionalLight = Camera.main.transform.FindLoop("DirectionalBack").GetComponent<Light>();
			this._frontLightDefaultRotation = this._frontDirectionalLight.transform.localRotation;
			this._backLightDefaultRotation = this._backDirectionalLight.transform.localRotation;
			GameObject mapLight;
			if ((mapLight = GameObject.Find("DirectionalFrontMap")) != null)
				this._frontDirectionalMapLight = mapLight.GetComponent<Light>();

			this._cubemapFolder = new FolderAssist();
			this._cubemapFolder.CreateFolderInfo(Application.dataPath + "/../abdata/plastic/cubemaps/", "*.unity3d", true, true);
			this._selectedCubemap = -1;
			this._previousSelectedCubeMap = -1;
			this._cubeMapFileNames = new string[this._cubemapFolder.lstFile.Count + 1];
			this._cubeMapFileNames[0] = "Procedural";
			int count = 1;
			foreach (FolderAssist.FileInfo fileInfo in this._cubemapFolder.lstFile)
			{
				this._cubeMapFileNames[count] = fileInfo.FileName;
				count++;
			}

			this.RefreshPresetList();
			this._environmentUpdateFlag = true;
		}

		private void Update()
		{
			if (this._oldAmbientMode != RenderSettings.ambientMode)
				this._environmentUpdateFlag = true;
			this._oldAmbientMode = RenderSettings.ambientMode;
			if (Input.GetKeyDown(this._shortcut))
				_showUI = !_showUI;

			if (_showUI && Input.GetKeyDown(KeyCode.Escape))
				_showUI = false;
			if (this._selectedCubemap == 0)
			{
				if (!Mathf.Approximately(this._previousAmbientIntensity, RenderSettings.ambientIntensity) || !(this._tempProceduralSkyboxParams).Equals(this._proceduralSkybox.skyboxparams))
					this._environmentUpdateFlag = true;
				this._proceduralSkybox.ApplySkyboxParams();
				this._tempProceduralSkyboxParams = this._proceduralSkybox.skyboxparams;
				this._previousAmbientIntensity = RenderSettings.ambientIntensity;
			}
			else if (this._selectedCubemap > 0)
			{
				if (!Mathf.Approximately(this._previousAmbientIntensity, RenderSettings.ambientIntensity) || !(this._tempSkyboxParams).Equals(this._skybox.skyboxparams))
					this._environmentUpdateFlag = true;
				this._skybox.ApplySkyboxParams();
				this._tempSkyboxParams = this._skybox.skyboxparams;
				this._previousAmbientIntensity = RenderSettings.ambientIntensity;
			}

			if (this._hideSkybox)
			{
				Camera.main.clearFlags = CameraClearFlags.SolidColor;
			}
			else
			{
				if (_isStudio == false && (HSIBLPlugin._self.level == 21 || HSIBLPlugin._self.level == 22))
				{
					if (Settings.basicSettings.CharaMakerReform)
						Camera.main.clearFlags = (CameraClearFlags)Settings.basicSettings.CharaMakerBackgroundType;
				}
				else
					Camera.main.clearFlags = this._cubemapLoaded ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
			}

			if (_isStudio && this._segi != null && Studio.Studio.Instance != null && this._lastObjectCount != Studio.Studio.Instance.dicObjectCtrl.Count)
			{
				Light selected = null;
				foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
				{
					OCILight light = pair.Value as OCILight;
					if (light != null && light.light.type == LightType.Directional && (selected == null || selected.intensity < light.light.intensity))
						selected = light.light;
				}
				this._segi.sun = selected;
				this._lastObjectCount = Studio.Studio.Instance.dicObjectCtrl.Count;
			}

			if (_isStudio && Studio.Studio.Instance != null)
			{
				TreeNodeObject node = Studio.Studio.Instance.treeNodeCtrl.selectNode;
				if (this._lastSelectedNode != node)
				{
					this._lastSelectedNode = node;
					if (node != null)
					{
						ObjectCtrlInfo objectCtrlInfo;
						if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out objectCtrlInfo))
						{
							if (objectCtrlInfo is OCILight light)
								this._currentVolumetricLight = light.light.GetComponent<VolumetricLight>();
							else
								this._currentVolumetricLight = null;
						}
						else
							this._currentVolumetricLight = null;
					}
					else
						this._currentVolumetricLight = null;
				}
			}
		}

		private void OnGUI()
		{
			if (!_showUI)
			{
				this._imguiBackground.gameObject.SetActive(false);
				return;
			}
			if (!UIUtils.styleInitialized)
				UIUtils.InitStyle();
			if (!Camera.main.hdr)
			{
				if (Camera.main.actualRenderingPath != RenderingPath.DeferredShading)
				{
					this._imguiBackground.gameObject.SetActive(true);
					IMGUIExtensions.FitRectTransformToRect(this._imguiBackground, UIUtils.cmWarningRect);
					UIUtils.cmWarningRect = GUILayout.Window(_uniqueId + 1, UIUtils.cmWarningRect, this.CharaMakerWarningWindow, "Warning", UIUtils.windowStyle);
					return;
				}
				Console.WriteLine("HSIBL Warning: HDR is somehow been disabled! Trying to re-enable it...");
				Camera.main.hdr = true;
				if (!Camera.main.hdr)
				{
					Console.WriteLine("HSIBL Error: Failed to enable HDR");
					_showUI = false;
					return;
				}
				Console.WriteLine("HSIBL Info: Done!");
			}

			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, UIUtils.scale);

			if (_errorcode > 1)
			{
				this._imguiBackground.gameObject.SetActive(true);
				IMGUIExtensions.FitRectTransformToRect(this._imguiBackground, UIUtils.errorWindowRect);
				UIUtils.errorWindowRect = GUILayout.Window(_uniqueId + 2, UIUtils.errorWindowRect, this.ErrorWindow, "", UIUtils.windowStyle);
				return;
			}
			if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp)
			{
				this.cameraCtrlOff = false;
				this._windowdragflag = false;
			}
			UIUtils.windowRect = UIUtils.LimitWindowRect(UIUtils.windowRect);
			this._imguiBackground.gameObject.SetActive(true);
			IMGUIExtensions.FitRectTransformToRect(this._imguiBackground, new Rect(UIUtils.windowRect.x * UIUtils.scale.x, UIUtils.windowRect.y * UIUtils.scale.y, UIUtils.windowRect.width * UIUtils.scale.x, UIUtils.windowRect.height * UIUtils.scale.y));
			IMGUIExtensions.DrawBackground(UIUtils.windowRect);
			UIUtils.windowRect = GUILayout.Window(_uniqueId + 3, UIUtils.windowRect, this.HSIBLWindow, "", UIUtils.windowStyle);
			if (this._currentTooltip.Length != 0)
			{
				Rect tooltipRect = new Rect(new Vector2(UIUtils.windowRect.xMin, UIUtils.windowRect.yMax), new Vector2(UIUtils.windowRect.width, UIUtils.labelStyle.CalcHeight(new GUIContent(this._currentTooltip), UIUtils.windowRect.width) + 10));
				GUI.Box(tooltipRect, "");
				GUI.Box(tooltipRect, "");
				GUI.Label(tooltipRect, this._currentTooltip, UIUtils.labelStyle);
			}
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
		}

		private void OnDestroy()
		{
			ModPrefs.SetInt("HSIBL", "effectTypeFilter", (int)this._effectTypeDisplay);
			ModPrefs.SetString("HSIBL", "shortcut", this._shortcut.ToString());
			ModPrefs.SetString("HSIBL", "defaultCharaMakerCubemap", this._defaultCharaMakerCubemap);
			Destroy(this._imguiBackground);
		}
		#endregion

		#region UI
		private void HSIBLWindow(int id)
		{
			this.CameraControlOffOnGUI();
			if (Event.current.type == EventType.MouseDown)
			{
				GUI.FocusWindow(id);
				this._windowdragflag = true;
				this.cameraCtrlOff = true;
			}
			else if (Event.current.type == EventType.MouseUp)
			{
				this._windowdragflag = false;
				this.cameraCtrlOff = false;
			}
			if (this._windowdragflag && Event.current.type == EventType.MouseDrag)
			{
				this.cameraCtrlOff = true;
			}

			GUILayout.BeginHorizontal();
			using (var verticalScope = new GUILayout.VerticalScope("box", GUILayout.MaxWidth(UIUtils.windowRect.width * 0.33f)))
			{
				//////////////////Load cubemaps/////////////// 
				GUI.enabled = !this._isLoading;
				this.CubeMapModule();
				GUI.enabled = true;

			}
			GUILayout.Space(1f);
			GUILayout.BeginVertical();
			this._tabMenu = GUILayout.Toolbar(this._tabMenu, GUIStrings.titlebar, UIUtils.titleStyle);
			if (this._tabMenu == 1 || this._tabMenu == 2)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Filter effects", UIUtils.labelStyleNoStretch);

				this._effectTypeDisplay = (HSLRE.HSLRE.EffectType)GUILayout.SelectionGrid((int)this._effectTypeDisplay - 1, this._effectTypeDisplayNames, 3, UIUtils.buttonStyleStrechWidth) + 1;
				GUILayout.EndHorizontal();
			}
			UIUtils.scrollPosition[this._tabMenu + 1] = GUILayout.BeginScrollView(UIUtils.scrollPosition[this._tabMenu + 1]);
			using (GUILayout.VerticalScope verticalScope = new GUILayout.VerticalScope("box"/*, GUILayout.MaxHeight(Screen.height * 0.8f)*/))
			{
				GUILayout.Space(UIUtils.space);
				switch (this._tabMenu)
				{
					case 0:
						////////////////////Lighting tweak/////////////////////

						if (this._selectedCubemap == 0)
							this.ProceduralSkyboxModule();
						else
							this.SkyboxModule();
						UIUtils.HorizontalLine();
						this.ReflectionModule();
						UIUtils.HorizontalLine();
						this.DefaultLightModule();
						break;
					case 1:
						int tonemappingIndex = Int32.MaxValue;
						if (this._toneMappingManager != null)
							tonemappingIndex = HSLRE.HSLRE.self.generalEffects.FindIndex(e => ReferenceEquals(e.effect, this._toneMappingManager));

						//////////////////////Field of View/////////////////////
						if (_isStudio)
							Studio.Studio.Instance.cameraCtrl.fieldOfView = UIUtils.SliderGUI(Studio.Studio.Instance.cameraCtrl.fieldOfView, 1f, 179f, "Field of View", "N1");
						else
						{
							Camera.main.fieldOfView = UIUtils.SliderGUI(Camera.main.fieldOfView, 1f, 179f, "Field of View", "N1");
							if (this._subCamera != null)
							{
								this._subCamera.fieldOfView = Camera.main.fieldOfView;
							}
						}
						if (this._vignette != null && (this._effectTypeDisplay & HSLRE.HSLRE.EffectType.FourKDiffuse) != 0)
						{
							int index = HSLRE.HSLRE.self.generalEffects.FindIndex(e => ReferenceEquals(e.effect, this._vignette));

							GUILayout.BeginHorizontal();

							GUILayout.BeginVertical();
							GUILayout.Label("", GUILayout.Width(4));
							GUILayout.EndVertical();

							Rect lastRect = GUILayoutUtility.GetLastRect();

							GUILayout.BeginVertical();
							UIUtils.HorizontalLine();
							this.VignetteAndChromaticAberrationModule(HSLRE.HSLRE.self.effectsDictionary[this._vignette], index + HSLRE.HSLRE.self.opaqueEffects.Count);
							GUILayout.EndVertical();

							Color c = GUI.color;
							GUI.color = index < tonemappingIndex ? Color.yellow : Color.red;
							GUI.DrawTexture(new Rect(lastRect.x, lastRect.y, lastRect.width, GUILayoutUtility.GetLastRect().height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
							GUI.color = c;

							GUILayout.EndHorizontal();
						}

						if (this._lensManager != null && (this._effectTypeDisplay & HSLRE.HSLRE.EffectType.LRE) != 0)
						{
							int index = HSLRE.HSLRE.self.generalEffects.FindIndex(e => e.effect == this._lensManager);
							UIUtils.HorizontalLine();
							this.LensPresetsModule();

							GUILayout.BeginHorizontal();

							GUILayout.BeginVertical();
							GUILayout.Label("", GUILayout.Width(4));
							GUILayout.EndVertical();

							Rect lastRect = GUILayoutUtility.GetLastRect();

							GUILayout.BeginVertical();
							this.LensModule(null, index + HSLRE.HSLRE.self.opaqueEffects.Count);
							GUILayout.EndVertical();

							Color c = GUI.color;
							GUI.color = index < tonemappingIndex ? Color.yellow : Color.red;
							GUI.DrawTexture(new Rect(lastRect.x, lastRect.y, lastRect.width, GUILayoutUtility.GetLastRect().height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
							GUI.color = c;

							GUILayout.EndHorizontal();

						}
						break;
					case 2:
						GUILayout.BeginHorizontal();
						GUILayout.Label("Effects are displayed in the same order they are applied.", UIUtils.labelStyle);
						if (GUILayout.Button("Reset order", UIUtils.buttonStyleNoStretch, GUILayout.ExpandWidth(false)))
							HSLRE.HSLRE.self.ResetOrder();
						GUILayout.EndHorizontal();

						for (int i = 0; i < HSLRE.HSLRE.self.opaqueEffects.Count; i++)
						{
							HSLRE.HSLRE.EffectData data = HSLRE.HSLRE.self.opaqueEffects[i];
							if ((data.effectType & this._effectTypeDisplay) == 0)
								continue;
							Action<HSLRE.HSLRE.EffectData, int> moduleFunction;
							if (!this._effectsModules.TryGetValue(data.effect, out moduleFunction))
								continue;
							GUILayout.BeginHorizontal();

							GUILayout.BeginVertical();
							GUILayout.Label("", GUILayout.Width(4));
							GUILayout.EndVertical();

							Rect lastRect = GUILayoutUtility.GetLastRect();

							GUILayout.BeginVertical();
							UIUtils.HorizontalLine();
							moduleFunction(data, i);
							GUILayout.EndVertical();

							Color c = GUI.color;
							GUI.color = Color.cyan;
							GUI.DrawTexture(new Rect(lastRect.x, lastRect.y, lastRect.width, GUILayoutUtility.GetLastRect().height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
							GUI.color = c;

							GUILayout.EndHorizontal();
						}
						tonemappingIndex = Int32.MaxValue;
						if (this._toneMappingManager != null)
							tonemappingIndex = HSLRE.HSLRE.self.generalEffects.FindIndex(e => ReferenceEquals(e.effect, this._toneMappingManager));
						for (int i = 0; i < HSLRE.HSLRE.self.generalEffects.Count; i++)
						{
							HSLRE.HSLRE.EffectData data = HSLRE.HSLRE.self.generalEffects[i];
							if ((data.effectType & this._effectTypeDisplay) == 0)
								continue;
							if (ReferenceEquals(data.effect, this._lensManager) || ReferenceEquals(data.effect, this._vignette))
								continue;
							Action<HSLRE.HSLRE.EffectData, int> moduleFunction;
							if (!this._effectsModules.TryGetValue(data.effect, out moduleFunction))
								continue;
							GUILayout.BeginHorizontal();

							GUILayout.BeginVertical();
							GUILayout.Label("", GUILayout.Width(4));
							GUILayout.EndVertical();

							Rect lastRect = GUILayoutUtility.GetLastRect();

							GUILayout.BeginVertical();
							UIUtils.HorizontalLine();
							moduleFunction(data, i + HSLRE.HSLRE.self.opaqueEffects.Count);
							GUILayout.EndVertical();

							Color c = GUI.color;
							GUI.color = i < tonemappingIndex ? Color.yellow : Color.red;
							GUI.DrawTexture(new Rect(lastRect.x, lastRect.y, lastRect.width, GUILayoutUtility.GetLastRect().height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
							GUI.color = c;

							GUILayout.EndHorizontal();
						}
						break;
					case 3:
						this.CharaRotateModule();
						this.UserCustomModule();
						break;
				}
			}
			GUILayout.EndScrollView();

			GUILayout.FlexibleSpace();

			if (this._tabMenu == 1 || this._tabMenu == 2)
			{
				GUILayout.BeginHorizontal(GUI.skin.box);
				Color c = GUI.color;
				GUI.color = Color.cyan;
				GUILayout.FlexibleSpace();
				GUILayout.Label("Opaque objects", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
				GUI.color = Color.yellow;
				GUILayout.FlexibleSpace();
				GUILayout.Label("Everything HDR", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
				GUI.color = Color.red;
				GUILayout.FlexibleSpace();
				GUILayout.Label("Everything LDR", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
				GUILayout.FlexibleSpace();
				GUI.color = c;
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUI.DragWindow();
			if (Event.current.type == EventType.repaint)
				this._currentTooltip = GUI.tooltip;
		}

		private void CubeMapModule()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Load Cubemaps:", UIUtils.labelStyle);
			GUILayout.FlexibleSpace();
			this._hideSkybox = UIUtils.ToggleButton(this._hideSkybox, new GUIContent("Hide", "Hide skybox in the background"));
			GUILayout.EndHorizontal();
			UIUtils.scrollPosition[0] = GUILayout.BeginScrollView(UIUtils.scrollPosition[0]);

			if (GUILayout.Button("None", UIUtils.buttonStyleStrechWidthAlignLeft))
				this._selectedCubemap = -1;

			this._selectedCubemap = GUILayout.SelectionGrid(this._selectedCubemap, this._cubeMapFileNames, 1, UIUtils.buttonStyleStrechWidthAlignLeft);

			this._selectedCubemap = this.LoadCubemap(this._selectedCubemap);

			GUILayout.EndScrollView();
		}

		private void ProceduralSkyboxModule()
		{
			GUILayout.Label("Procedural Skybox", UIUtils.titleStyle2);
			GUILayout.Space(UIUtils.space);
			this._proceduralSkybox.skyboxparams.exposure = UIUtils.SliderGUI(this._proceduralSkybox.skyboxparams.exposure, 0f, 8f, 1f, "Skybox Exposure:", "", "N3");
			this._proceduralSkybox.skyboxparams.sunsize = UIUtils.SliderGUI(this._proceduralSkybox.skyboxparams.sunsize, 0f, 1f, 0.1f, "Sun Size :", "", "N3");
			this._proceduralSkybox.skyboxparams.atmospherethickness = UIUtils.SliderGUI(this._proceduralSkybox.skyboxparams.atmospherethickness, 0f, 5f, 1f, "Atmosphere Thickness:", "", "N3");
			UIUtils.ColorPickerGUI(this._proceduralSkybox.skyboxparams.skytint, Color.gray, "Sky Tint:", "", (c) =>
			  {
				  this._proceduralSkybox.skyboxparams.skytint = c;
			  });
			UIUtils.ColorPickerGUI(this._proceduralSkybox.skyboxparams.groundcolor, Color.gray, "Gound Color:", "", (c) =>
			  {
				  this._proceduralSkybox.skyboxparams.groundcolor = c;
			  });
			RenderSettings.ambientIntensity = UIUtils.SliderGUI(RenderSettings.ambientIntensity, 0f, 2f, 1f, "Ambient Intensity:", "", "N3");
		}

		private void SkyboxModule()
		{
			GUILayout.Label("Skybox", UIUtils.titleStyle2);
			GUILayout.Space(UIUtils.space);
			this._skybox.skyboxparams.rotation = UIUtils.SliderGUI(this._skybox.skyboxparams.rotation, 0f, 360f, 0f, "Skybox Rotation:", "", "N2");
			this._skybox.skyboxparams.exposure = UIUtils.SliderGUI(this._skybox.skyboxparams.exposure, 0f, 8f, 1f, "Skybox Exposure:", "", "N3");
			UIUtils.ColorPickerGUI(this._skybox.skyboxparams.tint, Color.gray, "Skybox Tint:", "", c =>
			  {
				  this._skybox.skyboxparams.tint = c;
			  });
			RenderSettings.ambientIntensity = UIUtils.SliderGUI(RenderSettings.ambientIntensity, 0f, 2f, 1f, "Ambient Intensity:", "", "N3");
		}

		private void ReflectionModule()
		{
			this._probeComponent.enabled = UIUtils.ToggleGUI(this._probeComponent.enabled, new GUIContent(GUIStrings.reflection), UIUtils.titleStyle2);

			if (this._probeComponent.enabled)
			{
				GUILayout.Space(UIUtils.space);

				if (this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
				{
					if (GUILayout.Button(GUIStrings.reflectionProbeRefresh, UIUtils.buttonStyleNoStretch))
					{
						this._probeComponent.RenderProbe();
					}
				}

				if (this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
				{
					this._reflectionProbeRefreshRate = 0;
				}
				else if (this._probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.AllFacesAtOnce)
				{
					this._reflectionProbeRefreshRate = 2;
				}
				else if (this._probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.IndividualFaces)
				{
					this._reflectionProbeRefreshRate = 1;
				}
				else if (this._probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.NoTimeSlicing)
				{
					this._reflectionProbeRefreshRate = 3;
				}
				this._reflectionProbeRefreshRate = GUILayout.SelectionGrid(this._reflectionProbeRefreshRate, GUIStrings.reflectionProbeRefreshRateArray, 4, UIUtils.selectStyle);

				switch (this._reflectionProbeRefreshRate)
				{
					default:
					case 0:
						this._probeComponent.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
						break;
					case 1:
						this._probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
						this._probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
						break;
					case 2:
						this._probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
						this._probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
						break;
					case 3:
						this._probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
						this._probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
						break;
				}
				GUILayout.Label(GUIStrings.reflectionProbeResolution, UIUtils.labelStyle);
				this._reflectionProbeResolution = 0;
				for (int i = 0; i < this._possibleReflectionProbeResolutions.Length; i++)
				{
					if (this._possibleReflectionProbeResolutions[i] == this._probeComponent.resolution)
					{
						this._reflectionProbeResolution = i;
						break;
					}
				}

				this._reflectionProbeResolution = GUILayout.SelectionGrid(this._reflectionProbeResolution, this._possibleReflectionProbeResolutionsNames, this._possibleReflectionProbeResolutions.Length, UIUtils.selectStyle);

				this._probeComponent.resolution = this._possibleReflectionProbeResolutions[this._reflectionProbeResolution];
				if (_isStudio)
				{
					GUILayout.BeginHorizontal();
					if (GUILayout.Button("Move Reflection Probe to target", UIUtils.buttonStyleNoStretch))
					{
						this.probeGameObject.transform.position = Studio.Studio.Instance.cameraCtrl.targetTex.position;
						if (this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
							this._probeComponent.RenderProbe();
					}
					GUILayout.FlexibleSpace();
					GUILayout.Label(this.probeGameObject.transform.position.ToString(), UIUtils.labelStyle);
					GUILayout.EndHorizontal();
				}
				GUILayout.Space(UIUtils.space);
				this._probeComponent.boxProjection = UIUtils.ToggleGUI(this._probeComponent.boxProjection, new GUIContent("Box Projection", "Box Projection is useful for reflections in enclosed spaces  where some parrallax and movement in the reflection is wanted. If not endbled then cubemap reflection will be treated as coming infinite far away. And within this zone objects with the Standard shader will receive this probe's cubemap."));

				this._probeComponent.intensity = UIUtils.SliderGUI(this._probeComponent.intensity, 0f, 2f, 1f, GUIStrings.reflectionIntensity, "N3");

				this._probeComponent.shadowDistance = UIUtils.SliderGUI(this._probeComponent.shadowDistance, 1f, 100f, 100f, "Shadow Distance", "N1");

				this._probeComponent.nearClipPlane = UIUtils.SliderGUI(this._probeComponent.nearClipPlane, 0f, 25f, 1.01f, "Near Clip Plane", "N3");

				RenderSettings.reflectionBounces = Mathf.RoundToInt(UIUtils.SliderGUI(RenderSettings.reflectionBounces, 1, 5, 1, "Reflection Bounces", "The number of times a reflection includes other reflections. If set to 1, the scene will be rendered once, which means that a reflection will not be able to reflect another reflection and reflective objects will show up black, when seen in other reflective surfaces. If set to 2, the scene will be rendered twice and reflective objects will show reflections from the first pass, when seen in other reflective surfaces.", "0"));

				this._probeComponent.cullingMask = UIUtils.LayerMaskValue(this._probeComponent.cullingMask, new GUIContent("Culling Mask"), this._layers, "D", "E");
			}
		}

		private void DefaultLightModule()
		{
			GUILayout.Label("Directional Light", UIUtils.titleStyle2);
			GUILayout.Space(UIUtils.space);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Front Light", UIUtils.labelStyle);
			GUILayout.FlexibleSpace();
			bool lastFrontLightAnchor = this._frontDirectionalLight.transform.parent != null;
			this._frontLightAnchor = UIUtils.ToggleButton(lastFrontLightAnchor, new GUIContent("Rotate with camera"));
			GUILayout.EndHorizontal();

			if (this._frontLightAnchor)
			{
				this._lightsObj.transform.parent = Camera.main.transform;
				this._frontDirectionalLight.transform.parent = this._lightsObj.transform;
				if (this._frontLightAnchor != lastFrontLightAnchor)
				{
					this._lightsObj.transform.localRotation = _isStudio && Studio.Studio.Instance != null ? Quaternion.Euler(Studio.Studio.Instance.sceneInfo.cameraLightRot[0], Studio.Studio.Instance.sceneInfo.cameraLightRot[1], 0f) : this._lightsObjDefaultRotation;
					this._frontDirectionalLight.transform.localRotation = this._frontLightDefaultRotation;
				}
			}
			else
			{
				this._lightsObj.transform.parent = null;
				this._frontDirectionalLight.transform.parent = null;
				this._frontRotate.x = UIUtils.SliderGUI(this._frontRotate.x, 0f, 360f, 0f, "Vertical rotation", "N1");
				this._frontRotate.y = UIUtils.SliderGUI(this._frontRotate.y, 0f, 360f, 0f, "Horizontal rotation", "N1");
				this._frontDirectionalLight.transform.eulerAngles = this._frontRotate;
			}
			this._frontDirectionalLight.intensity = UIUtils.SliderGUI(this._frontDirectionalLight.intensity, 0f, 8f, 1f, "Intensity:", "N3");
			UIUtils.ColorPickerGUI(this._frontDirectionalLight.color, Color.white, "Color:", c =>
			 {
				 this._frontDirectionalLight.color = c;
			 });

			GUILayout.BeginHorizontal();
			GUILayout.Label("Back Light", UIUtils.labelStyle);
			GUILayout.FlexibleSpace();
			bool lastBackLightAnchor = this._backDirectionalLight.transform.parent != null;
			this._backLightAnchor = UIUtils.ToggleButton(lastBackLightAnchor, new GUIContent("Rotate with camera"));
			GUILayout.EndHorizontal();
			if (this._backLightAnchor)
			{
				this._backDirectionalLight.transform.parent = Camera.main.transform;
				if (this._backLightAnchor != lastBackLightAnchor)
					this._backDirectionalLight.transform.localRotation = this._backLightDefaultRotation;
			}
			else
			{
				this._backDirectionalLight.transform.parent = null;
				this._backRotate.x = UIUtils.SliderGUI(this._backRotate.x, 0f, 360f, 0f, "Vertical rotation", "", "N1");
				this._backRotate.y = UIUtils.SliderGUI(this._backRotate.y, 0f, 360f, 0f, "Horizontal rotation", "", "N1");
				this._backDirectionalLight.transform.eulerAngles = this._backRotate;
			}
			this._backDirectionalLight.intensity = UIUtils.SliderGUI(this._backDirectionalLight.intensity, 0f, 8f, 1f, "Intensity:", "", "N3");
			UIUtils.ColorPickerGUI(this._backDirectionalLight.color, Color.white, GUIStrings.color, "", c =>
			 {
				 this._backDirectionalLight.color = c;
			 });
		}

		private void LensPresetsModule()
		{
			GUILayout.Label("Lens Presets", UIUtils.labelStyle);
			GUILayout.Space(UIUtils.space);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("85mm", UIUtils.buttonStyleStrechWidth))
			{
				Camera.main.fieldOfView = 23.9f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = Camera.main.fieldOfView;
				if (this._subCamera != null)
				{
					this._subCamera.fieldOfView = Camera.main.fieldOfView;
				}
				this._lensManager.distortion.enabled = false;
				this._lensManager.vignette.enabled = true;
				this._lensManager.vignette.intensity = 0.7f;
				this._lensManager.vignette.color = Color.black;
				this._lensManager.vignette.blur = 0f;
				this._lensManager.vignette.desaturate = 0f;
				this._lensManager.vignette.roundness = 0.5625f;
				this._lensManager.vignette.smoothness = 2f;
			}
			if (GUILayout.Button("50mm", UIUtils.buttonStyleStrechWidth))
			{
				Camera.main.fieldOfView = 39.6f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = Camera.main.fieldOfView;
				if (this._subCamera != null)
				{
					this._subCamera.fieldOfView = Camera.main.fieldOfView;
				}
				this._lensManager.distortion.enabled = false;
				this._lensManager.vignette.enabled = true;
				this._lensManager.vignette.intensity = 1f;
				this._lensManager.vignette.color = Color.black;
				this._lensManager.vignette.blur = 0f;
				this._lensManager.vignette.desaturate = 0f;
				this._lensManager.vignette.roundness = 0.5625f;
				this._lensManager.vignette.smoothness = 2f;
			}
			if (GUILayout.Button("35mm", UIUtils.buttonStyleStrechWidth))
			{
				Camera.main.fieldOfView = 57.9f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = Camera.main.fieldOfView;
				if (this._subCamera != null)
				{
					this._subCamera.fieldOfView = Camera.main.fieldOfView;
				}
				this._lensManager.distortion.enabled = false;
				this._lensManager.vignette.enabled = true;
				this._lensManager.vignette.intensity = 1.6f;
				this._lensManager.vignette.color = Color.black;
				this._lensManager.vignette.blur = 0f;
				this._lensManager.vignette.desaturate = 0f;
				this._lensManager.vignette.roundness = 0.5625f;
				this._lensManager.vignette.smoothness = 1.6f;
			}
			if (GUILayout.Button("24mm", UIUtils.buttonStyleStrechWidth))
			{
				Camera.main.fieldOfView = 85.5f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = Camera.main.fieldOfView;
				if (this._subCamera != null)
				{
					this._subCamera.fieldOfView = Camera.main.fieldOfView;
				}
				this._lensManager.distortion.enabled = true;
				this._lensManager.distortion.amount = 25f;
				this._lensManager.distortion.amountX = 1f;
				this._lensManager.distortion.amountY = 1f;
				this._lensManager.distortion.scale = 1.025f;
				this._lensManager.vignette.enabled = true;
				this._lensManager.vignette.intensity = 1.8f;
				this._lensManager.vignette.color = Color.black;
				this._lensManager.vignette.blur = 0.1f;
				this._lensManager.vignette.desaturate = 0f;
				this._lensManager.vignette.roundness = 0.187f;
				this._lensManager.vignette.smoothness = 1.4f;
			}
			if (GUILayout.Button("16mm", UIUtils.buttonStyleStrechWidth))
			{
				Camera.main.fieldOfView = 132.6f;
				if (_isStudio)
					Studio.Studio.Instance.cameraCtrl.fieldOfView = Camera.main.fieldOfView;
				if (this._subCamera != null)
				{
					this._subCamera.fieldOfView = Camera.main.fieldOfView;
				}
				this._lensManager.distortion.enabled = true;
				this._lensManager.distortion.amount = 69f;
				this._lensManager.distortion.amountX = 1f;
				this._lensManager.distortion.amountY = 1f;
				this._lensManager.distortion.scale = 1.05f;
				this._lensManager.vignette.enabled = true;
				this._lensManager.vignette.intensity = 1.95f;
				this._lensManager.vignette.color = Color.black;
				this._lensManager.vignette.blur = 0.14f;
				this._lensManager.vignette.desaturate = 0.14f;
				this._lensManager.vignette.roundness = 0.814f;
				this._lensManager.vignette.smoothness = 1.143f;
			}
			GUILayout.EndHorizontal();
		}

		private void LensModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			//GUILayout.Space(UIUtils.space);
			//////////////////////Lens Aberration/////////////////

			UIUtils.HorizontalLine();
			this._lensManager.distortion.enabled = UIUtils.ToggleGUITitle(this._lensManager.distortion.enabled, new GUIContent(index + " Distortion"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(this._lensManager), () => HSLRE.HSLRE.self.MoveDown(this._lensManager));
			if (this._lensManager.distortion.enabled)
			{
				this._lensManager.distortion.amount = UIUtils.SliderGUI(this._lensManager.distortion.amount, -100f, 100f, 0f, "Distortion Amount", "N2");
				this._lensManager.distortion.amountX = UIUtils.SliderGUI(this._lensManager.distortion.amountX, 0f, 1f, 1f, "Amount multiplier on X axis", "N3");
				this._lensManager.distortion.amountY = UIUtils.SliderGUI(this._lensManager.distortion.amountY, 0f, 1f, 1f, "Amount multiplier on Y axis", "N3");
				this._lensManager.distortion.scale = UIUtils.SliderGUI(this._lensManager.distortion.scale, 0.5f, 1f, 1f, "Global screen scale", "N3");
			}
			//GUILayout.Space(UIUtils.space);
			UIUtils.HorizontalLine();

			this._lensManager.chromaticAberration.enabled = UIUtils.ToggleGUITitle(this._lensManager.chromaticAberration.enabled, new GUIContent((index + 0.1f).ToString("0.0") + " " + GUIStrings.chromaticAberration), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(this._lensManager), () => HSLRE.HSLRE.self.MoveDown(this._lensManager));
			if (this._lensManager.chromaticAberration.enabled)
			{
				this._lensManager.chromaticAberration.amount = UIUtils.SliderGUI(this._lensManager.chromaticAberration.amount, -4f, 4f, 0f, "Tangential distortion Amount", "N3");
				UIUtils.ColorPickerGUI(this._lensManager.chromaticAberration.color, Color.green, "Color", (c) =>
				{
					this._lensManager.chromaticAberration.color = c;
				});
			}
			//GUILayout.Space(UIUtils.space);
			UIUtils.HorizontalLine();
			this._lensManager.vignette.enabled = UIUtils.ToggleGUITitle(this._lensManager.vignette.enabled, new GUIContent((index + 0.2f).ToString("0.0") + " " + GUIStrings.vignette), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(this._lensManager), () => HSLRE.HSLRE.self.MoveDown(this._lensManager));
			if (this._lensManager.vignette.enabled)
			{
				this._lensManager.vignette.intensity = UIUtils.SliderGUI(this._lensManager.vignette.intensity, 0f, 3f, 1.4f, "Intensity", "N3");
				this._lensManager.vignette.smoothness = UIUtils.SliderGUI(this._lensManager.vignette.smoothness, 0.01f, 3f, 0.8f, "Smothness", "N3");
				this._lensManager.vignette.roundness = UIUtils.SliderGUI(this._lensManager.vignette.roundness, 0f, 1f, 1f, "Roundness", "N3");
				this._lensManager.vignette.desaturate = UIUtils.SliderGUI(this._lensManager.vignette.desaturate, 0f, 1f, 0f, "Desaturate", "N3");
				this._lensManager.vignette.blur = UIUtils.SliderGUI(this._lensManager.vignette.blur, 0f, 1f, 0f, "Blur Corner", "N3");

				UIUtils.ColorPickerGUI(this._lensManager.vignette.color, Color.black, GUIStrings.vignetteColor, (c) =>
				{
					this._lensManager.vignette.color = c;
				});
			}
		}

		private void ToneMappingColorGradingModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			this.EyeAdaptationModule(effectData, index);
			UIUtils.HorizontalLine();
			this.ToneMappingModule(effectData, index + 0.1f);
			UIUtils.HorizontalLine();
			this.ColorGradingModule(effectData, index + 0.2f);
		}

		private void EyeAdaptationModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			this._eyeEnabled = UIUtils.ToggleGUITitle(this._toneMappingManager.eyeAdaptation.enabled, new GUIContent(index + " " + GUIStrings.eyeAdaptation.text, GUIStrings.eyeAdaptation.tooltip), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));
			if (this._eyeEnabled)
			{
				GUILayout.Space(UIUtils.space);
				this._eyeMiddleGrey = UIUtils.SliderGUI(this._toneMappingManager.eyeAdaptation.middleGrey, 0f, 0.5f, 0.1f, "Middle Grey", "Midpoint Adjustment.", "N3");
				this._eyeMin = UIUtils.SliderGUI(this._toneMappingManager.eyeAdaptation.min, -8f, 0f, -4f, "Lowest Exposure Value", "The lowest possible exposure value; adjust this value to modify the brightest areas of your level.", "N3");
				this._eyeMax = UIUtils.SliderGUI(this._toneMappingManager.eyeAdaptation.max, 0f, 8f, 4f, "Highest Exposure Value", "The highest possible exposure value; adjust this value to modify the darkest areas of your level.", "N3");
				this._eyeSpeed = UIUtils.SliderGUI(this._toneMappingManager.eyeAdaptation.speed, 0f, 8f, "Adaptation Speed", "Speed of linear adaptation. Higher is faster.", "N3");
			}
			this._toneMappingManager.eyeAdaptation = new HSLRE.CustomEffects.TonemappingColorGrading.EyeAdaptationSettings
			{
				enabled = this._eyeEnabled,
				showDebug = false,
				middleGrey = this._eyeMiddleGrey,
				max = this._eyeMax,
				min = this._eyeMin,
				speed = this._eyeSpeed
			};
		}

		private void ToneMappingModule(HSLRE.HSLRE.EffectData effectData, float i)
		{
			this._tonemappingEnabled = UIUtils.ToggleGUITitle(this._toneMappingManager.tonemapping.enabled, new GUIContent(i.ToString("0.0") + " " + GUIStrings.tonemapping.text, GUIStrings.tonemapping.tooltip), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (this._tonemappingEnabled)
			{
				GUILayout.Space(UIUtils.space);
				int index = 0;
				switch (this._toneMappingManager.tonemapping.tonemapper)
				{
					default:
					case HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.ACES:
						index = 0;
						break;
					case HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.Hable:
						index = 1;
						break;
					case HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.HejlDawson:
						index = 2;
						break;
					case HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.Photographic:
						index = 3;
						break;
					case HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.Reinhard:
						index = 4;
						break;
				}
				GUILayout.Space(5f);
				index = GUILayout.SelectionGrid(index, new string[]
				{
					"ACES",
					"Hable",
					"HejlDawson",
					"Photographic",
					"Reinhard"
				}, 3, UIUtils.selectStyle);

				switch (index)
				{
					default:
					case 0:
						this._toneMapper = HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.ACES;
						break;
					case 1:
						this._toneMapper = HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.Hable;
						break;
					case 2:
						this._toneMapper = HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.HejlDawson;
						break;
					case 3:
						this._toneMapper = HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.Photographic;
						break;
					case 4:
						this._toneMapper = HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper.Reinhard;
						break;
				}
				this._ev = UIUtils.SliderGUI(this._ev, -5f, 5f, 0f, new GUIContent(GUIStrings.exposureValue, "Adjusts the overall exposure of the scene."), "N3");
			}

			this._toneMappingManager.tonemapping = new HSLRE.CustomEffects.TonemappingColorGrading.TonemappingSettings
			{
				tonemapper = this._toneMapper,
				exposure = Mathf.Pow(2f, this._ev),
				enabled = this._tonemappingEnabled,
				neutralBlackIn = this._toneMappingManager.tonemapping.neutralBlackIn,
				neutralBlackOut = this._toneMappingManager.tonemapping.neutralBlackOut,
				neutralWhiteClip = this._toneMappingManager.tonemapping.neutralWhiteClip,
				neutralWhiteIn = this._toneMappingManager.tonemapping.neutralWhiteIn,
				neutralWhiteLevel = this._toneMappingManager.tonemapping.neutralWhiteLevel,
				neutralWhiteOut = this._toneMappingManager.tonemapping.neutralWhiteOut,
				curve = this._toneMappingManager.tonemapping.curve
			};

		}

		private void ColorGradingModule(HSLRE.HSLRE.EffectData effectData, float index)
		{
			HSLRE.CustomEffects.TonemappingColorGrading.ColorGradingSettings colorGrading = this._toneMappingManager.colorGrading;
			colorGrading.enabled = UIUtils.ToggleGUITitle(colorGrading.enabled, new GUIContent(index.ToString("0.0") + " Color Grading", "Color Grading is the process of altering or correcting the color and luminance of the final image."), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (colorGrading.enabled)
			{
				HSLRE.CustomEffects.TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;

				settings.temperatureShift = UIUtils.SliderGUI(settings.temperatureShift, -2f, 2f, () => HSLRE.Settings.tonemappingSettings.temperatureShift, GUIStrings.tonemappingTemperatureShift, "Sets the white balance to a custom color temperature.", "0.00");
				settings.tint = UIUtils.SliderGUI(settings.tint, -2f, 2f, () => HSLRE.Settings.tonemappingSettings.tint, GUIStrings.tonemappingTint, "Sets the white balance to compensate for a green or magenta tint.", "0.00");
				settings.contrast = UIUtils.SliderGUI(settings.contrast, 0f, 5f, () => HSLRE.Settings.tonemappingSettings.contrast, GUIStrings.tonemappingContrast, "Expands or shrinks the overall range of tonal values.", "0.00");
				settings.hue = UIUtils.SliderGUI(settings.hue, -0.5f, 0.5f, () => HSLRE.Settings.tonemappingSettings.hue, GUIStrings.tonemappingHue, "Shift the hue of all colors.", "0.00");
				settings.saturation = UIUtils.SliderGUI(settings.saturation, 0f, 3f, () => HSLRE.Settings.tonemappingSettings.saturation, GUIStrings.tonemappingSaturation, "Pushes the intensity of all colors.", "0.00");
				settings.value = UIUtils.SliderGUI(settings.value, 0f, 10f, () => HSLRE.Settings.tonemappingSettings.value, GUIStrings.tonemappingValue, "Brightens or darkens all colors.", "0.00");
				settings.vibrance = UIUtils.SliderGUI(settings.vibrance, -1f, 1f, () => HSLRE.Settings.tonemappingSettings.vibrance, GUIStrings.tonemappingVibrance, "Adjusts the saturation so that clipping is minimized as colors approach full saturation.", "0.00");
				settings.gain = UIUtils.SliderGUI(settings.gain, 0f, 5f, () => HSLRE.Settings.tonemappingSettings.gain, GUIStrings.tonemappingGain, "Contrast gain curve. Controls the steepness of the curve.", "0.00");
				settings.gamma = UIUtils.SliderGUI(settings.gamma, 0f, 5f, () => HSLRE.Settings.tonemappingSettings.gamma, GUIStrings.tonemappingGamma, "Applies a pow function to the source.", "0.00");
				colorGrading.basics = settings;
			}

			this._toneMappingManager.colorGrading = colorGrading;
		}

		private void SMAAModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " SMAA"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				GUILayout.Space(UIUtils.space);
				SMAA.GlobalSettings settings = this._smaa.settings;
				SMAA.PredicationSettings predication = this._smaa.predication;
				SMAA.TemporalSettings temporal = this._smaa.temporal;

				GUILayout.Label(new GUIContent("Debug Pass", "Use this to fine tune your settings when working in Custom quality mode. \"Accumulation\" only works when \"Temporal Filtering\" is enabled."), UIUtils.labelStyle);
				settings.debugPass = (SMAA.DebugPass)GUILayout.SelectionGrid((int)settings.debugPass, this._possibleSMAADebugPassNames, 3, UIUtils.buttonStyleStrechWidth);
				GUILayout.Label(new GUIContent("Quality", "Low: 60% of the quality.\nMedium: 80% of the quality.\nHigh: 95% of the quality.\nUltra: 99% of the quality (overkill)."), UIUtils.labelStyle);
				settings.quality = (SMAA.QualityPreset)GUILayout.SelectionGrid((int)settings.quality, this._possibleSMAAQualityPresetNames, 5, UIUtils.buttonStyleStrechWidth);
				GUILayout.Label(new GUIContent("Edge Detection Method", "You have three edge detection methods to choose from: luma, color or depth.\nThey represent different quality/performance and anti-aliasing/sharpness tradeoffs, so our recommendation is for you to choose the one that best suits your particular scenario:\n\n- Depth edge detection is usually the fastest but it may miss some edges.\n- Luma edge detection is usually more expensive than depth edge detection, but catches visible edges that depth edge detection can miss.\n- Color edge detection is usually the most expensive one but catches chroma-only edges."), UIUtils.labelStyle);
				settings.edgeDetectionMethod = (SMAA.EdgeDetectionMethod)GUILayout.SelectionGrid((int)settings.edgeDetectionMethod, this._possibleSMAAEdgeDetectionMethodNames, 3, UIUtils.buttonStyleStrechWidth);
				if (settings.quality == SMAA.QualityPreset.Custom)
				{
					SMAA.QualitySettings quality = this._smaa.quality;
					quality.diagonalDetection = UIUtils.ToggleGUI(quality.diagonalDetection, new GUIContent("Diagonal Detection", "Enables/Disables diagonal processing."));
					quality.cornerDetection = UIUtils.ToggleGUI(quality.cornerDetection, new GUIContent("Corner Detection", "Enables/Disables corner detection. Leave this on to avoid blurry corners."));
					quality.threshold = UIUtils.SliderGUI(quality.threshold, 0f, 0.5f, 0.01f, "Threshold", "Filters out pixels under this level of brightness.", "N3");
					quality.depthThreshold = UIUtils.SliderGUI(quality.depthThreshold, 0.0001f, 10f, 0.01f, "Depth Threshold", "Specifies the threshold for depth edge detection. Lowering this value you will be able to detect more edges at the expense of performance.", "N4");
					quality.maxSearchSteps = (int)UIUtils.SliderGUI(quality.maxSearchSteps, 0f, 112, 16, "Max Search Steps", "Specifies the maximum steps performed in the horizontal/vertical pattern searches, at each side of the pixel.\nIn number of pixels, it's actually the double. So the maximum line length perfectly handled by, for example 16, is 64 (by perfectly, we meant that longer lines won't look as good, but still antialiased).", "N");
					quality.maxDiagonalSearchSteps = (int)UIUtils.SliderGUI(quality.maxDiagonalSearchSteps, 0f, 20f, 8, "Max Diagonal Search Steps", "Specifies the maximum steps performed in the diagonal pattern searches, at each side of the pixel. In this case we jump one pixel at time, instead of two.\nOn high-end machines it is cheap (between a 0.8x and 0.9x slower for 16 steps), but it can have a significant impact on older machines.", "N");
					quality.cornerRounding = (int)UIUtils.SliderGUI(quality.cornerRounding, 0f, 100f, 25f, "Corner Rounding", "Specifies how much sharp corners will be rounded.", "N");
					quality.localContrastAdaptationFactor = UIUtils.SliderGUI(quality.localContrastAdaptationFactor, 0f, 10f, 2f, "Local Contrast Adaptation Factor", "If there is a neighbor edge that has a local contrast factor times bigger contrast than current edge, current edge will be discarded.\nThis allows to eliminate spurious crossing edges, and is based on the fact that, if there is too much contrast in a direction, that will hide perceptually contrast in the other neighbors.", "N3");
					this._smaa.quality = quality;
				}

				predication.enabled = UIUtils.ToggleGUI(predication.enabled, new GUIContent("Predication", "Predicated thresholding allows to better preserve texture details and to improve performance, by decreasing the number of detected edges using an additional buffer (the detph buffer).\nIt locally decreases the luma or color threshold if an edge is found in an additional buffer (so the global threshold can be higher)."));
				if (predication.enabled)
				{
					predication.threshold = UIUtils.SliderGUI(predication.threshold, 0.0001f, 10f, 0.01f, "Threshold", "Threshold to be used in the additional predication buffer.", "N4");
					predication.scale = UIUtils.SliderGUI(predication.scale, 1f, 5f, 2, "Scale", "How much to scale the global threshold used for luma or color edge detection when using predication.", "N3");
					predication.strength = UIUtils.SliderGUI(predication.strength, 0f, 1f, 0.4f, "Strength", "How much to locally decrease the threshold.", "N4");
				}

				temporal.enabled = UIUtils.ToggleGUI(temporal.enabled, new GUIContent("Temporal", "Temporal filtering makes it possible for the SMAA algorithm to benefit from minute subpixel information available that has been accumulated over many frames."));
				if (temporal.enabled)
				{
					temporal.fuzzSize = UIUtils.SliderGUI(temporal.fuzzSize, 0.5f, 10f, 2f, "Fuzz Size", "The size of the fuzz-displacement (jitter) in pixels applied to the camera's perspective projection matrix.\nUsed for 2x temporal anti-aliasing.", "N3");
				}

				this._smaa.predication = predication;
				this._smaa.temporal = temporal;
				this._smaa.settings = settings;
			}
		}

		private void BloomModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " " + GUIStrings.bloom), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				GUILayout.Space(UIUtils.space);
				this._cinematicBloom.settings.intensity = UIUtils.SliderGUI(this._cinematicBloom.settings.intensity, 0f, 1f, 0.1f, "Intensity", "Blend factor of the result image.", "N3");
				this._cinematicBloom.settings.threshold = UIUtils.SliderGUI(this._cinematicBloom.settings.threshold, 0f, 8f, 1f, "Threshold", "Filters out pixels under this level of brightness.", "N3");
				this._cinematicBloom.settings.softKnee = UIUtils.SliderGUI(this._cinematicBloom.settings.softKnee, 0f, 1f, 0.2f, "Softknee", "Makes transition between under/over-threshold gradual.", "N3");
				this._cinematicBloom.settings.radius = UIUtils.SliderGUI(this._cinematicBloom.settings.radius, 0f, 16f, 3f, "Radius", "Changes extent of veiling effects in a screen resolution-independent fashion.", "N3");
				this._cinematicBloom.settings.antiFlicker = UIUtils.ToggleGUI(this._cinematicBloom.settings.antiFlicker, GUIStrings.bloomAntiflicker);
			}
		}

		private void SSAOModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUI(effectData.enabled, new GUIContent(index + " SSAO"), UIUtils.titleStyle2);
			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
				Studio.Studio.Instance.sceneInfo.enableSSAO = effectData.enabled;
			if (effectData.enabled)
			{
				//GUILayout.Label("SSAO", UIUtils.titlestyle2);
				GUILayout.Space(UIUtils.space);
				this._ssao.DebugAO = UIUtils.ToggleGUI(this._ssao.DebugAO, new GUIContent("Debug AO"));
				this._ssao.UseHighPrecisionDepthMap = UIUtils.ToggleGUI(this._ssao.UseHighPrecisionDepthMap, new GUIContent("Use high precision depth map"));

				GUILayout.Label(new GUIContent("Sample Count", "The number of ambient occlusion samples for each pixel on screen. More samples means slower but smoother rendering. Five presets are available"), UIUtils.labelStyle);
				this._ssao.Samples = (SSAOPro.SampleCount)GUILayout.SelectionGrid((int)this._ssao.Samples, this._possibleSSAOSampleCountNames, 3, UIUtils.buttonStyleStrechWidth);
				this._ssao.Downsampling = Mathf.RoundToInt(UIUtils.SliderGUI(this._ssao.Downsampling, 1f, 4f, 1f, "Downsampling", "Lets you change resolution at which calculations should be performed (for example, a downsampling value of 2 will work at half the screen resolution). Using downsampling increases rendering speed at the cost of quality.", "0"));
				this._ssao.Intensity = UIUtils.SliderGUI(this._ssao.Intensity, 0.0f, 16f, 2f, "Intensity", "The occlusion multiplier (degree of darkness added by ambient occlusion). Push this up or down to get a more or less visible effect.", "N3");
				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
					Studio.Studio.Instance.sceneInfo.ssaoIntensity = this._ssao.Intensity;
				this._ssao.Radius = UIUtils.SliderGUI(this._ssao.Radius, 0.01f, 1.25f, 0.125f, "Radius", "The maximum radius of a gap (in world units) that will introduce ambient occlusion.", "N3");
				this._ssao.Distance = UIUtils.SliderGUI(this._ssao.Distance, 0.0f, 10f, 1f, "Distance", "Represents the distance between an occluded sample and its occluder.", "N3");
				this._ssao.Bias = UIUtils.SliderGUI(this._ssao.Bias, 0.0f, 1f, 0.1f, "Bias", "The Bias value is added to the occlusion cone. If you’re getting artifacts you may want to push this up while playing with the Distance parameter.", "N3");
				this._ssao.LumContribution = UIUtils.SliderGUI(this._ssao.LumContribution, 0.0f, 1f, 0.5f, "Lighting Contribution", "Defines how much ambient occlusion should be added in bright areas. By pushing this up, bright areas will have less ambient occlusion which generally leads to more pleasing results.", "N3");
				UIUtils.ColorPickerGUI(this._ssao.OcclusionColor, Color.black, "Occlusion Color", c =>
				 {
					 this._ssao.OcclusionColor = c;
					 if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
						 Studio.Studio.Instance.sceneInfo.ssaoColor.SetDiffuseRGBA(c);
				 });

				GUILayout.Label(new GUIContent("Blur Mode", "None: no blur will be applied to the ambient occlusion pass. Gaussian: an optimized 9 - tap filter. Bilateral: a bilateral box filter capable of detecting borders. High Quality Bilateral: a smooth bilateral gaussian filter capable of detecting borders."), UIUtils.labelStyle);
				this._ssao.Blur = (SSAOPro.BlurMode)GUILayout.SelectionGrid((int)this._ssao.Blur, this._possibleSSAOBlurModeNames, 2, UIUtils.buttonStyleStrechWidth);
				if (this._ssao.Blur != SSAOPro.BlurMode.None)
				{
					this._ssao.BlurDownsampling = UIUtils.ToggleGUI(this._ssao.BlurDownsampling, new GUIContent("Blur Downsampling", "If enabled, the blur pass will be applied to the downsampled render before it gets resized to fit the screen. Else, it will be applied after the resize, which increases quality but is a bit slower."));
					this._ssao.BlurPasses = Mathf.RoundToInt(UIUtils.SliderGUI(this._ssao.BlurPasses, 1f, 4f, 1, "Blur Passes", "Applies more blur to give a smoother effect at the cost of performance.", "0"));
					if (this._ssao.Blur == SSAOPro.BlurMode.HighQualityBilateral)
						this._ssao.BlurBilateralThreshold = UIUtils.SliderGUI(this._ssao.BlurBilateralThreshold, 0.05f, 1f, 0.1f, "Depth Threshold", "Tweak this to adjust the blur \"sharpness\".", "N3");
				}
				this._ssao.CutoffDistance = UIUtils.SliderGUI(this._ssao.CutoffDistance, 0.1f, 400f, 150f, "Max Distance", "Used to stop applying ambient occlusion for distant objects (very useful when using fog). ", "N1");
				this._ssao.CutoffFalloff = UIUtils.SliderGUI(this._ssao.CutoffFalloff, 0.1f, 100f, 50f, "Falloff", "Used to ease out the cutoff, i.e. set it to 0 and the SSAO will stop abruptly at Max Distance; set it to 50 and the SSAO will smoothly disappear starting at (Max Distance) - (Falloff)", "N1");

				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("http://www.thomashourdel.com/ssaopro/doc/usage.html");
			}
		}

		private void SunShaftsModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Sun Shafts"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));
			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
				Studio.Studio.Instance.sceneInfo.enableSunShafts = effectData.enabled;
			if (effectData.enabled)
			{
				//GUILayout.Label("Sun Shafts", UIUtils.titlestyle2);
				GUILayout.Space(UIUtils.space);
				this._sunShafts.useDepthTexture = UIUtils.ToggleGUI(this._sunShafts.useDepthTexture, new GUIContent("Use Depth Buffer"));
				GUILayout.Label(new GUIContent("Resolution", "The resolution at which the shafts are generated. Lower resolutions are faster to calculate and create softer results."), UIUtils.labelStyle);
				this._sunShafts.resolution = (SunShafts.SunShaftsResolution)GUILayout.SelectionGrid((int)this._sunShafts.resolution, this._possibleSunShaftsResolutionNames, 3, UIUtils.buttonStyleStrechWidth);
				GUILayout.Label("Screen Blend Mode", UIUtils.labelStyle);
				this._sunShafts.screenBlendMode = (SunShafts.ShaftsScreenBlendMode)GUILayout.SelectionGrid((int)this._sunShafts.screenBlendMode, this._possibleShaftsScreenBlendModeNames, 2, UIUtils.buttonStyleStrechWidth);
				UIUtils.ColorPickerGUI(this._sunShafts.sunThreshold, new Color(0.87f, 0.74f, 0.65f), "Threshold Color", c =>
				 {
					 this._sunShafts.sunThreshold = c;
				 });
				UIUtils.ColorPickerGUI(this._sunShafts.sunColor, Color.white, "Shafts Color", c =>
				 {
					 this._sunShafts.sunColor = c;
				 });
				this._sunShafts.maxRadius = UIUtils.SliderGUI(this._sunShafts.maxRadius, 0f, 1f, 0.75f, "Max Radius", "The degree to which the shafts' brightness diminishes with distance from the Sun object.", "N3");
				this._sunShafts.sunShaftBlurRadius = UIUtils.SliderGUI(this._sunShafts.sunShaftBlurRadius, 0.01f, 20f, 2.5f, "Blur Radius", "The radius over which pixel colours are combined during blurring.", "N3");
				this._sunShafts.radialBlurIterations = Mathf.RoundToInt(UIUtils.SliderGUI(this._sunShafts.radialBlurIterations, 0f, 8f, 2f, "Radial Blur Iterations", "The number of repetitions of the blur operation. More iterations will give smoother blurring but each has a cost in processing time.", "0"));
				this._sunShafts.sunShaftIntensity = UIUtils.SliderGUI(this._sunShafts.sunShaftIntensity, 0f, 20f, 1.15f, "Intensity", "The brightness of the generated shafts.", "N3");
				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-SunShafts.html");
			}
		}

		private void DepthOfFieldModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Depth Of Field"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));
			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
				Studio.Studio.Instance.sceneInfo.enableDepth = effectData.enabled;
			//GUILayout.Label("Depth Of Field", UIUtils.titlestyle2);
			if (effectData.enabled)
			{
				GUILayout.Space(UIUtils.space);

				this._depthOfField.visualizeFocus = UIUtils.ToggleGUI(this._depthOfField.visualizeFocus, new GUIContent("Visualize Focus", "Overlay color indicating camera focus."));

				GUILayout.Label(new GUIContent("Focus Type", "Decides which focus point to use."), UIUtils.labelStyle);
				DOFFocusType newFocusType = (DOFFocusType)GUILayout.SelectionGrid((int)this._focusType, this._possibleDOFFocusTypeNames, 3, UIUtils.buttonStyleStrechWidth);

				if (this._focusType != newFocusType)
				{
					this._focusType = newFocusType;
					switch (this._focusType)
					{
						case DOFFocusType.Crosshair:
							this._depthOfField.focalTransform = this._depthOfFieldOriginalFocusPoint;
							break;
						case DOFFocusType.Object:
							if (_isStudio == false)
							{
								this._depthOfField.focalTransform = this._depthOfFieldOriginalFocusPoint;
								this._focusType = DOFFocusType.Crosshair;
								break;
							}
							ObjectCtrlInfo info = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault().Value;
							if (info != null)
							{
								this._depthOfField.focalTransform = info.guideObject.transformTarget;
								this._focussedObject = info;
							}
							else
							{
								this._depthOfField.focalTransform = this._depthOfFieldOriginalFocusPoint;
								this._focusType = DOFFocusType.Crosshair;
							}
							break;
						case DOFFocusType.Manual:
							this._depthOfField.focalTransform = null;
							break;
					}
				}

				switch (this._focusType)
				{
					case DOFFocusType.Object:

						if (this._depthOfField.focalTransform == null || this._focussedObject == null)
						{
							this._focusType = DOFFocusType.Manual;
							break;
						}
						GUILayout.BeginHorizontal();
						GUILayout.Label("Current Focus: " + this._focussedObject.treeNodeObject.textName, UIUtils.labelStyle);
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Select", UIUtils.buttonStyleStrechWidth))
						{
							KeyValuePair<TreeNodeObject, ObjectCtrlInfo> kvp = Studio.Studio.Instance.dicInfo.FirstOrDefault(e => e.Value == this._focussedObject);
							if (kvp.Key != null)
								Studio.Studio.Instance.treeNodeCtrl.SelectSingle(kvp.Key);
						}
						GUILayout.EndHorizontal();
						if (GUILayout.Button("Set selected as focus", UIUtils.buttonStyleStrechWidth))
						{
							ObjectCtrlInfo info;
							if (Studio.Studio.Instance.dicInfo.TryGetValue(Studio.Studio.Instance.treeNodeCtrl.selectNode, out info))
							{
								this._depthOfField.focalTransform = info.guideObject.transformTarget;
								this._focussedObject = info;
							}
						}
						break;
					case DOFFocusType.Manual:
						this._depthOfField.focalLength = UIUtils.SliderGUI(this._depthOfField.focalLength, 0.01f, 50f, 10f, "Focal Distance", "The distance to the focal plane from the camera position in world space.", "N2");
						break;
				}

				this._depthOfField.focalSize = UIUtils.SliderGUI(this._depthOfField.focalSize, 0f, 2f, 0.05f, "Focal Size", "Increase the total focal area.", "N3");
				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
					Studio.Studio.Instance.sceneInfo.depthFocalSize = this._depthOfField.focalSize;
				this._depthOfField.aperture = UIUtils.SliderGUI(this._depthOfField.aperture, 0f, 1f, 0.5f, "Aperture", "The camera’s aperture defining the transition between focused and defocused areas. It is good practice to keep this value as high as possible, as otherwise sampling artifacts might occur, especially when the Max Blur Distance is big. Bigger Aperture values will automatically downsample the image to produce a better defocus.", "N3");
				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
					Studio.Studio.Instance.sceneInfo.depthAperture = this._depthOfField.aperture;
				this._depthOfField.maxBlurSize = UIUtils.SliderGUI(this._depthOfField.maxBlurSize, 0f, 128f, 2f, "Max Blur Distance", "Max distance for filter taps. Affects texture cache and can cause undersampling artifacts if value is too big. A value smaller than 4.0 should produce decent results.", "N1");
				this._depthOfField.highResolution = UIUtils.ToggleGUI(this._depthOfField.highResolution, new GUIContent("High Resolution", "Perform defocus operations in full resolution. Affects performance but might help reduce unwanted artifacts and produce more defined bokeh shapes."));
				this._depthOfField.nearBlur = UIUtils.ToggleGUI(this._depthOfField.nearBlur, new GUIContent("Near Blur", "Foreground areas will overlap at a performance cost."));
				if (this._depthOfField.nearBlur)
					this._depthOfField.foregroundOverlap = UIUtils.SliderGUI(this._depthOfField.foregroundOverlap, 0.1f, 2f, 1, "Overlap Size", "Increase foreground overlap dilation if needed.", "N3");
				GUILayout.Label(new GUIContent("Blur Type", "Algorithm used to produce defocused areas. DX11 is effectively a bokeh splatting technique while DiscBlur indicates a more traditional (scatter as gather) based blur."), UIUtils.labelStyle);
				this._depthOfField.blurType = (DepthOfField.BlurType)GUILayout.SelectionGrid((int)this._depthOfField.blurType, this._possibleDOFBlurTypeNames, 2, UIUtils.buttonStyleStrechWidth);
				switch (this._depthOfField.blurType)
				{
					case DepthOfField.BlurType.DiscBlur:
						GUILayout.Label(new GUIContent("Sample Count", "Amount of filter taps. Greatly affects performance."), UIUtils.labelStyle);
						this._depthOfField.blurSampleCount = (DepthOfField.BlurSampleCount)GUILayout.SelectionGrid((int)this._depthOfField.blurSampleCount, this._possibleBlurSampleCountNames, 3, UIUtils.buttonStyleStrechWidth);
						break;
					case DepthOfField.BlurType.DX11:
						GUILayout.Label("DX11 Bokeh Settings", UIUtils.labelStyle);
						this._depthOfField.dx11BokehScale = UIUtils.SliderGUI(this._depthOfField.dx11BokehScale, 0f, 50f, 1.2f, "Bokeh Scale", "Size of bokeh texture.", "N3");
						this._depthOfField.dx11BokehIntensity = UIUtils.SliderGUI(this._depthOfField.dx11BokehIntensity, 0f, 100f, 2.5f, "Bokeh Intensity", "Blend strength of bokeh shapes.", "N2");
						this._depthOfField.dx11BokehThreshold = UIUtils.SliderGUI(this._depthOfField.dx11BokehThreshold, 0f, 2f, 0.5f, "Min Luminance", "Only pixels brighter than this value will cast bokeh shapes. Affects performance as it limits overdraw to a more reasonable amount.", "N3");
						this._depthOfField.dx11SpawnHeuristic = UIUtils.SliderGUI(this._depthOfField.dx11SpawnHeuristic, 0.01f, 1f, 0.0875f, "Spawn Heuristic", "Bokeh shapes will only be cast if pixel in questions passes a frequency check. A threshold around 0.1 seems like a good tradeoff between performance and quality.", "N4");
						break;
				}
				HSLRE.HSLRE.self.fixDofForUpscaledScreenshots = UIUtils.ToggleGUI(HSLRE.HSLRE.self.fixDofForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-DepthOfField.html");
			}
		}

		private void SSRModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUI(effectData.enabled, new GUIContent(index + " Screen Space Reflections"), UIUtils.titleStyle2);
			//GUILayout.Label("Screen Space Reflections", UIUtils.titlestyle2);
			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
				Studio.Studio.Instance.sceneInfo.enableSSR = effectData.enabled;
			if (effectData.enabled)
			{
				GUILayout.Space(UIUtils.space);

				ScreenSpaceReflection.SSRSettings ssrSettings = this._ssr.settings;

				GUILayout.Label("Debug Settings", UIUtils.labelStyle);
				ScreenSpaceReflection.DebugSettings debugSettings = ssrSettings.debugSettings;
				GUILayout.Label(new GUIContent("Debug Mode", "Various Debug Visualizations"), UIUtils.labelStyle);
				debugSettings.debugMode = (ScreenSpaceReflection.SSRDebugMode)GUILayout.SelectionGrid((int)debugSettings.debugMode, this._possibleSSRDebugModeNames, 2, UIUtils.buttonStyleStrechWidth);

				GUILayout.Label("Basic Settings", UIUtils.labelStyle);
				ScreenSpaceReflection.BasicSettings basicSettings = ssrSettings.basicSettings;
				{
					basicSettings.reflectionMultiplier = UIUtils.SliderGUI(basicSettings.reflectionMultiplier, 0.0f, 2f, 1f, "Reflection Multiplier", "Nonphysical multiplier for the SSR reflections. 1.0 is physically based.", "N3");
					basicSettings.maxDistance = UIUtils.SliderGUI(basicSettings.maxDistance, 0.5f, 1000f, 100f, "Max Distance", "Maximum reflection distance in world units.", "N1");
					basicSettings.fadeDistance = UIUtils.SliderGUI(basicSettings.fadeDistance, 0.0f, 1000f, 100f, "Fade Distance", "How far away from the maxDistance to begin fading SSR.", "N1");
					basicSettings.screenEdgeFading = UIUtils.SliderGUI(basicSettings.screenEdgeFading, 0.0f, 1f, 0.03f, "Screen Edge Fading", "Higher = fade out SSRR near the edge of the screen so that reflections don't pop under camera motion.", "N3");
					basicSettings.enableHDR = UIUtils.ToggleGUI(basicSettings.enableHDR, new GUIContent("Enable HDR", "Enable for better reflections of very bright objects at a performance cost"));
					basicSettings.additiveReflection = UIUtils.ToggleGUI(basicSettings.additiveReflection, new GUIContent("Additive Reflection", "Add reflections on top of existing ones. Not physically correct."));
				}

				GUILayout.Label("Reflection Settings", UIUtils.labelStyle);
				ScreenSpaceReflection.ReflectionSettings reflectionSettings = ssrSettings.reflectionSettings;
				{
					reflectionSettings.maxSteps = Mathf.RoundToInt(UIUtils.SliderGUI(reflectionSettings.maxSteps, 16f, 2048f, 0f, "Max Steps", "Max raytracing length.", "0"));
					reflectionSettings.rayStepSize = Mathf.RoundToInt(UIUtils.SliderGUI(reflectionSettings.rayStepSize, 0.0f, 4f, 3f, "Ray Step Size", "Log base 2 of ray tracing coarse step size. Higher traces farther, lower gives better quality silhouettes.", "0"));
					reflectionSettings.widthModifier = UIUtils.SliderGUI(reflectionSettings.widthModifier, 0.01f, 10f, 0.5f, "Width Modifier", "Typical thickness of columns, walls, furniture, and other objects that reflection rays might pass behind.", "N3");
					reflectionSettings.smoothFallbackThreshold = UIUtils.SliderGUI(reflectionSettings.smoothFallbackThreshold, 0.0f, 1f, 0f, "Smooth Fallback Threshold", "Increase if reflections flicker on very rough surfaces.", "N3");
					reflectionSettings.smoothFallbackDistance = UIUtils.SliderGUI(reflectionSettings.smoothFallbackDistance, 0.0f, 0.2f, 0f, "Smooth Fallback Distance", "Start falling back to non-SSR value solution at smoothFallbackThreshold - smoothFallbackDistance, with full fallback occuring at smoothFallbackThreshold.", "N3");
					reflectionSettings.fresnelFade = UIUtils.SliderGUI(reflectionSettings.fresnelFade, 0.0f, 1f, 1f, "Fresnel Fade", "Amplify Fresnel fade out. Increase if floor reflections look good close to the surface and bad farther 'under' the floor.", "N3");
					reflectionSettings.fresnelFadePower = UIUtils.SliderGUI(reflectionSettings.fresnelFadePower, 0.1f, 10f, 1f, "Fresnel Fade Power", "Higher values correspond to a faster Fresnel fade as the reflection changes from the grazing angle.", "N3");
					reflectionSettings.distanceBlur = UIUtils.SliderGUI(reflectionSettings.distanceBlur, 0.0f, 1f, 0f, "Distance Blur", "Controls how blurry reflections get as objects are further from the camera. 0 is constant blur no matter trace distance or distance from camera. 1 fully takes into account both factors.", "N3");
				}

				GUILayout.Label("Advanced Settings", UIUtils.labelStyle);
				ScreenSpaceReflection.AdvancedSettings advancedSettings = ssrSettings.advancedSettings;
				{
					advancedSettings.temporalFilterStrength = UIUtils.SliderGUI(advancedSettings.temporalFilterStrength, 0.0f, 0.99f, 0f, "Temporal Filter Strength", "Increase to decrease flicker in scenes; decrease to prevent ghosting (especially in dynamic scenes). 0 gives maximum performance.", "N3");
					advancedSettings.useTemporalConfidence = UIUtils.ToggleGUI(advancedSettings.useTemporalConfidence, new GUIContent("Use Temporal Confidence", "Enable to limit ghosting from applying the temporal filter."));
					advancedSettings.traceBehindObjects = UIUtils.ToggleGUI(advancedSettings.traceBehindObjects, new GUIContent("Trace Behind Objects", "Enable to allow rays to pass behind objects. This can lead to more screen-space reflections, but the reflections are more likely to be wrong."));
					advancedSettings.highQualitySharpReflections = UIUtils.ToggleGUI(advancedSettings.highQualitySharpReflections, new GUIContent("High Quality Sharp Reflections", "Enable to increase quality of the sharpest reflections (through filtering), at a performance cost."));
					advancedSettings.traceEverywhere = UIUtils.ToggleGUI(advancedSettings.traceEverywhere, new GUIContent("Trace Everywhere", "Improves quality in scenes with varying smoothness, at a potential performance cost."));
					advancedSettings.treatBackfaceHitAsMiss = UIUtils.ToggleGUI(advancedSettings.treatBackfaceHitAsMiss, new GUIContent("Treat Backface Hit As Miss", "Enable to force more surfaces to use reflection probes if you see streaks on the sides of objects or bad reflections of their backs."));
					advancedSettings.allowBackwardsRays = UIUtils.ToggleGUI(advancedSettings.allowBackwardsRays, new GUIContent("Allow Backward Rays", "Enable for a performance gain in scenes where most glossy objects are horizontal, like floors, water, and tables. Leave on for scenes with glossy vertical objects."));
					advancedSettings.improveCorners = UIUtils.ToggleGUI(advancedSettings.improveCorners, new GUIContent("Improve Corners", "Improve visual fidelity of reflections on rough surfaces near corners in the scene, at the cost of a small amount of performance."));
					GUILayout.Label(new GUIContent("Resolution", "Half resolution SSRR is much faster, but less accurate. Quality can be reclaimed for some performance by doing the resolve at full resolution."), UIUtils.labelStyle);
					advancedSettings.resolution = (ScreenSpaceReflection.SSRResolution)GUILayout.SelectionGrid((int)advancedSettings.resolution, this._possibleSSRResolutionNames, 2, UIUtils.buttonStyleStrechWidth);
					advancedSettings.bilateralUpsample = UIUtils.ToggleGUI(advancedSettings.bilateralUpsample, new GUIContent("Bilateral Upsample", "Drastically improves reflection reconstruction quality at the expense of some performance."));
					advancedSettings.reduceBanding = UIUtils.ToggleGUI(advancedSettings.reduceBanding, new GUIContent("Reduce Banding", "Improve visual fidelity of mirror reflections at the cost of a small amount of performance."));
					advancedSettings.highlightSuppression = UIUtils.ToggleGUI(advancedSettings.highlightSuppression, new GUIContent("Highlight Suppression", "Enable to limit the effect a few bright pixels can have on rougher surfaces"));
				}

				ssrSettings.basicSettings = basicSettings;
				ssrSettings.reflectionSettings = reflectionSettings;
				ssrSettings.advancedSettings = advancedSettings;
				ssrSettings.debugSettings = debugSettings;
				this._ssr.settings = ssrSettings;
				HSLRE.HSLRE.self.fixSsrForUpscaledScreenshots = UIUtils.ToggleGUI(HSLRE.HSLRE.self.fixSsrForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
			}
		}

		private void VolumetricLightsModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUI(effectData.enabled, new GUIContent(index + " Volumetric Lights"), UIUtils.titleStyle2);
			if (effectData.enabled)
			{
				GUILayout.Space(UIUtils.space);

				GUILayout.Label(new GUIContent("Resolution", ""), UIUtils.labelStyle);
				this._volumetricLights.Resolution = (VolumetricLightRenderer.VolumetricResolution)GUILayout.SelectionGrid((int)this._volumetricLights.Resolution, this._possibleVolumetricLightsResolutionNames, 3, UIUtils.buttonStyleStrechWidth);
				this._volumetricLights.VisualizeLightTexture = UIUtils.ToggleGUI(this._volumetricLights.VisualizeLightTexture, new GUIContent("Visualize Light Texture", ""));

				if (this._currentVolumetricLight != null)
				{
					GUILayout.Space(UIUtils.space);
					this._currentVolumetricLight.enabled = UIUtils.ToggleGUI(this._currentVolumetricLight.enabled, new GUIContent("Selected Light", "Enables/Disables the entire effect for this light."), UIUtils.titleStyle2);
					this._currentVolumetricLight.SampleCount = Mathf.RoundToInt(UIUtils.SliderGUI(this._currentVolumetricLight.SampleCount, 1f, 64f, 12f, "Sample Count", "The number of raymarching samples (trade quality for performance).", "0"));
					this._currentVolumetricLight.ScatteringCoef = UIUtils.SliderGUI(this._currentVolumetricLight.ScatteringCoef, 0f, 1f, 0.5f, "Scattering Coef", "Scattering coefficient controls the amount of light that is reflected towards the camera. Makes the effect stronger.", "N4");
					this._currentVolumetricLight.ExtinctionCoef = UIUtils.SliderGUI(this._currentVolumetricLight.ExtinctionCoef, 0f, 0.1f, 0.01f, "Extinction Coef", "Controls the amount of light absorbed or out-scattered with distance. Makes the effect weaker with increasing distance. It also attenuates existing scene color when used with directional lights.", "N4");
					this._currentVolumetricLight.MieG = UIUtils.SliderGUI(this._currentVolumetricLight.MieG, 0f, 0.999f, 0.1f, "Mie Scattering", "", "N4");
					if (this._currentVolumetricLight.Light.type == LightType.Directional)
					{
						this._currentVolumetricLight.SkyboxExtinctionCoef = UIUtils.SliderGUI(this._currentVolumetricLight.SkyboxExtinctionCoef, 0f, 1f, 0.9f, "Skybox Extinction Coef", "Only affects directional light. It controls how much the skybox is affected by Extinction coefficient. This technique ignores small air particles and decrease particle density with altitude. Skybox extinction coefficient can help when the skybox appears too \"foggy\".", "N4");
						this._currentVolumetricLight.MaxRayLength = UIUtils.SliderGUI(this._currentVolumetricLight.MaxRayLength, 0f, 500f, 400f, "Max Ray Length", "", "N2");
					}

					this._currentVolumetricLight.HeightFog = UIUtils.ToggleGUI(this._currentVolumetricLight.HeightFog, new GUIContent("Height Fog", "Exponential height fog"), UIUtils.titleStyle2);
					this._currentVolumetricLight.HeightScale = UIUtils.SliderGUI(this._currentVolumetricLight.HeightScale, 0f, 0.5f, 0.1f, "Scale", "", "N4");
					this._currentVolumetricLight.GroundLevel = UIUtils.SliderGUI(this._currentVolumetricLight.GroundLevel, -100f, 100f, 0f, "Ground Level", "", "N2");

					this._currentVolumetricLight.Noise = UIUtils.ToggleGUI(this._currentVolumetricLight.Noise, new GUIContent("Noise", "Volumetric noise"), UIUtils.titleStyle2);
					this._currentVolumetricLight.NoiseScale = UIUtils.SliderGUI(this._currentVolumetricLight.NoiseScale, -5f, 5f, 0.15f, "Scale", "", "N3");
					this._currentVolumetricLight.NoiseIntensity = UIUtils.SliderGUI(this._currentVolumetricLight.NoiseIntensity, 0f, 12f, 3.44f, "Intensity", "", "N3");
					this._currentVolumetricLight.NoiseIntensityOffset = UIUtils.SliderGUI(this._currentVolumetricLight.NoiseIntensityOffset, 0f, 1f, 0.2f, "Intensity Offset", "", "N4");
					this._currentVolumetricLight.NoiseVelocity = new Vector2(
							UIUtils.SliderGUI(this._currentVolumetricLight.NoiseVelocity.x, -100f, 100f, 1f, "Velocity X", "Noise animation speed (x)", "N2"),
							UIUtils.SliderGUI(this._currentVolumetricLight.NoiseVelocity.y, -100f, 100f, 0.3f, "Velocity Y", "Noise animation speed (y)", "N2")
					);
				}
				else
				{
					Color c = GUI.color;
					GUI.color = Color.yellow;
					GUILayout.Label(new GUIContent("Select a light to see more settings", ""), UIUtils.labelStyle);
					GUI.color = c;
				}
				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://github.com/SlightlyMad/VolumetricLights");
			}
		}

		private void BloomAndFlaresModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Bloom and Flares"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
				Studio.Studio.Instance.sceneInfo.enableBloom = effectData.enabled;

			if (effectData.enabled)
			{
				GUILayout.Label(new GUIContent("Blend mode", "The method used to add bloom to the color buffer. The softer \"Screen\" mode is better for preserving bright image details but doesn’t work with HDR."), UIUtils.labelStyle);
				this._bloomAndFlares.screenBlendMode = (BloomScreenBlendMode)GUILayout.SelectionGrid((int)this._bloomAndFlares.screenBlendMode, this._possibleScreenBlendModeNames, 2, UIUtils.buttonStyleStrechWidth);
				GUILayout.Label(new GUIContent("HDR", "Whether bloom is using HDR buffers. This will result in a different look as pixel intensities may leave the [0,1] range."), UIUtils.labelStyle);
				this._bloomAndFlares.hdr = (HDRBloomMode)GUILayout.SelectionGrid((int)this._bloomAndFlares.hdr, this._possibleHDRBloomModeNames, 3, UIUtils.buttonStyleStrechWidth);

				this._bloomAndFlares.bloomIntensity = UIUtils.SliderGUI(this._bloomAndFlares.bloomIntensity, 0f, 8f, 1f, "Intensity", "The global light intensity of the added light (affects bloom and lens flares).", "N3");
				this._bloomAndFlares.bloomThreshold = UIUtils.SliderGUI(this._bloomAndFlares.bloomThreshold, -0.05f, 4f, 0.5f, "Threshold", "Regions of the image brighter than this threshold receive blooming (and potentially lens flares).", "N3");
				this._bloomAndFlares.bloomBlurIterations = Mathf.RoundToInt(UIUtils.SliderGUI(this._bloomAndFlares.bloomBlurIterations, 1f, 4f, 2f, "Blur iterations", "The number of times gaussian blur is applied. More iterations improve smoothness but take extra time to process and hide small frequencies.", "0"));
				this._bloomAndFlares.sepBlurSpread = UIUtils.SliderGUI(this._bloomAndFlares.sepBlurSpread, 0.1f, 10f, 1.5f, "Blur spread", "The max radius of the blur. Does not affect performance.", "N3");
				this._bloomAndFlares.useSrcAlphaAsMask = UIUtils.SliderGUI(this._bloomAndFlares.useSrcAlphaAsMask, 0f, 1f, 0f, "Use alpha mask", "The degree to which the alpha channel acts as a mask for the bloom effect.", "N3");

				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
				{
					Studio.Studio.Instance.sceneInfo.bloomIntensity = this._bloomAndFlares.bloomIntensity;
					Studio.Studio.Instance.sceneInfo.bloomBlur = this._bloomAndFlares.sepBlurSpread;
				}

				this._bloomAndFlares.lensflares = UIUtils.ToggleGUI(this._bloomAndFlares.lensflares, new GUIContent("Cast lens flares", "Enable or disable automatic lens flare generation."));
				if (this._bloomAndFlares.lensflares)
				{
					GUILayout.Label(new GUIContent("Lens flare mode", "The type of lens flare. The options are Ghosting, Anamorphic or a mix of the two."), UIUtils.labelStyle);
					this._bloomAndFlares.lensflareMode = (LensflareStyle34)GUILayout.SelectionGrid((int)this._bloomAndFlares.lensflareMode, this._possibleLensFlareStyle34Names, 3, UIUtils.buttonStyleStrechWidth);
					this._bloomAndFlares.lensflareIntensity = UIUtils.SliderGUI(this._bloomAndFlares.lensflareIntensity, 0f, 8f, 1f, "Local intensity", "Local intensity used only for lens flares.", "N3");
					this._bloomAndFlares.lensflareThreshold = UIUtils.SliderGUI(this._bloomAndFlares.lensflareThreshold, 0f, 1f, 0.3f, "Local threshold", "The accumulative light intensity threshold that defines which image parts are candidates for lens flares.", "N3");

					switch (this._bloomAndFlares.lensflareMode)
					{
						case LensflareStyle34.Combined:
						case LensflareStyle34.Anamorphic:
							this._bloomAndFlares.hollyStretchWidth = UIUtils.SliderGUI(this._bloomAndFlares.hollyStretchWidth, 0f, 8f, 3.5f, "Stretch witdh", "The width for anamorphic lens flares.", "N3");
							this._bloomAndFlares.hollywoodFlareBlurIterations = Mathf.RoundToInt(UIUtils.SliderGUI(this._bloomAndFlares.hollywoodFlareBlurIterations, 1f, 4f, 2f, "Blur iterations", "The number of times blurring is applied to anamorphic lens flares. More iterations improve smoothness but take more processing time.", "0"));
							if (this._bloomAndFlares.lensflareMode == LensflareStyle34.Combined)
								goto case LensflareStyle34.Ghosting;
							UIUtils.ColorPickerGUI(this._bloomAndFlares.flareColorA, new Color(0.4f, 0.4f, 0.8f, 0.75f), "Tint color", "Color modulation for the anamorphic flare type.", c =>
							{
								this._bloomAndFlares.flareColorA = c;
							});
							break;
						case LensflareStyle34.Ghosting:
							UIUtils.ColorPickerGUI(this._bloomAndFlares.flareColorA, new Color(0.4f, 0.4f, 0.8f, 0.75f), "1st Color", "Color modulation for all lens flares when Ghosting or Combined is chosen.", c =>
							{
								this._bloomAndFlares.flareColorA = c;
							});
							UIUtils.ColorPickerGUI(this._bloomAndFlares.flareColorB, new Color(0.4f, 0.8f, 0.8f, 0.75f), "2nd Color", "Color modulation for all lens flares when Ghosting or Combined is chosen.", c =>
							{
								this._bloomAndFlares.flareColorB = c;
							});
							UIUtils.ColorPickerGUI(this._bloomAndFlares.flareColorC, new Color(0.8f, 0.4f, 0.8f, 0.75f), "3rd Color", "Color modulation for all lens flares when Ghosting or Combined is chosen.", c =>
							{
								this._bloomAndFlares.flareColorC = c;
							});
							UIUtils.ColorPickerGUI(this._bloomAndFlares.flareColorD, new Color(0.8f, 0.4f, 0f, 0.75f), "4th Color", "Color modulation for all lens flares when Ghosting or Combined is chosen.", c =>
							{
								this._bloomAndFlares.flareColorD = c;
							});
							break;
					}
				}
				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-BloomAndFlares.html");
			}
		}

		private void ColorCorrectionCurvesModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Color Correction Curves"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				this._colorCorrectionCurves.saturation = UIUtils.SliderGUI(this._colorCorrectionCurves.saturation, 0f, 5f, 1f, "Saturation", "Saturation level (0 creates a black & white image).", "N3");

				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-ColorCorrectionCurves.html");
			}
		}

		private void VignetteAndChromaticAberrationModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Vignette and Chromatic Aberration"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
				Studio.Studio.Instance.sceneInfo.enableVignette = effectData.enabled;

			if (effectData.enabled)
			{
				this._vignette.intensity = UIUtils.SliderGUI(this._vignette.intensity, 0f, 1f, 0.036f, "Vignetting", "The degree of darkening applied to the screen edges and corners. Choose 0 to disable this feature and save on performance.", "N3");
				this._vignette.blur = UIUtils.SliderGUI(this._vignette.blur, 0f, 1f, 0f, "Blurred Corners", "The amount of blur that is added to the screen corners. Choose 0 to disable this feature and save on performance.", "N3");
				this._vignette.blurSpread = UIUtils.SliderGUI(this._vignette.blurSpread, 0f, 1f, 0.75f, "Blurred Distance", "The blur filter sample distance used when blurring corners.", "N3");

				if (_isStudio && Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
					Studio.Studio.Instance.sceneInfo.vignetteVignetting = this._vignette.intensity;


				GUILayout.Label(new GUIContent("Aberration mode", "Advanced tries to model more aberration effects (the constant axial aberration existant on the entire image plane) while Simple only models tangential aberration (limited to corners)."), UIUtils.labelStyle);
				this._vignette.mode = (VignetteAndChromaticAberration.AberrationMode)GUILayout.SelectionGrid((int)this._vignette.mode, this._possibleAberrationModeNames, 2, UIUtils.buttonStyleStrechWidth);

				this._vignette.chromaticAberration = UIUtils.SliderGUI(this._vignette.chromaticAberration, 0f, 5f, 0.2f, "Tangential Aberration", "The degree of tangential chromatic aberration: Uniform on the entire image plane.", "N3");
				if (this._vignette.mode == VignetteAndChromaticAberration.AberrationMode.Advanced)
				{
					this._vignette.axialAberration = UIUtils.SliderGUI(this._vignette.axialAberration, 0f, 5f, 0.5f, "Axial Aberration", "The degree of axial chromatic aberration: Scales with smaller distance to the image plane’s corners.", "N3");
					this._vignette.luminanceDependency = UIUtils.SliderGUI(this._vignette.luminanceDependency, 0.001f, 1f, 0.25f, "Contrast Dependency", "The bigger this value, the more contrast is needed for the aberration to trigger. Higher values are more realistic (in this case, an HDR input is recommended).", "N3");
					this._vignette.blurDistance = UIUtils.SliderGUI(this._vignette.blurDistance, 0.001f, 5f, 2.5f, "Blur Distance", "Overall aberration intensity (not to confuse with color offset distance), defaults to 1.0.", "N3");
				}
				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-VignettingAndChromaticAberration.html");
			}
		}

		private void AntialiasingModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Antialiasing"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				GUILayout.Label(new GUIContent("AA Technique", "The algorithm to be used."), UIUtils.labelStyle);
				this._antialiasing.mode = (AAMode)GUILayout.SelectionGrid((int)this._antialiasing.mode, this._possibleAAModeNames, 3, UIUtils.buttonStyleStrechWidth);

				switch (this._antialiasing.mode)
				{
					case AAMode.FXAA3Console:
						this._antialiasing.edgeThresholdMin = UIUtils.SliderGUI(this._antialiasing.edgeThresholdMin, 0f, 4f, 0.05f, "Edge Min Threshold", "", "N3");
						this._antialiasing.edgeThreshold = UIUtils.SliderGUI(this._antialiasing.edgeThreshold, 0f, 4f, 0.2f, "Edge Threshold", "", "N3");
						this._antialiasing.edgeSharpness = UIUtils.SliderGUI(this._antialiasing.edgeSharpness, 0f, 16f, 4f, "Edge Sharpness", "", "N3");
						break;
					case AAMode.NFAA:
						this._antialiasing.offsetScale = UIUtils.SliderGUI(this._antialiasing.offsetScale, 0f, 4f, 0.2f, "Edge Detect Offset", "", "N3");
						this._antialiasing.blurRadius = UIUtils.SliderGUI(this._antialiasing.blurRadius, 0f, 18f, 18f, "Blur Radius", "", "N3");
						this._antialiasing.showGeneratedNormals = UIUtils.ToggleGUI(this._antialiasing.showGeneratedNormals, new GUIContent("Show Normals", ""));
						break;
					case AAMode.DLAA:
						this._antialiasing.dlaaSharp = UIUtils.ToggleGUI(this._antialiasing.dlaaSharp, new GUIContent("Sharp", ""));
						break;
				}
			}
		}

		private void NoiseAndGrainModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Noise and Grain"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				this._noiseAndGrain.dx11Grain = UIUtils.ToggleGUI(this._noiseAndGrain.dx11Grain, new GUIContent("DX11 Grain", "Enable high quality noise and grain (DX11/GL3 only)."));
				this._noiseAndGrain.monochrome = UIUtils.ToggleGUI(this._noiseAndGrain.monochrome, new GUIContent("Monochrome", "Use greyscale noise only."));

				this._noiseAndGrain.intensityMultiplier = UIUtils.SliderGUI(this._noiseAndGrain.intensityMultiplier, 0f, 10f, 0.25f, "Intensity multiplier", "Global intensity adjustment.", "N3");
				this._noiseAndGrain.generalIntensity = UIUtils.SliderGUI(this._noiseAndGrain.generalIntensity, 0f, 1f, 0.5f, "General", "Add noise equally for all luminance ranges.", "N3");
				this._noiseAndGrain.blackIntensity = UIUtils.SliderGUI(this._noiseAndGrain.blackIntensity, 0f, 1f, 1f, "Black Boost", "Add extra low luminance noise.", "N3");
				this._noiseAndGrain.whiteIntensity = UIUtils.SliderGUI(this._noiseAndGrain.whiteIntensity, 0f, 1f, 1f, "White Boost", "Add extra high luminance noise.", "N3");
				this._noiseAndGrain.midGrey = UIUtils.SliderGUI(this._noiseAndGrain.midGrey, 0f, 1f, 0.2f, "Mid Grey", "Defines ranges for high-level and low-level noise ranges above.", "N3");
				if (this._noiseAndGrain.monochrome == false)
				{
					UIUtils.ColorPickerGUI(new Color(this._noiseAndGrain.intensities.x, this._noiseAndGrain.intensities.y, this._noiseAndGrain.intensities.z), Color.white, "Color Weights", "Additionally tint noise.", (c) =>
					{
						this._noiseAndGrain.intensities = new Vector3(c.r, c.g, c.b);
					});
				}

				this._noiseAndGrain.filterMode = (FilterMode)GUILayout.SelectionGrid((int)this._noiseAndGrain.filterMode, this._possibleFilterModeNames, 3, UIUtils.buttonStyleStrechWidth);
				this._noiseAndGrain.softness = UIUtils.SliderGUI(this._noiseAndGrain.softness, 0f, 0.99f, 0.25f, "Softness", "Defines noise or grain crispness. Higher values might yield better performance but require temporary a render target.", "N3");

				if (this._noiseAndGrain.dx11Grain == false)
				{

					if (this._noiseAndGrain.monochrome)
					{
						this._noiseAndGrain.monochromeTiling = UIUtils.SliderGUI(this._noiseAndGrain.monochromeTiling, 1f, 128f, 64f, "Tiling", "Noise pattern tiling", "N1");
					}
					else
					{
						this._noiseAndGrain.tiling = new Vector3(
																 UIUtils.SliderGUI(this._noiseAndGrain.tiling.x, 1f, 128f, 64f, "Tiling (Red)", "Noise pattern tiling (can be tweaked for all color channels individually when in non-DX11 texture mode).", "N1"),
																 UIUtils.SliderGUI(this._noiseAndGrain.tiling.y, 1f, 128f, 64f, "Tiling (Green)", "Noise pattern tiling (can be tweaked for all color channels individually when in non-DX11 texture mode).", "N1"),
																 UIUtils.SliderGUI(this._noiseAndGrain.tiling.z, 1f, 128f, 64f, "Tiling (Blue)", "Noise pattern tiling (can be tweaked for all color channels individually when in non-DX11 texture mode).", "N1")
																);
					}
				}
				HSLRE.HSLRE.self.fixNoiseAndGrainForUpscaledScreenshots = UIUtils.ToggleGUI(HSLRE.HSLRE.self.fixNoiseAndGrainForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-NoiseAndGrain.html");
			}
		}

		private void MotionBlurModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Camera Motion Blur"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				this._motionBlur.velocityScale = UIUtils.SliderGUI(this._motionBlur.velocityScale, 0f, 8f, 0.375f, "Velocity Scale", "Higher scale makes image more likely to blur.", "N3");
				this._motionBlur.maxVelocity = UIUtils.SliderGUI(this._motionBlur.maxVelocity, 2f, 10f, 8f, "Velocity Max", "Maximum pixel distance blur will be clamped to and tile size for reconstruction filters (see below).", "N3");
				this._motionBlur.minVelocity = UIUtils.SliderGUI(this._motionBlur.minVelocity, 0.1f, 10f, 0.1f, "Velocity Min", "Minimum pixel distance at which blur is removed entirely.", "N3");

				GUILayout.Label(new GUIContent("Technique", "Motion Blur algorithm. Reconstruction filters will generally give best results at the expense of performance and a limited blur radius of 10 pixels unless a DX11/GL3 enabled graphics device is used."), UIUtils.labelStyle);
				this._motionBlur.filterType = (CameraMotionBlur.MotionBlurFilter)GUILayout.SelectionGrid((int)this._motionBlur.filterType, this._possibleMotionBlurFilterNames, 2, UIUtils.buttonStyleStrechWidth);

				if (this._motionBlur.filterType == CameraMotionBlur.MotionBlurFilter.CameraMotion)
				{
					this._motionBlur.rotationScale = UIUtils.SliderGUI(this._motionBlur.rotationScale, 0f, 8f, 1f, "Camera Rotation", "Scales strength of blurs due to camera rotations.", "N3");
					this._motionBlur.movementScale = UIUtils.SliderGUI(this._motionBlur.movementScale, 0f, 8f, 0f, "Camera Movement", "Scales strength of blurs due to camera translations.", "N3");
				}
				else
					this._motionBlur.velocityDownsample = Mathf.RoundToInt(UIUtils.SliderGUI(this._motionBlur.velocityDownsample, 1f, 32f, 1f, "Velocity Downsample", "Lower resolution velocity buffers might help performance but will heavily degrade blur quality. Might still be a valid option for simple scenes.", "0"));
				if (this._motionBlur.filterType >= CameraMotionBlur.MotionBlurFilter.Reconstruction)
					this._motionBlur.jitter = UIUtils.SliderGUI(this._motionBlur.jitter, 0f, 10f, 0.05f, "Jitter Strength", "N3");

				this._motionBlur.preview = UIUtils.ToggleGUI(this._motionBlur.preview, new GUIContent("Preview", "Preview how blur might look like given artificial camera motion values."));
				if (this._motionBlur.preview)
				{
					this._motionBlur.previewScale = new Vector3(
																UIUtils.SliderGUI(this._motionBlur.previewScale.x, -16f, 16f, 1f, "X", "N3"),
																UIUtils.SliderGUI(this._motionBlur.previewScale.y, -16f, 16f, 1f, "Y", "N3"),
																UIUtils.SliderGUI(this._motionBlur.previewScale.z, -16f, 16f, 1f, "Z", "N3")
															   );
				}
				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-CameraMotionBlur.html");
			}
		}

		private void AfterImageModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " After Image"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				this._afterImage.framesToMix = Mathf.RoundToInt(UIUtils.SliderGUI(this._afterImage.framesToMix, 1f, AfterImage.maxFrames, 8f, "Frames To Mix", "Number of frames to mix together.", "0"));
				this._afterImage.falloffPower = UIUtils.SliderGUI(this._afterImage.falloffPower, 0f, 2f, 10f / 7f, "Falloff Power", "How quickly older frames are faded out.", "N4");
			}
		}

		private void SEGIModule(HSLRE.HSLRE.EffectData effectData, int i)
		{
			effectData.enabled = UIUtils.ToggleGUI(effectData.enabled, new GUIContent(i + " SEGI"), UIUtils.titleStyle2);

			if (effectData.enabled)
			{
				if (RenderSettings.ambientIntensity > 0)
				{
					Color color = GUI.color;
					GUI.color = Color.red;
					GUILayout.Label("SEGI only works properly if the ambient intensity is set to 0, check the Lighting tab and make sure it's the case. Having HSStandard is also needed as it ensures the shaders of everything are compatible with SEGI.", UIUtils.labelStyle);
					GUI.color = color;
				}

				GUILayout.Label("Presets", UIUtils.labelStyle);
				this._selectedSegiPreset = GUILayout.SelectionGrid(this._selectedSegiPreset, this._possiblePresetNames, 3, UIUtils.buttonStyleStrechWidth);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(new GUIContent("Apply", "Pressing this will apply the selected preset and change your settings, be careful."), UIUtils.buttonStyleStrechWidth, GUILayout.ExpandWidth(false)))
					this._segi.ApplyPreset(this._selectedSegiPreset);
				GUILayout.EndHorizontal();

				this._segi.visualizeSunDepthTexture = UIUtils.ToggleGUI(this._segi.visualizeSunDepthTexture, new GUIContent("Visualize Sun Depth Texture", "Visualize the depth texture used to render proper shadows while injecting sunlight into voxel data."));
				this._segi.visualizeGI = UIUtils.ToggleGUI(this._segi.visualizeGI, new GUIContent("Visualize GI", "Visualize GI result only (no textures)."));
				this._segi.visualizeVoxels = UIUtils.ToggleGUI(this._segi.visualizeVoxels, new GUIContent("Visualize Voxels", "Directly view the voxels in the scene."));
				GUILayout.Label("Visualize GBuffer", UIUtils.labelStyle);
				this._segi.visualizeGBuffers = GUILayout.SelectionGrid(this._segi.visualizeGBuffers, new[] { "None", "GBuffer0", "GBuffer1", "GBuffer2", "Depth", "DepthNormals" }, 3, UIUtils.buttonStyleStrechWidth);

				GUILayout.Space(UIUtils.space);

				GUILayout.Label(new GUIContent("Voxel Resolution", "The resolution of the voxel texture used to calculate GI."), UIUtils.labelStyle);
				int index = 0;
				switch (this._segi.voxelResolution)
				{
					case SEGI.VoxelResolution.low:
						index = 0;
						break;
					case SEGI.VoxelResolution.high:
						index = 1;
						break;
				}
				index = GUILayout.SelectionGrid(index, this._possibleVoxelResolutionNames, 2, UIUtils.buttonStyleStrechWidth);
				switch (index)
				{
					case 0:
						this._segi.voxelResolution = SEGI.VoxelResolution.low;
						break;
					case 1:
						this._segi.voxelResolution = SEGI.VoxelResolution.high;
						break;
				}
				this._segi.voxelAA = UIUtils.ToggleGUI(this._segi.voxelAA, new GUIContent("Voxel AA", "Enables anti-aliasing during voxelization for higher precision voxels."));
				this._segi.innerOcclusionLayers = Mathf.RoundToInt(UIUtils.SliderGUI(this._segi.innerOcclusionLayers, 0f, 2f, 1f, "Inner Occlusion Layers", "Enables the writing of additional black occlusion voxel layers on the back face of geometry. Can help with light leaking but may cause artifacts with small objects.", "0"));
				this._segi.gaussianMipFilter = UIUtils.ToggleGUI(this._segi.gaussianMipFilter, new GUIContent("Gaussian Mip Filter", "Enables gaussian filtering during mipmap generation. This can improve visual smoothness and consistency, particularly with large moving objects."));
				this._segi.voxelSpaceSize = UIUtils.SliderGUI(this._segi.voxelSpaceSize, 1f, 100f, 50f, "Voxel Space Size", "The size of the voxel volume in world units. Everything inside the voxel volume will contribute to GI.", "N3");
				this._segi.shadowSpaceSize = UIUtils.SliderGUI(this._segi.shadowSpaceSize, 1f, 100f, 50f, "Shadow Space Size", "The size of the sun shadow texture used to inject sunlight with shadows into the voxels in world units. It is recommended to set this value similar to Voxel Space Size.", "N3");
				this._segi.updateGI = UIUtils.ToggleGUI(this._segi.updateGI, new GUIContent("Update GI", "Whether voxelization and multi-bounce rendering should update every frame. When disabled, GI tracing will use cached data from the last time this was enabled."));
				this._segi.infiniteBounces = UIUtils.ToggleGUI(this._segi.infiniteBounces, new GUIContent("Infinite Bounces", "Enables infinite bounces. This is expensive for complex scenes and is still experimental."));
				this._segi.hsStandardCustomShadersCompatibility = UIUtils.ToggleGUI(this._segi.hsStandardCustomShadersCompatibility, new GUIContent("HSStandard Custom Shaders Compatibility", "Enabling this makes sure the custom hair and skin shaders used in HSStandard when using the options \"dedicatedHairShader\" and \"dedicatedSkinShader\" are used by SEGI (incurs a performance cost)."));

				GUILayout.Label("VRAM Usage: " + this._segi.vramUsage.ToString("F2") + " MB", UIUtils.labelStyle);

				GUILayout.Space(UIUtils.space);
				this._segi.softSunlight = UIUtils.SliderGUI(this._segi.softSunlight, 0f, 16f, 0f, "Soft Sunlight", "The amount of soft diffuse sunlight that will be added to the scene. Use this to simulate the effect of clouds/haze scattering soft sunlight onto the scene.", "N3");
				UIUtils.ColorPickerGUI(this._segi.skyColor, Color.clear, "Sky Color", "The color of the light scattered onto the scene coming from the sky.", (c) =>
				{
					this._segi.skyColor = c;
				});
				this._segi.skyIntensity = UIUtils.SliderGUI(this._segi.skyIntensity, 0f, 8f, 1f, "Sky Intensity", "The brightness of the sky light.", "N3");
				this._segi.sphericalSkylight = UIUtils.ToggleGUI(this._segi.sphericalSkylight, new GUIContent("Spherical Skylight", "If enabled, light from the sky will come from all directions. If disabled, light from the sky will only come from the top hemisphere."));

				GUILayout.Space(UIUtils.space);

				this._segi.temporalBlendWeight = UIUtils.SliderGUI(this._segi.temporalBlendWeight, 0.01f, 1f, 0.1f, "Temporal Blend Weight", "The lower the value, the more previous frames will be blended with the current frame. Lower values result in smoother GI that updates less quickly.", "N3");
				this._segi.useBilateralFiltering = UIUtils.ToggleGUI(this._segi.useBilateralFiltering, new GUIContent("Bilateral Filtering", "Enables filtering of the GI result to reduce noise."));
				this._segi.halfResolution = UIUtils.ToggleGUI(this._segi.halfResolution, new GUIContent("Half Resolution", "If enabled, GI tracing will be done at half screen resolution. Improves speed of GI tracing."));
				this._segi.stochasticSampling = UIUtils.ToggleGUI(this._segi.stochasticSampling, new GUIContent("Stochastic Sampling", "If enabled, uses random jitter to reduce banding and discontinuities during GI tracing."));

				this._segi.cones = Mathf.RoundToInt(UIUtils.SliderGUI(this._segi.cones, 1f, 128f, 6f, "Cones", "The number of cones that will be traced in different directions for diffuse GI tracing. More cones result in a smoother result at the cost of performance.", "0"));
				this._segi.coneTraceSteps = Mathf.RoundToInt(UIUtils.SliderGUI(this._segi.coneTraceSteps, 1f, 32f, 14f, "Cone Trace Steps", "The number of tracing steps for each cone. Too few results in skipping thin features. Higher values result in more accuracy at the cost of performance.", "0"));
				this._segi.coneLength = UIUtils.SliderGUI(this._segi.coneLength, 0.1f, 2f, 1f, "Cone length", "The length of each cone that is traced for diffuse indirect lighting. This is essentially an adjustment for the \"GI radius\", meaning, this will directly influence how distant objects can be to contribute to GI. If you need a higher \"GI radius\", you may want to simply increase Voxel Space Size instead of this parameter.", "N3");
				this._segi.coneWidth = UIUtils.SliderGUI(this._segi.coneWidth, 0.5f, 6f, 5.5f, "Cone Width", "The width of each cone. Wider cones cause a softer and smoother result but affect accuracy and incrase over-occlusion. Thinner cones result in more accurate tracing with less coherent (more noisy) results and a higher tracing cost.", "N3");
				this._segi.coneTraceBias = UIUtils.SliderGUI(this._segi.coneTraceBias, 0f, 4f, 1f, "Cone Trace Bias", "The amount of offset above a surface that cone tracing begins. Higher values reduce \"voxel acne\" (similar to \"shadow acne\"). Values that are too high result in light-leaking.", "N3");
				this._segi.occlusionStrength = UIUtils.SliderGUI(this._segi.occlusionStrength, 0f, 4f, 1f, "Occlusion Strength", "The strength of shadowing solid objects will cause. Affects the strength of all indirect shadows.", "N3");
				this._segi.nearOcclusionStrength = UIUtils.SliderGUI(this._segi.nearOcclusionStrength, 0f, 4f, 0.5f, "Near Occlusion Strength", "The strength of shadowing nearby solid objects will cause. Only affects the strength of very close blockers.", "N3");
				this._segi.farOcclusionStrength = UIUtils.SliderGUI(this._segi.farOcclusionStrength, 0.1f, 4f, 1f, "Far Occlusion Strength", "How much light far occluders block. This value gives additional light blocking proportional to the width of the cone at each trace step.", "N3");
				this._segi.farthestOcclusionStrength = UIUtils.SliderGUI(this._segi.farthestOcclusionStrength, 0.1f, 4f, 1f, "Farthest Occlusion Strength", "How much light the farthest occluders block. This value gives additional light blocking proportional to (cone width)^2 at each trace step.", "N3");
				this._segi.occlusionPower = UIUtils.SliderGUI(this._segi.occlusionPower, 0.001f, 4f, 1.5f, "Occlusion Power", "The strength of shadowing far solid objects will cause. Only affects the strength of far blockers. Decrease this value if wide cones are causing over-occlusion.", "N3");
				this._segi.nearLightGain = UIUtils.SliderGUI(this._segi.nearLightGain, 0f, 4f, 1f, "Near Light Gain", "Affects the attenuation of indirect light. Higher values allow for more close-proximity indirect light. Lower values reduce close-proximity indirect light, sometimes resulting in a cleaner result.", "N3");
				this._segi.giGain = UIUtils.SliderGUI(this._segi.giGain, 0f, 4f, 1f, "GI Gain", "The overall brightness of indirect light. For Near Light Gain values around 1, a value of 1 for this property is recommended for a physically-accurate result.", "N3");

				GUILayout.Space(UIUtils.space);

				this._segi.secondaryBounceGain = UIUtils.SliderGUI(this._segi.secondaryBounceGain, 0f, 4f, 1f, "Secondary Bounce Gain", "Affects the strength of secondary/infinite bounces. Be careful, values above 1 can cause runaway light bouncing and flood areas with extremely bright light!", "N3");
				this._segi.secondaryCones = Mathf.RoundToInt(UIUtils.SliderGUI(this._segi.secondaryCones, 3f, 16f, 6f, "Secondary Cones", "The number of secondary cones that will be traced for calculating infinte bounces. Increasing this value improves the accuracy of secondary bounces at the cost of performance. Note: the performance cost of this scales with voxelized scene complexity.", "0"));
				this._segi.secondaryOcclusionStrength = UIUtils.SliderGUI(this._segi.secondaryOcclusionStrength, 0.1f, 4f, 1f, "Secondary Occlusion Strength", "The strength of light blocking during secondary bounce tracing. Be careful, a value too low can cause runaway light bouncing and flood areas with extremely bright light!", "N3");

				GUILayout.Space(UIUtils.space);

				this._segi.doReflections = UIUtils.ToggleGUI(this._segi.doReflections, new GUIContent("Do Reflections", "Enable this for cone-traced reflections."));
				this._segi.reflectionSteps = Mathf.RoundToInt(UIUtils.SliderGUI(this._segi.reflectionSteps, 12f, 128f, 64f, "Reflection Steps", "Number of reflection trace steps.", "0"));
				this._segi.reflectionOcclusionPower = UIUtils.SliderGUI(this._segi.reflectionOcclusionPower, 0.001f, 4f, 1f, "Reflection Occlusion Power", "Strength of light blocking during reflection tracing.", "N3");
				this._segi.skyReflectionIntensity = UIUtils.SliderGUI(this._segi.skyReflectionIntensity, 0f, 1f, 1f, "Sky Reflection Intensity", "Intensity of sky reflections.", "N3");

				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("https://github.com/sonicether/SEGI/blob/master/User%20Guide.pdf");
			}
		}

		private void AmplifyBloomModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Amplify Bloom"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				{
					GUILayout.Label(new GUIContent("Debug", "Debug each bloom/feature stage to screen."), UIUtils.labelStyle);
					this._amplifyBloom.DebugToScreen = (DebugToScreenEnum)GUILayout.SelectionGrid((int)this._amplifyBloom.DebugToScreen, this._possibleDebugToScreenEnumNames, 2, UIUtils.buttonStyleStrechWidth);

					GUILayout.Label(new GUIContent("Technique", "Method which will be used to upscale results. Realistic is visually more robust but less efficient."), UIUtils.labelStyle);
					this._amplifyBloom.UpscaleQuality = (UpscaleQualityEnum)GUILayout.SelectionGrid((int)this._amplifyBloom.UpscaleQuality, this._possibleUpscaleQualityEnumNames, 2, UIUtils.buttonStyleStrechWidth);
					GUILayout.Label(new GUIContent("Source Downscale", "Initial render texture scale on which the Main Threshold will be written."), UIUtils.labelStyle);
					this._amplifyBloom.MainThresholdSize = (MainThresholdSizeEnum)GUILayout.SelectionGrid((int)this._amplifyBloom.MainThresholdSize, this._possibleMainThresholdSizeEnumNames, 3, UIUtils.buttonStyleStrechWidth);
					GUILayout.Label(new GUIContent("Precision", "Switch between HDR and LDR Render Texture formats."), UIUtils.labelStyle);
					this._amplifyBloom.CurrentPrecisionMode = (PrecisionModes)GUILayout.SelectionGrid((int)this._amplifyBloom.CurrentPrecisionMode, this._possiblePrecisionModesNames, 2, UIUtils.buttonStyleStrechWidth);

					if (this._amplifyBloom.CurrentPrecisionMode == PrecisionModes.Low)
						this._amplifyBloom.BloomRange = UIUtils.SliderGUI(this._amplifyBloom.BloomRange, 1f, 1000f, 500f, "Range", "LDR Tweakable range. Use to match HDR results.", "0");

					this._amplifyBloom.OverallIntensity = UIUtils.SliderGUI(this._amplifyBloom.OverallIntensity, 0f, 8f, 0.8f, "Intensity", "Overall bloom intensity. Affects all the effects bellow.", "N3");

					this._amplifyBloom.OverallThreshold = UIUtils.SliderGUI(this._amplifyBloom.OverallThreshold, 0f, 8f, 0.53f, "Threshold", "Luminance threshold to setup what should generate bloom.", "N3");

					if (this._amplifyBloom.UpscaleQuality == UpscaleQualityEnum.Realistic)
					{
						this._amplifyBloom.UpscaleBlurRadius = UIUtils.SliderGUI(this._amplifyBloom.UpscaleBlurRadius, 1f, 12f, 1f, "Upscale Blur Radius", "Radius used on the tent filter when upscaling to source size", "N3");
					}
					//int weightMaxDowsampleCount = Mathf.Max(1, this._amplifyBloom.BloomDownsampleCount);
					{
						/*
                        EditorGUI.indentLevel++;
                        m_mipSettingsFoldout = CustomFoldout(m_mipSettingsFoldout, m_mipSettingGC);
                        if (m_mipSettingsFoldout)
                        {
                            EditorGUI.indentLevel++;
                            this._amplifyBloom.BloomDownsampleCount = UIUtils.SliderGUI(this._amplifyBloom.BloomDownsampleCount, AmplifyBloomBase.MinDownscales, this._amplifyBloom.SoftMaxdownscales, 0f, m_downscaleAmountGC, "N3");
                            bool guiState = this._amplifyBloom.BloomDownsampleCount != 0;

                            GUI.enabled = guiState;

                            int featuresSourceId = this._amplifyBloom.FeaturesSourceId + 1;
                            featuresSourceId = UIUtils.SliderGUI(featuresSourceId, 1, this._amplifyBloom.BloomDownsampleCount, 0f, m_featuresSourceIdGC, "N3");
                            this._amplifyBloom.FeaturesSourceId = featuresSourceId - 1;
                            EditorGUI.indentLevel--;
                        }
                        GUI.enabled = true;
                        
                        m_bloomWeightsFoldout = CustomFoldout(m_bloomWeightsFoldout, m_bloomWeightsFoldoutGC);
                        if (m_bloomWeightsFoldout)
                        {
                            EditorGUI.indentLevel++;

                            float blurStepSize = 15;
                            float blurRadiusSize = 15;
                            float weightSize = 25;

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(41);
                            GUILayout.LabelField(m_blurStepGC, GUILayout.MinWidth(blurStepSize));
                            GUILayout.Space(-26);
                            GUILayout.LabelField(m_blurRadiusGC, GUILayout.MinWidth(blurRadiusSize));
                            GUILayout.Space(-27);
                            GUILayout.LabelField(m_upscaleWeightGC, GUILayout.MinWidth(weightSize));
                            GUILayout.EndHorizontal();
                            for (int i = 0; i < weightMaxDowsampleCount; i++)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.LabelField(m_bloomWeightsLabelGCArr[i], GUILayout.Width(65));
                                GUILayout.Space(-30);

                                this._amplifyBloom.GaussianSteps[i] = GUILayout.IntField(string.Empty, this._amplifyBloom.GaussianSteps[i], GUILayout.MinWidth(blurStepSize));
                                this._amplifyBloom.GaussianSteps[i] = Mathf.Clamp(this._amplifyBloom.GaussianSteps[i], 0, AmplifyBloomBase.MaxGaussian);

                                GUILayout.Space(-27);
                                this._amplifyBloom.GaussianRadius[i] = GUILayout.FloatField(string.Empty, this._amplifyBloom.GaussianRadius[i], GUILayout.MinWidth(blurRadiusSize));
                                this._amplifyBloom.GaussianRadius[i] = Mathf.Max(this._amplifyBloom.GaussianRadius[i], 0f);

                                GUILayout.Space(-27);
                                int id = weightMaxDowsampleCount - 1 - i;
                                this._amplifyBloom.UpscaleWeights[id] = GUILayout.FloatField(string.Empty, this._amplifyBloom.UpscaleWeights[id], GUILayout.MinWidth(weightSize));
                                this._amplifyBloom.UpscaleWeights[id] = Mathf.Max(this._amplifyBloom.UpscaleWeights[id], 0f);

                                GUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
                        */
					}

					this._amplifyBloom.TemporalFilteringActive = UIUtils.ToggleGUI(this._amplifyBloom.TemporalFilteringActive, new GUIContent("Temporal Filter", "Settings for temporal filtering configuration."));
					if (this._amplifyBloom.TemporalFilteringActive)
					{
						//this._amplifyBloom.TemporalFilteringCurve = GUILayout.CurveField(m_filterCurveGC, this._amplifyBloom.TemporalFilteringCurve, TemporalCurveColor, TemporalCurveRanges);
						this._amplifyBloom.TemporalFilteringValue = UIUtils.SliderGUI(this._amplifyBloom.TemporalFilteringValue, 0.01f, 1f, 0.05f, "Filter Value", "Position on the filter curve.", "N3");
					}

					this._amplifyBloom.SeparateFeaturesThreshold = UIUtils.ToggleGUI(this._amplifyBloom.SeparateFeaturesThreshold, new GUIContent("Features Threshold", "Settings for features threshold."));
					if (this._amplifyBloom.SeparateFeaturesThreshold)
					{
						this._amplifyBloom.FeaturesThreshold = UIUtils.SliderGUI(this._amplifyBloom.FeaturesThreshold, 0f, 8f, 0.05f, "Threshold", "Threshold value for second threshold layer.", "N3");
					}
				}

				this._amplifyBloom.ApplyLensDirt = UIUtils.ToggleGUI(this._amplifyBloom.ApplyLensDirt, new GUIContent("Lens Dirt", "Settings for Lens Dirt composition."), UIUtils.titleStyle2);
				if (this._amplifyBloom.ApplyLensDirt)
				{
					this._amplifyBloom.LensDirtStrength = UIUtils.SliderGUI(this._amplifyBloom.LensDirtStrength, 0f, 8f, 2f, "Intensity", "Lens Dirt Intensity.", "N3");
					//m_lensDirtWeightsFoldout = CustomFoldout(m_lensDirtWeightsFoldout, m_lensWeightsFoldoutGC);
					//if (m_lensDirtWeightsFoldout)
					//{
					//    for (int i = 0; i < this._amplifyBloom.BloomDownsampleCount; i++)
					//    {
					//        int id = this._amplifyBloom.BloomDownsampleCount - 1 - i;
					//        this._amplifyBloom.LensDirtWeights[id] = GUILayout.FloatField(m_lensWeightGCArr[i], this._amplifyBloom.LensDirtWeights[id]);
					//        this._amplifyBloom.LensDirtWeights[id] = Mathf.Max(this._amplifyBloom.LensDirtWeights[id], 0f);
					//    }
					//}
					GUILayout.Label(new GUIContent("Dirt Texture", "Mask from which dirt is going to be created."), UIUtils.labelStyle);
					this._amplifyBloom.LensDirtTexture = (LensDirtTextureEnum)GUILayout.SelectionGrid((int)this._amplifyBloom.LensDirtTexture, this._possibleLensDirtTextureEnumNames, 2, UIUtils.buttonStyleStrechWidth);
				}

				this._amplifyBloom.ApplyLensStardurst = UIUtils.ToggleGUI(this._amplifyBloom.ApplyLensStardurst, new GUIContent("Lens Starburst", "Settings for Lens Starburts composition."), UIUtils.titleStyle2);
				if (this._amplifyBloom.ApplyLensStardurst)
				{
					this._amplifyBloom.LensStarburstStrength = UIUtils.SliderGUI(this._amplifyBloom.LensStarburstStrength, 0f, 8f, 2f, "Intensity", "Lens Starburst Intensity.", "N3");
					//EditorGUI.indentLevel++;
					//m_lensStarburstWeightsFoldout = CustomFoldout(m_lensStarburstWeightsFoldout, m_lensWeightsFoldoutGC);
					//if (m_lensStarburstWeightsFoldout)
					//{
					//    for (int i = 0; i < this._amplifyBloom.BloomDownsampleCount; i++)
					//    {
					//        int id = this._amplifyBloom.BloomDownsampleCount - 1 - i;
					//        this._amplifyBloom.LensStarburstWeights[id] = UIUtils.SliderGUI(this._amplifyBloom.LensStarburstWeights[id], 0f, 1f, 0f, m_lensWeightGCArr[i]);
					//        this._amplifyBloom.LensStarburstWeights[id] = Mathf.Max(this._amplifyBloom.LensStarburstWeights[id], 0f);
					//    }
					//}
					GUILayout.Label(new GUIContent("Starburst Texture", "Mask from which starburst is going to be created."), UIUtils.labelStyle);
					this._amplifyBloom.LensStardurstTex = (LensStarburstTextureEnum)GUILayout.SelectionGrid((int)this._amplifyBloom.LensStardurstTex, this._possibleLensStarburstTextureEnumNames, 2, UIUtils.buttonStyleStrechWidth);
				}

				this._amplifyBloom.BokehFilterInstance.ApplyBokeh = UIUtils.ToggleGUI(this._amplifyBloom.BokehFilterInstance.ApplyBokeh, new GUIContent("Bokeh Filter", "Settings for Bokeh filter generation."), UIUtils.titleStyle2);
				if (this._amplifyBloom.BokehFilterInstance.ApplyBokeh)
				{
					this._amplifyBloom.BokehFilterInstance.ApplyOnBloomSource = UIUtils.ToggleGUI(this._amplifyBloom.BokehFilterInstance.ApplyOnBloomSource, new GUIContent("Apply on Bloom Source", "Bokeh filtering can either be applied on the bloom source and visually affect it or only affect features (lens flare/glare/dirt/starburst)."));
					GUILayout.Label(new GUIContent("Aperture Shape", "Type of bokeh filter which will reshape bloom results."), UIUtils.labelStyle);
					this._amplifyBloom.BokehFilterInstance.ApertureShape = (ApertureShape)GUILayout.SelectionGrid((int)this._amplifyBloom.BokehFilterInstance.ApertureShape, this._possibleApertureShapeNames, 3, UIUtils.buttonStyleStrechWidth);

					this._amplifyBloom.BokehFilterInstance.OffsetRotation = UIUtils.SliderGUI(this._amplifyBloom.BokehFilterInstance.OffsetRotation, 0, 360, 0f, "Rotation", "Filter overall rotation.", "0");
					this._amplifyBloom.BokehFilterInstance.BokehSampleRadius = UIUtils.SliderGUI(this._amplifyBloom.BokehFilterInstance.BokehSampleRadius, 0.01f, 1f, 0.5f, "Sample Radius", "Bokeh imaginary camera DOF's radius.", "N3");
					this._amplifyBloom.BokehFilterInstance.Aperture = UIUtils.SliderGUI(this._amplifyBloom.BokehFilterInstance.Aperture, 0.01f, 0.05f, 0.05f, "Aperture", "Bokeh imaginary camera DOF's aperture.", "N3");
					this._amplifyBloom.BokehFilterInstance.FocalLength = UIUtils.SliderGUI(this._amplifyBloom.BokehFilterInstance.FocalLength, 0.018f, 0.055f, 0.018f, "Focal Length", "Bokeh imaginary camera DOF's focal length.", "N3");
					this._amplifyBloom.BokehFilterInstance.FocalDistance = UIUtils.SliderGUI(this._amplifyBloom.BokehFilterInstance.FocalDistance, 0.055f, 3f, 1.34f, "Focal Distance", "Bokeh imaginary camera DOF's focal distance.", "N3");
					this._amplifyBloom.BokehFilterInstance.MaxCoCDiameter = UIUtils.SliderGUI(this._amplifyBloom.BokehFilterInstance.MaxCoCDiameter, 0f, 2f, 0.18f, "Max CoC Diameter", "Bokeh imaginary camera DOF's Max Circle of Confusion diameter.", "N3");
				}

				this._amplifyBloom.LensFlareInstance.ApplyLensFlare = UIUtils.ToggleGUI(this._amplifyBloom.LensFlareInstance.ApplyLensFlare, new GUIContent("Lens Flare", "Overall settings for Lens Flare (Halo/Ghosts) generation."), UIUtils.titleStyle2);
				if (this._amplifyBloom.LensFlareInstance.ApplyLensFlare)
				{
					this._amplifyBloom.LensFlareInstance.OverallIntensity = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.OverallIntensity, 0f, 8f, 1f, "Intensity", "Overall intensity for both halo and ghosts.", "N3");
					this._amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount = Mathf.RoundToInt(UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount, 0, 3, 1, "Blur amount", "Amount of blur applied on generated halo and ghosts.", "0"));


					UIUtils.Gradient(this._amplifyBloom.LensFlareInstance.LensFlareGradient, "Radial Tint", "Dynamic tint color applied to halo and ghosts according to its screen position. Left most color on gradient corresponds to screen center.", false, () =>
					{
						this._amplifyBloom.LensFlareInstance.TextureFromGradient();
					}, this._amplifyBloom.LensFlareInstance.LensFlareGradientTexture);

					{
						GUILayout.Label("Ghosts", UIUtils.titleStyle2);
						this._amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity, 0f, 8f, 0.8f, "Intensity", "Ghosts intensity.", "N3");
						this._amplifyBloom.LensFlareInstance.LensFlareGhostAmount = Mathf.RoundToInt(UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareGhostAmount, 3, AmplifyBloom.MaxGhosts, 0f, "Count", "Amount of ghosts generated from each bloom area.", "0"));
						this._amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal, 0.01f, 1.0f, 0.228f, "Dispersal", "Distance between ghost generated from the same bloom area.", "N3");
						this._amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion, 0, 10, 2, "Chromatic Distortion", "Amount of chromatic distortion applied on each ghost.", "N3");
						this._amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor, 1, 2, 1, "Power Factor", "Base on ghost fade power function.", "N3");
						this._amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff, 1, 128, 128, "Power Falloff", "Exponent on ghost fade power function.", "N1");
					}

					{
						GUILayout.Label("Halo", UIUtils.titleStyle2);
						this._amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity, 0f, 1f, 0.1f, "Intensity", "Halo intensity.", "N3");
						this._amplifyBloom.LensFlareInstance.LensFlareHaloWidth = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareHaloWidth, 0, 1, 0.573f, "Width", "Width/Radius of the generated halo.", "N3");
						this._amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion, 0, 10, 1.51f, "Chromatic Distortion", "Amount of chromatic distortion applied on halo.", "N3");
						this._amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor, 1, 2, 1f, "Power Factor", "Base on halo fade power function.", "N3");
						this._amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff = UIUtils.SliderGUI(this._amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff, 1, 128, 128f, "Power Falloff", "Exponent on halo fade power function.", "N1");
					}
				}

				this._amplifyBloom.LensGlareInstance.ApplyLensGlare = UIUtils.ToggleGUI(this._amplifyBloom.LensGlareInstance.ApplyLensGlare, new GUIContent("Lens Glare", "Settings for Anamorphic Lens Glare generation."), UIUtils.titleStyle2);
				if (this._amplifyBloom.LensGlareInstance.ApplyLensGlare)
				{
					this._amplifyBloom.LensGlareInstance.Intensity = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.Intensity, 0f, 0.5f, 0.17f, "Intensity", "Lens Glare intensity.", "N3");
					this._amplifyBloom.LensGlareInstance.OverallStreakScale = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.OverallStreakScale, 0, 2, 1, "Streak Scale", "Overall glare streak length modifier.", "N3");
					UIUtils.ColorPickerGUI(this._amplifyBloom.LensGlareInstance.OverallTint, Color.white, "Overall Tint", "Tint applied uniformly across each type of glare.", (c) => { this._amplifyBloom.LensGlareInstance.OverallTint = c; });

					UIUtils.Gradient(this._amplifyBloom.LensGlareInstance.CromaticColorGradient, "Tint Along Glare", "Tint for spectral types along each ray.Leftmost color on the gradient corresponds to sample near bloom source.", false, () =>
					{
						this._amplifyBloom.LensGlareInstance.SetDirty();
					});

					GUILayout.Label(new GUIContent("Type", "Type of glare."), UIUtils.labelStyle);
					this._amplifyBloom.LensGlareInstance.CurrentGlare = (GlareLibType)GUILayout.SelectionGrid((int)this._amplifyBloom.LensGlareInstance.CurrentGlare, this._possibleGlareLibTypeNames, 2, UIUtils.buttonStyleStrechWidth);
					/*
                    if (this._amplifyBloom.LensGlareInstance.CurrentGlare == GlareLibType.Custom)
                    {
                        EditorGUI.indentLevel++;
                        this._amplifyBloom.LensGlareInstance.CustomGlareDefAmount = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDefAmount, 0, AmplifyGlare.MaxCustomGlare, 0f, m_customGlareSizeGC, "N3");
                        if (this._amplifyBloom.LensGlareInstance.CustomGlareDefAmount > 0)
                        {
                            this._amplifyBloom.LensGlareInstance.CustomGlareDefIdx = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDefIdx, 0, this._amplifyBloom.LensGlareInstance.CustomGlareDef.Length - 1, 0f, m_customGlareIdxGC, "N3");
                            for (int i = 0; i < this._amplifyBloom.LensGlareInstance.CustomGlareDef.Length; i++)
                            {
                                EditorGUI.indentLevel++;
                                m_customGlareFoldoutGC.text = "[" + i + "] " + this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.StarName;
                                this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].FoldoutValue = CustomFoldout(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].FoldoutValue, m_customGlareFoldoutGC);
                                if (this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].FoldoutValue)
                                {
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.StarName = GUILayout.TextField(m_customGlareNameGC, this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.StarName);
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].StarInclinationDeg = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].StarInclinationDeg, 0, 180, 0f, m_customGlareStarInclinationGC, "N3");
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].ChromaticAberration = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].ChromaticAberration, 0, 1, 0f, m_customGlareChromaticAberrationGC, "N3");
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.StarlinesCount = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.StarlinesCount, 1, AmplifyGlare.MaxStarLines, 0f, m_customGlareStarlinesCountGC, "N3");
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.PassCount = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.PassCount, 1, AmplifyGlare.MaxPasses, 0f, m_customGlarePassCountGC, "N3");
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.SampleLength = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.SampleLength, 0, 2, 0f, m_customGlareSampleLengthGC, "N3");
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.Attenuation = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.Attenuation, 0, 1, 0f, m_customGlareAttenuationGC, "N3");
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.CameraRotInfluence = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.CameraRotInfluence, 0f, 1f, 0f, m_customGlareRotationGC);
                                    ;
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.CustomIncrement = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.CustomIncrement, 0, 180, 0f, m_customGlareCustomIncrementGC, "N3");
                                    this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.LongAttenuation = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.CustomGlareDef[i].CustomStarData.LongAttenuation, 0, 1, 0f, m_customGlareLongAttenuationGC, "N3");
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                    */
					{
						this._amplifyBloom.LensGlareInstance.PerPassDisplacement = UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.PerPassDisplacement, 1, 8, 4, "Per Pass Displacement", "Distance between samples when creating each ray.", "N3");
						this._amplifyBloom.LensGlareInstance.GlareMaxPassCount = Mathf.RoundToInt(UIUtils.SliderGUI(this._amplifyBloom.LensGlareInstance.GlareMaxPassCount, 1, AmplifyGlare.MaxPasses, 4, "Max Per Ray Passes", "Max amount of passes used to build each ray. More passes means more defined and propagated rays but decreases performance.", "0"));
					}
				}
				HSLRE.HSLRE.self.fixAmplifyBloomForUpscaledScreenshots = UIUtils.ToggleGUI(HSLRE.HSLRE.self.fixAmplifyBloomForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
				if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonStyleStrechWidth))
					System.Diagnostics.Process.Start("http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Bloom/Manual");
			}
		}

		private void BlurModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUITitle(effectData.enabled, new GUIContent(index + " Blur"), GUIStrings.disableVsEnable, UIUtils.titleStyle2, () => HSLRE.HSLRE.self.MoveUp(effectData.effect), () => HSLRE.HSLRE.self.MoveDown(effectData.effect));

			if (effectData.enabled)
			{
				this._blur.downsample = Mathf.RoundToInt(UIUtils.SliderGUI(this._blur.downsample, 0, 2, 1, "Downsample", "The number of times the image will be downsampled before the blur is applied. Bigger blurs and faster speeds can be expected the more you downsample.", "0"));
				this._blur.blurSize = UIUtils.SliderGUI(this._blur.blurSize, 0, 10, 3, "Blur Size", "The spread used when filtering the image. Higher values produce bigger blurs.", "N3");
				this._blur.blurIterations = Mathf.RoundToInt(UIUtils.SliderGUI(this._blur.blurIterations, 1, 4, 2, "Blur Iterations", "The number of times the filter operations will be repeated.", "0"));
				GUILayout.Label(new GUIContent("Type", "Type of glare."), UIUtils.labelStyle);
				this._blur.blurType = (BlurOptimized.BlurType)GUILayout.SelectionGrid((int)this._blur.blurType, this._possibleBlurTypeNames, 2, UIUtils.buttonStyleStrechWidth);
				HSLRE.HSLRE.self.fixBlurForUpscaledScreenshots = UIUtils.ToggleGUI(HSLRE.HSLRE.self.fixBlurForUpscaledScreenshots, new GUIContent("Fix Upscaled Screenshots", ""));
			}
		}


		private void GenericModule(HSLRE.HSLRE.EffectData effectData, int index)
		{
			effectData.enabled = UIUtils.ToggleGUI(effectData.enabled, new GUIContent(index + " " + effectData.effect.GetType().Name), UIUtils.titleStyle2);
		}


		private void CharaMakerWarningWindow(int id)
		{
			GUILayout.BeginVertical();
			GUILayout.Label("Using image based lighting in the chara maker requires deferred renderingpath and HDR, meanwhile you can't use background image. DO you want to continue?", UIUtils.labelStyle3);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Just this time", UIUtils.buttonStyleNoStretch))
			{
				Console.WriteLine("HSIBL Info: Changing rendering path to deferred shading.");
				Camera.main.renderingPath = RenderingPath.DeferredShading;
				Camera.main.clearFlags = (CameraClearFlags)HSLRE.Settings.basicSettings.CharaMakerBackgroundType;
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Always", UIUtils.buttonStyleNoStretch))
			{
				HSLRE.Settings.basicSettings.CharaMakerReform = true;
				Console.WriteLine("HSIBL Info: Changing rendering path to deferred shading.");
				Camera.main.renderingPath = RenderingPath.DeferredShading;
				Camera.main.clearFlags = (CameraClearFlags)HSLRE.Settings.basicSettings.CharaMakerBackgroundType;
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("No", UIUtils.buttonStyleNoStretch))
			{
				_showUI = false;
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();

		}

		private void ErrorWindow(int id)
		{

			GUILayout.BeginVertical();
			GUILayout.Label("Error" + _errorcode + ": Please make sure you have installed HS linear rendering experiment (Version ≥ 3).", UIUtils.labelStyle3);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("OK", UIUtils.buttonStyleNoStretch))
			{
				_showUI = false;
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
		}
		#endregion

		#region Private Methods
		private void CameraControlOffOnGUI()
		{
			switch (Event.current.type)
			{
				case EventType.MouseDown:
				case EventType.MouseDrag:
					this.cameraCtrlOff = true;
					break;
				default:
					return;
			}

		}

		private IEnumerator RotateCharater()
		{
			while (true)
			{
				if (this._autoRotate && this._cMfemale != null)
				{
					this._charaRotate += this._autoRotateSpeed;
					if (this._charaRotate > 180f)
					{
						this._charaRotate -= 360f;
					}
					else if (this._charaRotate < -180f)
					{
						this._charaRotate += 360f;
					}
					this._cMfemale.SetRotation(this._rotateValue.x, this._rotateValue.y + this._charaRotate, this._rotateValue.z);
				}
				yield return new WaitForEndOfFrame();
			}
		}

		private void CharaRotateModule()
		{
			if (this._cMfemale)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Rotate Character", UIUtils.titleStyle2);
				GUILayout.FlexibleSpace();
				this._autoRotate = UIUtils.ToggleButton(this._autoRotate, new GUIContent("Auto Rotate"));
				GUILayout.EndHorizontal();
				if (this._autoRotate)
				{
					GUILayout.Label("Auto Rotate Speed " + this._autoRotateSpeed.ToString("N3"), UIUtils.labelStyle);
					this._autoRotateSpeed = GUILayout.HorizontalSlider(this._autoRotateSpeed, -5f, 5f, UIUtils.sliderStyle, UIUtils.thumbStyle);
				}
				else
				{
					this._charaRotate = GUILayout.HorizontalSlider(this._charaRotate, -180f, 180f, UIUtils.sliderStyle, UIUtils.thumbStyle);
					this._cMfemale.SetRotation(this._rotateValue.x, this._rotateValue.y + this._charaRotate, this._rotateValue.z);
				}
				GUILayout.Space(UIUtils.space);
			}
		}

		private void UserCustomModule()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(GUIStrings.customWindow, UIUtils.titleStyle2);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(GUIStrings.customWindowRemember, UIUtils.buttonStyleNoStretch, GUILayout.ExpandWidth(false)))
			{
				ModPrefs.SetFloat("HSIBL", "Window.width", UIUtils.windowRect.width);
				ModPrefs.SetFloat("HSIBL", "Window.height", UIUtils.windowRect.height);
				ModPrefs.SetFloat("HSIBL", "Window.x", UIUtils.windowRect.x);
				ModPrefs.SetFloat("HSIBL", "Window.y", UIUtils.windowRect.y);
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(UIUtils.space);
			float widthtemp = UIUtils.SliderGUI(
												UIUtils.windowRect.width,
												UIUtils.minWidth,
												UIUtils.Screen.width * 0.5f,
												this._getWindowWidth,
												GUIStrings.windowWidth,
											   "N0");
			if (Mathf.Approximately(widthtemp, UIUtils.windowRect.width) == false)
			{
				UIUtils.windowRect.x += (UIUtils.windowRect.width - widthtemp) * (UIUtils.windowRect.x) / (UIUtils.Screen.width - UIUtils.windowRect.width);
				UIUtils.windowRect.width = widthtemp;
			}
			UIUtils.windowRect.height = UIUtils.SliderGUI(
														  UIUtils.windowRect.height,
														  UIUtils.Screen.height * 0.2f,
														  UIUtils.Screen.height * 0.9f,
														  this._getWindowHeight,
														  GUIStrings.windowHeight,
														 "N0");


			if (_isStudio == false && GUILayout.Button("Set current cubemap as default for the chara maker", UIUtils.buttonStyleStrechWidth))
			{
				if (this._selectedCubemap == -1)
					this._defaultCharaMakerCubemap = "";
				else
					this._defaultCharaMakerCubemap = this._cubeMapFileNames[this._selectedCubemap];
			}

			GUILayout.Label("Presets", UIUtils.titleStyle2);
			GUILayout.BeginVertical(GUI.skin.box);
			this._presetsScroll = GUILayout.BeginScrollView(this._presetsScroll, false, true, GUILayout.MinHeight(160), GUILayout.MaxHeight(800));
			if (this._presets.Length != 0)
				foreach (string preset in this._presets)
				{
					if (GUILayout.Button(preset, UIUtils.buttonStyleStrechWidth))
					{
						if (this._removePresetMode)
							this.DeletePreset(preset + ".xml");
						else
							this.LoadPreset(preset + ".xml");
					}
				}
			else
				GUILayout.Label("No preset...", UIUtils.labelStyle);
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name: ", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
			Color c = GUI.color;
			if (this._presets.Any(p => p.Equals(this._presetName, StringComparison.OrdinalIgnoreCase)))
				GUI.color = Color.red;
			this._presetName = GUILayout.TextField(this._presetName, UIUtils.textFieldStyle2);
			GUI.color = c;
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUI.enabled = this._presetName.Length != 0;
			if (GUILayout.Button("Save current settings", UIUtils.buttonStyleStrechWidth))
			{
				this._presetName = this._presetName.Trim();
				this._presetName = string.Join("_", this._presetName.Split(Path.GetInvalidFileNameChars()));
				if (this._presetName.Length != 0)
				{
					this.SavePreset(this._presetName + ".xml");
					this.RefreshPresetList();
					this._removePresetMode = false;
				}
			}
			GUI.enabled = true;
			if (this._removePresetMode)
				GUI.color = Color.red;
			GUI.enabled = this._presets.Length != 0;
			if (GUILayout.Button(this._removePresetMode ? "Click on preset" : "Delete preset", UIUtils.buttonStyleStrechWidth))
				this._removePresetMode = !this._removePresetMode;
			GUI.enabled = true;
			GUI.color = c;
			GUILayout.EndHorizontal();
		}

		private void RefreshPresetList()
		{
			if (Directory.Exists(_presetFolder))
			{
				this._presets = Directory.GetFiles(_presetFolder, "*.xml");
				for (int i = 0; i < this._presets.Length; i++)
					this._presets[i] = Path.GetFileNameWithoutExtension(this._presets[i]);
			}
		}

		private IEnumerator EndOfFrame()
		{
			while (true)
			{
				if (this._environmentUpdateFlag)
				{
					this.UpdateEnvironment();
					this._environmentUpdateFlag = false;
				}
				yield return new WaitForEndOfFrame();
			}
		}

		private void UpdateEnvironment()
		{
			DynamicGI.UpdateEnvironment();
			if (this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
				this._probeComponent.RenderProbe();
		}

		private int LoadCubemap(int index)
		{
			if (this._previousSelectedCubeMap != index)
			{
				switch (index)
				{
					case -1:
						this._skybox.Skybox = this._originalSkybox;
						this._skybox.ApplySkybox();
						//RenderSettings.skybox = this._originalSkybox;
						RenderSettings.ambientMode = this._originalAmbientMode;
						RenderSettings.defaultReflectionMode = this._originalDefaultReflectionMode;
						this._cubemapLoaded = false;
						this._environmentUpdateFlag = true;
						break;
					case 0:
						this._proceduralSkybox.ApplySkybox();
						this._proceduralSkybox.ApplySkyboxParams();
						this._cubemapLoaded = true;
						this._environmentUpdateFlag = true;
						break;
					default:
						this.StartCoroutine(this.LoadCubemapFileAsync(this._cubemapFolder.lstFile[index - 1].FileName));
						break;
				}
				this._environmentUpdateFlag = true;
			}
			this._previousSelectedCubeMap = index;
			return index;
		}

		private int LoadCubemap(string cubemapName)
		{
			int index;
			switch (cubemapName)
			{
				case "":
					index = -1;
					break;
				case "Procedural":
					index = 0;
					break;
				default:
					index = -1;
					for (int i = 0; i < this._cubemapFolder.lstFile.Count; i++)
					{
						FolderAssist.FileInfo fileInfo = this._cubemapFolder.lstFile[i];
						if (string.Compare(cubemapName, fileInfo.FileName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							index = i + 1;
							break;
						}
					}
					break;
			}
			return this.LoadCubemap(index);
		}

		private IEnumerator LoadCubemapFileAsync(string filename)
		{
			this._isLoading = true;
			AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(Application.dataPath + "/../abdata/plastic/cubemaps/" + filename + ".unity3d");
			yield return assetBundleCreateRequest;
			AssetBundle cubemapbundle = assetBundleCreateRequest.assetBundle;
			AssetBundleRequest bundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetAsync<Material>("skybox");
			yield return bundleRequest;
			this._skybox.Skybox = bundleRequest.asset as Material;
			cubemapbundle.Unload(false);
			this._skybox.ApplySkybox();
			this._skybox.ApplySkyboxParams();
			this._environmentUpdateFlag = true;

			this._cubemapLoaded = true;
			cubemapbundle = null;
			bundleRequest = null;
			assetBundleCreateRequest = null;
			Resources.UnloadUnusedAssets();
			this._isLoading = false;
			yield break;
		}
		#endregion

		#region Saves
		private void OnSceneLoad(string path, XmlNode node)
		{
			this.StartCoroutine(this.OnSceneLoad_Routine(node));
		}

		private IEnumerator OnSceneLoad_Routine(XmlNode node)
		{
			yield return null;
			yield return null;
			if (node == null)
				yield break;
			this.LoadConfig(node);
		}

		private void OnSceneSave(string path, XmlTextWriter writer)
		{
			this.SaveConfig(writer);
		}

		private void SavePreset(string name)
		{
			if (Directory.Exists(_presetFolder) == false)
				Directory.CreateDirectory(_presetFolder);
			using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(_presetFolder, name), Encoding.UTF8))
			{
				writer.WriteStartElement("root");
				this.SaveConfig(writer);
				writer.WriteEndElement();
			}
		}

		private void LoadPreset(string name)
		{
			string path = Path.Combine(_presetFolder, name);
			if (File.Exists(path) == false)
				return;
			XmlDocument doc = new XmlDocument();
			doc.Load(path);
			this.LoadConfig(doc.FirstChild);
		}

		private void DeletePreset(string name)
		{
			File.Delete(Path.GetFullPath(Path.Combine(_presetFolder, name)));
			this._removePresetMode = false;
			this.RefreshPresetList();
		}

		private void LoadConfig(XmlNode node)
		{
			List<KeyValuePair<int, ObjectCtrlInfo>> dic = null;
			if (_isStudio)
				dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
			HSLRE.HSLRE.self.ResetOrder();
			foreach (XmlNode moduleNode in node.ChildNodes)
			{
				switch (moduleNode.Name)
				{
					case "cubemap":
						this._hideSkybox = XmlConvert.ToBoolean(moduleNode.Attributes["hide"].Value);
						int index = XmlConvert.ToInt32(moduleNode.Attributes["index"].Value);
						if (index == 0) //Procedural
							this._selectedCubemap = this.LoadCubemap(index);
						else //Other
							this._selectedCubemap = this.LoadCubemap(moduleNode.Attributes["fileName"].Value);
						break;
					case "skybox":
						this._proceduralSkybox.skyboxparams.exposure = XmlConvert.ToSingle(moduleNode.Attributes["proceduralExposure"].Value);
						this._proceduralSkybox.skyboxparams.sunsize = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSunsize"].Value);
						this._proceduralSkybox.skyboxparams.atmospherethickness = XmlConvert.ToSingle(moduleNode.Attributes["proceduralAtmospherethickness"].Value);
						Color c = Color.black;
						c.r = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintR"].Value);
						c.g = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintG"].Value);
						c.b = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintB"].Value);
						this._proceduralSkybox.skyboxparams.skytint = c;
						c.r = XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorR"].Value);
						c.g = XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorG"].Value);
						c.b = XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorB"].Value);
						this._proceduralSkybox.skyboxparams.groundcolor = c;

						this._skybox.skyboxparams.rotation = XmlConvert.ToSingle(moduleNode.Attributes["classicRotation"].Value);
						this._skybox.skyboxparams.exposure = XmlConvert.ToSingle(moduleNode.Attributes["classicExposure"].Value);
						c.r = XmlConvert.ToSingle(moduleNode.Attributes["classicTintR"].Value);
						c.g = XmlConvert.ToSingle(moduleNode.Attributes["classicTintG"].Value);
						c.b = XmlConvert.ToSingle(moduleNode.Attributes["classicTintB"].Value);
						this._skybox.skyboxparams.tint = c;
						RenderSettings.ambientIntensity = XmlConvert.ToSingle(moduleNode.Attributes["ambientIntensity"].Value);
						break;
					case "reflection":
						this._probeComponent.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
						this._probeComponent.refreshMode = XmlConvert.ToBoolean(moduleNode.Attributes["refreshOnDemand"].Value) ? ReflectionProbeRefreshMode.ViaScripting : ReflectionProbeRefreshMode.EveryFrame;
						this._probeComponent.timeSlicingMode = (ReflectionProbeTimeSlicingMode)XmlConvert.ToInt32(moduleNode.Attributes["timeSlicing"].Value);
						this._reflectionProbeResolution = XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
						this._probeComponent.resolution = this._possibleReflectionProbeResolutions[this._reflectionProbeResolution];
						this._probeComponent.intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
						if (moduleNode.Attributes["positionX"] != null)
							this.probeGameObject.transform.position = new Vector3(
																				  XmlConvert.ToSingle(moduleNode.Attributes["positionX"].Value),
																				  XmlConvert.ToSingle(moduleNode.Attributes["positionY"].Value),
																				  XmlConvert.ToSingle(moduleNode.Attributes["positionZ"].Value)
																				 );
						if (moduleNode.Attributes["shadowDistance"] != null)
							this._probeComponent.shadowDistance = XmlConvert.ToSingle(moduleNode.Attributes["shadowDistance"].Value);
						this._probeComponent.nearClipPlane = moduleNode.Attributes["nearClipPlane"] != null ? XmlConvert.ToSingle(moduleNode.Attributes["nearClipPlane"].Value) : 1.1f;
						RenderSettings.reflectionBounces = moduleNode.Attributes["bounce"] != null ? XmlConvert.ToInt32(moduleNode.Attributes["bounce"].Value) : 1;
						this._probeComponent.boxProjection = moduleNode.Attributes["boxProjection"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["boxProjection"].Value);
						if (moduleNode.Attributes["cullingMask"] != null)
							this._probeComponent.cullingMask = XmlConvert.ToInt32(moduleNode.Attributes["cullingMask"].Value);
						else
						{
							this._probeComponent.cullingMask = -1;
							this._probeComponent.cullingMask &= ~(1 << 24); //Suimono Water
						}
						this._probeComponent.cullingMask &= ~(1 << 27); //SEGI GI Blockers
						this._probeComponent.cullingMask &= ~(1 << 26); //Suimono FX
						this._probeComponent.cullingMask &= ~(1 << 25); //Suimono Depth
						break;
					case "defaultLight":
						bool frontAnchor = XmlConvert.ToBoolean(moduleNode.Attributes["frontAnchoredToCamera"].Value);
						if (frontAnchor)
						{
							this._lightsObj.transform.parent = Camera.main.transform;
							this._frontDirectionalLight.transform.parent = this._lightsObj.transform;
							this._lightsObj.transform.localRotation = _isStudio && Studio.Studio.Instance != null ? Quaternion.Euler(Studio.Studio.Instance.sceneInfo.cameraLightRot[0], Studio.Studio.Instance.sceneInfo.cameraLightRot[1], 0f) : this._lightsObjDefaultRotation;
							this._frontDirectionalLight.transform.localRotation = this._frontLightDefaultRotation;
						}
						else
						{
							this._lightsObj.transform.parent = null;
							this._frontDirectionalLight.transform.parent = null;
							this._frontDirectionalLight.transform.eulerAngles = this._frontRotate;
						}

						this._frontRotate.x = XmlConvert.ToSingle(moduleNode.Attributes["frontRotateX"].Value);
						this._frontRotate.y = XmlConvert.ToSingle(moduleNode.Attributes["frontRotateY"].Value);

						this._frontDirectionalLight.intensity = XmlConvert.ToSingle(moduleNode.Attributes["frontIntensity"].Value);
						c = Color.black;
						c.r = XmlConvert.ToSingle(moduleNode.Attributes["frontColorR"].Value);
						c.g = XmlConvert.ToSingle(moduleNode.Attributes["frontColorG"].Value);
						c.b = XmlConvert.ToSingle(moduleNode.Attributes["frontColorB"].Value);
						this._frontDirectionalLight.color = c;

						bool backAnchor = XmlConvert.ToBoolean(moduleNode.Attributes["backAnchoredToCamera"].Value);
						this._backDirectionalLight.transform.parent = backAnchor == false ? null : Camera.main.transform;

						if (backAnchor)
						{
							this._backDirectionalLight.transform.parent = Camera.main.transform;
							this._backDirectionalLight.transform.localRotation = this._backLightDefaultRotation;
						}
						else
						{
							this._backDirectionalLight.transform.parent = null;
							this._backDirectionalLight.transform.eulerAngles = this._backRotate;
						}

						this._backRotate.x = XmlConvert.ToSingle(moduleNode.Attributes["backRotateX"].Value);
						this._backRotate.y = XmlConvert.ToSingle(moduleNode.Attributes["backRotateY"].Value);

						this._backDirectionalLight.intensity = XmlConvert.ToSingle(moduleNode.Attributes["backIntensity"].Value);
						c = Color.black;
						c.r = XmlConvert.ToSingle(moduleNode.Attributes["backColorR"].Value);
						c.g = XmlConvert.ToSingle(moduleNode.Attributes["backColorG"].Value);
						c.b = XmlConvert.ToSingle(moduleNode.Attributes["backColorB"].Value);
						this._backDirectionalLight.color = c;

						break;

					case "smaa":
						if (this._smaa != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._smaa];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							SMAA.GlobalSettings settings = this._smaa.settings;
							SMAA.PredicationSettings predication = this._smaa.predication;
							SMAA.TemporalSettings temporal = this._smaa.temporal;
							SMAA.QualitySettings quality = this._smaa.quality;

							settings.debugPass = (SMAA.DebugPass)XmlConvert.ToInt32(moduleNode.Attributes["debugPass"].Value);
							settings.quality = (SMAA.QualityPreset)XmlConvert.ToInt32(moduleNode.Attributes["quality"].Value);
							settings.edgeDetectionMethod = (SMAA.EdgeDetectionMethod)XmlConvert.ToInt32(moduleNode.Attributes["edgeDetectionMethod"].Value);
							quality.diagonalDetection = XmlConvert.ToBoolean(moduleNode.Attributes["qualityDiagonalDetection"].Value);
							quality.cornerDetection = XmlConvert.ToBoolean(moduleNode.Attributes["qualityCornerDetection"].Value);
							quality.threshold = XmlConvert.ToSingle(moduleNode.Attributes["qualityThreshold"].Value);
							quality.depthThreshold = XmlConvert.ToSingle(moduleNode.Attributes["qualityDepthThreshold"].Value);
							quality.maxSearchSteps = XmlConvert.ToInt32(moduleNode.Attributes["qualityMaxSearchSteps"].Value);
							quality.maxDiagonalSearchSteps = XmlConvert.ToInt32(moduleNode.Attributes["qualityMaxDiagonalSearchSteps"].Value);
							quality.cornerRounding = XmlConvert.ToInt32(moduleNode.Attributes["qualityCornerRounding"].Value);
							quality.localContrastAdaptationFactor = XmlConvert.ToSingle(moduleNode.Attributes["qualityLocalContrastAdaptationFactor"].Value);
							predication.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["predicationEnabled"].Value);
							predication.threshold = XmlConvert.ToSingle(moduleNode.Attributes["predicationThreshold"].Value);
							predication.scale = XmlConvert.ToSingle(moduleNode.Attributes["predicationScale"].Value);
							predication.strength = XmlConvert.ToSingle(moduleNode.Attributes["predicationStrength"].Value);
							temporal.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["temporalEnabled"].Value);
							temporal.fuzzSize = XmlConvert.ToSingle(moduleNode.Attributes["temporalFuzzSize"].Value);

							this._smaa.quality = quality;
							this._smaa.temporal = temporal;
							this._smaa.predication = predication;
							this._smaa.settings = settings;
						}
						break;

					case "lens":
						if (this._lensManager != null)
						{
							Camera.main.fieldOfView = XmlConvert.ToSingle(moduleNode.Attributes["fov"].Value);
							if (_isStudio)
								Studio.Studio.Instance.cameraCtrl.fieldOfView = Camera.main.fieldOfView;
							this._lensManager.distortion.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["distortionEnabled"].Value);
							this._lensManager.distortion.amount = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmount"].Value);
							this._lensManager.distortion.amountX = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmountX"].Value);
							this._lensManager.distortion.amountY = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmountY"].Value);
							this._lensManager.distortion.scale = XmlConvert.ToSingle(moduleNode.Attributes["distortionScale"].Value);

							this._lensManager.chromaticAberration.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["chromaticAberrationEnabled"].Value);
							this._lensManager.chromaticAberration.amount = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationAmount"].Value);

							if (moduleNode.Attributes["chromaticAberrationColorR"] != null)
							{
								c = Color.green;
								c.r = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorR"].Value);
								c.g = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorG"].Value);
								c.b = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorB"].Value);
								this._lensManager.chromaticAberration.color = c;
							}

							this._lensManager.vignette.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["vignetteEnabled"].Value);
							this._lensManager.vignette.intensity = XmlConvert.ToSingle(moduleNode.Attributes["vignetteIntensity"].Value);
							this._lensManager.vignette.smoothness = XmlConvert.ToSingle(moduleNode.Attributes["vignetteSmoothness"].Value);
							this._lensManager.vignette.roundness = XmlConvert.ToSingle(moduleNode.Attributes["vignetteRoundness"].Value);
							this._lensManager.vignette.desaturate = XmlConvert.ToSingle(moduleNode.Attributes["vignetteDesaturate"].Value);
							this._lensManager.vignette.blur = XmlConvert.ToSingle(moduleNode.Attributes["vignetteBlur"].Value);

							c = Color.black;
							c.r = XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorR"].Value);
							c.g = XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorG"].Value);
							c.b = XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorB"].Value);
							this._lensManager.vignette.color = c;
						}
						break;
					case "colorGrading":
						if (this._toneMappingManager != null)
						{
							HSLRE.CustomEffects.TonemappingColorGrading.ColorGradingSettings colorGrading = this._toneMappingManager.colorGrading;
							HSLRE.CustomEffects.TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;
							colorGrading.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							settings.temperatureShift = XmlConvert.ToSingle(moduleNode.Attributes["temperatureShift"].Value);
							settings.tint = XmlConvert.ToSingle(moduleNode.Attributes["tint"].Value);
							settings.contrast = XmlConvert.ToSingle(moduleNode.Attributes["contrast"].Value);
							settings.hue = XmlConvert.ToSingle(moduleNode.Attributes["hue"].Value);
							settings.saturation = XmlConvert.ToSingle(moduleNode.Attributes["saturation"].Value);
							settings.value = XmlConvert.ToSingle(moduleNode.Attributes["value"].Value);
							settings.vibrance = XmlConvert.ToSingle(moduleNode.Attributes["vibrance"].Value);
							settings.gain = XmlConvert.ToSingle(moduleNode.Attributes["gain"].Value);
							settings.gamma = XmlConvert.ToSingle(moduleNode.Attributes["gamma"].Value);
							colorGrading.basics = settings;
							this._toneMappingManager.colorGrading = colorGrading;
						}
						break;
					case "tonemapping":
						if (this._toneMappingManager != null)
						{
							this._tonemappingEnabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._toneMapper = (HSLRE.CustomEffects.TonemappingColorGrading.Tonemapper)XmlConvert.ToInt32(moduleNode.Attributes["tonemapper"].Value);
							this._ev = XmlConvert.ToSingle(moduleNode.Attributes["exposure"].Value);

							this._toneMappingManager.tonemapping = new HSLRE.CustomEffects.TonemappingColorGrading.TonemappingSettings
							{
								tonemapper = this._toneMapper,
								exposure = Mathf.Pow(2f, this._ev),
								enabled = this._tonemappingEnabled,
								neutralBlackIn = this._toneMappingManager.tonemapping.neutralBlackIn,
								neutralBlackOut = this._toneMappingManager.tonemapping.neutralBlackOut,
								neutralWhiteClip = this._toneMappingManager.tonemapping.neutralWhiteClip,
								neutralWhiteIn = this._toneMappingManager.tonemapping.neutralWhiteIn,
								neutralWhiteLevel = this._toneMappingManager.tonemapping.neutralWhiteLevel,
								neutralWhiteOut = this._toneMappingManager.tonemapping.neutralWhiteOut,
								curve = this._toneMappingManager.tonemapping.curve
							};
						}
						break;
					case "eyeAdaptation":
						if (this._toneMappingManager != null)
						{
							this._eyeEnabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._eyeMiddleGrey = XmlConvert.ToSingle(moduleNode.Attributes["middleGrey"].Value);
							this._eyeMax = XmlConvert.ToSingle(moduleNode.Attributes["max"].Value);
							this._eyeMin = XmlConvert.ToSingle(moduleNode.Attributes["min"].Value);
							this._eyeSpeed = XmlConvert.ToSingle(moduleNode.Attributes["speed"].Value);

							this._toneMappingManager.eyeAdaptation = new HSLRE.CustomEffects.TonemappingColorGrading.EyeAdaptationSettings
							{
								enabled = this._eyeEnabled,
								showDebug = false,
								middleGrey = this._eyeMiddleGrey,
								max = this._eyeMax,
								min = this._eyeMin,
								speed = this._eyeSpeed
							};
						}
						break;

					case "bloom":
						if (this._cinematicBloom != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._cinematicBloom];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							this._cinematicBloom.settings.intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
							this._cinematicBloom.settings.threshold = XmlConvert.ToSingle(moduleNode.Attributes["threshold"].Value);
							this._cinematicBloom.settings.softKnee = XmlConvert.ToSingle(moduleNode.Attributes["softKnee"].Value);
							this._cinematicBloom.settings.radius = XmlConvert.ToSingle(moduleNode.Attributes["radius"].Value);
							this._cinematicBloom.settings.antiFlicker = XmlConvert.ToBoolean(moduleNode.Attributes["antiFlicker"].Value);
						}
						break;

					case "ssao":
						if (this._ssao != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._ssao];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							this._ssao.Samples = (SSAOPro.SampleCount)XmlConvert.ToInt32(moduleNode.Attributes["samples"].Value);
							this._ssao.Downsampling = XmlConvert.ToInt32(moduleNode.Attributes["downsampling"].Value);
							this._ssao.Radius = XmlConvert.ToSingle(moduleNode.Attributes["radius"].Value);
							this._ssao.Intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
							this._ssao.Distance = XmlConvert.ToSingle(moduleNode.Attributes["distance"].Value);
							this._ssao.Bias = XmlConvert.ToSingle(moduleNode.Attributes["bias"].Value);
							this._ssao.LumContribution = XmlConvert.ToSingle(moduleNode.Attributes["lumContribution"].Value);
							c = Color.black;
							c.r = XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorR"].Value);
							c.g = XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorG"].Value);
							c.b = XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorB"].Value);
							this._ssao.OcclusionColor = c;
							this._ssao.CutoffDistance = XmlConvert.ToSingle(moduleNode.Attributes["cutoffDistance"].Value);
							this._ssao.CutoffFalloff = XmlConvert.ToSingle(moduleNode.Attributes["cutoffFalloff"].Value);
							this._ssao.BlurPasses = XmlConvert.ToInt32(moduleNode.Attributes["blurPasses"].Value);
							this._ssao.BlurBilateralThreshold = XmlConvert.ToSingle(moduleNode.Attributes["blurBilateralThreshold"].Value);
							this._ssao.UseHighPrecisionDepthMap = XmlConvert.ToBoolean(moduleNode.Attributes["useHighPrecisionDepthMap"].Value);
							this._ssao.Blur = (SSAOPro.BlurMode)XmlConvert.ToInt32(moduleNode.Attributes["blur"].Value);
							this._ssao.BlurDownsampling = XmlConvert.ToBoolean(moduleNode.Attributes["blurDownsampling"].Value);
							this._ssao.DebugAO = XmlConvert.ToBoolean(moduleNode.Attributes["debugAO"].Value);

							if (_isStudio && Studio.Studio.Instance != null)
							{
								Studio.Studio.Instance.sceneInfo.enableSSAO = effectData.enabled;
								Studio.Studio.Instance.sceneInfo.ssaoIntensity = this._ssao.Intensity;
								Studio.Studio.Instance.sceneInfo.ssaoColor.SetDiffuseRGBA(c);
							}
						}
						break;
					case "sunShafts":
						if (this._sunShafts != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._sunShafts];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							this._sunShafts.useDepthTexture = XmlConvert.ToBoolean(moduleNode.Attributes["useDepthTexture"].Value);
							this._sunShafts.resolution = (SunShafts.SunShaftsResolution)XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
							this._sunShafts.screenBlendMode = (SunShafts.ShaftsScreenBlendMode)XmlConvert.ToInt32(moduleNode.Attributes["screenBlendMode"].Value);
							c = Color.black;
							c.r = XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdR"].Value);
							c.g = XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdG"].Value);
							c.b = XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdB"].Value);
							this._sunShafts.sunThreshold = c;
							c.r = XmlConvert.ToSingle(moduleNode.Attributes["sunColorR"].Value);
							c.g = XmlConvert.ToSingle(moduleNode.Attributes["sunColorG"].Value);
							c.b = XmlConvert.ToSingle(moduleNode.Attributes["sunColorB"].Value);
							this._sunShafts.sunColor = c;
							this._sunShafts.maxRadius = XmlConvert.ToSingle(moduleNode.Attributes["maxRadius"].Value);
							this._sunShafts.sunShaftBlurRadius = XmlConvert.ToSingle(moduleNode.Attributes["sunShaftBlurRadius"].Value);
							this._sunShafts.radialBlurIterations = XmlConvert.ToInt32(moduleNode.Attributes["radialBlurIterations"].Value);
							this._sunShafts.sunShaftIntensity = XmlConvert.ToSingle(moduleNode.Attributes["sunShaftIntensity"].Value);
							if (_isStudio && Studio.Studio.Instance != null)
								Studio.Studio.Instance.sceneInfo.enableSunShafts = effectData.enabled;
						}

						break;
					case "depthOfField":
						if (this._depthOfField != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._depthOfField];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							this._depthOfField.visualizeFocus = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeFocus"].Value);

							//this.StartCoroutine(SetFocusType()); //Yeah, sorry about that
							//IEnumerator SetFocusType()
							//{
							//    yield return null;
							//    yield return null;
							//    yield return null;
							if (moduleNode.Attributes["useCameraOriginAsFocus"] != null)
								this._focusType = XmlConvert.ToBoolean(moduleNode.Attributes["useCameraOriginAsFocus"].Value) ? DOFFocusType.Crosshair : DOFFocusType.Manual;
							else if (moduleNode.Attributes["focusType"] != null)
								this._focusType = (DOFFocusType)XmlConvert.ToInt32(moduleNode.Attributes["focusType"].Value);
							else
								this._focusType = DOFFocusType.Manual;

							switch (this._focusType)
							{
								case DOFFocusType.Crosshair:
									this._depthOfField.focalTransform = this._depthOfFieldOriginalFocusPoint;
									break;
								case DOFFocusType.Object:
									if (_isStudio && moduleNode.Attributes["focussedObjectIndex"] != null)
									{
										List<KeyValuePair<int, ObjectCtrlInfo>> list = Studio.Studio.Instance.dicObjectCtrl.OrderBy(e => e.Key).ToList();
										int focussedObjectIndex = XmlConvert.ToInt32(moduleNode.Attributes["focussedObjectIndex"].Value);
										if (focussedObjectIndex < list.Count)
										{
											this._focussedObject = list[focussedObjectIndex].Value;
											this._depthOfField.focalTransform = this._focussedObject.guideObject.transformTarget;
										}
										else
										{
											this._focusType = DOFFocusType.Crosshair;
											goto case DOFFocusType.Crosshair;
										}
									}
									else
									{
										this._focusType = DOFFocusType.Crosshair;
										goto case DOFFocusType.Crosshair;
									}
									break;
								case DOFFocusType.Manual:
									this._depthOfField.focalTransform = null;
									break;
							}
							//}

							this._depthOfField.focalLength = XmlConvert.ToSingle(moduleNode.Attributes["focalLength"].Value);
							this._depthOfField.focalSize = XmlConvert.ToSingle(moduleNode.Attributes["focalSize"].Value);
							this._depthOfField.aperture = XmlConvert.ToSingle(moduleNode.Attributes["aperture"].Value);
							this._depthOfField.maxBlurSize = XmlConvert.ToSingle(moduleNode.Attributes["maxBlurSize"].Value);
							this._depthOfField.highResolution = XmlConvert.ToBoolean(moduleNode.Attributes["highResolution"].Value);
							this._depthOfField.nearBlur = XmlConvert.ToBoolean(moduleNode.Attributes["nearBlur"].Value);
							this._depthOfField.foregroundOverlap = XmlConvert.ToSingle(moduleNode.Attributes["foregroundOverlap"].Value);
							this._depthOfField.blurType = (DepthOfField.BlurType)XmlConvert.ToInt32(moduleNode.Attributes["blurType"].Value);
							this._depthOfField.blurSampleCount = (DepthOfField.BlurSampleCount)XmlConvert.ToInt32(moduleNode.Attributes["blurSampleCount"].Value);
							this._depthOfField.dx11BokehScale = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehScale"].Value);
							this._depthOfField.dx11BokehIntensity = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehIntensity"].Value);
							this._depthOfField.dx11BokehThreshold = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehThreshold"].Value);
							this._depthOfField.dx11SpawnHeuristic = XmlConvert.ToSingle(moduleNode.Attributes["dx11SpawnHeuristic"].Value);
							HSLRE.HSLRE.self.fixDofForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
							if (_isStudio && Studio.Studio.Instance != null)
							{
								Studio.Studio.Instance.sceneInfo.enableDepth = effectData.enabled;
								Studio.Studio.Instance.sceneInfo.depthFocalSize = this._depthOfField.focalSize;
								Studio.Studio.Instance.sceneInfo.depthAperture = this._depthOfField.aperture;
							}
						}
						break;
					case "ssr":
						if (this._ssr != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._ssr];
							effectData.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							HSLRE.HSLRE.self.fixSsrForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);

							ScreenSpaceReflection.SSRSettings ssrSettings = this._ssr.settings;

							ScreenSpaceReflection.BasicSettings basicSettings = ssrSettings.basicSettings;
							basicSettings.reflectionMultiplier = XmlConvert.ToSingle(moduleNode.Attributes["reflectionMultiplier"].Value);
							basicSettings.maxDistance = XmlConvert.ToSingle(moduleNode.Attributes["maxDistance"].Value);
							basicSettings.fadeDistance = XmlConvert.ToSingle(moduleNode.Attributes["fadeDistance"].Value);
							basicSettings.screenEdgeFading = XmlConvert.ToSingle(moduleNode.Attributes["screenEdgeFading"].Value);
							basicSettings.enableHDR = XmlConvert.ToBoolean(moduleNode.Attributes["enableHDR"].Value);
							basicSettings.additiveReflection = XmlConvert.ToBoolean(moduleNode.Attributes["additiveReflection"].Value);

							ScreenSpaceReflection.ReflectionSettings reflectionSettings = ssrSettings.reflectionSettings;
							reflectionSettings.maxSteps = XmlConvert.ToInt32(moduleNode.Attributes["maxSteps"].Value);
							reflectionSettings.rayStepSize = XmlConvert.ToInt32(moduleNode.Attributes["rayStepSize"].Value);
							reflectionSettings.widthModifier = XmlConvert.ToSingle(moduleNode.Attributes["widthModifier"].Value);
							reflectionSettings.smoothFallbackThreshold = XmlConvert.ToSingle(moduleNode.Attributes["smoothFallbackThreshold"].Value);
							reflectionSettings.smoothFallbackDistance = XmlConvert.ToSingle(moduleNode.Attributes["smoothFallbackDistance"].Value);
							reflectionSettings.fresnelFade = XmlConvert.ToSingle(moduleNode.Attributes["fresnelFade"].Value);
							reflectionSettings.fresnelFadePower = XmlConvert.ToSingle(moduleNode.Attributes["fresnelFadePower"].Value);
							reflectionSettings.distanceBlur = XmlConvert.ToSingle(moduleNode.Attributes["distanceBlur"].Value);

							ScreenSpaceReflection.AdvancedSettings advancedSettings = ssrSettings.advancedSettings;
							advancedSettings.temporalFilterStrength = XmlConvert.ToSingle(moduleNode.Attributes["temporalFilterStrength"].Value);
							advancedSettings.useTemporalConfidence = XmlConvert.ToBoolean(moduleNode.Attributes["useTemporalConfidence"].Value);
							advancedSettings.traceBehindObjects = XmlConvert.ToBoolean(moduleNode.Attributes["traceBehindObjects"].Value);
							advancedSettings.highQualitySharpReflections = XmlConvert.ToBoolean(moduleNode.Attributes["highQualitySharpReflections"].Value);
							advancedSettings.traceEverywhere = XmlConvert.ToBoolean(moduleNode.Attributes["traceEverywhere"].Value);
							advancedSettings.treatBackfaceHitAsMiss = XmlConvert.ToBoolean(moduleNode.Attributes["treatBackfaceHitAsMiss"].Value);
							advancedSettings.allowBackwardsRays = XmlConvert.ToBoolean(moduleNode.Attributes["allowBackwardsRays"].Value);
							advancedSettings.improveCorners = XmlConvert.ToBoolean(moduleNode.Attributes["improveCorners"].Value);
							advancedSettings.resolution = (ScreenSpaceReflection.SSRResolution)XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
							advancedSettings.bilateralUpsample = XmlConvert.ToBoolean(moduleNode.Attributes["bilateralUpsample"].Value);
							advancedSettings.reduceBanding = XmlConvert.ToBoolean(moduleNode.Attributes["reduceBanding"].Value);
							advancedSettings.highlightSuppression = XmlConvert.ToBoolean(moduleNode.Attributes["highlightSuppression"].Value);

							ssrSettings.basicSettings = basicSettings;
							ssrSettings.reflectionSettings = reflectionSettings;
							ssrSettings.advancedSettings = advancedSettings;
							this._ssr.settings = ssrSettings;
							if (_isStudio && Studio.Studio.Instance != null)
								Studio.Studio.Instance.sceneInfo.enableSSR = effectData.enabled;
						}

						break;
					case "bloomAndFlares":
						if (this._bloomAndFlares != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._bloomAndFlares];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							this._bloomAndFlares.screenBlendMode = (BloomScreenBlendMode)XmlConvert.ToInt32(moduleNode.Attributes["screenBlendMode"].Value);
							this._bloomAndFlares.hdr = (HDRBloomMode)XmlConvert.ToSingle(moduleNode.Attributes["hdr"].Value);

							this._bloomAndFlares.bloomIntensity = XmlConvert.ToSingle(moduleNode.Attributes["bloomIntensity"].Value);
							this._bloomAndFlares.bloomThreshold = XmlConvert.ToSingle(moduleNode.Attributes["bloomThreshold"].Value);
							this._bloomAndFlares.bloomBlurIterations = XmlConvert.ToInt32(moduleNode.Attributes["bloomBlurIterations"].Value);
							this._bloomAndFlares.sepBlurSpread = XmlConvert.ToSingle(moduleNode.Attributes["sepBlurSpread"].Value);
							this._bloomAndFlares.useSrcAlphaAsMask = XmlConvert.ToSingle(moduleNode.Attributes["useSrcAlphaAsMask"].Value);

							this._bloomAndFlares.lensflares = XmlConvert.ToBoolean(moduleNode.Attributes["lensflares"].Value);
							this._bloomAndFlares.lensflareMode = (LensflareStyle34)XmlConvert.ToSingle(moduleNode.Attributes["lensflareMode"].Value);
							this._bloomAndFlares.lensflareIntensity = XmlConvert.ToSingle(moduleNode.Attributes["lensflareIntensity"].Value);
							this._bloomAndFlares.lensflareThreshold = XmlConvert.ToSingle(moduleNode.Attributes["lensflareThreshold"].Value);

							this._bloomAndFlares.hollyStretchWidth = XmlConvert.ToSingle(moduleNode.Attributes["hollyStretchWidth"].Value);
							this._bloomAndFlares.hollywoodFlareBlurIterations = XmlConvert.ToInt32(moduleNode.Attributes["hollywoodFlareBlurIterations"].Value);

							this._bloomAndFlares.flareColorA = new Color(
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorAR"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorAG"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorAB"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorAA"].Value)
																		);

							this._bloomAndFlares.flareColorB = new Color(
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorBR"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorBG"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorBB"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorBA"].Value)
																		);

							this._bloomAndFlares.flareColorC = new Color(
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorCR"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorCG"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorCB"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorCA"].Value)
																		);

							this._bloomAndFlares.flareColorD = new Color(
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorDR"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorDG"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorDB"].Value),
																		 XmlConvert.ToSingle(moduleNode.Attributes["flareColorDA"].Value)
																		);
							if (_isStudio && Studio.Studio.Instance != null)
							{
								Studio.Studio.Instance.sceneInfo.enableBloom = effectData.enabled;
								Studio.Studio.Instance.sceneInfo.bloomIntensity = this._bloomAndFlares.bloomIntensity;
								Studio.Studio.Instance.sceneInfo.bloomBlur = this._bloomAndFlares.sepBlurSpread;
							}
						}
						break;
					case "colorCorrectionCurves":
						if (this._colorCorrectionCurves != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._colorCorrectionCurves];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._colorCorrectionCurves.saturation = XmlConvert.ToSingle(moduleNode.Attributes["saturation"].Value);
						}
						break;
					case "vignetteAndChromaticAberration":
						if (this._vignette != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._vignette];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._vignette.intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
							this._vignette.blur = XmlConvert.ToSingle(moduleNode.Attributes["blur"].Value);
							this._vignette.blurSpread = XmlConvert.ToSingle(moduleNode.Attributes["blurSpread"].Value);
							this._vignette.mode = (VignetteAndChromaticAberration.AberrationMode)XmlConvert.ToInt32(moduleNode.Attributes["mode"].Value);
							this._vignette.chromaticAberration = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberration"].Value);
							this._vignette.axialAberration = XmlConvert.ToSingle(moduleNode.Attributes["axialAberration"].Value);
							this._vignette.luminanceDependency = XmlConvert.ToSingle(moduleNode.Attributes["luminanceDependency"].Value);
							this._vignette.blurDistance = XmlConvert.ToSingle(moduleNode.Attributes["blurDistance"].Value);

							if (_isStudio && Studio.Studio.Instance != null)
							{
								Studio.Studio.Instance.sceneInfo.enableVignette = effectData.enabled;
								Studio.Studio.Instance.sceneInfo.vignetteVignetting = this._vignette.intensity;
							}
						}
						break;
					case "antialiasing":
						if (this._antialiasing != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._antialiasing];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._antialiasing.mode = (AAMode)XmlConvert.ToInt32(moduleNode.Attributes["mode"].Value);
							this._antialiasing.edgeThresholdMin = XmlConvert.ToSingle(moduleNode.Attributes["edgeThresholdMin"].Value);
							this._antialiasing.edgeThreshold = XmlConvert.ToSingle(moduleNode.Attributes["edgeThreshold"].Value);
							this._antialiasing.edgeSharpness = XmlConvert.ToSingle(moduleNode.Attributes["edgeSharpness"].Value);
							this._antialiasing.offsetScale = XmlConvert.ToSingle(moduleNode.Attributes["offsetScale"].Value);
							this._antialiasing.blurRadius = XmlConvert.ToSingle(moduleNode.Attributes["blurRadius"].Value);
							this._antialiasing.showGeneratedNormals = XmlConvert.ToBoolean(moduleNode.Attributes["showGeneratedNormals"].Value);
							this._antialiasing.dlaaSharp = XmlConvert.ToBoolean(moduleNode.Attributes["dlaaSharp"].Value);

						}
						break;
					case "noiseAndGrain":
						if (this._noiseAndGrain != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._noiseAndGrain];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._noiseAndGrain.dx11Grain = XmlConvert.ToBoolean(moduleNode.Attributes["dx11Grain"].Value);
							this._noiseAndGrain.monochrome = XmlConvert.ToBoolean(moduleNode.Attributes["monochrome"].Value);
							this._noiseAndGrain.intensityMultiplier = XmlConvert.ToSingle(moduleNode.Attributes["intensityMultiplier"].Value);
							this._noiseAndGrain.generalIntensity = XmlConvert.ToSingle(moduleNode.Attributes["generalIntensity"].Value);
							this._noiseAndGrain.blackIntensity = XmlConvert.ToSingle(moduleNode.Attributes["blackIntensity"].Value);
							this._noiseAndGrain.whiteIntensity = XmlConvert.ToSingle(moduleNode.Attributes["whiteIntensity"].Value);
							this._noiseAndGrain.midGrey = XmlConvert.ToSingle(moduleNode.Attributes["midGrey"].Value);
							this._noiseAndGrain.intensities = new Vector3(
																		  XmlConvert.ToSingle(moduleNode.Attributes["intensitiesX"].Value),
																		  XmlConvert.ToSingle(moduleNode.Attributes["intensitiesY"].Value),
																		  XmlConvert.ToSingle(moduleNode.Attributes["intensitiesZ"].Value)
																		 );
							this._noiseAndGrain.filterMode = (FilterMode)XmlConvert.ToInt32(moduleNode.Attributes["filterMode"].Value);
							this._noiseAndGrain.softness = XmlConvert.ToSingle(moduleNode.Attributes["softness"].Value);
							this._noiseAndGrain.monochromeTiling = XmlConvert.ToSingle(moduleNode.Attributes["monochromeTiling"].Value);
							this._noiseAndGrain.tiling = new Vector3(
																	 XmlConvert.ToSingle(moduleNode.Attributes["tilingX"].Value),
																	 XmlConvert.ToSingle(moduleNode.Attributes["tilingY"].Value),
																	 XmlConvert.ToSingle(moduleNode.Attributes["tilingZ"].Value)
																	);
							HSLRE.HSLRE.self.fixNoiseAndGrainForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
						}
						break;
					case "motionBlur":
						if (this._motionBlur != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._motionBlur];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._motionBlur.velocityScale = XmlConvert.ToSingle(moduleNode.Attributes["velocityScale"].Value);
							this._motionBlur.maxVelocity = XmlConvert.ToSingle(moduleNode.Attributes["maxVelocity"].Value);
							this._motionBlur.minVelocity = XmlConvert.ToSingle(moduleNode.Attributes["minVelocity"].Value);
							this._motionBlur.filterType = (CameraMotionBlur.MotionBlurFilter)XmlConvert.ToInt32(moduleNode.Attributes["filterType"].Value);
							this._motionBlur.rotationScale = XmlConvert.ToSingle(moduleNode.Attributes["rotationScale"].Value);
							this._motionBlur.movementScale = XmlConvert.ToSingle(moduleNode.Attributes["movementScale"].Value);
							this._motionBlur.velocityDownsample = XmlConvert.ToInt32(moduleNode.Attributes["velocityDownsample"].Value);
							this._motionBlur.jitter = XmlConvert.ToSingle(moduleNode.Attributes["jitter"].Value);
							this._motionBlur.preview = XmlConvert.ToBoolean(moduleNode.Attributes["preview"].Value);
							this._motionBlur.previewScale = new Vector3(
																		XmlConvert.ToSingle(moduleNode.Attributes["previewScaleX"].Value),
																		XmlConvert.ToSingle(moduleNode.Attributes["previewScaleY"].Value),
																		XmlConvert.ToSingle(moduleNode.Attributes["previewScaleZ"].Value)
																	   );
						}
						break;
					case "afterImage":
						if (this._afterImage != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._afterImage];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._afterImage.framesToMix = XmlConvert.ToInt32(moduleNode.Attributes["framesToMix"].Value);
							this._afterImage.falloffPower = moduleNode.Attributes["falloffPower"] != null ? XmlConvert.ToSingle(moduleNode.Attributes["falloffPower"].Value) : 10f / 7f;
						}
						break;
					case "volumetricLights":
						if (this._volumetricLights != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._volumetricLights];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

							this._volumetricLights.Resolution = (VolumetricLightRenderer.VolumetricResolution)XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
							if (dic != null)
							{
								foreach (XmlNode childNode in moduleNode.ChildNodes)
								{
									int i = XmlConvert.ToInt32(childNode.Attributes["index"].Value);
									if (i >= dic.Count)
										continue;
									ObjectCtrlInfo oci = dic[i].Value;
									if (oci == null || !(oci is OCILight light))
										continue;
									VolumetricLight volumetricLight = light.light.GetComponent<VolumetricLight>();
									if (volumetricLight == null)
										continue;
									volumetricLight.enabled = childNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
									volumetricLight.SampleCount = XmlConvert.ToInt32(childNode.Attributes["sampleCount"].Value);
									volumetricLight.ScatteringCoef = XmlConvert.ToSingle(childNode.Attributes["scatteringCoef"].Value);
									volumetricLight.ExtinctionCoef = XmlConvert.ToSingle(childNode.Attributes["extinctionCoef"].Value);
									volumetricLight.MieG = XmlConvert.ToSingle(childNode.Attributes["mieG"].Value);
									volumetricLight.SkyboxExtinctionCoef = XmlConvert.ToSingle(childNode.Attributes["skyboxExtinctionCoef"].Value);
									volumetricLight.MaxRayLength = XmlConvert.ToSingle(childNode.Attributes["maxRayLength"].Value);
									volumetricLight.HeightFog = XmlConvert.ToBoolean(childNode.Attributes["heightFog"].Value);
									volumetricLight.HeightScale = XmlConvert.ToSingle(childNode.Attributes["heightScale"].Value);
									volumetricLight.GroundLevel = XmlConvert.ToSingle(childNode.Attributes["groundLevel"].Value);
									volumetricLight.Noise = XmlConvert.ToBoolean(childNode.Attributes["noise"].Value);
									volumetricLight.NoiseScale = XmlConvert.ToSingle(childNode.Attributes["noiseScale"].Value);
									volumetricLight.NoiseIntensity = XmlConvert.ToSingle(childNode.Attributes["noiseIntensity"].Value);
									volumetricLight.NoiseIntensityOffset = XmlConvert.ToSingle(childNode.Attributes["noiseIntensityOffset"].Value);
									volumetricLight.NoiseVelocity = new Vector2(
											XmlConvert.ToSingle(childNode.Attributes["noiseVelocityX"].Value),
											XmlConvert.ToSingle(childNode.Attributes["noiseVelocityY"].Value)
									);
								}
							}
						}
						break;
					case "segi":
						if (this._segi != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._segi];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._segi.voxelResolution = (SEGI.VoxelResolution)XmlConvert.ToInt32(moduleNode.Attributes["voxelResolution"].Value);
							this._segi.voxelAA = XmlConvert.ToBoolean(moduleNode.Attributes["voxelAA"].Value);
							this._segi.innerOcclusionLayers = XmlConvert.ToInt32(moduleNode.Attributes["innerOcclusionLayers"].Value);
							this._segi.gaussianMipFilter = XmlConvert.ToBoolean(moduleNode.Attributes["gaussianMipFilter"].Value);
							this._segi.voxelSpaceSize = XmlConvert.ToSingle(moduleNode.Attributes["voxelSpaceSize"].Value);
							this._segi.shadowSpaceSize = XmlConvert.ToSingle(moduleNode.Attributes["shadowSpaceSize"].Value);
							this._segi.updateGI = XmlConvert.ToBoolean(moduleNode.Attributes["updateGI"].Value);
							this._segi.infiniteBounces = XmlConvert.ToBoolean(moduleNode.Attributes["infiniteBounces"].Value);
							this._segi.softSunlight = XmlConvert.ToSingle(moduleNode.Attributes["softSunlight"].Value);
							this._segi.hsStandardCustomShadersCompatibility = moduleNode.Attributes["hsStandardCustomShadersCompatibility"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["hsStandardCustomShadersCompatibility"].Value);

							this._segi.skyColor.r = XmlConvert.ToSingle(moduleNode.Attributes["skyColorR"].Value);
							this._segi.skyColor.g = XmlConvert.ToSingle(moduleNode.Attributes["skyColorG"].Value);
							this._segi.skyColor.b = XmlConvert.ToSingle(moduleNode.Attributes["skyColorB"].Value);
							this._segi.skyColor.a = XmlConvert.ToSingle(moduleNode.Attributes["skyColorA"].Value);

							this._segi.skyIntensity = XmlConvert.ToSingle(moduleNode.Attributes["skyIntensity"].Value);
							this._segi.sphericalSkylight = XmlConvert.ToBoolean(moduleNode.Attributes["sphericalSkylight"].Value);

							this._segi.temporalBlendWeight = XmlConvert.ToSingle(moduleNode.Attributes["temporalBlendWeight"].Value);
							this._segi.useBilateralFiltering = XmlConvert.ToBoolean(moduleNode.Attributes["useBilateralFiltering"].Value);
							this._segi.halfResolution = XmlConvert.ToBoolean(moduleNode.Attributes["halfResolution"].Value);
							this._segi.stochasticSampling = XmlConvert.ToBoolean(moduleNode.Attributes["stochasticSampling"].Value);

							this._segi.cones = XmlConvert.ToInt32(moduleNode.Attributes["cones"].Value);
							this._segi.coneTraceSteps = XmlConvert.ToInt32(moduleNode.Attributes["coneTraceSteps"].Value);
							this._segi.coneLength = XmlConvert.ToSingle(moduleNode.Attributes["coneLength"].Value);
							this._segi.coneWidth = XmlConvert.ToSingle(moduleNode.Attributes["coneWidth"].Value);
							this._segi.coneTraceBias = XmlConvert.ToSingle(moduleNode.Attributes["coneTraceBias"].Value);
							this._segi.occlusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["occlusionStrength"].Value);
							this._segi.nearOcclusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["nearOcclusionStrength"].Value);
							this._segi.farOcclusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["farOcclusionStrength"].Value);
							this._segi.farthestOcclusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["farthestOcclusionStrength"].Value);
							this._segi.occlusionPower = XmlConvert.ToSingle(moduleNode.Attributes["occlusionPower"].Value);
							this._segi.nearLightGain = XmlConvert.ToSingle(moduleNode.Attributes["nearLightGain"].Value);
							this._segi.giGain = XmlConvert.ToSingle(moduleNode.Attributes["giGain"].Value);

							this._segi.secondaryBounceGain = XmlConvert.ToSingle(moduleNode.Attributes["secondaryBounceGain"].Value);
							this._segi.secondaryCones = XmlConvert.ToInt32(moduleNode.Attributes["secondaryCones"].Value);
							this._segi.secondaryOcclusionStrength = XmlConvert.ToSingle(moduleNode.Attributes["secondaryOcclusionStrength"].Value);

							this._segi.doReflections = XmlConvert.ToBoolean(moduleNode.Attributes["doReflections"].Value);
							this._segi.reflectionSteps = XmlConvert.ToInt32(moduleNode.Attributes["reflectionSteps"].Value);
							this._segi.reflectionOcclusionPower = XmlConvert.ToSingle(moduleNode.Attributes["reflectionOcclusionPower"].Value);
							this._segi.skyReflectionIntensity = XmlConvert.ToSingle(moduleNode.Attributes["skyReflectionIntensity"].Value);

							this._segi.visualizeSunDepthTexture = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeSunDepthTexture"].Value);
							this._segi.visualizeGI = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeGI"].Value);
							this._segi.visualizeVoxels = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeVoxels"].Value);
						}
						break;
					case "amplifyBloom":
						if (this._amplifyBloom != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._amplifyBloom];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._amplifyBloom.UpscaleQuality = (UpscaleQualityEnum)XmlConvert.ToInt32(moduleNode.Attributes["UpscaleQuality"].Value);
							this._amplifyBloom.MainThresholdSize = (MainThresholdSizeEnum)XmlConvert.ToInt32(moduleNode.Attributes["MainThresholdSize"].Value);
							this._amplifyBloom.CurrentPrecisionMode = (PrecisionModes)XmlConvert.ToInt32(moduleNode.Attributes["CurrentPrecisionMode"].Value);
							this._amplifyBloom.BloomRange = XmlConvert.ToSingle(moduleNode.Attributes["BloomRange"].Value);
							this._amplifyBloom.OverallIntensity = XmlConvert.ToSingle(moduleNode.Attributes["OverallIntensity"].Value);
							this._amplifyBloom.OverallThreshold = XmlConvert.ToSingle(moduleNode.Attributes["OverallThreshold"].Value);

							this._amplifyBloom.UpscaleBlurRadius = moduleNode.Attributes["UpscaleBlurRadius"] != null ? XmlConvert.ToSingle(moduleNode.Attributes["UpscaleBlurRadius"].Value) : 1;

							this._amplifyBloom.DebugToScreen = (DebugToScreenEnum)XmlConvert.ToInt32(moduleNode.Attributes["DebugToScreen"].Value);
							this._amplifyBloom.TemporalFilteringActive = XmlConvert.ToBoolean(moduleNode.Attributes["TemporalFilteringActive"].Value);
							this._amplifyBloom.TemporalFilteringValue = XmlConvert.ToSingle(moduleNode.Attributes["TemporalFilteringValue"].Value);
							this._amplifyBloom.SeparateFeaturesThreshold = XmlConvert.ToBoolean(moduleNode.Attributes["SeparateFeaturesThreshold"].Value);
							this._amplifyBloom.FeaturesThreshold = XmlConvert.ToSingle(moduleNode.Attributes["FeaturesThreshold"].Value);
							this._amplifyBloom.ApplyLensDirt = XmlConvert.ToBoolean(moduleNode.Attributes["ApplyLensDirt"].Value);
							this._amplifyBloom.LensDirtStrength = XmlConvert.ToSingle(moduleNode.Attributes["LensDirtStrength"].Value);
							this._amplifyBloom.LensDirtTexture = (LensDirtTextureEnum)XmlConvert.ToInt32(moduleNode.Attributes["LensDirtTexture"].Value);
							this._amplifyBloom.ApplyLensStardurst = XmlConvert.ToBoolean(moduleNode.Attributes["ApplyLensStardurst"].Value);
							this._amplifyBloom.LensStarburstStrength = XmlConvert.ToSingle(moduleNode.Attributes["LensStarburstStrength"].Value);
							this._amplifyBloom.LensStardurstTex = (LensStarburstTextureEnum)XmlConvert.ToInt32(moduleNode.Attributes["LensStardurstTex"].Value);
							this._amplifyBloom.BokehFilterInstance.ApplyBokeh = XmlConvert.ToBoolean(moduleNode.Attributes["BokehFilterInstance.ApplyBokeh"].Value);
							this._amplifyBloom.BokehFilterInstance.ApplyOnBloomSource = XmlConvert.ToBoolean(moduleNode.Attributes["BokehFilterInstance.ApplyOnBloomSource"].Value);
							this._amplifyBloom.BokehFilterInstance.ApertureShape = (ApertureShape)XmlConvert.ToInt32(moduleNode.Attributes["BokehFilterInstance.ApertureShape"].Value);
							this._amplifyBloom.BokehFilterInstance.OffsetRotation = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.OffsetRotation"].Value);
							this._amplifyBloom.BokehFilterInstance.BokehSampleRadius = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.BokehSampleRadius"].Value);
							this._amplifyBloom.BokehFilterInstance.Aperture = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.Aperture"].Value);
							this._amplifyBloom.BokehFilterInstance.FocalLength = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.FocalLength"].Value);
							this._amplifyBloom.BokehFilterInstance.FocalDistance = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.FocalDistance"].Value);
							this._amplifyBloom.BokehFilterInstance.MaxCoCDiameter = XmlConvert.ToSingle(moduleNode.Attributes["BokehFilterInstance.MaxCoCDiameter"].Value);
							this._amplifyBloom.LensFlareInstance.ApplyLensFlare = XmlConvert.ToBoolean(moduleNode.Attributes["LensFlareInstance.ApplyLensFlare"].Value);
							this._amplifyBloom.LensFlareInstance.OverallIntensity = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.OverallIntensity"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount = XmlConvert.ToInt32(moduleNode.Attributes["LensFlareInstance.LensFlareGaussianBlurAmount"].Value);
							List<GradientColorKey> colorKeys = new List<GradientColorKey>();
							List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();

							foreach (XmlNode key in FindChildNode(moduleNode, "LensFlareInstance.LensFlareGradient"))
							{
								switch (key.Name)
								{
									case "colorKey":
										colorKeys.Add(new GradientColorKey(
																		   new Color(
																					 XmlConvert.ToSingle(key.Attributes["r"].Value),
																					 XmlConvert.ToSingle(key.Attributes["g"].Value),
																					 XmlConvert.ToSingle(key.Attributes["b"].Value)
																					),
																		   XmlConvert.ToSingle(key.Attributes["time"].Value))
																		   );
										break;
									case "alphaKey":
										alphaKeys.Add(new GradientAlphaKey(
																		   XmlConvert.ToSingle(key.Attributes["alpha"].Value),
																		   XmlConvert.ToSingle(key.Attributes["time"].Value))
																		   );
										break;
								}
							}
							this._amplifyBloom.LensFlareInstance.LensFlareGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());

							this._amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareNormalizedGhostsIntensity"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareGhostAmount = XmlConvert.ToInt32(moduleNode.Attributes["LensFlareInstance.LensFlareGhostAmount"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareGhostsDispersal"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareGhostChrDistortion"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareGhostsPowerFactor"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareGhostsPowerFalloff"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareNormalizedHaloIntensity"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareHaloWidth = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareHaloWidth"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareHaloChrDistortion"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareHaloPowerFactor"].Value);
							this._amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff = XmlConvert.ToSingle(moduleNode.Attributes["LensFlareInstance.LensFlareHaloPowerFalloff"].Value);
							this._amplifyBloom.LensGlareInstance.ApplyLensGlare = XmlConvert.ToBoolean(moduleNode.Attributes["LensGlareInstance.ApplyLensGlare"].Value);
							this._amplifyBloom.LensGlareInstance.Intensity = XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.Intensity"].Value);
							this._amplifyBloom.LensGlareInstance.OverallStreakScale = XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallStreakScale"].Value);
							this._amplifyBloom.LensGlareInstance.OverallTint = new Color(XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallTintR"].Value),
																						 XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallTintG"].Value),
																						 XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallTintB"].Value),
																						 XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.OverallTintA"].Value));

							colorKeys.Clear();
							alphaKeys.Clear();

							foreach (XmlNode key in FindChildNode(moduleNode, "LensGlareInstance.CromaticColorGradient"))
							{
								switch (key.Name)
								{
									case "colorKey":
										colorKeys.Add(new GradientColorKey(
																		   new Color(
																					 XmlConvert.ToSingle(key.Attributes["r"].Value),
																					 XmlConvert.ToSingle(key.Attributes["g"].Value),
																					 XmlConvert.ToSingle(key.Attributes["b"].Value)
																					),
																		   XmlConvert.ToSingle(key.Attributes["time"].Value))
																		   );
										break;
									case "alphaKey":
										alphaKeys.Add(new GradientAlphaKey(
																		   XmlConvert.ToSingle(key.Attributes["alpha"].Value),
																		   XmlConvert.ToSingle(key.Attributes["time"].Value))
																		   );
										break;
								}
							}
							this._amplifyBloom.LensGlareInstance.CromaticColorGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());

							this._amplifyBloom.LensGlareInstance.CurrentGlare = (GlareLibType)XmlConvert.ToInt32(moduleNode.Attributes["LensGlareInstance.CurrentGlare"].Value);
							this._amplifyBloom.LensGlareInstance.PerPassDisplacement = XmlConvert.ToSingle(moduleNode.Attributes["LensGlareInstance.PerPassDisplacement"].Value);
							this._amplifyBloom.LensGlareInstance.GlareMaxPassCount = XmlConvert.ToInt32(moduleNode.Attributes["LensGlareInstance.GlareMaxPassCount"].Value);
							HSLRE.HSLRE.self.fixAmplifyBloomForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
						}
						break;
					case "effectsOrder":
						foreach (XmlNode childNode in moduleNode.ChildNodes)
						{
							HSLRE.HSLRE.EffectData data = HSLRE.HSLRE.self.generalEffects.Find(e => e.simpleName.Equals(childNode.Name, StringComparison.OrdinalIgnoreCase));
							if (data == null)
								continue;
							HSLRE.HSLRE.self.MoveAtIndex(data.effect, XmlConvert.ToInt32(childNode.Attributes["index"].Value));
						}
						break;
					case "blur":
						if (this._blur != null)
						{
							HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._blur];
							effectData.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
							this._blur.downsample = XmlConvert.ToInt32(moduleNode.Attributes["downsample"].Value);
							this._blur.blurSize = XmlConvert.ToSingle(moduleNode.Attributes["blurSize"].Value);
							this._blur.blurIterations = XmlConvert.ToInt32(moduleNode.Attributes["blurIterations"].Value);
							this._blur.blurType = (BlurOptimized.BlurType)XmlConvert.ToInt32(moduleNode.Attributes["blurType"].Value);
							HSLRE.HSLRE.self.fixBlurForUpscaledScreenshots = moduleNode.Attributes["fixUpscaledScreenshots"] != null && XmlConvert.ToBoolean(moduleNode.Attributes["fixUpscaledScreenshots"].Value);
						}
						break;
				}
			}
			if (this._selectedCubemap == 0)
			{
				this._proceduralSkybox.ApplySkyboxParams();
				this._tempProceduralSkyboxParams = this._proceduralSkybox.skyboxparams;
				this._previousAmbientIntensity = RenderSettings.ambientIntensity;
			}
			else if (this._selectedCubemap > 0)
			{
				this._skybox.ApplySkyboxParams();
				this._tempSkyboxParams = this._skybox.skyboxparams;
				this._previousAmbientIntensity = RenderSettings.ambientIntensity;
			}

			this._environmentUpdateFlag = true;

			if (this._segi != null)
			{
				Light selected = null;
				if (_isStudio)
				{
					foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
					{
						OCILight light = pair.Value as OCILight;
						if (light != null && light.light.type == LightType.Directional && (selected == null || selected.intensity < light.light.intensity))
							selected = light.light;
					}
				}
				else
				{
					foreach (Light light in GameObject.FindObjectsOfType<Light>())
					{
						if (light.type == LightType.Directional && (selected == null || selected.intensity < light.intensity))
							selected = light;
					}
				}
				this._segi.sun = selected;
			}
		}

		public static XmlNode FindChildNode(XmlNode self, string name)
		{
			if (self.HasChildNodes == false)
				return null;
			foreach (XmlNode chilNode in self.ChildNodes)
				if (chilNode.Name.Equals(name))
					return chilNode;
			return null;
		}

		private void SaveConfig(XmlWriter writer)
		{
			SortedDictionary<int, ObjectCtrlInfo> dic = null;
			if (_isStudio)
				dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);

			writer.WriteAttributeString("version", Assembly.GetExecutingAssembly().GetName().Version.ToString());

			{
				writer.WriteStartElement("cubemap");
				writer.WriteAttributeString("hide", XmlConvert.ToString(this._hideSkybox));
				writer.WriteAttributeString("index", XmlConvert.ToString(this._selectedCubemap));
				string fileName = "";
				if (this._selectedCubemap > 0)
					fileName = this._cubemapFolder.lstFile[this._selectedCubemap - 1].FileName;
				writer.WriteAttributeString("fileName", fileName);
				writer.WriteEndElement();
			}

			{
				writer.WriteStartElement("skybox");
				writer.WriteAttributeString("proceduralExposure", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.exposure));
				writer.WriteAttributeString("proceduralSunsize", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.sunsize));
				writer.WriteAttributeString("proceduralAtmospherethickness", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.atmospherethickness));
				writer.WriteAttributeString("proceduralSkytintR", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.skytint.r));
				writer.WriteAttributeString("proceduralSkytintG", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.skytint.g));
				writer.WriteAttributeString("proceduralSkytintB", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.skytint.b));
				writer.WriteAttributeString("proceduralGroundcolorR", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.groundcolor.r));
				writer.WriteAttributeString("proceduralGroundcolorG", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.groundcolor.g));
				writer.WriteAttributeString("proceduralGroundcolorB", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.groundcolor.b));

				writer.WriteAttributeString("classicRotation", XmlConvert.ToString(this._skybox.skyboxparams.rotation));
				writer.WriteAttributeString("classicExposure", XmlConvert.ToString(this._skybox.skyboxparams.exposure));
				writer.WriteAttributeString("classicTintR", XmlConvert.ToString(this._skybox.skyboxparams.tint.r));
				writer.WriteAttributeString("classicTintG", XmlConvert.ToString(this._skybox.skyboxparams.tint.g));
				writer.WriteAttributeString("classicTintB", XmlConvert.ToString(this._skybox.skyboxparams.tint.b));

				writer.WriteAttributeString("ambientIntensity", XmlConvert.ToString(RenderSettings.ambientIntensity));
				writer.WriteEndElement();
			}

			{
				writer.WriteStartElement("reflection");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(this._probeComponent.enabled));
				writer.WriteAttributeString("refreshOnDemand", XmlConvert.ToString(this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting));
				writer.WriteAttributeString("timeSlicing", XmlConvert.ToString((int)this._probeComponent.timeSlicingMode));
				writer.WriteAttributeString("resolution", XmlConvert.ToString(this._reflectionProbeResolution));
				writer.WriteAttributeString("intensity", XmlConvert.ToString(this._probeComponent.intensity));
				writer.WriteAttributeString("shadowDistance", XmlConvert.ToString(this._probeComponent.shadowDistance));
				writer.WriteAttributeString("nearClipPlane", XmlConvert.ToString(this._probeComponent.nearClipPlane));
				writer.WriteAttributeString("positionX", XmlConvert.ToString(this.probeGameObject.transform.position.x));
				writer.WriteAttributeString("positionY", XmlConvert.ToString(this.probeGameObject.transform.position.y));
				writer.WriteAttributeString("positionZ", XmlConvert.ToString(this.probeGameObject.transform.position.z));
				writer.WriteAttributeString("bounce", XmlConvert.ToString(RenderSettings.reflectionBounces));
				writer.WriteAttributeString("boxProjection", XmlConvert.ToString(this._probeComponent.boxProjection));
				writer.WriteAttributeString("cullingMask", XmlConvert.ToString(this._probeComponent.cullingMask));
				writer.WriteEndElement();
			}

			{
				writer.WriteStartElement("defaultLight");

				writer.WriteAttributeString("frontAnchoredToCamera", XmlConvert.ToString(this._frontDirectionalLight.transform.parent != null));
				writer.WriteAttributeString("frontRotateX", XmlConvert.ToString(this._frontRotate.x));
				writer.WriteAttributeString("frontRotateY", XmlConvert.ToString(this._frontRotate.y));
				writer.WriteAttributeString("frontIntensity", XmlConvert.ToString(this._frontDirectionalLight.intensity));
				writer.WriteAttributeString("frontColorR", XmlConvert.ToString(this._frontDirectionalLight.color.r));
				writer.WriteAttributeString("frontColorG", XmlConvert.ToString(this._frontDirectionalLight.color.g));
				writer.WriteAttributeString("frontColorB", XmlConvert.ToString(this._frontDirectionalLight.color.b));

				writer.WriteAttributeString("backAnchoredToCamera", XmlConvert.ToString(this._backDirectionalLight.transform.parent != null));
				writer.WriteAttributeString("backRotateX", XmlConvert.ToString(this._backRotate.x));
				writer.WriteAttributeString("backRotateY", XmlConvert.ToString(this._backRotate.y));
				writer.WriteAttributeString("backIntensity", XmlConvert.ToString(this._backDirectionalLight.intensity));
				writer.WriteAttributeString("backColorR", XmlConvert.ToString(this._backDirectionalLight.color.r));
				writer.WriteAttributeString("backColorG", XmlConvert.ToString(this._backDirectionalLight.color.g));
				writer.WriteAttributeString("backColorB", XmlConvert.ToString(this._backDirectionalLight.color.b));

				writer.WriteEndElement();
			}

			if (this._smaa != null)
			{
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._smaa];

				SMAA.GlobalSettings settings = this._smaa.settings;
				SMAA.PredicationSettings predication = this._smaa.predication;
				SMAA.TemporalSettings temporal = this._smaa.temporal;
				SMAA.QualitySettings quality = this._smaa.quality;

				writer.WriteStartElement("smaa");

				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("debugPass", XmlConvert.ToString((int)settings.debugPass));
				writer.WriteAttributeString("quality", XmlConvert.ToString((int)settings.quality));
				writer.WriteAttributeString("edgeDetectionMethod", XmlConvert.ToString((int)settings.edgeDetectionMethod));
				writer.WriteAttributeString("qualityDiagonalDetection", XmlConvert.ToString(quality.diagonalDetection));
				writer.WriteAttributeString("qualityCornerDetection", XmlConvert.ToString(quality.cornerDetection));
				writer.WriteAttributeString("qualityThreshold", XmlConvert.ToString(quality.threshold));
				writer.WriteAttributeString("qualityDepthThreshold", XmlConvert.ToString(quality.depthThreshold));
				writer.WriteAttributeString("qualityMaxSearchSteps", XmlConvert.ToString(quality.maxSearchSteps));
				writer.WriteAttributeString("qualityMaxDiagonalSearchSteps", XmlConvert.ToString(quality.maxDiagonalSearchSteps));
				writer.WriteAttributeString("qualityCornerRounding", XmlConvert.ToString(quality.cornerRounding));
				writer.WriteAttributeString("qualityLocalContrastAdaptationFactor", XmlConvert.ToString(quality.localContrastAdaptationFactor));
				writer.WriteAttributeString("predicationEnabled", XmlConvert.ToString(predication.enabled));
				writer.WriteAttributeString("predicationThreshold", XmlConvert.ToString(predication.threshold));
				writer.WriteAttributeString("predicationScale", XmlConvert.ToString(predication.scale));
				writer.WriteAttributeString("predicationStrength", XmlConvert.ToString(predication.strength));
				writer.WriteAttributeString("temporalEnabled", XmlConvert.ToString(temporal.enabled));
				writer.WriteAttributeString("temporalFuzzSize", XmlConvert.ToString(temporal.fuzzSize));

				writer.WriteEndElement();
			}

			if (this._lensManager != null)
			{
				writer.WriteStartElement("lens");

				writer.WriteAttributeString("fov", XmlConvert.ToString(Camera.main.fieldOfView));
				writer.WriteAttributeString("distortionEnabled", XmlConvert.ToString(this._lensManager.distortion.enabled));
				writer.WriteAttributeString("distortionAmount", XmlConvert.ToString(this._lensManager.distortion.amount));
				writer.WriteAttributeString("distortionAmountX", XmlConvert.ToString(this._lensManager.distortion.amountX));
				writer.WriteAttributeString("distortionAmountY", XmlConvert.ToString(this._lensManager.distortion.amountY));
				writer.WriteAttributeString("distortionScale", XmlConvert.ToString(this._lensManager.distortion.scale));

				writer.WriteAttributeString("chromaticAberrationEnabled", XmlConvert.ToString(this._lensManager.chromaticAberration.enabled));
				writer.WriteAttributeString("chromaticAberrationAmount", XmlConvert.ToString(this._lensManager.chromaticAberration.amount));
				writer.WriteAttributeString("chromaticAberrationColorR", XmlConvert.ToString(this._lensManager.chromaticAberration.color.r));
				writer.WriteAttributeString("chromaticAberrationColorG", XmlConvert.ToString(this._lensManager.chromaticAberration.color.g));
				writer.WriteAttributeString("chromaticAberrationColorB", XmlConvert.ToString(this._lensManager.chromaticAberration.color.b));

				writer.WriteAttributeString("vignetteEnabled", XmlConvert.ToString(this._lensManager.vignette.enabled));
				writer.WriteAttributeString("vignetteIntensity", XmlConvert.ToString(this._lensManager.vignette.intensity));
				writer.WriteAttributeString("vignetteSmoothness", XmlConvert.ToString(this._lensManager.vignette.smoothness));
				writer.WriteAttributeString("vignetteRoundness", XmlConvert.ToString(this._lensManager.vignette.roundness));
				writer.WriteAttributeString("vignetteDesaturate", XmlConvert.ToString(this._lensManager.vignette.desaturate));
				writer.WriteAttributeString("vignetteBlur", XmlConvert.ToString(this._lensManager.vignette.blur));
				writer.WriteAttributeString("vignetteColorR", XmlConvert.ToString(this._lensManager.vignette.color.r));
				writer.WriteAttributeString("vignetteColorG", XmlConvert.ToString(this._lensManager.vignette.color.g));
				writer.WriteAttributeString("vignetteColorB", XmlConvert.ToString(this._lensManager.vignette.color.b));

				writer.WriteEndElement();
			}

			if (this._toneMappingManager != null)
			{
				writer.WriteStartElement("tonemapping");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(this._toneMappingManager.tonemapping.enabled));
				writer.WriteAttributeString("tonemapper", XmlConvert.ToString((int)this._toneMappingManager.tonemapping.tonemapper));
				writer.WriteAttributeString("exposure", XmlConvert.ToString(this._ev));
				writer.WriteEndElement();
			}

			if (this._toneMappingManager != null)
			{
				HSLRE.CustomEffects.TonemappingColorGrading.ColorGradingSettings colorGrading = this._toneMappingManager.colorGrading;
				HSLRE.CustomEffects.TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;

				writer.WriteStartElement("colorGrading");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(colorGrading.enabled));
				writer.WriteAttributeString("temperatureShift", XmlConvert.ToString(settings.temperatureShift));
				writer.WriteAttributeString("tint", XmlConvert.ToString(settings.tint));
				writer.WriteAttributeString("contrast", XmlConvert.ToString(settings.contrast));
				writer.WriteAttributeString("hue", XmlConvert.ToString(settings.hue));
				writer.WriteAttributeString("saturation", XmlConvert.ToString(settings.saturation));
				writer.WriteAttributeString("value", XmlConvert.ToString(settings.value));
				writer.WriteAttributeString("vibrance", XmlConvert.ToString(settings.vibrance));
				writer.WriteAttributeString("gain", XmlConvert.ToString(settings.gain));
				writer.WriteAttributeString("gamma", XmlConvert.ToString(settings.gamma));
				writer.WriteEndElement();
			}

			if (this._toneMappingManager != null)
			{
				writer.WriteStartElement("eyeAdaptation");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.enabled));
				writer.WriteAttributeString("middleGrey", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.middleGrey));
				writer.WriteAttributeString("min", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.min));
				writer.WriteAttributeString("max", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.max));
				writer.WriteAttributeString("speed", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.speed));
				writer.WriteEndElement();
			}

			if (this._cinematicBloom != null)
			{
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._cinematicBloom];

				writer.WriteStartElement("bloom");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("intensity", XmlConvert.ToString(this._cinematicBloom.settings.intensity));
				writer.WriteAttributeString("threshold", XmlConvert.ToString(this._cinematicBloom.settings.threshold));
				writer.WriteAttributeString("softKnee", XmlConvert.ToString(this._cinematicBloom.settings.softKnee));
				writer.WriteAttributeString("radius", XmlConvert.ToString(this._cinematicBloom.settings.radius));
				writer.WriteAttributeString("antiFlicker", XmlConvert.ToString(this._cinematicBloom.settings.antiFlicker));
				writer.WriteEndElement();
			}

			if (this._ssao != null)
			{
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._ssao];

				writer.WriteStartElement("ssao");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("samples", XmlConvert.ToString((int)this._ssao.Samples));
				writer.WriteAttributeString("downsampling", XmlConvert.ToString(this._ssao.Downsampling));
				writer.WriteAttributeString("radius", XmlConvert.ToString(this._ssao.Radius));
				writer.WriteAttributeString("intensity", XmlConvert.ToString(this._ssao.Intensity));
				writer.WriteAttributeString("distance", XmlConvert.ToString(this._ssao.Distance));
				writer.WriteAttributeString("bias", XmlConvert.ToString(this._ssao.Bias));
				writer.WriteAttributeString("lumContribution", XmlConvert.ToString(this._ssao.LumContribution));
				writer.WriteAttributeString("occlusionColorR", XmlConvert.ToString(this._ssao.OcclusionColor.r));
				writer.WriteAttributeString("occlusionColorG", XmlConvert.ToString(this._ssao.OcclusionColor.g));
				writer.WriteAttributeString("occlusionColorB", XmlConvert.ToString(this._ssao.OcclusionColor.b));
				writer.WriteAttributeString("cutoffDistance", XmlConvert.ToString(this._ssao.CutoffDistance));
				writer.WriteAttributeString("cutoffFalloff", XmlConvert.ToString(this._ssao.CutoffFalloff));
				writer.WriteAttributeString("blurPasses", XmlConvert.ToString(this._ssao.BlurPasses));
				writer.WriteAttributeString("blurBilateralThreshold", XmlConvert.ToString(this._ssao.BlurBilateralThreshold));
				writer.WriteAttributeString("useHighPrecisionDepthMap", XmlConvert.ToString(this._ssao.UseHighPrecisionDepthMap));
				writer.WriteAttributeString("blur", XmlConvert.ToString((int)this._ssao.Blur));
				writer.WriteAttributeString("blurDownsampling", XmlConvert.ToString(this._ssao.BlurDownsampling));
				writer.WriteAttributeString("debugAO", XmlConvert.ToString(this._ssao.DebugAO));
				writer.WriteEndElement();
			}

			if (this._sunShafts != null)
			{
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._sunShafts];

				writer.WriteStartElement("sunShafts");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("useDepthTexture", XmlConvert.ToString(this._sunShafts.useDepthTexture));
				writer.WriteAttributeString("resolution", XmlConvert.ToString((int)this._sunShafts.resolution));
				writer.WriteAttributeString("screenBlendMode", XmlConvert.ToString((int)this._sunShafts.screenBlendMode));
				writer.WriteAttributeString("sunThresholdR", XmlConvert.ToString(this._sunShafts.sunThreshold.r));
				writer.WriteAttributeString("sunThresholdG", XmlConvert.ToString(this._sunShafts.sunThreshold.g));
				writer.WriteAttributeString("sunThresholdB", XmlConvert.ToString(this._sunShafts.sunThreshold.b));
				writer.WriteAttributeString("sunColorR", XmlConvert.ToString(this._sunShafts.sunColor.r));
				writer.WriteAttributeString("sunColorG", XmlConvert.ToString(this._sunShafts.sunColor.g));
				writer.WriteAttributeString("sunColorB", XmlConvert.ToString(this._sunShafts.sunColor.b));
				writer.WriteAttributeString("maxRadius", XmlConvert.ToString(this._sunShafts.maxRadius));
				writer.WriteAttributeString("sunShaftBlurRadius", XmlConvert.ToString(this._sunShafts.sunShaftBlurRadius));
				writer.WriteAttributeString("radialBlurIterations", XmlConvert.ToString(this._sunShafts.radialBlurIterations));
				writer.WriteAttributeString("sunShaftIntensity", XmlConvert.ToString(this._sunShafts.sunShaftIntensity));
				writer.WriteEndElement();
			}

			if (this._depthOfField != null)
			{
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._depthOfField];

				writer.WriteStartElement("depthOfField");
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("visualizeFocus", XmlConvert.ToString(this._depthOfField.visualizeFocus));
				writer.WriteAttributeString("focusType", XmlConvert.ToString((int)this._focusType));
				if (this._focusType == DOFFocusType.Object && this._focussedObject != null && this._depthOfField.focalTransform != null)
				{
					List<KeyValuePair<int, ObjectCtrlInfo>> list = Studio.Studio.Instance.dicObjectCtrl.OrderBy(e => e.Key).ToList();
					int index = list.FindIndex(e => e.Value == this._focussedObject);
					if (index != -1)
						writer.WriteAttributeString("focussedObjectIndex", XmlConvert.ToString(index));
				}
				writer.WriteAttributeString("focalLength", XmlConvert.ToString(this._depthOfField.focalLength));
				writer.WriteAttributeString("focalSize", XmlConvert.ToString(this._depthOfField.focalSize));
				writer.WriteAttributeString("aperture", XmlConvert.ToString(this._depthOfField.aperture));
				writer.WriteAttributeString("maxBlurSize", XmlConvert.ToString(this._depthOfField.maxBlurSize));
				writer.WriteAttributeString("highResolution", XmlConvert.ToString(this._depthOfField.highResolution));
				writer.WriteAttributeString("nearBlur", XmlConvert.ToString(this._depthOfField.nearBlur));
				writer.WriteAttributeString("foregroundOverlap", XmlConvert.ToString(this._depthOfField.foregroundOverlap));
				writer.WriteAttributeString("blurType", XmlConvert.ToString((int)this._depthOfField.blurType));
				writer.WriteAttributeString("blurSampleCount", XmlConvert.ToString((int)this._depthOfField.blurSampleCount));
				writer.WriteAttributeString("dx11BokehScale", XmlConvert.ToString(this._depthOfField.dx11BokehScale));
				writer.WriteAttributeString("dx11BokehIntensity", XmlConvert.ToString(this._depthOfField.dx11BokehIntensity));
				writer.WriteAttributeString("dx11BokehThreshold", XmlConvert.ToString(this._depthOfField.dx11BokehThreshold));
				writer.WriteAttributeString("dx11SpawnHeuristic", XmlConvert.ToString(this._depthOfField.dx11SpawnHeuristic));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(HSLRE.HSLRE.self.fixDofForUpscaledScreenshots));
				writer.WriteEndElement();
			}

			if (this._ssr != null)
			{
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._ssr];

				writer.WriteStartElement("ssr");

				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(HSLRE.HSLRE.self.fixSsrForUpscaledScreenshots));

				ScreenSpaceReflection.SSRSettings ssrSettings = this._ssr.settings;

				ScreenSpaceReflection.BasicSettings basicSettings = ssrSettings.basicSettings;
				{
					writer.WriteAttributeString("reflectionMultiplier", XmlConvert.ToString(basicSettings.reflectionMultiplier));
					writer.WriteAttributeString("maxDistance", XmlConvert.ToString(basicSettings.maxDistance));
					writer.WriteAttributeString("fadeDistance", XmlConvert.ToString(basicSettings.fadeDistance));
					writer.WriteAttributeString("screenEdgeFading", XmlConvert.ToString(basicSettings.screenEdgeFading));
					writer.WriteAttributeString("enableHDR", XmlConvert.ToString(basicSettings.enableHDR));
					writer.WriteAttributeString("additiveReflection", XmlConvert.ToString(basicSettings.additiveReflection));
				}

				ScreenSpaceReflection.ReflectionSettings reflectionSettings = ssrSettings.reflectionSettings;
				{
					writer.WriteAttributeString("maxSteps", XmlConvert.ToString(reflectionSettings.maxSteps));
					writer.WriteAttributeString("rayStepSize", XmlConvert.ToString(reflectionSettings.rayStepSize));
					writer.WriteAttributeString("widthModifier", XmlConvert.ToString(reflectionSettings.widthModifier));
					writer.WriteAttributeString("smoothFallbackThreshold", XmlConvert.ToString(reflectionSettings.smoothFallbackThreshold));
					writer.WriteAttributeString("smoothFallbackDistance", XmlConvert.ToString(reflectionSettings.smoothFallbackDistance));
					writer.WriteAttributeString("fresnelFade", XmlConvert.ToString(reflectionSettings.fresnelFade));
					writer.WriteAttributeString("fresnelFadePower", XmlConvert.ToString(reflectionSettings.fresnelFadePower));
					writer.WriteAttributeString("distanceBlur", XmlConvert.ToString(reflectionSettings.distanceBlur));
				}

				ScreenSpaceReflection.AdvancedSettings advancedSettings = ssrSettings.advancedSettings;
				{
					writer.WriteAttributeString("temporalFilterStrength", XmlConvert.ToString(advancedSettings.temporalFilterStrength));
					writer.WriteAttributeString("useTemporalConfidence", XmlConvert.ToString(advancedSettings.useTemporalConfidence));
					writer.WriteAttributeString("traceBehindObjects", XmlConvert.ToString(advancedSettings.traceBehindObjects));
					writer.WriteAttributeString("highQualitySharpReflections", XmlConvert.ToString(advancedSettings.highQualitySharpReflections));
					writer.WriteAttributeString("traceEverywhere", XmlConvert.ToString(advancedSettings.traceEverywhere));
					writer.WriteAttributeString("treatBackfaceHitAsMiss", XmlConvert.ToString(advancedSettings.treatBackfaceHitAsMiss));
					writer.WriteAttributeString("allowBackwardsRays", XmlConvert.ToString(advancedSettings.allowBackwardsRays));
					writer.WriteAttributeString("improveCorners", XmlConvert.ToString(advancedSettings.improveCorners));
					writer.WriteAttributeString("resolution", XmlConvert.ToString((int)advancedSettings.resolution));
					writer.WriteAttributeString("bilateralUpsample", XmlConvert.ToString(advancedSettings.bilateralUpsample));
					writer.WriteAttributeString("reduceBanding", XmlConvert.ToString(advancedSettings.reduceBanding));
					writer.WriteAttributeString("highlightSuppression", XmlConvert.ToString(advancedSettings.highlightSuppression));
				}
				writer.WriteEndElement();
			}

			if (this._bloomAndFlares != null)
			{
				writer.WriteStartElement("bloomAndFlares");

				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._bloomAndFlares];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));

				writer.WriteAttributeString("screenBlendMode", XmlConvert.ToString((int)this._bloomAndFlares.screenBlendMode));
				writer.WriteAttributeString("hdr", XmlConvert.ToString((int)this._bloomAndFlares.hdr));

				writer.WriteAttributeString("bloomIntensity", XmlConvert.ToString(this._bloomAndFlares.bloomIntensity));
				writer.WriteAttributeString("bloomThreshold", XmlConvert.ToString(this._bloomAndFlares.bloomThreshold));
				writer.WriteAttributeString("bloomBlurIterations", XmlConvert.ToString(this._bloomAndFlares.bloomBlurIterations));
				writer.WriteAttributeString("sepBlurSpread", XmlConvert.ToString(this._bloomAndFlares.sepBlurSpread));
				writer.WriteAttributeString("useSrcAlphaAsMask", XmlConvert.ToString(this._bloomAndFlares.useSrcAlphaAsMask));

				writer.WriteAttributeString("lensflares", XmlConvert.ToString(this._bloomAndFlares.lensflares));
				writer.WriteAttributeString("lensflareMode", XmlConvert.ToString((int)this._bloomAndFlares.lensflareMode));
				writer.WriteAttributeString("lensflareIntensity", XmlConvert.ToString(this._bloomAndFlares.lensflareIntensity));
				writer.WriteAttributeString("lensflareThreshold", XmlConvert.ToString(this._bloomAndFlares.lensflareThreshold));

				writer.WriteAttributeString("hollyStretchWidth", XmlConvert.ToString(this._bloomAndFlares.hollyStretchWidth));
				writer.WriteAttributeString("hollywoodFlareBlurIterations", XmlConvert.ToString(this._bloomAndFlares.hollywoodFlareBlurIterations));

				writer.WriteAttributeString("flareColorAR", XmlConvert.ToString(this._bloomAndFlares.flareColorA.r));
				writer.WriteAttributeString("flareColorAG", XmlConvert.ToString(this._bloomAndFlares.flareColorA.g));
				writer.WriteAttributeString("flareColorAB", XmlConvert.ToString(this._bloomAndFlares.flareColorA.b));
				writer.WriteAttributeString("flareColorAA", XmlConvert.ToString(this._bloomAndFlares.flareColorA.a));

				writer.WriteAttributeString("flareColorBR", XmlConvert.ToString(this._bloomAndFlares.flareColorB.r));
				writer.WriteAttributeString("flareColorBG", XmlConvert.ToString(this._bloomAndFlares.flareColorB.g));
				writer.WriteAttributeString("flareColorBB", XmlConvert.ToString(this._bloomAndFlares.flareColorB.b));
				writer.WriteAttributeString("flareColorBA", XmlConvert.ToString(this._bloomAndFlares.flareColorB.a));

				writer.WriteAttributeString("flareColorCR", XmlConvert.ToString(this._bloomAndFlares.flareColorC.r));
				writer.WriteAttributeString("flareColorCG", XmlConvert.ToString(this._bloomAndFlares.flareColorC.g));
				writer.WriteAttributeString("flareColorCB", XmlConvert.ToString(this._bloomAndFlares.flareColorC.b));
				writer.WriteAttributeString("flareColorCA", XmlConvert.ToString(this._bloomAndFlares.flareColorC.a));

				writer.WriteAttributeString("flareColorDR", XmlConvert.ToString(this._bloomAndFlares.flareColorD.r));
				writer.WriteAttributeString("flareColorDG", XmlConvert.ToString(this._bloomAndFlares.flareColorD.g));
				writer.WriteAttributeString("flareColorDB", XmlConvert.ToString(this._bloomAndFlares.flareColorD.b));
				writer.WriteAttributeString("flareColorDA", XmlConvert.ToString(this._bloomAndFlares.flareColorD.a));

				writer.WriteEndElement();
			}

			if (this._colorCorrectionCurves != null)
			{
				writer.WriteStartElement("colorCorrectionCurves");

				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._colorCorrectionCurves];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("saturation", XmlConvert.ToString(this._colorCorrectionCurves.saturation));

				writer.WriteEndElement();
			}

			if (this._vignette != null)
			{
				writer.WriteStartElement("vignetteAndChromaticAberration");

				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._vignette];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("intensity", XmlConvert.ToString(this._vignette.intensity));
				writer.WriteAttributeString("blur", XmlConvert.ToString(this._vignette.blur));
				writer.WriteAttributeString("blurSpread", XmlConvert.ToString(this._vignette.blurSpread));
				writer.WriteAttributeString("mode", XmlConvert.ToString((int)this._vignette.mode));
				writer.WriteAttributeString("chromaticAberration", XmlConvert.ToString(this._vignette.chromaticAberration));
				writer.WriteAttributeString("axialAberration", XmlConvert.ToString(this._vignette.axialAberration));
				writer.WriteAttributeString("luminanceDependency", XmlConvert.ToString(this._vignette.luminanceDependency));
				writer.WriteAttributeString("blurDistance", XmlConvert.ToString(this._vignette.blurDistance));

				writer.WriteEndElement();
			}

			if (this._antialiasing != null)
			{
				writer.WriteStartElement("antialiasing");

				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._antialiasing];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("mode", XmlConvert.ToString((int)this._antialiasing.mode));
				writer.WriteAttributeString("edgeThresholdMin", XmlConvert.ToString(this._antialiasing.edgeThresholdMin));
				writer.WriteAttributeString("edgeThreshold", XmlConvert.ToString(this._antialiasing.edgeThreshold));
				writer.WriteAttributeString("edgeSharpness", XmlConvert.ToString(this._antialiasing.edgeSharpness));
				writer.WriteAttributeString("offsetScale", XmlConvert.ToString(this._antialiasing.offsetScale));
				writer.WriteAttributeString("blurRadius", XmlConvert.ToString(this._antialiasing.blurRadius));
				writer.WriteAttributeString("showGeneratedNormals", XmlConvert.ToString(this._antialiasing.showGeneratedNormals));
				writer.WriteAttributeString("dlaaSharp", XmlConvert.ToString(this._antialiasing.dlaaSharp));

				writer.WriteEndElement();
			}

			if (this._noiseAndGrain != null)
			{
				writer.WriteStartElement("noiseAndGrain");
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._noiseAndGrain];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("dx11Grain", XmlConvert.ToString(this._noiseAndGrain.dx11Grain));
				writer.WriteAttributeString("monochrome", XmlConvert.ToString(this._noiseAndGrain.monochrome));
				writer.WriteAttributeString("intensityMultiplier", XmlConvert.ToString(this._noiseAndGrain.intensityMultiplier));
				writer.WriteAttributeString("generalIntensity", XmlConvert.ToString(this._noiseAndGrain.generalIntensity));
				writer.WriteAttributeString("blackIntensity", XmlConvert.ToString(this._noiseAndGrain.blackIntensity));
				writer.WriteAttributeString("whiteIntensity", XmlConvert.ToString(this._noiseAndGrain.whiteIntensity));
				writer.WriteAttributeString("midGrey", XmlConvert.ToString(this._noiseAndGrain.midGrey));
				writer.WriteAttributeString("intensitiesX", XmlConvert.ToString(this._noiseAndGrain.intensities.x));
				writer.WriteAttributeString("intensitiesY", XmlConvert.ToString(this._noiseAndGrain.intensities.y));
				writer.WriteAttributeString("intensitiesZ", XmlConvert.ToString(this._noiseAndGrain.intensities.z));
				writer.WriteAttributeString("filterMode", XmlConvert.ToString((int)this._noiseAndGrain.filterMode));
				writer.WriteAttributeString("softness", XmlConvert.ToString(this._noiseAndGrain.softness));
				writer.WriteAttributeString("monochromeTiling", XmlConvert.ToString(this._noiseAndGrain.monochromeTiling));
				writer.WriteAttributeString("tilingX", XmlConvert.ToString(this._noiseAndGrain.tiling.x));
				writer.WriteAttributeString("tilingY", XmlConvert.ToString(this._noiseAndGrain.tiling.y));
				writer.WriteAttributeString("tilingZ", XmlConvert.ToString(this._noiseAndGrain.tiling.z));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(HSLRE.HSLRE.self.fixNoiseAndGrainForUpscaledScreenshots));
				writer.WriteEndElement();
			}

			if (this._motionBlur != null)
			{
				writer.WriteStartElement("motionBlur");
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._motionBlur];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("velocityScale", XmlConvert.ToString(this._motionBlur.velocityScale));
				writer.WriteAttributeString("maxVelocity", XmlConvert.ToString(this._motionBlur.maxVelocity));
				writer.WriteAttributeString("minVelocity", XmlConvert.ToString(this._motionBlur.minVelocity));
				writer.WriteAttributeString("filterType", XmlConvert.ToString((int)this._motionBlur.filterType));
				writer.WriteAttributeString("rotationScale", XmlConvert.ToString(this._motionBlur.rotationScale));
				writer.WriteAttributeString("movementScale", XmlConvert.ToString(this._motionBlur.movementScale));
				writer.WriteAttributeString("velocityDownsample", XmlConvert.ToString(this._motionBlur.velocityDownsample));
				writer.WriteAttributeString("jitter", XmlConvert.ToString(this._motionBlur.jitter));
				writer.WriteAttributeString("preview", XmlConvert.ToString(this._motionBlur.preview));
				writer.WriteAttributeString("previewScaleX", XmlConvert.ToString(this._motionBlur.previewScale.x));
				writer.WriteAttributeString("previewScaleY", XmlConvert.ToString(this._motionBlur.previewScale.y));
				writer.WriteAttributeString("previewScaleZ", XmlConvert.ToString(this._motionBlur.previewScale.z));
				writer.WriteEndElement();
			}

			if (this._afterImage != null)
			{
				writer.WriteStartElement("afterImage");
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._afterImage];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("framesToMix", XmlConvert.ToString(this._afterImage.framesToMix));
				writer.WriteAttributeString("falloffPower", XmlConvert.ToString(this._afterImage.falloffPower));
				writer.WriteEndElement();
			}

			if (this._volumetricLights != null)
			{
				writer.WriteStartElement("volumetricLights");
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._volumetricLights];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));

				writer.WriteAttributeString("resolution", XmlConvert.ToString((int)this._volumetricLights.Resolution));
				if (dic != null)
				{
					int i = 0;
					foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
					{
						if (kvp.Value is OCILight light)
						{
							VolumetricLight volumetricLight = light.light.GetComponent<VolumetricLight>();
							writer.WriteStartElement("light");
							writer.WriteAttributeString("index", XmlConvert.ToString(i));

							writer.WriteAttributeString("enabled", XmlConvert.ToString(volumetricLight.enabled));
							writer.WriteAttributeString("sampleCount", XmlConvert.ToString(volumetricLight.SampleCount));
							writer.WriteAttributeString("scatteringCoef", XmlConvert.ToString(volumetricLight.ScatteringCoef));
							writer.WriteAttributeString("extinctionCoef", XmlConvert.ToString(volumetricLight.ExtinctionCoef));
							writer.WriteAttributeString("mieG", XmlConvert.ToString(volumetricLight.MieG));
							writer.WriteAttributeString("skyboxExtinctionCoef", XmlConvert.ToString(volumetricLight.SkyboxExtinctionCoef));
							writer.WriteAttributeString("maxRayLength", XmlConvert.ToString(volumetricLight.MaxRayLength));
							writer.WriteAttributeString("heightFog", XmlConvert.ToString(volumetricLight.HeightFog));
							writer.WriteAttributeString("heightScale", XmlConvert.ToString(volumetricLight.HeightScale));
							writer.WriteAttributeString("groundLevel", XmlConvert.ToString(volumetricLight.GroundLevel));
							writer.WriteAttributeString("noise", XmlConvert.ToString(volumetricLight.Noise));
							writer.WriteAttributeString("noiseScale", XmlConvert.ToString(volumetricLight.NoiseScale));
							writer.WriteAttributeString("noiseIntensity", XmlConvert.ToString(volumetricLight.NoiseIntensity));
							writer.WriteAttributeString("noiseIntensityOffset", XmlConvert.ToString(volumetricLight.NoiseIntensityOffset));
							writer.WriteAttributeString("noiseVelocityX", XmlConvert.ToString(volumetricLight.NoiseVelocity.x));
							writer.WriteAttributeString("noiseVelocityY", XmlConvert.ToString(volumetricLight.NoiseVelocity.y));

							writer.WriteEndElement();
						}
						++i;
					}
				}
				writer.WriteEndElement();
			}

			if (this._segi != null)
			{
				writer.WriteStartElement("segi");
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._segi];
				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("voxelResolution", XmlConvert.ToString((int)this._segi.voxelResolution));
				writer.WriteAttributeString("voxelAA", XmlConvert.ToString(this._segi.voxelAA));
				writer.WriteAttributeString("innerOcclusionLayers", XmlConvert.ToString(this._segi.innerOcclusionLayers));
				writer.WriteAttributeString("gaussianMipFilter", XmlConvert.ToString(this._segi.gaussianMipFilter));
				writer.WriteAttributeString("voxelSpaceSize", XmlConvert.ToString(this._segi.voxelSpaceSize));
				writer.WriteAttributeString("shadowSpaceSize", XmlConvert.ToString(this._segi.shadowSpaceSize));
				writer.WriteAttributeString("updateGI", XmlConvert.ToString(this._segi.updateGI));
				writer.WriteAttributeString("infiniteBounces", XmlConvert.ToString(this._segi.infiniteBounces));
				writer.WriteAttributeString("softSunlight", XmlConvert.ToString(this._segi.softSunlight));
				writer.WriteAttributeString("hsStandardCustomShadersCompatibility", XmlConvert.ToString(this._segi.hsStandardCustomShadersCompatibility));

				writer.WriteAttributeString("skyColorR", XmlConvert.ToString(this._segi.skyColor.r));
				writer.WriteAttributeString("skyColorG", XmlConvert.ToString(this._segi.skyColor.g));
				writer.WriteAttributeString("skyColorB", XmlConvert.ToString(this._segi.skyColor.b));
				writer.WriteAttributeString("skyColorA", XmlConvert.ToString(this._segi.skyColor.a));

				writer.WriteAttributeString("skyIntensity", XmlConvert.ToString(this._segi.skyIntensity));
				writer.WriteAttributeString("sphericalSkylight", XmlConvert.ToString(this._segi.sphericalSkylight));

				writer.WriteAttributeString("temporalBlendWeight", XmlConvert.ToString(this._segi.temporalBlendWeight));
				writer.WriteAttributeString("useBilateralFiltering", XmlConvert.ToString(this._segi.useBilateralFiltering));
				writer.WriteAttributeString("halfResolution", XmlConvert.ToString(this._segi.halfResolution));
				writer.WriteAttributeString("stochasticSampling", XmlConvert.ToString(this._segi.stochasticSampling));

				writer.WriteAttributeString("cones", XmlConvert.ToString(this._segi.cones));
				writer.WriteAttributeString("coneTraceSteps", XmlConvert.ToString(this._segi.coneTraceSteps));
				writer.WriteAttributeString("coneLength", XmlConvert.ToString(this._segi.coneLength));
				writer.WriteAttributeString("coneWidth", XmlConvert.ToString(this._segi.coneWidth));
				writer.WriteAttributeString("coneTraceBias", XmlConvert.ToString(this._segi.coneTraceBias));
				writer.WriteAttributeString("occlusionStrength", XmlConvert.ToString(this._segi.occlusionStrength));
				writer.WriteAttributeString("nearOcclusionStrength", XmlConvert.ToString(this._segi.nearOcclusionStrength));
				writer.WriteAttributeString("farOcclusionStrength", XmlConvert.ToString(this._segi.farOcclusionStrength));
				writer.WriteAttributeString("farthestOcclusionStrength", XmlConvert.ToString(this._segi.farthestOcclusionStrength));
				writer.WriteAttributeString("occlusionPower", XmlConvert.ToString(this._segi.occlusionPower));
				writer.WriteAttributeString("nearLightGain", XmlConvert.ToString(this._segi.nearLightGain));
				writer.WriteAttributeString("giGain", XmlConvert.ToString(this._segi.giGain));

				writer.WriteAttributeString("secondaryBounceGain", XmlConvert.ToString(this._segi.secondaryBounceGain));
				writer.WriteAttributeString("secondaryCones", XmlConvert.ToString(this._segi.secondaryCones));
				writer.WriteAttributeString("secondaryOcclusionStrength", XmlConvert.ToString(this._segi.secondaryOcclusionStrength));

				writer.WriteAttributeString("doReflections", XmlConvert.ToString(this._segi.doReflections));
				writer.WriteAttributeString("reflectionSteps", XmlConvert.ToString(this._segi.reflectionSteps));
				writer.WriteAttributeString("reflectionOcclusionPower", XmlConvert.ToString(this._segi.reflectionOcclusionPower));
				writer.WriteAttributeString("skyReflectionIntensity", XmlConvert.ToString(this._segi.skyReflectionIntensity));

				writer.WriteAttributeString("visualizeSunDepthTexture", XmlConvert.ToString(this._segi.visualizeSunDepthTexture));
				writer.WriteAttributeString("visualizeGI", XmlConvert.ToString(this._segi.visualizeGI));
				writer.WriteAttributeString("visualizeVoxels", XmlConvert.ToString(this._segi.visualizeVoxels));
				writer.WriteEndElement();
			}

			if (this._amplifyBloom != null)
			{
				writer.WriteStartElement("amplifyBloom");
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._amplifyBloom];

				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("UpscaleQuality", XmlConvert.ToString((int)this._amplifyBloom.UpscaleQuality));
				writer.WriteAttributeString("MainThresholdSize", XmlConvert.ToString((int)this._amplifyBloom.MainThresholdSize));
				writer.WriteAttributeString("CurrentPrecisionMode", XmlConvert.ToString((int)this._amplifyBloom.CurrentPrecisionMode));
				writer.WriteAttributeString("BloomRange", XmlConvert.ToString(this._amplifyBloom.BloomRange));
				writer.WriteAttributeString("OverallIntensity", XmlConvert.ToString(this._amplifyBloom.OverallIntensity));
				writer.WriteAttributeString("OverallThreshold", XmlConvert.ToString(this._amplifyBloom.OverallThreshold));
				writer.WriteAttributeString("UpscaleBlurRadius", XmlConvert.ToString(this._amplifyBloom.UpscaleBlurRadius));
				writer.WriteAttributeString("DebugToScreen", XmlConvert.ToString((int)this._amplifyBloom.DebugToScreen));
				writer.WriteAttributeString("TemporalFilteringActive", XmlConvert.ToString(this._amplifyBloom.TemporalFilteringActive));
				writer.WriteAttributeString("TemporalFilteringValue", XmlConvert.ToString(this._amplifyBloom.TemporalFilteringValue));
				writer.WriteAttributeString("SeparateFeaturesThreshold", XmlConvert.ToString(this._amplifyBloom.SeparateFeaturesThreshold));
				writer.WriteAttributeString("FeaturesThreshold", XmlConvert.ToString(this._amplifyBloom.FeaturesThreshold));
				writer.WriteAttributeString("ApplyLensDirt", XmlConvert.ToString(this._amplifyBloom.ApplyLensDirt));
				writer.WriteAttributeString("LensDirtStrength", XmlConvert.ToString(this._amplifyBloom.LensDirtStrength));
				writer.WriteAttributeString("LensDirtTexture", XmlConvert.ToString((int)this._amplifyBloom.LensDirtTexture));
				writer.WriteAttributeString("ApplyLensStardurst", XmlConvert.ToString(this._amplifyBloom.ApplyLensStardurst));
				writer.WriteAttributeString("LensStarburstStrength", XmlConvert.ToString(this._amplifyBloom.LensStarburstStrength));
				writer.WriteAttributeString("LensStardurstTex", XmlConvert.ToString((int)this._amplifyBloom.LensStardurstTex));
				writer.WriteAttributeString("BokehFilterInstance.ApplyBokeh", XmlConvert.ToString(this._amplifyBloom.BokehFilterInstance.ApplyBokeh));
				writer.WriteAttributeString("BokehFilterInstance.ApplyOnBloomSource", XmlConvert.ToString(this._amplifyBloom.BokehFilterInstance.ApplyOnBloomSource));
				writer.WriteAttributeString("BokehFilterInstance.ApertureShape", XmlConvert.ToString((int)this._amplifyBloom.BokehFilterInstance.ApertureShape));
				writer.WriteAttributeString("BokehFilterInstance.OffsetRotation", XmlConvert.ToString(this._amplifyBloom.BokehFilterInstance.OffsetRotation));
				writer.WriteAttributeString("BokehFilterInstance.BokehSampleRadius", XmlConvert.ToString(this._amplifyBloom.BokehFilterInstance.BokehSampleRadius));
				writer.WriteAttributeString("BokehFilterInstance.Aperture", XmlConvert.ToString(this._amplifyBloom.BokehFilterInstance.Aperture));
				writer.WriteAttributeString("BokehFilterInstance.FocalLength", XmlConvert.ToString(this._amplifyBloom.BokehFilterInstance.FocalLength));
				writer.WriteAttributeString("BokehFilterInstance.FocalDistance", XmlConvert.ToString(this._amplifyBloom.BokehFilterInstance.FocalDistance));
				writer.WriteAttributeString("BokehFilterInstance.MaxCoCDiameter", XmlConvert.ToString(this._amplifyBloom.BokehFilterInstance.MaxCoCDiameter));
				writer.WriteAttributeString("LensFlareInstance.ApplyLensFlare", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.ApplyLensFlare));
				writer.WriteAttributeString("LensFlareInstance.OverallIntensity", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.OverallIntensity));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGaussianBlurAmount", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareGaussianBlurAmount));
				writer.WriteAttributeString("LensFlareInstance.LensFlareNormalizedGhostsIntensity", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareNormalizedGhostsIntensity));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostAmount", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareGhostAmount));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostsDispersal", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareGhostsDispersal));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostChrDistortion", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareGhostChrDistortion));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostsPowerFactor", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFactor));
				writer.WriteAttributeString("LensFlareInstance.LensFlareGhostsPowerFalloff", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareGhostsPowerFalloff));
				writer.WriteAttributeString("LensFlareInstance.LensFlareNormalizedHaloIntensity", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareNormalizedHaloIntensity));
				writer.WriteAttributeString("LensFlareInstance.LensFlareHaloWidth", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareHaloWidth));
				writer.WriteAttributeString("LensFlareInstance.LensFlareHaloChrDistortion", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareHaloChrDistortion));
				writer.WriteAttributeString("LensFlareInstance.LensFlareHaloPowerFactor", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareHaloPowerFactor));
				writer.WriteAttributeString("LensFlareInstance.LensFlareHaloPowerFalloff", XmlConvert.ToString(this._amplifyBloom.LensFlareInstance.LensFlareHaloPowerFalloff));
				writer.WriteAttributeString("LensGlareInstance.ApplyLensGlare", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.ApplyLensGlare));
				writer.WriteAttributeString("LensGlareInstance.Intensity", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.Intensity));
				writer.WriteAttributeString("LensGlareInstance.OverallStreakScale", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.OverallStreakScale));
				writer.WriteAttributeString("LensGlareInstance.OverallTintR", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.OverallTint.r));
				writer.WriteAttributeString("LensGlareInstance.OverallTintG", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.OverallTint.g));
				writer.WriteAttributeString("LensGlareInstance.OverallTintB", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.OverallTint.b));
				writer.WriteAttributeString("LensGlareInstance.OverallTintA", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.OverallTint.a));
				writer.WriteAttributeString("LensGlareInstance.CurrentGlare", XmlConvert.ToString((int)this._amplifyBloom.LensGlareInstance.CurrentGlare));
				writer.WriteAttributeString("LensGlareInstance.PerPassDisplacement", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.PerPassDisplacement));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(HSLRE.HSLRE.self.fixAmplifyBloomForUpscaledScreenshots));
				writer.WriteAttributeString("LensGlareInstance.GlareMaxPassCount", XmlConvert.ToString(this._amplifyBloom.LensGlareInstance.GlareMaxPassCount));

				writer.WriteStartElement("LensFlareInstance.LensFlareGradient");
				foreach (GradientColorKey key in this._amplifyBloom.LensFlareInstance.LensFlareGradient.colorKeys)
				{
					writer.WriteStartElement("colorKey");
					writer.WriteAttributeString("r", XmlConvert.ToString(key.color.r));
					writer.WriteAttributeString("g", XmlConvert.ToString(key.color.g));
					writer.WriteAttributeString("b", XmlConvert.ToString(key.color.b));
					writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
					writer.WriteEndElement();
				}
				foreach (GradientAlphaKey key in this._amplifyBloom.LensFlareInstance.LensFlareGradient.alphaKeys)
				{
					writer.WriteStartElement("alphaKey");
					writer.WriteAttributeString("alpha", XmlConvert.ToString(key.alpha));
					writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
					writer.WriteEndElement();
				}
				writer.WriteEndElement();

				writer.WriteStartElement("LensGlareInstance.CromaticColorGradient");
				foreach (GradientColorKey key in this._amplifyBloom.LensGlareInstance.CromaticColorGradient.colorKeys)
				{
					writer.WriteStartElement("colorKey");
					writer.WriteAttributeString("r", XmlConvert.ToString(key.color.r));
					writer.WriteAttributeString("g", XmlConvert.ToString(key.color.g));
					writer.WriteAttributeString("b", XmlConvert.ToString(key.color.b));
					writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
					writer.WriteEndElement();
				}
				foreach (GradientAlphaKey key in this._amplifyBloom.LensGlareInstance.CromaticColorGradient.alphaKeys)
				{
					writer.WriteStartElement("alphaKey");
					writer.WriteAttributeString("alpha", XmlConvert.ToString(key.alpha));
					writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
					writer.WriteEndElement();
				}
				writer.WriteEndElement();

				writer.WriteEndElement();
			}

			if (this._blur != null)
			{
				writer.WriteStartElement("blur");
				HSLRE.HSLRE.EffectData effectData = HSLRE.HSLRE.self.effectsDictionary[this._blur];

				writer.WriteAttributeString("enabled", XmlConvert.ToString(effectData.enabled));
				writer.WriteAttributeString("downsample", XmlConvert.ToString(this._blur.downsample));
				writer.WriteAttributeString("blurSize", XmlConvert.ToString(this._blur.blurSize));
				writer.WriteAttributeString("blurIterations", XmlConvert.ToString(this._blur.blurIterations));
				writer.WriteAttributeString("blurType", XmlConvert.ToString((int)this._blur.blurType));
				writer.WriteAttributeString("fixUpscaledScreenshots", XmlConvert.ToString(HSLRE.HSLRE.self.fixBlurForUpscaledScreenshots));
				writer.WriteEndElement();
			}

			writer.WriteStartElement("effectsOrder");
			for (int i = 0; i < HSLRE.HSLRE.self.generalEffects.Count; i++)
			{
				HSLRE.HSLRE.EffectData data = HSLRE.HSLRE.self.generalEffects[i];
				writer.WriteStartElement(data.simpleName);
				writer.WriteAttributeString("index", XmlConvert.ToString(i));
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		#endregion
	}
}

