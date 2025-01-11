using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using Vectrosity;
#if IPA
using IllusionPlugin;
using Harmony;
#elif BEPINEX
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
#endif
#if KOIKATSU
using KKAPI.Studio.SaveLoad;
using ExtensibleSaveFormat;
using MessagePack;
using Expression = ExpressionBone;
#elif AISHOUJO || HONEYSELECT2
using AIChara;
using ExtensibleSaveFormat;
using UnityEngine.SceneManagement;
using CharaUtils;
#endif

namespace NodesConstraints
{
#if BEPINEX
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
#if KOIKATSU
    [BepInProcess("CharaStudio")]
#elif AISHOUJO || HONEYSELECT2
    [BepInProcess("StudioNEOV2")]
#endif
#endif
    public class NodesConstraints : GenericPlugin
#if IPA
    , IEnhancedPlugin
#endif
    {
        public const string Name = "NodesConstraints";
        public const string GUID = "com.joan6694.illusionplugins.nodesconstraints";
        public const string Version = "1.4";
#if KOIKATSU || AISHOUJO || HONEYSELECT2
        private const string _extSaveKey = "nodesConstraints";
        private const int _saveVersion = 0;
#endif
#if HONEYSELECT || KOIKATSU
        private const float _circleRadius = 0.01f;
#elif AISHOUJO || HONEYSELECT2
        private const float _circleRadius = 0.1f;
#endif

        private const int MainCameraParenObjectIndex = -2;
        private static NodesConstraints _self;

#if IPA
        public override string Name { get { return _name; } }
        public override string Version
        {
            get
            {
                return _versionNum
#if BETA
                       + "b"
#endif
                        ;
            }
        }
        public override string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }
#endif

        #region Private Types
        private enum SimpleListShowNodeType
        {
            All,
            IK,
            FK
        }

        private class Constraint
        {
            public bool enabled = true;

            public TransformLock positionLocks = new TransformLock(true, true, true);
            public TransformLock rotationLocks = new TransformLock(true, true, true);
            public TransformLock scaleLocks = new TransformLock(true, true, true);

            public GuideObject parent;
            public Transform parentTransform;
            public GuideObject child;
            public Transform childTransform;

            public bool position = true;
            public bool rotation = true;
            public bool lookAt = false;
            public bool scale = true;

            public bool mirrorPosition = false;
            public bool mirrorRotation = false;
            public bool mirrorScale = false;

            public float positionChangeFactor = 1;
            public float rotationChangeFactor = 1;
            public float scaleChangeFactor = 1;

            public float positionDamp = 0;
            public float rotationDamp = 0;
            public float scaleDamp = 0;

            public bool resetOriginalPosition = true;
            public bool resetOriginalRotation = true;
            public bool resetOriginalScale = true;

            public Vector3 positionOffset = Vector3.zero;
            public Quaternion rotationOffset = Quaternion.identity;
            public Vector3 scaleOffset = Vector3.one;

            public Vector3 originalChildPosition;
            public Quaternion originalChildRotation;
            public Vector3 originalChildScale;

            public string alias = "";
            public bool fixDynamicBone;
            public int? uniqueLoadId;
            public bool destroyed = false;
            private VectorLine _debugLine;

            public Vector3 originalParentPosition;
            public Quaternion originalParentRotation;
            public Vector3 originalParentScale;

            public Constraint()
            {
                _debugLine = VectorLine.SetLine(Color.white, Vector3.zero, Vector3.one);
                _debugLine.lineWidth = 3f;
                _debugLine.active = false;
            }

            public Constraint(Constraint other) : this()
            {
                enabled = other.enabled;
                parent = other.parent;
                parentTransform = other.parentTransform;
                child = other.child;
                childTransform = other.childTransform;
                position = other.position;
                rotation = other.rotation;
                scale = other.scale;
                positionOffset = other.positionOffset;
                rotationOffset = other.rotationOffset;
                scaleOffset = other.scaleOffset;
                originalChildPosition = other.originalChildPosition;
                originalChildRotation = other.originalChildRotation;
                originalChildScale = other.originalChildScale;
                alias = other.alias;
                fixDynamicBone = other.fixDynamicBone;
                originalParentPosition = other.parentTransform.position;
                originalParentRotation = other.parentTransform.rotation;
                originalParentScale = other.parentTransform.lossyScale;
            }

            public void SetActiveDebugLines(bool active)
            {
                _debugLine.active = active;
            }

            public void UpdateDebugLines()
            {
                _debugLine.points3[0] = parentTransform.position;
                _debugLine.points3[1] = childTransform.position;
                _debugLine.SetColor((position && rotation ? Color.magenta : (position ? Color.cyan : Color.green)));
                _debugLine.Draw();
            }

            public float GetInterpolationFactor(float damp)
            {
                return 1 - Mathf.Exp(-damp * _deltaTime);
            }

            public Vector3 GetPositionMovement()
            {
                return (parentTransform.position - originalParentPosition);
            }

            public void UpdatePosition()
            {
                Vector3 targetPos;
                if (mirrorPosition)
                    targetPos = -GetPositionMovement() * (positionChangeFactor + 1);
                else
                    targetPos = GetPositionMovement() * (positionChangeFactor - 1);
                targetPos += positionOffset;

                targetPos = parentTransform.TransformPoint(targetPos);

                if (!positionLocks.x)
                    targetPos.x = childTransform.position.x;
                if (!positionLocks.y)
                    targetPos.y = childTransform.position.y;
                if (!positionLocks.z)
                    targetPos.z = childTransform.position.z;

                if (positionDamp > 0)
                    targetPos = Vector3.Lerp(childTransform.position, targetPos, GetInterpolationFactor(positionDamp));
                childTransform.position = targetPos;

                if (child != null)
                    child.changeAmount.pos = child.transformTarget.localPosition;
            }

            public Quaternion GetRotationChange()
            {
                return Quaternion.Inverse(originalParentRotation) * parentTransform.rotation;
            }

            public void UpdateRotation()
            {
                Quaternion targetRot;
                if (lookAt)
                {
                    if (mirrorRotation)
                    {
                        var lookAt = Quaternion.LookRotation(parentTransform.position - childTransform.position);
                        targetRot = new Quaternion(lookAt.x * -1f, lookAt.y * -1f, lookAt.z, lookAt.w);
                    }
                    else
                        targetRot = Quaternion.LookRotation(parentTransform.position - childTransform.position);
                }
                else if (mirrorRotation)
                    targetRot = originalParentRotation * Quaternion.Inverse(GetRotationChange());
                else
                    targetRot = originalParentRotation * GetRotationChange();
                targetRot *= rotationOffset;
                targetRot = Quaternion.SlerpUnclamped(Quaternion.identity, targetRot, rotationChangeFactor);

                targetRot = Quaternion.Euler(
                        rotationLocks.x ? targetRot.eulerAngles.x : childTransform.rotation.eulerAngles.x,
                        rotationLocks.y ? targetRot.eulerAngles.y : childTransform.rotation.eulerAngles.y,
                        rotationLocks.z ? targetRot.eulerAngles.z : childTransform.rotation.eulerAngles.z
                    );

                if (rotationDamp > 0)
                    targetRot = Quaternion.Slerp(childTransform.rotation, targetRot, GetInterpolationFactor(rotationDamp));
                childTransform.rotation = targetRot;

                if (child != null)
                    child.changeAmount.rot = child.transformTarget.localEulerAngles;
            }

            public void UpdateScale()
            {
                Vector3 targetScale = new Vector3(
                    (originalParentScale.x * scaleOffset.x / childTransform.parent.lossyScale.x) * Mathf.Pow(parentTransform.lossyScale.x / originalParentScale.x, mirrorScale ? -scaleChangeFactor : scaleChangeFactor),
                    (originalParentScale.y * scaleOffset.y / childTransform.parent.lossyScale.y) * Mathf.Pow(parentTransform.lossyScale.y / originalParentScale.y, mirrorScale ? -scaleChangeFactor : scaleChangeFactor),
                    (originalParentScale.z * scaleOffset.z / childTransform.parent.lossyScale.z) * Mathf.Pow(parentTransform.lossyScale.z / originalParentScale.z, mirrorScale ? -scaleChangeFactor : scaleChangeFactor)
                );

                if (!scaleLocks.x)
                    targetScale.x = childTransform.localScale.x;
                if (!scaleLocks.y)
                    targetScale.y = childTransform.localScale.y;
                if (!scaleLocks.z)
                    targetScale.z = childTransform.localScale.z;

                if (scaleDamp > 0)
                    targetScale = Vector3.Lerp(childTransform.localScale, targetScale, GetInterpolationFactor(scaleDamp));
                childTransform.localScale = targetScale;

                if (child != null)
                    child.changeAmount.scale = child.transformTarget.localScale;
            }

            public void Destroy()
            {
                VectorLine.Destroy(ref _debugLine);
                destroyed = true;
            }

            public void CopyTo(Constraint constraint, bool onlyAttributes=true)
            {
                constraint.parentTransform = parentTransform;
                constraint.childTransform = childTransform;

                constraint.position = position;
                constraint.rotation = rotation;
                constraint.lookAt = lookAt;
                constraint.scale = scale;
                constraint.positionOffset = positionOffset;
                constraint.rotationOffset = rotationOffset;
                constraint.scaleOffset = scaleOffset;
                constraint.positionChangeFactor = positionChangeFactor;
                constraint.rotationChangeFactor = rotationChangeFactor;
                constraint.scaleChangeFactor = scaleChangeFactor;
                constraint.mirrorPosition = mirrorPosition;
                constraint.mirrorRotation = mirrorRotation;
                constraint.mirrorScale = mirrorScale;
                constraint.positionDamp = positionDamp;
                constraint.rotationDamp = rotationDamp;
                constraint.scaleDamp = scaleDamp;
                constraint.alias = alias;
                constraint.fixDynamicBone = fixDynamicBone;
                constraint.resetOriginalPosition = resetOriginalPosition;
                constraint.resetOriginalRotation = resetOriginalRotation;
                constraint.resetOriginalScale = resetOriginalScale;
                constraint.positionLocks = positionLocks.Copy();
                constraint.rotationLocks = rotationLocks.Copy();
                constraint.scaleLocks = scaleLocks.Copy();

                if (onlyAttributes)
                    return;

                if (constraint.position && position == false && resetOriginalPosition)
                {
                    constraint.childTransform.localPosition = constraint.originalChildPosition;
                    if (constraint.child != null)
                        constraint.child.changeAmount.pos = constraint.originalChildPosition;
                }
                if (constraint.rotation && rotation == false && resetOriginalRotation)
                {
                    constraint.childTransform.localRotation = constraint.originalChildRotation;
                    if (constraint.child != null)
                        constraint.child.changeAmount.rot = constraint.originalChildRotation.eulerAngles;
                }

                if (constraint.scale && scale == false && resetOriginalScale)
                {
                    constraint.childTransform.localScale = constraint.originalChildScale;
                    if (constraint.child != null)
                        constraint.child.changeAmount.scale = constraint.originalChildScale;
                }

                if (_allGuideObjects.TryGetValue(constraint.parentTransform, out constraint.parent) == false)
                    constraint.parent = null;
                if (_allGuideObjects.TryGetValue(constraint.childTransform, out constraint.child) == false)
                    constraint.child = null;

                constraint.originalChildPosition = constraint.childTransform.localPosition;
                constraint.originalChildRotation = constraint.childTransform.localRotation;
                constraint.originalChildScale = constraint.childTransform.localScale;
            }
        }

        public class TransformLock
        {
            public bool x;
            public bool y;
            public bool z;

            public TransformLock(bool x, bool y, bool z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public TransformLock Copy()
            {
                return new TransformLock(x, y, z);
            }
        }

#if KOIKATSU
        [Serializable]
        [MessagePackObject]
        private class AnimationControllerInfo
        {
            [Key("CharDicKey")]
            public int CharDicKey { get; set; }
            [Key("ItemDicKey")]
            public int ItemDicKey { get; set; }
            [Key("IKPart")]
            public string IKPart { get; set; }
            [Key("Version")]
            public string Version { get; set; }
            public static AnimationControllerInfo Unserialize(byte[] data) => MessagePackSerializer.Deserialize<AnimationControllerInfo>(data);
            public byte[] Serialize() => MessagePackSerializer.Serialize(this);
        }
#endif
        #endregion

        #region Private Variables
        private readonly HashSet<GameObject> _openedBones = new HashSet<GameObject>();

        private bool _studioLoaded;
        private bool _showUI = false;
        private RectTransform _imguiBackground;
        private const int _uniqueId = ('N' << 24) | ('O' << 16) | ('D' << 8) | 'E';
        private Rect _windowRect = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 300, 400, 600);
        private readonly Constraint _displayedConstraint = new Constraint();
        private Constraint _selectedConstraint;
        private readonly List<Constraint> _constraints = new List<Constraint>();
        private Vector2 _scroll;
        private HashSet<GuideObject> _selectedGuideObjects;
        private bool _initUI;
        private GUIStyle _wrapButton;
        private Transform _selectedBone;
        private Vector2 _advancedModeScroll;
        private Vector2 _simpleModeScroll;
        private HashSet<TreeNodeObject> _selectedWorkspaceObjects;
        private static Dictionary<Transform, GuideObject> _allGuideObjects;
        private VectorLine _parentCircle;
        private VectorLine _childCircle;
        private VectorLine _selectedCircle;
        private Action _onPreCullAction;
        private CameraEventsDispatcher _dispatcher;
        private bool _advancedList = false;
        private string _constraintsSearch = "";
        private string _bonesSearch = "";
        private GuideObject _selectedWorkspaceObject;
        private GuideObject _lastSelectedWorkspaceObject;
#if KOIKATSU
        private bool _kkAnimationControllerInstalled = true;
#endif
        private SimpleListShowNodeType _selectedShowNodeType = SimpleListShowNodeType.All;
        private string[] _simpleListShowNodeTypeNames;
        private int _totalActiveExpressions = 0;
        private int _currentExpressionIndex = 0;
        private readonly HashSet<Expression> _allExpressions = new HashSet<Expression>();

        private string _positionXStr = "0.0000";
        private string _positionYStr = "0.0000";
        private string _positionZStr = "0.0000";
        private string _positionFactorStr = "1.0000";
        private string _positionDampStr = "0.000";

        private string _rotationXStr = "0.000";
        private string _rotationYStr = "0.000";
        private string _rotationZStr = "0.000";
        private string _rotationFactorStr = "1.0000";
        private string _rotationDampStr = "0.000";

        private string _scaleXStr = "1.0000";
        private string _scaleYStr = "1.0000";
        private string _scaleZStr = "1.0000";
        private string _scaleFactorStr = "1.0000";
        private string _scaleDampStr = "0.000";

        private bool showExtraMov = false;
        private bool showExtraRot = false;
        private bool showExtraScale = false;

        private bool _debugMode;
        private Vector3 _debugLocalPosition;
        private Vector3 _debugWorldPosition;
        private Quaternion _debugLocalRotation;
        private Quaternion _debugWorldRotation;
        private Vector3 _debugLocalScale;
        private Vector3 _debugWorldScale;
        private bool _hasTimeline = false;

        private static float _deltaTime;
        #endregion

        internal static ConfigEntry<KeyboardShortcut> ConfigMainWindowShortcut { get; private set; }
        internal static ConfigEntry<int> ConstraintsAreaHeight { get; private set; }
        internal static ConfigEntry<int> NodesAreaHeight { get; private set; }

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();

            ConfigMainWindowShortcut = Config.Bind("Config", "Open NodeConstraints UI", new KeyboardShortcut(KeyCode.I, KeyCode.LeftControl));
            ConstraintsAreaHeight = Config.Bind("Interface", "Constraints Area Height", 150, new ConfigDescription("", new AcceptableValueRange<int>(40, 300)));
            NodesAreaHeight = Config.Bind("Interface", "Nodes Area Height", 200, new ConfigDescription("", new AcceptableValueRange<int>(40, 300)));

            _self = this;
#if HONEYSELECT
            HSExtSave.HSExtSave.RegisterHandler("nodesConstraints", null, null, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);
            float width = ModPrefs.GetFloat("NodesConstraints", "windowWidth", 400, true);
            if (width < 400)
                width = 400;
            this._windowRect = new Rect((Screen.width - width) / 2f, Screen.height / 2f - 300, width, 600);
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            ExtendedSave.SceneBeingLoaded += OnSceneLoad;
            ExtendedSave.SceneBeingImported += OnSceneImport;
            ExtendedSave.SceneBeingSaved += OnSceneSave;
#endif
            var harmonyInstance = HarmonyExtensions.CreateInstance(GUID);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            _simpleListShowNodeTypeNames = Enum.GetNames(typeof(SimpleListShowNodeType));
            this.ExecuteDelayed(() =>
            {
                if (TimelineCompatibility.Init())
                {
                    PopulateTimeline();
                    _hasTimeline = true;
                }
            }, 10);
        }

#if AISHOUJO || HONEYSELECT2
        protected override void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            base.LevelLoaded(scene, mode);
            if (mode == LoadSceneMode.Single && scene.name.Equals("Studio"))
                this.Init();
        }
#else
        protected override void LevelLoaded(int level)
        {
            base.LevelLoaded(level);
#if HONEYSELECT
            if (level == 3)
#elif SUNSHINE
            if (level == 2)
#elif KOIKATSU
            if (level == 1)
#endif
                Init();
        }
#endif


        private void Init()
        {
            _studioLoaded = true;
            _selectedWorkspaceObjects = (HashSet<TreeNodeObject>)Studio.Studio.Instance.treeNodeCtrl.GetPrivate("hashSelectNode");
            _selectedGuideObjects = (HashSet<GuideObject>)GuideObjectManager.Instance.GetPrivate("hashSelectObject");
            _allGuideObjects = (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject");
            _dispatcher = Camera.main.gameObject.AddComponent<CameraEventsDispatcher>();
            VectorLine.SetCamera3D(Camera.main);
            if (Camera.main.GetComponent<Expression>() == null)
                Camera.main.gameObject.AddComponent<Expression>();
#if KOIKATSU
            _kkAnimationControllerInstalled = BepInEx.Bootstrap.Chainloader.Plugins
                                                          .Select(MetadataHelper.GetMetadata)
                                                          .FirstOrDefault(x => x.GUID == "com.deathweasel.bepinex.animationcontroller") != null;
#endif
            _imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI();
            this.ExecuteDelayed(() =>
            {
                _parentCircle = VectorLine.SetLine(Color.green, new Vector3[16]);
                _childCircle = VectorLine.SetLine(Color.red, new Vector3[16]);
                _selectedCircle = VectorLine.SetLine(Color.cyan, new Vector3[16]);
                _parentCircle.lineWidth = 4f;
                _childCircle.lineWidth = 4f;
                _childCircle.lineWidth = 4f;
                _parentCircle.MakeCircle(Vector3.zero, Vector3.up, _circleRadius);
                _childCircle.MakeCircle(Vector3.zero, Vector3.up, _circleRadius);
                _selectedCircle.MakeCircle(Vector3.zero, Vector3.up, _circleRadius);
            }, 2);
        }

        protected override void Update()
        {
            //Contraints without GuideObject (e.g. bones) are updated through a postfix
            //This postfix does not have access to Time.deltaTime because it's not an update method
            //This postfix is however postfixed to an update method, so by saving the value here, it can be used there too
            _deltaTime = Time.deltaTime;
            if (_studioLoaded == false)
                return;
            _totalActiveExpressions = _allExpressions.Count(e => e.enabled && e.gameObject.activeInHierarchy);
            _currentExpressionIndex = 0;
            if (ConfigMainWindowShortcut.Value.IsDown())
            {
                _showUI = !_showUI;
                if (_selectedConstraint != null)
                    _selectedConstraint.SetActiveDebugLines(_showUI);
            }
            if (_onPreCullAction != null)
            {
                _onPreCullAction();
                _onPreCullAction = null;
            }
            _selectedWorkspaceObject = null;
            TreeNodeObject treeNode = _selectedWorkspaceObjects?.FirstOrDefault();
            if (treeNode != null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNode, out info))
                    _selectedWorkspaceObject = info.guideObject;
            }
            if (_selectedWorkspaceObject != _lastSelectedWorkspaceObject && _selectedWorkspaceObject != null)
                _selectedBone = _selectedWorkspaceObject.transformTarget;
            _lastSelectedWorkspaceObject = _selectedWorkspaceObject;
            if (_hasTimeline == false)
                ApplyNodesConstraints();

            if (_showUI)
            {
                _imguiBackground.gameObject.SetActive(true);
                IMGUIExtensions.FitRectTransformToRect(_imguiBackground, _windowRect);
            }
            else if (_imguiBackground != null)
                _imguiBackground.gameObject.SetActive(false);

            if (_showUI)
                _windowRect.height = 200f + ConstraintsAreaHeight.Value + NodesAreaHeight.Value;
        }

        [HarmonyPatch]
        private static class Timeline_Update_Postfix
        {
            private static bool Prepare()
            {
                return Type.GetType("Timeline.Timeline,Timeline") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("Timeline.Timeline,Timeline").GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            private static void Postfix()
            {
                if (_self._studioLoaded == false)
                    return;
                _self.ApplyNodesConstraints();
            }
        }

        protected override void OnGUI()
        {
            var skin = GUI.skin;
            GUI.skin = KKAPI.Utilities.IMGUIUtils.SolidBackgroundGuiSkin;

            if (_showUI == false)
                return;
            if (_initUI == false)
            {
                _wrapButton = new GUIStyle(GUI.skin.button);
                _wrapButton.wordWrap = true;
                _wrapButton.alignment = TextAnchor.MiddleLeft;
                _initUI = true;
            }
            _windowRect = GUILayout.Window(_uniqueId, _windowRect, WindowFunction, "Nodes Constraints " + Version
#if BETA
                                                                                                                       + "b"
#endif
            );
            GUI.skin = skin;
        }
        #endregion

        #region Private Methods
#if AISHOUJO || HONEYSELECT2
        [HarmonyPatch(typeof(ChaControl), "Load", typeof(bool))]
        private static class ChaControl_Load_Patches
        {
            private static void Postfix(ChaControl __instance)
            {
                //Illusion I fucking hate you why are you like this
                if (__instance.fullBodyIK != null && 
                    __instance.fullBodyIK.solver.leftShoulderEffector != null && 
                    __instance.fullBodyIK.solver.leftShoulderEffector.target != null && 
                    __instance.fullBodyIK.solver.leftShoulderEffector.target.name == "f_t_shoulder_R")
                    __instance.fullBodyIK.solver.leftShoulderEffector.target = __instance.fullBodyIK.solver.leftShoulderEffector.target.parent.Find("f_t_shoulder_L");
            }
        }
#endif

#if HONEYSELECT
        [HarmonyPatch(typeof(Expression), "Start")]
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
        [HarmonyPatch(typeof(Expression), "Initialize")]
#endif
        private static class Expression_Start_Patches
        {
            private static void Prefix(Expression __instance)
            {
                _self._allExpressions.Add(__instance);
            }
        }

        [HarmonyPatch(typeof(Expression), "OnDestroy")]
        private static class Expression_OnDestroy_Patches
        {
            private static void Prefix(Expression __instance)
            {
                _self._allExpressions.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(Expression), "LateUpdate"), HarmonyAfter("com.joan6694.illusionplugins.timeline")]
        private static class Expression_LateUpdate_Patches
        {
            private static void Postfix()
            {
                _self._currentExpressionIndex++;
                if (_self._currentExpressionIndex == _self._totalActiveExpressions) //Dirty fucking hack that I hate to make sure this runs after *everything*
                    _self.ApplyConstraints();
            }
        }

        [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
        private class Studio_Duplicate_Patches
        {
            private static void Prefix()
            {
                for (int i = 0; i < _self._constraints.Count; i++)
                {
                    Constraint constraint = _self._constraints[i];
                    if (constraint.parentTransform == null || constraint.childTransform == null)
                        continue;
                    if (constraint.enabled == false)
                        continue;
                    if (constraint.position)
                    {
                        constraint.childTransform.localPosition = constraint.originalChildPosition;
                        if (constraint.child != null)
                            constraint.child.changeAmount.pos = constraint.originalChildPosition;
                    }
                    if (constraint.rotation)
                    {
                        constraint.childTransform.localRotation = constraint.originalChildRotation;
                        if (constraint.child != null)
                            constraint.child.changeAmount.rot = constraint.originalChildRotation.eulerAngles;
                    }
                    if (constraint.scale)
                    {
                        constraint.childTransform.localScale = constraint.originalChildScale;
                        if (constraint.child != null)
                            constraint.child.changeAmount.scale = constraint.originalChildScale;
                    }
                }

                Studio.Studio.Instance.ExecuteDelayed(() =>
                {
                    for (int i = 0; i < _self._constraints.Count; i++)
                    {
                        Constraint constraint = _self._constraints[i];
                        if (constraint.parentTransform == null || constraint.childTransform == null)
                            continue;

                        ObjectCtrlInfo parentObjectSource = null;
                        ObjectCtrlInfo parentObjectDestination = null;
                        ObjectCtrlInfo childObjectSource = null;
                        ObjectCtrlInfo childObjectDestination = null;

                        Transform parentT = constraint.parentTransform;
                        while ((parentObjectSource = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(e => e.Value.guideObject.transformTarget == parentT).Value) == null)
                            parentT = parentT.parent;
                        foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
                        {
                            if (pair.Value == parentObjectSource.objectInfo.dicKey)
                            {
                                parentObjectDestination = Studio.Studio.Instance.dicObjectCtrl[pair.Key];
                                break;
                            }
                        }

                        Transform childT = constraint.childTransform;
                        while ((childObjectSource = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(e => e.Value.guideObject.transformTarget == childT).Value) == null)
                            childT = childT.parent;
                        foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
                        {
                            if (pair.Value == childObjectSource.objectInfo.dicKey)
                            {
                                childObjectDestination = Studio.Studio.Instance.dicObjectCtrl[pair.Key];
                                break;
                            }
                        }
                        if (parentObjectDestination != null && childObjectDestination != null)
                        {
                            var newConstraint = _self.AddConstraint(
                                                constraint.enabled,
                                                parentObjectDestination.guideObject.transformTarget.Find(constraint.parentTransform.GetPathFrom(parentObjectSource.guideObject.transformTarget)),
                                                childObjectDestination.guideObject.transformTarget.Find(constraint.childTransform.GetPathFrom(childObjectSource.guideObject.transformTarget)),
                                                constraint.position,
                                                constraint.mirrorPosition,
                                                constraint.positionOffset,
                                                constraint.rotation,
                                                constraint.mirrorRotation,
                                                constraint.lookAt,
                                                constraint.rotationOffset,
                                                constraint.scale,
                                                constraint.mirrorScale,
                                                constraint.scaleOffset,
                                                constraint.alias,
                                                constraint.positionChangeFactor,
                                                constraint.rotationChangeFactor,
                                                constraint.scaleChangeFactor,
                                                constraint.positionDamp,
                                                constraint.rotationDamp,
                                                constraint.scaleDamp,
                                                constraint.positionLocks,
                                                constraint.rotationLocks,
                                                constraint.scaleLocks,
                                                constraint.resetOriginalPosition,
                                                constraint.resetOriginalRotation,
                                                constraint.resetOriginalScale
                                               );

                            if( newConstraint != null )
                            {
                                newConstraint.fixDynamicBone = constraint.fixDynamicBone;
                            }
                        }
                    }
                }, 3);
            }
        }

        [HarmonyPatch(typeof(ObjectInfo), "Load", new[] { typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool) })]
        internal static class ObjectInfo_Load_Patches
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                int count = 0;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (count != 2 && inst.ToString().Contains("ReadInt32"))
                    {
                        ++count;
                        if (count == 2)
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Call, typeof(ObjectInfo_Load_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                        }
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

        [HarmonyPatch(typeof(GuideSelect), "OnPointerClick", new[] { typeof(PointerEventData) })]
        private static class GuideSelect_OnPointerClick_Patches
        {
            private static void Postfix()
            {
                GuideObject selectedGuideObject = _self._selectedGuideObjects.FirstOrDefault();

                if (selectedGuideObject != null)
                {
                    _self._selectedBone = selectedGuideObject.transformTarget;
                }
            }
        }

        // Applies the constraints that have GuideObjects linked (so that the underlying systems IK and FK can use those data after)
        private void ApplyNodesConstraints()
        {
            List<int> toDelete = null;
            for (int i = 0; i < _constraints.Count; i++)
            {
                Constraint constraint = _constraints[i];
                if (constraint.parentTransform == null || constraint.childTransform == null)
                {
                    if (toDelete == null)
                        toDelete = new List<int>();
                    toDelete.Add(i);
                    if (_selectedConstraint == constraint)
                    {
                        _selectedConstraint = null;
                        TimelineCompatibility.RefreshInterpolablesList();
                    }
                    continue;
                }
                if (constraint.enabled && (constraint.child != null || constraint.parent != null))
                {
                    if (constraint.position && (constraint.child == null || constraint.child.enablePos))
                        constraint.UpdatePosition();
                    if (constraint.rotation && (constraint.child == null || constraint.child.enableRot))
                        constraint.UpdateRotation();
                    if (constraint.scale && (constraint.child == null || constraint.child.enableScale))
                        constraint.UpdateScale();
                }
            }
            if (toDelete != null)
                for (int i = toDelete.Count - 1; i >= 0; --i)
                    RemoveConstraintAt(toDelete[i]);
        }

        // Applies all the constraints indiscriminately after everything overwriting everything
        private void ApplyConstraints()
        {
            List<int> toDelete = null;
            for (int i = 0; i < _constraints.Count; i++)
            {
                Constraint constraint = _constraints[i];
                if (constraint.parentTransform == null || constraint.childTransform == null)
                {
                    if (toDelete == null)
                        toDelete = new List<int>();
                    toDelete.Add(i);
                    if (_selectedConstraint == constraint)
                    {
                        _selectedConstraint = null;
                        TimelineCompatibility.RefreshInterpolablesList();
                    }
                    continue;
                }
                if (constraint.enabled == false)
                    continue;

                /* There is a timing when Transform is reset by DynamicBone. Skip the reset value so that it is not taken into the constraint.
                 * It is assumed that the function call is from Expression_LateUpdate_Patches.
                 */
                if (constraint.fixDynamicBone)
                    continue;

                if (constraint.position)
                    constraint.UpdatePosition();
                if (constraint.rotation)
                    constraint.UpdateRotation();
                if (constraint.scale)
                    constraint.UpdateScale();
            }
            if (toDelete != null)
                for (int i = toDelete.Count - 1; i >= 0; --i)
                    RemoveConstraintAt(toDelete[i]);

            if (_debugMode && _selectedBone != null)
            {
                _debugLocalPosition = _selectedBone.localPosition;
                _debugWorldPosition = _selectedBone.position;
                _debugLocalRotation = _selectedBone.localRotation;
                _debugWorldRotation = _selectedBone.rotation;
                _debugLocalScale = _selectedBone.localScale;
                _debugWorldScale = _selectedBone.lossyScale;
            }

            DrawDebugLines();
        }

        private void DrawDebugLines()
        {
            if (_parentCircle != null)
            {
                _parentCircle.active = _displayedConstraint.parentTransform != null && _showUI;
                if (_parentCircle.active)
                {
                    _parentCircle.MakeCircle(_displayedConstraint.parentTransform.position, Camera.main.transform.forward, _circleRadius);
                    _parentCircle.Draw();
                }
            }
            if (_childCircle != null)
            {
                _childCircle.active = _displayedConstraint.childTransform != null && _showUI;
                if (_childCircle.active)
                {
                    _childCircle.MakeCircle(_displayedConstraint.childTransform.position, Camera.main.transform.forward, _circleRadius);
                    _childCircle.Draw();
                }
            }

            if (_selectedCircle != null)
            {
                if (_advancedList)
                {
                    _selectedCircle.active = _selectedBone != null && _showUI;
                    if (_selectedCircle.active)
                    {
                        _selectedCircle.MakeCircle(_selectedBone.position, Camera.main.transform.forward, _circleRadius);
                        _selectedCircle.Draw();
                    }
                }
                else
                {
                    GuideObject selectedGuideObject = _selectedGuideObjects.FirstOrDefault();
                    _selectedCircle.active = selectedGuideObject != null && _showUI;
                    if (_selectedCircle.active)
                    {
                        _selectedCircle.MakeCircle(selectedGuideObject.transformTarget.position, Camera.main.transform.forward, _circleRadius);
                        _selectedCircle.Draw();
                    }
                }
            }

            if (_selectedConstraint != null && _showUI)
                _selectedConstraint.UpdateDebugLines();
        }

        private void WindowFunction(int id)
        {
            void DrawLinkRow(string transform, ref bool enabled, ref TransformLock locks, ref bool mirror, ref string xStr, ref string yStr, ref string zStr, ref string factorStr, ref string dampStr, ref bool resetOriginal, ref bool showExtra, Action onUseCurrent, Action onReset)
            {
                GUI.enabled = _displayedConstraint.parentTransform != null && _displayedConstraint.childTransform != null;
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.BeginHorizontal();
                    {
                        enabled = GUILayout.Toggle(enabled && _displayedConstraint.childTransform != null, $"Link {transform}");
                        GUILayout.FlexibleSpace();
                        //GUILayout.Label("X", GUILayout.ExpandWidth(false));
                        locks.x = GUILayout.Toggle(locks.x, new GUIContent("X", "If this axis is contrained to the parent or not"), "Button");
                        xStr = GUILayout.TextField(xStr, GUILayout.Width(50));
                        locks.y = GUILayout.Toggle(locks.y, new GUIContent("Y", "If this axis is contrained to the parent or not"), "Button");
                        yStr = GUILayout.TextField(yStr, GUILayout.Width(50));
                        locks.z = GUILayout.Toggle(locks.z, new GUIContent("Z", "If this axis is contrained to the parent or not"), "Button");
                        zStr = GUILayout.TextField(zStr, GUILayout.Width(50));
                        if (GUILayout.Button(new GUIContent("Use current", "Match current differences in parent and child as offsets, so these differences are kept upon contraining"), GUILayout.ExpandWidth(false)))
                            onUseCurrent();
                        if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                            onReset();
                        if (GUILayout.Button(showExtra ? "▲" : "▼", GUILayout.ExpandWidth(false)))
                            showExtra = !showExtra;
                    }
                    GUILayout.EndHorizontal();

                    if (showExtra)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("↳");
                            mirror = GUILayout.Toggle(mirror, new GUIContent("Mirror contraint", "Mirror the movement of the parent to the child"));
                            GUILayout.FlexibleSpace();
                            resetOriginal = GUILayout.Toggle(resetOriginal, new GUIContent("Reset child", "Reset this tranform back to its original state or not when disabling/deleting the constraint"));
                            GUILayout.Label(" | ");
                            GUILayout.Label(new GUIContent("Change factor", "Multiply the movement of the parent by this amount to the child movement"));
                            factorStr = GUILayout.TextField(factorStr, GUILayout.Width(50));
                            GUILayout.Label(new GUIContent("Dampen", "Dampen the childs movement towards the parent. higher values mean faster movement towards the parent. 0 is no dampening"));
                            dampStr = GUILayout.TextField(dampStr, GUILayout.Width(50));
                        }
                        GUILayout.EndHorizontal();
                        if (transform == "rotation")
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("  ");
                            _displayedConstraint.lookAt = GUILayout.Toggle(_displayedConstraint.lookAt, new GUIContent("Look at parent", "Make the front of the child always look at the position of the parent. Use offset to adjust what is considered the front (you can use 'Use Current' with this enabled to get the right offset)"));
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();
                GUI.enabled = true;
            }

            int visibleAreaSize = GUI.skin.window.border.top - 4;
            if (GUI.Button(new Rect(_windowRect.width - visibleAreaSize - 2, 2, visibleAreaSize, visibleAreaSize), "X"))
            {
                _showUI = false;
                _selectedConstraint?.SetActiveDebugLines(false);
                return;
            }

            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label((_displayedConstraint.parentTransform != null ? _displayedConstraint.parentTransform.name : ""));
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("->");
                            GUILayout.FlexibleSpace();
                            GUILayout.Label((_displayedConstraint.childTransform != null ? _displayedConstraint.childTransform.name : ""));
                        }
                        GUILayout.EndHorizontal();

                        DrawLinkRow("position", ref _displayedConstraint.position, ref _displayedConstraint.positionLocks, ref _displayedConstraint.mirrorPosition, ref _positionXStr, ref _positionYStr, ref _positionZStr, ref _positionFactorStr, ref _positionDampStr, ref _displayedConstraint.resetOriginalPosition, ref showExtraMov,
                            () =>
                            {
                                _onPreCullAction = () =>
                                {
                                    _displayedConstraint.positionOffset = _displayedConstraint.parentTransform.InverseTransformPoint(_displayedConstraint.childTransform.position);
                                    UpdateDisplayedPositionOffset();
                                };
                            },
                            () =>
                            {
                                _displayedConstraint.positionOffset = Vector3.zero;
                                UpdateDisplayedPositionOffset();
                            });

                        DrawLinkRow("rotation", ref _displayedConstraint.rotation, ref _displayedConstraint.rotationLocks, ref _displayedConstraint.mirrorRotation, ref _rotationXStr, ref _rotationYStr, ref _rotationZStr, ref _rotationFactorStr, ref _rotationDampStr, ref _displayedConstraint.resetOriginalRotation, ref showExtraRot,
                            () =>
                            {
                                _onPreCullAction = () =>
                                {
                                    if (_displayedConstraint.lookAt)
                                    {
                                        // https://docs.unity3d.com/ScriptReference/Quaternion.LookRotation.html
                                        var relativePos = _displayedConstraint.parentTransform.position - _displayedConstraint.childTransform.position;
                                        var lookAtRot = Quaternion.LookRotation(relativePos);
                                        if (_displayedConstraint.mirrorRotation)
                                            lookAtRot = new Quaternion(lookAtRot.x * -1f, lookAtRot.y * -1f, lookAtRot.z, lookAtRot.w);
                                        // Same as the no LookAt version below, just using the LookAt rotation the child would have without offsets as baseline for getting the offset
                                        _displayedConstraint.rotationOffset = Quaternion.Inverse(lookAtRot) * _displayedConstraint.childTransform.rotation;
                                    }
                                    else
                                        _displayedConstraint.rotationOffset = Quaternion.Inverse(_displayedConstraint.parentTransform.rotation) * _displayedConstraint.childTransform.rotation;
                                    UpdateDisplayedRotationOffset();
                                };
                            },
                            () =>
                            {
                                _displayedConstraint.rotationOffset = Quaternion.identity;
                                UpdateDisplayedRotationOffset();
                            });

                        DrawLinkRow("scale", ref _displayedConstraint.scale, ref _displayedConstraint.scaleLocks, ref _displayedConstraint.mirrorScale, ref _scaleXStr, ref _scaleYStr, ref _scaleZStr, ref _scaleFactorStr, ref _scaleDampStr, ref _displayedConstraint.resetOriginalScale, ref showExtraScale,
                            () =>
                            {
                                _onPreCullAction = () =>
                                {
                                    _displayedConstraint.scaleOffset = new Vector3(
                                            _displayedConstraint.childTransform.lossyScale.x / _displayedConstraint.parentTransform.lossyScale.x,
                                            _displayedConstraint.childTransform.lossyScale.y / _displayedConstraint.parentTransform.lossyScale.y,
                                            _displayedConstraint.childTransform.lossyScale.z / _displayedConstraint.parentTransform.lossyScale.z
                                    );
                                    UpdateDisplayedScaleOffset();
                                };
                            },
                            () =>
                            {
                                _displayedConstraint.scaleOffset = Vector3.one;
                                UpdateDisplayedScaleOffset();
                            });

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = _displayedConstraint.parentTransform != null && _displayedConstraint.childTransform != null;
                            GUILayout.Label("Alias", GUILayout.ExpandWidth(false));
                            _displayedConstraint.alias = GUILayout.TextField(_displayedConstraint.alias);
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = _displayedConstraint.parentTransform != null && _displayedConstraint.childTransform != null && (_displayedConstraint.position || _displayedConstraint.rotation || _displayedConstraint.scale) && _displayedConstraint.parentTransform != _displayedConstraint.childTransform;
                            if (GUILayout.Button("Add new"))
                            {
                                ValidateDisplayedPositionOffset();
                                ValidateDisplayedRotationOffset();
                                ValidateDisplayedScaleOffset();

                                var newConstraint = AddConstraint(true, _displayedConstraint.parentTransform, _displayedConstraint.childTransform, _displayedConstraint.position, _displayedConstraint.mirrorPosition, _displayedConstraint.positionOffset, _displayedConstraint.rotation, _displayedConstraint.mirrorRotation, _displayedConstraint.lookAt, _displayedConstraint.rotationOffset, _displayedConstraint.scale, _displayedConstraint.mirrorScale, _displayedConstraint.scaleOffset, _displayedConstraint.alias, _displayedConstraint.positionChangeFactor, _displayedConstraint.rotationChangeFactor, _displayedConstraint.scaleChangeFactor, _displayedConstraint.positionDamp, _displayedConstraint.rotationDamp, _displayedConstraint.scaleDamp, _displayedConstraint.positionLocks, _displayedConstraint.rotationLocks, _displayedConstraint.scaleLocks, _displayedConstraint.resetOriginalPosition, _displayedConstraint.resetOriginalRotation, _displayedConstraint.resetOriginalScale);

                                if (newConstraint != null)
                                {
                                    newConstraint.fixDynamicBone = _displayedConstraint.fixDynamicBone;
                                }
                            }
                            GUI.enabled = _selectedConstraint != null && _displayedConstraint.parentTransform != null && _displayedConstraint.childTransform != null && (_displayedConstraint.position || _displayedConstraint.rotation || _displayedConstraint.scale) && _displayedConstraint.parentTransform != _displayedConstraint.childTransform;
                            if (GUILayout.Button("Update selected"))
                            {
                                ValidateDisplayedPositionOffset();
                                ValidateDisplayedRotationOffset();
                                ValidateDisplayedScaleOffset();
                                _displayedConstraint.CopyTo(_selectedConstraint, false);
                                TimelineCompatibility.RefreshInterpolablesList();
                            }
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Search", GUILayout.ExpandWidth(false));
                _constraintsSearch = GUILayout.TextField(_constraintsSearch);
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    _constraintsSearch = "";
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical(GUILayout.Height(ConstraintsAreaHeight.Value));
                GUI.enabled = _selectedConstraint != null;
                if (GUILayout.Button("↑", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
                {
                    int index = _constraints.IndexOf(_selectedConstraint);

                    if (index > 0)
                    {
                        _constraints.RemoveAt(index);
                        _constraints.Insert(index - 1, _selectedConstraint);
                    }
                }
                if (GUILayout.Button("↓", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
                {
                    int index = _constraints.IndexOf(_selectedConstraint);

                    if (index < _constraints.Count - 1)
                    {
                        _constraints.RemoveAt(index);
                        _constraints.Insert(index + 1, _selectedConstraint);
                    }
                }
                GUI.enabled = true;
                GUILayout.EndVertical();

                _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Height(ConstraintsAreaHeight.Value), GUILayout.ExpandWidth(true));
                {
                    int toDelete = -1;
                    Action afterLoopAction = null;
                    for (int i = 0; i < _constraints.Count; i++)
                    {
                        Constraint constraint = _constraints[i];

                        if (constraint.parentTransform.name.IndexOf(_constraintsSearch, StringComparison.OrdinalIgnoreCase) == -1 && constraint.childTransform.name.IndexOf(_constraintsSearch, StringComparison.OrdinalIgnoreCase) == -1 && (string.IsNullOrEmpty(constraint.alias) || constraint.alias.IndexOf(_constraintsSearch, StringComparison.OrdinalIgnoreCase) == -1))
                            continue;

                        GUILayout.BeginHorizontal();
                        {
                            Color c = GUI.color;
                            if (_selectedConstraint == constraint)
                                GUI.color = Color.cyan;
                            bool newEnabled = GUILayout.Toggle(constraint.enabled, "", GUILayout.ExpandWidth(false));
                            if (constraint.enabled != newEnabled)
                                SetConstraintEnabled(constraint, newEnabled);

                            string constraintName;
                            if (string.IsNullOrEmpty(constraint.alias))
                                constraintName = constraint.parentTransform.name + " -> " + constraint.childTransform.name;
                            else
                                constraintName = constraint.alias;

                            if (GUILayout.Button(constraintName, _wrapButton))
                            {
                                if (_selectedConstraint != null)
                                    _selectedConstraint.SetActiveDebugLines(false);

                                _selectedConstraint = constraint;
                                _selectedConstraint.CopyTo(_displayedConstraint);
                                _selectedConstraint.SetActiveDebugLines(true);

                                UpdateDisplayedPositionOffset();
                                UpdateDisplayedRotationOffset();
                                UpdateDisplayedScaleOffset();

                                TimelineCompatibility.RefreshInterpolablesList();
                            }

                            constraint.fixDynamicBone = GUILayout.Toggle(constraint.fixDynamicBone, "Dynamic", GUILayout.ExpandWidth(false));

                            GUI.color = Color.red;
                            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                                toDelete = i;
                            GUI.color = c;
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (afterLoopAction != null)
                        afterLoopAction();
                    if (toDelete != -1)
                        RemoveConstraintAt(toDelete);
                }
                GUILayout.EndScrollView();

                GUILayout.EndHorizontal();

                _advancedList = GUILayout.Toggle(_advancedList, "Advanced List");

                GUILayout.BeginHorizontal();
                string oldSearch = _bonesSearch;
                GUILayout.Label("Search", GUILayout.ExpandWidth(false));
                _bonesSearch = GUILayout.TextField(_bonesSearch);
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    _bonesSearch = "";
                if (oldSearch.Length != 0 && _selectedBone != null && (_bonesSearch.Length == 0 || (_bonesSearch.Length < oldSearch.Length && oldSearch.StartsWith(_bonesSearch))))
                {
                    //string displayedName;
                    //bool aliased = true;
                    //if (_boneAliases.TryGetValue(this._selectedBone.name, out displayedName) == false)
                    //{
                    //    displayedName = this._selectedBone.name;
                    //    aliased = false;
                    //}
                    if (_selectedBone.name.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1/* || (aliased && displayedName.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1)*/)
                        OpenParents(_selectedBone, _selectedWorkspaceObject.transformTarget);
                }
                GUILayout.EndHorizontal();

                GuideObject selectedGuideObject = _selectedGuideObjects.FirstOrDefault();

                if (_advancedList == false)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Show nodes", GUILayout.ExpandWidth(false));
                    _selectedShowNodeType = (SimpleListShowNodeType)GUILayout.SelectionGrid((int)_selectedShowNodeType, _simpleListShowNodeTypeNames, 3);
                    GUILayout.EndHorizontal();

                    _simpleModeScroll = GUILayout.BeginScrollView(_simpleModeScroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Height(NodesAreaHeight.Value));
                    if (_selectedWorkspaceObject != null)
                    {
                        foreach (KeyValuePair<Transform, GuideObject> pair in _allGuideObjects)
                        {
                            if (pair.Key == null)
                                continue;
                            if (pair.Key.IsChildOf(_selectedWorkspaceObject.transformTarget) == false && pair.Key != _selectedWorkspaceObject.transformTarget)
                                continue;
                            switch (_selectedShowNodeType)
                            {
                                case SimpleListShowNodeType.IK:
                                    if (pair.Value.enablePos == false)
                                        continue;
                                    break;
                                case SimpleListShowNodeType.FK:
                                    if (pair.Value.enablePos)
                                        continue;
                                    break;
                            }
                            if (pair.Key.name.IndexOf(_bonesSearch, StringComparison.OrdinalIgnoreCase) == -1)
                                continue;
                            Color c = GUI.color;
                            if (pair.Value == selectedGuideObject)
                                GUI.color = Color.cyan;
                            else if (_displayedConstraint.parentTransform == pair.Value.transformTarget)
                                GUI.color = Color.green;
                            else if (_displayedConstraint.childTransform == pair.Value.transformTarget)
                                GUI.color = Color.red;

                            if (GUILayout.Button(pair.Key.name))
                            {
                                GuideObjectManager.Instance.selectObject = pair.Value;
                                _selectedBone = pair.Value.transformTarget;
                            }
                            GUI.color = c;
                        }
                    }
                    GUILayout.EndScrollView();
                }
                else
                {
                    _advancedModeScroll = GUILayout.BeginScrollView(_advancedModeScroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Height(NodesAreaHeight.Value));
                    if (_selectedWorkspaceObject != null)
                        foreach (Transform t in _selectedWorkspaceObject.transformTarget)
                            DisplayObjectTree(t.gameObject, 0);
                    GUILayout.EndScrollView();
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(selectedGuideObject != null ? "Set as parent" : "Set as parent (Camera)"))
                {
                    if (selectedGuideObject != null)
                        _displayedConstraint.parentTransform = _advancedList ? _selectedBone : selectedGuideObject.transformTarget;
                    else
                        _displayedConstraint.parentTransform = Studio.Studio.Instance.cameraCtrl.mainCmaera.transform;
                }
                GUI.enabled = true;

                GUI.enabled = selectedGuideObject != null;
                if (GUILayout.Button("Set as child"))
                {
                    _displayedConstraint.childTransform = _advancedList ? _selectedBone : selectedGuideObject.transformTarget;
                }
                GUI.enabled = true;

                GUI.enabled = _selectedBone != null;
                _debugMode = GUILayout.Toggle(_debugMode, "Debug", GUILayout.ExpandWidth(false));
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                if (_debugMode && _selectedBone != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Local Pos:", GUILayout.ExpandWidth(false));
                    GUILayout.TextField($"X {_debugLocalPosition.x} Y {_debugLocalPosition.y} Z {_debugLocalPosition.z}");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("World Pos:", GUILayout.ExpandWidth(false));
                    GUILayout.TextField($"X {_debugWorldPosition.x} Y {_debugWorldPosition.y} Z {_debugWorldPosition.z}");
                    GUILayout.EndHorizontal();

                    Vector3 euler = _debugLocalRotation.eulerAngles;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Local Rot (E):", GUILayout.ExpandWidth(false));
                    GUILayout.TextField($"X {euler.x} Y {euler.y} Z {euler.z}");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Local Rot (Q):", GUILayout.ExpandWidth(false));
                    GUILayout.TextField($"X {_debugLocalRotation.x} Y {_debugLocalRotation.y} Z {_debugLocalRotation.z} W {_debugLocalRotation.w}");
                    GUILayout.EndHorizontal();

                    euler = _debugWorldRotation.eulerAngles;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("World Rot (E):", GUILayout.ExpandWidth(false));
                    GUILayout.TextField($"X {euler.x} Y {euler.y} Z {euler.z}");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("World Rot (Q):", GUILayout.ExpandWidth(false));
                    GUILayout.TextField($"X {_debugLocalRotation.x} Y {_debugLocalRotation.y} Z {_debugLocalRotation.z} W {_debugLocalRotation.w}");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Local Scale:", GUILayout.ExpandWidth(false));
                    GUILayout.TextField($"X {_debugLocalScale.x} Y {_debugLocalScale.y} Z {_debugLocalScale.z}");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("World Scale:", GUILayout.ExpandWidth(false));
                    GUILayout.TextField($"X {_debugWorldScale.x} Y {_debugWorldScale.y} Z {_debugWorldScale.z}");
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
            KKAPI.Utilities.IMGUIUtils.DrawTooltip(_windowRect);
        }

        private void SetConstraintEnabled(Constraint constraint, bool newEnabled)
        {
            if (constraint.enabled && newEnabled == false)
            {
                if (constraint.position && constraint.resetOriginalPosition)
                {
                    constraint.childTransform.localPosition = constraint.originalChildPosition;
                    if (constraint.child != null)
                        constraint.child.changeAmount.pos = constraint.originalChildPosition;
                }
                if (constraint.rotation && constraint.resetOriginalRotation)
                {
                    constraint.childTransform.localRotation = constraint.originalChildRotation;
                    if (constraint.child != null)
                        constraint.child.changeAmount.rot = constraint.originalChildRotation.eulerAngles;
                }
                if (constraint.scale && constraint.resetOriginalScale)
                {
                    constraint.childTransform.localScale = constraint.originalChildScale;
                    if (constraint.child != null)
                        constraint.child.changeAmount.scale = constraint.originalChildScale;
                }
            }
            if (constraint.enabled == false && newEnabled)
            {
                constraint.originalChildPosition = constraint.childTransform.localPosition;
                constraint.originalChildRotation = constraint.childTransform.localRotation;
            }
            constraint.enabled = newEnabled;
        }

        private void UpdateDisplayedPositionOffset()
        {
            _positionXStr = _displayedConstraint.positionOffset.x.ToString("0.0000");
            _positionYStr = _displayedConstraint.positionOffset.y.ToString("0.0000");
            _positionZStr = _displayedConstraint.positionOffset.z.ToString("0.0000");
            _positionFactorStr = _displayedConstraint.positionChangeFactor.ToString("0.0000");
            _positionDampStr = _displayedConstraint.positionDamp.ToString("0.0000");
        }

        private void ValidateDisplayedPositionOffset()
        {
            float res;
            if (float.TryParse(_positionXStr, out res))
                _displayedConstraint.positionOffset.x = res;
            if (float.TryParse(_positionYStr, out res))
                _displayedConstraint.positionOffset.y = res;
            if (float.TryParse(_positionZStr, out res))
                _displayedConstraint.positionOffset.z = res;
            if (float.TryParse(_positionFactorStr, out res))
                _displayedConstraint.positionChangeFactor = res;
            if (float.TryParse(_positionDampStr, out res))
                _displayedConstraint.positionDamp = res;
            UpdateDisplayedPositionOffset();
        }

        private void UpdateDisplayedRotationOffset()
        {
            Vector3 euler = _displayedConstraint.rotationOffset.eulerAngles;
            _rotationXStr = euler.x.ToString("0.000");
            _rotationYStr = euler.y.ToString("0.000");
            _rotationZStr = euler.z.ToString("0.000");
            _rotationFactorStr = _displayedConstraint.rotationChangeFactor.ToString("0.0000");
            _rotationDampStr = _displayedConstraint.rotationDamp.ToString("0.0000");
        }

        private void ValidateDisplayedRotationOffset()
        {
            float resX;
            float resY;
            float resZ;
            if (!float.TryParse(_rotationXStr, out resX))
                resX = _displayedConstraint.rotationOffset.eulerAngles.x;
            if (!float.TryParse(_rotationYStr, out resY))
                resY = _displayedConstraint.rotationOffset.eulerAngles.y;
            if (!float.TryParse(_rotationZStr, out resZ))
                resZ = _displayedConstraint.rotationOffset.eulerAngles.z;
            if (float.TryParse(_rotationFactorStr, out float resFactor))
                _displayedConstraint.rotationChangeFactor = resFactor;
            if (float.TryParse(_rotationDampStr, out float resDamp))
                _displayedConstraint.rotationDamp = resDamp;

            _displayedConstraint.rotationOffset = Quaternion.Euler(resX, resY, resZ);
            UpdateDisplayedRotationOffset();
        }


        private void UpdateDisplayedScaleOffset()
        {
            _scaleXStr = _displayedConstraint.scaleOffset.x.ToString("0.0000");
            _scaleYStr = _displayedConstraint.scaleOffset.y.ToString("0.0000");
            _scaleZStr = _displayedConstraint.scaleOffset.z.ToString("0.0000");
            _scaleFactorStr = _displayedConstraint.scaleChangeFactor.ToString("0.0000");
            _scaleDampStr = _displayedConstraint.scaleDamp.ToString("0.0000");
        }

        private void ValidateDisplayedScaleOffset()
        {
            float res;
            if (float.TryParse(_scaleXStr, out res))
                _displayedConstraint.scaleOffset.x = res;
            if (float.TryParse(_scaleYStr, out res))
                _displayedConstraint.scaleOffset.y = res;
            if (float.TryParse(_scaleZStr, out res))
                _displayedConstraint.scaleOffset.z = res;
            if (float.TryParse(_scaleFactorStr, out res))
                _displayedConstraint.scaleChangeFactor = res;
            if (float.TryParse(_scaleDampStr, out res))
                _displayedConstraint.scaleDamp = res;
            UpdateDisplayedScaleOffset();
        }

        private Constraint AddConstraint(bool enabled, Transform parentTransform, Transform childTransform, bool linkPosition, Vector3 positionOffset, bool linkRotation, Quaternion rotationOffset, bool linkScale, Vector3 scaleOffset, string alias)
        {
            return AddConstraint(enabled, parentTransform, childTransform, linkPosition, false, positionOffset, linkRotation, false, false, rotationOffset, linkScale, false, scaleOffset, alias);
        }

        private Constraint AddConstraint(bool enabled, Transform parentTransform, Transform childTransform, bool linkPosition, bool mirrorPosition, Vector3 positionOffset, bool linkRotation, bool mirrorRotation, bool lookAt, Quaternion rotationOffset, bool linkScale, bool mirrorScale, Vector3 scaleOffset, string alias, float positionChangeFactor = 1, float rotationChangeFactor = 1, float scaleChangeFactor = 1, float positionDamp = 0, float rotationDamp = 0, float scaleDamp = 0, TransformLock positionLocks = null, TransformLock rotationLocks = null, TransformLock scaleLocks = null, bool resetOriginalPosition = true, bool resetOriginalRotation = true, bool resetOriginalScale = true)
        {
            bool shouldAdd = true;
            foreach (Constraint constraint in _constraints)
            {
                if (constraint.parentTransform == parentTransform && constraint.childTransform == childTransform ||
                    constraint.childTransform == parentTransform && constraint.parentTransform == childTransform)
                {
                    shouldAdd = false;
                    break;
                }
            }
            if (shouldAdd)
            {
                Constraint newConstraint = new Constraint();
                newConstraint.enabled = enabled;
                newConstraint.parentTransform = parentTransform;
                newConstraint.childTransform = childTransform;
                newConstraint.position = linkPosition;
                newConstraint.mirrorPosition = mirrorPosition;
                newConstraint.positionChangeFactor = positionChangeFactor;
                newConstraint.positionDamp = positionDamp;
                newConstraint.rotation = linkRotation;
                newConstraint.mirrorRotation = mirrorRotation;
                newConstraint.rotationChangeFactor = rotationChangeFactor;
                newConstraint.rotationDamp = rotationDamp;
                newConstraint.lookAt = lookAt;
                newConstraint.scale = linkScale;
                newConstraint.mirrorScale = mirrorScale;
                newConstraint.scaleChangeFactor = scaleChangeFactor;
                newConstraint.scaleDamp = scaleDamp;
                newConstraint.positionOffset = positionOffset;
                newConstraint.rotationOffset = rotationOffset;
                newConstraint.scaleOffset = scaleOffset;
                newConstraint.alias = alias;
                newConstraint.fixDynamicBone = false;
                newConstraint.resetOriginalPosition = resetOriginalPosition;
                newConstraint.resetOriginalRotation = resetOriginalRotation;
                newConstraint.resetOriginalScale = resetOriginalScale;

                if (positionLocks != null)
                    newConstraint.positionLocks = positionLocks.Copy();
                if (rotationLocks != null)
                    newConstraint.rotationLocks = rotationLocks.Copy();
                if (scaleLocks != null)
                    newConstraint.scaleLocks = scaleLocks.Copy();

                // Use current ParentTransform pos/rot/scale as default to not break backwards compatibility
                // Update to after adding if needed (e.g. OnSceneLoad/OnSceneImport
                newConstraint.originalParentPosition = parentTransform.position;
                newConstraint.originalParentRotation = parentTransform.rotation;
                newConstraint.originalParentScale = parentTransform.lossyScale;

                if (_allGuideObjects.TryGetValue(newConstraint.parentTransform, out newConstraint.parent) == false)
                    newConstraint.parent = null;
                if (_allGuideObjects.TryGetValue(newConstraint.childTransform, out newConstraint.child) == false)
                    newConstraint.child = null;
                newConstraint.originalChildPosition = newConstraint.childTransform.localPosition;
                newConstraint.originalChildRotation = newConstraint.childTransform.localRotation;
                newConstraint.originalChildScale = newConstraint.childTransform.localScale;

                _constraints.Add(newConstraint);
                TimelineCompatibility.RefreshInterpolablesList();
                return newConstraint;
            }
            return null;
        }

        private void ClearAllConstraints()
        {
            _constraints.Clear();
            _selectedConstraint = null;
        }

        private void RemoveConstraintAt(int index)
        {
            Constraint c = _constraints[index];
            if (c.childTransform != null)
            {
                if (c.position && c.resetOriginalPosition)
                {
                    c.childTransform.localPosition = c.originalChildPosition;
                    if (c.child != null)
                        c.child.changeAmount.pos = c.originalChildPosition;
                }
                if (c.rotation && c.resetOriginalRotation)
                {
                    c.childTransform.localRotation = c.originalChildRotation;
                    if (c.child != null)
                        c.child.changeAmount.rot = c.originalChildRotation.eulerAngles;
                }
                if (c.scale && c.resetOriginalScale)
                {
                    c.childTransform.localScale = c.originalChildScale;
                    if (c.child != null)
                        c.child.changeAmount.scale = c.originalChildScale;
                }
            }

            c.Destroy();
            _constraints.RemoveAt(index);
            if (c == _selectedConstraint)
                _selectedConstraint = null;
            TimelineCompatibility.RefreshInterpolablesList();
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            string displayedName = go.name;
            //bool aliased = true;
            //if (_boneAliases.TryGetValue(go.name, out displayedName) == false)
            //{
            //    displayedName = go.name;
            //    aliased = false;
            //}

            if (_bonesSearch.Length == 0 || go.name.IndexOf(_bonesSearch, StringComparison.OrdinalIgnoreCase) != -1/* || (aliased && displayedName.IndexOf(_bonesSearch, StringComparison.OrdinalIgnoreCase) != -1)*/)
            {
                Color c = GUI.color;
                if (_selectedBone == go.transform)
                    GUI.color = Color.cyan;
                else if (_displayedConstraint.parentTransform == go.transform)
                    GUI.color = Color.green;
                else if (_displayedConstraint.childTransform == go.transform)
                    GUI.color = Color.red;
                GUILayout.BeginHorizontal();
                if (_bonesSearch.Length == 0)
                {
                    GUILayout.Space(indent * 20f);
                    if (go.transform.childCount != 0)
                    {
                        if (GUILayout.Toggle(_openedBones.Contains(go), "", GUILayout.ExpandWidth(false)))
                        {
                            if (_openedBones.Contains(go) == false)
                                _openedBones.Add(go);
                        }
                        else
                        {
                            if (_openedBones.Contains(go))
                                _openedBones.Remove(go);
                        }
                    }
                    else
                        GUILayout.Space(20f);
                }
                if (GUILayout.Button(displayedName, GUILayout.ExpandWidth(false)))
                    _selectedBone = go.transform;
                GUI.color = c;
                GUILayout.EndHorizontal();
            }
            if (_bonesSearch.Length != 0 || _openedBones.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }


        private void OpenParents(Transform child, Transform limit)
        {
            if (child == limit)
                return;
            child = child.parent;
            while (child.parent != null && child != limit)
            {
                _openedBones.Add(child.gameObject);
                child = child.parent;
            }
            _openedBones.Add(child.gameObject);
        }
        #endregion

        #region Saves
#if KOIKATSU || AISHOUJO || HONEYSELECT2
        private void OnSceneLoad(string path)
        {
#if KOIKATSU
            if (_kkAnimationControllerInstalled == false)
                this.ExecuteDelayed(() =>
                {
                    LoadDataFromKKAnimationController(Studio.Studio.Instance.dicObjectCtrl);
                }, 2);
#endif

            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["constraints"]);
            OnSceneLoad(path, doc);
        }

        private void OnSceneImport(string path)
        {
#if KOIKATSU
            if (_kkAnimationControllerInstalled == false)
                this.ExecuteDelayed(() =>
                {
                    LoadDataFromKKAnimationController((Dictionary<int, ObjectCtrlInfo>)typeof(StudioSaveLoadApi).GetMethod("GetLoadedObjects", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { SceneOperationKind.Import }));
                }, 2);
#endif

            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["constraints"]);
            OnSceneImport(path, doc);
        }

        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                OnSceneSave(path, xmlWriter);

                PluginData data = new PluginData();
                data.version = _saveVersion;
                data.data.Add("constraints", stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }

        }
#endif

        private void OnSceneLoad(string path, XmlNode node)
        {
            if (node == null)
                return;
            Studio.Studio.Instance.ExecuteDelayed(() =>
            {
                LoadSceneGeneric(node.FirstChild, new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList());
            }, 8);
        }

        private void OnSceneImport(string path, XmlNode node)
        {
            if (node == null)
                return;
            Dictionary<int, ObjectCtrlInfo> toIgnore = new Dictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            Studio.Studio.Instance.ExecuteDelayed(() =>
            {
                LoadSceneGeneric(node.FirstChild, Studio.Studio.Instance.dicObjectCtrl.Where(e => toIgnore.ContainsKey(e.Key) == false).OrderBy(e => SceneInfo_Import_Patches._newToOldKeys[e.Key]).ToList());
            }, 8);
        }


        /// <summary>
        /// Other plugins should use this to force load some data.
        /// </summary>
        /// <param name="node"></param>
        public void ExternalLoadScene(XmlNode node)
        {
            ClearAllConstraints();
            LoadSceneGeneric(node, new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList());
        }

        private void LoadSceneGeneric(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            string v = node.Attributes["version"].Value;

            foreach (XmlNode childNode in node.ChildNodes)
            {
                int parentObjectIndex = XmlConvert.ToInt32(childNode.Attributes["parentObjectIndex"].Value);
                if (parentObjectIndex >= dic.Count)
                    continue;

                Transform parentTransform;
                if (parentObjectIndex == MainCameraParenObjectIndex)
                    parentTransform = Studio.Studio.Instance.cameraCtrl.mainCmaera.transform;
                else
                {
                    parentTransform = dic[parentObjectIndex].Value.guideObject.transformTarget;
                    parentTransform = parentTransform.Find(childNode.Attributes["parentPath"].Value);
                }
                if (parentTransform == null)
                    continue;

                int childObjectIndex = XmlConvert.ToInt32(childNode.Attributes["childObjectIndex"].Value);
                if (childObjectIndex >= dic.Count)
                    continue;
                Transform childTransform;
                if (childNode.Attributes["childPath"] != null)
                {
                    childTransform = dic[childObjectIndex].Value.guideObject.transformTarget;
                    childTransform = childTransform.Find(childNode.Attributes["childPath"].Value);
                    if (childTransform == null)
                        continue;
                }
                else
                {
                    childTransform = dic[childObjectIndex].Value.guideObject.transformTarget;
                    childTransform = childTransform.FindDescendant(childNode.Attributes["childName"].Value);
                    if (childTransform == null)
                        continue;
                }

                Constraint c = AddConstraint(
                        childNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value),
                        parentTransform,
                        childTransform,
                        XmlConvert.ToBoolean(childNode.Attributes["position"].Value),
                        new Vector3(
                                XmlConvert.ToSingle(childNode.Attributes["positionOffsetX"].Value),
                                XmlConvert.ToSingle(childNode.Attributes["positionOffsetY"].Value),
                                XmlConvert.ToSingle(childNode.Attributes["positionOffsetZ"].Value)
                        ),
                        XmlConvert.ToBoolean(childNode.Attributes["rotation"].Value),
                        new Quaternion(
                                XmlConvert.ToSingle(childNode.Attributes["rotationOffsetX"].Value),
                                XmlConvert.ToSingle(childNode.Attributes["rotationOffsetY"].Value),
                                XmlConvert.ToSingle(childNode.Attributes["rotationOffsetZ"].Value),
                                XmlConvert.ToSingle(childNode.Attributes["rotationOffsetW"].Value)
                        ),
                        childNode.Attributes["scale"] != null && XmlConvert.ToBoolean(childNode.Attributes["scale"].Value),
                        childNode.Attributes["scaleOffsetX"] == null
                                ? Vector3.one
                                : new Vector3(
                                        XmlConvert.ToSingle(childNode.Attributes["scaleOffsetX"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["scaleOffsetY"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["scaleOffsetZ"].Value)
                                ),
                        childNode.Attributes["alias"] != null ? childNode.Attributes["alias"].Value : ""
                );

                if (c != null)
                {
                    if (childNode.Attributes["uniqueLoadId"] != null)
                        c.uniqueLoadId = XmlConvert.ToInt32(childNode.Attributes["uniqueLoadId"].Value);

                    if (childNode.Attributes["dynamic"] != null)
                        c.fixDynamicBone = XmlConvert.ToBoolean(childNode.Attributes["dynamic"].Value);

                    if (childNode.Attributes["mirrorPosition"] != null)
                        c.mirrorPosition = XmlConvert.ToBoolean(childNode.Attributes["mirrorPosition"].Value);
                    if (childNode.Attributes["mirrorRotation"] != null)
                        c.mirrorRotation = XmlConvert.ToBoolean(childNode.Attributes["mirrorRotation"].Value);
                    if (childNode.Attributes["mirrorScale"] != null)
                        c.mirrorScale = XmlConvert.ToBoolean(childNode.Attributes["mirrorScale"].Value);
                    if (childNode.Attributes["lookAt"] != null)
                        c.lookAt = XmlConvert.ToBoolean(childNode.Attributes["lookAt"].Value);

                    if (childNode.Attributes["originalParentPositionX"] != null)
                        c.originalParentPosition = new Vector3(
                            XmlConvert.ToSingle(childNode.Attributes["originalParentPositionX"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["originalParentPositionY"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["originalParentPositionZ"].Value)
                        );
                    if (childNode.Attributes["originalParentRotationX"] != null)
                        c.originalParentRotation = new Quaternion(
                            XmlConvert.ToSingle(childNode.Attributes["originalParentRotationX"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["originalParentRotationY"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["originalParentRotationZ"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["originalParentRotationW"].Value)
                        );
                    if (childNode.Attributes["originalParentScaleX"] != null)
                        c.originalParentScale = new Vector3(
                            XmlConvert.ToSingle(childNode.Attributes["originalParentScaleX"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["originalParentScaleY"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["originalParentScaleZ"].Value)
                        );
                    // All added in the same update, so should alwasy exist if one of them is not null
                    if (childNode.Attributes["positionChangeFactor"] != null)
                    {
                        c.positionChangeFactor = XmlConvert.ToSingle(childNode.Attributes["positionChangeFactor"].Value);
                        c.rotationChangeFactor = XmlConvert.ToSingle(childNode.Attributes["rotationChangeFactor"].Value);
                        c.scaleChangeFactor = XmlConvert.ToSingle(childNode.Attributes["scaleChangeFactor"].Value);

                        c.positionDamp = XmlConvert.ToSingle(childNode.Attributes["positionDamp"].Value);
                        c.rotationDamp = XmlConvert.ToSingle(childNode.Attributes["rotationDamp"].Value);
                        c.scaleDamp = XmlConvert.ToSingle(childNode.Attributes["scaleDamp"].Value);

                        c.positionLocks = new TransformLock(
                            XmlConvert.ToBoolean(childNode.Attributes["positionLocksX"].Value),
                            XmlConvert.ToBoolean(childNode.Attributes["positionLocksY"].Value),
                            XmlConvert.ToBoolean(childNode.Attributes["positionLocksZ"].Value)
                        );
                        c.rotationLocks = new TransformLock(
                            XmlConvert.ToBoolean(childNode.Attributes["rotationLocksX"].Value),
                            XmlConvert.ToBoolean(childNode.Attributes["rotationLocksY"].Value),
                            XmlConvert.ToBoolean(childNode.Attributes["rotationLocksZ"].Value)
                        );
                        c.scaleLocks = new TransformLock(
                            XmlConvert.ToBoolean(childNode.Attributes["scaleLocksX"].Value),
                            XmlConvert.ToBoolean(childNode.Attributes["scaleLocksY"].Value),
                            XmlConvert.ToBoolean(childNode.Attributes["scaleLocksZ"].Value)
                        );
                    }
                    if (childNode.Attributes["resetOriginalPosition"] != null)
                    {
                        c.resetOriginalPosition = XmlConvert.ToBoolean(childNode.Attributes["resetOriginalPosition"].Value);
                        c.resetOriginalRotation = XmlConvert.ToBoolean(childNode.Attributes["resetOriginalRotation"].Value);
                        c.resetOriginalScale = XmlConvert.ToBoolean(childNode.Attributes["resetOriginalScale"].Value);
                    }
                }
            }
        }

#if KOIKATSU
        private void LoadDataFromKKAnimationController(Dictionary<int, ObjectCtrlInfo> loadedObjects)
        {
            PluginData v1Data = ExtendedSave.GetSceneExtendedDataById("KK_AnimationController");
            object animationInfo;
            List<AnimationControllerInfo> animationControllerInfoList = null;
            if (v1Data != null && v1Data.data != null && v1Data.data.TryGetValue("AnimationInfo", out animationInfo))
                animationControllerInfoList = ((object[])animationInfo).Select(x => AnimationControllerInfo.Unserialize((byte[])x)).ToList();

            foreach (var kvp in loadedObjects)
            {
                OCIChar ociChar = kvp.Value as OCIChar;
                if (ociChar == null)
                    continue;
                try
                {
                    //Version 1 save data
                    if (animationControllerInfoList != null)
                    {
                        foreach (AnimationControllerInfo animInfo in animationControllerInfoList)
                        {
                            //See if this is the right character
                            if (animInfo.CharDicKey != kvp.Key)
                                continue;

                            ObjectCtrlInfo linkedItem = loadedObjects[animInfo.ItemDicKey];

                            if (!animInfo.Version.IsNullOrEmpty())
                                AddLink(ociChar, animInfo.IKPart, linkedItem);
                        }
                        Logger.LogInfo($"Loaded KK_AnimationController animations for character {ociChar.charInfo.chaFile.parameter.fullname.Trim()}");
                    }
                    //Version 2 save data
                    else
                    {
                        PluginData data = ExtendedSave.GetExtendedDataById(ociChar.charInfo.chaFile, "com.deathweasel.bepinex.animationcontroller");
                        if (data != null && data.data != null)
                        {
                            if (data.data.TryGetValue("Links", out object loadedLinks) && loadedLinks != null)
                                foreach (KeyValuePair<object, object> link in (Dictionary<object, object>)loadedLinks)
                                    AddLink(ociChar, (string)link.Key, loadedObjects[(int)link.Value]);

                            if (data.data.TryGetValue("Eyes", out var loadedEyeLink) && loadedEyeLink != null)
                                AddEyeLink(ociChar, loadedObjects[(int)loadedEyeLink]);

                            Logger.LogInfo($"Loaded KK_AnimationController animations for character {ociChar.charInfo.chaFile.parameter.fullname.Trim()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Could not load KK_AnimationController animations.\n" + ex);
                }
            }
        }

        private void AddLink(OCIChar ociChar, string selectedGuideObject, ObjectCtrlInfo selectedObject)
        {
            OCIChar.IKInfo ikInfo = ociChar.listIKTarget.First(x => x.boneObject.name == selectedGuideObject);

            Transform parent = GetChildRootFromObjectCtrl(selectedObject);
            Transform child = ikInfo.guideObject.transformTarget;

            if (parent == null || child == null)
                return;
            foreach (Constraint c in _constraints)
            {
                if (c.parentTransform == parent && c.childTransform == child ||
                    c.childTransform == parent && c.parentTransform == child)
                    return;
            }

            AddConstraint(true, parent, child, true, Vector3.zero, true, Quaternion.identity, false, Vector3.one, "");
        }

        private void AddEyeLink(OCIChar ociChar, ObjectCtrlInfo selectedObject)
        {
            Transform parent = selectedObject.guideObject.transformTarget;
            Transform child = ociChar.lookAtInfo.target;

            if (parent == null || child == null)
                return;
            foreach (Constraint c in _constraints)
            {
                if (c.parentTransform == parent && c.childTransform == child ||
                    c.childTransform == parent && c.parentTransform == child)
                    return;
            }

            Constraint constraint = new Constraint();
            constraint.enabled = true;
            constraint.parentTransform = parent;
            constraint.childTransform = child;

            if (_allGuideObjects.TryGetValue(constraint.parentTransform, out constraint.parent) == false)
                constraint.parent = null;
            if (_allGuideObjects.TryGetValue(constraint.childTransform, out constraint.child) == false)
                constraint.child = null;

            constraint.position = true;
            constraint.positionOffset = Vector3.zero;
            constraint.rotation = true;
            constraint.rotationOffset = Quaternion.identity;
            constraint.scale = false;
            constraint.scaleOffset = Vector3.one;
            constraint.fixDynamicBone = false;

            AddConstraint(true, parent, child, true, Vector3.zero, true, Quaternion.identity, false, Vector3.one, "");
        }

        private Transform GetChildRootFromObjectCtrl(ObjectCtrlInfo objectCtrlInfo)
        {
            if (objectCtrlInfo == null)
                return null;
            OCIItem ociitem;
            if ((ociitem = (objectCtrlInfo as OCIItem)) != null)
                return ociitem.childRoot;
            OCIFolder ocifolder;
            if ((ocifolder = (objectCtrlInfo as OCIFolder)) != null)
                return ocifolder.childRoot;
            OCIRoute ociroute;
            if ((ociroute = (objectCtrlInfo as OCIRoute)) != null)
                return ociroute.childRoot;
            return null;
        }
#endif

        private void OnSceneSave(string path, XmlTextWriter xmlWriter)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();

            xmlWriter.WriteStartElement("constraints");

            xmlWriter.WriteAttributeString("version", Version);

            for (int i = 0; i < _constraints.Count; i++)
            {
                Constraint constraint = _constraints[i];
                int parentObjectIndex = -1;
                Transform parentT = constraint.parentTransform;
                if (parentT == Studio.Studio.Instance.cameraCtrl.mainCmaera.transform)
                    parentObjectIndex = MainCameraParenObjectIndex;
                else
                    while ((parentObjectIndex = dic.FindIndex(e => e.Value.guideObject.transformTarget == parentT)) == -1)
                        parentT = parentT.parent;
                string parentPath = constraint.parentTransform.GetPathFrom(parentT);

                int childObjectIndex = -1;
                Transform childT = constraint.childTransform;
                while ((childObjectIndex = dic.FindIndex(e => e.Value.guideObject.transformTarget == childT)) == -1)
                    childT = childT.parent;
                string childPath = constraint.childTransform.GetPathFrom(childT);

                xmlWriter.WriteStartElement("constraint");

                xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(constraint.enabled));
                xmlWriter.WriteAttributeString("parentObjectIndex", XmlConvert.ToString(parentObjectIndex));
                xmlWriter.WriteAttributeString("parentPath", parentPath);
                xmlWriter.WriteAttributeString("childObjectIndex", XmlConvert.ToString(childObjectIndex));
                xmlWriter.WriteAttributeString("childPath", childPath);
                xmlWriter.WriteAttributeString("uniqueLoadId", XmlConvert.ToString(i));

                xmlWriter.WriteAttributeString("position", XmlConvert.ToString(constraint.position));
                xmlWriter.WriteAttributeString("mirrorPosition", XmlConvert.ToString(constraint.mirrorPosition));
                xmlWriter.WriteAttributeString("positionOffsetX", XmlConvert.ToString(constraint.positionOffset.x));
                xmlWriter.WriteAttributeString("positionOffsetY", XmlConvert.ToString(constraint.positionOffset.y));
                xmlWriter.WriteAttributeString("positionOffsetZ", XmlConvert.ToString(constraint.positionOffset.z));

                xmlWriter.WriteAttributeString("rotation", XmlConvert.ToString(constraint.rotation));
                xmlWriter.WriteAttributeString("mirrorRotation", XmlConvert.ToString(constraint.mirrorRotation));
                xmlWriter.WriteAttributeString("lookAt", XmlConvert.ToString(constraint.lookAt));
                xmlWriter.WriteAttributeString("rotationOffsetW", XmlConvert.ToString(constraint.rotationOffset.w));
                xmlWriter.WriteAttributeString("rotationOffsetX", XmlConvert.ToString(constraint.rotationOffset.x));
                xmlWriter.WriteAttributeString("rotationOffsetY", XmlConvert.ToString(constraint.rotationOffset.y));
                xmlWriter.WriteAttributeString("rotationOffsetZ", XmlConvert.ToString(constraint.rotationOffset.z));

                xmlWriter.WriteAttributeString("scale", XmlConvert.ToString(constraint.scale));
                xmlWriter.WriteAttributeString("mirrorScale", XmlConvert.ToString(constraint.mirrorScale));
                xmlWriter.WriteAttributeString("scaleOffsetX", XmlConvert.ToString(constraint.scaleOffset.x));
                xmlWriter.WriteAttributeString("scaleOffsetY", XmlConvert.ToString(constraint.scaleOffset.y));
                xmlWriter.WriteAttributeString("scaleOffsetZ", XmlConvert.ToString(constraint.scaleOffset.z));

                xmlWriter.WriteAttributeString("originalParentPositionX", XmlConvert.ToString(constraint.originalParentPosition.x));
                xmlWriter.WriteAttributeString("originalParentPositionY", XmlConvert.ToString(constraint.originalParentPosition.y));
                xmlWriter.WriteAttributeString("originalParentPositionZ", XmlConvert.ToString(constraint.originalParentPosition.z));

                xmlWriter.WriteAttributeString("originalParentRotationX", XmlConvert.ToString(constraint.originalParentRotation.x));
                xmlWriter.WriteAttributeString("originalParentRotationY", XmlConvert.ToString(constraint.originalParentRotation.y));
                xmlWriter.WriteAttributeString("originalParentRotationZ", XmlConvert.ToString(constraint.originalParentRotation.z));
                xmlWriter.WriteAttributeString("originalParentRotationW", XmlConvert.ToString(constraint.originalParentRotation.w));

                xmlWriter.WriteAttributeString("originalParentScaleX", XmlConvert.ToString(constraint.originalParentScale.x));
                xmlWriter.WriteAttributeString("originalParentScaleY", XmlConvert.ToString(constraint.originalParentScale.y));
                xmlWriter.WriteAttributeString("originalParentScaleZ", XmlConvert.ToString(constraint.originalParentScale.z));

                xmlWriter.WriteAttributeString("positionChangeFactor", XmlConvert.ToString(constraint.positionChangeFactor));
                xmlWriter.WriteAttributeString("rotationChangeFactor", XmlConvert.ToString(constraint.rotationChangeFactor));
                xmlWriter.WriteAttributeString("scaleChangeFactor", XmlConvert.ToString(constraint.scaleChangeFactor));

                xmlWriter.WriteAttributeString("positionDamp", XmlConvert.ToString(constraint.positionDamp));
                xmlWriter.WriteAttributeString("rotationDamp", XmlConvert.ToString(constraint.rotationDamp));
                xmlWriter.WriteAttributeString("scaleDamp", XmlConvert.ToString(constraint.scaleDamp));

                xmlWriter.WriteAttributeString("positionLocksX", XmlConvert.ToString(constraint.positionLocks.x));
                xmlWriter.WriteAttributeString("positionLocksY", XmlConvert.ToString(constraint.positionLocks.y));
                xmlWriter.WriteAttributeString("positionLocksZ", XmlConvert.ToString(constraint.positionLocks.z));

                xmlWriter.WriteAttributeString("rotationLocksX", XmlConvert.ToString(constraint.rotationLocks.x));
                xmlWriter.WriteAttributeString("rotationLocksY", XmlConvert.ToString(constraint.rotationLocks.y));
                xmlWriter.WriteAttributeString("rotationLocksZ", XmlConvert.ToString(constraint.rotationLocks.z));

                xmlWriter.WriteAttributeString("scaleLocksX", XmlConvert.ToString(constraint.scaleLocks.x));
                xmlWriter.WriteAttributeString("scaleLocksY", XmlConvert.ToString(constraint.scaleLocks.y));
                xmlWriter.WriteAttributeString("scaleLocksZ", XmlConvert.ToString(constraint.scaleLocks.z));

                xmlWriter.WriteAttributeString("resetOriginalPosition", XmlConvert.ToString(constraint.resetOriginalPosition));
                xmlWriter.WriteAttributeString("resetOriginalRotation", XmlConvert.ToString(constraint.resetOriginalRotation));
                xmlWriter.WriteAttributeString("resetOriginalScale", XmlConvert.ToString(constraint.resetOriginalScale));

                xmlWriter.WriteAttributeString("alias", constraint.alias);
                xmlWriter.WriteAttributeString("dynamic", XmlConvert.ToString(constraint.fixDynamicBone));

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }
        #endregion

        #region Timeline Compatibility
        private void PopulateTimeline()
        {
            TimelineCompatibility.AddInterpolableModelDynamic(
                    owner: Name,
                    id: "constraintEnabled",
                    name: "Constraint Enabled",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        Constraint c = (Constraint)parameter;
                        bool newEnabled = (bool)leftValue;
                        if (c.enabled != newEnabled)
                            SetConstraintEnabled(c, newEnabled);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => _selectedConstraint != null,
                    getValue: (oci, parameter) => ((Constraint)parameter).enabled,
                    readValueFromXml: (parameter, node) => node.ReadBool("value"),
                    writeValueToXml: (parameter, writer, value) => writer.WriteValue("value", (bool)value),
                    getParameter: oci => _selectedConstraint,
                    readParameterFromXml: (oci, node) =>
                    {
                        int uniqueLoadId = node.ReadInt("parameter");
                        foreach (Constraint c in _constraints)
                            if (c.uniqueLoadId != null && c.uniqueLoadId.Value == uniqueLoadId)
                            {
                                c.uniqueLoadId = null;
                                return c;
                            }
                        return null;
                    },
                    writeParameterToXml: (oci, writer, parameter) =>
                    {
                        Constraint c = (Constraint)parameter;
                        int uniqueLoadId = _self._constraints.IndexOf(c);
                        if (uniqueLoadId != -1) // Should never happen but just in case
                            writer.WriteValue("parameter", uniqueLoadId);
                    },
                    checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                    {
                        if (parameter == null)
                            return false;
                        Constraint c = (Constraint)parameter;
                        if (c.destroyed || c.parentTransform == null || c.childTransform == null)
                            return false;
                        return true;
                    },
                    useOciInHash: false,
                    getFinalName: (currentName, oci, parameter) =>
                    {
                        if (parameter is Constraint c)
                            return string.IsNullOrEmpty(c.alias) == false ? $"NC: {c.alias}" : $"NC: {c.parentTransform?.name}/{c.childTransform?.name}";
                        return currentName;
                    });
        }
        #endregion
    }
}
