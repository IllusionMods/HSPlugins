using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using BepInEx;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
using Illusion.Extensions;

#endif
using Studio;
using ToolBox.Extensions;
using UnityEngine;


#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
using InstanceDict = System.Collections.Generic.Dictionary<FaceBlendShape, HSPE.AMModules.BlendShapesEditor>;
using InstancePair = System.Collections.Generic.KeyValuePair<FaceBlendShape, HSPE.AMModules.BlendShapesEditor>;
#elif PLAYHOME
using InstanceDict = System.Collections.Generic.Dictionary<Human, HSPE.AMModules.BlendShapesEditor>;
using InstancePair = System.Collections.Generic.KeyValuePair<Human, HSPE.AMModules.BlendShapesEditor>;
#endif

namespace HSPE.AMModules
{
    public class BlendShapesEditor : AdvancedModeModule
    {
        #region Constants
        private static readonly Dictionary<string, string> _skinnedMeshAliases = new Dictionary<string, string>
        {
#if HONEYSELECT || PLAYHOME
            {"cf_O_head", "Eyes / Mouth"},
            {"cf_O_ha", "Teeth"},
            {"cf_O_matuge", "Eyelashes"},
            {"cf_O_mayuge", "Eyebrows"},
            {"cf_O_sita", "Tongue"},
            {"cf_O_namida01", "Tears 1"},
            {"cf_O_namida02", "Tears 2"},

            {"cm_O_head", "Face"},
            {"cm_O_ha", "Teeth"},
            {"cm_O_mayuge", "Eyebrows"},
            {"cm_O_sita", "Tongue"},
            {"O_hige00", "Jaw"},
#elif KOIKATSU
            {"cf_O_face",  "Eyes / Mouth"},
            {"cf_O_tooth",  "Teeth"},
            {"cf_O_canine",  "Canines"},
            {"cf_O_eyeline",  "Upper Eyelashes"},
            {"cf_O_eyeline_low",  "Lower Eyelashes"},
            {"cf_O_mayuge",  "Eyebrows"},
            {"cf_Ohitomi_L",  "Left Eye White"},
            {"cf_Ohitomi_R",  "Right Eye White"},
            {"o_tang",  "Tongue"},
            {"cf_O_namida_L",  "Tears L"},
            {"cf_O_namida_M",  "Tears M"},
            {"cf_O_namida_S",  "Tears S"},
#elif AISHOUJO
            {"o_eyelashes",  "Eyelashes"},
            {"o_head",  "Head"},
            {"o_namida",  "Tears"},
            {"o_tang",  "Tongue"},
            {"o_tooth",  "Teeth"},
#elif HONEYSELECT2
            {"o_eyelashes",  "Eyelashes"},
            {"o_head",  "Head"},
            {"o_namida",  "Tears"},
            {"o_tang",  "Tongue"},
            {"o_tooth",  "Teeth"},
#endif
        };
        private static readonly string _presetsPath;
        #endregion

        #region Statics
        private static string[] _linkSuffixKeys = new string[] { "L", "R", "CL", "OP", "HALF" };
        private static InstanceDict _instanceByFaceBlendShape = new InstanceDict();
        private static string[] _presets = new string[0];
        internal static readonly Dictionary<string, string> _blendShapeAliases = new Dictionary<string, string>();
        private static readonly Dictionary<int, string> _femaleSeparators = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> _maleSeparators = new Dictionary<int, string>();
        private static readonly int _femaleEyesComponentsCount;
        private static readonly int _maleEyesComponentsCount;
        #endregion

        #region Private Types

        private class BlendShapeData
        {
            public float weight;
            public float originalWeight;
        }

        private class BlendLinkData
        {
            public SkinnedMeshRenderer renderer;
            public string blendName;
        }

#if HONEYSELECT || KOIKATSU
        [HarmonyPatch(typeof(FaceBlendShape), "LateUpdate")]
        private class FaceBlendShape_Patches
        {
            [HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
            public static void Postfix(FaceBlendShape __instance)
            {
                BlendShapesEditor editor;
                if (_instanceByFaceBlendShape.TryGetValue(__instance, out editor))
                    editor.FaceBlendShapeOnPostLateUpdate();
            }
        }
#elif PLAYHOME
        [HarmonyPatch(typeof(Human), "LateUpdate")]
        private class Human_Patches
        {
            [HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
            public static void Postfix(Human __instance)
            {
                BlendShapesEditor editor;
                if (_instanceByFaceBlendShape.TryGetValue(__instance, out editor))
                    editor.FaceBlendShapeOnPostLateUpdate();
            }
        }
#elif AISHOUJO || HONEYSELECT2
        [HarmonyPatch(typeof(FaceBlendShape), "OnLateUpdate")]
        private class FaceBlendShape_Patches
        {
            [HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
            public static void Postfix(FaceBlendShape __instance)
            {
                BlendShapesEditor editor;
                if (_instanceByFaceBlendShape.TryGetValue(__instance, out editor))
                    editor.FaceBlendShapeOnPostLateUpdate();
            }
        }
#endif

        #endregion

        #region Private Variables
        private class BlendRenderer
        {
            public string _fullPath;
            public Dictionary<int, string> _oriBlendIndex = new Dictionary<int, string>();
            public List<string> _nonMatchedOriBlendsNames = new List<string>();
            public Dictionary<string, string> _nonMatchBlendCorrection = new Dictionary<string, string>();
            public Dictionary<string, BlendShapeData> _dirtyBlends = new Dictionary<string, BlendShapeData>();
            public Dictionary<string, int> _blendIndics = new Dictionary<string, int>();
            public List<string> _blendNames = new List<string>();
            public Dictionary<string, int> _linkKeynames = new Dictionary<string, int>();
            public List<string> _linkKeynameList = new List<string>();
            public List<BlendRenderer> _linkedBlendRenderers = new List<BlendRenderer>();
            public SkinnedMeshRenderer _renderer;


            public void ClearOriginData()
            {
                _oriBlendIndex.Clear();
                _nonMatchedOriBlendsNames.Clear();
                _nonMatchBlendCorrection.Clear();
            }

            public void CreateLinkKeynames(bool female)
            {
                _linkKeynames.Clear();
                _linkKeynameList.Clear();

                if (_linkedBlendRenderers.Count > 0)
                {
                    int eyesComponentsCount = female ? _femaleEyesComponentsCount : _maleEyesComponentsCount;
                    int index = 0;
                    foreach (var blendName in _blendNames)
                    {
                        if (index >= eyesComponentsCount)
                        {
                            break;
                        }

                        string nameIndexStr = null;
                        {
                            int nameIndex = -1;
                            int nameIndexEnd = -1;

                            for (int i = 0; i < blendName.Length; i++)
                            {
                                if (nameIndex == -1)
                                {
                                    if (blendName[i] >= '0' && blendName[i] <= '9')
                                    {
                                        nameIndex = i;
                                    }
                                }
                                else
                                {
                                    if (blendName[i] < '0' || blendName[i] > '9')
                                    {
                                        nameIndexEnd = i - 1;
                                        break;
                                    }
                                }
                            }

                            if (nameIndex != -1 && nameIndexEnd != -1)
                            {
                                nameIndexStr = blendName.Substring(nameIndex, nameIndexEnd - nameIndex + 1);
                            }
                        }

                        int suffixPointIndex = blendName.LastIndexOf('_');
                        string mainKey = blendName.Substring(suffixPointIndex + 1).ToUpper();
                        if (_linkSuffixKeys.Contains(mainKey))
                        {
                            string prefix = blendName.Substring(0, suffixPointIndex);
                            suffixPointIndex = prefix.LastIndexOf('_');
                            mainKey = blendName.Substring(suffixPointIndex + 1).ToUpper();
                        }

                        if (nameIndexStr != null)
                        {
                            _linkKeynames.Add(mainKey + nameIndexStr, index);
                            _linkKeynameList.Add(mainKey + nameIndexStr);
                        }
                        else
                        {
                            _linkKeynames.Add(mainKey, index);
                            _linkKeynameList.Add(mainKey);
                        }

                        index++;
                    }
                }
            }

            public void ApplyLink(float weight, int index, bool nonDirty = false)
            {
                if (_linkedBlendRenderers.Count > 0 && index < _linkKeynameList.Count)
                {
                    string linkKeyname = null;

                    if (_oriBlendIndex.Count > 0)
                    {
                        linkKeyname = GetOriginBlendShapeName(index);

                        if(_blendIndics.TryGetValue(linkKeyname, out int blendIndex))
                        {
                            linkKeyname = _linkKeynameList[blendIndex];
                        }
                    }
                    else
                    {
                        linkKeyname = _linkKeynameList[index];
                    }

                    if (linkKeyname != null)
                    {
                        foreach (var linkedBlendRenderer in _linkedBlendRenderers)
                        {
                            if (linkedBlendRenderer._linkKeynames.TryGetValue(linkKeyname, out int linkIndex))
                            {
                                if (nonDirty)
                                {
                                    linkedBlendRenderer.NonDirty(linkedBlendRenderer._blendNames[linkIndex]);
                                }
                                else
                                {
                                    linkedBlendRenderer.SetBlendShapeWeight(linkIndex, weight);
                                }
                            }
                        }
                    }
                }
            }

            public void ClearDirty()
            {
                foreach (var currDirty in _dirtyBlends)
                {
                    _renderer.SetBlendShapeWeight(_blendIndics[currDirty.Key], currDirty.Value.originalWeight);
                }

                _dirtyBlends.Clear();
            }

            public void ApplyDirty()
            {
                foreach (var currDirty in _dirtyBlends)
                {
                    _renderer.SetBlendShapeWeight(_blendIndics[currDirty.Key], currDirty.Value.weight);
                }
            }

            public float GetBlendShapeWeight(int index)
            {
                float result = _renderer.GetBlendShapeWeight(index);

                if (_oriBlendIndex.Count > 0)
                {
                    string oriName = null;
                    if (_oriBlendIndex.TryGetValue(index, out oriName))
                    {
                        if (_blendIndics.TryGetValue(oriName, out int blendIndex))
                        {
                            result = _renderer.GetBlendShapeWeight(blendIndex);
                        }
                    }
                }

                return result;
            }

            private string GetOriginBlendShapeName(int index)
            {
                string blendName = null;

                if (_oriBlendIndex.TryGetValue(index, out blendName))
                {
                    if (_nonMatchBlendCorrection.TryGetValue(blendName, out string correctionName))
                    {
                        if (_blendIndics.ContainsKey(correctionName))
                        {
                            blendName = correctionName;
                        }
                        else
                        {
                            _nonMatchBlendCorrection.Remove(blendName);
                            blendName = null;
                        }
                    }
                    else
                    {
                        if (!_blendIndics.ContainsKey(blendName))
                        {
                            blendName = null;
                        }
                    }
                }
                else
                {
                    if (_blendNames.Count > index)
                    {
                        blendName = _blendNames[index];
                    }
                }

                return blendName;
            }

            public string GetBlendShapeName(int index)
            {
                string blendName = null;

                if (_oriBlendIndex.Count > 0)
                {
                    blendName = GetOriginBlendShapeName(index);
                }
                else
                {
                    if (_blendNames.Count > index)
                    {
                        blendName = _blendNames[index];
                    }
                }

                return blendName;
            }
            public BlendShapeData SetBlendShapeWeight(int index, float weight, bool denyOrigin = false)
            {
                BlendShapeData result = null;

                result = SetBlendDirty(index, denyOrigin);

                if (result != null)
                {
                    result.weight = weight;
                }

                return result;
            }

            public BlendShapeData SetBlendShapeWeight(string blendName, float weight)
            {
                BlendShapeData result = null;

                result = SetBlendDirty(blendName);

                if (result != null)
                {
                    result.weight = weight;
                }

                return result;
            }

            public BlendShapeData SetBlendDirty(int index, bool denyOrigin = false)
            {
                BlendShapeData blendShapeData = null;
                string blendName = null;

                if (_oriBlendIndex.Count > 0 && denyOrigin == false)
                {
                    if (_oriBlendIndex.TryGetValue(index, out blendName))
                    {
                        if (_nonMatchBlendCorrection.TryGetValue(blendName, out string correctionName))
                        {
                            if (_blendIndics.ContainsKey(correctionName))
                            {
                                blendName = correctionName;
                            }
                            else
                            {
                                _nonMatchBlendCorrection.Remove(blendName);
                                blendName = null;
                            }
                        }
                        else
                        {
                            if (!_blendIndics.ContainsKey(blendName))
                            {
                                blendName = null;
                            }
                        }
                    }
                    else
                    {
                        if (_blendNames.Count > index)
                        {
                            blendName = _blendNames[index];
                        }
                    }
                }
                else
                {
                    if (_blendNames.Count > index)
                    {
                        blendName = _blendNames[index];
                    }
                }

                if (blendName != null)
                {
                    if (_dirtyBlends.ContainsKey(blendName))
                    {
                        blendShapeData = _dirtyBlends[blendName];
                    }
                    else
                    {
                        blendShapeData = new BlendShapeData();
                        blendShapeData.originalWeight = _renderer.GetBlendShapeWeight(_blendIndics[blendName]);
                        blendShapeData.weight = blendShapeData.originalWeight;
                        _dirtyBlends.Add(blendName, blendShapeData);
                    }
                }

                return blendShapeData;
            }

            public BlendShapeData SetBlendDirty(string blendName)
            {
                BlendShapeData blendShapeData = null;

                if (_dirtyBlends.ContainsKey(blendName))
                {
                    blendShapeData = _dirtyBlends[blendName];
                }
                else
                {
                    if (_blendIndics.ContainsKey(blendName))
                    {
                        blendShapeData = new BlendShapeData();
                        blendShapeData.originalWeight = _renderer.GetBlendShapeWeight(_blendIndics[blendName]);
                        blendShapeData.weight = blendShapeData.originalWeight;
                        _dirtyBlends.Add(blendName, blendShapeData);
                    }
                }

                return blendShapeData;
            }

            public void NonDirty(string blendName)
            {
                if (_dirtyBlends.ContainsKey(blendName))
                {
                    _renderer.SetBlendShapeWeight(_renderer.sharedMesh.GetBlendShapeIndex(blendName), _dirtyBlends[blendName].originalWeight);
                    _dirtyBlends.Remove(blendName);
                }
            }
        };

        private Vector2 _skinnedMeshRenderersScroll;
        private Vector2 _blendShapesScroll;
        private Vector2 _nonMatchBlendRendererScroll;
        private Vector2 _nonMatchBlendScroll;
        private int _headlessReconstructionTimeout = 0;
        private BlendRenderer _faceRenderer;
        private BlendRenderer _skinnedMeshTarget;
        private BlendRenderer _nonMatchTarget;
        private string _selectedNonMathBlendName;
        private bool _nonMatchCorrectionMode = false;
        private bool _linkEyesComponents = true;
        private static readonly string _headCheckKey = "ct_head";
        private static readonly string _blendMatchCorrectionFileName = "__MatchCorrectionData__.xml";
        private readonly Dictionary<string, string> _matchCorractionList = new Dictionary<string, string>();
        private readonly Dictionary<string, BlendRenderer> _blendRenderers = new Dictionary<string, BlendRenderer>();
        private readonly Dictionary<string, BlendRenderer> _headRenderers = new Dictionary<string, BlendRenderer>();
        private readonly Dictionary<string, BlendRenderer> _headOriginBlendRenderers = new Dictionary<string, BlendRenderer>();
        private readonly Dictionary<SkinnedMeshRenderer, BlendRenderer> _blenderRenderbySkinnedRenderer = new Dictionary<SkinnedMeshRenderer, BlendRenderer>();

        private string _search = "";
        private readonly GenericOCITarget _target;
        private readonly Dictionary<XmlNode, BlendRenderer> _secondPassLoadingNodes = new Dictionary<XmlNode, BlendRenderer>();

        private int _renameIndex = -1;
        private string _renameString = "";
        private int _lastEditedBlendShape = -1;
        private bool _isBusy = false;

        enum BlendPresetMixMode
        {
            Mix = 0,
            Cross = 1,
            Add = 2,
            Subtract = 3,
            Count = 4
        }

        private bool _showSaveLoadWindow = false;
        private Rect _saveLoadWindowRect = new Rect();
        private RectTransform _imguiBackground = null;
        private Vector2 _presetsScroll;
        private string _presetName = "";
        private bool _removePresetMode;
        private BlendPresetMixMode _currBlendMixMode = BlendPresetMixMode.Mix;
        private float _presetMixRatio = 1.0f;
        private bool _intersectionPriorityToOrigin = true;
        private bool _loadFaceExpressionSettings = true;
        private float _addMaxValue = 100.0f;
        private bool _substractLeftIsOrigin = true;
        private Dictionary<string, float> _originWeights = new Dictionary<string, float>();
        private Dictionary<string, float> _presetWeights = new Dictionary<string, float>();
        private Dictionary<string, float> _mixResultWeight = new Dictionary<string, float>();
        private List<string> _mixTargetNames = new List<string>();

        #endregion

        #region Public Fields
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BlendShapes; } }
        public override string displayName { get { return "Blend Shapes"; } }
        public override bool shouldDisplay
        {
            get
            {
                return _blendRenderers.Any(r => r.Value != null && r.Value != null &&
                                           r.Value._renderer != null && r.Value._renderer.sharedMesh.blendShapeCount > 0);
            }
        }
        #endregion

        #region Unity Methods
        static BlendShapesEditor()
        {
#if HONEYSELECT || PLAYHOME
            _femaleSeparators.Add(0, "Eyes");
            _femaleSeparators.Add(58, "Mouth");
            _maleSeparators.Add(0, "Eyes");
            _maleSeparators.Add(14, "Mouth");
            _femaleEyesComponentsCount = 58;
            _maleEyesComponentsCount = 14;
#elif KOIKATSU
            _femaleSeparators.Add(0, "Eyes");
            _femaleSeparators.Add(28, "Mouth");
            _maleSeparators = _femaleSeparators;
            _femaleEyesComponentsCount = 28;
            _maleEyesComponentsCount = _femaleEyesComponentsCount;
#elif AISHOUJO || HONEYSELECT2
            _femaleSeparators.Add(0, "Eyes");
            _femaleSeparators.Add(16, "Eyebrows");
            _femaleSeparators.Add(26, "Mouth");
            _maleSeparators = _femaleSeparators;
            _femaleEyesComponentsCount = 16;
            _maleEyesComponentsCount = _femaleEyesComponentsCount;
#endif
            // Todo turn this into a setting
            var oldPresetsPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), HSPE.Name), "BlendShapesPresets");
            _presetsPath = Directory.Exists(oldPresetsPath) ? oldPresetsPath : Path.Combine(Path.Combine(Path.Combine(Paths.GameRootPath, "UserData"), HSPE.Name), "BlendShapesPresets");
        }

        public BlendShapesEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            _parent.onLateUpdate += LateUpdate;
            _parent.onDisable += OnDisable;
            _target = target;

            _imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI();
            MainWindow._self.ExecuteDelayed(() =>
            {
                if (_parent == null)
                {
                    return;
                }

                RefreshSkinnedMeshRendererList();

                if (_target.type != GenericOCITarget.Type.Character)
                {
                    return;
                }

                Init();
            });
        }

        public void DisableSubWindow()
        {
            _imguiBackground.gameObject.SetActive(false);
        }

        private static string FixFullPath(string fullpath)
        {
            string result = fullpath;

            if (fullpath.Contains(_headCheckKey))
            {
                result = fullpath.Substring(0, fullpath.LastIndexOf(_headCheckKey));
                result += _headCheckKey + fullpath.Substring(fullpath.LastIndexOf('/'));
            }

            return result;
        }

        public void SetBlendShapeWeight(SkinnedMeshRenderer renderer, int index, float weight)
        {
            if (_blenderRenderbySkinnedRenderer.TryGetValue(renderer, out BlendRenderer blendRenderer))
            {
                blendRenderer.SetBlendShapeWeight(index, weight);
            }
        }

        private void LateUpdate()
        {
            if (_target.type != GenericOCITarget.Type.Item)
            {
                return;
            }


            ApplyBlendShapeWeights();
        }

        public void OnGUI()
        {
            if (_showSaveLoadWindow == true)
            {
                _imguiBackground.gameObject.SetActive(true);
                IMGUIExtensions.DrawBackground(_saveLoadWindowRect);
                _saveLoadWindowRect = GUILayout.Window(MainWindow._uniqueId + 1, _saveLoadWindowRect, SaveLoadWindow, "Presets");
                IMGUIExtensions.FitRectTransformToRect(_imguiBackground, _saveLoadWindowRect);
            }
            else
            {
                _imguiBackground.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            foreach (var blendRenderer in _blendRenderers)
            {
                blendRenderer.Value.ClearDirty();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _parent.onLateUpdate -= LateUpdate;
            _parent.onDisable -= OnDisable;
            InstanceDict newInstances = new InstanceDict();
            foreach (InstancePair pair in _instanceByFaceBlendShape)
            {
                if (pair.Key != null)
                    newInstances.Add(pair.Key, pair.Value);
            }
            _instanceByFaceBlendShape = newInstances;
        }
        #endregion

        #region Public Methods
        public override void OnCharacterReplaced()
        {
            _isBusy = true;
            InstanceDict newInstanceByFBS = null;

            if (_headOriginBlendRenderers.Count == 0)
            {
                foreach (var blendRenderer in _blendRenderers)
                {
                    blendRenderer.Value._nonMatchedOriBlendsNames.Clear();
                    blendRenderer.Value._nonMatchBlendCorrection.Clear();
                    _headOriginBlendRenderers.Add(blendRenderer.Key, blendRenderer.Value);

                    for (int i = 0; i < blendRenderer.Value._blendNames.Count; i++)
                    {
                        blendRenderer.Value._oriBlendIndex.Add(i, blendRenderer.Value._blendNames[i]);
                    }
                }
            }

            foreach (InstancePair pair in _instanceByFaceBlendShape)
            {
                if (pair.Key == null)
                {
                    newInstanceByFBS = new InstanceDict();
                    break;
                }
            }
            if (newInstanceByFBS != null)
            {
                foreach (InstancePair pair in _instanceByFaceBlendShape)
                {
                    if (pair.Key != null)
                        newInstanceByFBS.Add(pair.Key, pair.Value);
                }
                _instanceByFaceBlendShape = newInstanceByFBS;
            }

            RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(() =>
            {
                RefreshSkinnedMeshRendererList();
                Init();
                _isBusy = false;
            });
        }

        public override void OnLoadClothesFile()
        {
            RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(RefreshSkinnedMeshRendererList);
        }
#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        public override void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force)
#elif KOIKATSU
        public override void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force)
#endif
        {
            _isBusy = true;
            RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(() =>
            {
                RefreshSkinnedMeshRendererList();
                _isBusy = false;
            });
        }
#endif

        public override void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(RefreshSkinnedMeshRendererList);
        }

        public override void GUILogic()
        {
            Color c = GUI.color;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(false));

            _skinnedMeshRenderersScroll = GUILayout.BeginScrollView(_skinnedMeshRenderersScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            foreach (var currBlendRenderer in _blendRenderers)
            {
                if (currBlendRenderer.Value._dirtyBlends.Count > 0)
                    GUI.color = Color.magenta;
                if (currBlendRenderer.Value == _skinnedMeshTarget)
                    GUI.color = Color.cyan;

                string key = currBlendRenderer.Key.Substring(currBlendRenderer.Key.LastIndexOf('/') + 1);
                if (BlendShapesEditor._skinnedMeshAliases.TryGetValue(key, out string result))
                {
                    key = result;
                }

                if (GUILayout.Button(key + ((GUI.color == Color.magenta) ? "*" : "")))
                {
                    _nonMatchCorrectionMode = false;
                    _skinnedMeshTarget = currBlendRenderer.Value;
                    _lastEditedBlendShape = -1;
                }
                GUI.color = c;
            }

            if (GUILayout.Button("nonMatched"))
            {
                _selectedNonMathBlendName = null;
                _skinnedMeshTarget = null;
                _nonMatchCorrectionMode = true;
            }

            GUILayout.EndScrollView();

            GUI.color = Color.green;
            if (GUILayout.Button("Save/Load preset"))
            {
                _saveLoadWindowRect = Rect.MinMaxRect(MainWindow._self._advancedModeRect.xMin - 180, MainWindow._self._advancedModeRect.yMin, MainWindow._self._advancedModeRect.xMin, MainWindow._self._advancedModeRect.yMax);
                _showSaveLoadWindow = true;
                RefreshPresets();
            }
            GUI.color = c;

            if (_target.type == GenericOCITarget.Type.Character)
                _linkEyesComponents = GUILayout.Toggle(_linkEyesComponents, "Link eyes components");
            if (GUILayout.Button("Force refresh list"))
                RefreshSkinnedMeshRendererList();
            GUI.color = Color.red;
            if (GUILayout.Button("Reset all"))
                ResetAll();
            GUI.color = c;


            bool hasOriginBlend = _headOriginBlendRenderers.Count > 0;
            if (hasOriginBlend)
            {
                GUI.color = Color.red;
            }

            if (GUILayout.Button("Refresh renderer"))
            {
                RefreshSkinnedMeshRendererList();
            }

            if (GUILayout.Button(hasOriginBlend ? "NonOriginChar" : "OriginChar"))
            {
                _headOriginBlendRenderers.Clear();

                if (hasOriginBlend)
                {
                    _nonMatchCorrectionMode = false;
                    _nonMatchTarget = null;
                    _selectedNonMathBlendName = null;
                    foreach (var blendRenderer in _blendRenderers)
                    {
                        blendRenderer.Value.ClearOriginData();
                    }
                }
                else
                {
                    foreach (var blendRenderer in _blendRenderers)
                    {
                        blendRenderer.Value._nonMatchedOriBlendsNames.Clear();
                        blendRenderer.Value._nonMatchBlendCorrection.Clear();
                        _headOriginBlendRenderers.Add(blendRenderer.Key, blendRenderer.Value);

                        for (int i = 0; i < blendRenderer.Value._blendNames.Count; i++)
                        {
                            blendRenderer.Value._oriBlendIndex.Add(i, blendRenderer.Value._blendNames[i]);
                        }
                    }
                }
            }
            GUI.color = c;

            if (_headOriginBlendRenderers.Count > 0)
            {
                if (GUILayout.Button("SaveMatch"))
                {
                    LoadMatchCorrection();

                    foreach (var blendRenderer in _blendRenderers)
                    {
                        foreach (var correction in blendRenderer.Value._nonMatchBlendCorrection)
                        {
                            _matchCorractionList[correction.Key] = correction.Value;
                        }
                    }

                    if (Directory.Exists(_presetsPath) == false)
                        Directory.CreateDirectory(_presetsPath);
                    using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(_presetsPath, _blendMatchCorrectionFileName), Encoding.UTF8))
                    {
                        writer.WriteStartElement("matchCorrection");

                        foreach (var correction in _matchCorractionList)
                        {
                            writer.WriteStartElement(correction.Key);
                            writer.WriteAttributeString("correction", correction.Value);
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                }

                if (GUILayout.Button("LoadMatch"))
                {
                    LoadMatchCorrection();

                    foreach (var blendRenderer in _blendRenderers)
                    {
                        blendRenderer.Value._nonMatchBlendCorrection.Clear();

                        foreach (var nonMatchBlendName in blendRenderer.Value._nonMatchedOriBlendsNames)
                        {
                            if (_matchCorractionList.TryGetValue(nonMatchBlendName, out string correctionName))
                            {
                                if (blendRenderer.Value._blendIndics.ContainsKey(correctionName))
                                {
                                    blendRenderer.Value._nonMatchBlendCorrection[nonMatchBlendName] = correctionName;
                                }
                            }
                        }
                    }
                }
            }

            GUILayout.EndVertical();

            if (_skinnedMeshTarget != null)
            {
                if (_skinnedMeshTarget._renderer == null)
                {
                    SkinnedMeshRenderer[] skinnedMeshRenderers = _parent.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                    foreach (var renderer in skinnedMeshRenderers)
                    {
                        string fullPath = FixFullPath(renderer.transform.GetPathFrom(_parent.transform));

                        if (fullPath == _skinnedMeshTarget._fullPath)
                        {
                            _skinnedMeshTarget._renderer = renderer;
                            break;
                        }
                    }
                }

                if (_skinnedMeshTarget._renderer == null)
                {
                    _blendRenderers.Remove(_skinnedMeshTarget._fullPath);
                    _skinnedMeshTarget = null;
                    return;
                }

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search", GUILayout.ExpandWidth(false));
                _search = GUILayout.TextField(_search, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    _search = "";
                GUILayout.EndHorizontal();

                _blendShapesScroll = GUILayout.BeginScrollView(_blendShapesScroll, false, true, GUILayout.ExpandWidth(false));

                bool zeroResult = true;

                foreach (var currBlend in _skinnedMeshTarget._blendIndics)
                {
                    string str1;
                    if ((_faceRenderer == _skinnedMeshTarget) && (_target.isFemale ? _femaleSeparators : _maleSeparators).TryGetValue(currBlend.Value, out str1))
                    {
                        GUILayout.Label(str1, GUI.skin.box);
                    }

                    string str2;
                    if (!_blendShapeAliases.TryGetValue(currBlend.Key, out str2))
                    {
                        str2 = null;
                    }

                    if (str2 != null && str2.IndexOf(_search, StringComparison.CurrentCultureIgnoreCase) != -1 || currBlend.Key.IndexOf(_search, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        zeroResult = false;
                        BlendShapeData blendShapeData1;
                        float num1;
                        if (_skinnedMeshTarget._dirtyBlends.TryGetValue(currBlend.Key, out blendShapeData1))
                        {
                            num1 = blendShapeData1.weight;
                            GUI.color = Color.magenta;
                        }
                        else
                        {
                            num1 = _skinnedMeshTarget._renderer.GetBlendShapeWeight(currBlend.Value);
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.BeginVertical(GUILayout.ExpandHeight(false));
                        GUILayout.BeginHorizontal();
                        if (_renameIndex != currBlend.Value)
                        {
                            GUILayout.Label(string.Format("{0} {1}", currBlend.Value, str2 == null ? currBlend.Key : str2));
                            GUILayout.FlexibleSpace();
                        }
                        else
                        {
                            GUILayout.Label(currBlend.Value.ToString(), GUILayout.ExpandWidth(false));
                            _renameString = GUILayout.TextField(_renameString, GUILayout.ExpandWidth(true));
                        }

                        if (GUILayout.Button(_renameIndex != currBlend.Value ? "Rename" : "Save", GUILayout.ExpandWidth(false)))
                        {
                            if (_renameIndex != currBlend.Value)
                            {
                                _renameIndex = currBlend.Value;
                                _renameString = str2 == null ? currBlend.Key : str2;
                            }
                            else
                            {
                                _renameIndex = -1;
                                _renameString = _renameString.Trim();
                                if (_renameString.IsNullOrEmpty() || _renameString == currBlend.Key)
                                {
                                    if (_blendShapeAliases.ContainsKey(currBlend.Key))
                                    {
                                        _blendShapeAliases.Remove(currBlend.Key);
                                    }
                                }
                                else if (!_blendShapeAliases.ContainsKey(currBlend.Key))
                                {
                                    _blendShapeAliases.Add(currBlend.Key, _renameString);
                                }
                                else
                                {
                                    _blendShapeAliases[currBlend.Key] = _renameString;
                                }
                            }
                        }
                        GUILayout.Label(num1.ToString("000"), GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        float num2 = GUILayout.HorizontalSlider(num1, 0.0f, 100f);
                        if (GUILayout.Button("-1", GUILayout.ExpandWidth(false)))
                        {
                            --num2;
                        }

                        if (GUILayout.Button("+1", GUILayout.ExpandWidth(false)))
                        {
                            ++num2;
                        }

                        float weight = Mathf.Clamp(num2, 0.0f, 100f);
                        GUILayout.EndHorizontal();

                        if (!Mathf.Approximately(weight, num1))
                        {
                            _lastEditedBlendShape = currBlend.Value;
                            _skinnedMeshTarget.SetBlendShapeWeight(currBlend.Key, weight);

                            if (_linkEyesComponents)
                            {
                                _skinnedMeshTarget.ApplyLink(weight, currBlend.Value);

                            }
                        }
                        GUILayout.EndVertical();
                        GUI.color = Color.red;
                        if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false), GUILayout.Height(50f)))
                        {
                            _skinnedMeshTarget.NonDirty(currBlend.Key);

                            if (_linkEyesComponents)
                            {
                                _skinnedMeshTarget.ApplyLink(0.0f, currBlend.Value, true);
                            }
                        }

                        GUILayout.EndHorizontal();
                        GUI.color = c;
                    }
                }

                if (zeroResult)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                GUI.color = Color.red;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                {
                    _skinnedMeshTarget.ClearDirty();
                }

                GUI.color = c;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            else if (_nonMatchCorrectionMode)
            {
                if (_headOriginBlendRenderers.Count > 0)
                {
                    GUILayout.BeginVertical(GUI.skin.box);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Mismatch blend list", GUI.skin.box);
                    GUILayout.EndHorizontal();

                    _nonMatchBlendScroll = GUILayout.BeginScrollView(_nonMatchBlendScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
                    bool notingMatch = true;

                    foreach (var currBlendRenderer in _blendRenderers)
                    {
                        foreach (var nonMatchedBlend in currBlendRenderer.Value._nonMatchedOriBlendsNames)
                        {
                            notingMatch = false;

                            GUI.color = c;
                            if (currBlendRenderer.Value == _nonMatchTarget && _selectedNonMathBlendName == nonMatchedBlend)
                            {
                                GUI.color = Color.magenta;
                            }

                            if (GUILayout.Button(nonMatchedBlend))
                            {
                                _selectedNonMathBlendName = nonMatchedBlend;
                                _nonMatchTarget = currBlendRenderer.Value;
                            }
                        }
                    }

                    GUI.color = c;

                    if (notingMatch)
                    {
                        GUILayout.Label("There's nothing that didn't match", GUI.skin.box);
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();

                    if (_selectedNonMathBlendName != null)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("target list", GUI.skin.box);
                        GUILayout.EndHorizontal();

                        _nonMatchBlendRendererScroll = GUILayout.BeginScrollView(_nonMatchBlendRendererScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));

                        string correctName = null;
                        _nonMatchTarget._nonMatchBlendCorrection.TryGetValue(_selectedNonMathBlendName, out correctName);

                        foreach (var currBlend in _nonMatchTarget._blendIndics)
                        {
                            GUI.color = c;
                            if (correctName == currBlend.Key)
                            {
                                GUI.color = Color.magenta;
                            }

                            if (GUILayout.Button(currBlend.Key))
                            {
                                _matchCorractionList[_selectedNonMathBlendName] = currBlend.Key;
                                _nonMatchTarget._nonMatchBlendCorrection[_selectedNonMathBlendName] = currBlend.Key;
                            }
                        }

                        GUILayout.EndScrollView();
                        GUILayout.EndVertical();

                        GUI.color = c;
                    }
                }
                else
                {
                    GUILayout.BeginVertical(GUI.skin.box);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Originchar not selected", GUI.skin.box);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
        }

        public void LoadFrom(BlendShapesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (var otherBlendRenderer in other._blendRenderers)
                {
                    BlendRenderer destRenderer = null;
                    if (_blendRenderers.TryGetValue(otherBlendRenderer.Key, out destRenderer))
                    {
                        foreach (var currDirty in otherBlendRenderer.Value._dirtyBlends)
                        {
                            destRenderer.SetBlendShapeWeight(currDirty.Key, currDirty.Value.weight);
                        }
                    }
                }

                _blendShapesScroll = other._blendShapesScroll;
                _skinnedMeshRenderersScroll = other._skinnedMeshRenderersScroll;
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;

            xmlWriter.WriteStartElement("skinnedMeshes");
            if (_target.type == GenericOCITarget.Type.Character)
            {
                xmlWriter.WriteAttributeString("eyesPtn", XmlConvert.ToString(_target.ociChar.charInfo.GetEyesPtn()));
#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
                xmlWriter.WriteAttributeString("eyesOpen", XmlConvert.ToString(_target.ociChar.charInfo.GetEyesOpenMax()));
#elif PLAYHOME
                    xmlWriter.WriteAttributeString("eyesOpen", XmlConvert.ToString(this._target.ociChar.charInfo.fileStatus.eyesOpenMax));
#endif
                xmlWriter.WriteAttributeString("mouthPtn", XmlConvert.ToString(_target.ociChar.charInfo.GetMouthPtn()));
                xmlWriter.WriteAttributeString("mouthOpen", XmlConvert.ToString(_target.ociChar.oiCharInfo.mouthOpen));
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                xmlWriter.WriteAttributeString("eyebrowsPtn", XmlConvert.ToString(_target.ociChar.charInfo.GetEyebrowPtn()));
                xmlWriter.WriteAttributeString("eyebrowsOpen", XmlConvert.ToString(_target.ociChar.charInfo.GetEyebrowOpenMax()));
#endif
                ++written;
            }

            foreach (var blendRenderer in _blendRenderers)
            {
                SkinnedMeshRenderer skinnedMeshRenderer2 = blendRenderer.Value._renderer;
                xmlWriter.WriteStartElement("skinnedMesh");
                xmlWriter.WriteAttributeString("name", skinnedMeshRenderer2.transform.GetPathFrom(_parent.transform));
                foreach (var dirty in blendRenderer.Value._dirtyBlends)
                {
                    xmlWriter.WriteStartElement("blendShape");
                    xmlWriter.WriteAttributeString("shapeName", dirty.Key);
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(skinnedMeshRenderer2.sharedMesh.GetBlendShapeIndex(dirty.Key)));
                    xmlWriter.WriteAttributeString("weight", XmlConvert.ToString(dirty.Value.weight));
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                ++written;
            }

            xmlWriter.WriteEndElement();

            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            ResetAll();
            bool changed = false;
            XmlNode skinnedMeshesNode = xmlNode.FindChildNode("skinnedMeshes");
            Dictionary<XmlNode, BlendRenderer> potentialChildrenNodes = new Dictionary<XmlNode, BlendRenderer>();
            if (skinnedMeshesNode != null)
            {
                if (_target.type == GenericOCITarget.Type.Character)
                {
                    if (skinnedMeshesNode.Attributes["eyesPtn"] != null)
                    {
#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
                        _target.ociChar.charInfo.ChangeEyesPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyesPtn"].Value), false);
#elif PLAYHOME
                        this._target.ociChar.charInfo.ChangeEyesPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyesPtn"].Value));
#endif
                    }
                    if (skinnedMeshesNode.Attributes["eyesOpen"] != null)
                        _target.ociChar.ChangeEyesOpen(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["eyesOpen"].Value));
                    if (skinnedMeshesNode.Attributes["mouthPtn"] != null)
                    {
#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
                        _target.ociChar.charInfo.ChangeMouthPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["mouthPtn"].Value), false);
#elif PLAYHOME
                        this._target.ociChar.charInfo.ChangeMouthPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["mouthPtn"].Value));
#endif
                    }
                    if (skinnedMeshesNode.Attributes["mouthOpen"] != null)
                        _target.ociChar.ChangeMouthOpen(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["mouthOpen"].Value));
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                    if (skinnedMeshesNode.Attributes["eyebrowsPtn"] != null)
                        _target.ociChar.charInfo.ChangeEyebrowPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyebrowsPtn"].Value), false);
                    if (skinnedMeshesNode.Attributes["eyebrowsOpen"] != null)
                        _target.ociChar.charInfo.ChangeEyebrowOpenMax(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["eyebrowsOpen"].Value));
#endif

                }
                foreach (XmlNode node in skinnedMeshesNode.ChildNodes)
                {
                    try
                    {
                        string currFullPath = FixFullPath(node.Attributes["name"].Value);

                        BlendRenderer blendRenderer = null;
                        if (_blendRenderers.TryGetValue(currFullPath, out blendRenderer))
                        {
                            potentialChildrenNodes.Add(node, blendRenderer);

                            if (LoadSingleSkinnedMeshRenderer(node, blendRenderer))
                            {
                                changed = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        HSPE.Logger.LogError("Couldn't load blendshape for object " + _parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
            if (potentialChildrenNodes.Count > 0)
            {
                foreach (var pair in potentialChildrenNodes)
                {
                    PoseController childController = pair.Value._renderer.GetComponentInParent<PoseController>();
                    if (childController != _parent && childController != null)
                    {
                        childController.enabled = true;
                        if (childController._blendShapesEditor._secondPassLoadingNodes.ContainsKey(pair.Key) == false)
                            childController._blendShapesEditor._secondPassLoadingNodes.Add(pair.Key, pair.Value);
                    }
                }
            }

            _parent.ExecuteDelayed(() =>
            {
                foreach (var pair in _secondPassLoadingNodes)
                {
                    try
                    {
                        if (_blendRenderers.TryGetValue(pair.Value._fullPath, out BlendRenderer blendRenderer))
                        {
                            if (blendRenderer == pair.Value)
                            {
                                LoadSingleSkinnedMeshRenderer(pair.Key, pair.Value);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        HSPE.Logger.LogError("Couldn't load blendshape for object " + _parent.name + " " + pair.Key.OuterXml + "\n" + e);
                    }
                }
                _secondPassLoadingNodes.Clear();
            }, 2);
            return changed || _secondPassLoadingNodes.Count > 0;
        }

        #endregion

        #region Private Methods
        private bool LoadSingleSkinnedMeshRenderer(XmlNode node, BlendRenderer renderer)
        {
            bool loaded = false;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                int index = XmlConvert.ToInt32(childNode.Attributes["index"].Value);
                BlendShapeData result = null;

                if (childNode.Attributes["blendName"] != null)
                {
                    result = renderer.SetBlendShapeWeight(childNode.Attributes["blendName"].Value, XmlConvert.ToSingle(childNode.Attributes["weight"].Value));
                }
                else
                {
                    result = renderer.SetBlendShapeWeight(index, XmlConvert.ToSingle(childNode.Attributes["weight"].Value));
                }

                if (result != null)
                {
                    loaded = true;
                }
            }

            return loaded;
        }
        private void LoadMatchCorrection()
        {
            string path = Path.Combine(_presetsPath, _blendMatchCorrectionFileName);
            if (!System.IO.File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNode firstNode = doc.FirstChild;

                _matchCorractionList.Clear();
                foreach (XmlNode currNode in firstNode.ChildNodes)
                {
                    _matchCorractionList[currNode.Name] = currNode.Attributes["correction"].Value;
                }
            }
        }
        private void RefreshPresets()
        {
            if (Directory.Exists(_presetsPath))
            {
                _presets = Directory.GetFiles(_presetsPath, "*.xml");
                for (int i = 0; i < _presets.Length; i++)
                    _presets[i] = Path.GetFileNameWithoutExtension(_presets[i]);
            }
        }
        private void SaveLoadWindow(int id)
        {
            GUILayout.BeginVertical();

            _presetsScroll = GUILayout.BeginScrollView(_presetsScroll, false, true, GUILayout.ExpandHeight(true));
            foreach (string preset in _presets)
            {
                if (GUILayout.Button(preset))
                {
                    if (_removePresetMode)
                        DeletePreset(preset + ".xml");
                    else
                    {
                        LoadPreset(preset + ".xml");
                    }
                }
            }
            GUILayout.EndScrollView();

            Color c = GUI.color;
            GUILayout.BeginVertical();
            if (_presets.Any(p => p.Equals(_presetName, StringComparison.OrdinalIgnoreCase)))
                GUI.color = Color.red;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.ExpandWidth(false));
            _presetName = GUILayout.TextField(_presetName);
            GUILayout.EndHorizontal();
            GUI.color = c;

            if (GUILayout.Button(_currBlendMixMode.ToString()))
            {
                _currBlendMixMode = (BlendPresetMixMode)(((int)_currBlendMixMode + 1) % (int)BlendPresetMixMode.Count);
            }

            GUILayout.BeginHorizontal();
            switch (_currBlendMixMode)
            {
                case BlendPresetMixMode.Mix:
                    {
                        GUILayout.Label("ratio : " + _presetMixRatio.ToString("0.00"));
                        _presetMixRatio = GUILayout.HorizontalSlider(_presetMixRatio, 0.0f, 1.0f, GUILayout.ExpandWidth(true));
                    }
                    break;
                case BlendPresetMixMode.Cross:
                    {
                        _intersectionPriorityToOrigin = GUILayout.Toggle(_intersectionPriorityToOrigin, "Priority : " + (_intersectionPriorityToOrigin ? "origin" : "preset"));
                    }
                    break;
                case BlendPresetMixMode.Add:
                    {
                        GUILayout.Label("max : " + _addMaxValue.ToString("000"));
                        _addMaxValue = GUILayout.HorizontalSlider(_addMaxValue, 0.0f, 100.0f, GUILayout.ExpandWidth(true));
                    }
                    break;
                case BlendPresetMixMode.Subtract:
                    {
                        _substractLeftIsOrigin = GUILayout.Toggle(_substractLeftIsOrigin, "Left is : " + (_substractLeftIsOrigin ? "origin" : "preset"));
                    }
                    break;
            }

            GUILayout.EndHorizontal();
            _loadFaceExpressionSettings = GUILayout.Toggle(_loadFaceExpressionSettings, "Load FaceExpSetting");


            GUI.enabled = _presetName.Length != 0;
            if (GUILayout.Button("Save"))
            {
                _presetName = _presetName.Trim();
                _presetName = string.Join("_", _presetName.Split(Path.GetInvalidFileNameChars()));
                if (_presetName.Length != 0)
                {
                    SavePreset(_presetName + ".xml");
                    RefreshPresets();
                    _removePresetMode = false;
                }

            }
            GUI.enabled = true;
            if (_removePresetMode)
                GUI.color = Color.red;
            GUI.enabled = _presets.Length != 0;
            if (GUILayout.Button(_removePresetMode ? "Click on preset" : "Delete"))
                _removePresetMode = !_removePresetMode;
            GUI.enabled = true;
            GUI.color = c;

            if (GUILayout.Button("Open folder"))
                System.Diagnostics.Process.Start(_presetsPath);
            if (GUILayout.Button("Close"))
                _showSaveLoadWindow = false;

            GUILayout.EndVertical();
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void SavePreset(string name)
        {
            if (Directory.Exists(_presetsPath) == false)
                Directory.CreateDirectory(_presetsPath);
            using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(_presetsPath, name), Encoding.UTF8))
            {
                writer.WriteStartElement("root");
                SaveXml(writer);
                writer.WriteEndElement();
            }
        }

        private void LoadPreset(string name)
        {
            string path = Path.Combine(_presetsPath, name);
            if (!System.IO.File.Exists(path))
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            LoadXmlWithMixMode(doc.FirstChild);
        }

        private void DeletePreset(string name)
        {
            System.IO.File.Delete(Path.GetFullPath(Path.Combine(_presetsPath, name)));
            _removePresetMode = false;
            RefreshPresets();
        }

        private void LoadNodeMix(XmlNode node, BlendRenderer renderer)
        {
            _originWeights.Clear();
            _presetWeights.Clear();
            _mixResultWeight.Clear();
            _mixTargetNames.Clear();

            foreach (var blend in renderer._dirtyBlends)
            {
                _originWeights.Add(blend.Key, blend.Value.weight);

                _mixResultWeight[blend.Key] = 0.0f;
            }

            foreach (XmlNode childNode in node.ChildNodes)
            {
                int index = XmlConvert.ToInt32(childNode.Attributes["index"].Value);

                if (childNode.Attributes["blendName"] != null)
                {
                    string blendName = childNode.Attributes["blendName"].Value;
                    _presetWeights.Add(blendName, XmlConvert.ToSingle(childNode.Attributes["weight"].Value));
                    _mixResultWeight[blendName] = 0.0f;
                }
                else
                {
                    if (renderer._blendNames.Count > index)
                    {
                        string blendName = renderer._blendNames[index];
                        _presetWeights.Add(blendName, XmlConvert.ToSingle(childNode.Attributes["weight"].Value));
                        _mixResultWeight[blendName] = 0.0f;
                    }
                }
            }

            foreach (var blend in _mixResultWeight)
            {
                _mixTargetNames.Add(blend.Key);
            }

            switch (_currBlendMixMode)
            {
                case BlendPresetMixMode.Mix:
                    {
                        if (_presetMixRatio == 1.0f)
                        {
                            foreach (var blend in _originWeights)
                            {
                                renderer.NonDirty(blend.Key);
                            }

                            _mixResultWeight.Clear();
                            _mixResultWeight.AddRange(_presetWeights);
                        }
                        else
                        {
                            foreach (var currTarget in _mixTargetNames)
                            {
                                float originWeight = 0.0f;
                                float presetWeight = 0.0f;

                                _originWeights.TryGetValue(currTarget, out originWeight);
                                _presetWeights.TryGetValue(currTarget, out presetWeight);

                                _mixResultWeight[currTarget] = Mathf.Lerp(originWeight, presetWeight, _presetMixRatio);
                            }
                        }
                    }
                    break;
                case BlendPresetMixMode.Cross:
                    {
                        foreach (var currTarget in _mixTargetNames)
                        {
                            float weight = 0.0f;
                            if (_intersectionPriorityToOrigin)
                            {
                                if (!_originWeights.TryGetValue(currTarget, out weight))
                                {
                                    _presetWeights.TryGetValue(currTarget, out weight);
                                }
                            }
                            else
                            {
                                if (!_presetWeights.TryGetValue(currTarget, out weight))
                                {
                                    _originWeights.TryGetValue(currTarget, out weight);
                                }
                            }

                            _mixResultWeight[currTarget] = weight;
                        }
                    }
                    break;
                case BlendPresetMixMode.Add:
                    {
                        foreach (var currTarget in _mixTargetNames)
                        {
                            float originWeight = 0.0f;
                            float presetWeight = 0.0f;

                            _originWeights.TryGetValue(currTarget, out originWeight);
                            _presetWeights.TryGetValue(currTarget, out presetWeight);

                            float resultWeight = originWeight + presetWeight;

                            _mixResultWeight[currTarget] = resultWeight < _addMaxValue ? resultWeight : _addMaxValue;
                        }
                    }
                    break;
                case BlendPresetMixMode.Subtract:
                    {
                        foreach (var currTarget in _mixTargetNames)
                        {
                            float originWeight = 0.0f;
                            float presetWeight = 0.0f;

                            _originWeights.TryGetValue(currTarget, out originWeight);
                            _presetWeights.TryGetValue(currTarget, out presetWeight);

                            float resultWeight = _substractLeftIsOrigin ? (originWeight - presetWeight) : (presetWeight - originWeight);

                            _mixResultWeight[currTarget] = resultWeight < 0.0f ? 0.0f : resultWeight;
                        }
                    }
                    break;
            }

            foreach (var blend in _mixResultWeight)
            {
                renderer.SetBlendShapeWeight(blend.Key, blend.Value);
            }
        }


        private void LoadXmlWithMixMode(XmlNode xmlNode)
        {
            XmlNode skinnedMeshesNode = xmlNode.FindChildNode("skinnedMeshes");

            if (skinnedMeshesNode != null)
            {
                if (_target.type == GenericOCITarget.Type.Character && _loadFaceExpressionSettings)
                {
                    if (skinnedMeshesNode.Attributes["eyesPtn"] != null)
                    {
#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
                        _target.ociChar.charInfo.ChangeEyesPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyesPtn"].Value), false);
#elif PLAYHOME
                        this._target.ociChar.charInfo.ChangeEyesPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyesPtn"].Value));
#endif
                    }
                    if (skinnedMeshesNode.Attributes["eyesOpen"] != null)
                        _target.ociChar.ChangeEyesOpen(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["eyesOpen"].Value));
                    if (skinnedMeshesNode.Attributes["mouthPtn"] != null)
                    {
#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
                        _target.ociChar.charInfo.ChangeMouthPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["mouthPtn"].Value), false);
#elif PLAYHOME
                        this._target.ociChar.charInfo.ChangeMouthPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["mouthPtn"].Value));
#endif
                    }
                    if (skinnedMeshesNode.Attributes["mouthOpen"] != null)
                        _target.ociChar.ChangeMouthOpen(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["mouthOpen"].Value));
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                    if (skinnedMeshesNode.Attributes["eyebrowsPtn"] != null)
                        _target.ociChar.charInfo.ChangeEyebrowPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyebrowsPtn"].Value), false);
                    if (skinnedMeshesNode.Attributes["eyebrowsOpen"] != null)
                        _target.ociChar.charInfo.ChangeEyebrowOpenMax(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["eyebrowsOpen"].Value));
#endif
                }

                foreach (XmlNode node in skinnedMeshesNode.ChildNodes)
                {
                    try
                    {
                        string currFullPath = FixFullPath(node.Attributes["name"].Value);

                        BlendRenderer blendRenderer = null;
                        if (_blendRenderers.TryGetValue(currFullPath, out blendRenderer))
                        {
                            LoadNodeMix(node, blendRenderer);
                        }
                    }
                    catch (Exception e)
                    {
                        HSPE.Logger.LogError("Couldn't load blendshape for object " + _parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
        }


        private void Init()
        {
            _faceRenderer = null;
#if HONEYSELECT
            _instanceByFaceBlendShape.Add(this._target.ociChar.charBody.fbsCtrl, this);
#elif PLAYHOME
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.human, this);
#elif KOIKATSU
            _instanceByFaceBlendShape.Add(_target.ociChar.charInfo.fbsCtrl, this);
#elif AISHOUJO || HONEYSELECT2
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.fbsCtrl, this);
#endif

            List<string> headTags = new List<string>();
            List<string> linkTags = new List<string>();

#if HONEYSELECT || KOIKATSU || PLAYHOME
            headTags.Add("cf_O_head");
            headTags.Add("cf_O_face");
#elif AISHOUJO || HONEYSELECT2
            headTags.Add("o_head");
#endif

#if HONEYSELECT || PLAYHOME
            linkTags.Add("cf_O_matuge");
            linkTags.Add("cf_O_namida01");
            linkTags.Add("cf_O_namida02");
#elif KOIKATSU
            linkTags.Add("cf_O_eyeline");
            linkTags.Add("cf_O_eyeline_low");
            linkTags.Add("cf_Ohitomi_L");
            linkTags.Add("cf_Ohitomi_R");
            linkTags.Add("cf_O_namida_L");
            linkTags.Add("cf_O_namida_M");
            linkTags.Add("cf_O_namida_S");
#elif AISHOUJO || HONEYSELECT2
            linkTags.Add("o_eyelashes");
            linkTags.Add("o_namida");
#endif

            Dictionary<string, List<BlendRenderer>> sameLevelblendRenderers = new Dictionary<string, List<BlendRenderer>>();

            foreach (var blendrenderer in _blendRenderers)
            {
                blendrenderer.Value._linkedBlendRenderers.Clear();
                string tagKey = blendrenderer.Value._renderer.name;
                string path = blendrenderer.Key.Substring(0, blendrenderer.Key.LastIndexOf('/'));

                if (headTags.Contains(tagKey))
                {
                    _faceRenderer = blendrenderer.Value;

                    if (!sameLevelblendRenderers.ContainsKey(path))
                    {
                        sameLevelblendRenderers.Add(path, new List<BlendRenderer>());
                    }

                    sameLevelblendRenderers[path].Add(blendrenderer.Value);
                }
                else if (linkTags.Contains(tagKey))
                {
                    if (!sameLevelblendRenderers.ContainsKey(path))
                    {
                        sameLevelblendRenderers.Add(path, new List<BlendRenderer>());
                    }

                    sameLevelblendRenderers[path].Add(blendrenderer.Value);
                }
            }

            foreach (var sameLevelBlendRendererList in sameLevelblendRenderers)
            {
                for (int i = 0; i < sameLevelBlendRendererList.Value.Count; i++)
                {
                    for (int j = 0; j < sameLevelBlendRendererList.Value.Count; j++)
                    {
                        if (j != i)
                        {
                            sameLevelBlendRendererList.Value[i]._linkedBlendRenderers.Add(sameLevelBlendRendererList.Value[j]);
                        }
                    }
                }
            }

            foreach (var blendrenderer in _blendRenderers)
            {
                blendrenderer.Value.CreateLinkKeynames(_target.isFemale);
            }
        }

        private void ResetAll()
        {
            foreach (var blendRenderer in _blendRenderers)
            {
                blendRenderer.Value.ClearDirty();
            }
        }

        private void FaceBlendShapeOnPostLateUpdate()
        {
            if (_parent.enabled)
            {
                ApplyBlendShapeWeights();
            }
        }

        private void ApplyBlendShapeWeights()
        {
            foreach (var currBlendRenderer in _blendRenderers)
            {
                if (currBlendRenderer.Value._renderer == null)
                {
                    continue;
                }

                if (currBlendRenderer.Value._blendNames.Count != currBlendRenderer.Value._renderer.sharedMesh.blendShapeCount)
                {
                    currBlendRenderer.Value._blendIndics.Clear();
                    currBlendRenderer.Value._blendNames.Clear();

                    for (int index = 0; index < currBlendRenderer.Value._renderer.sharedMesh.blendShapeCount; index++)
                    {
                        string blendName = currBlendRenderer.Value._renderer.sharedMesh.GetBlendShapeName(index);
                        currBlendRenderer.Value._blendIndics[blendName] = index;
                        currBlendRenderer.Value._blendNames.Add(blendName);
                    }

                    currBlendRenderer.Value.CreateLinkKeynames(_target.isFemale);
                }

                currBlendRenderer.Value.ApplyDirty();
            }
        }

        private void RefreshSkinnedMeshRendererList()
        {
            _nonMatchCorrectionMode = false;
            _nonMatchTarget = null;
            _selectedNonMathBlendName = null;

            Dictionary<string, BlendRenderer> nonMatchedBlendRenderers = new Dictionary<string, BlendRenderer>();

            foreach (var currBlend in _headOriginBlendRenderers)
            {
                currBlend.Value._renderer = null;

                nonMatchedBlendRenderers[currBlend.Key] = currBlend.Value;
            }

            foreach (var currBlend in _blendRenderers)
            {
                currBlend.Value._renderer = null;
                nonMatchedBlendRenderers[currBlend.Key] = currBlend.Value;
            }

            _blendRenderers.Clear();
            _blenderRenderbySkinnedRenderer.Clear();

            List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
            List<Transform> transformStack = new List<Transform>();
            transformStack.Add(_parent.transform);

            while (transformStack.Count > 0)
            {
                Transform currTransform = transformStack.Pop();

                SkinnedMeshRenderer skinnedMeshRenderer = currTransform.GetComponent<SkinnedMeshRenderer>();
                BlendShapesEditor currBlendShapesEditor = currTransform.GetComponent<BlendShapesEditor>();

                if (currBlendShapesEditor != null && currBlendShapesEditor != this)
                {
                    continue;
                }

                if (skinnedMeshRenderer != null)
                {
                    skinnedMeshRenderers.Add(skinnedMeshRenderer);
                }

                foreach (Transform child in currTransform)
                {
                    transformStack.Add(child);
                }
            }

            foreach (SkinnedMeshRenderer skin in skinnedMeshRenderers)
            {
                if (skin.sharedMesh != null && skin.sharedMesh.blendShapeCount > 0 && _parent._childObjects.All((child => !(skin).transform.IsChildOf(child.transform))))
                {
                    string fullpath = FixFullPath(skin.transform.GetPathFrom(_parent.transform));

                    if (nonMatchedBlendRenderers.ContainsKey(fullpath))
                    {
                        nonMatchedBlendRenderers[fullpath]._renderer = skin;
                        _blendRenderers.Add(fullpath, nonMatchedBlendRenderers[fullpath]);
                        nonMatchedBlendRenderers.Remove(fullpath);
                    }
                    else
                    {
                        BlendRenderer blendRenderer = new BlendRenderer();
                        blendRenderer._fullPath = fullpath;
                        blendRenderer._renderer = skin;
                        _blendRenderers.Add(fullpath, blendRenderer);
                    }
                }
            }

            List<string> removeDirtys = new List<string>();

            foreach (var currBlend in _blendRenderers)
            {
                currBlend.Value._blendIndics.Clear();
                currBlend.Value._blendNames.Clear();

                for (int index = 0; index < currBlend.Value._renderer.sharedMesh.blendShapeCount; index++)
                {
                    string blendName = currBlend.Value._renderer.sharedMesh.GetBlendShapeName(index);
                    currBlend.Value._blendIndics[blendName] = index;
                    currBlend.Value._blendNames.Add(blendName);
                }

                if (_headOriginBlendRenderers.Count > 0)
                {
                    currBlend.Value._nonMatchedOriBlendsNames.Clear();
                    currBlend.Value._nonMatchBlendCorrection.Clear();

                    foreach (var oriBlend in currBlend.Value._oriBlendIndex)
                    {
                        if (!currBlend.Value._blendIndics.ContainsKey(oriBlend.Value))
                        {
                            currBlend.Value._nonMatchedOriBlendsNames.Add(oriBlend.Value);
                        }
                    }
                }

                removeDirtys.Clear();

                foreach (var currDirty in currBlend.Value._dirtyBlends)
                {
                    if (!currBlend.Value._blendIndics.ContainsKey(currDirty.Key))
                    {
                        removeDirtys.Add(currDirty.Key);
                    }
                }

                foreach (var removeKey in removeDirtys)
                {
                    currBlend.Value._dirtyBlends.Remove(removeKey);
                }
            }

            if (_skinnedMeshTarget != null)
            {
                _skinnedMeshTarget = null;
                foreach (var currBlend in _blendRenderers)
                {
                    _skinnedMeshTarget = currBlend.Value;

                    break;
                }
            }

            foreach (var currBlend in _blendRenderers)
            {
                _blenderRenderbySkinnedRenderer.Add(currBlend.Value._renderer, currBlend.Value);
            }
        }
        #endregion

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            private class GroupParameter
            {
                public readonly BlendShapesEditor editor;
                public readonly string rendererPath;
                private BlendRenderer _renderer;
                private readonly int _hashCode;

                public BlendRenderer blendRenderer
                {
                    get
                    {
                        if (_renderer == null)
                        {
                            BlendRenderer BlendRenderer;

                            if (editor._blendRenderers.TryGetValue(FixFullPath(rendererPath), out BlendRenderer))
                            {
                                _renderer = BlendRenderer;
                            }
                        }

                        return _renderer;
                    }
                }

                public GroupParameter(BlendShapesEditor editor, BlendRenderer renderer)
                {
                    this.editor = editor;
                    _renderer = renderer;
                    rendererPath = _renderer._renderer.transform.GetPathFrom(this.editor._parent.transform);

                    unchecked
                    {
                        int hash = 17;
                        hash = hash * 31 + (this.editor != null ? this.editor.GetHashCode() : 0);
                        _hashCode = hash * 31 + (rendererPath != null ? rendererPath.GetHashCode() : 0);
                    }
                }

                public GroupParameter(BlendShapesEditor editor, string rendererPath)
                {
                    this.editor = editor;
                    this.rendererPath = rendererPath;

                    unchecked
                    {
                        int hash = 17;
                        hash = hash * 31 + (this.editor != null ? this.editor.GetHashCode() : 0);
                        _hashCode = hash * 31 + (this.rendererPath != null ? this.rendererPath.GetHashCode() : 0);
                    }
                }

                public override int GetHashCode()
                {
                    return _hashCode;
                }

                public override string ToString()
                {
                    return $"editor: {editor}, rendererPath: {rendererPath}, renderer: {_renderer._renderer}";
                }
            }

            private class IndividualParameter : GroupParameter
            {
                public readonly int index;
                private readonly int _hashCode;

                public IndividualParameter(BlendShapesEditor editor, BlendRenderer renderer, int index) : base(editor, renderer)
                {
                    this.index = index;

                    unchecked
                    {
                        _hashCode = base.GetHashCode() * 31 + this.index.GetHashCode();
                    }
                }

                public IndividualParameter(BlendShapesEditor editor, string rendererPath, int index) : base(editor, rendererPath)
                {
                    this.index = index;

                    unchecked
                    {
                        _hashCode = base.GetHashCode() * 31 + this.index.GetHashCode();
                    }
                }

                public override int GetHashCode()
                {
                    return _hashCode;
                }

                public override string ToString()
                {
                    return $"{base.ToString()}, index: {index}";
                }
            }

            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "lastBlendShape",
                        name: "BlendShape (Last Modified)",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            IndividualParameter p = (IndividualParameter)parameter;
                            if (p.editor._isBusy == false && p.index >= 0)
                            {
                                float weight = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
                                p.blendRenderer.SetBlendShapeWeight(p.index, weight);

                                if (p.editor._linkEyesComponents)
                                {
                                    p.blendRenderer.ApplyLink(weight, p.index);
                                }
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: oci => oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null,
                        getValue: (oci, parameter) =>
                        {
                            IndividualParameter p = (IndividualParameter)parameter;
                            return p.blendRenderer.GetBlendShapeWeight(p.index);
                        },
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: oci =>
                        {
                            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                            return new IndividualParameter(controller._blendShapesEditor, controller._blendShapesEditor._skinnedMeshTarget, controller._blendShapesEditor._lastEditedBlendShape);
                        },
                        readParameterFromXml: (oci, node) => new IndividualParameter(oci.guideObject.transformTarget.GetComponent<PoseController>()._blendShapesEditor, node.Attributes["parameter1"].Value, node.ReadInt("parameter2")),
                        writeParameterToXml: (oci, writer, o) =>
                        {
                            IndividualParameter p = (IndividualParameter)o;
                            writer.WriteAttributeString("parameter1", p.rendererPath);
                            writer.WriteValue("parameter2", p.index);
                        },
                        checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                        {
                            if (parameter == null)
                                return false;
                            IndividualParameter p = (IndividualParameter)parameter;
                            if (p.editor == null)
                                return false;
                            if (p.editor._isBusy)
                                return true;
                            if (p.blendRenderer == null || p.blendRenderer._renderer == null)
                                return false;
                            return true;
                        },
                        getFinalName: (name, oci, parameter) =>
                        {
                            IndividualParameter p = (IndividualParameter)parameter;
                            string skinnedMeshName = null;
                            if (p.blendRenderer._renderer != null && _skinnedMeshAliases.TryGetValue(p.blendRenderer._renderer.name, out skinnedMeshName) == false)
                                skinnedMeshName = p.blendRenderer._renderer.name;
                            return $"BS ({skinnedMeshName} {p.index})";
                        });

                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "groupBlendShape",
                        name: "BlendShape (Group)",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            GroupParameter p = (GroupParameter)parameter;

                            if (p.editor._isBusy == false)
                            {
                                BlendRenderer renderer = p.blendRenderer;
                                float[] left = (float[])leftValue;
                                float[] right = (float[])rightValue;
                                for (int i = 0; i < left.Length; i++)
                                {
                                    float weight = Mathf.LerpUnclamped(left[i], right[i], factor);
                                    renderer.SetBlendShapeWeight(i, weight);

                                    if (p.editor._linkEyesComponents)
                                    {
                                        p.blendRenderer.ApplyLink(weight, i);
                                    }
                                }
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: oci => oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null,
                        getValue: GetGroupValue,
                        readValueFromXml: ReadGroupValueFromXml,
                        writeValueToXml: WriteGroupValueToXml,
                        getParameter: GetGroupParameter,
                        readParameterFromXml: ReadGroupParameterFromXml,
                        writeParameterToXml: WriteGroupParameterToXml,
                        checkIntegrity: CheckGroupIntegrity,
                        getFinalName: (name, oci, parameter) =>
                        {
                            GroupParameter p = (GroupParameter)parameter;
                            string skinnedMeshName = null;
                            if (p.blendRenderer._renderer != null && _skinnedMeshAliases.TryGetValue(p.blendRenderer._renderer.name, out skinnedMeshName) == false)
                                skinnedMeshName = p.blendRenderer._renderer.name;
                            return $"BS ({skinnedMeshName})";
                        });
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "groupBlendShapeLink",
                        name: "BlendShape (Group, Link)",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            GroupParameter p = (GroupParameter)parameter;

                            if (p.editor._isBusy == false)
                            {
                                BlendRenderer renderer = p.blendRenderer;
                                float[] left = (float[])leftValue;
                                float[] right = (float[])rightValue;
                                int targetLinkMaxCount = p.editor._target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount;
                                for (int index = 0; index < left.Length; ++index)
                                {
                                    float weight = Mathf.LerpUnclamped(left[index], right[index], factor);
                                    renderer.SetBlendShapeWeight(index, weight);

                                    renderer.ApplyLink(weight, index);
                                }
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: oci => oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && oci.guideObject.transformTarget.GetComponent<CharaPoseController>() != null,
                        getValue: GetGroupValue,
                        readValueFromXml: ReadGroupValueFromXml,
                        writeValueToXml: WriteGroupValueToXml,
                        getParameter: GetGroupParameter,
                        readParameterFromXml: ReadGroupParameterFromXml,
                        writeParameterToXml: WriteGroupParameterToXml,
                        checkIntegrity: CheckGroupIntegrity,
                        getFinalName: (name, oci, parameter) =>
                        {
                            GroupParameter p = (GroupParameter)parameter;
                            string skinnedMeshName = null;
                            if (p.blendRenderer._renderer != null && _skinnedMeshAliases.TryGetValue(p.blendRenderer._renderer.name, out skinnedMeshName) == false)
                                skinnedMeshName = p.blendRenderer._renderer.name;
                            return $"BS ({skinnedMeshName}, L)";
                        });
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "groupBlendShapeNameLink",
                        name: "BlendShape (Last Modified, Name Link)",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            IndividualParameter p = (IndividualParameter)parameter;
                            if (p.editor._isBusy == false && p.index >= 0)
                            {
                                float weight = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
                                var name = p.blendRenderer.GetBlendShapeName(p.index);
                                foreach (var blendRenderer in p.editor._blendRenderers)
                                    p.blendRenderer.SetBlendShapeWeight(name, weight);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: oci => oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null,
                        getValue: (oci, parameter) =>
                        {
                            IndividualParameter p = (IndividualParameter)parameter;
                            return p.blendRenderer.GetBlendShapeWeight(p.index);
                        },
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: oci =>
                        {
                            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                            return new IndividualParameter(controller._blendShapesEditor, controller._blendShapesEditor._skinnedMeshTarget, controller._blendShapesEditor._lastEditedBlendShape);
                        },
                        readParameterFromXml: (oci, node) => new IndividualParameter(oci.guideObject.transformTarget.GetComponent<PoseController>()._blendShapesEditor, node.Attributes["parameter1"].Value, node.ReadInt("parameter2")),
                        writeParameterToXml: (oci, writer, o) =>
                        {
                            IndividualParameter p = (IndividualParameter)o;
                            writer.WriteAttributeString("parameter1", p.rendererPath);
                            writer.WriteValue("parameter2", p.index);
                        },
                         checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                         {
                             if (parameter == null)
                                 return false;
                             IndividualParameter p = (IndividualParameter)parameter;
                             if (p.editor == null)
                                 return false;
                             if (p.editor._isBusy)
                                 return true;
                             if (p.blendRenderer == null || p.blendRenderer._renderer == null)
                                 return false;
                             return true;
                         },
                         getFinalName: (name, oci, parameter) =>
                         {
                             IndividualParameter p = (IndividualParameter)parameter;
                             string skinnedMeshName = null;
                             if (p.blendRenderer._renderer != null && _skinnedMeshAliases.TryGetValue(p.blendRenderer._renderer.name, out skinnedMeshName) == false)
                                 skinnedMeshName = p.blendRenderer._renderer.name;
                             return $"BS ({skinnedMeshName} {p.index}) NameLink";
                         });


                //TODO maybe do that, or maybe not, idk
                //ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                //        owner: HSPE._name,
                //        id: "everythingBlendShape",
                //        name: "BlendShape (Everything)",
                //        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                //        {
                //            HashedPair<BlendShapesEditor, SkinnedMeshRenderer> pair = (HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)parameter;
                //            float[] left = (float[])leftValue;
                //            float[] right = (float[])rightValue;
                //            for (int i = 0; i < left.Length; i++)
                //                pair.key.SetBlendShapeWeight(pair.value, i, Mathf.LerpUnclamped(left[i], right[i], factor));
                //        },
                //        interpolateAfter: null,
                //        isCompatibleWithTarget: oci => oci != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null,
                //        getValue: (oci, parameter) =>
                //        {
                //            HashedPair<BlendShapesEditor, SkinnedMeshRenderer> pair = (HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)parameter;
                //            float[] value = new float[pair.value.sharedMesh.blendShapeCount];
                //            for (int i = 0; i < value.Length; ++i)
                //                value[i] = pair.value.GetBlendShapeWeight(i);
                //            return value;
                //        },
                //        readValueFromXml: node =>
                //        {
                //            float[] value = new float[node.ReadInt("valueCount")];
                //            for (int i = 0; i < value.Length; i++)
                //                value[i] = node.ReadFloat($"value{i}");
                //            return value;
                //        },
                //        writeValueToXml: (writer, o) =>
                //        {
                //            float[] value = (float[])o;
                //            writer.WriteValue("valueCount", value.Length);
                //            for (int i = 0; i < value.Length; i++)
                //                writer.WriteValue($"value{i}", value[i]);
                //        },
                //        getParameter: oci => oci.guideObject.transformTarget.GetComponent<PoseController>()._blendShapesEditor,
                //        checkIntegrity: (oci, parameter) => parameter != null,
                //        getFinalName: (name, oci, parameter) => "BS (Everything)");
            }

            private static object GetGroupParameter(ObjectCtrlInfo oci)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new GroupParameter(controller._blendShapesEditor, controller._blendShapesEditor._skinnedMeshTarget);
            }

            private static object GetGroupValue(ObjectCtrlInfo oci, object parameter)
            {
                GroupParameter p = (GroupParameter)parameter;
                SkinnedMeshRenderer renderer = p.blendRenderer._renderer;
                float[] value = new float[renderer.sharedMesh.blendShapeCount];
                for (int i = 0; i < value.Length; ++i)
                    value[i] = renderer.GetBlendShapeWeight(i);
                return value;
            }
            private static object ReadGroupValueFromXml(object parameter, XmlNode node)
            {
                GroupParameter p = (GroupParameter)parameter;
                SkinnedMeshRenderer renderer = p.blendRenderer._renderer;
                int count = node.ReadInt("valueCount");
                if (renderer.sharedMesh.blendShapeCount != count)
                {
                    Dictionary<int, float> tempValue = new Dictionary<int, float>();
                    for (int i = 0; i < count; i++)
                    {
                        string nameKey = $"name{i}";
                        if (node.Attributes[nameKey] != null)
                        {
                            string n = node.Attributes[nameKey].Value;
                            int index = renderer.sharedMesh.GetBlendShapeIndex(n);
                            if (index != -1)
                            {
                                if (tempValue.ContainsKey(index))
                                    tempValue[index] = node.ReadFloat($"value{i}");
                                else
                                    tempValue.Add(index, node.ReadFloat($"value{i}"));
                            }
                        }
                        else
                            tempValue.Add(i, node.ReadFloat($"value{i}"));
                    }
                    float[] value = new float[renderer.sharedMesh.blendShapeCount];
                    for (int i = 0; i < value.Length; i++)
                    {
                        float v;
                        if (tempValue.TryGetValue(i, out v))
                            value[i] = v;
                        else
                            value[i] = 0;
                    }
                    return value;
                }
                else
                {
                    float[] value = new float[count];
                    for (int i = 0; i < value.Length; i++)
                        value[i] = node.ReadFloat($"value{i}");
                    return value;
                }
            }
            private static void WriteGroupValueToXml(object parameter, XmlTextWriter writer, object o)
            {
                GroupParameter p = (GroupParameter)parameter;
                SkinnedMeshRenderer renderer = p.blendRenderer._renderer;
                float[] value = (float[])o;
                writer.WriteValue("valueCount", value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.WriteValue($"value{i}", value[i]);
                    writer.WriteAttributeString($"name{i}", renderer.sharedMesh.GetBlendShapeName(i));
                }
            }
            private static object ReadGroupParameterFromXml(ObjectCtrlInfo oci, XmlNode node)
            {
                return new GroupParameter(oci.guideObject.transformTarget.GetComponent<PoseController>()._blendShapesEditor, node.Attributes["parameter"].Value);
            }
            private static void WriteGroupParameterToXml(ObjectCtrlInfo oci, XmlTextWriter writer, object o)
            {
                writer.WriteAttributeString("parameter", ((GroupParameter)o).rendererPath);
            }
            private static bool CheckGroupIntegrity(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue)
            {
                if (parameter == null || leftValue == null || rightValue == null)
                {
                    return false;
                }

                GroupParameter groupParameter = (GroupParameter)parameter;
                return groupParameter.editor != null && (groupParameter.editor._isBusy || groupParameter.blendRenderer != null);
            }
        }
        #endregion
    }
}