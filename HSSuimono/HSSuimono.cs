using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using Harmony;
using IllusionPlugin;
using Studio;
using Suimono.Core;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace HSSuimono
{
	public class HSSuimono : GenericPlugin, IEnhancedPlugin
	{
		public override string Name { get { return "HSSuimono"; } }
		public override string Version { get { return "1.0.0"; } }
		public override string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }

		#region Private Variables
		internal static HSSuimono _self;
		private const int _uniqueId = ('S' << 24) | ('U' << 16) | ('I' << 8) | 'M';

		internal AssetBundle _resources;
		private SuimonoModule _module;
		private Suimono_UnderwaterFog _underwaterFog;
		private TreeNodeObject _lastSelectedNode;
		private SuimonoObject _currentSurface = null;
		private fx_EffectObject _currentEffect = null;
		private Rigidbody _currentBuoyancyRigidbody = null;
		private readonly List<fx_buoyancy> _currentBuoyancyObjects = new List<fx_buoyancy>();
		private bool _showUi;
		private bool _showGlobalSettings = true;
		private bool _showObjectSettings = true;
		private bool _showEffectSettings = true;
		private bool _showBuoyancySettings = true;
		private Rect _rect = new Rect(Screen.width / 2 - 230, Screen.height / 2 - 350, 460, 700);
		private RectTransform _background;
		private Vector2 _scroll;
		#endregion

		#region Unity Methods
		protected override void Awake()
		{
			base.Awake();
			_self = this;
			var harmony = HarmonyExtensions.CreateInstance("com.joan6694.illusionplugins.hssuimono");
			harmony.PatchAllSafe();
			this.StartCoroutine(this.LoadBundleAsync());
			this.ExecuteDelayed(() =>
			{
				if (TimelineCompatibility.Init())
				{
					this.PopulateTimeline();
				}
			});
		}

		private IEnumerator LoadBundleAsync()
		{
			AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(Assembly.GetExecutingAssembly().GetResource("HSSuimono.Resources.HSSuimonoResources.unity3d"));
			while (request.isDone == false)
				yield return null;
			this._resources = request.assetBundle;
		}

		protected override void LevelLoaded(int level)
		{
			base.LevelLoaded(level);
			if (level == 3)
				this.Init();
		}

		protected override void Update()
		{
			base.Update();
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.W))
			{
				this._showUi = !this._showUi;
				this._background.gameObject.SetActive(this._showUi);
			}

			if (Studio.Studio.Instance != null)
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
							this._currentSurface = objectCtrlInfo.guideObject.transformTarget.GetComponent<SuimonoObject>();
							this._currentEffect = objectCtrlInfo.guideObject.transformTarget.GetComponent<fx_EffectObject>();
							this._currentBuoyancyRigidbody = objectCtrlInfo.guideObject.transformTarget.GetComponent<Rigidbody>();
							this._currentBuoyancyObjects.Clear();
							foreach (Transform child in objectCtrlInfo.guideObject.transformTarget)
							{
								fx_buoyancy b = child.GetComponent<fx_buoyancy>();
								if (b != null)
									this._currentBuoyancyObjects.Add(b);
							}
						}
						else
						{
							this._currentSurface = null;
							this._currentEffect = null;
							this._currentBuoyancyRigidbody = null;
							this._currentBuoyancyObjects.Clear();
						}
					}
					else
					{
						this._currentSurface = null;
						this._currentEffect = null;
						this._currentBuoyancyRigidbody = null;
						this._currentBuoyancyObjects.Clear();
					}
				}
			}
			if (this._module != null)
			{
				bool e = this._module.sObjects.Count != 0;
				if (this._module.enabled != e)
				{
					this._module.enabled = e;
					if (e == false)
						Camera.main.backgroundColor = Manager.Config.EtcData.BackColor;
				}
				if (this._underwaterFog != null && this._underwaterFog.enabled != e)
					this._underwaterFog.enabled = e;
			}
		}

		protected override void OnGUI()
		{
			base.OnGUI();
			if (this._showUi == false)
				return;
			IMGUIExtensions.DrawBackground(this._rect);
			this._rect = GUILayout.Window(_uniqueId, this._rect, this.WindowFunction, this.Name + " " + this.Version);
			IMGUIExtensions.FitRectTransformToRect(this._background, this._rect);
		}

		#endregion

		private void Init()
		{
			HSExtSave.HSExtSave.RegisterHandler("hssuimono", null, null, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);

			this._background = IMGUIExtensions.CreateUGUIPanelForIMGUI();
			this.ExecuteDelayed(() =>
			{
				GameObject module = this._resources.LoadAsset<GameObject>("SUIMONO_Module");
				this._module = GameObject.Instantiate(module).GetComponent<SuimonoModule>();
				module.hideFlags |= HideFlags.HideInHierarchy;
				this._module.autoSetCameraFX = true;
				this._module.transform.localPosition = Vector3.zero;
				this.ExecuteDelayed(() =>
				{
					this._underwaterFog = Camera.main.GetComponent<Suimono_UnderwaterFog>();
				}, 2);
			}, 10);
		}

		private void WindowFunction(int id)
		{
			this._scroll = GUILayout.BeginScrollView(this._scroll);

			Color colorEnabled = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			Color colorDisabled = new Color(1.0f, 1.0f, 1.0f, 0.25f);
			GUI.contentColor = colorEnabled;

			GUILayout.BeginVertical(GUI.skin.box);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			this._showGlobalSettings = GUILayout.Toggle(this._showGlobalSettings, "Global Settings", GUILayout.ExpandWidth(false));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			if (this._showGlobalSettings)
			{
				//TODO maybe do something with that
				//GUILayout.Label("Camera Mode");
				//this._module.cameraTypeIndex = GUILayout.SelectionGrid(this._module.cameraTypeIndex, this._module.cameraTypeOptions.ToArray(), );
				//if (this._module.cameraTypeIndex == 0)
				//{
				//	GUI.contentColor = colorDisabled;
				//	GUI.backgroundColor = colorDisabled;
				//}

				//GUILayout.Label("Scene Camera Object");
				//this._module.manualCamera = EditorGUI.ObjectField(new Rect(rt.x + margin + 165, rt.y + verAdd + 45, setWidth, 18), "", this._module.manualCamera, typeof(Transform), true) as Transform;

				//GUI.contentColor = colorEnabled;
				//GUI.backgroundColor = colorEnabled;
				//GUILayout.Label("Scene Track Object");
				//this._module.setTrack = EditorGUI.ObjectField(new Rect(rt.x + margin + 165, rt.y + verAdd + 75, setWidth, 18), "", this._module.setTrack, typeof(Transform), true) as Transform;
				//GUILayout.Label("Scene Light Object");
				//this._module.setLight = EditorGUI.ObjectField(new Rect(rt.x + margin + 165, rt.y + verAdd + 95, setWidth, 18), "", this._module.setLight, typeof(Light), true) as Light;

				//this._module.autoSetLayers = GUILayout.Toggle(this._module.autoSetLayers, "Set Automatic Layers");

				//this._module.autoSetCameraFX = GUILayout.Toggle(this._module.autoSetCameraFX, "Set Automatic FX");

				//GUILayout.BeginVertical(GUI.skin.box);
				//GUILayout.BeginHorizontal();
				//GUILayout.FlexibleSpace();
				//this._module.showGeneral = GUILayout.Toggle(this._module.showGeneral, "General Settings", GUILayout.ExpandWidth(false));
				//GUILayout.FlexibleSpace();
				//GUILayout.EndHorizontal();
				//if (this._module.showGeneral)
				{
					//this._module.playSounds = GUILayout.Toggle(this._module.playSounds, "Enable Sounds");
					//if (!this._module.playSounds)
					//{
					//	GUI.contentColor = colorDisabled;
					//	GUI.backgroundColor = colorDisabled;
					//}
					//IMGUIExtensions.FloatValue("Max Sound Volume", this._module.maxVolume, 0.0f, 1.0f, "0.0000", f => this._module.maxVolume = f);
					//GUI.contentColor = colorEnabled;
					//GUI.backgroundColor = colorEnabled;

					//this._module.playSoundBelow = GUILayout.Toggle(this._module.playSoundBelow, "Enable Underwater Sound");
					//this._module.playSoundAbove = GUILayout.Toggle(this._module.playSoundAbove, "Enable Above-Water Sound");



					this._module.enableUnderwaterFX = GUILayout.Toggle(this._module.enableUnderwaterFX, "Enable Underwater FX");
					this._module.enableTransition = GUILayout.Toggle(this._module.enableTransition, "Enable Transition FX");
					this._module.enableInteraction = GUILayout.Toggle(this._module.enableInteraction, "Enable Interaction");

					//this._module.disableMSAA = GUILayout.Toggle(this._module.disableMSAA, "Disable MSAA (fixes display errors in Forward Rendering)");

					IMGUIExtensions.FloatValue("Underwater Transition Threshold", this._module.underwaterThreshold, "0.00", f => this._module.underwaterThreshold = f);
				}
				//GUILayout.EndVertical();

				//GUILayout.BeginVertical(GUI.skin.box);
				//GUILayout.BeginHorizontal();
				//GUILayout.FlexibleSpace();
				//this._module.showPerformance = GUILayout.Toggle(this._module.showPerformance, "Advanced Water Settings", GUILayout.ExpandWidth(false));
				//GUILayout.FlexibleSpace();
				//GUILayout.EndHorizontal();
				//if (this._module.showPerformance)
				{
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;
					this._module.enableTransparency = GUILayout.Toggle(this._module.enableTransparency, "Water Transparency");
					if (!this._module.enableTransparency)
					{
						GUI.contentColor = colorDisabled;
						GUI.backgroundColor = colorDisabled;
					}

					if (!this._module.enableTransparency)
					{
						GUI.contentColor = colorDisabled;
						GUI.backgroundColor = colorDisabled;
					}
					if (this._module.gameObject.activeInHierarchy)
						IMGUIExtensions.LayerMaskValue("Render Layers", this._module.transLayer, 2, value => this._module.transLayer = value);
					if (this._module.gameObject.activeInHierarchy)
					{
						GUILayout.Label("Use Resolution");
						this._module.transResolution = GUILayout.SelectionGrid(this._module.transResolution, this._module.resOptions, 5);
					}
					IMGUIExtensions.FloatValue("Distance", this._module.transRenderDistance, "0.00", f => this._module.transRenderDistance = f);

					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					this._module.enableReflections = GUILayout.Toggle(this._module.enableReflections, "Water Reflections");
					if (!this._module.enableReflections)
					{
						GUI.contentColor = colorDisabled;
						GUI.backgroundColor = colorDisabled;
					}
					this._module.enableDynamicReflections = GUILayout.Toggle(this._module.enableDynamicReflections, "Enable Dynamic Reflections");
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					this._module.enableCaustics = GUILayout.Toggle(this._module.enableCaustics, "Caustic FX");
					if (!this._module.enableCaustics)
					{
						GUI.contentColor = colorDisabled;
						GUI.backgroundColor = colorDisabled;
					}

					IMGUIExtensions.IntValue("FPS", this._module.suimonoModuleLibrary.causticObject.causticFPS, 0, 255, "0", v => this._module.suimonoModuleLibrary.causticObject.causticFPS = v);
					IMGUIExtensions.ColorValue("Caustic Tint", this._module.suimonoModuleLibrary.causticObject.causticTint, color => this._module.suimonoModuleLibrary.causticObject.causticTint = color);

					if (this._module.gameObject.activeInHierarchy)
						IMGUIExtensions.LayerMaskValue("Render Layers", this._module.causticLayer, 2, value => this._module.causticLayer = value);

					IMGUIExtensions.FloatValue("Bright", this._module.suimonoModuleLibrary.causticObject.causticIntensity, 0.0f, 3.0f, "0.0000", f => this._module.suimonoModuleLibrary.causticObject.causticIntensity = f);

					IMGUIExtensions.FloatValue("Scale", this._module.suimonoModuleLibrary.causticObject.causticScale, 0.5f, 15.0f, "0.00", f => this._module.suimonoModuleLibrary.causticObject.causticScale = f);

					if (this._module.setLight == null)
					{
						GUI.contentColor = colorDisabled;
						GUI.backgroundColor = colorDisabled;
					}
					this._module.suimonoModuleLibrary.causticObject.inheritLightColor = GUILayout.Toggle(this._module.suimonoModuleLibrary.causticObject.inheritLightColor, "Inherit Light Direction");
					this._module.suimonoModuleLibrary.causticObject.inheritLightDirection = GUILayout.Toggle(this._module.suimonoModuleLibrary.causticObject.inheritLightDirection, "Inherit Light Color");

					if (this._module.enableCaustics)
					{
						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}
					this._module.enableCausticsBlending = GUILayout.Toggle(this._module.enableCausticsBlending, "Enable Advanced Caustic FX (affects performance)");

					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					this._module.enableAdvancedDistort = GUILayout.Toggle(this._module.enableAdvancedDistort, "Advanced Wake and Distortion Effects");
					if (!this._module.enableAdvancedDistort)
					{
						GUI.contentColor = colorDisabled;
						GUI.backgroundColor = colorDisabled;
					}
					GUI.contentColor = colorDisabled;
					GUI.backgroundColor = colorDisabled;
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					GUILayout.BeginHorizontal();
					this._module.enableAutoAdvance = GUILayout.Toggle(this._module.enableAutoAdvance, "Auto-advance System Timer");
					if (!this._module.enableAutoAdvance)
						IMGUIExtensions.FloatValue("", this._module.systemTime, "0.000", f => this._module.systemTime = f);
					GUILayout.EndHorizontal();
				}
				//GUILayout.EndVertical();

				if (this._module.useTenkoku == 1.0f)
				{
					GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					this._module.showTenkoku = GUILayout.Toggle(this._module.showTenkoku, "Tenkoku Sky System - Integration", GUILayout.ExpandWidth(false));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					if (this._module.showTenkoku)
					{

						this._module.tenkokuUseWind = GUILayout.Toggle(this._module.tenkokuUseWind, "Use Wind Settings");

						this._module.tenkokuUseReflect = GUILayout.Toggle(this._module.tenkokuUseReflect, "Calculate Sky Reflections");

					}
					GUILayout.EndVertical();
				}
			}

			GUILayout.EndVertical();

			if (this._currentSurface != null)
				this.SurfaceEditor();
			if (this._currentEffect != null)
				this.EffectEditor();
			if (this._currentBuoyancyRigidbody != null && this._currentBuoyancyObjects.Count != 0)
				this.BuoyancyEditor();
			GUILayout.EndScrollView();

			if (GUILayout.Button("Open Documentation in Browser"))
				Process.Start("http://www.tanukidigital.com/suimono/documentation/suimono2_documentation.pdf");

			GUI.DragWindow();
		}

		private void SurfaceEditor()
		{
			Color colorEnabled = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			Color colorDisabled = new Color(1.0f, 1.0f, 1.0f, 0.25f);
			// CURRENT OBJECT
			GUILayout.BeginVertical(GUI.skin.box);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			this._showObjectSettings = GUILayout.Toggle(this._showObjectSettings, "Selected Surface Settings", GUILayout.ExpandWidth(false));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			if (this._showObjectSettings)
			{
				int localPresetIndex = -1;

				Color colorWarning = new Color(0.9f, 0.5f, 0.1f, 1.0f);
				Color highlightColor2 = new Color(0.7f, 1f, 0.2f, 0.6f);
				Color highlightColor = new Color(1f, 0.5f, 0f, 0.9f);

				highlightColor = new Color(0.0f, 0.81f, 0.9f, 0.6f);

				if (localPresetIndex == -1)
					localPresetIndex = this._currentSurface.presetUseIndex;

				// GENERAL SETTINGS
				GUI.contentColor = colorEnabled;

				GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				this._currentSurface.showGeneral = GUILayout.Toggle(this._currentSurface.showGeneral, "General Settings", GUILayout.ExpandWidth(false));
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				this._currentSurface.editorIndex = GUILayout.SelectionGrid(this._currentSurface.editorIndex, this._currentSurface.editorOptions, 2);
				if (this._currentSurface.showGeneral)
				{
					if (this._currentSurface.enableCustomMesh == false)
					{
						GUILayout.Label("Surface Type");
						this._currentSurface.typeIndex = GUILayout.SelectionGrid(this._currentSurface.typeIndex, this._currentSurface.typeOptions, 3);
					}

					if (this._currentSurface.typeIndex == 0)
					{
						IMGUIExtensions.FloatValue("Ocean Scale", this._currentSurface.oceanScale, "0.000", f => this._currentSurface.oceanScale = f);
					}

					GUILayout.Label("Surface LOD");
					if (this._currentSurface.enableCustomMesh && this._currentSurface.typeIndex != 0)
					{
						GUI.contentColor = colorWarning;
						GUI.backgroundColor = colorWarning;
						GUILayout.Label("NOTE: Not available while using custom mesh!");
						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}
					else
					{
						if (this._currentSurface.typeIndex == 0)
						{
							this._currentSurface.lodIndex = 0;
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}
						if (this._currentSurface.typeIndex == 2)
						{
							this._currentSurface.lodIndex = 3;
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}
						this._currentSurface.lodIndex = GUILayout.SelectionGrid(this._currentSurface.lodIndex, this._currentSurface.lodOptions, 4);
						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}

					//ADVANCED FX SETTINGS
					//
					//EditorGUI.LabelField(new Rect(rt.x+margin+10, rt.y+80, 260, 18),"ADVANCED FX SETTINGS");
					//EditorGUI.LabelField(new Rect(rt.x+margin+37, rt.y+100, 150, 18),"Enable Underwater FX");
					//this._currentSurface.enableUnderwaterFX = EditorGUI.Toggle(new Rect(rt.x+margin+10, rt.y+100, 30, 18),"", this._currentSurface.enableUnderwaterFX);
					//EditorGUI.LabelField(new Rect(rt.x+margin+220, rt.y+100, 150, 18),"Enable Caustic FX");
					//this._currentSurface.enableCausticFX = EditorGUI.Toggle(new Rect(rt.x+margin+200, rt.y+100, 30, 18),"", this._currentSurface.enableCausticFX);

					//SCENE REFLECTIONS
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					this._currentSurface.enableReflections = GUILayout.Toggle(this._currentSurface.useEnableReflections, "Scene Reflections");
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					if (this._currentSurface.useEnableReflections)
					{
						this._currentSurface.enableDynamicReflections = GUILayout.Toggle(this._currentSurface.useEnableDynamicReflections, "Enable Dynamic Reflections");

						if (!this._currentSurface.useEnableDynamicReflections)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}
						if (this._currentSurface.gameObject.activeInHierarchy)
							IMGUIExtensions.LayerMaskValue("Reflect Layers", this._currentSurface.reflectLayer, 2, value => this._currentSurface.reflectLayer = value);
						if (this._currentSurface.gameObject.activeInHierarchy)
						{
							GUILayout.Label("Resolution");
							this._currentSurface.reflectResolution = GUILayout.SelectionGrid(this._currentSurface.reflectResolution, this._currentSurface.resOptions, 5);
						}

						//EditorGUI.LabelField(new Rect(rt.x+margin+27, rt.y+basePos+68, 180, 18),"Reflection Distance");
						//this._currentSurface.reflectionDistance = EditorGUI.Slider(new Rect(rt.x+margin+165, rt.y+basePos+68, setWidth, 18),"",this._currentSurface.reflectionDistance,0.0,100000.0);
						//EditorGUI.LabelField(new Rect(rt.x+margin+27, rt.y+basePos+88, 180, 18),"Reflection Spread");
						//this._currentSurface.reflectionSpread = EditorGUI.Slider(new Rect(rt.x+margin+165, rt.y+basePos+88, setWidth, 18),"",this._currentSurface.reflectionSpread,0.0,1.0);

						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;

						if (this._currentSurface.enableDynamicReflections && this._currentSurface.useEnableDynamicReflections)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}
						if (this._currentSurface.gameObject.activeInHierarchy)
						{
							GUILayout.Label("Fallback Mode");
							this._currentSurface.reflectFallback = GUILayout.SelectionGrid(this._currentSurface.reflectFallback, this._currentSurface.resFallbackOptions, 2);
						}
						//TODO IDK
						//if (this._currentSurface.reflectFallback == 2)
						//{
						//	this._currentSurface.customRefCubemap = EditorGUI.ObjectField(new Rect(rt.x + margin + 250, rt.y + basePos + 78, 136, 16), this._currentSurface.customRefCubemap, typeof(Texture), true) as Texture;
						//}
						//if (this._currentSurface.reflectFallback == 3)
						//{
						//	this._currentSurface.customRefColor = EditorGUI.ColorField(new Rect(rt.x + margin + 250, rt.y + basePos + 78, 136, 16), this._currentSurface.customRefColor);
						//}

						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}

					//TESSELLATION
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					this._currentSurface.enableTess = GUILayout.Toggle(this._currentSurface.enableTess, "Tessellation");

					if (this._currentSurface.enableTess)
					{
						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;

						if (this._currentSurface.typeIndex == 2)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}

						if (!this._currentSurface.enableTess)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}

						IMGUIExtensions.FloatValue("Tessellation Factor", this._currentSurface.waveTessAmt, 0.001f, 100.0f, "0.000", f => this._currentSurface.waveTessAmt = f);
						IMGUIExtensions.FloatValue("Tessellation Start", this._currentSurface.waveTessMin, 0.0f, 1.0f, "0.0000", f => this._currentSurface.waveTessMin = f);
						IMGUIExtensions.FloatValue("Tessellation Spread", this._currentSurface.waveTessSpread, 0.0f, 1.0f, "0.0000", f => this._currentSurface.waveTessSpread = f);
						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}

					GUI.contentColor = colorWarning;
					GUI.backgroundColor = colorWarning;
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					// INTERACTION
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;
					this._currentSurface.enableInteraction = GUILayout.Toggle(this._currentSurface.enableInteraction, "Enable Interaction");

					// CUSTOM TEXTURES
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;

					//TODO MAYBE
					//GUI.contentColor = colorEnabled;
					//GUI.backgroundColor = colorEnabled;
					//this._currentSurface.enableCustomTextures = GUILayout.Toggle(this._currentSurface.enableCustomTextures, "Custom Textures");

					//if (this._currentSurface.enableCustomTextures)
					//{

					//	GUI.contentColor = colorDisabled;
					//	GUI.backgroundColor = colorDisabled;
					//	GUILayout.Label("RGBA Normal");
					//	GUI.contentColor = colorEnabled;
					//	GUI.backgroundColor = colorEnabled;
					//	GUILayout.Label("Shallow Waves");
					//	//GUI.Label (new Rect(rt.x+margin+92, rt.y+basePos+72, 100, 18), new GUIContent ("Height"));
					//	this._currentSurface.customTexNormal1 = EditorGUI.ObjectField(new Rect(rt.x + margin + 34, rt.y + basePos + 24, 95, 45), this._currentSurface.customTexNormal1, typeof(Texture2D), true) as Texture2D;
					//	//this._currentSurface.customTexHeight1 = EditorGUI.ObjectField(new Rect(rt.x+margin+88, rt.y+basePos+24, 45, 45), this._currentSurface.customTexHeight1, typeof(Texture2D), true) as Texture2D;

					//	GUI.contentColor = colorDisabled;
					//	GUI.backgroundColor = colorDisabled;
					//	GUILayout.Label("RGBA Normal");
					//	GUI.contentColor = colorEnabled;
					//	GUI.backgroundColor = colorEnabled;
					//	GUILayout.Label("Turbulent Waves");
					//	//GUI.Label (new Rect(rt.x+margin+211, rt.y+basePos+72, 100, 18), new GUIContent ("Height"));
					//	this._currentSurface.customTexNormal2 = EditorGUI.ObjectField(new Rect(rt.x + margin + 155, rt.y + basePos + 24, 95, 45), this._currentSurface.customTexNormal2, typeof(Texture2D), true) as Texture2D;
					//	//this._currentSurface.customTexHeight2 = EditorGUI.ObjectField(new Rect(rt.x+margin+209, rt.y+basePos+24, 45, 45), this._currentSurface.customTexHeight2, typeof(Texture2D), true) as Texture2D;

					//	GUI.contentColor = colorDisabled;
					//	GUI.backgroundColor = colorDisabled;
					//	GUILayout.Label("RGBA Normal");
					//	GUI.contentColor = colorEnabled;
					//	GUI.backgroundColor = colorEnabled;
					//	GUILayout.Label("Deep Waves");
					//	//GUI.Label (new Rect(rt.x+margin+335, rt.y+basePos+72, 100, 18), new GUIContent ("Height"));
					//	this._currentSurface.customTexNormal3 = EditorGUI.ObjectField(new Rect(rt.x + margin + 277, rt.y + basePos + 24, 95, 45), this._currentSurface.customTexNormal3, typeof(Texture2D), true) as Texture2D;
					//	//this._currentSurface.customTexHeight3 = EditorGUI.ObjectField(new Rect(rt.x+margin+333, rt.y+basePos+24, 45, 45), this._currentSurface.customTexHeight3, typeof(Texture2D), true) as Texture2D;
					//}

					// CUSTOM MESH
					GUI.contentColor = colorEnabled;
					GUI.backgroundColor = colorEnabled;
					//TODO maybe for later
					//this._currentSurface.enableCustomMesh = GUILayout.Toggle(this._currentSurface.enableCustomMesh, "Custom Mesh");

					//if (this._currentSurface.enableCustomMesh)
					//{

					//	//TODO probably not
					//	//if (this._currentSurface.typeIndex != 0)
					//	//{
					//	//	this._currentSurface.customMesh = EditorGUI.ObjectField(new Rect(rt.x + margin + 171, rt.y + basePos + 8, 200, 18), this._currentSurface.customMesh, typeof(Mesh), true) as Mesh;
					//	//}

					//	//infinite ocean warning messages
					//	GUI.contentColor = colorWarning;
					//	GUI.backgroundColor = colorWarning;
					//	if (this._currentSurface.typeIndex == 0)
					//	{
					//		GUILayout.Label("NOTE: Not available in Infinite Ocean Mode!");
					//	}
					//	GUI.contentColor = colorEnabled;
					//	GUI.backgroundColor = colorEnabled;
					//}
				}
				GUILayout.EndVertical();

				if (this._currentSurface.editorIndex == 1)
				{
					//WAVE SETTINGS
					GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					this._currentSurface.showWaves = GUILayout.Toggle(this._currentSurface.showWaves, "Wave Settings", GUILayout.ExpandWidth(false));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					if (this._currentSurface.showWaves)
					{
						if (!this._currentSurface.customWaves)
							IMGUIExtensions.FloatValue("Wave Scale (Beaufort)", this._currentSurface.beaufortScale, 0.0f, 12.0f, "0.00", f => this._currentSurface.beaufortScale = f);
						else
						{
							GUILayout.BeginHorizontal();
							GUILayout.Label("Wave Scale (Beaufort)");
							GUI.contentColor = colorWarning;
							GUI.backgroundColor = colorWarning;
							GUILayout.Label("Disabled: Using custom settings!");
							GUILayout.EndHorizontal();
						}
						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;

						IMGUIExtensions.FloatValue("Wave Direction", this._currentSurface.flowDirection, 0.0f, 360.0f, "0.00", f => this._currentSurface.flowDirection = f);

						IMGUIExtensions.FloatValue("Wave Speed", this._currentSurface.flowSpeed, 0.0f, 0.1f, "0.00000", f => this._currentSurface.flowSpeed = f);


						IMGUIExtensions.FloatValue("Height Projection", this._currentSurface.heightProjection, 0.0f, 1.0f, "0.0000", f => this._currentSurface.heightProjection = f);

						this._currentSurface.customWaves = GUILayout.Toggle(this._currentSurface.customWaves, "Use Custom Settings");
						if (!this._currentSurface.customWaves)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}

						IMGUIExtensions.FloatValue("Turbulence Amount", this._currentSurface.turbulenceFactor, 0.0f, 1.0f, "0.0000", f => this._currentSurface.turbulenceFactor = f);

						IMGUIExtensions.FloatValue("Wave Height", this._currentSurface.waveHeight, 0.0f, 4.0f, "0.00", f => this._currentSurface.waveHeight = f);

						Color cColor = GUI.contentColor;
						Color bColor = GUI.backgroundColor;
						if (this._currentSurface.typeIndex == 0)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}
						IMGUIExtensions.FloatValue("Wave Scale", this._currentSurface.waveScale, 0.0f, 5.0f, "0.00", f => this._currentSurface.waveScale = f);
						GUI.contentColor = cColor;
						GUI.backgroundColor = bColor;

						IMGUIExtensions.FloatValue("Large Wave Height", this._currentSurface.lgWaveHeight, 0.0f, 4.0f, "0.00", f => this._currentSurface.lgWaveHeight = f);

						IMGUIExtensions.FloatValue("Large Wave Scale", this._currentSurface.lgWaveScale, 0.0f, 4.0f, "0.00", f => this._currentSurface.lgWaveScale = f);

						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}
					GUILayout.EndVertical();
							/*
					//SHORELINE SETTINGS
					GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					this._currentSurface.showShore = GUILayout.Toggle(this._currentSurface.showShore, "Shoreline Settings", GUILayout.ExpandWidth(false));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					if (this._currentSurface.showShore)
					{
						IMGUIExtensions.FloatValue("Shoreline Height", this._currentSurface.shorelineHeight, 0.0f, 1.0f, "0.0000", f => this._currentSurface.shorelineHeight = f);

						IMGUIExtensions.FloatValue("Shoreline Frequency", this._currentSurface.shorelineFreq, 0.0f, 1.0f, "0.0000", f => this._currentSurface.shorelineFreq = f);

						IMGUIExtensions.FloatValue("Shoreline Speed", this._currentSurface.shorelineSpeed, 0.0f, 10.0f, "0.00", f => this._currentSurface.shorelineSpeed = f);

						IMGUIExtensions.FloatValue("Shoreline Normalize", this._currentSurface.shorelineNorm, 0.0f, 1.0f, "0.0000", f => this._currentSurface.shorelineNorm = f);
					}
					GUILayout.EndVertical();
							*/
					// SURFACE SETTINGS
					GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					this._currentSurface.showSurface = GUILayout.Toggle(this._currentSurface.showSurface, "Water Surface", GUILayout.ExpandWidth(false));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					if (this._currentSurface.showSurface)
					{
						IMGUIExtensions.FloatValue("Overall Brightness", this._currentSurface.overallBright, 0.0f, 10.0f, "0.00", f => this._currentSurface.overallBright = f);
						IMGUIExtensions.FloatValue("Overall Transparency", this._currentSurface.overallTransparency, 0.0f, 1.0f, "0.0000", f => this._currentSurface.overallTransparency = f);

						IMGUIExtensions.FloatValue("Edge Blending", this._currentSurface.edgeAmt, 0.0f, 1.0f, "0.0000", f => this._currentSurface.edgeAmt = f);

						IMGUIExtensions.FloatValue("Depth Absorption", this._currentSurface.depthAmt, 0.0f, 1.0f, "0.0000", f => this._currentSurface.depthAmt = f);
						IMGUIExtensions.FloatValue("Shallow Absorption", this._currentSurface.shallowAmt, 0.0f, 1.0f, "0.0000", f => this._currentSurface.shallowAmt = f);

						IMGUIExtensions.ColorValue("Depth Color", this._currentSurface.depthColor, color => this._currentSurface.depthColor = color);
						IMGUIExtensions.ColorValue("Shallow Color", this._currentSurface.shallowColor, color => this._currentSurface.shallowColor = color);

						IMGUIExtensions.ColorValue("Surface Blend Color", this._currentSurface.blendColor, color => this._currentSurface.blendColor = color);
						IMGUIExtensions.ColorValue("Surface Overlay Color", this._currentSurface.overlayColor, color => this._currentSurface.overlayColor = color);

						IMGUIExtensions.FloatValue("Refraction Amount", this._currentSurface.refractStrength, 0.0f, 1.0f, "0.0000", f => this._currentSurface.refractStrength = f);
						IMGUIExtensions.FloatValue("Chromatic Shift", this._currentSurface.aberrationScale, 0.0f, 1.0f, "0.0000", f => this._currentSurface.aberrationScale = f);

						if (!this._currentSurface.moduleObject.enableCausticsBlending)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}
						IMGUIExtensions.FloatValue("Caustics Blend", this._currentSurface.causticsFade, 0.0f, 1.0f, "0.0000", f => this._currentSurface.causticsFade = f);
						IMGUIExtensions.ColorValue("Caustics Color", this._currentSurface.causticsColor, color => this._currentSurface.causticsColor = color);
						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;

						IMGUIExtensions.FloatValue("Reflection Blur", this._currentSurface.reflectBlur, 0.0f, 1.0f, "0.0000", f => this._currentSurface.reflectBlur = f);
						IMGUIExtensions.FloatValue("Reflection Distortion", this._currentSurface.reflectProjection, 0.0f, 1.0f, "0.0000", f => this._currentSurface.reflectProjection = f);
						IMGUIExtensions.FloatValue("Reflection Term", this._currentSurface.reflectTerm, 0.0f, 1.0f, "0.0000", f => this._currentSurface.reflectTerm = f);

						IMGUIExtensions.FloatValue("Reflection Sharpen", this._currentSurface.reflectSharpen, 0.0f, 1.0f, "0.0000", f => this._currentSurface.reflectSharpen = f);

						IMGUIExtensions.ColorValue("Reflection Color", this._currentSurface.reflectionColor, color => this._currentSurface.reflectionColor = color);

						IMGUIExtensions.FloatValue("Hot Specular", this._currentSurface.roughness, 0.0f, 1.0f, "0.0000", f => this._currentSurface.roughness = f);
						IMGUIExtensions.FloatValue("Wide Specular", this._currentSurface.roughness2, 0.0f, 1.0f, "0.0000", f => this._currentSurface.roughness2 = f);
						IMGUIExtensions.ColorValue("Specular Color", this._currentSurface.specularColor, color => this._currentSurface.specularColor = color);
						IMGUIExtensions.ColorValue("Back Light Scatter", this._currentSurface.sssColor, color => this._currentSurface.sssColor = color);
					}
					GUILayout.EndVertical();

					// FOAM SETTINGS
					GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					this._currentSurface.showFoam = GUILayout.Toggle(this._currentSurface.showFoam, "Foam Settings", GUILayout.ExpandWidth(false));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					if (this._currentSurface.showFoam)
					{
						this._currentSurface.enableFoam = GUILayout.Toggle(this._currentSurface.enableFoam, "Enable Foam");
						if (!this._currentSurface.enableFoam)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}
						IMGUIExtensions.FloatValue("Foam Scale", this._currentSurface.foamScale, 0.0f, 1.0f, "0.0000", f => this._currentSurface.foamScale = f);
						IMGUIExtensions.FloatValue("Foam Speed", this._currentSurface.foamSpeed, 0.0f, 1.0f, "0.0000", f => this._currentSurface.foamSpeed = f);
						IMGUIExtensions.ColorValue("Foam Color", this._currentSurface.foamColor, color => this._currentSurface.foamColor = color);

						IMGUIExtensions.FloatValue("Edge Foam", this._currentSurface.edgeFoamAmt, 0.0f, 0.9f, "0.0000", f => this._currentSurface.edgeFoamAmt = f);
						IMGUIExtensions.FloatValue("Shoreline Wave Foam", this._currentSurface.shallowFoamAmt, 0.0f, 2.0f, "0.0000", f => this._currentSurface.shallowFoamAmt = f);

						IMGUIExtensions.FloatValue("Wave Foam", this._currentSurface.heightFoamAmt, 0.0f, 1.0f, "0.0000", f => this._currentSurface.heightFoamAmt = f);
						IMGUIExtensions.FloatValue("Wave Height", this._currentSurface.hFoamHeight, 0.0f, 1.0f, "0.0000", f => this._currentSurface.hFoamHeight = f);
						IMGUIExtensions.FloatValue("Wave Spread", this._currentSurface.hFoamSpread, 0.0f, 1.0f, "0.0000", f => this._currentSurface.hFoamSpread = f);

						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}
					GUILayout.EndVertical();

					// UNDERWATER SETTINGS
					GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					this._currentSurface.showUnderwater = GUILayout.Toggle(this._currentSurface.showUnderwater, "Underwater", GUILayout.ExpandWidth(false));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					if (this._currentSurface.showUnderwater)
					{
						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;

						this._currentSurface.enableUnderwater = GUILayout.Toggle(this._currentSurface.enableUnderwater, "Enable Underwater");

						if (!this._currentSurface.enableUnderwater)
						{
							GUI.contentColor = colorDisabled;
							GUI.backgroundColor = colorDisabled;
						}

						this._currentSurface.enableUnderDebris = GUILayout.Toggle(this._currentSurface.enableUnderDebris, "Enable Debris");

						IMGUIExtensions.FloatValue("Light Factor", this._currentSurface.underLightFactor, 0.0f, 1.0f, "0.0000", f => this._currentSurface.underLightFactor = f);

						IMGUIExtensions.FloatValue("Refraction Amount", this._currentSurface.underRefractionAmount, 0.0f, 0.1f, "0.0000", f => this._currentSurface.underRefractionAmount = f);

						IMGUIExtensions.FloatValue("Refraction Scale", this._currentSurface.underRefractionScale, 0.0f, 3.0f, "0.00", f => this._currentSurface.underRefractionScale = f);
						IMGUIExtensions.FloatValue("Refraction Speed", this._currentSurface.underRefractionSpeed, 0.0f, 5.0f, "0.00", f => this._currentSurface.underRefractionSpeed = f);

						IMGUIExtensions.FloatValue("Blur Amount", this._currentSurface.underBlurAmount, 0.0f, 1.0f, "0.0000", f => this._currentSurface.underBlurAmount = f);

						IMGUIExtensions.FloatValue("Depth Darkening Range", this._currentSurface.underDarkRange, 0.0f, 500.0f, "0.00", f => this._currentSurface.underDarkRange = f);

						IMGUIExtensions.FloatValue("Fog Distance", this._currentSurface.underwaterFogDist, 0.0f, 100.0f, "0.00", f => this._currentSurface.underwaterFogDist = f);
						IMGUIExtensions.FloatValue("Fog Spread", this._currentSurface.underwaterFogSpread, -20.0f, 20.0f, "0.00", f => this._currentSurface.underwaterFogSpread = f);

						IMGUIExtensions.ColorValue("Fog Color", this._currentSurface.underwaterColor, color => this._currentSurface.underwaterColor = color);

						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}
					GUILayout.EndVertical();
				}

				// SIMPLE SETTINGS
				if (this._currentSurface.editorIndex == 0)
				{
					GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					this._currentSurface.showSimpleEditor = GUILayout.Toggle(this._currentSurface.showSimpleEditor, "Simple Water Settings", GUILayout.ExpandWidth(false));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					if (this._currentSurface.showSimpleEditor)
					{
						IMGUIExtensions.FloatValue("Wave Scale (Beaufort)", this._currentSurface.beaufortScale, 0.0f, 20.0f, "0.00", f => this._currentSurface.beaufortScale = f);
						IMGUIExtensions.FloatValue("Wave Direction", this._currentSurface.flowDirection, 0.0f, 360.0f, "0.00", f => this._currentSurface.flowDirection = f);
						IMGUIExtensions.FloatValue("Flow Speed", this._currentSurface.flowSpeed, 0.0f, 0.1f, "0.0000", f => this._currentSurface.flowSpeed = f);
						IMGUIExtensions.FloatValue("Wave Scale", this._currentSurface.waveScale, 0.0f, 1.0f, "0.0000", f => this._currentSurface.waveScale = f);

						IMGUIExtensions.FloatValue("Refraction Amount", this._currentSurface.refractStrength, 0.0f, 1.0f, "0.0000", f => this._currentSurface.refractStrength = f);
						IMGUIExtensions.FloatValue("Reflection Distortion", this._currentSurface.reflectProjection, 0.0f, 1.0f, "0.0000", f => this._currentSurface.reflectProjection = f);
						IMGUIExtensions.ColorValue("Reflection Color", this._currentSurface.reflectionColor, color => this._currentSurface.reflectionColor = color);

						IMGUIExtensions.FloatValue("Depth Absorption", this._currentSurface.depthAmt, 0.0f, 1.0f, "0.0000", f => this._currentSurface.depthAmt = f);
						IMGUIExtensions.ColorValue("Depth Color", this._currentSurface.depthColor, color => this._currentSurface.depthColor = color);
						this._currentSurface.shallowColor = new Color(0f, 0f, 0f, 0f);

						IMGUIExtensions.FloatValue("Foam Scale", this._currentSurface.foamScale, 0.0f, 1.0f, "0.0000", f => this._currentSurface.foamScale = f);
						IMGUIExtensions.FloatValue("Foam Amount", this._currentSurface.edgeFoamAmt, 0.0f, 1.0f, "0.0000", f => this._currentSurface.edgeFoamAmt = f);
						IMGUIExtensions.ColorValue("Foam Color", this._currentSurface.foamColor, color => this._currentSurface.foamColor = color);

						IMGUIExtensions.FloatValue("Underwater Refraction", this._currentSurface.underRefractionAmount, 0.0f, 0.1f, "0.0000", f => this._currentSurface.underRefractionAmount = f);
						IMGUIExtensions.FloatValue("Underwater Density", this._currentSurface.underwaterFogSpread, -20.0f, 20.0f, "0.00", f => this._currentSurface.underwaterFogSpread = f);
						IMGUIExtensions.ColorValue("Underwater Depth Color", this._currentSurface.underwaterColor, color => this._currentSurface.underwaterColor = color);
						this._currentSurface.underLightFactor = 1.0f;
						this._currentSurface.underRefractionScale = 0.5f;
						this._currentSurface.underRefractionSpeed = 1.0f;
						this._currentSurface.underwaterFogDist = 15.0f;

						GUI.contentColor = colorEnabled;
						GUI.backgroundColor = colorEnabled;
					}
					GUILayout.EndVertical();
				}

				//TODO MAYBE
				// PRESET MANAGER

				GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				this._currentSurface.showPresets = GUILayout.Toggle(this._currentSurface.showPresets, "Presets", GUILayout.ExpandWidth(false));
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				if (this._currentSurface.showPresets)
				{
					////select preset file
					//GUILayout.Label("Use Preset Folder:");
					//this._currentSurface.presetFileIndex = GUILayout.SelectionGrid(this._currentSurface.presetFileIndex, this._currentSurface.presetDirs.ToArray(), );

					//GUILayout.Label("Transition:");
					//this._currentSurface.presetTransIndexFrm = GUILayout.SelectionGrid(this._currentSurface.presetTransIndexFrm, this._currentSurface.presetFiles, );
					//GUILayout.Label("-->");
					//this._currentSurface.presetTransIndexTo = GUILayout.SelectionGrid(this._currentSurface.presetTransIndexTo, this._currentSurface.presetFiles, );
					//this._currentSurface.presetTransitionTime = EditorGUI.FloatField(new Rect(rt.x + margin + 285, rt.y + 43, 30, 18), this._currentSurface.presetTransitionTime);
					//string transAction = "Start";

					////if (this._currentSurface.presetStartTransition) transAction = (this._currentSurface.presetTransitionCurrent*this._currentSurface.presetTransitionTime).ToString("F2");//"Stop";

					//if (GUI.Button(new Rect(rt.x + margin + 324, rt.y + 44, 60, 15), transAction))
					//{
					//	//this._currentSurface.presetStartTransition = !this._currentSurface.presetStartTransition;
					//	string foldName = this._currentSurface.presetDirs[this._currentSurface.presetFileIndex];
					//	string frmName = this._currentSurface.presetFiles[this._currentSurface.presetTransIndexFrm];
					//	string toName = this._currentSurface.presetFiles[this._currentSurface.presetTransIndexTo];
					//	this._currentSurface.SuimonoTransitionPreset(foldName, frmName, toName, this._currentSurface.presetTransitionTime);
					//}

					////}
					foreach (string presetName in this._currentSurface.presetFiles)
					{
						if (GUILayout.Button(presetName))
							this._currentSurface.SuimonoSetPreset("", presetName);
					}

					GUI.color = new Color(1f, 1f, 1f, 0.55f);
					//TODO LATER FOR REAL
					//if (GUI.Button(new Rect(rt.x + margin + (presetWidth - 49), rt.y + 86 + ((prx) * 13), 65, 18), "+ NEW")) this._currentSurface.PresetAdd();

					GUI.color = colorEnabled;
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndVertical();
		}

		private void EffectEditor()
		{
			Color colorEnabled = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			Color colorDisabled = new Color(1.0f, 1.0f, 1.0f, 0.25f);

			GUILayout.BeginVertical(GUI.skin.box);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			this._showEffectSettings = GUILayout.Toggle(this._showEffectSettings, "Selected Effect Settings", GUILayout.ExpandWidth(false));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			if (this._showEffectSettings)
			{
				//if (GUILayout.Button("Particle Effect"))
				//	this._currentEffect.typeIndex = 0;
				//if (GUILayout.Button("Audio Effect"))
				//	this._currentEffect.typeIndex = 1;
				//if (GUILayout.Button("Event Trigger"))
				//	this._currentEffect.typeIndex = 2;

				GUILayout.Label("Action Type");
				this._currentEffect.actionIndex = GUILayout.SelectionGrid(this._currentEffect.actionIndex, this._currentEffect.actionOptions, 3);

				if (this._currentEffect.actionIndex == 0 || this._currentEffect.actionIndex == 2)
				{

					IMGUIExtensions.FloatValue("Reset Time", this._currentEffect.actionReset, "0.00", f => this._currentEffect.actionReset = f);

					if (this._currentEffect.actionIndex == 2)
						IMGUIExtensions.IntValue("Repeat Number", this._currentEffect.actionNum, "0.00", f => this._currentEffect.actionNum = f);
				}

				//switch (this._currentEffect.typeIndex)
				//{
				//	//SET EFFECT PARTICLE UI
				//	case 0:

				GUILayout.Label("Particle Effect");

				this._currentEffect.systemIndex = GUILayout.SelectionGrid(this._currentEffect.systemIndex, this._currentEffect.sysNames.ToArray(), 3);

				GUILayout.Label("Emit Number");
				IMGUIExtensions.FloatValue("Min", this._currentEffect.emitNum.x, 0.0f, 20.0f, "0", f => this._currentEffect.emitNum.x = Mathf.Round(f));
				if (this._currentEffect.emitNum.x > this._currentEffect.emitNum.y)
					this._currentEffect.emitNum.y = this._currentEffect.emitNum.x;
				IMGUIExtensions.FloatValue("Max", this._currentEffect.emitNum.y, 0.0f, 20.0f, "0", f => this._currentEffect.emitNum.y = Mathf.Round(f));
				if (this._currentEffect.emitNum.y < this._currentEffect.emitNum.x)
					this._currentEffect.emitNum.x = this._currentEffect.emitNum.y;

				GUILayout.Label("Particle Size");
				IMGUIExtensions.FloatValue("Min", this._currentEffect.effectSize.x, 0.0f, 4.0f, "0.00", f => this._currentEffect.effectSize.x = f);
				if (this._currentEffect.effectSize.x > this._currentEffect.effectSize.y)
					this._currentEffect.effectSize.y = this._currentEffect.effectSize.x;
				IMGUIExtensions.FloatValue("Max", this._currentEffect.effectSize.y, 0.0f, 4.0f, "0.00", f => this._currentEffect.effectSize.y = f);
				if (this._currentEffect.effectSize.y < this._currentEffect.effectSize.x)
					this._currentEffect.effectSize.x = this._currentEffect.effectSize.y;




				IMGUIExtensions.FloatValue("Emission Speed", this._currentEffect.emitSpeed, "0.00", f => this._currentEffect.emitSpeed = f);

				IMGUIExtensions.FloatValue("Directional Speed", this._currentEffect.directionMultiplier, "0.00", f => this._currentEffect.directionMultiplier = f);

				this._currentEffect.emitAtWaterLevel = GUILayout.Toggle(this._currentEffect.emitAtWaterLevel, "Emit At Surface");

				IMGUIExtensions.FloatValue("Distance Range", this._currentEffect.effectDistance, "0.00", f => this._currentEffect.effectDistance = f);

				this._currentEffect.clampRot = GUILayout.Toggle(this._currentEffect.clampRot, "Clamp Rotation");

				IMGUIExtensions.ColorValue("Tint Color", this._currentEffect.tintCol, color => this._currentEffect.tintCol = color);
				//break;
				//case 1:
				//	rt = GUILayoutUtility.GetRect(buttonText, buttonStyle);

				//	GUILayout.Label("Select Audio Sample");
				//	this._currentEffect.audioObj = EditorGUI.ObjectField(new Rect(rt.x + margin + 150, rt.y + 15, 220, 18), this._currentEffect.audioObj, typeof(AudioClip), true) as AudioClip;

				//	GUILayout.Label("Audio Volume Range");
				//	IMGUIExtensions.FloatValue("Min", this._currentEffect.audioVol.x, 0.0f, 1.0f, "0.0000", f => this._currentEffect.audioVol.x = f);
				//	IMGUIExtensions.FloatValue("Max", this._currentEffect.audioVol.y, 0.0f, 1.0f, "0.0000", f => this._currentEffect.audioVol.y = f);

				//	GUILayout.Label("Audio Pitch Range");
				//	IMGUIExtensions.FloatValue("Min", this._currentEffect.audioPit.x, 0.0f, 2.0f, "0.0000", f => this._currentEffect.audioPit.x = f);
				//	IMGUIExtensions.FloatValue("Max", this._currentEffect.audioPit.y, 0.0f, 2.0f, "0.0000", f => this._currentEffect.audioPit.y = f);

				//	IMGUIExtensions.FloatValue("Audio Repeat Speed", this._currentEffect.audioSpeed, , f => this._currentEffect.audioSpeed = f);

				//	GUILayout.Space(150.0f);
				//	break;
				//SET EVENT UI
				//case 2:
				//	GUI.contentColor = colorDisabled;
				//	GUI.backgroundColor = colorDisabled;
				//	GUILayout.Label("*Event will be triggered REGARDLESS of action type*");
				//	GUI.contentColor = colorEnabled;
				//	GUI.backgroundColor = colorEnabled;


				//	this._currentEffect.enableEvents = GUILayout.Toggle(this._currentEffect.enableEvents, "Enable Event Broadcasting");


				//	if (!this._currentEffect.enableEvents)
				//	{
				//		GUI.contentColor = colorDisabled;
				//		GUI.backgroundColor = colorDisabled;
				//	}

				//	IMGUIExtensions.FloatValue("Interval(sec)", this._currentEffect.eventInterval, "0.00", f => this._currentEffect.eventInterval = f);
				//	this._currentEffect.eventAtSurface = GUILayout.Toggle(this._currentEffect.eventAtSurface, "At Surface");

				//	GUI.contentColor = colorEnabled;
				//	GUI.backgroundColor = colorEnabled;

				//	break;
				//}

				GUILayout.Label("Activation Rules");

				if (this._currentEffect.effectRule.Length <= 0)
					GUILayout.Label("There are currenctly no rules to view...");
				else
				{
					for (int i = 0; i < this._currentEffect.effectRule.Length; i++)
					{
						GUILayout.BeginHorizontal();
						GUILayout.Label("Rule " + (i + 1));
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
							this._currentEffect.DeleteRule(i);
						GUILayout.EndHorizontal();
						if (i >= this._currentEffect.effectRule.Length)
							continue;
						this._currentEffect.ruleIndex[i] = GUILayout.SelectionGrid(this._currentEffect.ruleIndex[i], this._currentEffect.ruleOptions, 2);
						int index = this._currentEffect.ruleIndex[i];
						if (index > 3 && index < 8)
							IMGUIExtensions.FloatValue("", this._currentEffect.effectData[i], "0.00", f => this._currentEffect.effectData[i] = f);
					}
				}

				if (GUILayout.Button("+ ADD NEW RULE"))
					this._currentEffect.AddRule();
			}

			GUILayout.EndVertical();
		}

		private void BuoyancyEditor()
		{
			GUILayout.BeginVertical(GUI.skin.box);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			this._showBuoyancySettings = GUILayout.Toggle(this._showBuoyancySettings, "Buoyancy Settings", GUILayout.ExpandWidth(false));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			if (this._showBuoyancySettings)
			{
				IMGUIExtensions.FloatValue("Object Mass", this._currentBuoyancyRigidbody.mass, "0.00", f => this._currentBuoyancyRigidbody.mass = f);
				IMGUIExtensions.FloatValue("Drag", this._currentBuoyancyRigidbody.drag, "0.00", f => this._currentBuoyancyRigidbody.drag = f);
				IMGUIExtensions.FloatValue("Angular Drag", this._currentBuoyancyRigidbody.angularDrag, "0.00", f => this._currentBuoyancyRigidbody.angularDrag = f);
				for (int i = 0; i < this._currentBuoyancyObjects.Count; i++)
				{
					fx_buoyancy buoyancy = this._currentBuoyancyObjects[i];
					GUILayout.BeginVertical(GUI.skin.box);
					GUILayout.Label("Buoyancy Object " + (i + 1) + ": " + buoyancy.name);

					buoyancy.engageBuoyancy = GUILayout.Toggle(buoyancy.engageBuoyancy, "Engage Buoyancy");
					IMGUIExtensions.FloatValue("Activation Range", buoyancy.activationRange, "0.00", f => buoyancy.activationRange = f);
					buoyancy.keepAtSurface = GUILayout.Toggle(buoyancy.keepAtSurface, "Keep At Surface");
					IMGUIExtensions.FloatValue("Buoyancy Offset", buoyancy.buoyancyOffset, "0.00", f => buoyancy.buoyancyOffset = f);
					IMGUIExtensions.FloatValue("Buoyancy Strength", buoyancy.buoyancyStrength, "0.00", f => buoyancy.buoyancyStrength = f);
					//IMGUIExtensions.FloatValue("Force Amount", buoyancy.forceAmount, "0.00", f => buoyancy.forceAmount = f);
					//IMGUIExtensions.FloatValue("Force Height Factor", buoyancy.forceHeightFactor, "0.00", f => buoyancy.forceHeightFactor = f);

					GUILayout.EndVertical();
				}
			}
			GUILayout.EndVertical();
		}

		private void OnSceneLoad(string scenePath, XmlNode node)
		{
			this.ExecuteDelayed(() =>
			{
				this.OnSceneLoadGeneric(node, new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList());
			}, 3);
		}

		private void OnSceneImport(string scenePath, XmlNode node)
		{
			Dictionary<int, ObjectCtrlInfo> toIgnore = new Dictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
			this.ExecuteDelayed(() =>
			{
				this.OnSceneLoadGeneric(node, Studio.Studio.Instance.dicObjectCtrl.Where(e => toIgnore.ContainsKey(e.Key) == false).OrderBy(e => SceneInfo_Import_Patches._newToOldKeys[e.Key]).ToList());
			}, 3);
		}

		private void OnSceneLoadGeneric(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
		{
			if (node == null)
				return;
			this._showGlobalSettings = XmlConvert.ToBoolean(node.Attributes["showGlobalSettings"].Value);
			this._module.enableUnderwaterFX = XmlConvert.ToBoolean(node.Attributes["enableUnderwaterFX"].Value);
			this._module.enableTransition = XmlConvert.ToBoolean(node.Attributes["enableTransition"].Value);
			this._module.enableInteraction = XmlConvert.ToBoolean(node.Attributes["enableInteraction"].Value);
			this._module.underwaterThreshold = XmlConvert.ToSingle(node.Attributes["underwaterThreshold"].Value);
			this._module.enableTransparency = XmlConvert.ToBoolean(node.Attributes["enableTransparency"].Value);
			this._module.transLayer = XmlConvert.ToInt32(node.Attributes["transLayer"].Value);
			this._module.transResolution = XmlConvert.ToInt32(node.Attributes["transResolution"].Value);
			this._module.transRenderDistance = XmlConvert.ToSingle(node.Attributes["transRenderDistance"].Value);
			this._module.enableReflections = XmlConvert.ToBoolean(node.Attributes["enableReflections"].Value);
			this._module.enableDynamicReflections = XmlConvert.ToBoolean(node.Attributes["enableDynamicReflections"].Value);
			this._module.enableCaustics = XmlConvert.ToBoolean(node.Attributes["enableCaustics"].Value);
			this._module.suimonoModuleLibrary.causticObject.causticFPS = XmlConvert.ToInt32(node.Attributes["causticFPS"].Value);
			this._module.suimonoModuleLibrary.causticObject.causticTint = new Color(
					XmlConvert.ToSingle(node.Attributes["causticTintR"].Value),
					XmlConvert.ToSingle(node.Attributes["causticTintG"].Value),
					XmlConvert.ToSingle(node.Attributes["causticTintB"].Value),
					XmlConvert.ToSingle(node.Attributes["causticTintA"].Value)
					);
			this._module.causticLayer = XmlConvert.ToInt32(node.Attributes["causticLayer"].Value);
			this._module.suimonoModuleLibrary.causticObject.causticIntensity = XmlConvert.ToSingle(node.Attributes["causticIntensity"].Value);
			this._module.suimonoModuleLibrary.causticObject.causticScale = XmlConvert.ToSingle(node.Attributes["causticScale"].Value);
			this._module.suimonoModuleLibrary.causticObject.inheritLightColor = XmlConvert.ToBoolean(node.Attributes["inheritLightColor"].Value);
			this._module.suimonoModuleLibrary.causticObject.inheritLightDirection = XmlConvert.ToBoolean(node.Attributes["inheritLightDirection"].Value);
			this._module.enableCausticsBlending = XmlConvert.ToBoolean(node.Attributes["enableCausticsBlending"].Value);
			this._module.enableAdvancedDistort = XmlConvert.ToBoolean(node.Attributes["enableAdvancedDistort"].Value);
			this._module.enableAutoAdvance = XmlConvert.ToBoolean(node.Attributes["enableAutoAdvance"].Value);
			this._module.systemTime = this._module.enableAutoAdvance || node.Attributes["systemTime"] == null ? 0f : XmlConvert.ToSingle(node.Attributes["systemTime"].Value);


			foreach (XmlNode childNode in node.ChildNodes)
			{
				int index = XmlConvert.ToInt32(childNode.Attributes["index"].Value);
				if (index >= dic.Count)
					continue;
				ObjectCtrlInfo oci = dic[index].Value;
				if (oci == null || oci.guideObject == null)
					continue;
				switch (childNode.Name)
				{
					case "surface":
						SuimonoObject surface = oci.guideObject.transformTarget.GetComponent<SuimonoObject>();
						if (surface == null)
							break;
						surface.showGeneral = XmlConvert.ToBoolean(childNode.Attributes["showGeneral"].Value);
						surface.editorIndex = XmlConvert.ToInt32(childNode.Attributes["editorIndex"].Value);
						surface.typeIndex = XmlConvert.ToInt32(childNode.Attributes["typeIndex"].Value);
						surface.oceanScale = XmlConvert.ToSingle(childNode.Attributes["oceanScale"].Value);
						surface.lodIndex = XmlConvert.ToInt32(childNode.Attributes["lodIndex"].Value);
						surface.enableReflections = XmlConvert.ToBoolean(childNode.Attributes["enableReflections"].Value);
						surface.enableDynamicReflections = XmlConvert.ToBoolean(childNode.Attributes["enableDynamicReflections"].Value);
						surface.reflectLayer = XmlConvert.ToInt32(childNode.Attributes["reflectLayer"].Value);
						surface.reflectResolution = XmlConvert.ToInt32(childNode.Attributes["reflectResolution"].Value);
						surface.reflectFallback = XmlConvert.ToInt32(childNode.Attributes["reflectFallback"].Value);
						surface.enableTess = XmlConvert.ToBoolean(childNode.Attributes["enableTess"].Value);
						surface.waveTessAmt = XmlConvert.ToSingle(childNode.Attributes["waveTessAmt"].Value);
						surface.waveTessMin = XmlConvert.ToSingle(childNode.Attributes["waveTessMin"].Value);
						surface.waveTessSpread = XmlConvert.ToSingle(childNode.Attributes["waveTessSpread"].Value);
						surface.enableInteraction = XmlConvert.ToBoolean(childNode.Attributes["enableInteraction"].Value);
						surface.showWaves = XmlConvert.ToBoolean(childNode.Attributes["showWaves"].Value);
						surface.beaufortScale = XmlConvert.ToSingle(childNode.Attributes["beaufortScale"].Value);
						surface.flowDirection = XmlConvert.ToSingle(childNode.Attributes["flowDirection"].Value);
						surface.flowSpeed = XmlConvert.ToSingle(childNode.Attributes["flowSpeed"].Value);
						surface.waveScale = XmlConvert.ToSingle(childNode.Attributes["waveScale"].Value);
						surface.heightProjection = XmlConvert.ToSingle(childNode.Attributes["heightProjection"].Value);
						surface.customWaves = XmlConvert.ToBoolean(childNode.Attributes["customWaves"].Value);
						surface.waveHeight = XmlConvert.ToSingle(childNode.Attributes["waveHeight"].Value);
						surface.turbulenceFactor = XmlConvert.ToSingle(childNode.Attributes["turbulenceFactor"].Value);
						surface.lgWaveHeight = XmlConvert.ToSingle(childNode.Attributes["lgWaveHeight"].Value);
						surface.lgWaveScale = XmlConvert.ToSingle(childNode.Attributes["lgWaveScale"].Value);
						surface.showShore = XmlConvert.ToBoolean(childNode.Attributes["showShore"].Value);
						surface.shorelineHeight = XmlConvert.ToSingle(childNode.Attributes["shorelineHeight"].Value);
						surface.shorelineFreq = XmlConvert.ToSingle(childNode.Attributes["shorelineFreq"].Value);
						surface.shorelineSpeed = XmlConvert.ToSingle(childNode.Attributes["shorelineSpeed"].Value);
						surface.shorelineNorm = XmlConvert.ToSingle(childNode.Attributes["shorelineNorm"].Value);
						surface.showSurface = XmlConvert.ToBoolean(childNode.Attributes["showSurface"].Value);
						surface.overallBright = XmlConvert.ToSingle(childNode.Attributes["overallBright"].Value);
						surface.overallTransparency = XmlConvert.ToSingle(childNode.Attributes["overallTransparency"].Value);
						surface.edgeAmt = XmlConvert.ToSingle(childNode.Attributes["edgeAmt"].Value);
						surface.depthAmt = XmlConvert.ToSingle(childNode.Attributes["depthAmt"].Value);
						surface.shallowAmt = XmlConvert.ToSingle(childNode.Attributes["shallowAmt"].Value);
						surface.depthColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["depthColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["depthColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["depthColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["depthColorA"].Value)
						);
						surface.shallowColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["shallowColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["shallowColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["shallowColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["shallowColorA"].Value)
						);
						surface.blendColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["blendColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["blendColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["blendColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["blendColorA"].Value)
						);
						surface.overlayColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["overlayColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["overlayColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["overlayColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["overlayColorA"].Value)
						);
						surface.refractStrength = XmlConvert.ToSingle(childNode.Attributes["refractStrength"].Value);
						surface.aberrationScale = XmlConvert.ToSingle(childNode.Attributes["aberrationScale"].Value);
						surface.causticsFade = XmlConvert.ToSingle(childNode.Attributes["causticsFade"].Value);
						surface.causticsColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["causticsColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["causticsColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["causticsColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["causticsColorA"].Value)
						);
						surface.reflectBlur = XmlConvert.ToSingle(childNode.Attributes["reflectBlur"].Value);
						surface.reflectProjection = XmlConvert.ToSingle(childNode.Attributes["reflectProjection"].Value);
						surface.reflectTerm = XmlConvert.ToSingle(childNode.Attributes["reflectTerm"].Value);
						surface.reflectSharpen = XmlConvert.ToSingle(childNode.Attributes["reflectSharpen"].Value);
						surface.reflectionColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["reflectionColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["reflectionColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["reflectionColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["reflectionColorA"].Value)
						);
						surface.roughness = XmlConvert.ToSingle(childNode.Attributes["roughness"].Value);
						surface.roughness2 = XmlConvert.ToSingle(childNode.Attributes["roughness2"].Value);
						surface.specularColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["specularColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["specularColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["specularColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["specularColorA"].Value)
						);
						surface.sssColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["sssColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["sssColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["sssColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["sssColorA"].Value)
						);
						surface.showFoam = XmlConvert.ToBoolean(childNode.Attributes["showFoam"].Value);
						surface.enableFoam = XmlConvert.ToBoolean(childNode.Attributes["enableFoam"].Value);
						surface.foamScale = XmlConvert.ToSingle(childNode.Attributes["foamScale"].Value);
						surface.foamSpeed = XmlConvert.ToSingle(childNode.Attributes["foamSpeed"].Value);
						surface.foamColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["foamColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["foamColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["foamColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["foamColorA"].Value)
						);
						surface.edgeFoamAmt = XmlConvert.ToSingle(childNode.Attributes["edgeFoamAmt"].Value);
						surface.shallowFoamAmt = XmlConvert.ToSingle(childNode.Attributes["shallowFoamAmt"].Value);
						surface.heightFoamAmt = XmlConvert.ToSingle(childNode.Attributes["heightFoamAmt"].Value);
						surface.hFoamHeight = XmlConvert.ToSingle(childNode.Attributes["hFoamHeight"].Value);
						surface.hFoamSpread = XmlConvert.ToSingle(childNode.Attributes["hFoamSpread"].Value);
						surface.showUnderwater = XmlConvert.ToBoolean(childNode.Attributes["showUnderwater"].Value);
						surface.enableUnderwater = XmlConvert.ToBoolean(childNode.Attributes["enableUnderwater"].Value);
						surface.enableUnderDebris = XmlConvert.ToBoolean(childNode.Attributes["enableUnderDebris"].Value);
						surface.underLightFactor = XmlConvert.ToSingle(childNode.Attributes["underLightFactor"].Value);
						surface.underRefractionAmount = XmlConvert.ToSingle(childNode.Attributes["underRefractionAmount"].Value);
						surface.underRefractionScale = XmlConvert.ToSingle(childNode.Attributes["underRefractionScale"].Value);
						surface.underRefractionSpeed = XmlConvert.ToSingle(childNode.Attributes["underRefractionSpeed"].Value);
						surface.underBlurAmount = XmlConvert.ToSingle(childNode.Attributes["underBlurAmount"].Value);
						surface.underDarkRange = XmlConvert.ToSingle(childNode.Attributes["underDarkRange"].Value);
						surface.underwaterFogDist = XmlConvert.ToSingle(childNode.Attributes["underwaterFogDist"].Value);
						surface.underwaterFogSpread = XmlConvert.ToSingle(childNode.Attributes["underwaterFogSpread"].Value);
						surface.underwaterColor = new Color(
								XmlConvert.ToSingle(childNode.Attributes["underwaterColorR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["underwaterColorG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["underwaterColorB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["underwaterColorA"].Value)
						);
						surface.showSimpleEditor = XmlConvert.ToBoolean(childNode.Attributes["showSimpleEditor"].Value);
						break;
					case "effect":
						fx_EffectObject effect = oci.guideObject.transformTarget.GetComponent<fx_EffectObject>();
						if (effect == null)
							break;

						effect.actionIndex = XmlConvert.ToInt32(childNode.Attributes["actionIndex"].Value);
						effect.actionReset = XmlConvert.ToSingle(childNode.Attributes["actionReset"].Value);
						effect.actionNum = XmlConvert.ToInt32(childNode.Attributes["actionNum"].Value);
						if (childNode.Attributes["systemIndex"] != null)
							effect.systemIndex = XmlConvert.ToInt32(childNode.Attributes["systemIndex"].Value);
						effect.emitNum.x = XmlConvert.ToSingle(childNode.Attributes["emitNumX"].Value);
						effect.emitNum.y = XmlConvert.ToSingle(childNode.Attributes["emitNumY"].Value);
						effect.effectSize.x = XmlConvert.ToSingle(childNode.Attributes["effectSizeX"].Value);
						effect.effectSize.y = XmlConvert.ToSingle(childNode.Attributes["effectSizeY"].Value);
						effect.emitSpeed = XmlConvert.ToSingle(childNode.Attributes["emitSpeed"].Value);
						effect.directionMultiplier = XmlConvert.ToSingle(childNode.Attributes["directionMultiplier"].Value);
						effect.emitAtWaterLevel = XmlConvert.ToBoolean(childNode.Attributes["emitAtWaterLevel"].Value);
						effect.effectDistance = XmlConvert.ToSingle(childNode.Attributes["effectDistance"].Value);
						effect.clampRot = XmlConvert.ToBoolean(childNode.Attributes["clampRot"].Value);
						effect.tintCol = new Color(
								XmlConvert.ToSingle(childNode.Attributes["tintColR"].Value),
								XmlConvert.ToSingle(childNode.Attributes["tintColG"].Value),
								XmlConvert.ToSingle(childNode.Attributes["tintColB"].Value),
								XmlConvert.ToSingle(childNode.Attributes["tintColA"].Value)
								);

						for (int i = 0; i < childNode.ChildNodes.Count; i++)
						{
							XmlNode grandChildNode = childNode.ChildNodes[i];
							int ruleIndex = XmlConvert.ToInt32(grandChildNode.Attributes["ruleIndex"].Value);
							effect.ruleIndex[i] = ruleIndex;
							if (ruleIndex > 3 && ruleIndex < 8 && grandChildNode.Attributes["effectData"] != null)
								effect.effectData[i] = XmlConvert.ToSingle(grandChildNode.Attributes["effectData"].Value);
						}

						break;
					case "buoyancy":
						Rigidbody buoyancyRigidbody = oci.guideObject.transformTarget.GetComponent<Rigidbody>();
						if (buoyancyRigidbody == null)
							break;
						buoyancyRigidbody.mass = XmlConvert.ToSingle(childNode.Attributes["mass"].Value);
						if (childNode.Attributes["drag"] != null)
							buoyancyRigidbody.drag = XmlConvert.ToSingle(childNode.Attributes["drag"].Value);
						buoyancyRigidbody.angularDrag = XmlConvert.ToSingle(childNode.Attributes["angularDrag"].Value);
						List<fx_buoyancy> buoyancyObjects = new List<fx_buoyancy>();
						foreach (Transform child in oci.guideObject.transformTarget)
						{
							fx_buoyancy b = child.GetComponent<fx_buoyancy>();
							if (b != null)
								buoyancyObjects.Add(b);
						}
						int limit = Mathf.Min(buoyancyObjects.Count, childNode.ChildNodes.Count);
						for (int i = 0; i < limit; i++)
						{
							XmlNode grandChildNode = childNode.ChildNodes[i];
							fx_buoyancy buoyancy = buoyancyObjects[i];

							buoyancy.engageBuoyancy = XmlConvert.ToBoolean(grandChildNode.Attributes["engageBuoyancy"].Value);
							buoyancy.activationRange = XmlConvert.ToSingle(grandChildNode.Attributes["activationRange"].Value);
							buoyancy.keepAtSurface = XmlConvert.ToBoolean(grandChildNode.Attributes["keepAtSurface"].Value);
							buoyancy.buoyancyOffset = XmlConvert.ToSingle(grandChildNode.Attributes["buoyancyOffset"].Value);
							buoyancy.buoyancyStrength = XmlConvert.ToSingle(grandChildNode.Attributes["buoyancyStrength"].Value);
							//buoyancy.forceAmount = XmlConvert.ToSingle(grandChildNode.Attributes["forceAmount"].Value);
							//buoyancy.forceHeightFactor = XmlConvert.ToSingle(grandChildNode.Attributes["forceHeightFactor"].Value);
						}
						break;
				}
			}
		}

		private void OnSceneSave(string scenePath, XmlTextWriter xmlWriter)
		{
			xmlWriter.WriteAttributeString("version", this.Version);

			xmlWriter.WriteAttributeString("showGlobalSettings", XmlConvert.ToString(this._showGlobalSettings));
			xmlWriter.WriteAttributeString("enableUnderwaterFX", XmlConvert.ToString(this._module.enableUnderwaterFX));
			xmlWriter.WriteAttributeString("enableTransition", XmlConvert.ToString(this._module.enableTransition));
			xmlWriter.WriteAttributeString("enableInteraction", XmlConvert.ToString(this._module.enableInteraction));
			xmlWriter.WriteAttributeString("underwaterThreshold", XmlConvert.ToString(this._module.underwaterThreshold));
			xmlWriter.WriteAttributeString("enableTransparency", XmlConvert.ToString(this._module.enableTransparency));
			xmlWriter.WriteAttributeString("transLayer", XmlConvert.ToString(this._module.transLayer));
			xmlWriter.WriteAttributeString("transResolution", XmlConvert.ToString(this._module.transResolution));
			xmlWriter.WriteAttributeString("transRenderDistance", XmlConvert.ToString(this._module.transRenderDistance));
			xmlWriter.WriteAttributeString("enableReflections", XmlConvert.ToString(this._module.enableReflections));
			xmlWriter.WriteAttributeString("enableDynamicReflections", XmlConvert.ToString(this._module.enableDynamicReflections));
			xmlWriter.WriteAttributeString("enableCaustics", XmlConvert.ToString(this._module.enableCaustics));
			xmlWriter.WriteAttributeString("causticFPS", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.causticFPS));
			xmlWriter.WriteAttributeString("causticTintR", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.causticTint.r));
			xmlWriter.WriteAttributeString("causticTintG", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.causticTint.g));
			xmlWriter.WriteAttributeString("causticTintB", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.causticTint.b));
			xmlWriter.WriteAttributeString("causticTintA", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.causticTint.a));
			xmlWriter.WriteAttributeString("causticLayer", XmlConvert.ToString(this._module.causticLayer));
			xmlWriter.WriteAttributeString("causticIntensity", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.causticIntensity));
			xmlWriter.WriteAttributeString("causticScale", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.causticScale));
			xmlWriter.WriteAttributeString("inheritLightColor", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.inheritLightColor));
			xmlWriter.WriteAttributeString("inheritLightDirection", XmlConvert.ToString(this._module.suimonoModuleLibrary.causticObject.inheritLightDirection));
			xmlWriter.WriteAttributeString("enableCausticsBlending", XmlConvert.ToString(this._module.enableCausticsBlending));
			xmlWriter.WriteAttributeString("enableAdvancedDistort", XmlConvert.ToString(this._module.enableAdvancedDistort));
			xmlWriter.WriteAttributeString("enableAutoAdvance", XmlConvert.ToString(this._module.enableAutoAdvance));
			xmlWriter.WriteAttributeString("systemTime", XmlConvert.ToString(this._module.systemTime));

			SortedDictionary<int, ObjectCtrlInfo> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
			int i = 0;
			foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
			{
				SuimonoObject surface = kvp.Value.guideObject.transformTarget.GetComponent<SuimonoObject>();
				if (surface != null)
				{
					xmlWriter.WriteStartElement("surface");
					xmlWriter.WriteAttributeString("index", XmlConvert.ToString(i));

					xmlWriter.WriteAttributeString("showGeneral", XmlConvert.ToString(surface.showGeneral));
					xmlWriter.WriteAttributeString("editorIndex", XmlConvert.ToString(surface.editorIndex));
					xmlWriter.WriteAttributeString("typeIndex", XmlConvert.ToString(surface.typeIndex));
					xmlWriter.WriteAttributeString("oceanScale", XmlConvert.ToString(surface.oceanScale));
					xmlWriter.WriteAttributeString("lodIndex", XmlConvert.ToString(surface.lodIndex));
					xmlWriter.WriteAttributeString("enableReflections", XmlConvert.ToString(surface.enableReflections));
					xmlWriter.WriteAttributeString("enableDynamicReflections", XmlConvert.ToString(surface.enableDynamicReflections));
					xmlWriter.WriteAttributeString("reflectLayer", XmlConvert.ToString(surface.reflectLayer));
					xmlWriter.WriteAttributeString("reflectResolution", XmlConvert.ToString(surface.reflectResolution));
					xmlWriter.WriteAttributeString("reflectFallback", XmlConvert.ToString(surface.reflectFallback));
					xmlWriter.WriteAttributeString("enableTess", XmlConvert.ToString(surface.enableTess));
					xmlWriter.WriteAttributeString("waveTessAmt", XmlConvert.ToString(surface.waveTessAmt));
					xmlWriter.WriteAttributeString("waveTessMin", XmlConvert.ToString(surface.waveTessMin));
					xmlWriter.WriteAttributeString("waveTessSpread", XmlConvert.ToString(surface.waveTessSpread));
					xmlWriter.WriteAttributeString("enableInteraction", XmlConvert.ToString(surface.enableInteraction));
					xmlWriter.WriteAttributeString("showWaves", XmlConvert.ToString(surface.showWaves));
					xmlWriter.WriteAttributeString("beaufortScale", XmlConvert.ToString(surface.beaufortScale));
					xmlWriter.WriteAttributeString("flowDirection", XmlConvert.ToString(surface.flowDirection));
					xmlWriter.WriteAttributeString("flowSpeed", XmlConvert.ToString(surface.flowSpeed));
					xmlWriter.WriteAttributeString("waveScale", XmlConvert.ToString(surface.waveScale));
					xmlWriter.WriteAttributeString("heightProjection", XmlConvert.ToString(surface.heightProjection));
					xmlWriter.WriteAttributeString("customWaves", XmlConvert.ToString(surface.customWaves));
					xmlWriter.WriteAttributeString("waveHeight", XmlConvert.ToString(surface.waveHeight));
					xmlWriter.WriteAttributeString("turbulenceFactor", XmlConvert.ToString(surface.turbulenceFactor));
					xmlWriter.WriteAttributeString("lgWaveHeight", XmlConvert.ToString(surface.lgWaveHeight));
					xmlWriter.WriteAttributeString("lgWaveScale", XmlConvert.ToString(surface.lgWaveScale));
					xmlWriter.WriteAttributeString("showShore", XmlConvert.ToString(surface.showShore));
					xmlWriter.WriteAttributeString("shorelineHeight", XmlConvert.ToString(surface.shorelineHeight));
					xmlWriter.WriteAttributeString("shorelineFreq", XmlConvert.ToString(surface.shorelineFreq));
					xmlWriter.WriteAttributeString("shorelineSpeed", XmlConvert.ToString(surface.shorelineSpeed));
					xmlWriter.WriteAttributeString("shorelineNorm", XmlConvert.ToString(surface.shorelineNorm));
					xmlWriter.WriteAttributeString("showSurface", XmlConvert.ToString(surface.showSurface));
					xmlWriter.WriteAttributeString("overallBright", XmlConvert.ToString(surface.overallBright));
					xmlWriter.WriteAttributeString("overallTransparency", XmlConvert.ToString(surface.overallTransparency));
					xmlWriter.WriteAttributeString("edgeAmt", XmlConvert.ToString(surface.edgeAmt));
					xmlWriter.WriteAttributeString("depthAmt", XmlConvert.ToString(surface.depthAmt));
					xmlWriter.WriteAttributeString("shallowAmt", XmlConvert.ToString(surface.shallowAmt));
					xmlWriter.WriteAttributeString("depthColorR", XmlConvert.ToString(surface.depthColor.r));
					xmlWriter.WriteAttributeString("depthColorG", XmlConvert.ToString(surface.depthColor.g));
					xmlWriter.WriteAttributeString("depthColorB", XmlConvert.ToString(surface.depthColor.b));
					xmlWriter.WriteAttributeString("depthColorA", XmlConvert.ToString(surface.depthColor.a));
					xmlWriter.WriteAttributeString("shallowColorR", XmlConvert.ToString(surface.shallowColor.r));
					xmlWriter.WriteAttributeString("shallowColorG", XmlConvert.ToString(surface.shallowColor.g));
					xmlWriter.WriteAttributeString("shallowColorB", XmlConvert.ToString(surface.shallowColor.b));
					xmlWriter.WriteAttributeString("shallowColorA", XmlConvert.ToString(surface.shallowColor.a));
					xmlWriter.WriteAttributeString("blendColorR", XmlConvert.ToString(surface.blendColor.r));
					xmlWriter.WriteAttributeString("blendColorG", XmlConvert.ToString(surface.blendColor.g));
					xmlWriter.WriteAttributeString("blendColorB", XmlConvert.ToString(surface.blendColor.b));
					xmlWriter.WriteAttributeString("blendColorA", XmlConvert.ToString(surface.blendColor.a));
					xmlWriter.WriteAttributeString("overlayColorR", XmlConvert.ToString(surface.overlayColor.r));
					xmlWriter.WriteAttributeString("overlayColorG", XmlConvert.ToString(surface.overlayColor.g));
					xmlWriter.WriteAttributeString("overlayColorB", XmlConvert.ToString(surface.overlayColor.b));
					xmlWriter.WriteAttributeString("overlayColorA", XmlConvert.ToString(surface.overlayColor.a));
					xmlWriter.WriteAttributeString("refractStrength", XmlConvert.ToString(surface.refractStrength));
					xmlWriter.WriteAttributeString("aberrationScale", XmlConvert.ToString(surface.aberrationScale));
					xmlWriter.WriteAttributeString("causticsFade", XmlConvert.ToString(surface.causticsFade));
					xmlWriter.WriteAttributeString("causticsColorR", XmlConvert.ToString(surface.causticsColor.r));
					xmlWriter.WriteAttributeString("causticsColorG", XmlConvert.ToString(surface.causticsColor.g));
					xmlWriter.WriteAttributeString("causticsColorB", XmlConvert.ToString(surface.causticsColor.b));
					xmlWriter.WriteAttributeString("causticsColorA", XmlConvert.ToString(surface.causticsColor.a));
					xmlWriter.WriteAttributeString("reflectBlur", XmlConvert.ToString(surface.reflectBlur));
					xmlWriter.WriteAttributeString("reflectProjection", XmlConvert.ToString(surface.reflectProjection));
					xmlWriter.WriteAttributeString("reflectTerm", XmlConvert.ToString(surface.reflectTerm));
					xmlWriter.WriteAttributeString("reflectSharpen", XmlConvert.ToString(surface.reflectSharpen));
					xmlWriter.WriteAttributeString("reflectionColorR", XmlConvert.ToString(surface.reflectionColor.r));
					xmlWriter.WriteAttributeString("reflectionColorG", XmlConvert.ToString(surface.reflectionColor.g));
					xmlWriter.WriteAttributeString("reflectionColorB", XmlConvert.ToString(surface.reflectionColor.b));
					xmlWriter.WriteAttributeString("reflectionColorA", XmlConvert.ToString(surface.reflectionColor.a));
					xmlWriter.WriteAttributeString("roughness", XmlConvert.ToString(surface.roughness));
					xmlWriter.WriteAttributeString("roughness2", XmlConvert.ToString(surface.roughness2));
					xmlWriter.WriteAttributeString("specularColorR", XmlConvert.ToString(surface.specularColor.r));
					xmlWriter.WriteAttributeString("specularColorG", XmlConvert.ToString(surface.specularColor.g));
					xmlWriter.WriteAttributeString("specularColorB", XmlConvert.ToString(surface.specularColor.b));
					xmlWriter.WriteAttributeString("specularColorA", XmlConvert.ToString(surface.specularColor.a));
					xmlWriter.WriteAttributeString("sssColorR", XmlConvert.ToString(surface.sssColor.r));
					xmlWriter.WriteAttributeString("sssColorG", XmlConvert.ToString(surface.sssColor.g));
					xmlWriter.WriteAttributeString("sssColorB", XmlConvert.ToString(surface.sssColor.b));
					xmlWriter.WriteAttributeString("sssColorA", XmlConvert.ToString(surface.sssColor.a));
					xmlWriter.WriteAttributeString("showFoam", XmlConvert.ToString(surface.showFoam));
					xmlWriter.WriteAttributeString("enableFoam", XmlConvert.ToString(surface.enableFoam));
					xmlWriter.WriteAttributeString("foamScale", XmlConvert.ToString(surface.foamScale));
					xmlWriter.WriteAttributeString("foamSpeed", XmlConvert.ToString(surface.foamSpeed));
					xmlWriter.WriteAttributeString("foamColorR", XmlConvert.ToString(surface.foamColor.r));
					xmlWriter.WriteAttributeString("foamColorG", XmlConvert.ToString(surface.foamColor.g));
					xmlWriter.WriteAttributeString("foamColorB", XmlConvert.ToString(surface.foamColor.b));
					xmlWriter.WriteAttributeString("foamColorA", XmlConvert.ToString(surface.foamColor.a));
					xmlWriter.WriteAttributeString("edgeFoamAmt", XmlConvert.ToString(surface.edgeFoamAmt));
					xmlWriter.WriteAttributeString("shallowFoamAmt", XmlConvert.ToString(surface.shallowFoamAmt));
					xmlWriter.WriteAttributeString("heightFoamAmt", XmlConvert.ToString(surface.heightFoamAmt));
					xmlWriter.WriteAttributeString("hFoamHeight", XmlConvert.ToString(surface.hFoamHeight));
					xmlWriter.WriteAttributeString("hFoamSpread", XmlConvert.ToString(surface.hFoamSpread));
					xmlWriter.WriteAttributeString("showUnderwater", XmlConvert.ToString(surface.showUnderwater));
					xmlWriter.WriteAttributeString("enableUnderwater", XmlConvert.ToString(surface.enableUnderwater));
					xmlWriter.WriteAttributeString("enableUnderDebris", XmlConvert.ToString(surface.enableUnderDebris));
					xmlWriter.WriteAttributeString("underLightFactor", XmlConvert.ToString(surface.underLightFactor));
					xmlWriter.WriteAttributeString("underRefractionAmount", XmlConvert.ToString(surface.underRefractionAmount));
					xmlWriter.WriteAttributeString("underRefractionScale", XmlConvert.ToString(surface.underRefractionScale));
					xmlWriter.WriteAttributeString("underRefractionSpeed", XmlConvert.ToString(surface.underRefractionSpeed));
					xmlWriter.WriteAttributeString("underBlurAmount", XmlConvert.ToString(surface.underBlurAmount));
					xmlWriter.WriteAttributeString("underDarkRange", XmlConvert.ToString(surface.underDarkRange));
					xmlWriter.WriteAttributeString("underwaterFogDist", XmlConvert.ToString(surface.underwaterFogDist));
					xmlWriter.WriteAttributeString("underwaterFogSpread", XmlConvert.ToString(surface.underwaterFogSpread));
					xmlWriter.WriteAttributeString("underwaterColorR", XmlConvert.ToString(surface.underwaterColor.r));
					xmlWriter.WriteAttributeString("underwaterColorG", XmlConvert.ToString(surface.underwaterColor.g));
					xmlWriter.WriteAttributeString("underwaterColorB", XmlConvert.ToString(surface.underwaterColor.b));
					xmlWriter.WriteAttributeString("underwaterColorA", XmlConvert.ToString(surface.underwaterColor.a));
					xmlWriter.WriteAttributeString("showSimpleEditor", XmlConvert.ToString(surface.showSimpleEditor));

					xmlWriter.WriteEndElement();
				}
				fx_EffectObject effect = kvp.Value.guideObject.transformTarget.GetComponent<fx_EffectObject>();
				if (effect != null)
				{
					xmlWriter.WriteStartElement("effect");
					xmlWriter.WriteAttributeString("index", XmlConvert.ToString(i));

					xmlWriter.WriteAttributeString("actionIndex", XmlConvert.ToString(effect.actionIndex));
					xmlWriter.WriteAttributeString("actionReset", XmlConvert.ToString(effect.actionReset));
					xmlWriter.WriteAttributeString("actionNum", XmlConvert.ToString(effect.actionNum));
					xmlWriter.WriteAttributeString("systemIndex", XmlConvert.ToString(effect.systemIndex));
					xmlWriter.WriteAttributeString("emitNumX", XmlConvert.ToString(effect.emitNum.x));
					xmlWriter.WriteAttributeString("emitNumY", XmlConvert.ToString(effect.emitNum.y));
					xmlWriter.WriteAttributeString("effectSizeX", XmlConvert.ToString(effect.effectSize.x));
					xmlWriter.WriteAttributeString("effectSizeY", XmlConvert.ToString(effect.effectSize.y));
					xmlWriter.WriteAttributeString("emitSpeed", XmlConvert.ToString(effect.emitSpeed));
					xmlWriter.WriteAttributeString("directionMultiplier", XmlConvert.ToString(effect.directionMultiplier));
					xmlWriter.WriteAttributeString("emitAtWaterLevel", XmlConvert.ToString(effect.emitAtWaterLevel));
					xmlWriter.WriteAttributeString("effectDistance", XmlConvert.ToString(effect.effectDistance));
					xmlWriter.WriteAttributeString("clampRot", XmlConvert.ToString(effect.clampRot));
					xmlWriter.WriteAttributeString("tintColR", XmlConvert.ToString(effect.tintCol.r));
					xmlWriter.WriteAttributeString("tintColG", XmlConvert.ToString(effect.tintCol.g));
					xmlWriter.WriteAttributeString("tintColB", XmlConvert.ToString(effect.tintCol.b));
					xmlWriter.WriteAttributeString("tintColA", XmlConvert.ToString(effect.tintCol.a));

					for (int j = 0; j < effect.effectRule.Length; j++)
					{
						xmlWriter.WriteStartElement("rule");
						int ruleIndex = effect.ruleIndex[j];
						xmlWriter.WriteAttributeString("ruleIndex", XmlConvert.ToString(ruleIndex));
						if (ruleIndex > 3 && ruleIndex < 8)
							xmlWriter.WriteAttributeString("effectData", XmlConvert.ToString(effect.effectData[j]));
						xmlWriter.WriteEndElement();
					}
					xmlWriter.WriteEndElement();
				}
				Rigidbody buoyancyRigidbody = kvp.Value.guideObject.transformTarget.GetComponent<Rigidbody>();
				if (buoyancyRigidbody != null)
				{
					List<fx_buoyancy> buoyancyObjects = new List<fx_buoyancy>();
					foreach (Transform child in buoyancyRigidbody.transform)
					{
						fx_buoyancy b = child.GetComponent<fx_buoyancy>();
						if (b != null)
							buoyancyObjects.Add(b);
					}
					if (buoyancyObjects.Count != 0)
					{
						xmlWriter.WriteStartElement("buoyancy");
						xmlWriter.WriteAttributeString("index", XmlConvert.ToString(i));
						xmlWriter.WriteAttributeString("mass", XmlConvert.ToString(buoyancyRigidbody.mass));
						xmlWriter.WriteAttributeString("drag", XmlConvert.ToString(buoyancyRigidbody.drag));
						xmlWriter.WriteAttributeString("angularDrag", XmlConvert.ToString(buoyancyRigidbody.angularDrag));

						foreach (fx_buoyancy buoyancy in buoyancyObjects)
						{
							xmlWriter.WriteStartElement("object");

							xmlWriter.WriteAttributeString("engageBuoyancy", XmlConvert.ToString(buoyancy.engageBuoyancy));
							xmlWriter.WriteAttributeString("activationRange", XmlConvert.ToString(buoyancy.activationRange));
							xmlWriter.WriteAttributeString("keepAtSurface", XmlConvert.ToString(buoyancy.keepAtSurface));
							xmlWriter.WriteAttributeString("buoyancyOffset", XmlConvert.ToString(buoyancy.buoyancyOffset));
							xmlWriter.WriteAttributeString("buoyancyStrength", XmlConvert.ToString(buoyancy.buoyancyStrength));
							//xmlWriter.WriteAttributeString("forceAmount", XmlConvert.ToString(buoyancy.forceAmount));
							//xmlWriter.WriteAttributeString("forceHeightFactor", XmlConvert.ToString(buoyancy.forceHeightFactor));

							xmlWriter.WriteEndElement();
						}

						xmlWriter.WriteEndElement();
					}
				}
				++i;
			}
		}

		#region Patches
		[HarmonyPatch]
		internal static class SEGI_Init_Patches
		{
			private static bool Prepare()
			{
				return Type.GetType("HSLRE.CustomEffects.SEGI,HSLRE") != null;
			}

			private static MethodInfo TargetMethod()
			{
				return Type.GetType("HSLRE.CustomEffects.SEGI,HSLRE").GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
			}

			private static void Postfix(ref LayerMask ___giCullingMask)
			{
				___giCullingMask |= 1 << 24 /*_self._module.layerWaterNum*/;
				___giCullingMask |= 1 << 25 /*_self._module.layerDepthNum*/;
				___giCullingMask |= 1 << 26 /*_self._module.layerScreenFXNum*/;
			}
		}

		[HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
		internal static class Studio_Duplicate_Patches
		{
			public static void Postfix(Studio.Studio __instance)
			{
				foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
				{
					ObjectCtrlInfo source;
					if (__instance.dicObjectCtrl.TryGetValue(pair.Value, out source) == false)
						continue;
					ObjectCtrlInfo destination;
					if (__instance.dicObjectCtrl.TryGetValue(pair.Key, out destination) == false)
						continue;
					//if (source is OCIChar && destination is OCIChar || source is OCIItem && destination is OCIItem)
					//	_self.OnDuplicate(source, destination);
				}
			}
		}

		[HarmonyPatch(typeof(ObjectInfo), "Load", new[] { typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool) })]
		internal static class ObjectInfo_Load_Patches
		{
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				int count = 0;
				List<CodeInstruction> instructionsList = instructions.ToList();
				MethodInfo readInt32 = typeof(BinaryReader).GetMethod(nameof(BinaryReader.ReadInt32), BindingFlags.Public | BindingFlags.Instance);
				foreach (CodeInstruction inst in instructionsList)
				{
					yield return inst;
					if (inst.opcode == OpCodes.Callvirt && inst.operand == readInt32)
					{
						if (count == 1)
						{
							yield return new CodeInstruction(OpCodes.Ldarg_0);
							yield return new CodeInstruction(OpCodes.Call, typeof(ObjectInfo_Load_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
						}
						++count;
					}
				}
			}

			private static int Injected(int originalIndex, ObjectInfo __instance)
			{
				SceneInfo_Import_Patches._newToOldKeys.Add(__instance.dicKey, originalIndex);
				return originalIndex; //Doing this so other transpilers can use this value if they want
			}
		}

		[HarmonyPatch(typeof(SceneInfo), "Import", new[] { typeof(BinaryReader), typeof(Version) })]
		internal static class SceneInfo_Import_Patches //This is here because I fucked up the save format making it impossible to import scenes correctly
		{
			internal static Dictionary<int, int> _newToOldKeys = new Dictionary<int, int>();

			private static void Prefix()
			{
				_newToOldKeys.Clear();
			}
		}
		#endregion

		#region Timeline Compatibility
		private void PopulateTimeline()
		{
			//TODO
			//Caustic tint
			//Caustic brightness
			//Caustic scale

			//System timer

			//Tessellation factor
			//Tessellation start
			//Tessellation spread

			//Wave direction
			//Wave speed
			//Height projection

			//Turbulence amount
			//Wave scale
			//Wave height
			//Large wave height
			//Large wave scale

			//Overall brightness
			//Overall transparency
			//Edge blending
			//Depth absorption
			//Depth color
			//Shallow color
			//Surface blend color
			//Surface overlay color
			//Refraction amount
			//Chromatic shift
			//Caustic blend
			//Caustics color
			//Reflection blur
			//Reflection distortion
			//Reflection term
			//Reflection sharpen
			//Reflection color
			//Hot specular
			//Wide specular
			//Specular color
			//Back light scatter

			//Foam scale
			//Foam speed
			//Foam color
			//Edge foam
			//Shoreline wave foam
			//Wave foam
			//Wave height
			//Wave spread

			//Enable debris
			//Light factor
			//Refraction amount
			//Refraction scale
			//Refraction speed
			//Blur amount
			//Depth darkening range
			//Fog distance
			//Fog spread
			//Fog color

			TimelineCompatibility.AddInterpolableModelStatic(
					owner: this.Name,
					id: "systemTimer",
					parameter: null,
					name: "System Timer",
					interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => this._module.systemTime = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor),
					interpolateAfter: null,
					isCompatibleWithTarget: oci => true,
					getValue: (oci, parameter) => this._module.systemTime,
					readValueFromXml: (parameter, node) => node.ReadFloat("value"),
					writeValueToXml: (parameter, writer, value) => writer.WriteValue("value", (float)value),
					useOciInHash: false
			);
		}
		#endregion

	}
}