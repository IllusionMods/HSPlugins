using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using Harmony;
using Studio;
using ToolBox;
#if HONEYSELECT
using IllusionPlugin;
#elif KOIKATSU
using BepInEx;
using UnityEngine.SceneManagement;
using ExtensibleSaveFormat;
using System.IO;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace LightingEditor
{
#if KOIKATSU
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.lightingeditor", Name: "LightingEditor", Version: LightingEditor.versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInProcess("CharaStudio")]
#endif
    public class LightingEditor :
#if HONEYSELECT
    IEnhancedPlugin
#elif KOIKATSU
    BaseUnityPlugin
#endif
    {
#region Constants
        public const string versionNum = "1.1.0";
#if KOIKATSU
        private const int _saveVersion = 0;
        private const string _extSaveKey = "lightingData";
#endif
#endregion

#region Private Variables
        private RectTransform _imageDirectional;
        private RectTransform _imagePoint;
        private RectTransform _imageSpot;
        private RectTransform _container;

        private Toggle _shadowToggle;

        private Slider _bounceIntesitySlider;
        private InputField _bounceIntensityInputField;

        private RectTransform _shadowContainer;

        private Slider _shadowStrengthSlider;
        private InputField _shadowStrengthInputField;

#if KOIKATSU
        private Dropdown _shadowResolutionDropdown;
#endif

        private Slider _shadowBiasSlider;
        private InputField _shadowBiasInputField;

        private Slider _normalBiasSlider;
        private InputField _normalBiasInputField;

        private Slider _shadowNearPlaneSlider;
        private InputField _shadowNearPlaneInputField;

        private Toggle _cullingMaskFoldout;
        private RectTransform _cullingMaskContainer;
        private readonly Dictionary<int, Toggle> _cullingMaskLayers = new Dictionary<int, Toggle>();

        private Light _target;

#if HONEYSELECT
        private Slider _shadowDistanceSlider;
        private InputField _shadowDistanceInputField;
        private float _defaultShadowDistance;
#elif KOIKATSU
        private FieldInfo _sceneInfoLightCount;

        private Toggle _charaLightToggle;

        private Dropdown _ambientModeDropdown;

        private RectTransform _colorContainer;
        private RectTransform _trilightColorContainer;

        private RawImage _ambientLightImage;
        private RawImage _skyColorImage;
        private RawImage _equatorColorImage;
        private RawImage _groundColorImage;

        private Slider _ambientIntensitySlider;
        private InputField _ambientIntensityInputField;

        private Slider _reflectionBouncesSlider;
        private InputField _reflectionBouncesInputField;

        private Slider _reflectionIntensitySlider;
        private InputField _reflectionIntensityInputField;
#endif

        private static int _lightIntensityMaxValue = 2;
        private RectTransform _charaLightUI;
        private bool _lastEnabled;
        #endregion

        #region Plugins
#if HONEYSELECT
        public string Name { get { return "LightingEditor"; }}
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }

        public void OnLevelWasInitialized(int level){}
        public void OnFixedUpdate(){}
        public void OnLateUpdate(){}
        public void OnLevelWasLoaded(int level)
        {
            this.LevelLoaded(level);
        }

#elif KOIKATSU
        void Awake()
        {
            SceneManager.sceneLoaded += this.SceneLoaded;
            this.OnApplicationStart();
        }

        private void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            this.LevelLoaded(scene.buildIndex);
        }

        void Update()
        {
            this.OnUpdate();
        }
#endif
#endregion

#region Unity Methods
        public void OnApplicationStart()
        {
#if HONEYSELECT
            HSExtSave.HSExtSave.RegisterHandler("lightingEditor", null, null, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);
            _lightIntensityMaxValue = ModPrefs.GetInt("LightingEditor", "lightIntensityMaxValue", 2, true);
#elif KOIKATSU
            ExtendedSave.SceneBeingLoaded += this.OnSceneLoad;
            ExtendedSave.SceneBeingImported += this.OnSceneImport;
            ExtendedSave.SceneBeingSaved += this.OnSceneSave;
            this._sceneInfoLightCount = typeof(SceneInfo).GetField("lightCount", BindingFlags.Instance | BindingFlags.NonPublic);
            string valueString = BepInEx.Config.GetEntry("lightIntensityMaxValue", "2");
            if (string.IsNullOrEmpty(valueString) == false)
                int.TryParse(valueString, out _lightIntensityMaxValue);
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.illusionplugins.lightingeditor");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
#if HONEYSELECT
            QualitySettings.pixelLightCount = ModPrefs.GetInt("LightingEditor", "pixelLightCountOverride", 16, true);
            this._defaultShadowDistance = QualitySettings.shadowDistance;
#endif
        }

        public void OnApplicationQuit()
        {
#if KOIKATSU
            BepInEx.Config.SetEntry("lightIntensityMaxValue", _lightIntensityMaxValue.ToString());
#endif
        }

        private void LevelLoaded(int level)
        {
#if HONEYSELECT
            if (level == 3)
#elif KOIKATSU
            if (level == 1)
#endif
            {
                this.SpawnUI();
            }
        }

        public void OnUpdate()
        {
            Light lastTarget = this._target;

            if (Studio.Studio.Instance == null)
                return;
            TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
            if (treeNodeObject != null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                {
                    OCILight selected = info as OCILight;
                    this._target = selected?.light;
                }
                else
                    this._target = null;
            }
            else
                this._target = null;

            if (lastTarget != this._target)
                this.UpdateUI();

#if KOIKATSU
            this._sceneInfoLightCount.SetValue(Studio.Studio.Instance.sceneInfo, 0); // Doing this here because patching didn't work for some reason
            
#endif
            if (this._lastEnabled != this._charaLightUI.gameObject.activeInHierarchy)
                this.UpdateGlobalUI();
            this._lastEnabled = this._charaLightUI.gameObject.activeInHierarchy;
        }
        #endregion

        #region Private Methods
        private void SpawnUI()
        {
            RectTransform parent = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/02_Light").transform as RectTransform;
            this._imageDirectional = parent.Find("Image Directional") as RectTransform;
            this._imagePoint = parent.Find("Image Point") as RectTransform;
            this._imageSpot = parent.Find("Image Spot") as RectTransform;
#if HONEYSELECT
            AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.Resources.LightingEditorResources);
#elif KOIKATSU
            AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.ResourcesKOI.LightingEditorResourcesKOI);
#endif
            this._container = GameObject.Instantiate(bundle.LoadAsset<GameObject>("Container")).GetComponent<RectTransform>();
            this._container.SetParent(parent);
            this._container.localPosition = Vector3.zero;
            this._container.localScale = Vector3.one;
            this._container.anchorMax = new Vector2(this._imageDirectional.rect.width, this._container.anchorMax.y);

            Slider lightIntensity = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/02_Light/Slider Intensity").GetComponent<Slider>();
            lightIntensity.minValue = 0f;
            lightIntensity.maxValue = _lightIntensityMaxValue;

            this._shadowToggle = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/02_Light/Toggle Shadow").GetComponent<Toggle>();
            this._shadowToggle.onValueChanged.AddListener(this.ShadowTypeChanged);

            this._bounceIntesitySlider = this._container.Find("Bounce Intensity/Slider").GetComponent<Slider>();
            this._bounceIntesitySlider.onValueChanged.AddListener(this.BounceIntensityChanged);
            this._bounceIntensityInputField = this._container.Find("Bounce Intensity/InputField").GetComponent<InputField>();
            this._bounceIntensityInputField.onEndEdit.AddListener(this.BounceIntensityChanged);
            this._container.Find("Bounce Intensity/Button").GetComponent<Button>().onClick.AddListener(this.ResetBounceIntensity);

            this._shadowContainer = this._container.Find("Shadow Container") as RectTransform;

            this._shadowStrengthSlider = this._container.Find("Shadow Container/Strength/Slider").GetComponent<Slider>();
            this._shadowStrengthSlider.onValueChanged.AddListener(this.ShadowStrengthChanged);
            this._shadowStrengthInputField = this._container.Find("Shadow Container/Strength/InputField").GetComponent<InputField>();
            this._shadowStrengthInputField.onEndEdit.AddListener(this.ShadowStrengthChanged);
            this._container.Find("Shadow Container/Strength/Button").GetComponent<Button>().onClick.AddListener(this.ResetShadowStrength);

#if KOIKATSU
            this._shadowResolutionDropdown = this._container.Find("Shadow Container/Resolution/Dropdown").GetComponent<Dropdown>();
            this._shadowResolutionDropdown.onValueChanged.AddListener(this.ShadowResolutionChanged);
#endif

            this._shadowBiasSlider = this._container.Find("Shadow Container/Bias/Slider").GetComponent<Slider>();
            this._shadowBiasSlider.onValueChanged.AddListener(this.ShadowBiasChanged);
            this._shadowBiasInputField = this._container.Find("Shadow Container/Bias/InputField").GetComponent<InputField>();
            this._shadowBiasInputField.onEndEdit.AddListener(this.ShadowBiasChanged);
            this._container.Find("Shadow Container/Bias/Button").GetComponent<Button>().onClick.AddListener(this.ResetShadowBias);

            this._normalBiasSlider = this._container.Find("Shadow Container/Normal Bias/Slider").GetComponent<Slider>();
            this._normalBiasSlider.onValueChanged.AddListener(this.NormalBiasChanged);
            this._normalBiasInputField = this._container.Find("Shadow Container/Normal Bias/InputField").GetComponent<InputField>();
            this._normalBiasInputField.onEndEdit.AddListener(this.NormalBiasChanged);
            this._container.Find("Shadow Container/Normal Bias/Button").GetComponent<Button>().onClick.AddListener(this.ResetNormalBias);

            this._shadowNearPlaneSlider = this._container.Find("Shadow Container/Shadow Near Plane/Slider").GetComponent<Slider>();
            this._shadowNearPlaneSlider.onValueChanged.AddListener(this.ShadowNearPlaneChanged);
            this._shadowNearPlaneInputField = this._container.Find("Shadow Container/Shadow Near Plane/InputField").GetComponent<InputField>();
            this._shadowNearPlaneInputField.onEndEdit.AddListener(this.ShadowNearPlaneChanged);
            this._container.Find("Shadow Container/Shadow Near Plane/Button").GetComponent<Button>().onClick.AddListener(this.ResetShadowNearPlane);

            this._cullingMaskFoldout = this._container.Find("Culling Mask/Toggle").GetComponent<Toggle>();
            this._cullingMaskFoldout.onValueChanged.AddListener(this.CullingMaskFoldoutChanged);

            this._cullingMaskContainer = this._container.Find("Culling Mask Container") as RectTransform;
            GameObject layerTemplate = this._container.Find("Culling Mask Container/Layer Template").gameObject;
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                    continue;
                RectTransform display = GameObject.Instantiate(layerTemplate).transform as RectTransform;
                display.SetParent(this._cullingMaskContainer);
                display.localPosition = Vector3.zero;
                display.localScale = Vector3.one;

                display.GetComponentInChildren<Text>().text = layerName;

                Toggle toggle = display.GetComponentInChildren<Toggle>();
                int i1 = i;
                toggle.onValueChanged.AddListener((b) => this.CullingMaskLayerChanged(i1));
                this._cullingMaskLayers.Add(i, toggle);
            }

            layerTemplate.gameObject.SetActive(false);

#if HONEYSELECT
            parent = GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_CameraLight").transform as RectTransform;
#elif KOIKATSU
            parent = GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light").transform as RectTransform;
#endif
            this._charaLightUI = parent;
            RectTransform globalContainer = GameObject.Instantiate(bundle.LoadAsset<GameObject>("Global Container")).GetComponent<RectTransform>();
            globalContainer.SetParent(parent);
            globalContainer.localPosition = Vector3.zero;
            globalContainer.localScale = Vector3.one;
#if HONEYSELECT
            globalContainer.anchoredPosition = new Vector2(-globalContainer.rect.width / 2, -88f);
#elif KOIKATSU
            globalContainer.anchoredPosition = new Vector2(-95f, -110f);
#endif

#if HONEYSELECT
            this._shadowDistanceSlider = globalContainer.Find("Shadow Distance/Slider").GetComponent<Slider>();
            this._shadowDistanceSlider.onValueChanged.AddListener(this.ShadowDistanceChanged);
            this._shadowDistanceInputField = globalContainer.Find("Shadow Distance/InputField").GetComponent<InputField>();
            this._shadowDistanceInputField.onEndEdit.AddListener(this.ShadowDistanceChanged);
            globalContainer.Find("Shadow Distance/Button").GetComponent<Button>().onClick.AddListener(this.ResetShadowDistance);

            this._shadowDistanceSlider.value = QualitySettings.shadowDistance;
            this._shadowDistanceInputField.text = QualitySettings.shadowDistance.ToString("0.00");
#elif KOIKATSU
            this._charaLightToggle = globalContainer.Find("Enabled/Toggle").GetComponent<Toggle>();
            this._charaLightToggle.onValueChanged.AddListener(this.CharaLightToggled);

            this._ambientIntensitySlider = globalContainer.Find("Ambient Intensity/Slider").GetComponent<Slider>();
            this._ambientIntensitySlider.onValueChanged.AddListener(this.AmbientIntensityChanged);
            this._ambientIntensityInputField = globalContainer.Find("Ambient Intensity/InputField").GetComponent<InputField>();
            this._ambientIntensityInputField.onEndEdit.AddListener(this.AmbientIntensityChanged);
            globalContainer.Find("Ambient Intensity/Button").GetComponent<Button>().onClick.AddListener(this.ResetAmbientIntensity);

            this._ambientModeDropdown = globalContainer.Find("Ambient Mode/Dropdown").GetComponent<Dropdown>();
            this._ambientModeDropdown.onValueChanged.AddListener(this.AmbientModeChanged);

            this._ambientLightImage = globalContainer.Find("Color/RawImage").GetComponent<RawImage>();
            this._ambientLightImage.GetComponent<Button>().onClick.AddListener(() =>
            {
                Studio.Studio.Instance.colorPalette.Setup("Ambient Light", RenderSettings.ambientLight, this.AmbientLightChanged, false);
            });
            this._colorContainer = this._ambientLightImage.transform.parent as RectTransform;

            this._skyColorImage = globalContainer.Find("Trilight Color Container/Sky Color/RawImage").GetComponent<RawImage>();
            this._skyColorImage.GetComponent<Button>().onClick.AddListener(() =>
            {
                Studio.Studio.Instance.colorPalette.Setup("Sky Color", RenderSettings.ambientSkyColor, this.SkyColorChanged, false);
            });
            this._trilightColorContainer = this._skyColorImage.transform.parent.parent as RectTransform;

            this._equatorColorImage = globalContainer.Find("Trilight Color Container/Equator Color/RawImage").GetComponent<RawImage>();
            this._equatorColorImage.GetComponent<Button>().onClick.AddListener(() =>
            {
                Studio.Studio.Instance.colorPalette.Setup("Equator Color", RenderSettings.ambientEquatorColor, this.EquatorColorChanged, false);
            });

            this._groundColorImage = globalContainer.Find("Trilight Color Container/Ground Color/RawImage").GetComponent<RawImage>();
            this._groundColorImage.GetComponent<Button>().onClick.AddListener(() =>
            {
                Studio.Studio.Instance.colorPalette.Setup("Equator Color", RenderSettings.ambientGroundColor, this.GroundColorChanged, false);
            });

            this._reflectionBouncesSlider = globalContainer.Find("Reflection Bounces/Slider").GetComponent<Slider>();
            this._reflectionBouncesSlider.onValueChanged.AddListener(this.ReflectionBouncesChanged);
            this._reflectionBouncesInputField = globalContainer.Find("Reflection Bounces/InputField").GetComponent<InputField>();
            this._reflectionBouncesInputField.onEndEdit.AddListener(this.ReflectionBouncesChanged);
            globalContainer.Find("Reflection Bounces/Button").GetComponent<Button>().onClick.AddListener(this.ResetReflectionBounces);

            this._reflectionIntensitySlider = globalContainer.Find("Reflection Intensity/Slider").GetComponent<Slider>();
            this._reflectionIntensitySlider.onValueChanged.AddListener(this.ReflectionIntensityChanged);
            this._reflectionIntensityInputField = globalContainer.Find("Reflection Intensity/InputField").GetComponent<InputField>();
            this._reflectionIntensityInputField.onEndEdit.AddListener(this.ReflectionIntensityChanged);
            globalContainer.Find("Reflection Intensity/Button").GetComponent<Button>().onClick.AddListener(this.ResetReflectionIntensity);
#endif
            bundle.Unload(true);
        }

        private void UpdateUI()
        {
            if (this._target == null)
                return;
            if (this._imageDirectional.gameObject.activeSelf)
                this._container.anchoredPosition = new Vector2(0f, -this._imageDirectional.rect.height);
            if (this._imagePoint.gameObject.activeSelf)
                this._container.anchoredPosition = new Vector2(0f, -this._imagePoint.rect.height);
            if (this._imageSpot.gameObject.activeSelf)
                this._container.anchoredPosition = new Vector2(0f, -this._imageSpot.rect.height);

            this._bounceIntesitySlider.value = this._target.bounceIntensity;
            this._bounceIntensityInputField.text = this._target.bounceIntensity.ToString("0.000");
            this.ShadowTypeChanged(false);
            this._shadowStrengthSlider.value = this._target.shadowStrength;
            this._shadowStrengthInputField.text = this._target.shadowStrength.ToString("0.000");
#if KOIKATSU
            this._shadowResolutionDropdown.value = ((int)this._target.shadowResolution) + 1;
#endif
            this._shadowBiasSlider.value = this._target.shadowBias;
            this._shadowBiasInputField.text = this._target.shadowBias.ToString("0.000");
            this._normalBiasSlider.value = this._target.shadowNormalBias;
            this._normalBiasInputField.text = this._target.shadowNormalBias.ToString("0.000");
            this._shadowNearPlaneSlider.value = this._target.shadowNearPlane;
            this._shadowNearPlaneInputField.text = this._target.shadowNearPlane.ToString("0.000");
            this.CullingMaskFoldoutChanged(false);
            foreach (KeyValuePair<int, Toggle> pair in this._cullingMaskLayers)
                pair.Value.isOn = (this._target.cullingMask & (1 << pair.Key)) != 0;
        }

        private void UpdateGlobalUI()
        {
            if (this._charaLightUI == null || this._charaLightUI.gameObject.activeInHierarchy == false)
                return;
#if HONEYSELECT
            this._shadowDistanceSlider.value = QualitySettings.shadowDistance;
            this._shadowDistanceInputField.text = QualitySettings.shadowDistance.ToString("0.00");
#elif KOIKATSU
            this._charaLightToggle.isOn = ((Light)Studio.Studio.Instance.cameraLightCtrl.GetPrivate("lightChara").GetPrivate("light")).enabled;

            this._ambientIntensitySlider.value = RenderSettings.ambientIntensity;
            this._ambientIntensityInputField.text = RenderSettings.ambientIntensity.ToString("0.000");

            this._ambientLightImage.color = RenderSettings.ambientLight;
            this._skyColorImage.color = RenderSettings.ambientSkyColor;
            this._equatorColorImage.color = RenderSettings.ambientEquatorColor;
            this._groundColorImage.color = RenderSettings.ambientGroundColor;

            this._reflectionBouncesSlider.value = RenderSettings.reflectionBounces;
            this._reflectionBouncesInputField.text = RenderSettings.reflectionBounces.ToString("0.000");

            this._reflectionIntensitySlider.value = RenderSettings.reflectionIntensity;
            this._reflectionIntensityInputField.text = RenderSettings.reflectionIntensity.ToString("0.000");
            this.AmbientModeChanged(0);
#endif
        }
#if KOIKATSU
        private void OnSceneLoad(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data[_extSaveKey]);
            this.OnSceneLoad(path, doc.FirstChild);
        }

        private void OnSceneImport(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data[_extSaveKey]);
            this.OnSceneImport(path, doc.FirstChild);
        }

        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter writer = new XmlTextWriter(stringWriter))
            {
                writer.WriteStartElement("lightingEditor");
                this.OnSceneSave(path, writer);
                writer.WriteEndElement();
                PluginData data = new PluginData();
                data.version = _saveVersion;
                data.data.Add(_extSaveKey, stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }
        }
#endif

        private void OnSceneLoad(string path, XmlNode node)
        {
            Studio.Studio.Instance.ExecuteDelayed(() =>
            {
                this.LoadGeneric(node, new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList());
            }, 3);
            if (node != null)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    switch (childNode.Name)
                    {
                        case "shadowDistance":
                            QualitySettings.shadowDistance = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                            break;
                    }
                }
            }
        }

        private void OnSceneImport(string path, XmlNode node)
        {
            Dictionary<int, ObjectCtrlInfo> toIgnore = new Dictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            Studio.Studio.Instance.ExecuteDelayed(() =>
            {
                this.LoadGeneric(node, Studio.Studio.Instance.dicObjectCtrl.Where(e => toIgnore.ContainsKey(e.Key) == false).OrderBy(e => SceneInfo_Import_Patches._newToOldKeys[e.Key]).ToList());
            }, 3);
        }

        private void OnSceneSave(string path, XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteAttributeString("version", LightingEditor.versionNum);
            xmlWriter.WriteStartElement("shadowDistance");
            xmlWriter.WriteAttributeString("value", XmlConvert.ToString(QualitySettings.shadowDistance));
            xmlWriter.WriteEndElement();
            SortedDictionary<int, ObjectCtrlInfo> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
            {
                OCILight ociLight = kvp.Value as OCILight;
                if (ociLight != null)
                {
                    xmlWriter.WriteStartElement("lightInfo");
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));

                    this.SaveLight(ociLight.light, xmlWriter);

                    xmlWriter.WriteEndElement();
                }
            }
        }


        private void LoadGeneric(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            if (node == null)
                return;
            string v = node.Attributes["version"].Value;
            node = node.CloneNode(true);
            int i = 0;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "lightInfo":
                        OCILight ociLight = null;
                        while (i < dic.Count && (ociLight = dic[i].Value as OCILight) == null)
                            ++i;
                        if (i == dic.Count)
                            break;
                        this.LoadLight(ociLight.light, childNode);
                        ++i;
                        break;
                }
            }
        }

        private void SaveLight(Light light, XmlTextWriter xmlWriter)
        {
            //xmlWriter.WriteAttributeString("bounceIntensity", XmlConvert.ToString(light.bounceIntensity));
#if KOIKATSU
            xmlWriter.WriteAttributeString("shadowResolution", XmlConvert.ToString((int)light.shadowResolution));
#endif
            xmlWriter.WriteAttributeString("shadowStrength", XmlConvert.ToString(light.shadowStrength));
            xmlWriter.WriteAttributeString("shadowBias", XmlConvert.ToString(light.shadowBias));
            xmlWriter.WriteAttributeString("shadowNormalBias", XmlConvert.ToString(light.shadowNormalBias));
            xmlWriter.WriteAttributeString("shadowNearPlane", XmlConvert.ToString(light.shadowNearPlane));
            xmlWriter.WriteAttributeString("cullingMask", XmlConvert.ToString(light.cullingMask));
        }

        private void LoadLight(Light light, XmlNode node)
        {
            //light.bounceIntensity = XmlConvert.ToSingle(node.Attributes["bounceIntensity"].Value);
#if KOIKATSU
            light.shadowResolution = (LightShadowResolution)XmlConvert.ToInt32(node.Attributes["shadowResolution"].Value);
#endif
            light.shadowStrength = XmlConvert.ToSingle(node.Attributes["shadowStrength"].Value);
            light.shadowBias = XmlConvert.ToSingle(node.Attributes["shadowBias"].Value);
            light.shadowNormalBias = XmlConvert.ToSingle(node.Attributes["shadowNormalBias"].Value);
            light.shadowNearPlane = XmlConvert.ToSingle(node.Attributes["shadowNearPlane"].Value);
            light.cullingMask = XmlConvert.ToInt32(node.Attributes["cullingMask"].Value);
        }
#endregion

#region Callbacks
        private void ShadowTypeChanged(bool b)
        {
            this._shadowContainer.gameObject.SetActive(this._shadowToggle.isOn);
        }
        private void BounceIntensityChanged(float f)
        {
            this._target.bounceIntensity = this._bounceIntesitySlider.value;
            this._bounceIntensityInputField.text = this._target.bounceIntensity.ToString("0.000");
        }
        private void BounceIntensityChanged(string s)
        {
            if (float.TryParse(this._bounceIntensityInputField.text, out float value))
                this._target.bounceIntensity = value;
            this._bounceIntesitySlider.value = this._target.bounceIntensity;
            this._bounceIntensityInputField.text = this._target.bounceIntensity.ToString("0.000");
        }
        private void ResetBounceIntensity()
        {
            this._target.bounceIntensity = 1f;
            this._bounceIntesitySlider.value = this._target.bounceIntensity;
            this._bounceIntensityInputField.text = this._target.bounceIntensity.ToString("0.000");
        }
        private void ShadowStrengthChanged(float f)
        {
            this._target.shadowStrength = this._shadowStrengthSlider.value;
            this._shadowStrengthInputField.text = this._target.shadowStrength.ToString("0.000");
        }
        private void ShadowStrengthChanged(string s)
        {
            if (float.TryParse(this._shadowStrengthInputField.text, out float value))
                this._target.shadowStrength = value;
            this._shadowStrengthSlider.value = this._target.shadowStrength;
            this._shadowStrengthInputField.text = this._target.shadowStrength.ToString("0.000");
        }
        private void ResetShadowStrength()
        {
            this._target.shadowStrength = 0.8f;
            this._shadowStrengthSlider.value = this._target.shadowStrength;
            this._shadowStrengthInputField.text = this._target.shadowStrength.ToString("0.000");
        }
#if KOIKATSU
        private void ShadowResolutionChanged(int i)
        {
            this._target.shadowResolution = (LightShadowResolution)(this._shadowResolutionDropdown.value - 1);
        }
#endif
        private void ShadowBiasChanged(float f)
        {
            this._target.shadowBias = this._shadowBiasSlider.value;
            this._shadowBiasInputField.text = this._target.shadowBias.ToString("0.0000");
        }
        private void ShadowBiasChanged(string s)
        {
            if (float.TryParse(this._shadowBiasInputField.text, out float value))
                this._target.shadowBias = value;
            this._shadowBiasSlider.value = this._target.shadowBias;
            this._shadowBiasInputField.text = this._target.shadowBias.ToString("0.0000");
        }
        private void ResetShadowBias()
        {
            this._target.shadowBias = 0.0015f;
            this._shadowBiasSlider.value = this._target.shadowBias;
            this._shadowBiasInputField.text = this._target.shadowBias.ToString("0.0000");
        }
        private void NormalBiasChanged(float f)
        {
            this._target.shadowNormalBias = this._normalBiasSlider.value;
            this._normalBiasInputField.text = this._target.shadowNormalBias.ToString("0.000");
        }
        private void NormalBiasChanged(string s)
        {
            if (float.TryParse(this._normalBiasInputField.text, out float value))
                this._target.shadowNormalBias = value;
            this._normalBiasSlider.value = this._target.shadowNormalBias;
            this._normalBiasInputField.text = this._target.shadowNormalBias.ToString("0.000");
        }
        private void ResetNormalBias()
        {
            this._target.shadowNormalBias = 0.8f;
            this._normalBiasSlider.value = this._target.shadowNormalBias;
            this._normalBiasInputField.text = this._target.shadowNormalBias.ToString("0.000");
        }
        private void ShadowNearPlaneChanged(float f)
        {
            this._target.shadowNearPlane = this._shadowNearPlaneSlider.value;
            this._shadowNearPlaneInputField.text = this._target.shadowNearPlane.ToString("0.000");
        }
        private void ShadowNearPlaneChanged(string s)
        {
            if (float.TryParse(this._shadowNearPlaneInputField.text, out float value))
                this._target.shadowNearPlane = value;
            this._shadowNearPlaneSlider.value = this._target.shadowNearPlane;
            this._shadowNearPlaneInputField.text = this._target.shadowNearPlane.ToString("0.000");
        }
        private void ResetShadowNearPlane()
        {
            this._target.shadowNearPlane = 0.1f;
            this._shadowNearPlaneSlider.value = this._target.shadowNearPlane;
            this._shadowNearPlaneInputField.text = this._target.shadowNearPlane.ToString("0.000");
        }
        private void CullingMaskFoldoutChanged(bool b)
        {
            this._cullingMaskContainer.gameObject.SetActive(this._cullingMaskFoldout.isOn);
        }
        private void CullingMaskLayerChanged(int layer)
        {
            bool enabled = this._cullingMaskLayers[layer].isOn;
            this._target.cullingMask = (this._target.cullingMask & ~(1 << layer)) | ((enabled ? 1 : 0) << layer);
        }
#if HONEYSELECT
        private void ShadowDistanceChanged(float f)
        {
            QualitySettings.shadowDistance = this._shadowDistanceSlider.value;
            this._shadowDistanceInputField.text = QualitySettings.shadowDistance.ToString("0.00");
        }
        private void ShadowDistanceChanged(string s)
        {
            QualitySettings.shadowDistance = float.Parse(this._shadowDistanceInputField.text);
            this._shadowDistanceSlider.value = QualitySettings.shadowDistance;
            this._shadowDistanceInputField.text = QualitySettings.shadowDistance.ToString("0.00");
        }
        private void ResetShadowDistance()
        {
            QualitySettings.shadowDistance = this._defaultShadowDistance;
            this._shadowDistanceSlider.value = QualitySettings.shadowDistance;
            this._shadowDistanceInputField.text = QualitySettings.shadowDistance.ToString("0.00");
        }

#elif KOIKATSU
        private void CharaLightToggled(bool b)
        {
            ((Light)Studio.Studio.Instance.cameraLightCtrl.GetPrivate("lightChara").GetPrivate("light")).enabled = this._charaLightToggle.isOn;
        }

        private void AmbientIntensityChanged(float f)
        {
            RenderSettings.ambientIntensity = this._ambientIntensitySlider.value;
            this._ambientIntensityInputField.text = RenderSettings.ambientIntensity.ToString("0.000");
        }
        private void AmbientIntensityChanged(string s)
        {
            RenderSettings.ambientIntensity = float.Parse(this._ambientIntensityInputField.text);
            this._ambientIntensitySlider.value = RenderSettings.ambientIntensity;
            this._ambientIntensityInputField.text = RenderSettings.ambientIntensity.ToString("0.000");
        }
        private void ResetAmbientIntensity()
        {
            RenderSettings.ambientIntensity = 1.5f;
            this._ambientIntensitySlider.value = RenderSettings.ambientIntensity;
            this._ambientIntensityInputField.text = RenderSettings.ambientIntensity.ToString("0.000");
        }

        private void AmbientModeChanged(int i)
        {
            RenderSettings.ambientMode = ((AmbientMode[])Enum.GetValues(typeof(AmbientMode)))[this._ambientModeDropdown.value];
            this._colorContainer.gameObject.SetActive(RenderSettings.ambientMode != AmbientMode.Custom && RenderSettings.ambientMode != AmbientMode.Trilight);
            this._trilightColorContainer.gameObject.SetActive(RenderSettings.ambientMode == AmbientMode.Trilight);
        }

        private void AmbientLightChanged(Color c)
        {
            this._ambientLightImage.color = c;
            RenderSettings.ambientLight = c;
        }

        private void SkyColorChanged(Color c)
        {
            this._skyColorImage.color = c;
            RenderSettings.ambientSkyColor = c;
        }

        private void EquatorColorChanged(Color c)
        {
            this._equatorColorImage.color = c;
            RenderSettings.ambientEquatorColor = c;
        }

        private void GroundColorChanged(Color c)
        {
            this._groundColorImage.color = c;
            RenderSettings.ambientGroundColor = c;
        }

        private void ReflectionBouncesChanged(float f)
        {
            RenderSettings.reflectionBounces = Mathf.RoundToInt(this._reflectionBouncesSlider.value);
            this._reflectionBouncesInputField.text = RenderSettings.reflectionBounces.ToString("0.000");
        }
        private void ReflectionBouncesChanged(string s)
        {
            RenderSettings.reflectionBounces = int.Parse(this._ambientIntensityInputField.text);
            this._reflectionBouncesSlider.value = RenderSettings.reflectionBounces;
            this._reflectionBouncesInputField.text = RenderSettings.reflectionBounces.ToString("0.000");
        }
        private void ResetReflectionBounces()
        {
            RenderSettings.reflectionBounces = 1;
            this._reflectionBouncesSlider.value = RenderSettings.reflectionBounces;
            this._reflectionBouncesInputField.text = RenderSettings.reflectionBounces.ToString("0.000");
        }

        private void ReflectionIntensityChanged(float f)
        {
            RenderSettings.reflectionIntensity = this._reflectionIntensitySlider.value;
            this._reflectionIntensityInputField.text = RenderSettings.reflectionIntensity.ToString("0.000");
        }
        private void ReflectionIntensityChanged(string s)
        {
            RenderSettings.reflectionIntensity = float.Parse(this._reflectionIntensityInputField.text);
            this._reflectionIntensitySlider.value = RenderSettings.reflectionIntensity;
            this._reflectionIntensityInputField.text = RenderSettings.reflectionIntensity.ToString("0.000");
        }
        private void ResetReflectionIntensity()
        {
            RenderSettings.reflectionIntensity = 0.7f;
            this._reflectionIntensitySlider.value = RenderSettings.reflectionIntensity;
            this._reflectionIntensityInputField.text = RenderSettings.reflectionIntensity.ToString("0.000");
        }
#endif
        #endregion

        #region Patches
        [HarmonyPatch(typeof(MPLightCtrl), "OnValueChangeIntensity", new[] { typeof(float) })]
        public class MPLightCtrl_OnValueChangeIntensity_Patches
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    if (set == false && inst.opcode == OpCodes.Ldc_R4 && instructionsList[i + 1].opcode == OpCodes.Ldc_R4)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);
                        yield return new CodeInstruction(OpCodes.Ldc_R4, (float)_lightIntensityMaxValue);
                        ++i;
                        set = true;
                    }
                    else
                        yield return inst;
                }
            }
        }

        [HarmonyPatch(typeof(MPLightCtrl), "OnEndEditIntensity", new[] { typeof(string) })]
        public class MPLightCtrl_OnEndEditIntensity_Patches
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    if (set == false && inst.opcode == OpCodes.Ldc_R4 && instructionsList[i + 1].opcode == OpCodes.Ldc_R4)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);
                        yield return new CodeInstruction(OpCodes.Ldc_R4, (float)_lightIntensityMaxValue);
                        ++i;
                        set = true;
                    }
                    else
                        yield return inst;
                }
            }
        }

        [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
        public class Studio_Duplicate_Patches
        {
            public static void Postfix(Studio.Studio __instance)
            {
                __instance.ExecuteDelayed(() =>
                {
                    foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
                    {
                        ObjectCtrlInfo src;
                        if (__instance.dicObjectCtrl.TryGetValue(pair.Value, out src) == false)
                            continue;
                        OCILight source = src as OCILight;
                        if (source == null)
                            continue;
                        ObjectCtrlInfo dest;
                        if (__instance.dicObjectCtrl.TryGetValue(pair.Key, out dest) == false)
                            continue;
                        OCILight destination = dest as OCILight;
                        if (destination == null)
                            continue;
                        destination.light.shadowStrength = source.light.shadowStrength;
                        destination.light.shadowBias = source.light.shadowBias;
                        destination.light.shadowNormalBias = source.light.shadowNormalBias;
                        destination.light.shadowNearPlane = source.light.shadowNearPlane;
                        destination.light.cullingMask = source.light.cullingMask;
                    }
                }, 5);
            }
        }

        [HarmonyPatch(typeof(ObjectInfo), "Load", new[] { typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool) })]
        internal static class ObjectInfo_Load_Patches
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (set == false && instructionsList[i + 1].opcode == OpCodes.Pop)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(ObjectInfo_Load_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                        set = true;
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
        private static class SceneInfo_Import_Patches //This is here because I fucked up the save format making it impossible to import scenes correctly
        {
            internal static readonly Dictionary<int, int> _newToOldKeys = new Dictionary<int, int>();

            private static void Prefix()
            {
                _newToOldKeys.Clear();
            }
        }

#if HONEYSELECT
        [HarmonyPatch(typeof(AddObjectLight), "Load", new[] { typeof(OILightInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        internal static class AddObjectLightt_Load_Patches
        {
            private static void Postfix(AddObjectLight __instance, OCILight __result)
            {
                __result.treeNodeObject.onVisible = (TreeNodeObject.OnVisibleFunc)Delegate.Combine(__result.treeNodeObject.onVisible, new TreeNodeObject.OnVisibleFunc(state =>
                {
                    __result.SetEnable(state);
                }));
            }
        }
#endif
        #endregion
    }
}
