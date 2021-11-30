using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
	using UnityEditor;
#endif

namespace Suimono.Core
{
	[ExecuteInEditMode]
	public class SuimonoModule : MonoBehaviour
	{

		//Underwater Effects variables
		public string suimonoVersionNumber = "";
		public float systemTime = 0f;

		//layers
		public bool autoSetLayers = true;
		public string layerWater;
		public int layerWaterNum = -1;
		public string layerDepth;
		public int layerDepthNum = -1;
		public string layerScreenFX;
		public int layerScreenFXNum = -1;
		//public string layerUnderwater;

		public bool layersAreSet = false;

#if UNITY_EDITOR
			public SerializedObject tagManager;
			public SerializedProperty projectlayers;
#endif

		public bool autoSetCameraFX = true;
		public Transform manualCamera;
		public Transform mainCamera;
		public int cameraTypeIndex = 0;
		[System.NonSerialized] public string[] cameraTypeOptions = { "Auto Select Camera", "Manual Select Camera" };
		public Transform setCamera;
		public Transform setTrack;
		public Light setLight;

		public bool enableUnderwaterFX = true;
		public bool enableInteraction = true;
		public float objectEnableUnderwaterFX = 1f;

		public bool disableMSAA = false;

		public bool enableRefraction = true;
		public bool enableReflections = true;
		public bool enableDynamicReflections = true;
		public bool enableCaustics = true;
		public bool enableCausticsBlending = false;
		public bool enableAdvancedEdge = true;
		public bool enableAdvancedDistort = true;
		public bool enableTenkoku = false;
		public bool enableAutoAdvance = true;
		public bool showPerformance = false;
		public bool showGeneral = false;

		public Color underwaterColor = new Color(0.58f, 0.61f, 0.61f, 0.0f);
		public bool enableTransition = true;
		public float transition_offset = 0.1f;
		public GameObject fxRippleObject;

		//private ParticleSystem underwaterDebris;
		private float underLightAmt = 0f;

		private GameObject targetSurface;
		private float doTransitionTimer = 0f;

		public bool isUnderwater = false;
		private static bool doWaterTransition = false;


		//transparency
		public bool enableTransparency = true;
		private bool useEnableTransparency = true;
		public int transResolution = 3;
		public int transLayer = 0;
		public LayerMask transLayerMask;
		public int causticLayer = 0;
		public LayerMask causticLayerMask;
		[System.NonSerialized]
		public List<string> suiLayerMasks;
		[System.NonSerialized]
		public string[] resOptions = { "4096", "2048", "1024", "512", "256", "128", "64", "32", "16", "8" };
		[System.NonSerialized]
		public List<int> resolutions = new List<int>() { 4096, 2048, 1024, 512, 256, 128, 64, 32, 16, 8 };

		public float transRenderDistance = 100f;
		//private Suimono.Core.cameraTools transToolsObject;
		//private Camera transCamObject;
		//private Suimono.Core.cameraTools causticToolsObject;
		//private Suimono.Core.cameraCausticsHandler causticHandlerObjectTrans;
		//private Suimono.Core.cameraCausticsHandler causticHandlerObject;
		//private Camera causticCamObject;
		//private GameObject wakeObject;
		//private Camera wakeCamObject;
		//private GameObject normalsObject;
		//private Camera normalsCamObject;

		public bool playSounds = true;
		public bool playSoundBelow = true;
		public bool playSoundAbove = true;
		public float maxVolume = 1f;
		public int maxSounds = 10;
		public AudioClip[] defaultSplashSound;

		//public Suimono.Core.SuimonoModuleFX fxObject;

		private float setvolume = 0.65f;

		//private Suimono.Core.fx_soundModule sndparentobj;
		private GameObject underSoundObject;
		private AudioSource underSoundComponent;
		private AudioSource[] sndComponents;
		private int currentSound = 0;

		public float currentObjectIsOver = 0f;
		public float currentObjectDepth = 0f;
		public float currentTransitionDepth = 0f;
		public float currentSurfaceLevel = 0f;
		public float underwaterThreshold = 0.1f;
		public SuimonoObject suimonoObject;


		private ParticleSystem.Particle[] effectBubbles;
		public SuimonoModuleLib suimonoModuleLibrary;

		//private Renderer underwaterDebrisRendererComponent;
		//private Transform underwaterDebrisTransform;
		public Camera setCameraComponent;
		private float underTrans = 0f;

		//tenkoku specific variables
		public float useTenkoku = 0f;
		public float tenkokuWindDir = 0f;
		public float tenkokuWindAmt = 0f;
		public bool tenkokuUseWind = true;
		private GameObject tenObject;
		public bool showTenkoku = true;
		public bool tenkokuUseReflect = true;
		private WindZone tenkokuWindModule;

		//collect for GC
		private int lx;
		private int fx;
		private int px;
		private ParticleSystem.Particle[] setParticles;
		private AudioClip setstep;
		private float setpitch;
		//private float waitTime;
		private AudioSource useSoundAudioComponent;
		private float useRefract;
		private float useLight = 1f;
		private Color useLightCol;
		private Vector2 flow_dir;
		private Vector3 tempAngle;
		//private Color getmap;
		private float getheight;
		private float getheightC;
		private float getheightT;
		private float getheightR;
		private bool isOverWater;
		private float surfaceLevel;
		//private float groundLevel;
		private int layer;
		private int layermask;
		private Vector3 testpos;
		private int i;
		private RaycastHit hit;
		private Vector2 pixelUV;
		private float returnValue;
		private float[] returnValueAll;
		private float h1;
		private float setDegrees = 0f;
		private float enabledUFX = 1f;
		private float enabledCaustics = 1f;

		private float setUnderBright;
		//public Suimono.Core.fx_causticModule causticObject;
		private float enCaustic = 0f;
		private float setEdge = 1f;
		private Suimono_UnderwaterFog underwaterObject;
		private GameObject currentSurfaceObject = null;

		public float[] heightValues;
		//public Light causticObjectLight;
		public float isForward = 0f;
		public float isAdvDist = 0f;

		//Height Variables
		public float waveScale = 1f;
		public float flowSpeed = 0.02f;
		public float offset = 0f;
		public Texture2D heightTex;
		public Texture2D heightTexT;
		public Texture2D heightTexR;
		public Transform heightObject;
		public Vector2 relativePos = new Vector2(0f, 0f);
		public Vector3 texCoord = new Vector3(0f, 0f, 0f);
		public Vector3 texCoord1 = new Vector3(0f, 0f, 0f);
		public Vector3 texCoordT = new Vector3(0f, 0f, 0f);
		public Vector3 texCoordT1 = new Vector3(0f, 0f, 0f);
		public Vector3 texCoordR = new Vector3(0f, 0f, 0f);
		public Vector3 texCoordR1 = new Vector3(0f, 0f, 0f);
		public Color heightVal0;
		public Color heightVal1;
		public Color heightValT0;
		public Color heightValT1;
		public Color heightValR0;
		public Color heightValR1;
		public float localTime = 0f;
		private float baseHeight = 0f;
		private float baseAngle = 0f;
		private Color[] pixelArray;
		private Color[] pixelArrayT;
		private Color[] pixelArrayR;

		private Texture2D useDecodeTex;
		private Color[] useDecodeArray;
		public int row;
		public int pixIndex;
		public Color pixCol;

		public int t;
		public int y;
#if UNITY_EDITOR
			public SerializedProperty layerTP;
			public SerializedProperty layerWP;
			public SerializedProperty layerSP;
			public SerializedProperty layerXP;
			public SerializedProperty layerN;
#endif

		public Vector3 dir;
		public Vector3 pivotPoint;
		public float useLocalTime;
		public Vector2 flow_dirC;
		public Vector2 flowSpeed0;
		public Vector2 flowSpeed1;
		public Vector2 flowSpeed2;
		public Vector2 flowSpeed3;
		public float tScale;
		public Vector2 oPos;

		private int renderCount = 0;
		private int randSeed;
		private System.Random modRand;

		//private float surfaceTimer = 0f;
		//private Suimono.Core.SuimonoObject[] sObjects;
		[NonSerialized]
		public List<SuimonoObject> sObjects;
		private List<Renderer> sRends;
		private List<Renderer> sRendSCs;



		//Variables for Unity 5.4+ only
#if UNITY_5_4_OR_NEWER
			private ParticleSystem.EmissionModule debrisEmission;
#endif

		private ColorSpace _colorspace;
		private float _deltaTime;

		private void Awake()
		{

			//###  SET CURRENT SUIMONO NUMBER   ###
			this.suimonoVersionNumber = "2.1.6";

			//Force name
			this.gameObject.name = "SUIMONO_Module";

			//Initialize Lists
			this.sObjects = new List<SuimonoObject>();
			this.sRends = new List<Renderer>();
			this.sRendSCs = new List<Renderer>();
		}

		private void Start()
		{

			//### DISCONNECT FROM PREFAB ###;
#if UNITY_EDITOR
				PrefabUtility.DisconnectPrefabInstance(this.gameObject);
#endif

			//set random
			this.randSeed = System.Environment.TickCount;
			this.modRand = new System.Random(this.randSeed);

			//### SET LAYERS ###;
			this.InitLayers();

			//Set Camera and Track Objects
			this.Suimono_CheckCamera();

			//SET PHYSICS LAYER INTERACTIONS
			//This is introduced because Unity 5 no longer handles mesh colliders and triggers without throwing an error.
			//thanks a whole lot guys O_o (for nuthin').  The below physics setup should workaround this problem for everyone.
			for (this.lx = 0; this.lx < 32; this.lx++)
			{
				//loop through and decouple layer collisions for all layers(up to 20).
				//layer 4 is the built-in water layer.
				Physics.IgnoreLayerCollision(this.lx, this.layerWaterNum);
			}

			//INITIATE OBJECTS
			this.suimonoModuleLibrary = this.gameObject.GetComponent<SuimonoModuleLib>() as SuimonoModuleLib;
			/*
		    if (GameObject.Find("_caustic_effects") != null) causticObject = GameObject.Find("_caustic_effects").GetComponent<Suimono.Core.fx_causticModule>();
			if (causticObject != null) causticObjectLight = GameObject.Find("mainCausticObject").GetComponent<Light>();

			//transparency objects
			transToolsObject = this.transform.Find("cam_SuimonoTrans").gameObject.GetComponent<Suimono.Core.cameraTools>();
			transCamObject = this.transform.Find("cam_SuimonoTrans").gameObject.GetComponent<Camera>() as Camera;
			causticHandlerObjectTrans = this.transform.Find("cam_SuimonoTrans").gameObject.GetComponent<cameraCausticsHandler>();

			causticToolsObject = this.transform.Find("cam_SuimonoCaustic").gameObject.GetComponent<Suimono.Core.cameraTools>();
			causticCamObject = this.transform.Find("cam_SuimonoCaustic").gameObject.GetComponent<Camera>() as Camera;
			causticHandlerObject = this.transform.Find("cam_SuimonoCaustic").gameObject.GetComponent<Suimono.Core.cameraCausticsHandler>();

			//wake advanced effect objects
			wakeObject = this.transform.Find("cam_SuimonoWake").gameObject;
			wakeCamObject = this.transform.Find("cam_SuimonoWake").gameObject.GetComponent<Camera>() as Camera;
			normalsObject = this.transform.Find("cam_SuimonoNormals").gameObject;
			normalsCamObject = this.transform.Find("cam_SuimonoNormals").gameObject.GetComponent<Camera>() as Camera;

		    //Effects Initialization
		    fxObject = this.gameObject.GetComponent<Suimono.Core.SuimonoModuleFX>() as Suimono.Core.SuimonoModuleFX;
			if (GameObject.Find("_sound_effects") != null) sndparentobj = GameObject.Find("_sound_effects").gameObject.GetComponent<Suimono.Core.fx_soundModule>();
		    if (GameObject.Find("effect_underwater_debris") != null) underwaterDebris = GameObject.Find("effect_underwater_debris").gameObject.GetComponent<ParticleSystem>();
		    //if (GameObject.Find("effect_fx_bubbles") != null) effectBubbleSystem = GameObject.Find("effect_fx_bubbles").gameObject.GetComponent<ParticleSystem>();
		    */



			//get Surface objects
			//InitSurfaceRenderers();


#if UNITY_EDITOR
			if (EditorApplication.isPlaying){
#endif

			if (this.suimonoModuleLibrary.sndparentobj != null)
			{


				//init sound object pool
				this.maxSounds = this.suimonoModuleLibrary.sndparentobj.maxSounds;
				this.sndComponents = new AudioSource[this.maxSounds];
				GameObject[] soundObjectPrefab = new GameObject[this.maxSounds];

				for (int sx = 0; sx < (this.maxSounds); sx++)
				{
					soundObjectPrefab[sx] = new GameObject("SuimonoAudioObject");
					soundObjectPrefab[sx].transform.parent = this.suimonoModuleLibrary.sndparentobj.transform;
					soundObjectPrefab[sx].AddComponent<AudioSource>();
					this.sndComponents[sx] = soundObjectPrefab[sx].GetComponent<AudioSource>();

				}

				//init underwater sound
				if (this.suimonoModuleLibrary.sndparentobj.underwaterSound != null)
				{
					this.underSoundObject = new GameObject("Underwater Sound");
					this.underSoundObject.AddComponent<AudioSource>();
					this.underSoundObject.transform.parent = this.suimonoModuleLibrary.sndparentobj.transform;
					this.underSoundComponent = this.underSoundObject.GetComponent<AudioSource>();
				}
			}

#if UNITY_EDITOR
			}
#endif



			//tun off antialiasing (causes unexpected rendering issues.  Recommend post fx aliasing instead)
			if (this.disableMSAA)
			{
				QualitySettings.antiAliasing = 0;
			}

			//cache quality settings
			this._colorspace = QualitySettings.activeColorSpace;

			//set linear space flag
			Shader.SetGlobalFloat("_Suimono_isLinear", this._colorspace == ColorSpace.Linear ? 1.0f : 0.0f);

			//store pixel arrays for Height Calculation
			if (this.suimonoModuleLibrary != null)
			{

				if (this.suimonoModuleLibrary.texNormalC != null)
				{
					this.heightTex = this.suimonoModuleLibrary.texNormalC;
					this.pixelArray = this.suimonoModuleLibrary.texNormalC.GetPixels(0);
				}
				if (this.suimonoModuleLibrary.texNormalT != null)
				{
					this.heightTexT = this.suimonoModuleLibrary.texNormalT;
					this.pixelArrayT = this.suimonoModuleLibrary.texNormalT.GetPixels(0);
				}
				if (this.suimonoModuleLibrary.texNormalR != null)
				{
					this.heightTexR = this.suimonoModuleLibrary.texNormalR;
					this.pixelArrayR = this.suimonoModuleLibrary.texNormalR.GetPixels(0);
				}

			}

			//set tenkoku flag
			this.tenObject = GameObject.Find("Tenkoku DynamicSky");
			Shader.SetGlobalFloat("_useTenkoku", 0.0f);
		}

		private void InitLayers()
		{

			//check whether layers are set
#if UNITY_EDITOR
            this.tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            this.projectlayers = this.tagManager.FindProperty("layers");
            this.layersAreSet = false;
				
				for (this.t = 8; this.t <= 31; this.t++){
                    this.layerTP = this.projectlayers.GetArrayElementAtIndex(this.t);
			        if (this.layerTP.stringValue != ""){
			        	if (this.layerTP.stringValue == "Suimono_Water" || this.layerTP.stringValue == "Suimono_Depth" || this.layerTP.stringValue == "Suimono_Screen" || this.layerTP.stringValue == "Suimono_Underwater"){
                            this.layersAreSet = true;
			        	}
			        }
			    }

	    	    if (!this.autoSetLayers){
					//if (layerTP.stringValue == "Suimono_Water") layerWaterNum = t;
					//if (layerTP.stringValue == "Suimono_Depth") layerDepthNum = t;
					//if (layerTP.stringValue == "Suimono_Screen") layerScreenFXNum = t;

                    this.layerWaterNum = LayerMask.NameToLayer("Suimono_Water");
                    this.layerDepthNum = LayerMask.NameToLayer("Suimono_Depth");
                    this.layerScreenFXNum = LayerMask.NameToLayer("Suimono_Screen");

	    		}


#endif


			if (this.autoSetLayers)
			{

				//Set Layers if Applicable
				if (!this.layersAreSet)
				{
#if UNITY_EDITOR
					
			        if (this.projectlayers == null || !this.projectlayers.isArray){
			            Debug.LogWarning("Can't set up Suimono layers.  It's possible the format of the layers and tags data has changed in this version of Unity.");
			            Debug.LogWarning("Layers is null: " + (this.projectlayers == null));
			            return;
			        }

                    this.layerWater = "Suimono_Water";
                    this.layerDepth = "Suimono_Depth";
                    this.layerScreenFX = "Suimono_Screen";

					//ASSIGN LAYERS
                    this.layerWaterNum = -1;
                    this.layerDepthNum = -1;
                    this.layerScreenFXNum = -1;

					for (this.y = 8; this.y <= 31; this.y++){
                        this.layerWP = this.projectlayers.GetArrayElementAtIndex(this.y);
			            if (this.layerWP.stringValue != this.layerWater && this.layerWP.stringValue == "" && this.layerWaterNum == -1){
                            this.layerWaterNum = this.y;
			                if (!this.layersAreSet) Debug.Log("Setting up Suimono layers.  Layer " + this.layerWaterNum + " is now called " + this.layerWater);
                            this.layerWP.stringValue = this.layerWater;
			            }
                        this.layerSP = this.projectlayers.GetArrayElementAtIndex(this.y);
			            if (this.layerSP.stringValue != this.layerDepth && this.layerWP.stringValue == "" && this.layerDepthNum == -1){
                            this.layerDepthNum = this.y;
			                if (!this.layersAreSet) Debug.Log("Setting up Suimono layers.  Layer " + this.layerDepthNum + " is now called " + this.layerDepth);
                            this.layerSP.stringValue = this.layerDepth;
			            }
                        this.layerXP = this.projectlayers.GetArrayElementAtIndex(this.y);
			            if (this.layerXP.stringValue != this.layerScreenFX && this.layerWP.stringValue == "" && this.layerScreenFXNum == -1){
                            this.layerScreenFXNum = this.y;
			                if (!this.layersAreSet) Debug.Log("Setting up Suimono layers.  Layer " + this.layerScreenFXNum + " is now called " + this.layerScreenFX);
                            this.layerXP.stringValue = this.layerScreenFX;
			            }
			        }

			        if (!this.layersAreSet)
                        this.tagManager.ApplyModifiedProperties();

#endif
					this.layersAreSet = true;
				}
			}
		}






		/*
                void InitLayers_orig () {

                    //check whether layers are set
                    #if UNITY_EDITOR
                        tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                        projectlayers = tagManager.FindProperty("layers");
                        layersAreSet = false;

                        for (t = 8; t <= 31; t++){
                            layerTP = projectlayers.GetArrayElementAtIndex(t);
                            if (layerTP.stringValue != ""){
                                if (layerTP.stringValue == "Suimono_Water" || layerTP.stringValue == "Suimono_Depth" || layerTP.stringValue == "Suimono_Screen" || layerTP.stringValue == "Suimono_Underwater"){
                                    layersAreSet = true;
                                }
                            }

                            if (!autoSetLayers){
                                if (layerTP.stringValue == "Suimono_Water") layerWaterNum = t;
                                if (layerTP.stringValue == "Suimono_Depth") layerDepthNum = t;
                                if (layerTP.stringValue == "Suimono_Screen") layerScreenFXNum = t;
                            }

                        }
                    #endif


                    if (autoSetLayers){

                        //Set Layers if Applicable
                        if (!layersAreSet){
                        #if UNITY_EDITOR

                            if (projectlayers == null || !projectlayers.isArray){
                                Debug.LogWarning("Can't set up Suimono layers.  It's possible the format of the layers and tags data has changed in this version of Unity.");
                                Debug.LogWarning("Layers is null: " + (projectlayers == null));
                                return;
                            }

                            layerWater = "Suimono_Water";
                            layerDepth = "Suimono_Depth";
                            layerScreenFX = "Suimono_Screen";
                            layerUnderwater = "Suimono_Underwater";

                            //ASSIGN LAYERS
                            layerWaterNum = -1;
                            layerDepthNum = -1;
                            layerScreenFXNum = -1;

                            for (y = 8; y <= 31; y++){
                                layerWP = projectlayers.GetArrayElementAtIndex(y);
                                if (layerWP.stringValue != layerWater && layerWP.stringValue == "" && layerWaterNum == -1){
                                    layerWaterNum = y;
                                    if (!layersAreSet) Debug.Log("Setting up Suimono layers.  Layer " + layerWaterNum + " is now called " + layerWater);
                                    layerWP.stringValue = layerWater;
                                }
                                layerSP = projectlayers.GetArrayElementAtIndex(y);
                                if (layerSP.stringValue != layerDepth && layerWP.stringValue == "" && layerDepthNum == -1){
                                    layerDepthNum = y;
                                    if (!layersAreSet) Debug.Log("Setting up Suimono layers.  Layer " + layerDepthNum + " is now called " + layerDepth);
                                    layerSP.stringValue = layerDepth;
                                }
                                layerXP = projectlayers.GetArrayElementAtIndex(y);
                                if (layerXP.stringValue != layerScreenFX && layerWP.stringValue == "" && layerScreenFXNum == -1){
                                    layerScreenFXNum = y;
                                    if (!layersAreSet) Debug.Log("Setting up Suimono layers.  Layer " + layerScreenFXNum + " is now called " + layerScreenFX);
                                    layerXP.stringValue = layerScreenFX;
                                }
                            }

                            if (!layersAreSet) tagManager.ApplyModifiedProperties();

                        #endif
                        layersAreSet = true;
                        }
                    }
                }
        */

		private void LateUpdate()
		{

			//Cache Time for performance
			this._deltaTime = Time.deltaTime;


			//Update Random
			if (this.modRand == null)
				this.modRand = new System.Random(this.randSeed);

			//Set Water System Time
			if (this.systemTime < 0.0f)
				this.systemTime = 0.0f;
			if (this.enableAutoAdvance)
				this.systemTime += this._deltaTime;


			//set project layer masks
#if UNITY_EDITOR
            this.suiLayerMasks = new List<string>();
            this.tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            this.projectlayers = this.tagManager.FindProperty("layers");
				for (this.i = 0; this.i < this.projectlayers.arraySize; this.i++){
                    this.layerN = this.projectlayers.GetArrayElementAtIndex(this.i);
                    this.suiLayerMasks.Add(this.layerN.stringValue);
				}
#endif

			//CHECK FOR SURFACE OBJECT CULLING
			this.SetCullFunction();

			//GET TENKOKU SPECIFIC VARIABLES
			this.useTenkoku = 0.0f;
			if (this.tenObject != null)
			{
				if (this.tenObject.activeInHierarchy)
				{
					this.useTenkoku = 1.0f;
				}

				if (this.useTenkoku == 1.0f)
				{
					if (this.setLight == null)
						this.setLight = GameObject.Find("LIGHT_World").GetComponent<Light>();
					if (this.tenkokuWindModule == null)
					{
						this.tenkokuWindModule = GameObject.Find("Tenkoku_WindZone").GetComponent<WindZone>();
					}
					else
					{
						this.tenkokuWindDir = this.tenkokuWindModule.transform.eulerAngles.y;
						this.tenkokuWindAmt = this.tenkokuWindModule.windMain;
					}
				}
			}
			Shader.SetGlobalFloat("_useTenkoku", this.useTenkoku);


			//GET RIPPLE OBJECT REFERENCE AND LAYER
			if (Application.isPlaying && this.fxRippleObject == null)
			{
				this.fxRippleObject = GameObject.Find("fx_rippleNormals(Clone)");
			}
			if (this.fxRippleObject != null)
			{
				this.fxRippleObject.layer = this.layerScreenFXNum;
			}


			//SET COMPONENT LAYERS
			if (this.suimonoModuleLibrary.normalsCamObject != null)
				this.suimonoModuleLibrary.normalsCamObject.cullingMask = 1 << this.layerScreenFXNum;
			if (this.suimonoModuleLibrary.wakeCamObject != null)
				this.suimonoModuleLibrary.wakeCamObject.cullingMask = 1 << this.layerScreenFXNum;


			//HANDLE COMPONENTS

			//Tranparency function
			if (this.suimonoModuleLibrary.transCamObject != null)
			{
				this.transLayer = (this.transLayer & ~(1 << this.layerWaterNum)); //remove water layer from transparent mask
				this.transLayer = (this.transLayer & ~(1 << this.layerDepthNum)); //remove Depth layer from transparent mask
				this.transLayer = (this.transLayer & ~(1 << this.layerScreenFXNum)); //remove Screen layer from transparent mask

				this.suimonoModuleLibrary.transCamObject.cullingMask = this.transLayer;
				this.suimonoModuleLibrary.transCamObject.farClipPlane = this.transRenderDistance * 1.2f;
				Shader.SetGlobalFloat("_suimonoTransDist", this.transRenderDistance);
			}
			else
			{
				this.suimonoModuleLibrary.transCamObject = this.transform.Find("cam_SuimonoTrans").gameObject.GetComponent<Camera>() as Camera;
			}

			if (this.suimonoModuleLibrary.transToolsObject != null)
			{
				this.suimonoModuleLibrary.transToolsObject.resolution = System.Convert.ToInt32(this.resolutions[this.transResolution]);
				//if (useEnableTransparency == false){
				//	suimonoModuleLibrary.transToolsObject.gameObject.SetActive(false);
				//} else {
				//	suimonoModuleLibrary.transToolsObject.gameObject.SetActive(true);
				//}
				this.suimonoModuleLibrary.transToolsObject.gameObject.SetActive(this.enableTransparency);

			}
			else
			{
				this.suimonoModuleLibrary.transToolsObject = this.transform.Find("cam_SuimonoTrans").gameObject.GetComponent<cameraTools>();
			}


			//Caustic function
			if (this.suimonoModuleLibrary.causticCamObject != null)
			{
				if (this.enableCaustics == false)
				{
					if (!this.enableCausticsBlending)
						this.suimonoModuleLibrary.causticCamObject.gameObject.SetActive(false);
				}
				else
				{
					this.suimonoModuleLibrary.causticCamObject.gameObject.SetActive(this.enableCausticsBlending);
					this.transLayer = (this.transLayer & ~(1 << this.layerDepthNum)); //remove Depth layer from transparent mask
					this.transLayer = (this.transLayer & ~(1 << this.layerScreenFXNum)); //remove Screen layer from transparent mask
					this.suimonoModuleLibrary.causticCamObject.cullingMask = this.transLayer;
					this.suimonoModuleLibrary.causticCamObject.farClipPlane = this.transRenderDistance * 1.2f;
				}

				//remove caustics from transparency function
				this.suimonoModuleLibrary.causticHandlerObjectTrans.enabled = !this.enableCausticsBlending;
			}
			else
			{
				this.suimonoModuleLibrary.causticCamObject = this.transform.Find("cam_SuimonoCaustic").gameObject.GetComponent<Camera>() as Camera;
			}

			if (this.suimonoModuleLibrary.causticToolsObject != null)
			{
				this.suimonoModuleLibrary.causticToolsObject.resolution = System.Convert.ToInt32(this.resolutions[this.transResolution]);
			}
			else
			{
				this.suimonoModuleLibrary.causticToolsObject = this.transform.Find("cam_SuimonoCaustic").gameObject.GetComponent<cameraTools>();
			}

			Shader.SetGlobalFloat("_enableTransparency", this.useEnableTransparency ? 1.0f : 0.0f);

			//caustics function
			this.enCaustic = 0.0f;
			if (this.enableCaustics)
				this.enCaustic = 1.0f;
			Shader.SetGlobalFloat("_suimono_enableCaustic", this.enCaustic);

			//force suimono layers to caustics casting light
			//(note, this isn't strictly necessary as none of these elements accept caustic lighting, but
			//it's helpful to keep the deferred light occlusion limit more manageable.  These layers don't
			//matter when it comes to lighting, so there's no point in ever having them turned off.
			this.causticLayer = (this.causticLayer | (1 << this.layerWaterNum));
			this.causticLayer = (this.causticLayer | (1 << this.layerDepthNum));
			this.causticLayer = (this.causticLayer | (1 << this.layerScreenFXNum));

			//advanced edge function
			this.setEdge = 1.0f;
			if (!this.enableAdvancedEdge)
				this.setEdge = 0.0f;
			Shader.SetGlobalFloat("_suimono_advancedEdge", this.setEdge);
		}

		private void FixedUpdate()
		{

			//SET PHYSICS LAYER INTERACTIONS
			//This is introduced because Unity 5 no longer handles mesh colliders and triggers without throwing an error.
			//thanks a whole lot guys O_o (for nuthin').  The below physics setup should workaround this problem for everyone.
			if (this.autoSetLayers)
			{
				for (this.lx = 0; this.lx < 20; this.lx++)
				{
					//loop through and decouple layer collisions for all layers(up to 20).
					//layer 4 is the built-in water layer.
					Physics.IgnoreLayerCollision(this.lx, this.layerWaterNum);
				}
			}

			//Set Camera and Track Objects
			this.Suimono_CheckCamera();

			//set caustics
			if (this.suimonoModuleLibrary.causticObject != null)
			{
				this.suimonoModuleLibrary.causticObject.enableCaustics = Application.isPlaying && this.enableCaustics;

				if (this.setLight != null)
					this.suimonoModuleLibrary.causticObject.sceneLightObject = this.setLight;
			}

			//play underwater sounds
			this.PlayUnderwaterSound();


			//######## HANDLE FORWARD RENDERING SWITCH #######
			if (this.setCamera != null)
			{
				this.isForward = 0.0f;
				if (this.setCameraComponent.actualRenderingPath == RenderingPath.Forward)
				{
					this.isForward = 1.0f;
				}
				Shader.SetGlobalFloat("_isForward", this.isForward);
			}


			//######## HANDLE ADVANCED DISTORTION SWITCH #######
			if (this.enableAdvancedDistort)
			{
				this.isAdvDist = 1.0f;
				this.suimonoModuleLibrary.wakeObject.SetActive(true);
				this.suimonoModuleLibrary.normalsObject.SetActive(true);
			}
			else
			{
				this.isAdvDist = 0.0f;
				this.suimonoModuleLibrary.wakeObject.SetActive(false);
				this.suimonoModuleLibrary.normalsObject.SetActive(false);
			}
			Shader.SetGlobalFloat("_suimono_advancedDistort", this.isAdvDist);


			//######## Set Camera Background Color on Shader #######
			if (this.setCameraComponent != null)
			{
				if (this.suimonoObject != null)
				{
					this.setCameraComponent.backgroundColor = this.suimonoObject.underwaterColor;
				}
				Shader.SetGlobalColor("_cameraBGColor", this.setCameraComponent.backgroundColor);
			}


			//######## Set Camera Specific Settings #######
			if (this.setCameraComponent != null)
			{
				this.setCameraComponent.depthTextureMode |= DepthTextureMode.Depth;
				this.setCameraComponent.depthTextureMode |= DepthTextureMode.DepthNormals;

				//set camera depth mode to 'Depth'.  The alternative mode
				//'DepthNormals' causes rendering errors in Deferred Rendering
				//if (this.setCameraComponent.actualRenderingPath == RenderingPath.DeferredShading)
				//{
				//	this.setCameraComponent.depthTextureMode = DepthTextureMode.Depth;

				//}
				//else if (this.setCameraComponent.actualRenderingPath == RenderingPath.DeferredLighting)
				//{
				//	this.setCameraComponent.depthTextureMode = DepthTextureMode.Depth;

				//}
				//else
				//{
				//	this.setCameraComponent.depthTextureMode = DepthTextureMode.DepthNormals;
				//}

				//Set Water specific visibility layers on camera
				this.setCameraComponent.cullingMask |= (1 << this.layerWaterNum);
				this.setCameraComponent.cullingMask = (this.setCameraComponent.cullingMask & ~(1 << this.layerDepthNum) & ~(1 << this.layerScreenFXNum));
			}
		}

		private void OnDisable()
		{
			this.CancelInvoke("StoreSurfaceHeight");
		}

		private void OnEnable()
		{
			this.InvokeRepeating("StoreSurfaceHeight", 0.01f, 0.1f);
		}

		private void StoreSurfaceHeight()
		{
			if (this.enabled)
			{
				if (this.setCamera != null)
				{
					this.heightValues = this.SuimonoGetHeightAll(this.setCamera.transform.position);
					this.currentSurfaceLevel = this.heightValues[1];
					this.currentObjectDepth = this.heightValues[3];
					this.currentObjectIsOver = this.heightValues[4];
					this.currentTransitionDepth = this.heightValues[9];
					this.objectEnableUnderwaterFX = this.heightValues[10];

					this.checkUnderwaterEffects();
					this.checkWaterTransition();
				}
			}
		}

		private void PlayUnderwaterSound()
		{
			if (Application.isPlaying)
			{
				if (this.underSoundObject != null && this.setTrack != null && this.underSoundComponent != null)
				{
					this.underSoundObject.transform.position = this.setTrack.position;

					if (this.currentTransitionDepth > 0.0f)
					{
						if (this.playSoundBelow && this.playSounds)
						{
							this.underSoundComponent.clip = this.suimonoModuleLibrary.sndparentobj.underwaterSound;
							this.underSoundComponent.volume = this.maxVolume;
							this.underSoundComponent.loop = true;
							if (!this.underSoundComponent.isPlaying)
							{
								this.underSoundComponent.Play();
							}
						}
						else
						{
							this.underSoundComponent.Stop();
						}
					}
					else
					{
						if (this.suimonoModuleLibrary.sndparentobj.underwaterSound != null)
						{
							if (this.playSoundAbove && this.playSounds)
							{
								this.underSoundComponent.clip = this.suimonoModuleLibrary.sndparentobj.abovewaterSound;
								this.underSoundComponent.volume = 0.45f * this.maxVolume;
								this.underSoundComponent.loop = true;
								if (!this.underSoundComponent.isPlaying)
								{
									this.underSoundComponent.Play();
								}
							}
							else
							{
								this.underSoundComponent.Stop();
							}
						}
					}
				}
			}
		}




		public void AddFX(int fxSystem, Vector3 effectPos, int addRate, float addSize, float addRot, float addARot, Vector3 addVeloc, Color addCol)
		{
			if (this.suimonoModuleLibrary.fxObject != null)
			{
				this.fx = fxSystem;

				if (this.suimonoModuleLibrary.fxObject.fxParticles[this.fx] != null)
				{
					this.suimonoModuleLibrary.fxObject.fxParticles[this.fx].Emit(addRate);
					//get particles
					if (this.setParticles != null)
						this.setParticles = null;
					this.setParticles = new ParticleSystem.Particle[this.suimonoModuleLibrary.fxObject.fxParticles[this.fx].particleCount];
					this.suimonoModuleLibrary.fxObject.fxParticles[this.fx].GetParticles(this.setParticles);
					//set particles
					if (this.suimonoModuleLibrary.fxObject.fxParticles[this.fx].particleCount > 0.0f)
					{
						for (this.px = (this.suimonoModuleLibrary.fxObject.fxParticles[this.fx].particleCount - addRate); this.px < this.suimonoModuleLibrary.fxObject.fxParticles[this.fx].particleCount; this.px++)
						{

							//set position
							this.setParticles[this.px].position = new Vector3(effectPos.x, effectPos.y, effectPos.z);

							//set variables
							this.setParticles[this.px].startSize = addSize;

							this.setParticles[this.px].rotation = addRot;
							this.setParticles[this.px].angularVelocity = addARot;

							this.setParticles[this.px].velocity = new Vector3(addVeloc.x, addVeloc.y, addVeloc.z);

							this.setParticles[this.px].startColor *= addCol;

						}
						this.suimonoModuleLibrary.fxObject.fxParticles[this.fx].SetParticles(this.setParticles, this.setParticles.Length);
						this.suimonoModuleLibrary.fxObject.fxParticles[this.fx].Play();
					}
				}
			}
		}




		public void AddSoundFX(AudioClip sndClip, Vector3 soundPos, Vector3 sndVelocity)
		{

			if (this.enableInteraction)
			{
				this.setpitch = 1.0f;
				this.setvolume = 1.0f;

				if (this.playSounds && this.suimonoModuleLibrary.sndparentobj.defaultSplashSound.Length >= 1)
				{
					this.setstep = this.suimonoModuleLibrary.sndparentobj.defaultSplashSound[this.modRand.Next(0, this.defaultSplashSound.Length - 1)];
					this.setpitch = sndVelocity.y;
					this.setvolume = sndVelocity.z;
					this.setvolume = Mathf.Lerp(0.0f, 1.0f, this.setvolume) * this.maxVolume;

					//check depth and morph sounds if underwater
					if (this.currentObjectDepth > 0.0f)
					{
						this.setpitch *= 0.25f;
						this.setvolume *= 0.5f;
					}

					this.useSoundAudioComponent = this.sndComponents[this.currentSound];
					this.useSoundAudioComponent.clip = sndClip;
					if (!this.useSoundAudioComponent.isPlaying)
					{
						this.useSoundAudioComponent.transform.localPosition = soundPos;
						this.useSoundAudioComponent.volume = this.setvolume;
						this.useSoundAudioComponent.pitch = this.setpitch;
						this.useSoundAudioComponent.minDistance = 4.0f;
						this.useSoundAudioComponent.maxDistance = 20.0f;
						this.useSoundAudioComponent.clip = this.setstep;
						this.useSoundAudioComponent.loop = false;
						this.useSoundAudioComponent.Play();
					}

					this.currentSound += 1;
					if (this.currentSound >= (this.maxSounds - 1))
						this.currentSound = 0;
				}

			}
		}






		public void AddSound(string sndMode, Vector3 soundPos, Vector3 sndVelocity)
		{

			if (this.enableInteraction)
			{
				this.setpitch = 1.0f;
				this.setvolume = 1.0f;

				if (this.playSounds && this.suimonoModuleLibrary.sndparentobj.defaultSplashSound.Length >= 1)
				{
					this.setstep = this.suimonoModuleLibrary.sndparentobj.defaultSplashSound[this.modRand.Next(0, this.suimonoModuleLibrary.sndparentobj.defaultSplashSound.Length - 1)];
					this.setpitch = sndVelocity.y;
					this.setvolume = sndVelocity.z;
					this.setvolume = Mathf.Lerp(0.0f, 10.0f, this.setvolume);

					//check depth and morph sounds if underwater
					if (this.currentObjectDepth > 0.0f)
					{
						this.setpitch *= 0.25f;
						this.setvolume *= 0.5f;
					}

					this.useSoundAudioComponent = this.sndComponents[this.currentSound];
					if (!this.useSoundAudioComponent.isPlaying)
					{
						this.useSoundAudioComponent.transform.localPosition = soundPos;
						this.useSoundAudioComponent.volume = this.setvolume;
						this.useSoundAudioComponent.pitch = this.setpitch;
						this.useSoundAudioComponent.minDistance = 4.0f;
						this.useSoundAudioComponent.maxDistance = 20.0f;
						this.useSoundAudioComponent.clip = this.setstep;
						this.useSoundAudioComponent.loop = false;
						this.useSoundAudioComponent.Play();
					}

					this.currentSound += 1;
					if (this.currentSound >= (this.maxSounds - 1))
						this.currentSound = 0;
				}
			}
		}

		private void checkUnderwaterEffects()
		{

			if (Application.isPlaying)
			{
				if (this.currentTransitionDepth > this.underwaterThreshold)
				{

					if (this.suimonoObject != null)
					{
						if (this.enableUnderwaterFX && this.suimonoObject.enableUnderwater && this.currentObjectIsOver == 1.0f)
						{
							this.isUnderwater = true;
							Shader.SetGlobalFloat("_Suimono_IsUnderwater", 1.0f);
							if (this.suimonoObject != null)
							{
								this.suimonoObject.useShader = this.suimonoObject.shader_Under;
							}
							if (this.suimonoModuleLibrary.causticHandlerObject != null)
							{
								this.suimonoModuleLibrary.causticHandlerObjectTrans.isUnderwater = true;
								this.suimonoModuleLibrary.causticHandlerObject.isUnderwater = true;
							}
						}
					}

				}
				else
				{
					//swap camera rendering to back to default
					this.isUnderwater = false;
					Shader.SetGlobalFloat("_Suimono_IsUnderwater", 0.0f);
					if (this.suimonoObject != null)
					{
						this.suimonoObject.useShader = this.suimonoObject.shader_Surface;
					}
					if (this.suimonoModuleLibrary.causticHandlerObject != null)
					{
						this.suimonoModuleLibrary.causticHandlerObjectTrans.isUnderwater = false;
						this.suimonoModuleLibrary.causticHandlerObject.isUnderwater = false;
					}
				}
			}
		}

		private void checkWaterTransition()
		{
			if (Application.isPlaying)
			{
				this.doTransitionTimer += this._deltaTime;

				//SET COLORS
				if (this.currentTransitionDepth > this.underwaterThreshold && this.currentObjectIsOver == 1.0f)
				{

					doWaterTransition = true;

					//set underwater debris
					if (this.suimonoObject != null && this.setCamera != null)
					{

						if (this.enableUnderwaterFX && this.suimonoObject.enableUnderwater && this.objectEnableUnderwaterFX == 1.0f)
						{

							if (this.suimonoObject.enableUnderDebris)
							{
								this.suimonoModuleLibrary.underwaterDebrisTransform.position = this.setCamera.transform.position;
								this.suimonoModuleLibrary.underwaterDebrisTransform.rotation = this.setCamera.transform.rotation;
								this.suimonoModuleLibrary.underwaterDebrisTransform.Translate(Vector3.forward * 5.0f);

								this.suimonoModuleLibrary.underwaterDebrisRendererComponent.enabled = true;

								ParticleSystem.EmissionModule debrisEmission = this.suimonoModuleLibrary.underwaterDebris.emission;
								debrisEmission.enabled = this.isUnderwater;

							}
							else
							{
								if (this.suimonoModuleLibrary.underwaterDebris != null)
									this.suimonoModuleLibrary.underwaterDebrisRendererComponent.enabled = false;
							}

							this.setUnderBright = this.underLightAmt;
							this.setUnderBright *= 0.5f;


							//set attributes to shader
							this.useLight = 1.0f;
							this.useLightCol = new Color(1f, 1f, 1f, 1f);
							this.useRefract = 1.0f;
							if (this.setLight != null)
							{
								this.useLight = this.setLight.intensity;
								this.useLightCol = this.setLight.color;
							}
							if (!this.enableRefraction)
								this.useRefract = 0.0f;


							if (this.underwaterObject == null)
							{
								if (this.setCamera.gameObject.GetComponent<Suimono_UnderwaterFog>() != null)
								{
									this.underwaterObject = this.setCamera.gameObject.GetComponent<Suimono_UnderwaterFog>();
								}
							}
							if (this.underwaterObject != null)
							{
								this.underwaterObject.lightFactor = this.suimonoObject.underLightFactor * this.useLight;
								this.underwaterObject.refractAmt = this.suimonoObject.underRefractionAmount * this.useRefract;
								this.underwaterObject.refractScale = this.suimonoObject.underRefractionScale;
								this.underwaterObject.refractSpd = this.suimonoObject.underRefractionSpeed * this.useRefract;
								this.underwaterObject.fogEnd = this.suimonoObject.underwaterFogDist;
								this.underwaterObject.fogStart = this.suimonoObject.underwaterFogSpread;
								this.underwaterObject.blurSpread = this.suimonoObject.underBlurAmount;
								this.underwaterObject.underwaterColor = this.suimonoObject.underwaterColor;
								this.underwaterObject.darkRange = this.suimonoObject.underDarkRange;

								Shader.SetGlobalColor("_suimono_lightColor", this.useLightCol);
								this.underwaterObject.doTransition = false;

								//set caustic and underwater light brightness
								if (this.suimonoModuleLibrary.causticObject != null)
								{

									if (Application.isPlaying)
									{

										if (this.suimonoModuleLibrary.causticObject != null)
										{
											this.suimonoModuleLibrary.causticObject.heightFac = this.underwaterObject.hFac * 2.0f;
										}
									}
								}
							}


						}
						else
						{
							if (this.suimonoModuleLibrary.underwaterDebris != null)
								this.suimonoModuleLibrary.underwaterDebrisRendererComponent.enabled = false;
						}
					}

					if (this.underwaterObject != null)
					{
						this.underwaterObject.cancelTransition = true;
					}

				}
				else
				{

					//reset underwater debris
					if (this.suimonoModuleLibrary.underwaterDebris != null)
					{
						this.suimonoModuleLibrary.underwaterDebrisRendererComponent.enabled = false;
					}

					//show water transition
					if (this.enableTransition)
					{
						if (doWaterTransition && this.setCamera != null)
						{
							this.doTransitionTimer = 0.0f;

							if (this.underwaterObject != null)
							{
								this.underwaterObject.doTransition = true;
							}

							doWaterTransition = false;

						}
						else
						{
							this.underTrans = 1.0f;
						}
					}
				}

				if (!this.enableUnderwaterFX)
				{
					if (this.suimonoModuleLibrary.underwaterDebrisRendererComponent != null)
					{
						this.suimonoModuleLibrary.underwaterDebrisRendererComponent.enabled = false;
					}
				}
			}
		}

		private void Suimono_CheckCamera()
		{

			//get main camera object
			if (this.cameraTypeIndex == 0)
			{
				if (Camera.main != null)
				{
					this.mainCamera = Camera.main.transform;
				}
				this.manualCamera = null;
			}

			if (this.cameraTypeIndex == 1)
			{
				if (this.manualCamera != null)
				{
					this.mainCamera = this.manualCamera;
				}
				else
				{
					if (Camera.main != null)
					{
						this.mainCamera = Camera.main.transform;
					}
				}
			}

			//set camera
			if (this.setCamera != this.mainCamera)
			{
				this.setCamera = this.mainCamera;
				this.setCameraComponent = this.setCamera.gameObject.GetComponent<Camera>();
				this.underwaterObject = this.setCamera.gameObject.GetComponent<Suimono_UnderwaterFog>();
			}

			//set camera component
			if (this.setCameraComponent == null && this.setCamera != null)
			{
				this.setCameraComponent = this.setCamera.gameObject.GetComponent<Camera>();
			}

			//reset camera component
			if (this.setCamera != null && this.setCameraComponent != null)
			{
				if (this.setCameraComponent.transform != this.setCamera)
				{
					this.setCameraComponent = this.setCamera.gameObject.GetComponent<Camera>();
					this.underwaterObject = this.setCamera.gameObject.GetComponent<Suimono_UnderwaterFog>();
				}
			}

			//set track object
			if (this.setTrack == null && this.setCamera != null)
			{
				this.setTrack = this.setCamera.transform;
			}

			//install camera effects
			this.InstallCameraEffect();
		}





		/*
                public Vector2 SuimonoConvertAngleToDegrees(float convertAngle){

                    flow_dir = new Vector3(0f,0f,0f);
                    tempAngle = new Vector3(0f,0f,0f);
                    if (convertAngle <= 180.0f){
                        tempAngle = Vector3.Slerp(Vector3.forward,-Vector3.forward,(convertAngle)/180.0f);
                        flow_dir = new Vector2(tempAngle.x,tempAngle.z);
                    }
                    if (convertAngle > 180.0f){
                        tempAngle = Vector3.Slerp(-Vector3.forward,Vector3.forward,(convertAngle-180.0f)/180.0f);
                        flow_dir = new Vector2(-tempAngle.x,tempAngle.z);
                    }

                    return flow_dir;
                }
        */





		public Vector2 SuimonoConvertAngleToVector(float convertAngle)
		{
			this.flow_dir = new Vector3(0f, 0f, 0f);
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







		public float SuimonoGetHeight(Vector3 testObject, string returnMode)
		{

			// Get Heights
			this.CalculateHeights(testObject);

			// Return values
			this.returnValue = 0.0f;

			if (returnMode == "height")
				this.returnValue = this.getheight;
			if (returnMode == "surfaceLevel")
				this.returnValue = this.surfaceLevel + this.getheight;
			if (returnMode == "baseLevel")
				this.returnValue = this.surfaceLevel;
			if (returnMode == "object depth")
				this.returnValue = this.getheight - testObject.y;
			if (returnMode == "isOverWater" && this.isOverWater)
				this.returnValue = 1.0f;
			if (returnMode == "isOverWater" && !this.isOverWater)
				this.returnValue = 0.0f;

			if (returnMode == "isAtSurface")
			{
				if (((this.surfaceLevel + this.getheight) - testObject.y) < 0.25f && ((this.surfaceLevel + this.getheight) - testObject.y) > -0.25f)
					this.returnValue = 1.0f;
			}

			if (this.suimonoObject != null)
			{
				if (returnMode == "direction")
					this.returnValue = this.suimonoObject.flowDirection;
				if (returnMode == "speed")
					this.returnValue = this.suimonoObject.flowSpeed;

				if (returnMode == "wave height")
				{
					this.h1 = 0.0f;
					this.returnValue = this.getheight / this.h1;
				}
			}

			if (returnMode == "transitionDepth")
				this.returnValue = ((this.surfaceLevel + this.getheight) - (testObject.y - (this.transition_offset * this.underTrans)));


			if (returnMode == "underwaterEnabled")
			{
				this.enabledUFX = 1f;
				if (!this.suimonoObject.enableUnderwater)
					this.enabledUFX = 0f;
				this.returnValue = this.enabledUFX;
			}

			if (returnMode == "causticsEnabled")
			{
				this.enabledCaustics = 1f;
				if (!this.suimonoObject.enableCausticFX)
					this.enabledCaustics = 0f;
				this.returnValue = this.enabledCaustics;
			}

			return this.returnValue;
		}






		public float[] SuimonoGetHeightAll(Vector3 testObject)
		{

			// Get Heights
			this.CalculateHeights(testObject);

			// Return values
			this.returnValueAll = new float[12];

			// 0 height
			this.returnValueAll[0] = (this.getheight);

			// 1 surface level
			this.returnValueAll[1] = (this.getheight);

			// 2 base level
			this.returnValueAll[2] = (this.surfaceLevel);

			// 3 object depth
			this.returnValueAll[3] = ((this.getheight) - testObject.y);

			// 4 is Over Water
			this.returnValue = 1.0f;
			if (!this.isOverWater)
				this.returnValue = 0.0f;
			this.returnValueAll[4] = this.returnValue;

			// 5 is at surface
			this.returnValue = 0.0f;
			if (((this.getheight) - testObject.y) < 0.25f && ((this.getheight) - testObject.y) > -0.25f)
				this.returnValue = 1.0f;
			this.returnValueAll[5] = (this.returnValue);

			// 6 direction
			if (this.suimonoObject != null)
			{
				this.setDegrees = this.suimonoObject.flowDirection + this.suimonoObject.transform.eulerAngles.y;
				if (this.setDegrees < 0.0f)
					this.setDegrees = 365.0f + this.setDegrees;
				if (this.setDegrees > 365.0f)
					this.setDegrees = this.setDegrees - 365.0f;
				if (this.suimonoObject != null)
					this.returnValueAll[6] = this.setDegrees;
				if (this.suimonoObject == null)
					this.returnValueAll[6] = 0.0f;

				// 7 speed
				if (this.suimonoObject != null)
					this.returnValueAll[7] = (this.suimonoObject.flowSpeed);
				if (this.suimonoObject == null)
					this.returnValueAll[7] = 0.0f;

				// 8 wave height
				if (this.suimonoObject != null)
					this.h1 = (this.suimonoObject.lgWaveHeight);
				if (this.suimonoObject == null)
					this.h1 = 0.0f;
				this.returnValueAll[8] = (this.getheight / this.h1);
			}

			// 9 transition depth
			this.returnValueAll[9] = ((this.getheight) - (testObject.y - (this.transition_offset * this.underTrans)));

			// 10 enabled Underwater FX
			this.enabledUFX = 1f;
			if (this.suimonoObject != null)
			{
				if (!this.suimonoObject.enableUnderwater)
					this.enabledUFX = 0f;
				this.returnValueAll[10] = this.enabledUFX;
			}
			// 11 enabled Underwater FX
			this.enabledCaustics = 1f;
			if (this.suimonoObject != null)
			{
				if (!this.suimonoObject.enableCausticFX)
					this.enabledCaustics = 0f;
				this.returnValueAll[11] = this.enabledCaustics;
			}

			return this.returnValueAll;
		}





		public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
		{
			this.dir = point - pivot;
			this.dir = Quaternion.Euler(angles * -1f) * this.dir;
			point = this.dir + pivot;
			return point;
		}






		public Color DecodeHeightPixels(float texPosx, float texPosy, int texNum)
		{

			if (texNum == 0)
			{
				this.useDecodeTex = this.heightTex;
				this.useDecodeArray = this.pixelArray;
			}
			if (texNum == 1)
			{
				this.useDecodeTex = this.heightTexT;
				this.useDecodeArray = this.pixelArrayT;
			}
			if (texNum == 2)
			{
				this.useDecodeTex = this.heightTexR;
				this.useDecodeArray = this.pixelArrayR;
			}

			texPosx = (texPosx % this.useDecodeTex.width);
			texPosy = (texPosy % this.useDecodeTex.height);
			if (texPosx < 0f) texPosx = this.useDecodeTex.width - Mathf.Abs(texPosx);
			if (texPosy < 0f) texPosy = this.useDecodeTex.height - Mathf.Abs(texPosy);
			if (texPosx > this.useDecodeTex.width) texPosx = texPosx - this.useDecodeTex.width;
			if (texPosy > this.useDecodeTex.height) texPosy = texPosy - this.useDecodeTex.height;

			this.row = (this.useDecodeArray.Length / this.useDecodeTex.height) - Mathf.FloorToInt(texPosy);
			this.pixIndex = ((Mathf.FloorToInt(texPosx) + 1) + (this.useDecodeArray.Length - (this.useDecodeTex.width * this.row))) - 1;
			if (this.pixIndex > this.useDecodeArray.Length)
				this.pixIndex = this.pixIndex - (this.useDecodeArray.Length);
			if (this.pixIndex < 0)
				this.pixIndex = this.useDecodeArray.Length - this.pixIndex;

			this.pixCol = this.useDecodeArray[this.pixIndex];

			if (this._colorspace == ColorSpace.Linear)
			{
				this.pixCol.a = Mathf.Clamp(Mathf.Lerp(-0.035f, 0.5f, this.pixCol.a), 0f, 1f);
			}
			if (this._colorspace == ColorSpace.Gamma)
			{
				this.pixCol.a = Mathf.Clamp(Mathf.Lerp(-0.035f, 0.5f, this.pixCol.a), 0f, 1f);
			}

			return this.pixCol;
		}

		private void CalculateHeights(Vector3 testObject)
		{
			this.getheight = -1.0f;
			this.getheightC = -1.0f;
			this.getheightT = -1.0f;
			this.getheightR = 0.0f;
			this.isOverWater = false;
			this.surfaceLevel = -1.0f;

			this.layermask = 1 << this.layerWaterNum;
			this.testpos = new Vector3(testObject.x, testObject.y + 5000f, testObject.z);

			if (Physics.Raycast(this.testpos, -Vector3.up, out this.hit, 10000f, this.layermask))
			{
				this.targetSurface = this.hit.transform.gameObject;
				if (this.currentSurfaceObject != this.targetSurface || this.suimonoObject == null)
				{
					this.currentSurfaceObject = this.targetSurface;
					if (this.hit.transform.parent != null && this.hit.transform.parent.gameObject.GetComponent<SuimonoObject>() != null)
					{
						this.suimonoObject = this.hit.transform.parent.gameObject.GetComponent<SuimonoObject>();
					}
				}

				if (this.suimonoObject != null)
				{
					if (this.enableInteraction && this.suimonoObject.enableInteraction)
					{


						if (this.suimonoObject.typeIndex == 0)
						{
							this.heightObject = this.hit.transform;
						}
						else
						{
							this.heightObject = this.hit.transform.parent;
						}

						if (this.suimonoObject != null && this.hit.collider != null)
						{
							this.isOverWater = true;
							this.surfaceLevel = this.heightObject.position.y;

							//calculate relative position
							if (this.heightObject != null)
							{
								this.baseHeight = this.heightObject.position.y;
								this.baseAngle = this.heightObject.rotation.y;
								this.relativePos.x = ((this.heightObject.position.x - testObject.x) / (20.0f * this.heightObject.localScale.x) + 1f) * 0.5f * this.heightObject.localScale.x;
								this.relativePos.y = ((this.heightObject.position.z - testObject.z) / (20.0f * this.heightObject.localScale.z) + 1f) * 0.5f * this.heightObject.localScale.z;
							}

							//calculate offset
							this.useLocalTime = this.suimonoObject.localTime;
							this.flow_dirC = this.SuimonoConvertAngleToVector(this.suimonoObject.flowDirection);
							this.flowSpeed0 = new Vector2(this.flow_dirC.x * this.useLocalTime, this.flow_dirC.y * this.useLocalTime);
							this.flowSpeed1 = new Vector2(this.flow_dirC.x * this.useLocalTime * 0.25f, this.flow_dirC.y * this.useLocalTime * 0.25f);
							this.flowSpeed2 = new Vector2(this.flow_dirC.x * this.useLocalTime * 0.0625f, this.flow_dirC.y * this.useLocalTime * 0.0625f);
							this.flowSpeed3 = new Vector2(this.flow_dirC.x * this.useLocalTime * 0.125f, this.flow_dirC.y * this.useLocalTime * 0.125f);
							this.tScale = (1.0f / (this.suimonoObject.waveScale));
							this.oPos = new Vector2(0.0f - this.suimonoObject.savePos.x, 0.0f - this.suimonoObject.savePos.y);

							//calculate texture coordinates
							if (this.heightTex != null)
							{
								this.texCoord.x = (this.relativePos.x * this.tScale + this.flowSpeed0.x + (this.oPos.x)) * this.heightTex.width;
								this.texCoord.z = (this.relativePos.y * this.tScale + this.flowSpeed0.y + (this.oPos.y)) * this.heightTex.height;
								this.texCoord1.x = ((this.relativePos.x * this.tScale * 0.75f) - this.flowSpeed1.x + (this.oPos.x * 0.75f)) * this.heightTex.width;
								this.texCoord1.z = ((this.relativePos.y * this.tScale * 0.75f) - this.flowSpeed1.y + (this.oPos.y * 0.75f)) * this.heightTex.height;

								this.texCoordT.x = (this.relativePos.x * this.tScale + this.flowSpeed0.x + (this.oPos.x)) * this.heightTexT.width;
								this.texCoordT.z = (this.relativePos.y * this.tScale + this.flowSpeed0.y + (this.oPos.y)) * this.heightTexT.height;
								this.texCoordT1.x = ((this.relativePos.x * this.tScale * 0.5f) - this.flowSpeed1.x + (this.oPos.x * 0.5f)) * this.heightTexT.width;
								this.texCoordT1.z = ((this.relativePos.y * this.tScale * 0.5f) - this.flowSpeed1.y + (this.oPos.y * 0.5f)) * this.heightTexT.height;

								this.texCoordR.x = (this.relativePos.x * this.suimonoObject.lgWaveScale * this.tScale + this.flowSpeed2.x + (this.oPos.x * this.suimonoObject.lgWaveScale)) * this.heightTexR.width;
								this.texCoordR.z = (this.relativePos.y * this.suimonoObject.lgWaveScale * this.tScale + this.flowSpeed2.y + (this.oPos.y * this.suimonoObject.lgWaveScale)) * this.heightTexR.height;
								this.texCoordR1.x = ((this.relativePos.x * this.suimonoObject.lgWaveScale * this.tScale) + this.flowSpeed3.x + (this.oPos.x * this.suimonoObject.lgWaveScale)) * this.heightTexR.width;
								this.texCoordR1.z = ((this.relativePos.y * this.suimonoObject.lgWaveScale * this.tScale) + this.flowSpeed3.y + (this.oPos.y * this.suimonoObject.lgWaveScale)) * this.heightTexR.height;

								//rotate coordinates
								if (this.baseAngle != 0.0f)
								{
									this.pivotPoint = new Vector3(this.heightTex.width * this.heightObject.localScale.x * this.tScale * 0.5f + (this.flowSpeed0.x * this.heightTex.width), 0f, this.heightTex.height * this.heightObject.localScale.z * this.tScale * 0.5f + (this.flowSpeed0.y * this.heightTex.height));
									this.texCoord = this.RotatePointAroundPivot(this.texCoord, this.pivotPoint, this.heightObject.eulerAngles);
									this.pivotPoint = new Vector3(this.heightTex.width * this.heightObject.localScale.x * this.tScale * 0.5f * 0.75f - (this.flowSpeed1.x * this.heightTex.width), 0f, this.heightTex.height * this.heightObject.localScale.z * this.tScale * 0.5f * 0.75f - (this.flowSpeed1.y * this.heightTex.height));
									this.texCoord1 = this.RotatePointAroundPivot(this.texCoord1, this.pivotPoint, this.heightObject.eulerAngles);

									this.pivotPoint = new Vector3(this.heightTexT.width * this.heightObject.localScale.x * this.tScale * 0.5f + (this.flowSpeed0.x * this.heightTexT.width), 0f, this.heightTexT.height * this.heightObject.localScale.z * this.tScale * 0.5f + (this.flowSpeed0.y * this.heightTexT.height));
									this.texCoordT = this.RotatePointAroundPivot(this.texCoordT, this.pivotPoint, this.heightObject.eulerAngles);
									this.pivotPoint = new Vector3(this.heightTexT.width * this.heightObject.localScale.x * this.tScale * 0.5f * 0.5f - (this.flowSpeed1.x * this.heightTexT.width), 0f, this.heightTexT.height * this.heightObject.localScale.z * this.tScale * 0.5f * 0.5f - (this.flowSpeed1.y * this.heightTexT.height));
									this.texCoordT1 = this.RotatePointAroundPivot(this.texCoordT1, this.pivotPoint, this.heightObject.eulerAngles);

									this.pivotPoint = new Vector3(this.heightTexR.width * this.heightObject.localScale.x * this.suimonoObject.lgWaveScale * this.tScale * 0.5f + (this.flowSpeed2.x * this.heightTexR.width), 0f, this.heightTexR.height * this.heightObject.localScale.z * this.suimonoObject.lgWaveScale * this.tScale * 0.5f + (this.flowSpeed2.y * this.heightTexR.height));
									this.texCoordR = this.RotatePointAroundPivot(this.texCoordR, this.pivotPoint, this.heightObject.eulerAngles);
									this.pivotPoint = new Vector3(this.heightTexR.width * this.heightObject.localScale.x * this.suimonoObject.lgWaveScale * this.tScale * 0.5f + (this.flowSpeed3.x * this.heightTexR.width), 0f, this.heightTexR.height * this.heightObject.localScale.z * this.suimonoObject.lgWaveScale * this.tScale * 0.5f + (this.flowSpeed3.y * this.heightTexR.height));
									this.texCoordR1 = this.RotatePointAroundPivot(this.texCoordR1, this.pivotPoint, this.heightObject.eulerAngles);
								}

								//decode height value
								this.heightVal0 = this.DecodeHeightPixels(this.texCoord.x, this.texCoord.z, 0);
								this.heightVal1 = this.DecodeHeightPixels(this.texCoord1.x, this.texCoord1.z, 0);
								this.heightValT0 = this.DecodeHeightPixels(this.texCoordT.x, this.texCoordT.z, 1);
								this.heightValT1 = this.DecodeHeightPixels(this.texCoordT1.x, this.texCoordT1.z, 1);
								this.heightValR0 = this.DecodeHeightPixels(this.texCoordR.x, this.texCoordR.z, 2);
								this.heightValR1 = this.DecodeHeightPixels(this.texCoordR1.x, this.texCoordR1.z, 2);

								//set heightvalue
								this.getheightC = (this.heightVal0.a + this.heightVal1.a) * 0.8f;
								this.getheightT = ((this.heightValT0.a * 0.2f) + (this.heightValT0.a * this.heightValT1.a * 0.8f)) * this.suimonoObject.turbulenceFactor * 0.5f;
								this.getheightR = ((this.heightValR0.a * 4.0f) + (this.heightValR1.a * 3.0f));

								this.getheight = this.baseHeight + (this.getheightC * this.suimonoObject.waveHeight);
								this.getheight += (this.getheightT * this.suimonoObject.waveHeight);
								this.getheight += (this.getheightR * this.suimonoObject.lgWaveHeight);
								this.getheight = Mathf.Lerp(this.baseHeight, this.getheight, this.suimonoObject.useHeightProjection);
							}
						}
					}
				}
			}
		}



		public void RegisterSurface(SuimonoObject surface)
		{
			if (Application.isPlaying)
			{
				if (surface != null && this.sObjects != null)
				{
					// If IndexOf() returns a valid index, the surface was already registered before and we can break
					if (this.sObjects.IndexOf(surface) > -1) return;

					this.sObjects.Add(surface);
					this.sRends.Add(surface.transform.Find("Suimono_Object").gameObject.GetComponent<Renderer>());
					this.sRendSCs.Add(surface.transform.Find("Suimono_ObjectScale").gameObject.GetComponent<Renderer>());
				}
			}
		}

		public void DeregisterSurface(SuimonoObject surface)
		{
			if (Application.isPlaying)
			{
				if (surface != null && this.sObjects != null)
				{
					int surfaceIndex = this.sObjects.IndexOf(surface);
					// If IndexOf() returns -1, the surface wasn't registered before and we can break
					if (surfaceIndex < 0) return;

					this.sObjects.RemoveAt(surfaceIndex);
					this.sRends.RemoveAt(surfaceIndex);
					this.sRendSCs.RemoveAt(surfaceIndex);
				}
			}
		}

		/*
                //Check for visibility of all Suimono Surfaces in Scene
                void InitSurfaceRenderers(){
                    sObjects = FindObjectsOfType(typeof(Suimono.Core.SuimonoObject)) as Suimono.Core.SuimonoObject[];
                    sRends = new List<Renderer>();
                    sRendSCs = new List<Renderer>();

                    for (int s = 0; s < sObjects.Length; s++){
                        sRends.Add( sObjects[s].transform.Find("Suimono_Object").gameObject.GetComponent<Renderer>() );
                        sRendSCs.Add( sObjects[s].transform.Find("Suimono_ObjectScale").gameObject.GetComponent<Renderer>() );
                    }
                }
        */

		/*
                //Checks if surface objects are visible and disables module functions accordingly.
                void SetCullFunction(){

                    renderCount = 0;

                    //Update surface count every 5 seconds
                    surfaceTimer += _deltaTime;
                    if (surfaceTimer > 5f){
                        surfaceTimer = 0f;
                        InitSurfaceRenderers();
                    }

                    //check for visibility
                    for (int sX = 0; sX < sRends.Count; sX++){
                        if (sRends[sX] != null){
                        if (sRends[sX].isVisible){
                            if (sObjects[sX].typeIndex == 0){
                                if (sRendSCs[sX].isVisible){
                                    renderCount++;
                                }
                            } else {
                                renderCount++;
                            }
                        }
                        }
                    }

                    //Enable Functions
                    if (renderCount > 0 || isUnderwater){
                        useEnableTransparency = enableTransparency;
                    }

                    //Disable Functions
                    if (renderCount <= 0 && !isUnderwater){
                        useEnableTransparency = false;
                    }	


                }
        */

		//Checks if surface objects are visible and disables module functions accordingly.
		private void SetCullFunction()
		{
			this.renderCount = 0;

			//Update surface count every 5 seconds
			//surfaceTimer += _deltaTime;
			//if (surfaceTimer > 5f){
			//	surfaceTimer = 0f;
			//	InitSurfaceRenderers();
			//}

			//check for visibility
			for (int sX = 0; sX < this.sObjects.Count; sX++)
			{
				if (this.sRends[sX] == null || !this.sRends[sX].isVisible) continue;

				if (this.sObjects[sX].typeIndex == 0)
				{
					if (this.sRendSCs[sX] != null && this.sRendSCs[sX].isVisible)
					{
						this.renderCount++;
					}
				}
				else
				{
					this.renderCount++;
				}
			}

			//Enable Functions
			if (this.renderCount > 0 || this.isUnderwater)
			{
				this.useEnableTransparency = this.enableTransparency;
			}

			//Disable Functions
			if (this.renderCount <= 0 && !this.isUnderwater)
			{
				this.useEnableTransparency = false;
			}


		}

		private void InstallCameraEffect()
		{

			//Installs Camera effect if it doesn't already exist.
			if (this.setCameraComponent != null && this.autoSetCameraFX)
			{
				if (this.setCameraComponent.gameObject.GetComponent<Suimono_UnderwaterFog>() != null)
				{
					//do nothing
				}
				else
				{
					if (this.enableUnderwaterFX)
					{
						this.setCameraComponent.gameObject.AddComponent<Suimono_UnderwaterFog>();
					}
				}
			}
		}



	}
}