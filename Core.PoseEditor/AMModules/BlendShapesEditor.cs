using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using BepInEx;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
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

#if KOIKATSU
        private class SkinnedMeshRendererWrapper
        {
            public SkinnedMeshRenderer renderer;
            public List<SkinnedMeshRendererWrapper> links;
        }
#endif

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
        private Vector2 _skinnedMeshRenderersScroll;
        private Vector2 _blendShapesScroll;
        private readonly Dictionary<string, SkinnedMeshRenderer> _skinnedMeshRenderers = new Dictionary<string, SkinnedMeshRenderer>();
        private readonly Dictionary<SkinnedMeshRenderer, string> _rendererNames = new Dictionary<SkinnedMeshRenderer, string>();
        //private readonly Dictionary<string, Dictionary<string, BlendShapeData>> _blendShapeData = new Dictionary<string, Dictionary<string, BlendShapeData>>();
        private readonly Dictionary<string, Dictionary<string, BlendShapeData>> _dirtySkinnedMeshRenderers = new Dictionary<string, Dictionary<string, BlendShapeData>>();
        //private int _headlessReconstructionTimeout = 0;
        private SkinnedMeshRenderer _headRenderer;
        private SkinnedMeshRenderer _skinnedMeshTarget;
        private bool _linkEyesComponents = true;
        private readonly Dictionary<string, Dictionary<int, string>> _oriHeadBlendIndex = new Dictionary<string, Dictionary<int, string>>();
#if KOIKATSU
        private readonly Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> _links = new Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererWrapper>();
#elif HONEYSELECT2 || AISHOUJO
        private readonly Dictionary<string, Dictionary<string, List<BlendLinkData>>> _links = new Dictionary<string, Dictionary<string, List<BlendLinkData>>>();
#endif
        private string _search = "";
        private readonly GenericOCITarget _target;
        private readonly Dictionary<XmlNode, SkinnedMeshRenderer> _secondPassLoadingNodes = new Dictionary<XmlNode, SkinnedMeshRenderer>();
        private bool _showSaveLoadWindow = false;
        private Vector2 _presetsScroll;
        private string _presetName = "";
        private bool _removePresetMode;
        private int _renameIndex = -1;
        private string _renameString = "";
        private int _lastEditedBlendShape = -1;
        private bool _isBusy = false;
#endregion

        #region Public Fields
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BlendShapes; } }
        public override string displayName { get { return "Blend Shapes"; } }
        public override bool shouldDisplay { get { return _skinnedMeshRenderers.Any(r => r.Value != null && r.Value.sharedMesh != null && r.Value.sharedMesh.blendShapeCount > 0); } }
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
            MainWindow._self.ExecuteDelayed(() =>
            {
                if (_parent == null) return;

                RefreshSkinnedMeshRendererList();
                if (_target.type == GenericOCITarget.Type.Character)
                    Init();
            });
        }


        private string GetRendererName(SkinnedMeshRenderer renderer)
        {
            string rendererName = null;
            if (renderer != null)
            {
                _rendererNames.TryGetValue(renderer, out rendererName);
            }

            return rendererName;
        }

        private string GetBlendShapeName(SkinnedMeshRenderer renderer, int index)
        {
            string blendShapeName = null;
            string rendererName = GetRendererName(renderer);
            if (index > -1 && rendererName != null && renderer.sharedMesh.blendShapeCount > index)
            {
                if (_oriHeadBlendIndex.TryGetValue(rendererName, out var ohbi))
                {
                    ohbi.TryGetValue(index, out blendShapeName);
                }
                else
                {
                    blendShapeName = renderer.sharedMesh.GetBlendShapeName(index);
                }
            }

            return blendShapeName;
        }

        private void SetBlendShapeWeightFromKey(SkinnedMeshRenderer renderer, string key, float value)
        {
            int blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(key);
            if (blendShapeIndex == -1)
            {
                return;
            }

            renderer.SetBlendShapeWeight(blendShapeIndex, value);
        }

        private void LateUpdate()
        {
            if (_target.type != GenericOCITarget.Type.Item)
                return;

            ApplyBlendShapeWeights();
        }

        public void OnGUI()
        {
            if (_showSaveLoadWindow == false)
                return;
            Rect windowRect = Rect.MinMaxRect(MainWindow._self._advancedModeRect.xMin - 180, MainWindow._self._advancedModeRect.yMin, MainWindow._self._advancedModeRect.xMin, MainWindow._self._advancedModeRect.yMax);
            IMGUIExtensions.DrawBackground(windowRect);
            GUILayout.Window(MainWindow._uniqueId + 1, windowRect, SaveLoadWindow, "Presets");
        }

        private void OnDisable()
        {
            if (_dirtySkinnedMeshRenderers.Count != 0)
            {
                foreach (var skinnedMeshRenderer in _dirtySkinnedMeshRenderers)
                {
                    _skinnedMeshRenderers.TryGetValue(skinnedMeshRenderer.Key, out var renderer);
                    foreach (var keyValuePair in skinnedMeshRenderer.Value)
                        SetBlendShapeWeightFromKey(renderer, keyValuePair.Key, keyValuePair.Value.originalWeight);
                }
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
            _links.Clear();

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
            foreach (var skinnedMeshRenderer in _skinnedMeshRenderers)
            {
                if (skinnedMeshRenderer.Value != null && skinnedMeshRenderer.Value.sharedMesh != null && skinnedMeshRenderer.Value.sharedMesh.blendShapeCount != 0)
                {
                    if (_dirtySkinnedMeshRenderers[skinnedMeshRenderer.Key].Count > 0)
                        GUI.color = Color.magenta;
                    if (skinnedMeshRenderer.Value == _skinnedMeshTarget)
                        GUI.color = Color.cyan;
                    if (!_skinnedMeshAliases.TryGetValue(skinnedMeshRenderer.Key, out var key))
                        key = skinnedMeshRenderer.Key;
                    if (GUILayout.Button(key + ((GUI.color == Color.magenta) ? "*" : "")))
                    {
                        _skinnedMeshTarget = skinnedMeshRenderer.Value;
                        _lastEditedBlendShape = -1;
                    }
                    GUI.color = c;
                }
            }
            GUILayout.EndScrollView();

            GUI.color = Color.green;
            if (GUILayout.Button("Save/Load preset"))
            {
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
            if (GUILayout.Button("NonOriChar"))
                _oriHeadBlendIndex.Clear();
            if (_oriHeadBlendIndex.Count > 0)
                GUI.color = Color.red;
            if (GUILayout.Button("OriginChar"))
            {
                _oriHeadBlendIndex.Clear();
                foreach (var skinnedMeshRenderer in _skinnedMeshRenderers)
                {
                    _oriHeadBlendIndex.Add(skinnedMeshRenderer.Key, new Dictionary<int, string>());
                    for (int key = 0; key < skinnedMeshRenderer.Value.sharedMesh.blendShapeCount; ++key)
                    {
                        if (!_oriHeadBlendIndex[skinnedMeshRenderer.Key].ContainsKey(key))
                            _oriHeadBlendIndex[skinnedMeshRenderer.Key].Add(key, skinnedMeshRenderer.Value.sharedMesh.GetBlendShapeName(key));
                    }
                }
            }
            GUI.color = c;
            GUILayout.EndVertical();


            if (_skinnedMeshTarget != null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search", GUILayout.ExpandWidth(false));
                _search = GUILayout.TextField(_search, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    _search = "";
                GUILayout.EndHorizontal();

                _blendShapesScroll = GUILayout.BeginScrollView(_blendShapesScroll, false, true, GUILayout.ExpandWidth(false));

                string rendererName1 = GetRendererName(_skinnedMeshTarget);
                _dirtySkinnedMeshRenderers.TryGetValue(rendererName1, out var data);
                bool zeroResult = true;
                for (int index = 0; index < _skinnedMeshTarget.sharedMesh.blendShapeCount; ++index)
                {
                    string blendShapeName = GetBlendShapeName(_skinnedMeshTarget, index);
                    if (blendShapeName != null)
                    {
                        int blendShapeIndex = _skinnedMeshTarget.sharedMesh.GetBlendShapeIndex(blendShapeName);

                        if ((_headRenderer == _skinnedMeshTarget) && (_target.isFemale ? _femaleSeparators : _maleSeparators).TryGetValue(index, out var str1))
                        {
                            GUILayout.Label(str1, GUI.skin.box);
                        }

                        if (!_blendShapeAliases.TryGetValue(blendShapeName, out var str2))
                        {
                            str2 = null;
                        }

                        if (str2 != null && str2.IndexOf(_search, StringComparison.CurrentCultureIgnoreCase) != -1 || blendShapeName.IndexOf(_search, StringComparison.CurrentCultureIgnoreCase) != -1)
                        {
                            zeroResult = false;
                            float num1;
                            if (data != null && data.TryGetValue(blendShapeName, out var blendShapeData1))
                            {
                                num1 = blendShapeData1.weight;
                                GUI.color = Color.magenta;
                            }
                            else
                            {
                                num1 = _skinnedMeshTarget.GetBlendShapeWeight(blendShapeIndex);
                            }

                            GUILayout.BeginHorizontal();
                            GUILayout.BeginVertical(GUILayout.ExpandHeight(false));
                            GUILayout.BeginHorizontal();
                            if (_renameIndex != index)
                            {
                                GUILayout.Label(string.Format("{0} {1}", index, str2 == null ? blendShapeName : str2));
                                GUILayout.FlexibleSpace();
                            }
                            else
                            {
                                GUILayout.Label(index.ToString(), GUILayout.ExpandWidth(false));
                                _renameString = GUILayout.TextField(_renameString, GUILayout.ExpandWidth(true));
                            }
                            if (GUILayout.Button(_renameIndex != index ? "Rename" : "Save", GUILayout.ExpandWidth(false)))
                            {
                                if (_renameIndex != index)
                                {
                                    _renameIndex = index;
                                    _renameString = str2 == null ? blendShapeName : str2;
                                }
                                else
                                {
                                    _renameIndex = -1;
                                    _renameString = _renameString.Trim();
                                    if (_renameString.IsNullOrEmpty() || _renameString == blendShapeName)
                                    {
                                        if (_blendShapeAliases.ContainsKey(blendShapeName))
                                        {
                                            _blendShapeAliases.Remove(blendShapeName);
                                        }
                                    }
                                    else
                                    {
                                        _blendShapeAliases[blendShapeName] = _renameString;
                                    }
                                }
                            }
                            GUILayout.Label(num1.ToString("000"), GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            float num2 = GUILayout.HorizontalSlider(num1, 0.0f, 100f);
                            if (GUILayout.Button("-1", GUILayout.ExpandWidth(false)))
                                --num2;

                            if (GUILayout.Button("+1", GUILayout.ExpandWidth(false)))
                                ++num2;

                            float weight = Mathf.Clamp(num2, 0.0f, 100f);
                            GUILayout.EndHorizontal();

                            if (!Mathf.Approximately(weight, num1))
                            {
                                _lastEditedBlendShape = blendShapeIndex;
                                SetBlendShapeWeight(_skinnedMeshTarget, blendShapeIndex, weight);

#if KOIKATSU
                                if (_linkEyesComponents && blendShapeIndex < (_target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount))
                                {
                                    SkinnedMeshRendererWrapper wrapper;
                                    if (_links.TryGetValue(_skinnedMeshTarget, out wrapper))
                                    {
                                        foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                        {
                                            if (blendShapeIndex < link.renderer.sharedMesh.blendShapeCount)
                                                SetBlendShapeWeight(link.renderer, blendShapeIndex, weight);
                                        }
                                    }
                                }
#endif
                            }
                            GUILayout.EndVertical();
                            GUI.color = Color.red;
                            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false), GUILayout.Height(50f)) && data != null && data.TryGetValue(blendShapeName, out blendShapeData1))
                            {
                                _skinnedMeshTarget.SetBlendShapeWeight(blendShapeIndex, blendShapeData1.originalWeight);
                                data.Remove(blendShapeName);
#if KOIKATSU
                                if (_linkEyesComponents && blendShapeIndex < (_target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount))
                                {
                                    SkinnedMeshRendererWrapper wrapper;
                                    if (_links.TryGetValue(_skinnedMeshTarget, out wrapper))
                                    {
                                        foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                        {
                                            string linkBSName = GetBlendShapeName(link.renderer, blendShapeIndex);
                                            if (_dirtySkinnedMeshRenderers.TryGetValue(GetRendererName(link.renderer), out var data2) && data2.TryGetValue(linkBSName, out var bsData))
                                            {
                                                link.renderer.SetBlendShapeWeight(blendShapeIndex, bsData.originalWeight);
                                                data2.Remove(linkBSName);
                                            }
                                        }
                                    }
                                }
#elif AISHOUJO || HONEYSELECT2
                                if (_linkEyesComponents
                                        && index < (_target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount)
                                        && _links.TryGetValue(rendererName1, out var dictionary2)
                                        && dictionary2.TryGetValue(blendShapeName, out var blendLinkDataList))
                                {
                                    foreach (BlendLinkData blendLinkData in blendLinkDataList)
                                    {
                                        string rendererName2 = GetRendererName(blendLinkData.renderer);
                                        if (rendererName2 != null)
                                        {
                                            _dirtySkinnedMeshRenderers[rendererName2].TryGetValue(blendLinkData.blendName, out var blendShapeData2);
                                            blendLinkData.renderer.SetBlendShapeWeight(blendLinkData.renderer.sharedMesh.GetBlendShapeIndex(blendLinkData.blendName), blendShapeData2.originalWeight);
                                            _dirtySkinnedMeshRenderers[rendererName2].Remove(blendLinkData.blendName);
                                        }
                                    }
                                }
#endif

                            }
                            GUILayout.EndHorizontal();
                            GUI.color = c;
                        }
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
#if KOIKATSU
                    if (_linkEyesComponents)
                    {
                        SkinnedMeshRendererWrapper wrapper;
                        if (_links.TryGetValue(_skinnedMeshTarget, out wrapper))
                            foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                SetMeshRendererNotDirty(link.renderer);
                    }
#elif AISHOUJO || HONEYSELECT2
                    if (_linkEyesComponents
                       && _dirtySkinnedMeshRenderers.TryGetValue(rendererName1, out var dictionary3)
                       && _links.TryGetValue(rendererName1, out var dictionary4))
                    {
                        foreach (var keyValuePair in dictionary3)
                        {
                            if (dictionary4.TryGetValue(keyValuePair.Key, out var blendLinkDataList))
                            {
                                foreach (BlendLinkData blendLinkData in blendLinkDataList)
                                {
                                    string rendererName3 = GetRendererName(blendLinkData.renderer);
                                    if (rendererName3 != null)
                                    {
                                        _dirtySkinnedMeshRenderers[rendererName3].TryGetValue(blendLinkData.blendName, out var blendShapeData);
                                        blendLinkData.renderer.SetBlendShapeWeight(blendLinkData.renderer.sharedMesh.GetBlendShapeIndex(blendLinkData.blendName), blendShapeData.originalWeight);
                                        _dirtySkinnedMeshRenderers[rendererName3].Remove(blendLinkData.blendName);
                                    }
                                }
                            }
                        }
                    }
#endif
                    SetMeshRendererNotDirty(_skinnedMeshTarget);
                }
                GUI.color = c;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
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

        private BlendShapeData SetBlendShapeWeight(SkinnedMeshRenderer renderer, int index, float weight)
        {
            string blendShapeName = GetBlendShapeName(renderer, index);
            BlendShapeData blendShapeData = null;
            if (blendShapeName != null)
            {
                blendShapeData = SetBlendShapeDirty(renderer, blendShapeName);
                blendShapeData.weight = weight;
#if AISHOUJO || HONEYSELECT2
                if (_linkEyesComponents && index < (_target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount) && _links.TryGetValue(GetRendererName(renderer), out var dictionary) && dictionary.TryGetValue(blendShapeName, out var blendLinkDataList))
                {
                    foreach (BlendLinkData blendLinkData in blendLinkDataList)
                    {
                        SetBlendShapeDirty(blendLinkData.renderer, blendLinkData.blendName).weight = weight;
                    }
                }
#endif
            }

            return blendShapeData;
        }

        public void LoadFrom(BlendShapesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (var skinnedMeshRenderer in other._dirtySkinnedMeshRenderers)
                {
                    if (other._dirtySkinnedMeshRenderers.TryGetValue(skinnedMeshRenderer.Key, out var dictionary))
                    {
                        skinnedMeshRenderer.Value.Clear();
                        foreach (var keyValuePair in dictionary)
                        {
                            skinnedMeshRenderer.Value.Add(keyValuePair.Key, keyValuePair.Value);
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
            if (_dirtySkinnedMeshRenderers.Count != 0)
            {
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

                foreach (var skinnedMeshRenderer1 in _dirtySkinnedMeshRenderers)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer2 = _skinnedMeshRenderers[skinnedMeshRenderer1.Key];
                    xmlWriter.WriteStartElement("skinnedMesh");
                    xmlWriter.WriteAttributeString("name", skinnedMeshRenderer2.transform.GetPathFrom(_parent.transform));
                    foreach (var keyValuePair in skinnedMeshRenderer1.Value)
                    {
                        xmlWriter.WriteStartElement("blendShape");
                        xmlWriter.WriteAttributeString("index", XmlConvert.ToString(skinnedMeshRenderer2.sharedMesh.GetBlendShapeIndex(keyValuePair.Key)));
                        xmlWriter.WriteAttributeString("weight", XmlConvert.ToString(keyValuePair.Value.weight));
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                    ++written;
                }

                xmlWriter.WriteEndElement();
            }

            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            ResetAll();
            RefreshSkinnedMeshRendererList();

            bool changed = false;
            XmlNode skinnedMeshesNode = xmlNode.FindChildNode("skinnedMeshes");
            Dictionary<XmlNode, SkinnedMeshRenderer> potentialChildrenNodes = new Dictionary<XmlNode, SkinnedMeshRenderer>();
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
                        if (_skinnedMeshRenderers.TryGetValue(node.Attributes["name"].Value.Substring(node.Attributes["name"].Value.LastIndexOf('/') + 1), out var renderer))
                        {
                            potentialChildrenNodes.Add(node, renderer);

                            if (LoadSingleSkinnedMeshRenderer(node, renderer))
                                changed = true;
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
                    PoseController childController = pair.Value.GetComponentInParent<PoseController>();
                    if (childController != _parent)
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
                        string rendererName = GetRendererName(pair.Value);
                        if (rendererName != null)
                        {
                            if (_skinnedMeshRenderers.TryGetValue(rendererName, out SkinnedMeshRenderer _))
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
        private bool LoadSingleSkinnedMeshRenderer(XmlNode node, SkinnedMeshRenderer renderer)
        {
            bool loaded = false;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                int index = XmlConvert.ToInt32(childNode.Attributes["index"].Value);
                string blendShapeName = GetBlendShapeName(renderer, index);
                if (blendShapeName == null || index >= renderer.sharedMesh.blendShapeCount)
                    continue;
                loaded = true;
                BlendShapeData blendShapeData = SetBlendShapeDirty(renderer, blendShapeName);
                blendShapeData.originalWeight = renderer.GetBlendShapeWeight(index);
                blendShapeData.weight = XmlConvert.ToSingle(childNode.Attributes["weight"].Value);
            }

            return loaded;
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
                        LoadPreset(preset + ".xml");
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
            if (!File.Exists(path))
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            LoadXml(doc.FirstChild);
        }

        private void DeletePreset(string name)
        {
            File.Delete(Path.GetFullPath(Path.Combine(_presetsPath, name)));
            _removePresetMode = false;
            RefreshPresets();
        }

        private void Init()
        {
            _headRenderer = null;
#if HONEYSELECT
            _instanceByFaceBlendShape.Add(this._target.ociChar.charBody.fbsCtrl, this);
#elif PLAYHOME
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.human, this);
#elif KOIKATSU
            _instanceByFaceBlendShape.Add(_target.ociChar.charInfo.fbsCtrl, this);
#elif AISHOUJO || HONEYSELECT2
            _instanceByFaceBlendShape.Add(_target.ociChar.charInfo.fbsCtrl, this);
#endif

            List<SkinnedMeshRenderer> skinnedMeshRendererList = new List<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in _skinnedMeshRenderers)
            {
                string key = skinnedMeshRenderer.Key;

                switch (key)
                {
#if HONEYSELECT || KOIKATSU || PLAYHOME
                    case "cf_O_head":
                    case "cf_O_face":
#elif AISHOUJO || HONEYSELECT2
                    case "o_head":
#endif
                        _headRenderer = skinnedMeshRenderer.Value;
                        break;
                }

                switch (key)
                {
#if HONEYSELECT || PLAYHOME
                    case "cf_O_head":
                    case "cf_O_matuge":
                    case "cf_O_namida01":
                    case "cf_O_namida02":
#elif KOIKATSU
                    case "cf_O_face":
                    case "cf_O_eyeline":
                    case "cf_O_eyeline_low":
                    case "cf_Ohitomi_L":
                    case "cf_Ohitomi_R":
                    case "cf_O_namida_L":
                    case "cf_O_namida_M":
                    case "cf_O_namida_S":
#elif AISHOUJO || HONEYSELECT2
                    case "o_eyelashes":
                    case "o_namida":
                    case "o_head":
#endif

#if KOIKATSU
                        SkinnedMeshRendererWrapper wrapper = new SkinnedMeshRendererWrapper
                        {
                            renderer = skinnedMeshRenderer.Value,
                            links = new List<SkinnedMeshRendererWrapper>()
                        };

                        if (!_links.ContainsKey(skinnedMeshRenderer.Value))
                            _links.Add(skinnedMeshRenderer.Value, wrapper);

#elif AISHOUJO || HONEYSELECT2
                        skinnedMeshRendererList.Add(skinnedMeshRenderer.Value);
#endif

                        break;
                }


            }

#if KOIKATSU
            foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> pair in _links)
            {
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> pair2 in _links)
                {
                    if (pair.Key != pair2.Key)
                        pair.Value.links.Add(pair2.Value);
                }
            }
#elif AISHOUJO || HONEYSELECT2
            List<Dictionary<string, string>> dictionaryList = new List<Dictionary<string, string>>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRendererList)
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                for (int index = 0; index < skinnedMeshRenderer.sharedMesh.blendShapeCount; ++index)
                {
                    string blendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(index);
                    dictionary.Add(blendShapeName.Substring(blendShapeName.IndexOf(".", StringComparison.Ordinal) + 1), blendShapeName);
                }
                dictionaryList.Add(dictionary);
            }

            for (int index1 = 0; index1 < skinnedMeshRendererList.Count; ++index1)
            {
                string rendererName = GetRendererName(skinnedMeshRendererList[index1]);
                if (rendererName != null)
                {
                    _links.Add(rendererName, new Dictionary<string, List<BlendLinkData>>());
                    SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRendererList[index1];
                    for (int index2 = 0; index2 < skinnedMeshRenderer.sharedMesh.blendShapeCount; ++index2)
                    {
                        string blendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(index2);
                        string key = blendShapeName.Substring(blendShapeName.IndexOf(".", StringComparison.Ordinal) + 1);
                        _links[rendererName].Add(blendShapeName, new List<BlendLinkData>());
                        for (int index3 = 0; index3 < skinnedMeshRendererList.Count; ++index3)
                        {
                            if (index3 != index1 && dictionaryList[index3].TryGetValue(key, out var str))
                            {
                                _links[rendererName][blendShapeName].Add(new BlendLinkData()
                                {
                                    renderer = skinnedMeshRendererList[index3],
                                    blendName = str
                                });
                            }

                        }
                    }
                }
            }
#endif
        }

        private void ResetAll()
        {
            foreach (var skinnedMeshRenderer in _skinnedMeshRenderers)
            {
                foreach (var keyValuePair in _dirtySkinnedMeshRenderers[skinnedMeshRenderer.Key])
                {
                    SetBlendShapeWeightFromKey(skinnedMeshRenderer.Value, keyValuePair.Key, keyValuePair.Value.originalWeight);
                }

                _dirtySkinnedMeshRenderers[skinnedMeshRenderer.Key].Clear();
            }
        }

        private void FaceBlendShapeOnPostLateUpdate()
        {
            if (_parent.enabled)
                ApplyBlendShapeWeights();
        }

        private void ApplyBlendShapeWeights()
        {
            if (_dirtySkinnedMeshRenderers.Count == 0)
            {
                return;
            }

            foreach (var skinnedMeshRenderer in _dirtySkinnedMeshRenderers)
            {
                _skinnedMeshRenderers.TryGetValue(skinnedMeshRenderer.Key, out var renderer);
                foreach (var keyValuePair in skinnedMeshRenderer.Value)
                {
                    SetBlendShapeWeightFromKey(renderer, keyValuePair.Key, keyValuePair.Value.weight);
                }
            }
        }

        private void SetMeshRendererNotDirty(SkinnedMeshRenderer renderer)
        {
            string rendererName = GetRendererName(renderer);
            if (rendererName == null || !_skinnedMeshRenderers.ContainsKey(rendererName))
            {
                return;
            }

            foreach (var keyValuePair in _dirtySkinnedMeshRenderers[rendererName])
            {
                int blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(keyValuePair.Key);
                if (blendShapeIndex != -1)
                    renderer.SetBlendShapeWeight(blendShapeIndex, keyValuePair.Value.originalWeight);
            }

            _dirtySkinnedMeshRenderers[rendererName].Clear();
        }

        private BlendShapeData SetBlendShapeDirty(SkinnedMeshRenderer renderer, string blendName)
        {
            string rendererName = GetRendererName(renderer);
            BlendShapeData blendShapeData = null;
            if (rendererName != null)
            {
                Dictionary<string, BlendShapeData> skinnedMeshRenderer = _dirtySkinnedMeshRenderers[rendererName];
                if (!skinnedMeshRenderer.TryGetValue(blendName, out blendShapeData))
                {
                    blendShapeData = new BlendShapeData();
                    blendShapeData.originalWeight = renderer.GetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex(blendName));
                    skinnedMeshRenderer.Add(blendName, blendShapeData);
                }
            }

            return blendShapeData;
        }


        private void RefreshSkinnedMeshRendererList()
        {
            SkinnedMeshRenderer[] skinnedMeshRenderers = _parent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var prevDirty = new Dictionary<string, Dictionary<string, BlendShapeData>>(_dirtySkinnedMeshRenderers);
            var overlapNames = new Dictionary<string, int>();

            _rendererNames.Clear();
            _skinnedMeshRenderers.Clear();
            _dirtySkinnedMeshRenderers.Clear();

            foreach (SkinnedMeshRenderer skin in skinnedMeshRenderers)
            {
                if (skin != null && skin.sharedMesh != null && skin.sharedMesh.blendShapeCount > 0 && _parent._childObjects.All((child => !(skin).transform.IsChildOf(child.transform))))
                {
                    string name = skin.name;
                    if (overlapNames.ContainsKey(name))
                    {
                        var overlapCount = overlapNames[name];
                        overlapNames[name] = overlapCount + 1;
                        name += overlapCount.ToString();
                    }
                    else
                    {
                        overlapNames.Add(name, 1);
                    }

                    _rendererNames.Add(skin, name);

                    if (!prevDirty.ContainsKey(name))
                    {
                        prevDirty.Remove(name);
                    }

                    _dirtySkinnedMeshRenderers.Add(name, new Dictionary<string, BlendShapeData>());
                    _skinnedMeshRenderers.Add(name, skin);
                }
            }

            foreach (var currRenderer in prevDirty)
            {
                if (_skinnedMeshRenderers.TryGetValue(currRenderer.Key, out var skinnedMeshRenderer))
                {
                    foreach (var currBlendShape in currRenderer.Value)
                    {
                        int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(currBlendShape.Key);
                        if (blendShapeIndex != -1)
                        {
                            currBlendShape.Value.originalWeight = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
                            _dirtySkinnedMeshRenderers[currRenderer.Key].Add(currBlendShape.Key, currBlendShape.Value);
                        }
                    }
                }
            }

            if (_skinnedMeshTarget == null)
            {
                return;
            }

            foreach (var currRenderer in _skinnedMeshRenderers)
            {
                if (currRenderer.Value.sharedMesh.blendShapeCount > 0)
                {
                    _skinnedMeshTarget = currRenderer.Value;
                    break;
                }
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
                private SkinnedMeshRenderer _renderer;
                private readonly int _hashCode;

                public SkinnedMeshRenderer renderer
                {
                    get
                    {
                        if (_renderer == null)
                        {
                            editor._skinnedMeshRenderers.TryGetValue(rendererPath.Substring(rendererPath.LastIndexOf('/') + 1), out var skinnedMeshRenderer);
                            _renderer = skinnedMeshRenderer;
                        }

                        return _renderer;
                    }
                }

                public GroupParameter(BlendShapesEditor editor, SkinnedMeshRenderer renderer)
                {
                    this.editor = editor;
                    _renderer = renderer;
                    rendererPath = _renderer.transform.GetPathFrom(this.editor._parent.transform);

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
                    return $"editor: {editor}, rendererPath: {rendererPath}, renderer: {_renderer}";
                }
            }

            private class IndividualParameter : GroupParameter
            {
                public readonly int index;
                private readonly int _hashCode;

                public IndividualParameter(BlendShapesEditor editor, SkinnedMeshRenderer renderer, int index) : base(editor, renderer)
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
                            if (p.editor._isBusy == false)
                                p.editor.SetBlendShapeWeight(p.renderer, p.index, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: oci => oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null,
                        getValue: (oci, parameter) =>
                        {
                            IndividualParameter p = (IndividualParameter)parameter;
                            return p.renderer.GetBlendShapeWeight(p.index);
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
                            if (p.renderer == null || p.index >= p.renderer.sharedMesh.blendShapeCount)
                                return false;
                            return true;
                        },
                        getFinalName: (name, oci, parameter) =>
                        {
                            IndividualParameter p = (IndividualParameter)parameter;
                            if (_skinnedMeshAliases.TryGetValue(p.renderer.name, out var skinnedMeshName) == false)
                                skinnedMeshName = p.renderer.name;
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
                                SkinnedMeshRenderer renderer = p.renderer;
                                float[] left = (float[])leftValue;
                                float[] right = (float[])rightValue;
                                for (int i = 0; i < left.Length; i++)
                                    p.editor.SetBlendShapeWeight(renderer, i, Mathf.LerpUnclamped(left[i], right[i], factor));
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
                            if (_skinnedMeshAliases.TryGetValue(p.renderer.name, out var skinnedMeshName) == false)
                                skinnedMeshName = p.renderer.name;
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
                                SkinnedMeshRenderer renderer = p.renderer;
                                float[] left = (float[])leftValue;
                                float[] right = (float[])rightValue;
#if KOIKATSU
                                int targetLinkMaxCount = p.editor._target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount;
                                for (int i = 0; i < left.Length; i++)
                                {
                                    float newBlendShapeWeight = Mathf.LerpUnclamped(left[i], right[i], factor);
                                    p.editor.SetBlendShapeWeight(renderer, i, newBlendShapeWeight);
                                    if (i < targetLinkMaxCount)
                                    {
                                        SkinnedMeshRendererWrapper wrapper;
                                        if (p.editor._links.TryGetValue(renderer, out wrapper))
                                        {
                                            foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                            {
                                                if (i < link.renderer.sharedMesh.blendShapeCount)
                                                    p.editor.SetBlendShapeWeight(link.renderer, i, newBlendShapeWeight);
                                            }
                                        }
                                    }
                                }
#elif AISHOUJO || HONEYSELECT2
                                for (int index = 0; index < left.Length; ++index)
                                {
                                    float weight = Mathf.LerpUnclamped(left[index], right[index], factor);
                                    p.editor.SetBlendShapeWeight(renderer, index, weight);
                                }
#endif
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
                            if (_skinnedMeshAliases.TryGetValue(p.renderer.name, out var skinnedMeshName) == false)
                                skinnedMeshName = p.renderer.name;
                            return $"BS ({skinnedMeshName}, L)";
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
                SkinnedMeshRenderer renderer = p.renderer;
                float[] value = new float[renderer.sharedMesh.blendShapeCount];
                for (int i = 0; i < value.Length; ++i)
                    value[i] = renderer.GetBlendShapeWeight(i);
                return value;
            }
            private static object ReadGroupValueFromXml(object parameter, XmlNode node)
            {
                GroupParameter p = (GroupParameter)parameter;
                SkinnedMeshRenderer renderer = p.renderer;
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
                        if (tempValue.TryGetValue(i, out var v))
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
                SkinnedMeshRenderer renderer = p.renderer;
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
                return groupParameter.editor != null && (groupParameter.editor._isBusy || groupParameter.renderer != null);
            }
        }
#endregion
    }
}