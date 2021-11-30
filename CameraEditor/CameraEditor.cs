#if HONEYSELECT
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Harmony;
using IllusionPlugin;
using Studio;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;
#elif KOIKATSU
using BepInEx;
using UnityEngine.SceneManagement;
#endif

namespace CameraEditor
{
#if KOIKATSU
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.cameraeditor", Name: "CameraEditor", Version: CameraEditor.versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInProcess("CharaStudio")]
#endif
    public class CameraEditor :
#if HONEYSELECT
    IEnhancedPlugin
#elif KOIKATSU
    BaseUnityPlugin
#endif
    {
        public const string versionNum = "1.0.0";

        #region Private Variables
        private static CameraEditor _self;
        private ScrollRect _scrollRect;
        private GameObject _slotPrefab;
        private Button _addButton;
        private readonly RectTransform[] _defaultSlots = new RectTransform[10];
        private readonly List<RectTransform> _additionalDisplayedSlots = new List<RectTransform>();
        private readonly List<Studio.CameraControl.CameraData> _additionalSlots = new List<Studio.CameraControl.CameraData>();
        private Sprite _blankCameraIcon;
        private InputField _positionXField;
        private InputField _positionYField;
        private InputField _positionZField;
        private InputField _rotationXField;
        private InputField _rotationYField;
        private InputField _rotationZField;
        private InputField _distanceField;
        private Studio.CameraControl.CameraData _globalCameraData;
        private bool _optionsEnabled = false;
        private Coroutine _setPositionHandler;
        private Coroutine _setRotationHandler;
        private Coroutine _setDistanceHandler;
        private GameObject _optionsMenu;
        #endregion

        #region Properties
#if HONEYSELECT
        public string Name { get { return "CameraEditor"; }}
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }
#endif
        #endregion

        #region Unity Methods
#if HONEYSELECT
        public void OnApplicationStart()
#elif KOIKATSU
        void Awake()
#endif
        {
#if KOIKATSU
            SceneManager.sceneLoaded += this.SceneLoaded;
#endif
            _self = this;
            HSExtSave.HSExtSave.RegisterHandler("cameraEditor", null, null, this.OnSceneLoaded, null, this.OnSceneSaved, null, null);
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.illusionplugins.cameraeditor");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

#if HONEYSELECT
        public void OnApplicationQuit() {}
#endif

#if HONEYSELECT
        public void OnLevelWasLoaded(int level)
#elif KOIKATSU
        private void SceneLoaded(Scene scene, LoadSceneMode loadMode)
#endif 
        {
#if HONEYSELECT
            if (level == 3)
#elif KOIKATSU
            if (scene.buildIndex == 1)
#endif
            {
                UIUtility.Init();
                this.Init();
            }
        }

#if HONEYSELECT
        public void OnLateUpdate()
#elif KOIKATSU
        void LateUpdate()
#endif
        {
            if (this._optionsMenu != null && this._optionsMenu.activeSelf && this._positionXField != null)
            {
                if (this._positionXField.isFocused == false)
                    this._positionXField.text = this._globalCameraData.pos.x.ToString("0.000");
                if (this._positionYField.isFocused == false)
                    this._positionYField.text = this._globalCameraData.pos.y.ToString("0.000");
                if (this._positionZField.isFocused == false)
                    this._positionZField.text = this._globalCameraData.pos.z.ToString("0.000");
                if (this._rotationXField.isFocused == false)
                    this._rotationXField.text = this._globalCameraData.rotate.x.ToString("0.00");
                if (this._rotationYField.isFocused == false)
                    this._rotationYField.text = this._globalCameraData.rotate.y.ToString("0.00");
                if (this._rotationZField.isFocused == false)
                    this._rotationZField.text = this._globalCameraData.rotate.z.ToString("0.00");
                if (this._distanceField.isFocused == false)
                    this._distanceField.text = this._globalCameraData.distance.z.ToString("0.000");
            }
        }


#if HONEYSELECT
        public void OnLevelWasInitialized(int level) { }
        public void OnFixedUpdate() { }
        public void OnUpdate() { }
#endif
        #endregion

        #region Private Methods
        private void Init()
        {
            RectTransform container = (RectTransform)GameObject.Find("StudioScene/Canvas System Menu/02_Camera").transform;
            this._slotPrefab = container.GetChild(0).gameObject;
            this._scrollRect = UIUtility.CreateScrollView("Scroll", container);
            this._scrollRect.transform.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-56f - 40f, -56f), new Vector2(336f, -8f));
            this._scrollRect.vertical = false;
            GameObject.Destroy(this._scrollRect.GetComponent<Image>());
            GameObject.Destroy(this._scrollRect.horizontalScrollbar.gameObject);
            GameObject.Destroy(this._scrollRect.verticalScrollbar.gameObject);
            this._scrollRect.viewport.SetRect();
            this._scrollRect.viewport.GetComponent<Image>().sprite = null;
            this._scrollRect.scrollSensitivity *= -18f;
            this._scrollRect.content.SetRect(Vector2.zero, new Vector2(0f, 1f));
            for (int i = 0; i < 10; i++)
            {
                this._defaultSlots[i] = (RectTransform)container.Find(i.ToString("00"));
                this._defaultSlots[i].SetParent(this._scrollRect.content, true);
            }
            this._addButton = UIUtility.CreateButton("Add", this._scrollRect.content, "+");
            Text t = this._addButton.GetComponentInChildren<Text>();
            t.rectTransform.SetRect();
            t.alignByGeometry = true;
            t.color = Color.black;
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = 300;
            this._addButton.onClick.AddListener(() =>
            {
                this._additionalSlots.Add(new Studio.CameraControl.CameraData());
                this.UpdateUI();
            });

            AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.Resources.CameraEditorResources);
            this._blankCameraIcon = bundle.LoadAsset<Sprite>("icon");

            RectTransform transform = (RectTransform)GameObject.Instantiate(bundle.LoadAsset<GameObject>("CameraOptions")).transform;
            transform.SetParent(container);
            transform.localScale = Vector3.one;
            transform.anchoredPosition = new Vector2(-56 - 80f, -24);

            this._globalCameraData = (Studio.CameraControl.CameraData)Studio.Studio.Instance.cameraCtrl.GetPrivate("cameraData");

            this._optionsMenu = transform.Find("Menu").gameObject;

            Button onOffButton = transform.Find("Button").GetComponent<Button>();
            RawImage onOffImage = onOffButton.GetComponent<RawImage>();
            onOffButton.onClick.AddListener(() =>
            {
                this._optionsEnabled = !this._optionsEnabled;
                this._optionsMenu.SetActive(this._optionsEnabled);
                onOffImage.color = this._optionsEnabled ? Color.green : Color.white;
            });

            this._positionXField = transform.Find("Menu/Controls/Position/X").GetComponent<InputField>();
            this._positionXField.onEndEdit.AddListener(s =>
            {
                float result;
                if (this._positionXField.text.Length == 0)
                {
                    result = 0;
                    this.SetPosition(new Vector3(result, this._globalCameraData.pos.y, this._globalCameraData.pos.z));
                }
                else if (float.TryParse(this._positionXField.text, out result))
                    this.SetPosition(new Vector3(result, this._globalCameraData.pos.y, this._globalCameraData.pos.z));
            });
            this._positionXField.gameObject.AddComponent<OnScrollDispatcher>().onScroll += (eventData) =>
            {
                if (this._positionXField.isFocused)
                    return;
                if (eventData.scrollDelta.y > 0)
                    this.SetPosition(this._globalCameraData.pos + new Vector3(0.1f, 0f, 0f), 0.05f);
                else if (eventData.scrollDelta.y < 0)
                    this.SetPosition(this._globalCameraData.pos - new Vector3(0.1f, 0f, 0f), 0.05f);
            };

            this._positionYField = transform.Find("Menu/Controls/Position/Y").GetComponent<InputField>();
            this._positionYField.onEndEdit.AddListener(s =>
            {
                float result;
                if (this._positionYField.text.Length == 0)
                {
                    result = 0;
                    this.SetPosition(new Vector3(this._globalCameraData.pos.x, result, this._globalCameraData.pos.z));
                }
                else if (float.TryParse(this._positionYField.text, out result))
                    this.SetPosition(new Vector3(this._globalCameraData.pos.x, result, this._globalCameraData.pos.z));
            });
            this._positionYField.gameObject.AddComponent<OnScrollDispatcher>().onScroll += (eventData) =>
            {
                if (this._positionYField.isFocused)
                    return;
                if (eventData.scrollDelta.y > 0)
                    this.SetPosition(this._globalCameraData.pos + new Vector3(0f, 0.1f, 0f), 0.05f);
                else if (eventData.scrollDelta.y < 0)
                    this.SetPosition(this._globalCameraData.pos - new Vector3(0f, 0.1f, 0f), 0.05f);
            };

            this._positionZField = transform.Find("Menu/Controls/Position/Z").GetComponent<InputField>();
            this._positionZField.onEndEdit.AddListener(s =>
            {
                float result;
                if (this._positionZField.text.Length == 0)
                {
                    result = 0;
                    this.SetPosition(new Vector3(this._globalCameraData.pos.x, this._globalCameraData.pos.y, result));
                }
                else if (float.TryParse(this._positionZField.text, out result))
                    this.SetPosition(new Vector3(this._globalCameraData.pos.x, this._globalCameraData.pos.y, result));
            });
            this._positionZField.gameObject.AddComponent<OnScrollDispatcher>().onScroll += (eventData) =>
            {
                if (this._positionZField.isFocused)
                    return;
                if (eventData.scrollDelta.y > 0)
                    this.SetPosition(this._globalCameraData.pos + new Vector3(0f, 0f, 0.1f), 0.05f);
                else if (eventData.scrollDelta.y < 0)
                    this.SetPosition(this._globalCameraData.pos - new Vector3(0f, 0f, 0.1f), 0.05f);
            };

            Button resetPosButton = transform.Find("Menu/Controls/Position/Reset").GetComponent<Button>();
            resetPosButton.onClick.AddListener(() =>
            {
                this.SetPosition(Vector3.zero);
            });

            this._rotationXField = transform.Find("Menu/Controls/Rotation/X").GetComponent<InputField>();
            this._rotationXField.onEndEdit.AddListener(s =>
            {
                float result;
                if (this._rotationXField.text.Length == 0)
                {
                    result = 0;
                    this.SetRotation(new Vector3(result, this._globalCameraData.rotate.y, this._globalCameraData.rotate.z));
                }
                else if (float.TryParse(this._rotationXField.text, out result))
                    this.SetRotation(new Vector3(result, this._globalCameraData.rotate.y, this._globalCameraData.rotate.z));
            });
            this._rotationXField.gameObject.AddComponent<OnScrollDispatcher>().onScroll += (eventData) =>
            {
                if (this._rotationXField.isFocused)
                    return;
                if (eventData.scrollDelta.y > 0)
                    this.SetRotation(this._globalCameraData.rotate + new Vector3(1f, 0f, 0f), 0.05f);
                else if (eventData.scrollDelta.y < 0)
                    this.SetRotation(this._globalCameraData.rotate - new Vector3(1f, 0f, 0f), 0.05f);
            };

            this._rotationYField = transform.Find("Menu/Controls/Rotation/Y").GetComponent<InputField>();
            this._rotationYField.onEndEdit.AddListener(s =>
            {
                float result;
                if (this._rotationYField.text.Length == 0)
                {
                    result = 0;
                    this.SetRotation(new Vector3(this._globalCameraData.rotate.x, result, this._globalCameraData.rotate.z));
                }
                else if (float.TryParse(this._rotationYField.text, out result))
                    this.SetRotation(new Vector3(this._globalCameraData.rotate.x, result, this._globalCameraData.rotate.z));
            });
            this._rotationYField.gameObject.AddComponent<OnScrollDispatcher>().onScroll += (eventData) =>
            {
                if (this._rotationYField.isFocused)
                    return;
                if (eventData.scrollDelta.y > 0)
                    this.SetRotation(this._globalCameraData.rotate + new Vector3(0f, 1f, 0f), 0.05f);
                else if (eventData.scrollDelta.y < 0)
                    this.SetRotation(this._globalCameraData.rotate - new Vector3(0f, 1f, 0f), 0.05f);
            };
            this._rotationZField = transform.Find("Menu/Controls/Rotation/Z").GetComponent<InputField>();
            this._rotationZField.onEndEdit.AddListener(s =>
            {
                float result;
                if (this._rotationZField.text.Length == 0)
                {
                    result = 0;
                    this.SetRotation(new Vector3(this._globalCameraData.rotate.x, this._globalCameraData.rotate.y, result));
                }
                else if (float.TryParse(this._rotationZField.text, out result))
                    this.SetRotation(new Vector3(this._globalCameraData.rotate.x, this._globalCameraData.rotate.y, result));
            });
            this._rotationZField.gameObject.AddComponent<OnScrollDispatcher>().onScroll += (eventData) =>
            {
                if (this._rotationZField.isFocused)
                    return;
                if (eventData.scrollDelta.y > 0)
                    this.SetRotation(this._globalCameraData.rotate + new Vector3(0f, 0f, 1f), 0.05f);
                else if (eventData.scrollDelta.y < 0)
                    this.SetRotation(this._globalCameraData.rotate - new Vector3(0f, 0f, 1f), 0.05f);
            };

            Button resetRotButton = transform.Find("Menu/Controls/Rotation/Reset").GetComponent<Button>();
            resetRotButton.onClick.AddListener(() =>
            {
                this.SetRotation(Vector3.zero);
            });

            this._distanceField = transform.Find("Menu/Controls/Distance/Value").GetComponent<InputField>();
            this._distanceField.onEndEdit.AddListener(s =>
            {
                float result;
                if (this._distanceField.text.Length == 0)
                {
                    result = 0;
                    this.SetDistance(result);
                }
                else if (float.TryParse(this._distanceField.text, out result))
                    this.SetDistance(result);
                this._distanceField.text = this._globalCameraData.distance.z.ToString("0.000");
            });
            this._distanceField.gameObject.AddComponent<OnScrollDispatcher>().onScroll += (eventData) =>
            {
                if (this._distanceField.isFocused)
                    return;
                if (eventData.scrollDelta.y > 0)
                    this.SetDistance(this._globalCameraData.distance.z + 0.1f);
                else if (eventData.scrollDelta.y < 0)
                    this.SetDistance(this._globalCameraData.distance.z - 0.1f);
            };

            Button minusOneButton = transform.Find("Menu/Controls/Distance/MinusOne").GetComponent<Button>();
            minusOneButton.onClick.AddListener(() =>
            {
                this.SetDistance(this._globalCameraData.distance.z - 0.1f);
            });

            Button plusOneButton = transform.Find("Menu/Controls/Distance/PlusOne").GetComponent<Button>();
            plusOneButton.onClick.AddListener(() =>
            {
                this.SetDistance(this._globalCameraData.distance.z + 0.1f);
            });

            bundle.Unload(false);

            this.UpdateUI();
        }

        private void UpdateUI()
        {
            int i = 0;
            foreach (RectTransform rectTransform in this._defaultSlots)
            {
                rectTransform.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40 * i, -48f), new Vector2(40f * i + 32f, 0));
                ++i;
            }
            int j = 0;
            for (; j < this._additionalSlots.Count; j++)
            {
                RectTransform rectTransform;
                if (j >= this._additionalDisplayedSlots.Count)
                {
                    rectTransform = (RectTransform)GameObject.Instantiate(this._slotPrefab).transform;
                    rectTransform.gameObject.name = (10 + j).ToString("00");
                    rectTransform.SetParent(this._scrollRect.content);
                    rectTransform.localScale = this._slotPrefab.transform.localScale;
                    Button saveButton = rectTransform.Find("Button Save").GetComponent<Button>();
                    saveButton.onClick = new Button.ButtonClickedEvent();
                    int j1 = j;
                    saveButton.onClick.AddListener(() => { this._additionalSlots[j1] = Studio.Studio.Instance.cameraCtrl.Export(); });
                    Button loadButton = rectTransform.Find("Button Load").GetComponent<Button>();
                    loadButton.onClick = new Button.ButtonClickedEvent();
                    loadButton.onClick.AddListener(() => { Studio.Studio.Instance.cameraCtrl.Import(this._additionalSlots[j1]); });
                    loadButton.GetComponent<Image>().sprite = this._blankCameraIcon;
                    Text t = UIUtility.CreateText("Number", loadButton.transform, (j + 11).ToString());
                    t.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, -1f), new Vector2(-5f, -12.8f));
                    t.color = new Color32(100, 99, 95, 255);
                    t.fontStyle = FontStyle.Bold;
                    t.alignByGeometry = true;
                    t.resizeTextForBestFit = true;
                    t.resizeTextMinSize = 4;
                    t.resizeTextMaxSize = 300;
                    t.alignment = TextAnchor.MiddleCenter;
                    this._additionalDisplayedSlots.Add(rectTransform);
                }
                else
                    rectTransform = this._additionalDisplayedSlots[j];
                rectTransform.gameObject.SetActive(true);
                rectTransform.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40 * i, -48f), new Vector2(40f * i + 32f, 0));
                ++i;
            }
            for (; j < this._additionalDisplayedSlots.Count; j++)
                this._additionalDisplayedSlots[j].gameObject.SetActive(false);
            this._addButton.transform.SetAsLastSibling();
            this._addButton.transform.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40 * i, -48f), new Vector2(40f * i + 32f, 0));
            ++i;
            this._scrollRect.content.sizeDelta = new Vector2(40 * i - 8f, this._scrollRect.content.sizeDelta.y);
        }

        private void SetPosition(Vector3 position, float time = 0.1f)
        {
            if (this._setPositionHandler != null)
                return;
                //this._scrollRect.StopCoroutine(this._setPositionHandler);
            this._setPositionHandler = this._scrollRect.StartCoroutine(this.LerpPosition(this._globalCameraData.pos, position, time));
        }

        private IEnumerator LerpPosition(Vector3 start, Vector3 end, float time = 0.1f)
        {
            float startTime = Time.unscaledTime;
            while (Time.unscaledTime - startTime < time)
            {
                yield return null;
                this._globalCameraData.pos = Vector3.Lerp(start, end, (Time.unscaledTime - startTime) / time);
            }
            this._globalCameraData.pos = end;
            this._setPositionHandler = null;
        }

        private void SetRotation(Vector3 rotation, float time = 0.1f)
        {
            if (this._setRotationHandler != null)
                return;
                //this._scrollRect.StopCoroutine(this._setRotationHandler);
            this._setRotationHandler = this._scrollRect.StartCoroutine(this.LerpRotation(this._globalCameraData.rotate, rotation, time));
        }

        private IEnumerator LerpRotation(Vector3 start, Vector3 end, float time = 0.1f)
        {
            float startTime = Time.unscaledTime;
            while (Time.unscaledTime - startTime < time)
            {
                yield return null;
                float percentage = (Time.unscaledTime - startTime) / time;
                this._globalCameraData.rotate = new Vector3(Mathf.LerpAngle(start.x, end.x, percentage), Mathf.LerpAngle(start.y, end.y, percentage), Mathf.LerpAngle(start.z, end.z, percentage));
            }
            this._globalCameraData.rotate = end;
            this._setRotationHandler = null;
        }

        private void SetDistance(float distance, float time = 0.05f)
        {
            if (distance > 0)
                distance = 0;
            if (this._setDistanceHandler != null)
                return;
                //this._scrollRect.StopCoroutine(this._setDistanceHandler);
            this._setDistanceHandler = this._scrollRect.StartCoroutine(this.LerpDistance(this._globalCameraData.distance.z, distance, time));
        }

        private IEnumerator LerpDistance(float start, float end, float time = 0.05f)
        {
            float startTime = Time.unscaledTime;
            while (Time.unscaledTime - startTime < time)
            {
                yield return null;
                this._globalCameraData.distance = new Vector3(this._globalCameraData.distance.x, this._globalCameraData.distance.y, Mathf.LerpAngle(start, end, (Time.unscaledTime - startTime) / time));
            }
            this._globalCameraData.distance = new Vector3(this._globalCameraData.distance.x, this._globalCameraData.distance.y, end);
            this._setDistanceHandler = null;
        }
        #endregion

        #region Saves
        private void OnSceneLoaded(string path, XmlNode node)
        {
            this._additionalSlots.Clear();
            if (node != null)
            {
                foreach (XmlNode cameraNode in node.ChildNodes)
                {
                    switch (cameraNode.Name)
                    {
                        case "cameraData":
                            Studio.CameraControl.CameraData data = new Studio.CameraControl.CameraData();
                            data.pos = new Vector3(
                                                   XmlConvert.ToSingle(cameraNode.Attributes["posX"].Value),
                                                   XmlConvert.ToSingle(cameraNode.Attributes["posY"].Value),
                                                   XmlConvert.ToSingle(cameraNode.Attributes["posZ"].Value)
                                                  );
                            data.rotate = new Vector3(
                                                      XmlConvert.ToSingle(cameraNode.Attributes["rotateX"].Value),
                                                      XmlConvert.ToSingle(cameraNode.Attributes["rotateY"].Value),
                                                      XmlConvert.ToSingle(cameraNode.Attributes["rotateZ"].Value)
                                                     );
                            data.distance = new Vector3(
                                                        XmlConvert.ToSingle(cameraNode.Attributes["distanceX"].Value),
                                                        XmlConvert.ToSingle(cameraNode.Attributes["distanceY"].Value),
                                                        XmlConvert.ToSingle(cameraNode.Attributes["distanceZ"].Value)
                                                       );
                            data.parse = XmlConvert.ToSingle(cameraNode.Attributes["parse"].Value);
                            this._additionalSlots.Add(data);
                            break;
                    }
                }
            }
            this.UpdateUI();
        }

        private void OnSceneSaved(string path, XmlTextWriter writer)
        {
            foreach (Studio.CameraControl.CameraData data in this._additionalSlots)
            {
                writer.WriteStartElement("cameraData");
                writer.WriteAttributeString("posX", XmlConvert.ToString(data.pos.x));
                writer.WriteAttributeString("posY", XmlConvert.ToString(data.pos.y));
                writer.WriteAttributeString("posZ", XmlConvert.ToString(data.pos.z));
                writer.WriteAttributeString("rotateX", XmlConvert.ToString(data.rotate.x));
                writer.WriteAttributeString("rotateY", XmlConvert.ToString(data.rotate.y));
                writer.WriteAttributeString("rotateZ", XmlConvert.ToString(data.rotate.z));
                writer.WriteAttributeString("distanceX", XmlConvert.ToString(data.distance.x));
                writer.WriteAttributeString("distanceY", XmlConvert.ToString(data.distance.y));
                writer.WriteAttributeString("distanceZ", XmlConvert.ToString(data.distance.z));
                writer.WriteAttributeString("parse", XmlConvert.ToString(data.parse)); //IS IT TOO HARD FOR YOU TO CALL THAT SHIT "FIELD OF VIEW" ILLUSION?!
                writer.WriteEndElement();
            }
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(SystemButtonCtrl), "OnSelectInitYes")]
        private static class SystemButtonCtrl_OnSelectInitYes_Patches
        {
            private static void Postfix(SystemButtonCtrl __instance)
            {
                _self._additionalSlots.Clear();
                _self.UpdateUI();
            }
        }
        #endregion

    }
}
