using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
	using UnityEditor;
#endif



namespace Suimono.Core
{

	[ExecuteInEditMode]
	public class SuimonoObject : MonoBehaviour
	{


		public float systemTime = 0.0f;
		public float systemLocalTime = 0.0f;
		public float flowSpeed = 0.1f;
		public float flowDirection = 180.0f;
		public bool useBeaufortScale = false;
		public float beaufortScale = 1.0f;
		public float turbulenceFactor = 1.0f;
		public float waveScale = 0.5f;
		public float lgWaveHeight = 0.0f;
		public float lgWaveScale = 1.0f;
		public float waveHeight = 1.0f;
		public float heightProjection = 1.0f;
		public float useHeightProjection = 1.0f;
		public float refractStrength = 1.0f;
		public float reflectProjection = 1.0f;
		public float reflectBlur = 0.0f;
		public float aberrationScale = 0.1f;
		public float roughness = 0.1f;
		public float roughness2 = 0.35f;
		public float reflectTerm = 0.0255f;
		public float reflectSharpen = 0.0f;
		public bool showDepthMask = false;
		public bool showWorldMask = false;
		public float cameraDistance = 1000.0f;
		public float underwaterDepth = 5f;

		//DX9 version
		public bool useDX9Settings = false;


		//objects
		public SuimonoModule moduleObject;
		private SuimonoModuleLib suimonoModuleLibrary;
		private GameObject suimonoObject;
		private Renderer surfaceRenderer;
		private MeshFilter surfaceMesh;
		private MeshCollider surfaceCollider;
		private cameraTools surfaceReflections;
		private Suimono_DistanceBlur surfaceReflBlur;
		private GameObject scaleObject;
		private Renderer scaleRenderer;
		private MeshCollider scaleCollider;
		private MeshFilter scaleMesh;
		private Renderer surfaceVolume;

		//materials
		private Material tempMaterial;

		//type and options
		public string suimonoVersionNumber;
		public bool showGeneral = false;
		public int typeIndex = 1;
		[NonSerialized] public string[] typeOptions = { "Infinite 3D Ocean", "3D Waves", "Flat Plane" };
		public int editorIndex = 1;
		public int editorUseIndex = 1;
		[NonSerialized] public string[] editorOptions = { "Simple", "Advanced" };


		//mesh and lod
		public bool enableCustomMesh = false;
		public int lodIndex;
		public int useLodIndex;
		[NonSerialized] public string[] lodOptions = { "High Detail", "Medium Detail", "Low Detail", "Single Quad" };
		public Mesh customMesh;
		public float oceanScale = 2.0f;
		private bool meshWasSet = false;

		//casutics
		public bool enableCausticFX = true;
		public float causticsFade = 0.55f;
		public Color causticsColor = new Color(1f, 1f, 1f, 1f);

		//tessellation
		public bool enableTess = true;
		public bool useEnableTess = true;
		public float waveTessAmt = 8.0f;
		public float waveTessMin = 0.0f;
		public float waveTessSpread = 0.08f;

		//interaction
		public bool enableInteraction = true;

		//reflections
		public float dynamicReflectFlag = 1.0f;
		public bool enableReflections = true;
		public bool enableDynamicReflections = true;

		public bool useEnableReflections = true;
		public bool useEnableDynamicReflections = true;

		public bool useReflections = true;
		public bool useDynReflections = true;
		public int reflectLayer = 0;
		public int reflectResolution = 4;
		public LayerMask reflectLayerMask;
		public float reflectionDistance = 1000.0f;

		[NonSerialized] public List<string> suiLayerMasks = new List<string>();

		[NonSerialized] public string[] resOptions = { "4096", "2048", "1024", "512", "256", "128", "64", "32", "16", "8" };
		[NonSerialized] public List<int> resolutions = new List<int>() { 4096, 2048, 1024, 512, 256, 128, 64, 32, 16, 8 };
		public int reflectFallback = 1;
		[NonSerialized] public string[] resFallbackOptions = { "None", "Skybox", "Custom Cubemap", "Color" };
		public Texture customRefCubemap;
		public Color customRefColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
		public Color reflectionColor = new Color(1f, 1f, 1f, 1f);

		//custom textures
		public bool enableCustomTextures = false;
		public Texture2D customTexNormal1;
		//public Texture2D customTexHeight1;
		public Texture2D customTexNormal2;
		//public Texture2D customTexHeight2;
		public Texture2D customTexNormal3;
		//public Texture2D customTexHeight3;
		public Texture2D useTexNormal1;
		//public Texture2D useTexHeight1;
		public Texture2D useTexNormal2;
		//public Texture2D useTexHeight2;
		public Texture2D useTexNormal3;
		//public Texture2D useTexHeight3;

		//waves
		public bool showWaves = false;
		public bool customWaves = false;
		public float localTime = 0.0f;
		private Vector2 flow_dir = new Vector2(0f, 0f);
		private Vector3 tempAngle;
		public float beaufortVal = 1.0f;

		//shorelines
		public bool showShore = false;
		public float shorelineHeight = 0.75f;
		public float shorelineFreq = 0.5f;
		public float shorelineScale = 0.15f;
		public float shorelineSpeed = 2.5f;
		public float shorelineNorm = 0.5f;

		//surface
		public bool showSurface = false;
		public float overallBright = 1.0f;
		public float overallTransparency = 1.0f;
		public float depthAmt = 0.1f;
		public float shallowAmt = 0.1f;
		public Color depthColor;
		public Color shallowColor;
		public float edgeAmt = 0.1f;
		public Color specularColor;
		public Color sssColor;
		public Color blendColor;
		public Color overlayColor;

		//foam
		public bool showFoam = false;
		public bool enableFoam = true;
		public Color foamColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
		public float foamScale = 40.0f;
		public float foamSpeed = 0.1f;
		public float edgeFoamAmt = 0.5f;
		public float shallowFoamAmt = 1.0f;
		public float hFoamHeight = 1.0f;
		public float hFoamSpread = 1.0f;
		public float heightFoamAmt = 0.5f;

		//underwater 
		public bool showUnderwater = false;
		public Color underwaterColor = new Color(1f, 0f, 0f, 1f);
		public float underLightFactor = 1.0f;
		public float underRefractionAmount = 0.005f;
		public float underRefractionScale = 1.5f;
		public float underRefractionSpeed = 0.5f;
		public float underwaterFogDist = 20.0f;
		public float underwaterFogSpread = 0.0f;
		public bool enableUnderwater = true;
		public bool enableUnderDebris = false;
		public float underBlurAmount = 1.0f;
		public float underDarkRange = 40.0f;

		//scaling
		public float setScale = 1.0f;
		public Vector3 currentAngles = new Vector3(0f, 0f, 0f);
		public Vector3 currentPosition = new Vector3(0f, 0f, 0f);
		public Vector3 newPos = new Vector3(0f, 0f, 0f);
		public float spacer = 0.0f;
		public float setScaleX = 0.0f;
		public float setScaleZ = 0.0f;
		public float offamt = 0.0f;
		public Vector2 savePos = new Vector2(0f, 0f);
		public Vector2 recPos = new Vector2(0f, 0f);
		public Vector2 _suimono_uv = new Vector2(0f, 0f);

		//editor
		public bool showSimpleEditor = false;

		//shaders
		public Shader useShader;
		public Shader currUseShader;
		public Shader shader_Surface;
		public Shader shader_Scale;
		public Shader shader_Under;

		//presets
		[NonSerialized] public List<string> presetDirs;
		public string[] presetFiles;
		public int presetIndex = -1;
		public int presetUseIndex = -1;
		public int presetFileIndex = 0;
		public int presetFileUseIndex = 0;
		public string[] presetOptions;
		public bool showPresets = false;
		public bool presetStartTransition = false;
		public float presetTimer = 1f;
		public string currentPresetFolder = "Built-In Presets";
		public string currentPresetName = "";
		public int presetTransitionCurrent = 0;
		public float presetTransitionTime = 1.0f;
		public int presetTransIndexFrm = 0;
		public int presetTransIndexTo = 0;
		public bool presetToggleSave = false;
		public bool presetsLoaded = false;
		public string[] presetDataArray;
		public string presetDataString;
		public string dir = "";
		public string baseDir = "SUIMONO - WATER SYSTEM 2/RESOURCES/";
		public string presetSaveName = "my custom preset";
		public string presetFile = "";
		public string workData;
		public string workData2;


		//temporary preset data
		private Color temp_depthColor;
		private Color temp_shallowColor;
		private Color temp_blendColor;
		private Color temp_overlayColor;
		private Color temp_causticsColor;
		private Color temp_reflectionColor;
		private Color temp_specularColor;
		private Color temp_sssColor;
		private Color temp_foamColor;
		private Color temp_underwaterColor;
		private float temp_beaufortScale;
		private float temp_flowDirection;
		private float temp_flowSpeed;
		private float temp_waveScale;
		private float temp_waveHeight;
		private float temp_heightProjection;
		private float temp_turbulenceFactor;
		private float temp_lgWaveHeight;
		private float temp_lgWaveScale;
		private float temp_shorelineHeight;
		private float temp_shorelineFreq;
		private float temp_shorelineScale;
		private float temp_shorelineSpeed;
		private float temp_shorelineNorm;
		private float temp_overallBright;
		private float temp_overallTransparency;
		private float temp_edgeAmt;
		private float temp_depthAmt;
		private float temp_shallowAmt;
		private float temp_refractStrength;
		private float temp_aberrationScale;
		private float temp_causticsFade;
		private float temp_reflectProjection;
		private float temp_reflectBlur;
		private float temp_reflectTerm;
		private float temp_reflectSharpen;
		private float temp_roughness;
		private float temp_roughness2;
		private float temp_foamScale;
		private float temp_foamSpeed;
		private float temp_edgeFoamAmt;
		private float temp_shallowFoamAmt;
		private float temp_heightFoamAmt;
		private float temp_hFoamHeight;
		private float temp_hFoamSpread;
		private float temp_underLightFactor;
		private float temp_underRefractionAmount;
		private float temp_underRefractionScale;
		private float temp_underRefractionSpeed;
		private float temp_underBlurAmount;
		private float temp_underwaterFogDist;
		private float temp_underwaterFogSpread;
		private float temp_underDarkRange;


		public string materialPath;
		public float oceanUseScale;
		public float useSc;
		public Vector2 setSc;
		public Vector2 scaleOff;
		public int i;
		public string layerName;
		public Material skybox;

		[NonSerialized] public List<string> presetDirsArr = new List<string>();
		public int d;
		public int dn;
		[NonSerialized] public List<string> presetFilesArr = new List<string>();
		public string pdir;
		public FileInfo[] fileInfo;
		public int f = 0;
		public int px = 0;
		public int nx = 0;
		public int ax = 0;
		public int n = 0;

		[NonSerialized] public List<string> tempPresetDirsArr = new List<string>();
		public FileInfo[] dirInfo;
		public string[] tempPresetDirs;
		[NonSerialized] public List<string> tempPresetFilesArr = new List<string>();
		public string[] tempPresetFiles;
		public string oldName;
		public string moveName;
		public int setNum;

		public StreamWriter sw;
		public StreamReader sr;
		public string key;
		public string dat;
		public int pFrom;
		public int pTo;
		public int dx;
		public TextAsset datFile;
		public string[] dataS;
		public string retData;
		public bool retVal;

		private float suimono_refl_off = 0.0f;
		private float suimono_refl_sky = 0.0f;
		private float suimono_refl_cube = 0.0f;
		private float suimono_refl_color = 0.0f;

		private static bool reloadData = false;

		private void Start()
		{

			//DISCONNECT FROM PREFAB
#if UNITY_EDITOR
			PrefabUtility.DisconnectPrefabInstance(this.gameObject);
#endif

			//SET PRESET DIRECTORIES
#if UNITY_EDITOR
				baseDir = "SUIMONO - WATER SYSTEM 2/RESOURCES/";
#else
			this.baseDir = "Resources/";
#endif
			this.dir = Application.dataPath + "/" + this.baseDir;


			//get Suimono objects
			if (GameObject.Find("SUIMONO_Module") != null)
			{
				this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));
				if (this.moduleObject == null)
					this.moduleObject = GameObject.Find("SUIMONO_Module").GetComponent<SuimonoModule>();
				if (this.moduleObject != null)
				{
					this.moduleObject.enabled = true;
					this.suimonoModuleLibrary = this.moduleObject.GetComponent<SuimonoModuleLib>();
				}
			}

			//get surface objects
			this.suimonoObject = this.transform.Find("Suimono_Object").gameObject;
			this.surfaceRenderer = this.transform.Find("Suimono_Object").gameObject.GetComponent<Renderer>();
			this.surfaceMesh = this.transform.Find("Suimono_Object").GetComponent<MeshFilter>();
			this.surfaceCollider = this.transform.Find("Suimono_Object").GetComponent<MeshCollider>();
			this.surfaceReflections = this.transform.Find("cam_LocalReflections").gameObject.GetComponent<cameraTools>();
			this.surfaceReflBlur = this.transform.Find("cam_LocalReflections").gameObject.GetComponent<Suimono_DistanceBlur>();

			//get scale object (infinite ocean)
			this.scaleObject = this.transform.Find("Suimono_ObjectScale").gameObject;
			this.scaleRenderer = this.transform.Find("Suimono_ObjectScale").gameObject.GetComponent<Renderer>();
			this.scaleCollider = this.transform.Find("Suimono_ObjectScale").gameObject.GetComponent<MeshCollider>();
			if (this.scaleCollider == null)
			{
				this.scaleCollider = this.transform.Find("Suimono_ObjectScale").gameObject.AddComponent<MeshCollider>();
			}
			this.scaleMesh = this.transform.Find("Suimono_ObjectScale").GetComponent<MeshFilter>();

			//Store Shader References
			this.shader_Surface = HSSuimono.HSSuimono._self._resources.LoadAsset<Shader>("Suimono2_surface");
			this.shader_Scale = HSSuimono.HSSuimono._self._resources.LoadAsset<Shader>("Suimono2_surface_scale");
			this.shader_Under = HSSuimono.HSSuimono._self._resources.LoadAsset<Shader>("Suimono2_surface_under");

			//save material if not already saved
			//#if UNITY_EDITOR
			//materialPath = "Assets/SUIMONO - WATER SYSTEM 2/Resources/mat_" + this.gameObject.name + ".mat";
			//if (AssetDatabase.LoadAssetAtPath(materialPath,Material) == null){
			//	tempMaterial = new Material(suimonoObject.GetComponent<Renderer>().sharedMaterial);
			//	AssetDatabase.CreateAsset(tempMaterial, materialPath);
			//}
			//tempMaterial = AssetDatabase.LoadAssetAtPath(materialPath,Material);
			//#else
			//tempMaterial = new Material(suimonoObject.GetComponent<Renderer>().sharedMaterial);
			this.tempMaterial = new Material(this.suimonoModuleLibrary.materialSurface);
			//#endif

			//setup custom material surface
			if (this.suimonoObject != null)
			{
				this.tempMaterial.shader = this.shader_Surface;
				this.suimonoObject.GetComponent<Renderer>().sharedMaterial = this.tempMaterial;
				this.surfaceRenderer = this.transform.Find("Suimono_Object").gameObject.GetComponent<Renderer>();
			}

			//Load initial scene data
			this.ReloadData();
		}




#if UNITY_EDITOR
			//Check for scene reload and set flag
			[UnityEditor.Callbacks.DidReloadScripts]
			private static void ReloadDataFlag(){
				reloadData = true;
			}
#endif

		private void OnEnable()
		{
			if (Application.isPlaying)
			{
				if (this.moduleObject == null)
				{
					this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));
				}
				if (this.moduleObject != null)
				{
					this.moduleObject.RegisterSurface(this);
				}
			}
		}

		private void OnDisable()
		{
			if (Application.isPlaying)
			{
				if (this.moduleObject == null)
				{
					this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));
				}
				if (this.moduleObject != null)
				{
					this.moduleObject.DeregisterSurface(this);
				}
			}
		}

		private void ReloadData()
		{
			reloadData = false;

			//Set Layer Masks
#if UNITY_EDITOR
				suiLayerMasks = new List<string>();
				for (i = 0; i < 32; i++){
					layerName = LayerMask.LayerToName(i);
					suiLayerMasks.Add(layerName);
				}
#endif

			//Init Presets
#if !UNITY_WEBPLAYER && UNITY_EDITOR
				PresetInit();
				PresetLoad(presetIndex);
#endif

		}

		private void LateUpdate()
		{

			if (this.moduleObject != null)
			{

				//-------------------------------------------------------
				//###  SET SUIMONO VERSION  ###
				//-------------------------------------------------------
				//inherit suimono version number from module object for display in UI elements
				this.suimonoVersionNumber = this.moduleObject.suimonoVersionNumber;


				//-------------------------------------------------------
				//###  RELOAD TEMPORARY DATA  ###
				//-------------------------------------------------------
				if (reloadData)
					this.ReloadData();


				//-------------------------------------------------------
				//###  SET LOCAL TIME AND DIRECTION  ###
				//-------------------------------------------------------
				this.systemLocalTime = this.moduleObject.systemTime;
				this.localTime = this.systemLocalTime * this.flowSpeed * (1.0f / this.waveScale);
				this.flow_dir = this.SuimonoConvertAngleToVector(this.flowDirection);

				this.surfaceRenderer.sharedMaterial.SetVector("_suimono_Dir", new Vector4(this.flow_dir.x, 1.0f, this.flow_dir.y, this.localTime));

				//-------------------------------------------------------
				//###  SET LAYER MASK  ###
				//-------------------------------------------------------
				this.gameObject.layer = this.moduleObject.layerWaterNum;
				if (this.suimonoObject != null)
					this.suimonoObject.layer = this.moduleObject.layerWaterNum;
				if (this.scaleObject != null)
					this.scaleObject.layer = this.moduleObject.layerWaterNum;
				if (this.surfaceReflections != null)
					this.surfaceReflections.gameObject.layer = this.moduleObject.layerWaterNum;


				//-------------------------------------------------------
				//###  FORCE SIZING  ###
				//-------------------------------------------------------
				if (this.underwaterDepth < 0.1f)
					this.underwaterDepth = 0.1f;
				if (!this.enableCustomMesh)
				{
					this.transform.localScale = new Vector3(this.transform.localScale.x, 1.0f, this.transform.localScale.z);
				}
				if (this.typeIndex == 0)
				{
					this.suimonoObject.transform.localScale = new Vector3(this.suimonoObject.transform.localScale.x, 1.0f, this.suimonoObject.transform.localScale.z);
					this.scaleObject.transform.localScale = new Vector3(this.scaleObject.transform.localScale.x, 1.0f, this.scaleObject.transform.localScale.z);
					this.surfaceReflections.transform.localScale = new Vector3(this.surfaceReflections.transform.localScale.x, 1.0f, this.surfaceReflections.transform.localScale.z);
				}

				//-------------------------------------------------------
				//###  CALCULATE BEAUFORT SCALE  ###
				//-------------------------------------------------------
				this.useBeaufortScale = !this.customWaves;
				if (this.useBeaufortScale)
				{
					this.beaufortVal = this.beaufortScale / 12.0f;
					this.turbulenceFactor = Mathf.Clamp(Mathf.Lerp(-0.1f, 2.1f, this.beaufortVal) * 0.9f, 0.0f, 0.75f);
					this.waveHeight = Mathf.Clamp(Mathf.Lerp(0.0f, 5.0f, this.beaufortVal), 0.0f, 0.65f);
					this.waveHeight = this.waveHeight - Mathf.Clamp(Mathf.Lerp(-1.5f, 1.0f, this.beaufortVal), 0.0f, 0.5f);
					this.lgWaveHeight = Mathf.Clamp(Mathf.Lerp(-0.2f, 1.1f, this.beaufortVal) * 2.8f, 0.0f, 3.0f);

					//freeze scale for ocean
					if (this.typeIndex == 0)
					{
						this.waveScale = 0.5f;
						this.lgWaveScale = 0.03125f;
					}
				}


				//-------------------------------------------------------
				//###  LOAD PRESET CHANGES  ###
				//-------------------------------------------------------
#if UNITY_EDITOR
					// detect changes to preset and run update when applicable
					if (presetUseIndex != presetIndex){
						presetUseIndex = presetIndex;
						PresetLoad(presetIndex);
					}
					// reload preset settings when editor mode is switched between simple and advanced.
					if (editorUseIndex != editorIndex){
						editorUseIndex = editorIndex;
						PresetLoad(presetIndex);
					}
#endif



				//-------------------------------------------------------
				//###  HANDLE PRESET TRANSITIONS  ###
				//-------------------------------------------------------
				if (this.presetStartTransition)
				{
					if (this.presetTimer >= 1.0f)
					{
						this.presetStartTransition = false;
					}
					else
					{
						this.presetTimer += (Time.deltaTime / this.presetTransitionTime);
						this.PresetLoadBuild(this.currentPresetFolder, this.currentPresetName);
					}
				}




				//-------------------------------------------------------
				//###  SET MESH LOD LEVEL  ###
				//-------------------------------------------------------
				// change the surface mesh based on the selected level of detail
				if (this.typeIndex == 0)
					this.useLodIndex = 0; //infinite ocean requires high detail mesh
				if (this.typeIndex == 1)
					this.useLodIndex = this.lodIndex; //3d waves setting picks mesh set in UI
				if (this.typeIndex == 2)
					this.useLodIndex = 3; // flat surface forces single quad mesh

				if (this.typeIndex == 0)
					this.enableCustomMesh = false; // force custom mesh off when using infinite ocean
				if (this.enableCustomMesh == false)
				{
					if (this.suimonoModuleLibrary && !this.meshWasSet)
					{
						if (this.suimonoModuleLibrary.texNormalC && this.surfaceMesh != null)
							this.surfaceMesh.mesh = this.suimonoModuleLibrary.meshLevel[this.useLodIndex];
						if (this.suimonoModuleLibrary.texNormalC && this.surfaceCollider != null)
							this.surfaceCollider.sharedMesh = this.suimonoModuleLibrary.meshLevel[3];
						this.meshWasSet = true;
					}
					else
					{
						this.meshWasSet = false;
					}
				}
				else
				{
					if (this.customMesh != null)
					{
						if (this.surfaceMesh != null)
							this.surfaceMesh.mesh = this.customMesh;
						if (this.surfaceCollider != null)
							this.surfaceCollider.sharedMesh = this.customMesh;
					}
					else
					{
						if (this.suimonoModuleLibrary.texNormalC && this.surfaceMesh != null)
							this.surfaceMesh.mesh = this.suimonoModuleLibrary.meshLevel[this.useLodIndex];
						if (this.suimonoModuleLibrary.texNormalC && this.surfaceCollider != null)
							this.surfaceCollider.sharedMesh = this.suimonoModuleLibrary.meshLevel[3];
						this.meshWasSet = false;
					}
				}
				if (this.useLodIndex == 3)
				{
					this.useHeightProjection = 0.0f;
					this.useEnableTess = false;
				}
				else
				{
					this.useHeightProjection = this.heightProjection;
					this.useEnableTess = this.enableTess;
				}

				//set scale mesh
				if (this.suimonoModuleLibrary.texNormalC && this.scaleMesh != null)
					this.scaleMesh.mesh = this.suimonoModuleLibrary.meshLevel[1];


				//-------------------------------------------------------
				//###  Set Custom Textures  ###
				//-------------------------------------------------------
				if (this.enableCustomTextures)
				{
					if (this.customTexNormal1 != null)
					{
						this.useTexNormal1 = this.customTexNormal1;
					}
					else
					{
						this.useTexNormal1 = this.suimonoModuleLibrary.texNormalC;
					}
					//if (customTexHeight1 != null){
					//	useTexHeight1 = customTexHeight1;
					//	} else {
					//		useTexHeight1 = suimonoModuleLibrary.texHeightC;
					//	}
					if (this.customTexNormal2 != null)
					{
						this.useTexNormal2 = this.customTexNormal2;
					}
					else
					{
						this.useTexNormal2 = this.suimonoModuleLibrary.texNormalT;
					}
					//if (customTexHeight2 != null){
					//	useTexHeight2 = customTexHeight2;
					//	} else {
					//		useTexHeight2 = suimonoModuleLibrary.texHeightT;
					//	}
					if (this.customTexNormal3 != null)
					{
						this.useTexNormal3 = this.customTexNormal3;
					}
					else
					{
						this.useTexNormal3 = this.suimonoModuleLibrary.texNormalR;
					}
					//if (customTexHeight3 != null){
					//	useTexHeight3 = customTexHeight3;
					//	} else {
					//		useTexHeight3 = suimonoModuleLibrary.texHeightR;
					//	}

				}
				else
				{
					if (this.suimonoModuleLibrary != null)
					{
						this.useTexNormal1 = this.suimonoModuleLibrary.texNormalC;
						//useTexHeight1 = suimonoModuleLibrary.texHeightC;
						this.useTexNormal2 = this.suimonoModuleLibrary.texNormalT;
						//useTexHeight2 = suimonoModuleLibrary.texHeightT;
						this.useTexNormal3 = this.suimonoModuleLibrary.texNormalR;
						//useTexHeight3 = suimonoModuleLibrary.texHeightR;
					}
				}
				if (this.suimonoModuleLibrary != null)
				{
					if (this.surfaceRenderer != null)
						this.surfaceRenderer.sharedMaterial.SetTexture("_MaskTex", this.suimonoModuleLibrary.texMask);
				}


				//-------------------------------------------------------
				//###  SET REFLECTION OBJECT PROPERTIES  ###
				//-------------------------------------------------------
				if (this.surfaceReflections != null && this.moduleObject != null)
				{
					this.useEnableReflections = this.enableReflections;
					this.useEnableDynamicReflections = this.enableDynamicReflections;

					if (!this.moduleObject.enableReflections)
						this.useEnableReflections = false;
					if (!this.moduleObject.enableDynamicReflections)
						this.useEnableDynamicReflections = false;

					if (!this.useEnableReflections || !this.moduleObject.enableReflections)
					{
						this.useReflections = false;
						this.surfaceReflections.gameObject.SetActive(false);
					}
					else
					{

						if (!this.useEnableDynamicReflections || !this.moduleObject.enableDynamicReflections)
						{
							this.surfaceReflections.gameObject.SetActive(false);
						}
						else
						{
							this.surfaceReflections.gameObject.SetActive(true);
							this.useReflections = true;
							this.reflectLayer = (this.reflectLayer & ~(1 << this.moduleObject.layerWaterNum));
							this.reflectLayer = (this.reflectLayer & ~(1 << this.moduleObject.layerDepthNum));
							this.reflectLayer = (this.reflectLayer & ~(1 << this.moduleObject.layerScreenFXNum));

							this.surfaceReflections.setLayers = this.reflectLayer;
							this.surfaceReflections.resolution = Convert.ToInt32(this.resolutions[this.reflectResolution]);
							if (this.moduleObject.setCameraComponent != null)
							{
								this.reflectionDistance = this.moduleObject.setCameraComponent.farClipPlane + 200.0f;
							}
							this.surfaceReflections.reflectionDistance = this.reflectionDistance;

							//blur settings
							this.surfaceReflBlur.blurAmt = this.reflectBlur;

							if (this.useShader == this.shader_Under)
							{
								this.surfaceReflections.isUnderwater = true;
							}
							else
							{
								this.surfaceReflections.isUnderwater = false;
							}
						}
					}
				}


				//-------------------------------------------------------
				//###  SEND SETTINGS TO SHADER  ###
				//-------------------------------------------------------
				if (this.surfaceRenderer != null)
				{

					//set shader
					if (Application.isPlaying && this.useShader != null)
					{
						if (this.currUseShader != this.useShader)
						{
							this.currUseShader = this.useShader;
							this.surfaceRenderer.sharedMaterial.shader = this.currUseShader;
						}
					}

					//set playmode
					if (!Application.isPlaying)
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("_isPlaying", 0.0f);
					}
					else
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("_isPlaying", 1.0f);
					}

					//set texture
					this.surfaceRenderer.sharedMaterial.SetTexture("_NormalTexS", this.useTexNormal1);
					this.surfaceRenderer.sharedMaterial.SetTexture("_NormalTexD", this.useTexNormal2);
					this.surfaceRenderer.sharedMaterial.SetTexture("_NormalTexR", this.useTexNormal3);

					//set beaufort and waves
					this.surfaceRenderer.sharedMaterial.SetFloat("_beaufortFlag", this.useBeaufortScale ? 1.0f : 0.0f);
					this.surfaceRenderer.sharedMaterial.SetFloat("_beaufortScale", this.beaufortVal);
					this.surfaceRenderer.sharedMaterial.SetFloat("_turbulenceFactor", this.turbulenceFactor);

					//set texture speed and scale
					this.surfaceRenderer.sharedMaterial.SetTextureScale("_NormalTexS", new Vector2((this.suimonoObject.transform.localScale.x / this.waveScale) * this.transform.localScale.x, (this.suimonoObject.transform.localScale.z / this.waveScale) * this.transform.localScale.z));
					this.surfaceRenderer.sharedMaterial.SetVector("_scaleUVs", new Vector4(this.suimonoObject.transform.localScale.x / this.waveScale, this.suimonoObject.transform.localScale.z / this.waveScale, 0f, 0f));
					this.surfaceRenderer.sharedMaterial.SetFloat("_lgWaveScale", this.lgWaveScale);
					this.surfaceRenderer.sharedMaterial.SetFloat("_lgWaveHeight", this.lgWaveHeight);

					//set tessellation settings
					if (this.typeIndex == 0)
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("_tessScale", this.suimonoObject.transform.localScale.x);
					}
					else
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("_tessScale", this.transform.localScale.x);
					}
					this.surfaceRenderer.sharedMaterial.SetFloat("_Tess", Mathf.Lerp(0.001f, this.waveTessAmt, this.useEnableTess ? 1.0f : 0.0f));
					this.surfaceRenderer.sharedMaterial.SetFloat("_minDist", Mathf.Lerp(-180.0f, 0.0f, this.waveTessMin));
					this.surfaceRenderer.sharedMaterial.SetFloat("_maxDist", Mathf.Lerp(20.0f, 500.0f, this.waveTessSpread));

					//set system fog coordinates
					this.surfaceRenderer.sharedMaterial.SetFloat("_unity_fogstart", RenderSettings.fogStartDistance);
					this.surfaceRenderer.sharedMaterial.SetFloat("_unity_fogend", RenderSettings.fogEndDistance);

					//set caustics
					this.surfaceRenderer.sharedMaterial.SetFloat("_causticsFlag", this.enableCausticFX ? 1.0f : 0.0f);
					this.surfaceRenderer.sharedMaterial.SetFloat("_CausticsFade", Mathf.Lerp(1f, 500f, this.causticsFade));
					this.surfaceRenderer.sharedMaterial.SetColor("_CausticsColor", this.causticsColor);

					//set aberration scale
					this.surfaceRenderer.sharedMaterial.SetFloat("_aberrationScale", this.aberrationScale);

					//set foam speed and scale
					this.surfaceRenderer.sharedMaterial.SetFloat("_enableFoam", this.enableFoam ? 1.0f : 0.0f);
					this.surfaceRenderer.sharedMaterial.SetFloat("_EdgeFoamFade", Mathf.Lerp(1500.0f, 5.0f, this.edgeFoamAmt));
					this.surfaceRenderer.sharedMaterial.SetFloat("_HeightFoamAmt", this.heightFoamAmt);
					this.surfaceRenderer.sharedMaterial.SetFloat("_HeightFoamHeight", this.hFoamHeight);
					this.surfaceRenderer.sharedMaterial.SetFloat("_HeightFoamSpread", this.hFoamSpread);
					this.surfaceRenderer.sharedMaterial.SetFloat("_foamSpeed", this.foamSpeed);
					this.surfaceRenderer.sharedMaterial.SetTextureScale("_FoamTex", this.foamScale * new Vector2((this.suimonoObject.transform.localScale.x / this.foamScale) * this.transform.localScale.x, (this.suimonoObject.transform.localScale.z / this.foamScale) * this.transform.localScale.z));
					this.surfaceRenderer.sharedMaterial.SetFloat("_foamScale", Mathf.Lerp(160.0f, 1.0f, this.foamScale));
					this.surfaceRenderer.sharedMaterial.SetColor("_FoamColor", this.foamColor);
					this.surfaceRenderer.sharedMaterial.SetFloat("_ShallowFoamAmt", this.shallowFoamAmt);

					//set height and normal scales
					this.surfaceRenderer.sharedMaterial.SetFloat("_heightScaleFac", (1.0f / this.transform.localScale.y));
					this.surfaceRenderer.sharedMaterial.SetFloat("_heightProjection", this.useHeightProjection);
					this.surfaceRenderer.sharedMaterial.SetFloat("_heightScale", this.waveHeight);
					this.surfaceRenderer.sharedMaterial.SetFloat("_RefractStrength", this.refractStrength);
					this.surfaceRenderer.sharedMaterial.SetFloat("_ReflectStrength", this.reflectProjection);

					//set shoreline properties
					this.surfaceRenderer.sharedMaterial.SetFloat("_shorelineHeight", this.shorelineHeight);
					this.surfaceRenderer.sharedMaterial.SetFloat("_shorelineFrequency", this.shorelineFreq);
					this.surfaceRenderer.sharedMaterial.SetFloat("_shorelineScale", 0.1f);
					this.surfaceRenderer.sharedMaterial.SetFloat("_shorelineSpeed", this.shorelineSpeed);
					this.surfaceRenderer.sharedMaterial.SetFloat("_shorelineNorm", this.shorelineNorm);

					//set physical properties
					this.surfaceRenderer.sharedMaterial.SetFloat("_roughness", this.roughness);
					this.surfaceRenderer.sharedMaterial.SetFloat("_roughness2", this.roughness2);
					this.surfaceRenderer.sharedMaterial.SetFloat("_reflecTerm", this.reflectTerm);
					this.surfaceRenderer.sharedMaterial.SetFloat("_reflecSharp", Mathf.Lerp(0.0f, -1.5f, this.reflectSharpen));


					//set surface settings
					this.surfaceRenderer.sharedMaterial.SetFloat("_overallBrightness", this.overallBright);
					this.surfaceRenderer.sharedMaterial.SetFloat("_overallTransparency", this.overallTransparency);
					this.surfaceRenderer.sharedMaterial.SetFloat("_DepthFade", Mathf.Lerp(0.1f, 200.0f, this.depthAmt));
					this.surfaceRenderer.sharedMaterial.SetFloat("_ShallowFade", Mathf.Lerp(0.1f, 800.0f, this.shallowAmt));
					this.surfaceRenderer.sharedMaterial.SetColor("_depthColor", this.depthColor);
					this.surfaceRenderer.sharedMaterial.SetColor("_shallowColor", this.shallowColor);
					this.surfaceRenderer.sharedMaterial.SetFloat("_EdgeFade", Mathf.Lerp(10.0f, 1000.0f, this.edgeAmt));
					this.surfaceRenderer.sharedMaterial.SetColor("_SpecularColor", this.specularColor);
					this.surfaceRenderer.sharedMaterial.SetColor("_SSSColor", this.sssColor);
					this.surfaceRenderer.sharedMaterial.SetColor("_BlendColor", this.blendColor);
					this.surfaceRenderer.sharedMaterial.SetColor("_OverlayColor", this.overlayColor);
					this.surfaceRenderer.sharedMaterial.SetColor("_UnderwaterColor", this.underwaterColor);

					//set reflection properties
					this.surfaceRenderer.sharedMaterial.SetFloat("_reflectFlag", this.useEnableReflections ? 1.0f : 0.0f);
					this.surfaceRenderer.sharedMaterial.SetFloat("_reflectDynamicFlag", this.useEnableDynamicReflections ? 1.0f : 0.0f);
					this.surfaceRenderer.sharedMaterial.SetFloat("_reflectFallback", this.reflectFallback);
					this.surfaceRenderer.sharedMaterial.SetColor("_reflectFallbackColor", this.customRefColor);
					this.surfaceRenderer.sharedMaterial.SetColor("_ReflectionColor", this.reflectionColor);

					//set skybox texture
					this.skybox = RenderSettings.skybox;
					if (this.skybox != null && this.skybox.HasProperty("_Tex") && this.skybox.HasProperty("_Tint") && this.skybox.HasProperty("_Exposure") && this.skybox.HasProperty("_Rotation"))
					{
						this.surfaceRenderer.sharedMaterial.SetTexture("_SkyCubemap", this.skybox.GetTexture("_Tex"));
						this.surfaceRenderer.sharedMaterial.SetColor("_SkyTint", this.skybox.GetColor("_Tint"));
						this.surfaceRenderer.sharedMaterial.SetFloat("_SkyExposure", this.skybox.GetFloat("_Exposure"));
						this.surfaceRenderer.sharedMaterial.SetFloat("_SkyRotation", this.skybox.GetFloat("_Rotation"));
					}

					//set custom cubemap
					if (this.customRefCubemap != null)
						this.surfaceRenderer.sharedMaterial.SetTexture("_CubeTex", this.customRefCubemap);

					//set camera properties
					this.surfaceRenderer.sharedMaterial.SetFloat("_cameraDistance", this.cameraDistance);


					//Force shoreline foam scale
					this.surfaceRenderer.sharedMaterial.SetTextureScale("_WaveTex", Vector2.one);
					this.scaleRenderer.sharedMaterial.SetTextureScale("_WaveTex", Vector2.one);


					/*
					//-------------------------------------------------------
					//###  SET SHADER DEFINE KEYWORDS  ###
					//-------------------------------------------------------
					if (surfaceRenderer != null){
						surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_TESS_ON");
						surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_TRANS_ON");
						surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_CAUST_ON");
						surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_DYNREFL_ON");
						if (enableTess) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_TESS_ON");
						if (moduleObject.enableTransparency) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_TRANS_ON");
						if (moduleObject.enableCaustics) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_CAUST_ON");
						if (useDynReflections) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_DYNREFL_ON");

						surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_REFL_OFF");
						surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_REFL_SKY");
						surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_REFL_CUBE");
						surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_REFL_COLOR");
						if (reflectFallback == 0) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_REFL_OFF");
						if (reflectFallback == 1) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_REFL_SKY");
						if (reflectFallback == 2) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_REFL_CUBE");
						if (reflectFallback == 3) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_REFL_COLOR");

						//surfaceRenderer.sharedMaterial.DisableKeyword("SUIMONO_FOAM_ON");
						//if (Application.isPlaying && enableFoam) surfaceRenderer.sharedMaterial.EnableKeyword("SUIMONO_FOAM_ON");
					}

					if (scaleRenderer != null && typeIndex == 0){
						scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_TESS_ON");
						scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_TRANS_ON");
						scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_CAUST_ON");
						scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_DYNREFL_ON");
						if (enableTess) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_TESS_ON");
						if (moduleObject.enableTransparency) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_TRANS_ON");
						if (moduleObject.enableCaustics) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_CAUST_ON");
						if (useDynReflections) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_DYNREFL_ON");

						scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_REFL_OFF");
						scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_REFL_SKY");
						scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_REFL_CUBE");
						scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_REFL_COLOR");
						if (reflectFallback == 0) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_REFL_OFF");
						if (reflectFallback == 1) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_REFL_SKY");
						if (reflectFallback == 2) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_REFL_CUBE");
						if (reflectFallback == 3) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_REFL_COLOR");

						//scaleRenderer.sharedMaterial.DisableKeyword("SUIMONO_FOAM_ON");
						//if (Application.isPlaying && enableFoam) scaleRenderer.sharedMaterial.EnableKeyword("SUIMONO_FOAM_ON");

					}
					*/





					//-------------------------------------------------------
					//###  SET SHADER BRANCH KEYS  ###
					//-------------------------------------------------------

					this.suimono_refl_off = 0.0f;
					this.suimono_refl_sky = 0.0f;
					this.suimono_refl_cube = 0.0f;
					this.suimono_refl_color = 0.0f;
					if (this.reflectFallback == 0)
						this.suimono_refl_off = 1.0f;
					if (this.reflectFallback == 1)
						this.suimono_refl_sky = 1.0f;
					if (this.reflectFallback == 2)
						this.suimono_refl_cube = 1.0f;
					if (this.reflectFallback == 3)
						this.suimono_refl_color = 1.0f;

					if (this.surfaceRenderer != null)
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("suimono_tess_on", this.enableTess ? 1.0f : 0.0f);
						this.surfaceRenderer.sharedMaterial.SetFloat("suimono_trans_on", this.moduleObject.enableTransparency ? 1.0f : 0.0f);
						this.surfaceRenderer.sharedMaterial.SetFloat("suimono_caust_on", this.moduleObject.enableCaustics ? 1.0f : 0.0f);
						this.surfaceRenderer.sharedMaterial.SetFloat("suimono_dynrefl_on", this.useDynReflections ? 1.0f : 0.0f);
						this.surfaceRenderer.sharedMaterial.SetFloat("suimono_refl_off", this.suimono_refl_off);
						this.surfaceRenderer.sharedMaterial.SetFloat("suimono_refl_sky", this.suimono_refl_sky);
						this.surfaceRenderer.sharedMaterial.SetFloat("suimono_refl_cube", this.suimono_refl_cube);
						this.surfaceRenderer.sharedMaterial.SetFloat("suimono_refl_color", this.suimono_refl_color);
					}

					if (this.scaleRenderer != null && this.typeIndex == 0)
					{
						this.scaleRenderer.sharedMaterial.SetFloat("suimono_tess_on", this.enableTess ? 1.0f : 0.0f);
						this.scaleRenderer.sharedMaterial.SetFloat("suimono_trans_on", this.moduleObject.enableTransparency ? 1.0f : 0.0f);
						this.scaleRenderer.sharedMaterial.SetFloat("suimono_caust_on", this.moduleObject.enableCaustics ? 1.0f : 0.0f);
						this.scaleRenderer.sharedMaterial.SetFloat("suimono_dynrefl_on", this.useDynReflections ? 1.0f : 0.0f);
						this.scaleRenderer.sharedMaterial.SetFloat("suimono_refl_off", this.suimono_refl_off);
						this.scaleRenderer.sharedMaterial.SetFloat("suimono_refl_sky", this.suimono_refl_sky);
						this.scaleRenderer.sharedMaterial.SetFloat("suimono_refl_cube", this.suimono_refl_cube);
						this.scaleRenderer.sharedMaterial.SetFloat("suimono_refl_color", this.suimono_refl_color);
					}

				}


				//-------------------------------------------------------
				//###  enable / disable infinite scale surface  ###
				//-------------------------------------------------------
				if (this.typeIndex == 0 && Application.isPlaying)
				{
					if (this.moduleObject.isUnderwater)
					{
						if (this.scaleRenderer != null)
							this.scaleRenderer.enabled = false;
						if (this.scaleCollider != null)
							this.scaleCollider.enabled = false;

					}
					else
					{
						if (this.scaleRenderer != null)
							this.scaleRenderer.enabled = true;
						if (this.scaleCollider != null)
							this.scaleCollider.enabled = true;
					}
				}
				else
				{
					if (this.scaleRenderer != null)
						this.scaleRenderer.enabled = false;
					if (this.scaleCollider != null)
						this.scaleCollider.enabled = false;
				}



				//-------------------------------------------------------
				//###  set position and rotation for infinite ocean  ###
				//-------------------------------------------------------
				if (Application.isPlaying)
				{
					if (this.typeIndex == 0)
					{

						//force rotation
						this.transform.eulerAngles = new Vector3(this.transform.eulerAngles.x, 0.0f, this.transform.eulerAngles.z);

						//calculate scales
						if (this.oceanScale < 1.0f)
							this.oceanScale = 1.0f;
						this.offamt = (0.4027f * this.oceanScale) / this.waveScale;
						this.spacer = (this.suimonoObject.transform.localScale.x * 4.0f);
						this.newPos = new Vector3(this.moduleObject.setCamera.position.x, this.suimonoObject.transform.position.y, this.moduleObject.setCamera.position.z);
						if (Mathf.Abs(this.suimonoObject.transform.position.x - this.newPos.x) > this.spacer)
						{
							if (this.suimonoObject.transform.position.x > this.newPos.x)
								this.setScaleX -= this.offamt;
							if (this.suimonoObject.transform.position.x < this.newPos.x)
								this.setScaleX += this.offamt;
							this.suimonoObject.transform.position = new Vector3(this.newPos.x, this.suimonoObject.transform.position.y, this.suimonoObject.transform.position.z);
							this.scaleObject.transform.position = new Vector3(this.newPos.x, this.scaleObject.transform.position.y, this.scaleObject.transform.position.z);
						}
						if (Mathf.Abs(this.suimonoObject.transform.position.z - this.newPos.z) > this.spacer)
						{
							if (this.suimonoObject.transform.position.z > this.newPos.z)
								this.setScaleZ -= this.offamt;
							if (this.suimonoObject.transform.position.z < this.newPos.z)
								this.setScaleZ += this.offamt;
							this.suimonoObject.transform.position = new Vector3(this.suimonoObject.transform.position.x, this.suimonoObject.transform.position.y, this.newPos.z);
							this.scaleObject.transform.position = new Vector3(this.scaleObject.transform.position.x, this.scaleObject.transform.position.y, this.newPos.z);
						}

						//update position
						if (this.currentPosition != this.suimonoObject.transform.position)
						{
							this.currentPosition = this.suimonoObject.transform.position;
							this.savePos = new Vector2(this.setScaleX, this.setScaleZ);
						}

						//set shader offset
						this.surfaceRenderer.sharedMaterial.SetFloat("_suimono_uvx", 0.0f - (this.savePos.x));
						this.surfaceRenderer.sharedMaterial.SetFloat("_suimono_uvy", 0.0f - (this.savePos.y));

						//set scale object offset
						this.scaleObject.transform.localPosition = new Vector3(this.scaleObject.transform.localPosition.x, -0.1f, this.scaleObject.transform.localPosition.z);

						//set infinite ocean object scaling
						if (this.scaleRenderer != null)
						{
							this.setScale = Mathf.Ceil(this.moduleObject.setCameraComponent.farClipPlane / 20.0f) * this.suimonoObject.transform.localScale.x;
							this.scaleObject.transform.localScale = new Vector3(this.setScale * 0.5f, 1.0f, this.setScale * 0.5f);

							this.oceanUseScale = 4.0f;
							this.transform.localScale = new Vector3(1f, 1f, 1f);
							this.suimonoObject.transform.localScale = new Vector3(this.oceanUseScale * this.oceanScale, 1.0f, this.oceanUseScale * this.oceanScale);

							//copy shader settings to infinite scale surface
							if (this.scaleRenderer != null)
							{
								this.scaleRenderer.material.CopyPropertiesFromMaterial(this.tempMaterial);
								this.scaleRenderer.sharedMaterial.SetFloat("_suimono_uvx", 0.0f - this.savePos.x);
								this.scaleRenderer.sharedMaterial.SetFloat("_suimono_uvy", 0.0f - this.savePos.y);

								this.setSc = this.scaleRenderer.sharedMaterial.GetTextureScale("_NormalTexS");

								this.useSc = (this.scaleObject.transform.localScale.x / this.suimonoObject.transform.localScale.x);
								this.scaleRenderer.sharedMaterial.SetTextureScale("_NormalTexS", this.setSc * this.useSc);
								this.scaleRenderer.sharedMaterial.SetTextureScale("_FoamTex", this.setSc * this.useSc);
							}

						}


					}
					else
					{
						this.savePos = new Vector3(0f, 0f, 0f);
						this.suimonoObject.transform.localScale = Vector3.one;
						this.suimonoObject.transform.localPosition = Vector3.zero;
						this.scaleObject.transform.localScale = Vector3.one;
						this.scaleObject.transform.localPosition = Vector3.zero;
						this.surfaceRenderer.sharedMaterial.SetFloat("_suimono_uvx", 0.0f);
						this.surfaceRenderer.sharedMaterial.SetFloat("_suimono_uvy", 0.0f);

					}
				}



				//-------------------------------------------------------
				//###  Set Debug Modes  ###
				//-------------------------------------------------------
				if (this.surfaceRenderer != null)
				{
					if (this.showDepthMask)
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("_suimono_DebugDepthMask", 1.0f);
					}
					else
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("_suimono_DebugDepthMask", 0.0f);
					}

					if (this.showWorldMask)
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("_suimono_DebugWorldNormalMask", 1.0f);
					}
					else
					{
						this.surfaceRenderer.sharedMaterial.SetFloat("_suimono_DebugWorldNormalMask", 0.0f);
					}

				}


				//-------------------------------------------------------
				//###  Update Preset Listing  ###
				//-------------------------------------------------------
#if UNITY_EDITOR
					if (presetFileUseIndex != presetFileIndex){
						presetFileUseIndex = presetFileIndex;
						PresetInit();
					}
#endif
			}
		}





		public Vector2 SuimonoConvertAngleToVector(float convertAngle)
		{
			this.flow_dir = new Vector2(0f, 0f);
			this.tempAngle = new Vector3(0f, 0f, 0f);
			if (convertAngle <= 180.0f)
			{
				this.tempAngle = Vector3.Slerp(Vector3.forward, -Vector3.forward, (convertAngle) / 180.0f);
				this.flow_dir = new Vector2(this.tempAngle.x, this.tempAngle.z);
			}
			if (convertAngle > 180.0f)
			{
				this.tempAngle = Vector3.Slerp(-Vector3.forward, Vector3.forward, (convertAngle - 180.0f) / 180.0f);
				this.flow_dir = new Vector2(-this.tempAngle.x, this.tempAngle.z);
			}

			return this.flow_dir;
		}









		// ########## PUBLIC FUNCTIONS #######################################################################################################################

		public void SuimonoSetPreset(string fName, string pName)
		{
			this.presetTimer = 1f;
			this.SetTemporaryPresetData();
			this.PresetLoadBuild(fName, pName);
		}



		public void SuimonoSavePreset(string fName, string pName)
		{
			int setFolder = -1;
			int setPreset = -1;

			setFolder = this.PresetGetNum("folder", fName);
			setPreset = this.PresetGetNum("preset", pName);

			if (setFolder >= 0 && setPreset >= 0)
			{
				this.PresetSave(setFolder, setPreset);
			}
			else
			{
				Debug.Log("The Preset " + pName + " in folder " + fName + " cannot be found!");
			}
		}





		//########### NEW PRESETS #########################################################################################################################
		private void PresetInit()
		{
			this.presetTimer = 1f;

#if !UNITY_WEBPLAYER

			//get preset directories
			this.presetDirsArr = new List<string>();
			this.dirInfo = new DirectoryInfo(this.dir + "/").GetFiles("SUIMONO_PRESETS_*");
			if (new DirectoryInfo(this.dir + "/") != null)
			{
				for (this.d = 0; this.d < this.dirInfo.Length; this.d++)
				{
					this.presetDirsArr.Add(this.dirInfo[this.d].ToString());
				}
			}
			this.presetDirs = new List<string>(new string[this.presetDirsArr.Count]);
			for (this.dn = 0; this.dn < this.presetDirsArr.Count; this.dn++)
			{
				this.presetDirs[this.dn] = this.presetDirsArr[this.dn].ToString();
				this.presetDirs[this.dn] = this.presetDirs[this.dn].Remove(0, this.dir.Length);
				this.presetDirs[this.dn] = this.presetDirs[this.dn].Replace("SUIMONO_PRESETS_", "");
				this.presetDirs[this.dn] = this.presetDirs[this.dn].Replace(".meta", "");
			}


			//get preset files listing
			this.presetFilesArr = new List<string>();
			this.pdir = this.dir + "/SUIMONO_PRESETS_" + this.presetDirs[this.presetFileIndex];

			this.fileInfo = new DirectoryInfo(this.pdir).GetFiles("SUIMONO_PRESET_*");
			if (new DirectoryInfo(this.pdir) != null)
			{
				for (this.f = 0; this.f < this.fileInfo.Length; this.f++)
				{
					this.presetFilesArr.Add(this.fileInfo[this.f].ToString());
				}
			}
			this.px = 0;
			for (this.nx = 0; this.nx < this.presetFilesArr.Count; this.nx++)
			{
				if (!this.presetFilesArr[this.nx].ToString().Contains(".meta"))
					this.px++;
			}
			this.presetFiles = new string[this.px];
			this.ax = 0;
			for (this.n = 0; this.n < this.presetFilesArr.Count; this.n++)
			{
				if (!this.presetFilesArr[this.n].ToString().Contains(".meta"))
				{
					this.presetFiles[this.ax] = this.presetFilesArr[this.n].ToString();
					this.presetFiles[this.ax] = this.presetFiles[this.ax].Remove(0, this.pdir.Length);
					this.presetFiles[this.ax] = this.presetFiles[this.ax].Replace("SUIMONO_PRESET_", "");
					this.presetFiles[this.ax] = this.presetFiles[this.ax].Replace(".txt", "");
					this.ax++;
				}
			}

#endif
		}







		public int PresetGetNum(string mode, string pName)
		{

#if !UNITY_WEBPLAYER

			int setMode = -1;
			int setFolder = -1;
			int setPreset = -1;

			if (mode == "folder")
			{
				//get preset directories
				this.tempPresetDirsArr = new List<string>();
				this.dirInfo = new DirectoryInfo(this.dir + "/").GetFiles("SUIMONO_PRESETS_*");
				if (new DirectoryInfo(this.dir + "/") != null)
				{
					for (this.d = 0; this.d < this.dirInfo.Length; this.d++)
					{
						this.tempPresetDirsArr.Add(this.dirInfo[this.d].ToString());
					}
				}
				this.tempPresetDirs = new string[this.tempPresetDirsArr.Count];
				for (this.dn = 0; this.dn < this.tempPresetDirsArr.Count; this.dn++)
				{
					this.tempPresetDirs[this.dn] = this.tempPresetDirsArr[this.dn].ToString();
					this.tempPresetDirs[this.dn] = this.tempPresetDirs[this.dn].Remove(0, this.dir.Length);
					this.tempPresetDirs[this.dn] = this.tempPresetDirs[this.dn].Replace("SUIMONO_PRESETS_", "");
					this.tempPresetDirs[this.dn] = this.tempPresetDirs[this.dn].Replace(".meta", "");
					if (this.tempPresetDirs[this.dn] == pName) setFolder = this.dn;
				}
				setMode = setFolder;
			}

			if (mode == "preset")
			{
				//get preset files listing
				this.tempPresetFilesArr = new List<string>();
				this.pdir = this.dir + "/SUIMONO_PRESETS_" + this.presetDirs[this.presetFileIndex];
				this.fileInfo = new DirectoryInfo(this.pdir).GetFiles("SUIMONO_PRESET_*");
				if (new DirectoryInfo(this.pdir) != null)
				{
					for (this.f = 0; this.f < this.fileInfo.Length; this.f++)
					{
						this.tempPresetFilesArr.Add(this.fileInfo[this.f].ToString());
					}
				}
				this.px = 0;
				for (this.nx = 0; this.nx < this.tempPresetFilesArr.Count; this.nx++)
				{
					if (!this.tempPresetFilesArr[this.nx].ToString().Contains(".meta"))
						this.px++;
				}
				this.tempPresetFiles = new string[this.px];
				this.ax = 0;
				for (this.n = 0; this.n < this.tempPresetFilesArr.Count; this.n++)
				{
					if (!this.tempPresetFilesArr[this.n].ToString().Contains(".meta"))
					{
						this.tempPresetFiles[this.ax] = this.tempPresetFilesArr[this.n].ToString();
						this.tempPresetFiles[this.ax] = this.tempPresetFiles[this.ax].Remove(0, this.pdir.Length);
						this.tempPresetFiles[this.ax] = this.tempPresetFiles[this.ax].Replace("SUIMONO_PRESET_", "");
						this.tempPresetFiles[this.ax] = this.tempPresetFiles[this.ax].Replace(".txt", "");
						if (this.tempPresetFiles[this.ax] == pName) setPreset = this.ax;
						this.ax++;
					}
				}
				setMode = setPreset;
			}

			return setMode;

#endif
		}






		public void PresetRename(int ppos, string newName)
		{
#if UNITY_EDITOR
#if !UNITY_WEBPLAYER
			pdir = dir + "/SUIMONO_PRESETS_"+presetDirs[presetFileIndex];
			oldName = pdir+"/SUIMONO_PRESET_"+presetFiles[ppos]+".txt";
			moveName = pdir+"/SUIMONO_PRESET_"+newName+".txt";
			File.Move(oldName,moveName);
			AssetDatabase.Refresh();
			PresetInit();
#endif
#endif
		}


		public void PresetAdd()
		{
#if UNITY_EDITOR
#if !UNITY_WEBPLAYER
			setNum = presetFiles.Length;
			pdir = dir + "/SUIMONO_PRESETS_"+presetDirs[presetFileIndex];
			while (File.Exists(pdir+"/SUIMONO_PRESET_New Preset "+setNum+".txt")){
				setNum += 1;
			}
			if (presetFiles.Length >= 1){
				File.Create(pdir+"/SUIMONO_PRESET_New Preset "+setNum+".txt").Close();
			} else {
				File.Create(pdir+"/SUIMONO_PRESET_New Preset 1.txt").Close();
				setNum = 1;
			}
			AssetDatabase.Refresh();
			PresetInit();
			SuimonoSavePreset(presetDirs[presetFileIndex],"New Preset "+setNum);
#endif
#endif
		}


		public void PresetDelete(int fpos, int ppos)
		{
#if UNITY_EDITOR
#if !UNITY_WEBPLAYER
			pdir = dir + "/SUIMONO_PRESETS_"+presetDirs[fpos];
			if (File.Exists(pdir+"/SUIMONO_PRESET_"+presetFiles[ppos]+".txt")){
				File.Delete(pdir+"/SUIMONO_PRESET_"+presetFiles[ppos]+".txt");
				if (presetIndex == ppos) presetIndex = -1;
			}
			AssetDatabase.Refresh();
			PresetInit();
#endif
#endif
		}


		public void PresetSave(int fpos, int ppos)
		{
#if UNITY_EDITOR
#if !UNITY_WEBPLAYER
			pdir = dir + "/SUIMONO_PRESETS_"+presetDirs[fpos];
			if (File.Exists(pdir+"/SUIMONO_PRESET_"+presetFiles[ppos]+".txt")){

				//Caclulate data
				presetDataString = "";
				presetDataString += (PresetEncode("color_depth")) + "\n";
				presetDataString += (PresetEncode("color_shallow")) + "\n";
				presetDataString += (PresetEncode("color_blend")) + "\n";	
				presetDataString += (PresetEncode("color_overlay")) + "\n";
				presetDataString += (PresetEncode("color_caustics")) + "\n";
				presetDataString += (PresetEncode("color_reflection")) + "\n";
				presetDataString += (PresetEncode("color_specular")) + "\n";
				presetDataString += (PresetEncode("color_sss")) + "\n";
				presetDataString += (PresetEncode("color_foam")) + "\n";
				presetDataString += (PresetEncode("color_underwater")) + "\n";
				presetDataString += (PresetEncode("data_beaufort")) + "\n";
				presetDataString += (PresetEncode("data_flowdir")) + "\n";
				presetDataString += (PresetEncode("data_flowspeed")) + "\n";
				presetDataString += (PresetEncode("data_wavescale")) + "\n";
				presetDataString += (PresetEncode("data_customwaves")) + "\n";
				presetDataString += (PresetEncode("data_waveheight")) + "\n";
				presetDataString += (PresetEncode("data_heightprojection")) + "\n";
				presetDataString += (PresetEncode("data_turbulence")) + "\n";
				presetDataString += (PresetEncode("data_lgwaveheight")) + "\n";
				presetDataString += (PresetEncode("data_lgwavescale")) + "\n";
				presetDataString += (PresetEncode("data_shorelineheight")) + "\n";
				presetDataString += (PresetEncode("data_shorelinefreq")) + "\n";
				presetDataString += (PresetEncode("data_shorelinescale")) + "\n";
				presetDataString += (PresetEncode("data_shorelinespeed")) + "\n";
				presetDataString += (PresetEncode("data_shorelinenorm")) + "\n";
				presetDataString += (PresetEncode("data_overallbright")) + "\n";
				presetDataString += (PresetEncode("data_overalltransparency")) + "\n";
				presetDataString += (PresetEncode("data_edgeamt")) + "\n";
				presetDataString += (PresetEncode("data_depthamt")) + "\n";
				presetDataString += (PresetEncode("data_shallowamt")) + "\n";
				presetDataString += (PresetEncode("data_refractstrength")) + "\n";
				presetDataString += (PresetEncode("data_aberrationscale")) + "\n";
				presetDataString += (PresetEncode("data_causticsfade")) + "\n";
				presetDataString += (PresetEncode("data_reflectprojection")) + "\n";
				presetDataString += (PresetEncode("data_reflectblur")) + "\n";
				presetDataString += (PresetEncode("data_reflectterm")) + "\n";
				presetDataString += (PresetEncode("data_reflectsharpen")) + "\n";
				presetDataString += (PresetEncode("data_roughness")) + "\n";
				presetDataString += (PresetEncode("data_roughness2")) + "\n";
				presetDataString += (PresetEncode("data_enablefoam")) + "\n";
				presetDataString += (PresetEncode("data_foamscale")) + "\n";
				presetDataString += (PresetEncode("data_foamspeed")) + "\n";
				presetDataString += (PresetEncode("data_edgefoamamt")) + "\n";
				presetDataString += (PresetEncode("data_shallowfoamamt")) + "\n";
				presetDataString += (PresetEncode("data_heightfoamamt")) + "\n";
				presetDataString += (PresetEncode("data_hfoamheight")) + "\n";
				presetDataString += (PresetEncode("data_hfoamspread")) + "\n";
				presetDataString += (PresetEncode("data_enableunderdebris")) + "\n";
				presetDataString += (PresetEncode("data_underlightfactor")) + "\n";
				presetDataString += (PresetEncode("data_underrefractionamount")) + "\n";
				presetDataString += (PresetEncode("data_underrefractionscale")) + "\n";
				presetDataString += (PresetEncode("data_underrefractionspeed")) + "\n";
				presetDataString += (PresetEncode("data_underbluramount")) + "\n";
				presetDataString += (PresetEncode("data_underwaterfogdist")) + "\n";
				presetDataString += (PresetEncode("data_underwaterfogspread")) + "\n";
				presetDataString += (PresetEncode("data_underDarkRange")) + "\n";

				//save data
				sw = new StreamWriter(pdir+"/SUIMONO_PRESET_"+presetFiles[ppos]+".txt");
				sw.AutoFlush = true;
				sw.Write(presetDataString);
			    sw.Close();

				Debug.Log("Preset '"+presetFiles[ppos]+"' has been saved!");
			}
#endif
#endif
		}




		public void PresetLoad(int ppos)
		{
			if (this.presetIndex >= 0)
			{
				this.pdir = this.dir + "/SUIMONO_PRESETS_" + this.presetDirs[this.presetFileIndex];
				this.sr = new StreamReader(this.pdir + "/SUIMONO_PRESET_" + this.presetFiles[ppos] + ".txt");
				this.presetDataString = this.sr.ReadToEnd();
				this.sr.Close();

				this.presetDataArray = this.presetDataString.Split("\n"[0]);

				//Decode Data
				for (this.dx = 0; this.dx < this.presetDataArray.Length; this.dx++)
				{
					if (this.presetDataArray[this.dx] != "" && this.presetDataArray[this.dx] != "\n")
					{
						this.pFrom = this.presetDataArray[this.dx].IndexOf("<") + "<".Length;
						this.pTo = this.presetDataArray[this.dx].LastIndexOf(">");
						this.key = this.presetDataArray[this.dx].Substring(this.pFrom, this.pTo - this.pFrom);

						this.pFrom = this.presetDataArray[this.dx].IndexOf("(") + "(".Length;
						this.pTo = this.presetDataArray[this.dx].LastIndexOf(")");
						this.dat = this.presetDataArray[this.dx].Substring(this.pFrom, this.pTo - this.pFrom);

						this.SetTemporaryPresetData();
						this.PresetDecode(this.key, this.dat);
					}
				}
			}
		}

		private void PresetLoadBuild(string fName, string pName)
		{
#if !UNITY_WEBPLAYER
			this.datFile = HSSuimono.HSSuimono._self._resources.LoadAsset<TextAsset>("SUIMONO_PRESET_" + pName);
			this.presetDataString = this.datFile.text;
			this.presetDataArray = this.presetDataString.Split("\n"[0]);

			//Decode Data
			for (this.dx = 0; this.dx < this.presetDataArray.Length; this.dx++)
			{
				if (this.presetDataArray[this.dx] != "" && this.presetDataArray[this.dx] != "\n")
				{
					this.pFrom = this.presetDataArray[this.dx].IndexOf("<") + "<".Length;
					this.pTo = this.presetDataArray[this.dx].LastIndexOf(">");
					this.key = this.presetDataArray[this.dx].Substring(this.pFrom, this.pTo - this.pFrom);

					this.pFrom = this.presetDataArray[this.dx].IndexOf("(") + "(".Length;
					this.pTo = this.presetDataArray[this.dx].LastIndexOf(")");
					this.dat = this.presetDataArray[this.dx].Substring(this.pFrom, this.pTo - this.pFrom);

					this.PresetDecode(this.key, this.dat);
				}
			}
#endif
		}

		private void PresetDecode(string key, string dat)
		{
			this.dataS = dat.Split(","[0]);

			if (this.presetTimer > 1f)
				this.presetTimer = 1f;

			if (key == "color_depth")
				this.depthColor = Color.Lerp(this.temp_depthColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_shallow")
				this.shallowColor = Color.Lerp(this.temp_shallowColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_blend")
				this.blendColor = Color.Lerp(this.temp_blendColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_overlay")
				this.overlayColor = Color.Lerp(this.temp_overlayColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_caustics")
				this.causticsColor = Color.Lerp(this.temp_causticsColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_reflection")
				this.reflectionColor = Color.Lerp(this.temp_reflectionColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_specular")
				this.specularColor = Color.Lerp(this.temp_specularColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_sss")
				this.sssColor = Color.Lerp(this.temp_sssColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_foam")
				this.foamColor = Color.Lerp(this.temp_foamColor, this.DecodeColor(this.dataS), this.presetTimer);
			if (key == "color_underwater")
				this.underwaterColor = Color.Lerp(this.temp_underwaterColor, this.DecodeColor(this.dataS), this.presetTimer);

			if (key == "data_beaufort")
				this.beaufortScale = Mathf.Lerp(this.temp_beaufortScale, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_flowdir")
				this.flowDirection = Mathf.Lerp(this.temp_flowDirection, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_flowspeed")
				this.flowSpeed = Mathf.Lerp(this.temp_flowSpeed, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_wavescale")
				this.waveScale = Mathf.Lerp(this.temp_waveScale, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_waveheight")
				this.waveHeight = Mathf.Lerp(this.temp_waveHeight, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_heightprojection")
				this.heightProjection = Mathf.Lerp(this.temp_heightProjection, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_turbulence")
				this.turbulenceFactor = Mathf.Lerp(this.temp_turbulenceFactor, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_lgwaveheight")
				this.lgWaveHeight = Mathf.Lerp(this.temp_lgWaveHeight, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_lgwavescale")
				this.lgWaveScale = Mathf.Lerp(this.temp_lgWaveScale, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_shorelineheight")
				this.shorelineHeight = Mathf.Lerp(this.temp_shorelineHeight, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_shorelinefreq")
				this.shorelineFreq = Mathf.Lerp(this.temp_shorelineFreq, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_shorelinescale")
				this.shorelineScale = Mathf.Lerp(this.temp_shorelineScale, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_shorelinespeed")
				this.shorelineSpeed = Mathf.Lerp(this.temp_shorelineSpeed, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_shorelinenorm")
				this.shorelineNorm = Mathf.Lerp(this.temp_shorelineNorm, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_overallbright")
				this.overallBright = Mathf.Lerp(this.temp_overallBright, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_overalltransparency")
				this.overallTransparency = Mathf.Lerp(this.temp_overallTransparency, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_edgeamt")
				this.edgeAmt = Mathf.Lerp(this.temp_edgeAmt, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_depthamt")
				this.depthAmt = Mathf.Lerp(this.temp_depthAmt, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_shallowamt")
				this.shallowAmt = Mathf.Lerp(this.temp_shallowAmt, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_refractstrength")
				this.refractStrength = Mathf.Lerp(this.temp_refractStrength, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_aberrationscale")
				this.aberrationScale = Mathf.Lerp(this.temp_aberrationScale, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_causticsfade")
				this.causticsFade = Mathf.Lerp(this.temp_causticsFade, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_reflectprojection")
				this.reflectProjection = Mathf.Lerp(this.temp_reflectProjection, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_reflectblur")
				this.reflectBlur = Mathf.Lerp(this.temp_reflectBlur, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_reflectterm")
				this.reflectTerm = Mathf.Lerp(this.temp_reflectTerm, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_reflectsharpen")
				this.reflectSharpen = Mathf.Lerp(this.temp_reflectSharpen, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_roughness")
				this.roughness = Mathf.Lerp(this.temp_roughness, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_roughness2")
				this.roughness2 = Mathf.Lerp(this.temp_roughness2, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_foamscale")
				this.foamScale = Mathf.Lerp(this.temp_foamScale, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_foamspeed")
				this.foamSpeed = Mathf.Lerp(this.temp_foamSpeed, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_edgefoamamt")
				this.edgeFoamAmt = Mathf.Lerp(this.temp_edgeFoamAmt, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_shallowfoamamt")
				this.shallowFoamAmt = Mathf.Lerp(this.temp_shallowFoamAmt, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_heightfoamamt")
				this.heightFoamAmt = Mathf.Lerp(this.temp_heightFoamAmt, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_hfoamheight")
				this.hFoamHeight = Mathf.Lerp(this.temp_hFoamHeight, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_hfoamspread")
				this.hFoamSpread = Mathf.Lerp(this.temp_hFoamSpread, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_underlightfactor")
				this.underLightFactor = Mathf.Lerp(this.temp_underLightFactor, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_underrefractionamount")
				this.underRefractionAmount = Mathf.Lerp(this.temp_underRefractionAmount, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_underrefractionscale")
				this.underRefractionScale = Mathf.Lerp(this.temp_underRefractionScale, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_underrefractionspeed")
				this.underRefractionSpeed = Mathf.Lerp(this.temp_underRefractionSpeed, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_underbluramount")
				this.underBlurAmount = Mathf.Lerp(this.temp_underBlurAmount, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_underwaterfogdist")
				this.underwaterFogDist = Mathf.Lerp(this.temp_underwaterFogDist, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_underwaterfogspread")
				this.underwaterFogSpread = Mathf.Lerp(this.temp_underwaterFogSpread, this.DecodeFloat(this.dataS), this.presetTimer);
			if (key == "data_underDarkRange")
				this.underDarkRange = Mathf.Lerp(this.temp_underDarkRange, this.DecodeFloat(this.dataS), this.presetTimer);

			if (key == "data_customwaves")
				this.customWaves = this.DecodeBool(this.dataS);
			if (key == "data_enablefoam")
				this.enableFoam = this.DecodeBool(this.dataS);
			if (key == "data_enableunderdebris")
				this.enableUnderDebris = this.DecodeBool(this.dataS);
		}


		public Color DecodeColor(string[] data)
		{
			return new Color(this.DecodeSingleFloat(data[0]), this.DecodeSingleFloat(data[1]), this.DecodeSingleFloat(data[2]), this.DecodeSingleFloat(data[3]));
		}
		public float DecodeFloat(string[] data)
		{
			return this.DecodeSingleFloat(data[0]);
		}

		public int DecodeInt(string[] data)
		{
			return int.Parse(data[0]);
		}
		public bool DecodeBool(string[] data)
		{
			this.retVal = false;
			if (data[0] == "True")
				this.retVal = true;
			return this.retVal;
		}

		public float DecodeSingleFloat(string data)
		{
			return float.Parse(data,
					  System.Globalization.NumberStyles.Float,
					  System.Globalization.CultureInfo.InvariantCulture);
		}







		public string PresetEncode(string key)
		{
			this.retData = "";

			if (key == "color_depth")
				this.retData = this.EncodeColor(this.depthColor);
			if (key == "color_shallow")
				this.retData = this.EncodeColor(this.shallowColor);
			if (key == "color_blend")
				this.retData = this.EncodeColor(this.blendColor);
			if (key == "color_overlay")
				this.retData = this.EncodeColor(this.overlayColor);
			if (key == "color_caustics")
				this.retData = this.EncodeColor(this.causticsColor);
			if (key == "color_reflection")
				this.retData = this.EncodeColor(this.reflectionColor);
			if (key == "color_specular")
				this.retData = this.EncodeColor(this.specularColor);
			if (key == "color_sss")
				this.retData = this.EncodeColor(this.sssColor);
			if (key == "color_foam")
				this.retData = this.EncodeColor(this.foamColor);
			if (key == "color_underwater")
				this.retData = this.EncodeColor(this.underwaterColor);
			if (key == "data_customwaves")
				this.retData = "(" + this.customWaves.ToString().Replace(" ", "") + ")";
			if (key == "data_enableunderdebris")
				this.retData = "(" + this.enableUnderDebris.ToString().Replace(" ", "") + ")";
			if (key == "data_enablefoam")
				this.retData = "(" + this.enableFoam.ToString().Replace(" ", "") + ")";
			if (key == "data_beaufort")
				this.retData = "(" + this.EncodeSingleFloat(this.beaufortScale) + ")";
			if (key == "data_flowdir")
				this.retData = "(" + this.EncodeSingleFloat(this.flowDirection) + ")";
			if (key == "data_flowspeed")
				this.retData = "(" + this.EncodeSingleFloat(this.flowSpeed) + ")";
			if (key == "data_wavescale")
				this.retData = "(" + this.EncodeSingleFloat(this.waveScale) + ")";
			if (key == "data_waveheight")
				this.retData = "(" + this.EncodeSingleFloat(this.waveHeight) + ")";
			if (key == "data_heightprojection")
				this.retData = "(" + this.EncodeSingleFloat(this.heightProjection) + ")";
			if (key == "data_turbulence")
				this.retData = "(" + this.EncodeSingleFloat(this.turbulenceFactor) + ")";
			if (key == "data_lgwaveheight")
				this.retData = "(" + this.EncodeSingleFloat(this.lgWaveHeight) + ")";
			if (key == "data_lgwavescale")
				this.retData = "(" + this.EncodeSingleFloat(this.lgWaveScale) + ")";
			if (key == "data_shorelineheight")
				this.retData = "(" + this.EncodeSingleFloat(this.shorelineHeight) + ")";
			if (key == "data_shorelinefreq")
				this.retData = "(" + this.EncodeSingleFloat(this.shorelineFreq) + ")";
			if (key == "data_shorelinescale")
				this.retData = "(" + this.EncodeSingleFloat(this.shorelineScale) + ")";
			if (key == "data_shorelinespeed")
				this.retData = "(" + this.EncodeSingleFloat(this.shorelineSpeed) + ")";
			if (key == "data_shorelinenorm")
				this.retData = "(" + this.EncodeSingleFloat(this.shorelineNorm) + ")";
			if (key == "data_overallbright")
				this.retData = "(" + this.EncodeSingleFloat(this.overallBright) + ")";
			if (key == "data_overalltransparency")
				this.retData = "(" + this.EncodeSingleFloat(this.overallTransparency) + ")";
			if (key == "data_edgeamt")
				this.retData = "(" + this.EncodeSingleFloat(this.edgeAmt) + ")";
			if (key == "data_depthamt")
				this.retData = "(" + this.EncodeSingleFloat(this.depthAmt) + ")";
			if (key == "data_shallowamt")
				this.retData = "(" + this.EncodeSingleFloat(this.shallowAmt) + ")";
			if (key == "data_refractstrength")
				this.retData = "(" + this.EncodeSingleFloat(this.refractStrength) + ")";
			if (key == "data_aberrationscale")
				this.retData = "(" + this.EncodeSingleFloat(this.aberrationScale) + ")";
			if (key == "data_causticsfade")
				this.retData = "(" + this.EncodeSingleFloat(this.causticsFade) + ")";
			if (key == "data_reflectprojection")
				this.retData = "(" + this.EncodeSingleFloat(this.reflectProjection) + ")";
			if (key == "data_reflectblur")
				this.retData = "(" + this.EncodeSingleFloat(this.reflectBlur) + ")";
			if (key == "data_reflectterm")
				this.retData = "(" + this.EncodeSingleFloat(this.reflectTerm) + ")";
			if (key == "data_reflectsharpen")
				this.retData = "(" + this.EncodeSingleFloat(this.reflectSharpen) + ")";
			if (key == "data_roughness")
				this.retData = "(" + this.EncodeSingleFloat(this.roughness) + ")";
			if (key == "data_roughness2")
				this.retData = "(" + this.EncodeSingleFloat(this.roughness2) + ")";
			if (key == "data_foamscale")
				this.retData = "(" + this.EncodeSingleFloat(this.foamScale) + ")";
			if (key == "data_foamspeed")
				this.retData = "(" + this.EncodeSingleFloat(this.foamSpeed) + ")";
			if (key == "data_edgefoamamt")
				this.retData = "(" + this.EncodeSingleFloat(this.edgeFoamAmt) + ")";
			if (key == "data_shallowfoamamt")
				this.retData = "(" + this.EncodeSingleFloat(this.shallowFoamAmt) + ")";
			if (key == "data_heightfoamamt")
				this.retData = "(" + this.EncodeSingleFloat(this.heightFoamAmt) + ")";
			if (key == "data_hfoamheight")
				this.retData = "(" + this.EncodeSingleFloat(this.hFoamHeight) + ")";
			if (key == "data_hfoamspread")
				this.retData = "(" + this.EncodeSingleFloat(this.hFoamSpread) + ")";
			if (key == "data_underlightfactor")
				this.retData = "(" + this.EncodeSingleFloat(this.underLightFactor) + ")";
			if (key == "data_underrefractionamount")
				this.retData = "(" + this.EncodeSingleFloat(this.underRefractionAmount) + ")";
			if (key == "data_underrefractionscale")
				this.retData = "(" + this.EncodeSingleFloat(this.underRefractionScale) + ")";
			if (key == "data_underrefractionspeed")
				this.retData = "(" + this.EncodeSingleFloat(this.underRefractionSpeed) + ")";
			if (key == "data_underbluramount")
				this.retData = "(" + this.EncodeSingleFloat(this.underBlurAmount) + ")";
			if (key == "data_underwaterfogdist")
				this.retData = "(" + this.EncodeSingleFloat(this.underwaterFogDist) + ")";
			if (key == "data_underwaterfogspread")
				this.retData = "(" + this.EncodeSingleFloat(this.underwaterFogSpread) + ")";
			if (key == "data_underDarkRange")
				this.retData = "(" + this.EncodeSingleFloat(this.underDarkRange) + ")";

			this.retData = "<" + key + ">" + this.retData;
			return this.retData;
		}


		public string EncodeSingleFloat(float data)
		{
			return data.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}

		public string EncodeColor(Color data)
		{
			return string.Format("({0},{1},{2},{3})", this.EncodeSingleFloat(data.r), this.EncodeSingleFloat(data.g), this.EncodeSingleFloat(data.b), this.EncodeSingleFloat(data.a));
		}




		//PRESET TRANSITION FUNCTION OPTIONS:

		//Option 1: (string)preset name, (float)transition time in seconds
		//it is assumed that preset is in the "Built-In Presets" folder.
		public void SuimonoTransitionPreset(string pName, float transitionTime)
		{
			this.presetTimer = 0f;
			this.presetTransitionTime = transitionTime;
			this.presetStartTransition = true;
			this.currentPresetFolder = "Built-In Presets";
			this.currentPresetName = pName;
			this.SetTemporaryPresetData();
		}
		//Option 2: (string)preset folder name, (string)preset name, (float)transition time in seconds
		//For accessing presets in a custom folder name.
		public void SuimonoTransitionPreset(string fName, string pName, float transitionTime)
		{
			this.presetTimer = 0f;
			this.presetTransitionTime = transitionTime;
			this.presetStartTransition = true;
			this.currentPresetFolder = fName;
			this.currentPresetName = pName;
			this.SetTemporaryPresetData();
		}
		//Option 3: (string)preset folder name, (string)from preset name, (string)to preset name, (float)transition time in seconds
		//For transitioning directly from one preset to another, when presets are in same folder.
		public void SuimonoTransitionPreset(string fName, string pName0, string pName1, float transitionTime)
		{
			this.SuimonoSetPreset(fName, pName0);
			this.presetTimer = 0f;
			this.presetTransitionTime = transitionTime;
			this.presetStartTransition = true;
			this.currentPresetFolder = fName;
			this.currentPresetName = pName1;
			this.SetTemporaryPresetData();
		}
		//Option 4: (string)from preset folder name, (string)from preset name, (string)to preset folder name, (string)to preset name, (float)transition time in seconds
		//For transitioning directly from one preset to another, when presets are in different folders.
		public void SuimonoTransitionPreset(string fName0, string pName0, string fName1, string pName1, float transitionTime)
		{
			this.SuimonoSetPreset(fName0, pName0);
			this.presetTimer = 0f;
			this.presetTransitionTime = transitionTime;
			this.presetStartTransition = true;
			this.currentPresetFolder = fName1;
			this.currentPresetName = pName1;
			this.SetTemporaryPresetData();
		}



		//Caches current data for usage in lerping preset transitions
		private void SetTemporaryPresetData()
		{
			this.temp_depthColor = this.depthColor;
			this.temp_shallowColor = this.shallowColor;
			this.temp_blendColor = this.blendColor;
			this.temp_overlayColor = this.overlayColor;
			this.temp_causticsColor = this.causticsColor;
			this.temp_reflectionColor = this.reflectionColor;
			this.temp_specularColor = this.specularColor;
			this.temp_sssColor = this.sssColor;
			this.temp_foamColor = this.foamColor;
			this.temp_underwaterColor = this.underwaterColor;

			this.temp_beaufortScale = this.beaufortScale;
			this.temp_flowDirection = this.flowDirection;
			this.temp_flowSpeed = this.flowSpeed;
			this.temp_waveScale = this.waveScale;
			this.temp_waveHeight = this.waveHeight;
			this.temp_heightProjection = this.heightProjection;
			this.temp_turbulenceFactor = this.turbulenceFactor;
			this.temp_lgWaveHeight = this.lgWaveHeight;
			this.temp_lgWaveScale = this.lgWaveScale;
			this.temp_shorelineHeight = this.shorelineHeight;
			this.temp_shorelineFreq = this.shorelineFreq;
			this.temp_shorelineScale = this.shorelineScale;
			this.temp_shorelineSpeed = this.shorelineSpeed;
			this.temp_shorelineNorm = this.shorelineNorm;
			this.temp_overallBright = this.overallBright;
			this.temp_overallTransparency = this.overallTransparency;
			this.temp_edgeAmt = this.edgeAmt;
			this.temp_depthAmt = this.depthAmt;
			this.temp_shallowAmt = this.shallowAmt;
			this.temp_refractStrength = this.refractStrength;
			this.temp_aberrationScale = this.aberrationScale;
			this.temp_causticsFade = this.causticsFade;
			this.temp_reflectProjection = this.reflectProjection;
			this.temp_reflectBlur = this.reflectBlur;
			this.temp_reflectTerm = this.reflectTerm;
			this.temp_reflectSharpen = this.reflectSharpen;
			this.temp_roughness = this.roughness;
			this.temp_roughness2 = this.roughness2;
			this.temp_foamScale = this.foamScale;
			this.temp_foamSpeed = this.foamSpeed;
			this.temp_edgeFoamAmt = this.edgeFoamAmt;
			this.temp_shallowFoamAmt = this.shallowFoamAmt;
			this.temp_heightFoamAmt = this.heightFoamAmt;
			this.temp_hFoamHeight = this.hFoamHeight;
			this.temp_hFoamSpread = this.hFoamSpread;
			this.temp_underLightFactor = this.underLightFactor;
			this.temp_underRefractionAmount = this.underRefractionAmount;
			this.temp_underRefractionScale = this.underRefractionScale;
			this.temp_underRefractionSpeed = this.underRefractionSpeed;
			this.temp_underBlurAmount = this.underBlurAmount;
			this.temp_underwaterFogDist = this.underwaterFogDist;
			this.temp_underwaterFogSpread = this.underwaterFogSpread;
			this.temp_underDarkRange = this.underDarkRange;
		}




	}
}