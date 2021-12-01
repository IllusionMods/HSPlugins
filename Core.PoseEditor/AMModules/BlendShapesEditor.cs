using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using Studio;
using ToolBox;
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
        private class SkinnedMeshRendererData
        {
            public readonly Dictionary<int, BlendShapeData> dirtyBlendShapes = new Dictionary<int, BlendShapeData>();
            public string path;

            public SkinnedMeshRendererData()
            {
            }

            public SkinnedMeshRendererData(SkinnedMeshRendererData other)
            {
                foreach (KeyValuePair<int, BlendShapeData> kvp in other.dirtyBlendShapes)
                {
                    this.dirtyBlendShapes.Add(kvp.Key, new BlendShapeData() { weight = kvp.Value.weight, originalWeight = kvp.Value.originalWeight });
                }
            }
        }

        private class BlendShapeData
        {
            public float weight;
            public float originalWeight;
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

        private class SkinnedMeshRendererWrapper
        {
            public SkinnedMeshRenderer renderer;
            public List<SkinnedMeshRendererWrapper> links;
        }
        #endregion

        #region Private Variables
        private Vector2 _skinnedMeshRenderersScroll;
        private Vector2 _blendShapesScroll;
        private readonly List<SkinnedMeshRenderer> _skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
        private readonly Dictionary<string, SkinnedMeshRenderer> _skinnedMeshRenderersByPath = new Dictionary<string, SkinnedMeshRenderer>();
        private readonly Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererData> _dirtySkinnedMeshRenderers = new Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererData>();
        private readonly Dictionary<string, SkinnedMeshRendererData> _headlessDirtySkinnedMeshRenderers = new Dictionary<string, SkinnedMeshRendererData>();
        private int _headlessReconstructionTimeout = 0;
        private SkinnedMeshRenderer _headRenderer;
        private SkinnedMeshRenderer _skinnedMeshTarget;
        private bool _linkEyesComponents = true;
        private readonly Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> _links = new Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererWrapper>();
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
        public override bool shouldDisplay { get { return this._skinnedMeshRenderers.Any(r => r != null && r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0); } }
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
            _presetsPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), HSPE._name), "BlendShapesPresets");
        }

        public BlendShapesEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            this._parent.onLateUpdate += this.LateUpdate;
            this._parent.onDisable += this.OnDisable;
            this._target = target;
            MainWindow._self.ExecuteDelayed(() =>
            {
                this.RefreshSkinnedMeshRendererList();
                if (this._target.type == GenericOCITarget.Type.Character)
                    this.Init();
            });
        }

        private void LateUpdate()
        {
            if (this._headlessReconstructionTimeout >= 0)
            {
                this._headlessReconstructionTimeout--;
                foreach (KeyValuePair<string, SkinnedMeshRendererData> pair in new Dictionary<string, SkinnedMeshRendererData>(this._headlessDirtySkinnedMeshRenderers))
                {
                    Transform t = this._parent.transform.Find(pair.Key);
                    if (t != null)
                    {
                        SkinnedMeshRenderer renderer = t.GetComponent<SkinnedMeshRenderer>();
                        if (renderer != null)
                        {
                            this._dirtySkinnedMeshRenderers.Add(renderer, pair.Value);
                            this._headlessDirtySkinnedMeshRenderers.Remove(pair.Key);
                        }
                    }
                }
            }
            else if (this._headlessDirtySkinnedMeshRenderers.Count != 0)
                this._headlessDirtySkinnedMeshRenderers.Clear();
            if (this._target.type == GenericOCITarget.Type.Item)
                this.ApplyBlendShapeWeights();
        }

        public void OnGUI()
        {
            if (this._showSaveLoadWindow == false)
                return;
            Rect windowRect = Rect.MinMaxRect(MainWindow._self._advancedModeRect.xMin - 180, MainWindow._self._advancedModeRect.yMin, MainWindow._self._advancedModeRect.xMin, MainWindow._self._advancedModeRect.yMax);
            IMGUIExtensions.DrawBackground(windowRect);
            GUILayout.Window(MainWindow._uniqueId + 1, windowRect, this.SaveLoadWindow, "Presets");
        }

        private void OnDisable()
        {
            if (this._dirtySkinnedMeshRenderers.Count != 0)
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> kvp in this._dirtySkinnedMeshRenderers)
                    foreach (KeyValuePair<int, BlendShapeData> weight in kvp.Value.dirtyBlendShapes)
                        kvp.Key.SetBlendShapeWeight(weight.Key, weight.Value.originalWeight);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            this._parent.onLateUpdate -= this.LateUpdate;
            this._parent.onDisable -= this.OnDisable;
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
            this._isBusy = true;
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
            this._links.Clear();

            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(() =>
            {
                this.RefreshSkinnedMeshRendererList();
                this.Init();
                this._isBusy = false;
            });
        }

        public override void OnLoadClothesFile()
        {
            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }
#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        public override void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force)
#elif KOIKATSU
        public override void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force)
#endif
        {
            this._isBusy = true;
            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(() =>
            {
                this.RefreshSkinnedMeshRendererList();
                this._isBusy = false;
            });
        }
#endif

        public override void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }

        public override void GUILogic()
        {
            Color c = GUI.color;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(false));

            this._skinnedMeshRenderersScroll = GUILayout.BeginScrollView(this._skinnedMeshRenderersScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            foreach (SkinnedMeshRenderer r in this._skinnedMeshRenderers)
            {
                if (r == null || r.sharedMesh == null || r.sharedMesh.blendShapeCount == 0)
                    continue;
                if (this._dirtySkinnedMeshRenderers.ContainsKey(r))
                    GUI.color = Color.magenta;
                if (ReferenceEquals(r, this._skinnedMeshTarget))
                    GUI.color = Color.cyan;
                string dName;
                if (_skinnedMeshAliases.TryGetValue(r.name, out dName) == false)
                    dName = r.name;
                if (GUILayout.Button(dName + (this._dirtySkinnedMeshRenderers.ContainsKey(r) ? "*" : "")))
                {
                    this._skinnedMeshTarget = r;
                    this._lastEditedBlendShape = -1;
                }
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            GUI.color = Color.green;
            if (GUILayout.Button("Save/Load preset"))
            {
                this._showSaveLoadWindow = true;
                this.RefreshPresets();
            }
            GUI.color = c;

            if (this._target.type == GenericOCITarget.Type.Character)
                this._linkEyesComponents = GUILayout.Toggle(this._linkEyesComponents, "Link eyes components");
            if (GUILayout.Button("Force refresh list"))
                this.RefreshSkinnedMeshRendererList();
            GUI.color = Color.red;
            if (GUILayout.Button("Reset all"))
                this.ResetAll();
            GUI.color = c;
            GUILayout.EndVertical();


            if (this._skinnedMeshTarget != null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search", GUILayout.ExpandWidth(false));
                this._search = GUILayout.TextField(this._search, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    this._search = "";
                GUILayout.EndHorizontal();

                this._blendShapesScroll = GUILayout.BeginScrollView(this._blendShapesScroll, false, true, GUILayout.ExpandWidth(false));

                SkinnedMeshRendererData data = null;
                this._dirtySkinnedMeshRenderers.TryGetValue(this._skinnedMeshTarget, out data);
                bool zeroResult = true;
                for (int i = 0; i < this._skinnedMeshTarget.sharedMesh.blendShapeCount; ++i)
                {
                    if (this._headRenderer == this._skinnedMeshTarget)
                    {
                        Dictionary<int, string> separatorDict = this._target.isFemale ? _femaleSeparators : _maleSeparators;
                        string s;
                        if (separatorDict.TryGetValue(i, out s))
                            GUILayout.Label(s, GUI.skin.box);
                    }
                    string blendShapeName = this._skinnedMeshTarget.sharedMesh.GetBlendShapeName(i);
                    string blendShapeAlias;
                    if (_blendShapeAliases.TryGetValue(blendShapeName, out blendShapeAlias) == false)
                        blendShapeAlias = null;
                    if ((blendShapeAlias != null && blendShapeAlias.IndexOf(this._search, StringComparison.CurrentCultureIgnoreCase) != -1) ||
                        blendShapeName.IndexOf(this._search, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        zeroResult = false;
                        float blendShapeWeight;

                        BlendShapeData bsData;
                        if (data != null && data.dirtyBlendShapes.TryGetValue(i, out bsData))
                        {
                            blendShapeWeight = bsData.weight;
                            GUI.color = Color.magenta;
                        }
                        else
                            blendShapeWeight = this._skinnedMeshTarget.GetBlendShapeWeight(i);

                        GUILayout.BeginHorizontal();

                        GUILayout.BeginVertical(GUILayout.ExpandHeight(false));

                        GUILayout.BeginHorizontal();
                        if (this._renameIndex != i)
                        {
                            GUILayout.Label($"{i} {(blendShapeAlias == null ? blendShapeName : blendShapeAlias)}");
                            GUILayout.FlexibleSpace();
                        }
                        else
                        {
                            GUILayout.Label(i.ToString(), GUILayout.ExpandWidth(false));
                            this._renameString = GUILayout.TextField(this._renameString, GUILayout.ExpandWidth(true));
                        }
                        if (GUILayout.Button(this._renameIndex != i ? "Rename" : "Save", GUILayout.ExpandWidth(false)))
                        {
                            if (this._renameIndex != i)
                            {
                                this._renameIndex = i;
                                this._renameString = blendShapeAlias == null ? blendShapeName : blendShapeAlias;
                            }
                            else
                            {
                                this._renameIndex = -1;
                                this._renameString = this._renameString.Trim();
                                if (this._renameString == string.Empty || this._renameString == blendShapeName)
                                {
                                    if (_blendShapeAliases.ContainsKey(blendShapeName))
                                        _blendShapeAliases.Remove(blendShapeName);
                                }
                                else
                                {
                                    if (_blendShapeAliases.ContainsKey(blendShapeName) == false)
                                        _blendShapeAliases.Add(blendShapeName, this._renameString);
                                    else
                                        _blendShapeAliases[blendShapeName] = this._renameString;
                                }
                            }
                        }
                        GUILayout.Label(blendShapeWeight.ToString("000"), GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        float newBlendShapeWeight = GUILayout.HorizontalSlider(blendShapeWeight, 0f, 100f);
                        if (GUILayout.Button("-1", GUILayout.ExpandWidth(false)))
                            newBlendShapeWeight -= 1;
                        if (GUILayout.Button("+1", GUILayout.ExpandWidth(false)))
                            newBlendShapeWeight += 1;
                        newBlendShapeWeight = Mathf.Clamp(newBlendShapeWeight, 0, 100);
                        GUILayout.EndHorizontal();
                        if (Mathf.Approximately(newBlendShapeWeight, blendShapeWeight) == false)
                        {
                            this._lastEditedBlendShape = i;
                            this.SetBlendShapeWeight(this._skinnedMeshTarget, i, newBlendShapeWeight);
                            if (this._linkEyesComponents && i < (this._target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount))
                            {
                                SkinnedMeshRendererWrapper wrapper;
                                if (this._links.TryGetValue(this._skinnedMeshTarget, out wrapper))
                                {
                                    foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                    {
                                        if (i < link.renderer.sharedMesh.blendShapeCount)
                                            this.SetBlendShapeWeight(link.renderer, i, newBlendShapeWeight);
                                    }
                                }
                            }
                        }
                        GUILayout.EndVertical();

                        GUI.color = Color.red;

                        if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false), GUILayout.Height(50)) && data != null && data.dirtyBlendShapes.TryGetValue(i, out bsData))
                        {
                            this._skinnedMeshTarget.SetBlendShapeWeight(i, bsData.originalWeight);
                            data.dirtyBlendShapes.Remove(i);
                            if (data.dirtyBlendShapes.Count == 0)
                                this.SetMeshRendererNotDirty(this._skinnedMeshTarget);

                            if (this._linkEyesComponents && i < (this._target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount))
                            {
                                SkinnedMeshRendererWrapper wrapper;
                                if (this._links.TryGetValue(this._skinnedMeshTarget, out wrapper))
                                {
                                    foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                    {
                                        if (this._dirtySkinnedMeshRenderers.TryGetValue(link.renderer, out data) && data.dirtyBlendShapes.TryGetValue(i, out bsData))
                                        {
                                            link.renderer.SetBlendShapeWeight(i, bsData.originalWeight);
                                            data.dirtyBlendShapes.Remove(i);
                                            if (data.dirtyBlendShapes.Count == 0)
                                                this.SetMeshRendererNotDirty(link.renderer);
                                        }
                                    }
                                }
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
                    this.SetMeshRendererNotDirty(this._skinnedMeshTarget);
                    if (this._linkEyesComponents)
                    {
                        SkinnedMeshRendererWrapper wrapper;
                        if (this._links.TryGetValue(this._skinnedMeshTarget, out wrapper))
                            foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                this.SetMeshRendererNotDirty(link.renderer);
                    }
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
            BlendShapeData bsData = this.SetBlendShapeDirty(renderer, index);
            bsData.weight = weight;
            return bsData;
        }

        public void LoadFrom(BlendShapesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> kvp in other._dirtySkinnedMeshRenderers)
                {
                    Transform obj = this._parent.transform.Find(kvp.Key.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                    {
                        SkinnedMeshRenderer renderer = obj.GetComponent<SkinnedMeshRenderer>();
                        this._dirtySkinnedMeshRenderers.Add(renderer, new SkinnedMeshRendererData(kvp.Value));
                    }
                }
                this._blendShapesScroll = other._blendShapesScroll;
                this._skinnedMeshRenderersScroll = other._skinnedMeshRenderersScroll;
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this._dirtySkinnedMeshRenderers.Count != 0)
            {
                xmlWriter.WriteStartElement("skinnedMeshes");
                if (this._target.type == GenericOCITarget.Type.Character)
                {
                    xmlWriter.WriteAttributeString("eyesPtn", XmlConvert.ToString(this._target.ociChar.charInfo.GetEyesPtn()));
#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
                    xmlWriter.WriteAttributeString("eyesOpen", XmlConvert.ToString(this._target.ociChar.charInfo.GetEyesOpenMax()));
#elif PLAYHOME
                    xmlWriter.WriteAttributeString("eyesOpen", XmlConvert.ToString(this._target.ociChar.charInfo.fileStatus.eyesOpenMax));
#endif
                    xmlWriter.WriteAttributeString("mouthPtn", XmlConvert.ToString(this._target.ociChar.charInfo.GetMouthPtn()));
                    xmlWriter.WriteAttributeString("mouthOpen", XmlConvert.ToString(this._target.ociChar.oiCharInfo.mouthOpen));
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                    xmlWriter.WriteAttributeString("eyebrowsPtn", XmlConvert.ToString(this._target.ociChar.charInfo.GetEyebrowPtn()));
                    xmlWriter.WriteAttributeString("eyebrowsOpen", XmlConvert.ToString(this._target.ociChar.charInfo.GetEyebrowOpenMax()));
#endif
                    ++written;
                }
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> kvp in this._dirtySkinnedMeshRenderers)
                {
                    xmlWriter.WriteStartElement("skinnedMesh");
                    xmlWriter.WriteAttributeString("name", kvp.Key.transform.GetPathFrom(this._parent.transform));

                    foreach (KeyValuePair<int, BlendShapeData> weight in kvp.Value.dirtyBlendShapes)
                    {
                        xmlWriter.WriteStartElement("blendShape");
                        xmlWriter.WriteAttributeString("index", XmlConvert.ToString(weight.Key));
                        xmlWriter.WriteAttributeString("weight", XmlConvert.ToString(weight.Value.weight));
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
            this.ResetAll();
            this.RefreshSkinnedMeshRendererList();

            bool changed = false;
            XmlNode skinnedMeshesNode = xmlNode.FindChildNode("skinnedMeshes");
            Dictionary<XmlNode, SkinnedMeshRenderer> potentialChildrenNodes = new Dictionary<XmlNode, SkinnedMeshRenderer>();
            if (skinnedMeshesNode != null)
            {
                if (this._target.type == GenericOCITarget.Type.Character)
                {
                    if (skinnedMeshesNode.Attributes["eyesPtn"] != null)
                    {
#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
                        this._target.ociChar.charInfo.ChangeEyesPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyesPtn"].Value), false);
#elif PLAYHOME
                        this._target.ociChar.charInfo.ChangeEyesPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyesPtn"].Value));
#endif
                    }
                    if (skinnedMeshesNode.Attributes["eyesOpen"] != null)
                        this._target.ociChar.ChangeEyesOpen(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["eyesOpen"].Value));
                    if (skinnedMeshesNode.Attributes["mouthPtn"] != null)
                    {
#if HONEYSELECT || KOIKATSU || AISHOUJO || HONEYSELECT2
                        this._target.ociChar.charInfo.ChangeMouthPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["mouthPtn"].Value), false);
#elif PLAYHOME
                        this._target.ociChar.charInfo.ChangeMouthPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["mouthPtn"].Value));
#endif
                    }
                    if (skinnedMeshesNode.Attributes["mouthOpen"] != null)
                        this._target.ociChar.ChangeMouthOpen(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["mouthOpen"].Value));
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                    if (skinnedMeshesNode.Attributes["eyebrowsPtn"] != null)
                        this._target.ociChar.charInfo.ChangeEyebrowPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyebrowsPtn"].Value), false);
                    if (skinnedMeshesNode.Attributes["eyebrowsOpen"] != null)
                        this._target.ociChar.charInfo.ChangeEyebrowOpenMax(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["eyebrowsOpen"].Value));
#endif

                }
                foreach (XmlNode node in skinnedMeshesNode.ChildNodes)
                {
                    try
                    {
                        Transform t = this._parent.transform.Find(node.Attributes["name"].Value);
                        if (t == null)
                            continue;
                        SkinnedMeshRenderer renderer = t.GetComponent<SkinnedMeshRenderer>();
                        if (renderer == null)
                            continue;
                        if (this._skinnedMeshRenderers.Contains(renderer) == false)
                        {
                            potentialChildrenNodes.Add(node, renderer);
                            continue;
                        }
                        if (this.LoadSingleSkinnedMeshRenderer(node, renderer))
                            changed = true;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load blendshape for object " + this._parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
            if (potentialChildrenNodes.Count > 0)
            {
                foreach (KeyValuePair<XmlNode, SkinnedMeshRenderer> pair in potentialChildrenNodes)
                {
                    PoseController childController = pair.Value.GetComponentInParent<PoseController>();
                    if (childController != this._parent)
                    {
                        childController.enabled = true;
                        if (childController._blendShapesEditor._secondPassLoadingNodes.ContainsKey(pair.Key) == false)
                            childController._blendShapesEditor._secondPassLoadingNodes.Add(pair.Key, pair.Value);
                    }
                }
            }

            this._parent.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<XmlNode, SkinnedMeshRenderer> pair in this._secondPassLoadingNodes)
                {
                    try
                    {
                        if (this._skinnedMeshRenderers.Contains(pair.Value) == false)
                            continue;
                        this.LoadSingleSkinnedMeshRenderer(pair.Key, pair.Value);

                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load blendshape for object " + this._parent.name + " " + pair.Key.OuterXml + "\n" + e);
                    }
                }
                this._secondPassLoadingNodes.Clear();
            }, 2);
            return changed || this._secondPassLoadingNodes.Count > 0;
        }
        #endregion

        #region Private Methods
        private bool LoadSingleSkinnedMeshRenderer(XmlNode node, SkinnedMeshRenderer renderer)
        {
            bool loaded = false;
            SkinnedMeshRendererData data = new SkinnedMeshRendererData();
            foreach (XmlNode childNode in node.ChildNodes)
            {
                int index = XmlConvert.ToInt32(childNode.Attributes["index"].Value);
                if (index >= renderer.sharedMesh.blendShapeCount)
                    continue;
                loaded = true;
                BlendShapeData bsData = this.SetBlendShapeDirty(data, index);
                bsData.originalWeight = renderer.GetBlendShapeWeight(index);
                bsData.weight = XmlConvert.ToSingle(childNode.Attributes["weight"].Value);
            }
            data.path = renderer.transform.GetPathFrom(this._parent.transform);
            this._dirtySkinnedMeshRenderers.Add(renderer, data);
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

            this._presetsScroll = GUILayout.BeginScrollView(this._presetsScroll, false, true, GUILayout.ExpandHeight(true));
            foreach (string preset in _presets)
            {
                if (GUILayout.Button(preset))
                {
                    if (this._removePresetMode)
                        this.DeletePreset(preset + ".xml");
                    else
                        this.LoadPreset(preset + ".xml");
                }
            }
            GUILayout.EndScrollView();

            Color c = GUI.color;
            GUILayout.BeginVertical();
            if (_presets.Any(p => p.Equals(this._presetName, StringComparison.OrdinalIgnoreCase)))
                GUI.color = Color.red;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.ExpandWidth(false));
            this._presetName = GUILayout.TextField(this._presetName);
            GUILayout.EndHorizontal();
            GUI.color = c;

            GUI.enabled = this._presetName.Length != 0;
            if (GUILayout.Button("Save"))
            {
                this._presetName = this._presetName.Trim();
                this._presetName = string.Join("_", this._presetName.Split(Path.GetInvalidFileNameChars()));
                if (this._presetName.Length != 0)
                {
                    this.SavePreset(this._presetName + ".xml");
                    this.RefreshPresets();
                    this._removePresetMode = false;
                }

            }
            GUI.enabled = true;
            if (this._removePresetMode)
                GUI.color = Color.red;
            GUI.enabled = _presets.Length != 0;
            if (GUILayout.Button(this._removePresetMode ? "Click on preset" : "Delete"))
                this._removePresetMode = !this._removePresetMode;
            GUI.enabled = true;
            GUI.color = c;

            if (GUILayout.Button("Open folder"))
                System.Diagnostics.Process.Start(_presetsPath);
            if (GUILayout.Button("Close"))
                this._showSaveLoadWindow = false;

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
                this.SaveXml(writer);
                writer.WriteEndElement();
            }
        }

        private void LoadPreset(string name)
        {
            string path = Path.Combine(_presetsPath, name);
            if (File.Exists(path) == false)
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            this.LoadXml(doc.FirstChild);
        }

        private void DeletePreset(string name)
        {
            File.Delete(Path.GetFullPath(Path.Combine(_presetsPath, name)));
            this._removePresetMode = false;
            this.RefreshPresets();
        }

        private void Init()
        {
            this._headRenderer = null;
#if HONEYSELECT
            _instanceByFaceBlendShape.Add(this._target.ociChar.charBody.fbsCtrl, this);
#elif PLAYHOME
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.human, this);
#elif KOIKATSU
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.fbsCtrl, this);
#elif AISHOUJO || HONEYSELECT2
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.fbsCtrl, this);
#endif
            foreach (SkinnedMeshRenderer renderer in this._skinnedMeshRenderers)
            {
                switch (renderer.name)
                {
#if HONEYSELECT || KOIKATSU || PLAYHOME
                    case "cf_O_head":
                    case "cf_O_face":
#elif AISHOUJO || HONEYSELECT2
                    case "o_head":
#endif
                        this._headRenderer = renderer;
                        break;
                }

                switch (renderer.name)
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
                        SkinnedMeshRendererWrapper wrapper = new SkinnedMeshRendererWrapper
                        {
                            renderer = renderer,
                            links = new List<SkinnedMeshRendererWrapper>()
                        };
                        this._links.Add(renderer, wrapper);

                        break;
                }
            }

            foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> pair in this._links)
            {
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> pair2 in this._links)
                {
                    if (pair.Key != pair2.Key)
                        pair.Value.links.Add(pair2.Value);
                }
            }
        }

        private void ResetAll()
        {
            foreach (SkinnedMeshRenderer renderer in this._skinnedMeshRenderers)
                this.SetMeshRendererNotDirty(renderer);
        }

        private void FaceBlendShapeOnPostLateUpdate()
        {
            if (this._parent.enabled)
                this.ApplyBlendShapeWeights();
        }

        private void ApplyBlendShapeWeights()
        {
            if (this._dirtySkinnedMeshRenderers.Count != 0)
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> kvp in this._dirtySkinnedMeshRenderers)
                    foreach (KeyValuePair<int, BlendShapeData> weight in kvp.Value.dirtyBlendShapes)
                        kvp.Key.SetBlendShapeWeight(weight.Key, weight.Value.weight);
        }



        private void SetMeshRendererNotDirty(SkinnedMeshRenderer renderer)
        {
            if (this._dirtySkinnedMeshRenderers.ContainsKey(renderer))
            {
                SkinnedMeshRendererData data = this._dirtySkinnedMeshRenderers[renderer];
                foreach (KeyValuePair<int, BlendShapeData> kvp in data.dirtyBlendShapes)
                    renderer.SetBlendShapeWeight(kvp.Key, kvp.Value.originalWeight);
                this._dirtySkinnedMeshRenderers.Remove(renderer);
            }
        }

        private SkinnedMeshRendererData SetMeshRendererDirty(SkinnedMeshRenderer renderer)
        {
            SkinnedMeshRendererData data;
            if (this._dirtySkinnedMeshRenderers.TryGetValue(renderer, out data) == false)
            {
                data = new SkinnedMeshRendererData();
                data.path = renderer.transform.GetPathFrom(this._parent.transform);
                this._dirtySkinnedMeshRenderers.Add(renderer, data);
            }
            return data;
        }

        private BlendShapeData SetBlendShapeDirty(SkinnedMeshRenderer renderer, int index)
        {
            SkinnedMeshRendererData data = this.SetMeshRendererDirty(renderer);
            BlendShapeData bsData;
            if (data.dirtyBlendShapes.TryGetValue(index, out bsData) == false)
            {
                bsData = new BlendShapeData();
                bsData.originalWeight = renderer.GetBlendShapeWeight(index);
                data.dirtyBlendShapes.Add(index, bsData);
            }
            return bsData;
        }

        private BlendShapeData SetBlendShapeDirty(SkinnedMeshRendererData data, int index)
        {
            BlendShapeData bsData;
            if (data.dirtyBlendShapes.TryGetValue(index, out bsData) == false)
            {
                bsData = new BlendShapeData();
                data.dirtyBlendShapes.Add(index, bsData);
            }
            return bsData;
        }

        private void RefreshSkinnedMeshRendererList()
        {
            SkinnedMeshRenderer[] skinnedMeshRenderers = this._parent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            List<SkinnedMeshRenderer> toDelete = null;
            foreach (SkinnedMeshRenderer r in this._skinnedMeshRenderers)
                if (skinnedMeshRenderers.Contains(r) == false)
                {
                    if (toDelete == null)
                        toDelete = new List<SkinnedMeshRenderer>();
                    toDelete.Add(r);
                }
            if (toDelete != null)
            {
                foreach (SkinnedMeshRenderer r in toDelete)
                {
                    SkinnedMeshRendererData data;
                    if (this._dirtySkinnedMeshRenderers.TryGetValue(r, out data))
                    {
                        this._headlessDirtySkinnedMeshRenderers.Add(data.path, data);
                        this._headlessReconstructionTimeout = 5;
                        this._dirtySkinnedMeshRenderers.Remove(r);
                    }
                    this._skinnedMeshRenderers.Remove(r);
                }
            }
            List<SkinnedMeshRenderer> toAdd = null;
            foreach (SkinnedMeshRenderer r in skinnedMeshRenderers)
                if (this._skinnedMeshRenderers.Contains(r) == false && this._parent._childObjects.All(child => r.transform.IsChildOf(child.transform) == false))
                {
                    if (toAdd == null)
                        toAdd = new List<SkinnedMeshRenderer>();
                    toAdd.Add(r);
                }
            if (toAdd != null)
            {
                foreach (SkinnedMeshRenderer r in toAdd)
                    this._skinnedMeshRenderers.Add(r);
            }
            this._skinnedMeshRenderersByPath.Clear();
            foreach (SkinnedMeshRenderer renderer in this._skinnedMeshRenderers)
            {
                string path = renderer.transform.GetPathFrom(this._parent.transform);
                if (this._skinnedMeshRenderersByPath.ContainsKey(path) == false)
                    this._skinnedMeshRenderersByPath.Add(path, renderer);
            }
            if (this._skinnedMeshRenderers.Count != 0 && this._skinnedMeshTarget != null)
                this._skinnedMeshTarget = this._skinnedMeshRenderers.FirstOrDefault(s => s.sharedMesh.blendShapeCount > 0);
        }
        #endregion

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            private class GroupParameter
            {
                public readonly BlendShapesEditor editor;
                public readonly string rendererPath;

                public SkinnedMeshRenderer renderer
                {
                    get
                    {
                        if (this._renderer == null)
                            this.editor._skinnedMeshRenderersByPath.TryGetValue(this.rendererPath, out this._renderer);
                        return this._renderer;
                    }
                }
                private SkinnedMeshRenderer _renderer;
                private readonly int _hashCode;

                public GroupParameter(BlendShapesEditor editor, SkinnedMeshRenderer renderer)
                {
                    this.editor = editor;
                    this._renderer = renderer;
                    this.rendererPath = this._renderer.transform.GetPathFrom(this.editor._parent.transform);

                    unchecked
                    {
                        int hash = 17;
                        hash = hash * 31 + (this.editor != null ? this.editor.GetHashCode() : 0);
                        this._hashCode = hash * 31 + (this.rendererPath != null ? this.rendererPath.GetHashCode() : 0);
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
                        this._hashCode = hash * 31 + (this.rendererPath != null ? this.rendererPath.GetHashCode() : 0);
                    }
                }

                public override int GetHashCode()
                {
                    return this._hashCode;
                }

                public override string ToString()
                {
                    return $"editor: {this.editor}, rendererPath: {this.rendererPath}, renderer: {this._renderer}";
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
                        this._hashCode = base.GetHashCode() * 31 + this.index.GetHashCode();
                    }
                }

                public IndividualParameter(BlendShapesEditor editor, string rendererPath, int index) : base(editor, rendererPath)
                {
                    this.index = index;

                    unchecked
                    {
                        this._hashCode = base.GetHashCode() * 31 + this.index.GetHashCode();
                    }
                }

                public override int GetHashCode()
                {
                    return this._hashCode;
                }

                public override string ToString()
                {
                    return $"{base.ToString()}, index: {this.index}";
                }
            }

            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
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
                            string skinnedMeshName;
                            if (_skinnedMeshAliases.TryGetValue(p.renderer.name, out skinnedMeshName) == false)
                                skinnedMeshName = p.renderer.name;
                            return $"BS ({skinnedMeshName} {p.index})";
                        });

                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
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
                            string skinnedMeshName;
                            if (_skinnedMeshAliases.TryGetValue(p.renderer.name, out skinnedMeshName) == false)
                                skinnedMeshName = p.renderer.name;
                            return $"BS ({skinnedMeshName})";
                        });
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
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
                            string skinnedMeshName;
                            if (_skinnedMeshAliases.TryGetValue(p.renderer.name, out skinnedMeshName) == false)
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
                    return false;
                GroupParameter p = (GroupParameter)parameter;
                if (p.editor == null)
                    return false;
                if (p.editor._isBusy)
                    return true;
                SkinnedMeshRenderer renderer = p.renderer;
                if (renderer == null || renderer.sharedMesh.blendShapeCount != ((float[])leftValue).Length || renderer.sharedMesh.blendShapeCount != ((float[])rightValue).Length)
                    return false;
                return true;
            }

        }
        #endregion
    }
}