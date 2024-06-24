using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using ToolBox.Extensions;
using UnityEngine;
using Vectrosity;

namespace HSPE.AMModules
{
    public class BonesEditor : AdvancedModeModule
    {
#if HONEYSELECT || KOIKATSU || PLAYHOME
        private const float _cubeSize = 0.012f;
#else
        private const float _cubeSize = 0.12f;
#endif
        #region Private Types
        private enum CoordType
        {
            Position,
            Rotation,
            Scale,
            RotateAround
        }

        private class TransformData
        {
            public EditableValue<Vector3> position;
            public EditableValue<Quaternion> rotation;
            public EditableValue<Vector3> scale;
            public EditableValue<Vector3> originalPosition;
            public EditableValue<Quaternion> originalRotation;
            public EditableValue<Vector3> originalScale;

            public TransformData()
            {
            }

            public TransformData(TransformData other)
            {
                position = other.position;
                rotation = other.rotation;
                scale = other.scale;
                originalPosition = other.originalPosition;
                originalRotation = other.originalRotation;
                originalScale = other.originalScale;
            }
        }
        #endregion

        #region Private Variables
        private static int _positionIncIndex = -1;
        private static float _positionInc = 0.1f;
        private static int _rotationIncIndex = 0;
        private static float _rotationInc = 1f;
        private static int _scaleIncIndex = -2;
        private static float _scaleInc = 0.01f;
        private static int _rotateAroundIncIndex = 0;
        private static float _rotateAroundInc = 1f;
        private static string _search = "";
        internal static readonly Dictionary<string, string> _femaleShortcuts = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> _maleShortcuts = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> _itemShortcuts = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> _boneAliases = new Dictionary<string, string>();
        private static Vector3? _clipboardPosition;
        private static Quaternion? _clipboardRotation;
        private static Vector3? _clipboardScale;

        private readonly GenericOCITarget _target;
        private Vector2 _boneEditionScroll;
        private string _currentAlias = "";
        private Transform _boneTarget;
        private Transform _twinBoneTarget;
        private bool _symmetricalEdition = false;
        private CoordType _boneEditionCoordType = CoordType.Rotation;
        private Dictionary<GameObject, TransformData> _dirtyBones = new Dictionary<GameObject, TransformData>();
        private bool _lastShouldSaveValue = false;
        private Vector3? _oldFKRotationValue;
        private Vector3? _oldFKTwinRotationValue;
        private Vector2 _shortcutsScroll;
        private readonly Dictionary<Transform, string> _boneEditionShortcuts = new Dictionary<Transform, string>();
        private bool _removeShortcutMode;
        private readonly HashSet<GameObject> _openedBones = new HashSet<GameObject>();
        private bool _drawGizmos = true;
        private bool _onlyModified = false;
        private bool _sortByName = false;
        private bool _isWorld = false;
        private bool _goToObject;
        private List<GameObject> searchResults;

        private static readonly List<VectorLine> _cubeDebugLines = new List<VectorLine>();
        #endregion

        #region Public Accessors
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BonesEditor; } }
        public override string displayName { get { return "Bones"; } }
        public override bool isEnabled
        {
            set
            {
                base.isEnabled = value;
                UpdateDebugLinesState(this);
            }
        }
        internal bool ValidBoneTarget(Transform transform)
        {
            if (transform != null && transform.GetComponent<PoseController>() == null)
            {
                return transform.GetComponentInParent<PoseController>() == _parent;
            }
            return false;
        }

        #endregion

        #region Constructor
        public BonesEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            _target = target;
            _parent.onLateUpdate += LateUpdate;
            _parent.onDisable += OnDisable;
            if (_cubeDebugLines.Count == 0)
            {
                Vector3 topLeftForward = (Vector3.up + Vector3.left + Vector3.forward) * _cubeSize,
                    topRightForward = (Vector3.up + Vector3.right + Vector3.forward) * _cubeSize,
                    bottomLeftForward = (Vector3.down + Vector3.left + Vector3.forward) * _cubeSize,
                    bottomRightForward = (Vector3.down + Vector3.right + Vector3.forward) * _cubeSize,
                    topLeftBack = (Vector3.up + Vector3.left + Vector3.back) * _cubeSize,
                    topRightBack = (Vector3.up + Vector3.right + Vector3.back) * _cubeSize,
                    bottomLeftBack = (Vector3.down + Vector3.left + Vector3.back) * _cubeSize,
                    bottomRightBack = (Vector3.down + Vector3.right + Vector3.back) * _cubeSize;
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftForward, topRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightForward, bottomRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightForward, bottomLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftForward, topLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topRightBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightBack, bottomRightBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomLeftBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, topLeftBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightBack, topRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, bottomLeftForward));

                VectorLine l = VectorLine.SetLine(_redColor, Vector3.zero, Vector3.right * _cubeSize * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);
                l = VectorLine.SetLine(_greenColor, Vector3.zero, Vector3.up * _cubeSize * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);
                l = VectorLine.SetLine(_blueColor, Vector3.zero, Vector3.forward * _cubeSize * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);

                foreach (VectorLine line in _cubeDebugLines)
                {
                    line.lineWidth = 2f;
                    line.active = false;
                }
            }

            if (_target.type == GenericOCITarget.Type.Character)
            {
#if HONEYSELECT || PLAYHOME
                if (this._target.isFemale)
                {
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Hand_s_L"), "L. Hand");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Hand_s_R"), "R. Hand");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Foot02_L"), "L. Foot");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Foot02_R"), "R. Foot");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_FaceRoot"), "Face");
                }
                else
                {
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_Hand_s_L"), "L. Hand");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_Hand_s_R"), "R. Hand");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_Foot02_L"), "L. Foot");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_Foot02_R"), "R. Foot");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_FaceRoot"), "Face");
                }
#elif KOIKATSU
                _boneEditionShortcuts.Add(_parent.transform.FindDescendant("cf_s_hand_L"), "L. Hand");
                _boneEditionShortcuts.Add(_parent.transform.FindDescendant("cf_s_hand_R"), "R. Hand");
                _boneEditionShortcuts.Add(_parent.transform.FindDescendant("cf_j_foot_L"), "L. Foot");
                _boneEditionShortcuts.Add(_parent.transform.FindDescendant("cf_j_foot_R"), "R. Foot");
                _boneEditionShortcuts.Add(_parent.transform.FindDescendant("cf_J_FaceBase"), "Face");
#elif AISHOUJO || HONEYSELECT2
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Hand_s_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Hand_s_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Foot02_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Foot02_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_FaceRoot"), "Face");
#endif
            }
            UpdateSearch();
            //this._parent.StartCoroutine(this.EndOfFrame());
        }
        #endregion

        #region Unity Methods
        private void LateUpdate()
        {
            if (_target.type == GenericOCITarget.Type.Item)
                ApplyBoneManualCorrection();
        }

        private void OnDisable()
        {
            if (_dirtyBones.Count == 0)
                return;
            foreach (KeyValuePair<GameObject, TransformData> kvp in _dirtyBones)
            {
                if (kvp.Key == null)
                    continue;
                if (kvp.Value.scale.hasValue)
                    kvp.Key.transform.localScale = kvp.Value.originalScale;
                if (kvp.Value.rotation.hasValue)
                    kvp.Key.transform.localRotation = kvp.Value.originalRotation;
                if (kvp.Value.position.hasValue)
                    kvp.Key.transform.localPosition = kvp.Value.originalPosition;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _parent.onLateUpdate -= LateUpdate;
            _parent.onDisable -= OnDisable;
        }

        #endregion

        #region Public Methods
        public override void IKExecutionOrderOnPostLateUpdate()
        {
            ApplyBoneManualCorrection();
        }

        public override void DrawAdvancedModeChanged()
        {
            UpdateDebugLinesState(this);
        }

        public static void SelectionChanged(BonesEditor self)
        {
            UpdateDebugLinesState(self);
        }

        public Transform GetTwinBone(Transform bone)
        {
            if (bone.name.EndsWith("_L"))
                return _parent.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_R");
            if (bone.name.EndsWith("_R"))
                return _parent.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_L");
            if (bone.parent.name.EndsWith("_L"))
            {
                Transform twinParent = _parent.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_R");
                if (twinParent != null)
                {
                    int index = bone.GetSiblingIndex();
                    if (index < twinParent.childCount)
                        return twinParent.GetChild(index);
                }
            }
            if (bone.parent.name.EndsWith("_R"))
            {
                Transform twinParent = _parent.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_L");
                if (twinParent != null)
                {
                    int index = bone.GetSiblingIndex();
                    if (index < twinParent.childCount)
                        return twinParent.GetChild(index);
                }
            }
            return null;
        }

        public override void GUILogic()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            string oldSearch = _search;
            GUILayout.Label("Search", GUILayout.ExpandWidth(false));
            _search = GUILayout.TextField(_search);
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                _search = "";
            if (oldSearch.Length != 0 && _boneTarget != null && (_search.Length == 0 || (_search.Length < oldSearch.Length && oldSearch.StartsWith(_search))))
            {
                string displayedName;
                bool aliased = true;
                if (_boneAliases.TryGetValue(_boneTarget.name, out displayedName) == false)
                {
                    displayedName = _boneTarget.name;
                    aliased = false;
                }
                if (_boneTarget.name.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1 || (aliased && displayedName.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1))
                    OpenParents(_boneTarget.gameObject);
            }
            GUILayout.EndHorizontal();

            if (_search != oldSearch && _search.Length > 0)
                UpdateSearch();

            // Draw bone list scrollview
            _boneEditionScroll = GUILayout.BeginScrollView(_boneEditionScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            if (_onlyModified && _sortByName && _dirtyBones != null && _dirtyBones.Count > 0)
            {
                foreach (GameObject goKey in _dirtyBones.Keys)
                {
                    if (!_boneAliases.TryGetValue(goKey.name, out var value2))
                        value2 = goKey.name;

                    GUI.color = goKey.transform == _boneTarget ? Color.cyan : Color.magenta;
                    if (GUILayout.Button(value2 + "*", GUILayout.ExpandWidth(false)))
                        ChangeBoneTarget(goKey.transform);
                }
                GUI.color = Color.white;
            }
            else
            {
                foreach (Transform child in _parent.transform)
                    DisplayObjectTree(child.gameObject, 0);

                if (_goToObject && Event.current.rawType == EventType.Repaint)
                {
                    if (_boneTarget != null)
                        GoToObject(_boneTarget.gameObject);

                    _goToObject = false;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Alias", GUILayout.ExpandWidth(false));
            _currentAlias = GUILayout.TextField(_currentAlias, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
            {
                if (_boneTarget != null)
                {
                    _currentAlias = _currentAlias.Trim();
                    if (_currentAlias.Length == 0)
                    {
                        if (_boneAliases.ContainsKey(_boneTarget.name))
                            _boneAliases.Remove(_boneTarget.name);
                    }
                    else
                    {
                        if (_boneAliases.ContainsKey(_boneTarget.name) == false)
                            _boneAliases.Add(_boneTarget.name, _currentAlias);
                        else
                            _boneAliases[_boneTarget.name] = _currentAlias;
                    }
                }
                else
                    _currentAlias = "";
            }
            GUILayout.EndHorizontal();


            string text = _boneTarget == null ? "(no target)" : _boneTarget.name;
            GUILayout.BeginHorizontal();
            GUI.enabled = _boneTarget != null;
            if (GUILayout.Button(text))
            {
                GoToObject(_boneTarget?.gameObject);
            }
            GUI.enabled = _twinBoneTarget != null;
            if (GUILayout.Button("L\\R", GUILayout.Width(30f)))
            {
                GoToObject(_twinBoneTarget?.gameObject);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unfold modified"))
            {
                foreach (KeyValuePair<GameObject, TransformData> dirtyBone in _dirtyBones)
                {
                    OpenParents(dirtyBone.Key);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            bool flag2 = GUILayout.Toggle(_drawGizmos, " Gizmo");
            if (flag2 != _drawGizmos)
            {
                _drawGizmos = flag2;
                UpdateDebugLinesState(this);
            }
            GUI.enabled = _dirtyBones.Count > 0;
            bool flag3 = GUILayout.Toggle(_onlyModified, " Modified");
            if (flag3 && _dirtyBones.Count == 0)
            {
                flag3 = false;
            }
            if (flag3 != _onlyModified)
            {
                _onlyModified = flag3;
                if (flag3)
                {
                    foreach (GameObject key2 in _dirtyBones.Keys)
                    {
                        OpenParents(key2);
                    }
                }
                else if (_boneTarget != null)
                {
                    GoToObject(_boneTarget.gameObject);
                }
            }
            if (flag3)
            {
                bool flag4 = GUILayout.Toggle(_sortByName, " Sort");
                if (flag4 && _dirtyBones.Count == 0)
                {
                    flag4 = false;
                }
                if (flag4 != _sortByName)
                {
                    _sortByName = flag4;
                    if (_sortByName)
                    {
                        Dictionary<GameObject, TransformData> dictionary = _dirtyBones.OrderBy((KeyValuePair<GameObject, TransformData> x) => x.Key.name).ToDictionary((KeyValuePair<GameObject, TransformData> x) => x.Key, (KeyValuePair<GameObject, TransformData> x) => x.Value);
                        _dirtyBones.Clear();
                        foreach (KeyValuePair<GameObject, TransformData> item2 in dictionary)
                        {
                            _dirtyBones.Add(item2.Key, item2.Value);
                        }
                    }
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Toggle(false, " Sort");
            }
            GUI.enabled = true;
            bool flag5 = GUILayout.Toggle(_isWorld, " World Pos.");
            if (flag5 != _isWorld)
            {
                _isWorld = flag5;
                UpdateDebugLinesState(this);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Legend:");
            Color co = GUI.color;
            GUI.color = Color.cyan;
            GUILayout.Button("Selected");
            GUI.color = Color.magenta;
            GUILayout.Button("Changed*");
            GUI.color = CollidersEditor._colliderColor;
            GUILayout.Button("Collider");
            GUI.color = co;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(350f * ((MainWindow._self._advancedModeRect.width / 650f - 1f) / 2.7f + 1f)));
            {
                OCIChar.BoneInfo fkBoneInfo = null;
                if (_boneTarget != null && _target.fkEnabled)
                    _target.fkObjects.TryGetValue(_boneTarget.gameObject, out fkBoneInfo);
                OCIChar.BoneInfo fkTwinBoneInfo = null;
                if (_symmetricalEdition && _twinBoneTarget != null && _target.fkEnabled)
                    _target.fkObjects.TryGetValue(_twinBoneTarget.gameObject, out fkTwinBoneInfo);
                GUILayout.BeginHorizontal(GUI.skin.box);
                TransformData transformData = null;
                if (_boneTarget != null)
                    _dirtyBones.TryGetValue(_boneTarget.gameObject, out transformData);

                if (transformData != null && transformData.position.hasValue)
                    GUI.color = Color.magenta;
                if (_boneEditionCoordType == CoordType.Position)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Position" + (transformData != null && transformData.position.hasValue ? "*" : "")))
                    _boneEditionCoordType = CoordType.Position;
                GUI.color = co;

                if (transformData != null && transformData.rotation.hasValue)
                    GUI.color = Color.magenta;
                if (_boneEditionCoordType == CoordType.Rotation)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Rotation" + (transformData != null && transformData.rotation.hasValue ? "*" : "")))
                    _boneEditionCoordType = CoordType.Rotation;
                GUI.color = co;

                if (transformData != null && transformData.scale.hasValue)
                    GUI.color = Color.magenta;
                if (_boneEditionCoordType == CoordType.Scale)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Scale" + (transformData != null && transformData.scale.hasValue ? "*" : "")))
                    _boneEditionCoordType = CoordType.Scale;
                GUI.color = co;

                if (_boneEditionCoordType == CoordType.RotateAround)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Rotate Around"))
                    _boneEditionCoordType = CoordType.RotateAround;
                GUI.color = co;

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUI.enabled = _boneTarget != null;
                GUILayout.BeginVertical();
                bool shouldSaveValue;
                switch (_boneEditionCoordType)
                {
                    case CoordType.Position:
                        Vector3 boneTargetPosition2 = GetBoneTargetPosition();
                        if (_isWorld && _boneTarget != null && _boneTarget.parent != null)
                            boneTargetPosition2 = _boneTarget.parent.TransformPoint(boneTargetPosition2);

                        shouldSaveValue = false;
                        boneTargetPosition2 = Vector3Editor(boneTargetPosition2, _positionInc, "X:\t", "Y:\t", "Z:\t", () => shouldSaveValue = true);
                        if (shouldSaveValue && CanClickSpeedLimited())
                        {
                            if (_isWorld && _boneTarget != null && _boneTarget.parent != null)
                                boneTargetPosition2 = _boneTarget.parent.InverseTransformPoint(boneTargetPosition2);

                            SetBoneTargetPosition(boneTargetPosition2);
                        }

                        _lastShouldSaveValue = shouldSaveValue;
                        break;
                    case CoordType.Rotation:
                        Quaternion boneTargetRotation2 = GetBoneTargetRotation(fkBoneInfo);

                        // BUG: this seems to work and display correct rotation, but after clicking buttons in editor and converting back to local rotation, it has identical result on rotation as if _isWorld was false
                        if (_isWorld && _boneTarget != null && _boneTarget.parent != null)
                            boneTargetRotation2 = _boneTarget.parent.rotation * boneTargetRotation2;

                        shouldSaveValue = false;
                        boneTargetRotation2 = QuaternionEditor(boneTargetRotation2, _rotationInc, "X (Pitch):\t", "Y (Yaw):\t", "Z (Roll):\t", () => shouldSaveValue = true);
                        if (shouldSaveValue && CanClickSpeedLimited())
                        {
                            if (_isWorld && _boneTarget != null && _boneTarget.parent != null)
                                boneTargetRotation2 = Quaternion.Inverse(_boneTarget.parent.transform.rotation) * boneTargetRotation2;

                            SetBoneTargetRotation(boneTargetRotation2);
                            SetBoneTargetRotationFKNode(boneTargetRotation2, false, fkBoneInfo, fkTwinBoneInfo);

                            if (fkBoneInfo != null)
                                _parent.ExecuteDelayed(() => SetBoneTargetRotationFKNode(boneTargetRotation2, true, fkBoneInfo, fkTwinBoneInfo));

                            _lastShouldSaveValue = shouldSaveValue;
                        }

                        break;
                    case CoordType.Scale:
                        Vector3 boneTargetScale = GetBoneTargetScale();
                        shouldSaveValue = false;
                        boneTargetScale = Vector3Editor(boneTargetScale, _scaleInc, "X:\t", "Y:\t", "Z:\t", () => shouldSaveValue = true, true);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X/Y/Z");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.Button("0", GUILayout.Width(20f)))
                        {
                            shouldSaveValue = true;
                            boneTargetScale = Vector3.one;
                        }

                        if (GUILayout.RepeatButton((-_scaleInc).ToString("+0.#####;-0.#####")) && RepeatControl() && _boneTarget != null)
                        {
                            shouldSaveValue = true;
                            boneTargetScale -= _scaleInc * Vector3.one;
                        }

                        if (GUILayout.RepeatButton(_scaleInc.ToString("+0.#####;-0.#####")) && RepeatControl() && _boneTarget != null)
                        {
                            shouldSaveValue = true;
                            boneTargetScale += _scaleInc * Vector3.one;
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        if (shouldSaveValue && CanClickSpeedLimited())
                            SetBoneTargetScale(boneTargetScale);

                        _lastShouldSaveValue = shouldSaveValue;
                        break;
                    case CoordType.RotateAround:
                        shouldSaveValue = false;
                        Vector3 axis = Vector3.zero;
                        float angle = 0f;
                        Color c = GUI.color;
                        GUI.color = AdvancedModeModule._redColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X (Pitch)");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && _boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = _boneTarget.right;
                            if (RepeatControl())
                                angle = -_rotateAroundInc;
                        }

                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && _boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = _boneTarget.right;
                            if (RepeatControl())
                                angle = _rotateAroundInc;
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;
                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y (Yaw)");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && _boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = _boneTarget.up;
                            if (RepeatControl())
                                angle = -_rotateAroundInc;
                        }

                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && _boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = _boneTarget.up;
                            if (RepeatControl())
                                angle = _rotateAroundInc;
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;
                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z (Roll)");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && _boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = _boneTarget.forward;
                            if (RepeatControl())
                                angle = -_rotateAroundInc;
                        }

                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && _boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = _boneTarget.forward;
                            if (RepeatControl())
                                angle = _rotateAroundInc;
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        if (Event.current.rawType == EventType.Repaint && CanClickSpeedLimited())
                        {
                            if (_boneTarget != null)
                            {
                                if (shouldSaveValue)
                                {
                                    Quaternion currentRotation = GetBoneTargetRotation(fkBoneInfo);

                                    Vector3 currentPosition = GetBoneTargetPosition();

                                    _boneTarget.RotateAround(Studio.Studio.Instance.cameraCtrl.targetPos, axis, angle);

                                    if (fkBoneInfo != null)
                                    {
                                        if (_oldFKRotationValue == null)
                                            _oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                                        fkBoneInfo.guideObject.changeAmount.rot = _boneTarget.localEulerAngles;
                                    }

                                    TransformData td = SetBoneDirty(_boneTarget.gameObject);

                                    td.rotation = _boneTarget.localRotation;
                                    if (!td.originalRotation.hasValue)
                                        td.originalRotation = currentRotation;

                                    td.position = _boneTarget.localPosition;
                                    if (!td.originalPosition.hasValue)
                                        td.originalPosition = currentPosition;
                                }
                                else if (fkBoneInfo != null && _lastShouldSaveValue)
                                {
                                    GuideCommand.EqualsInfo[] infos = new GuideCommand.EqualsInfo[1];
                                    infos[0] = new GuideCommand.EqualsInfo()
                                    {
                                        dicKey = fkBoneInfo.guideObject.dicKey,
                                        oldValue = _oldFKRotationValue.Value,
                                        newValue = fkBoneInfo.guideObject.changeAmount.rot
                                    };
                                    UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos));
                                    _oldFKRotationValue = null;
                                }
                            }

                            _lastShouldSaveValue = shouldSaveValue;
                        }

                        break;
                }
                GUILayout.EndVertical();
                GUI.enabled = true;
                switch (_boneEditionCoordType)
                {
                    case CoordType.Position:
                        IncEditor(ref _positionIncIndex, out _positionInc);
                        break;
                    case CoordType.Rotation:
                        IncEditor(ref _rotationIncIndex, out _rotationInc);
                        break;
                    case CoordType.Scale:
                        IncEditor(ref _scaleIncIndex, out _scaleInc);
                        break;
                    case CoordType.RotateAround:
                        IncEditor(ref _rotateAroundIncIndex, out _rotateAroundInc);
                        break;
                }

                GUILayout.EndHorizontal();
                GUI.enabled = _boneTarget != null;
                if (_boneEditionCoordType != CoordType.RotateAround)
                {
                    GUILayout.BeginHorizontal();
                    _symmetricalEdition = GUILayout.Toggle(_symmetricalEdition, "Symmetrical");
                    GUILayout.FlexibleSpace();
                    switch (_boneEditionCoordType)
                    {
                        case CoordType.Position:
                            if (GUILayout.Button("Copy Position", GUILayout.ExpandWidth(false)) && _boneTarget != null)
                                _clipboardPosition = GetBoneTargetPosition();
                            bool guiEnabled = GUI.enabled;
                            GUI.enabled = _clipboardPosition != null;
                            if (GUILayout.Button("Paste Position", GUILayout.ExpandWidth(false)) && _clipboardPosition != null && _boneTarget != null)
                                SetBoneTargetPosition(_clipboardPosition.Value);
                            GUI.enabled = guiEnabled;
                            break;
                        case CoordType.Rotation:
                            if (GUILayout.Button("Copy Rotation", GUILayout.ExpandWidth(false)) && _boneTarget != null)
                                _clipboardRotation = GetBoneTargetRotation(fkBoneInfo);
                            guiEnabled = GUI.enabled;
                            GUI.enabled = _clipboardRotation != null;
                            if (GUILayout.Button("Paste Rotation", GUILayout.ExpandWidth(false)) && _clipboardRotation != null && _boneTarget != null)
                            {
                                SetBoneTargetRotation(_clipboardRotation.Value);
                                SetBoneTargetRotationFKNode(_clipboardRotation.Value, true, fkBoneInfo, fkTwinBoneInfo);
                            }
                            GUI.enabled = guiEnabled;
                            break;
                        case CoordType.Scale:
                            if (GUILayout.Button("Copy Scale", GUILayout.ExpandWidth(false)) && _boneTarget != null && _boneTarget != null)
                                _clipboardScale = GetBoneTargetScale();
                            guiEnabled = GUI.enabled;
                            GUI.enabled = _clipboardScale != null;
                            if (GUILayout.Button("Paste Scale", GUILayout.ExpandWidth(false)) && _clipboardScale != null && _boneTarget != null)
                                SetBoneTargetScale(_clipboardScale.Value);
                            GUI.enabled = guiEnabled;
                            break;
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Pos.") && _boneTarget != null)
                    ResetBonePos(_boneTarget, _twinBoneTarget, Event.current.control);
                if (GUILayout.Button("Reset Rot.") && _boneTarget != null)
                    ResetBoneRot(_boneTarget, _twinBoneTarget, Event.current.control);
                if (GUILayout.Button("Reset Scale") && _boneTarget != null)
                    ResetBoneScale(_boneTarget, _twinBoneTarget, Event.current.control);

                if (GUILayout.Button("Default") && _boneTarget != null)
                {
                    TransformData td = SetBoneDirty(_boneTarget.gameObject);
                    td.position = Vector3.zero;
                    if (!td.originalPosition.hasValue)
                        td.originalPosition = _boneTarget.localPosition;

                    Quaternion symmetricalRotation = Quaternion.identity;
                    if (fkBoneInfo != null)
                    {
                        if (_lastShouldSaveValue == false)
                            _oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                        fkBoneInfo.guideObject.changeAmount.rot = Vector3.zero;

                        if (_symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                        {
                            if (_lastShouldSaveValue == false)
                                _oldFKTwinRotationValue = fkTwinBoneInfo.guideObject.changeAmount.rot;
                            fkTwinBoneInfo.guideObject.changeAmount.rot = symmetricalRotation.eulerAngles;
                        }
                    }

                    td.rotation = Quaternion.identity;
                    if (!td.originalRotation.hasValue)
                        td.originalRotation = _boneTarget.localRotation;

                    td.scale = Vector3.one;
                    if (!td.originalScale.hasValue)
                        td.originalScale = _boneTarget.localScale;

                    if (_symmetricalEdition && _twinBoneTarget != null)
                    {
                        td = SetBoneDirty(_twinBoneTarget.gameObject);
                        td.position = Vector3.zero;
                        if (!td.originalPosition.hasValue)
                            td.originalPosition = _twinBoneTarget.localPosition;

                        td.rotation = symmetricalRotation;
                        if (!td.originalRotation.hasValue)
                            td.originalRotation = _twinBoneTarget.localRotation;

                        td.scale = Vector3.one;
                        if (!td.originalScale.hasValue)
                            td.originalScale = _twinBoneTarget.localScale;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Dirty Pos.") && _boneTarget != null)
                    MakeDirtyBonePos(_boneTarget, _twinBoneTarget, Event.current.control);
                if (GUILayout.Button("Dirty Rot.") && _boneTarget != null)
                {
                    if (fkBoneInfo == null || !fkBoneInfo.active)
                        MakeDirtyBoneRot(_boneTarget, _twinBoneTarget, Event.current.control);
                }
                if (GUILayout.Button("Dirty Scale") && _boneTarget != null)
                    MakeDirtyBoneScale(_boneTarget, _twinBoneTarget, Event.current.control);
                GUILayout.EndHorizontal();
                GUI.enabled = true;
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();

                Dictionary<string, string> customShortcuts = _target.type == GenericOCITarget.Type.Character ? (_target.isFemale ? _femaleShortcuts : _maleShortcuts) : _itemShortcuts;

                GUIStyle style = GUI.skin.GetStyle("Label");
                TextAnchor bak = style.alignment;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Shortcuts", style);
                style.alignment = bak;

                if (GUILayout.Button("+ Add Shortcut") && _boneTarget != null)
                {
                    string path = _boneTarget.GetPathFrom(_parent.transform);
                    if (path.Length != 0)
                    {
                        if (customShortcuts.ContainsKey(path) == false)
                            customShortcuts.Add(path, _boneTarget.name);
                    }
                    _removeShortcutMode = false;
                }

                Color color = GUI.color;
                if (_removeShortcutMode)
                    GUI.color = AdvancedModeModule._redColor;
                if (GUILayout.Button(_removeShortcutMode ? "Click on a shortcut" : "- Remove Shortcut"))
                    _removeShortcutMode = !_removeShortcutMode;
                GUI.color = color;

                GUILayout.EndHorizontal();

                _shortcutsScroll = GUILayout.BeginScrollView(_shortcutsScroll);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                int i = 0;
                int half = (_boneEditionShortcuts.Count + customShortcuts.Count(e => _parent.transform.Find(e.Key) != null)) / 2;
                foreach (KeyValuePair<Transform, string> kvp in _boneEditionShortcuts)
                {
                    if (i == half)
                    {
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                    }

                    if (GUILayout.Button(kvp.Value))
                        GoToObject(kvp.Key.gameObject);
                    ++i;
                }
                string toRemove = null;
                foreach (KeyValuePair<string, string> kvp in customShortcuts)
                {
                    Transform shortcut = _parent.transform.Find(kvp.Key);
                    if (shortcut == null)
                        continue;

                    if (i == half)
                    {
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                    }

                    string sName = kvp.Value;
                    string newName;
                    if (_boneAliases.TryGetValue(sName, out newName))
                        sName = newName;
                    if (GUILayout.Button(sName))
                    {
                        if (_removeShortcutMode)
                        {
                            toRemove = kvp.Key;
                            _removeShortcutMode = false;
                        }
                        else
                        {
                            GoToObject(shortcut.gameObject);
                        }
                    }
                    ++i;

                }
                if (toRemove != null)
                    customShortcuts.Remove(toRemove);

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.EndScrollView();

                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void ChangeBoneTarget(Transform newTarget)
        {
            if (ValidBoneTarget(newTarget))
            {
                _boneTarget = newTarget;
                _currentAlias = _boneAliases.ContainsKey(_boneTarget.name) ? _boneAliases[_boneTarget.name] : "";
                _twinBoneTarget = GetTwinBone(newTarget);
                if (_boneTarget == _twinBoneTarget)
                    _twinBoneTarget = null;
                UpdateDebugLinesState(this);
            }
            else
            {
                _boneTarget = null;
            }
        }

        private Vector3 GetBoneTargetPosition()
        {
            if (_boneTarget == null)
                return Vector3.zero;
            return GetBonePosition(_boneTarget);
        }

        private Vector3 GetBonePosition(Transform bone)
        {
            TransformData data;
            if (_dirtyBones.TryGetValue(bone.gameObject, out data) && data.position.hasValue)
                return data.position;
            return bone.localPosition;
        }

        private void SetBoneTargetPosition(Vector3 position)
        {
            if (_boneTarget != null)
            {
                SetBonePosition(_boneTarget, position);

                if (_symmetricalEdition && _twinBoneTarget != null)
                {
                    position.x *= -1f;
                    SetBonePosition(_twinBoneTarget, position);
                }
            }
        }

        private void SetBonePosition(Transform bone, Vector3 position)
        {
            TransformData td = SetBoneDirty(bone.gameObject);
            td.position = position;
            if (!td.originalPosition.hasValue)
                td.originalPosition = bone.localPosition;
        }

        private Quaternion GetBoneTargetRotation(OCIChar.BoneInfo fkBoneInfo)
        {
            if (_boneTarget == null)
                return Quaternion.identity;
            if (fkBoneInfo != null && fkBoneInfo.active)
                return fkBoneInfo.guideObject.transformTarget.localRotation;
            return GetBoneRotation(_boneTarget);
        }

        private Quaternion GetBoneRotation(Transform bone)
        {
            TransformData data;
            if (_dirtyBones.TryGetValue(bone.gameObject, out data) && data.rotation.hasValue)
                return data.rotation;
            return bone.localRotation;
        }

        private void SetBoneTargetRotation(Quaternion rotation)
        {
            if (_boneTarget != null)
            {
                SetBoneRotation(_boneTarget, rotation);

                if (_symmetricalEdition && _twinBoneTarget != null)
                    SetBoneRotation(_twinBoneTarget, new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w));
            }
        }

        private void SetBoneRotation(Transform bone, Quaternion rotation)
        {
            OCIChar.BoneInfo value = null;
            if (bone != null && _target.fkEnabled)
            {
                _target.fkObjects.TryGetValue(bone.gameObject, out value);
            }
            if (value == null || !value.active)
            {
                TransformData td = SetBoneDirty(bone.gameObject);
                td.rotation = rotation;
                if (!td.originalRotation.hasValue)
                    td.originalRotation = bone.localRotation;
            }
        }

        internal void SetBoneTargetRotationFKNode(Quaternion rotation, bool pushValue)
        {
            if (_boneTarget == null || !_target.fkEnabled)
            {
                return;
            }
            OCIChar.BoneInfo value = null;
            OCIChar.BoneInfo value2 = null;
            _target.fkObjects.TryGetValue(_boneTarget.gameObject, out value);
            if (value != null && value.active)
            {
                if (_twinBoneTarget != null && value != null && value.active)
                {
                    _target.fkObjects.TryGetValue(_twinBoneTarget.gameObject, out value2);
                }
                SetBoneTargetRotationFKNode(rotation, pushValue, value, value2);
            }
        }
        private void SetBoneTargetRotationFKNode(Quaternion rotation, bool pushValue, OCIChar.BoneInfo fkBoneInfo, OCIChar.BoneInfo fkTwinBoneInfo)
        {
            if (_boneTarget != null && fkBoneInfo != null)
            {
                if (_oldFKRotationValue == null)
                    _oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                fkBoneInfo.guideObject.changeAmount.rot = rotation.eulerAngles;

                if (_symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                {
                    if (_oldFKTwinRotationValue == null)
                        _oldFKTwinRotationValue = fkTwinBoneInfo.guideObject.changeAmount.rot;
                    fkTwinBoneInfo.guideObject.changeAmount.rot = new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w).eulerAngles;
                }

                if (pushValue)
                {
                    GuideCommand.EqualsInfo[] infos;
                    if (_symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                        infos = new GuideCommand.EqualsInfo[2];
                    else
                        infos = new GuideCommand.EqualsInfo[1];
                    infos[0] = new GuideCommand.EqualsInfo()
                    {
                        dicKey = fkBoneInfo.guideObject.dicKey,
                        oldValue = _oldFKRotationValue.Value,
                        newValue = fkBoneInfo.guideObject.changeAmount.rot
                    };
                    if (_symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                        infos[1] = new GuideCommand.EqualsInfo()
                        {
                            dicKey = fkTwinBoneInfo.guideObject.dicKey,
                            oldValue = _oldFKTwinRotationValue.Value,
                            newValue = fkTwinBoneInfo.guideObject.changeAmount.rot
                        };
                    UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos));
                    _oldFKRotationValue = null;
                    _oldFKTwinRotationValue = null;
                }
            }
        }

        private Vector3 GetBoneTargetScale()
        {
            if (_boneTarget == null)
                return Vector3.one;
            return GetBoneScale(_boneTarget);
        }

        private Vector3 GetBoneScale(Transform bone)
        {
            TransformData data;
            if (_dirtyBones.TryGetValue(_boneTarget.gameObject, out data) && data.scale.hasValue)
                return data.scale;
            return _boneTarget.localScale;
        }

        private void SetBoneTargetScale(Vector3 scale)
        {
            if (_boneTarget != null)
            {
                SetBoneScale(_boneTarget, scale);

                if (_symmetricalEdition && _twinBoneTarget != null)
                    SetBoneScale(_twinBoneTarget, scale);
            }
        }

        private void SetBoneScale(Transform bone, Vector3 scale)
        {
            TransformData td = SetBoneDirty(bone.gameObject);
            td.scale = scale;
            if (!td.originalScale.hasValue)
                td.originalScale = bone.localScale;
        }

        public void GoToObject(GameObject go)
        {
            if (ReferenceEquals(go, _parent.transform.gameObject))
                return;
            GameObject goBak = go;
            ChangeBoneTarget(go.transform);
            OpenParents(go);
            Vector2 scroll = new Vector2(0f, -GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) - 4);
            GetScrollPosition(_parent.transform.gameObject, goBak, 0, ref scroll);
            scroll.y -= GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) + 4;
            _boneEditionScroll = scroll;
        }

        public void LoadFrom(BonesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (GameObject openedBone in other._openedBones)
                {
                    Transform obj = _parent.transform.Find(openedBone.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                        _openedBones.Add(obj.gameObject);
                }
                foreach (KeyValuePair<GameObject, TransformData> kvp in other._dirtyBones)
                {
                    Transform obj = _parent.transform.Find(kvp.Key.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                        _dirtyBones.Add(obj.gameObject, new TransformData(kvp.Value));
                }
                _boneEditionScroll = other._boneEditionScroll;
                _shortcutsScroll = other._shortcutsScroll;
                _drawGizmos = other._drawGizmos;
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (_dirtyBones.Count != 0)
            {
                xmlWriter.WriteStartElement("advancedObjects");
                foreach (KeyValuePair<GameObject, TransformData> kvp in _dirtyBones)
                {
                    if (kvp.Key == null)
                        continue;
                    string n = kvp.Key.transform.GetPathFrom(_parent.transform);
                    xmlWriter.WriteStartElement("object");
                    xmlWriter.WriteAttributeString("name", n);

                    if (kvp.Value.position.hasValue)
                    {
                        xmlWriter.WriteAttributeString("posX", XmlConvert.ToString(kvp.Value.position.value.x));
                        xmlWriter.WriteAttributeString("posY", XmlConvert.ToString(kvp.Value.position.value.y));
                        xmlWriter.WriteAttributeString("posZ", XmlConvert.ToString(kvp.Value.position.value.z));
                    }
                    OCIChar.BoneInfo info;
                    if (kvp.Value.rotation.hasValue && (!_target.fkEnabled || !_target.fkObjects.TryGetValue(kvp.Key, out info) || info.active == false))
                    {
                        xmlWriter.WriteAttributeString("rotW", XmlConvert.ToString(kvp.Value.rotation.value.w));
                        xmlWriter.WriteAttributeString("rotX", XmlConvert.ToString(kvp.Value.rotation.value.x));
                        xmlWriter.WriteAttributeString("rotY", XmlConvert.ToString(kvp.Value.rotation.value.y));
                        xmlWriter.WriteAttributeString("rotZ", XmlConvert.ToString(kvp.Value.rotation.value.z));
                    }

                    if (kvp.Value.scale.hasValue)
                    {
                        xmlWriter.WriteAttributeString("scaleX", XmlConvert.ToString(kvp.Value.scale.value.x));
                        xmlWriter.WriteAttributeString("scaleY", XmlConvert.ToString(kvp.Value.scale.value.y));
                        xmlWriter.WriteAttributeString("scaleZ", XmlConvert.ToString(kvp.Value.scale.value.z));
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
            bool changed = false;
            ResetAll();
            XmlNode objects = xmlNode.FindChildNode("advancedObjects");

            if (objects != null)
            {
                foreach (XmlNode node in objects.ChildNodes)
                {
                    try
                    {
                        if (node.Name != "object")
                            continue;
                        string name = node.Attributes["name"].Value;
                        Transform obj = _parent.transform.Find(name);
                        if (obj == null)
                            continue;
                        TransformData data = new TransformData();
                        if (node.Attributes["posX"] != null && node.Attributes["posY"] != null && node.Attributes["posZ"] != null)
                        {
                            Vector3 pos;
                            pos.x = XmlConvert.ToSingle(node.Attributes["posX"].Value);
                            pos.y = XmlConvert.ToSingle(node.Attributes["posY"].Value);
                            pos.z = XmlConvert.ToSingle(node.Attributes["posZ"].Value);
                            data.position = pos;
                            data.originalPosition = obj.localPosition;
                        }
                        if (node.Attributes["rotW"] != null && node.Attributes["rotX"] != null && node.Attributes["rotY"] != null && node.Attributes["rotZ"] != null)
                        {
                            Quaternion rot;
                            rot.w = XmlConvert.ToSingle(node.Attributes["rotW"].Value);
                            rot.x = XmlConvert.ToSingle(node.Attributes["rotX"].Value);
                            rot.y = XmlConvert.ToSingle(node.Attributes["rotY"].Value);
                            rot.z = XmlConvert.ToSingle(node.Attributes["rotZ"].Value);
                            data.rotation = rot;
                            data.originalRotation = obj.localRotation;
                        }
                        if (node.Attributes["scaleX"] != null && node.Attributes["scaleY"] != null && node.Attributes["scaleZ"] != null)
                        {
                            Vector3 scale;
                            scale.x = XmlConvert.ToSingle(node.Attributes["scaleX"].Value);
                            scale.y = XmlConvert.ToSingle(node.Attributes["scaleY"].Value);
                            scale.z = XmlConvert.ToSingle(node.Attributes["scaleZ"].Value);
                            data.scale = scale;
                            data.originalScale = obj.localScale;
                        }

                        if (data.position.hasValue || data.rotation.hasValue || data.scale.hasValue)
                        {
                            changed = true;
                            _dirtyBones.Add(obj.gameObject, data);
                        }
                    }
                    catch (Exception e)
                    {
                        HSPE.Logger.LogError("Couldn't load bones for object " + _parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
                Dictionary<GameObject, TransformData> dictionary = _dirtyBones.OrderBy((KeyValuePair<GameObject, TransformData> x) => x.Key.name).ToDictionary((KeyValuePair<GameObject, TransformData> x) => x.Key, (KeyValuePair<GameObject, TransformData> x) => x.Value);
                _dirtyBones.Clear();
                foreach (KeyValuePair<GameObject, TransformData> item in dictionary)
                {
                    _dirtyBones.Add(item.Key, item.Value);
                }
            }

            return changed;
        }
        #endregion

        #region Private Methods
        private void ResetAll()
        {
            while (_dirtyBones.Count != 0)
            {
                KeyValuePair<GameObject, TransformData> pair = _dirtyBones.First();
                ResetBonePos(pair.Key.transform);
                ResetBoneRot(pair.Key.transform);
                ResetBoneScale(pair.Key.transform);
            }
        }

        private void ResetBonePos(Transform bone, Transform twinBone = null, bool withChildren = false, int depth = 0) // Depth is a safeguard because for some reason I saw people getting stackoverflows on this function
        {
            if (depth == 64)
                return;
            TransformData data;
            if (_dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.position.Reset();
                SetBoneNotDirtyIf(bone.gameObject);
            }
            if (_symmetricalEdition && twinBone != null && _dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.position.Reset();
                SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren && _dirtyBones.Count != 0)
            {
                foreach (Transform childBone in bone)
                {
                    Transform twinChildBone = GetTwinBone(childBone);
                    if (twinChildBone == childBone)
                        twinChildBone = null;
                    ResetBonePos(childBone, twinChildBone, true, depth + 1);
                }
            }
        }

        private void ResetBoneRot(Transform bone, Transform twinBone = null, bool withChildren = false, int depth = 0)
        {
            if (depth == 64)
                return;
            TransformData data;
            OCIChar.BoneInfo info;
            if ((!_target.fkEnabled || !_target.fkObjects.TryGetValue(bone.gameObject, out info) || !info.active) && _dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.rotation.Reset();
                SetBoneNotDirtyIf(bone.gameObject);
            }
            if (_symmetricalEdition && twinBone != null && (!_target.fkEnabled || !_target.fkObjects.TryGetValue(twinBone.gameObject, out info) || !info.active) && _dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.rotation.Reset();
                SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren && _dirtyBones.Count != 0)
            {
                foreach (Transform childBone in bone)
                {
                    Transform twinChildBone = GetTwinBone(childBone);
                    if (twinChildBone == childBone)
                        twinChildBone = null;
                    ResetBoneRot(childBone, twinChildBone, true, depth + 1);
                }
            }
        }

        private void ResetBoneScale(Transform bone, Transform twinBone = null, bool withChildren = false, int depth = 0)
        {
            if (depth == 64)
                return;
            TransformData data;
            if (_dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.scale.Reset();
                SetBoneNotDirtyIf(bone.gameObject);
            }
            if (_symmetricalEdition && twinBone != null && _dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.scale.Reset();
                SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren && _dirtyBones.Count != 0)
            {
                foreach (Transform childBone in bone)
                {
                    Transform twinChildBone = GetTwinBone(childBone);
                    if (twinChildBone == childBone)
                        twinChildBone = null;
                    ResetBoneScale(childBone, twinChildBone, true, depth + 1);
                }
            }
        }

        private void MakeDirtyBonePos(Transform bone, Transform twinBone, bool withChildren = false)
        {
            SetBonePosition(bone, GetBonePosition(bone));
            if (_symmetricalEdition && twinBone != null)
                SetBonePosition(twinBone, GetBonePosition(twinBone));
            if (withChildren)
            {
                for (int i = 0; i < bone.childCount; ++i)
                {
                    Transform child = bone.GetChild(i);
                    Transform twinChild = GetTwinBone(child);
                    if (twinChild == child)
                        twinChild = null;
                    MakeDirtyBonePos(child, twinChild, true);
                }
            }
        }

        private void MakeDirtyBoneRot(Transform bone, Transform twinBone, bool withChildren = false)
        {
            SetBoneRotation(bone, GetBoneRotation(bone));
            if (_symmetricalEdition && twinBone != null)
                SetBoneRotation(twinBone, GetBoneRotation(twinBone));
            if (withChildren)
            {
                for (int i = 0; i < bone.childCount; ++i)
                {
                    Transform child = bone.GetChild(i);
                    Transform twinChild = GetTwinBone(child);
                    if (twinChild == child)
                        twinChild = null;
                    MakeDirtyBoneRot(child, twinChild, true);
                }
            }
        }

        private void MakeDirtyBoneScale(Transform bone, Transform twinBone, bool withChildren = false)
        {
            SetBoneScale(bone, GetBoneScale(bone));
            if (_symmetricalEdition && twinBone != null)
                SetBoneScale(twinBone, GetBoneScale(twinBone));
            if (withChildren)
            {
                for (int i = 0; i < bone.childCount; ++i)
                {
                    Transform child = bone.GetChild(i);
                    Transform twinChild = GetTwinBone(child);
                    if (twinChild == child)
                        twinChild = null;
                    MakeDirtyBoneScale(child, twinChild, true);
                }
            }
        }
        private bool WildCardSearch(string text, string search)
        {
            string regex = "^.*" + Regex.Escape(search).Replace("\\?", ".").Replace("\\*", ".*") + ".*$";
            return Regex.IsMatch(text, regex, RegexOptions.IgnoreCase);
        }

        private void UpdateSearch()
        {
            searchResults = new List<GameObject>();
            void FilterBones(GameObject go)
            {
                if (_parent._childObjects.Contains(go))
                    return;
                string displayedName;
                bool aliased = true;
                if (_boneAliases.TryGetValue(go.name, out displayedName) == false)
                {
                    displayedName = go.name;
                    aliased = false;
                }

                if (_search.Length == 0 || WildCardSearch(go.name, _search) || (aliased && WildCardSearch(displayedName, _search)))
                    searchResults.Add(go);

                for (int i = 0; i < go.transform.childCount; ++i)
                    FilterBones(go.transform.GetChild(i).gameObject);
            }

            foreach (Transform child in _parent.transform)
            {
                FilterBones(child.gameObject);
            }
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            if (_parent._childObjects.Contains(go))
                return;

            if (_boneAliases.TryGetValue(go.name, out string displayedName) == false)
                displayedName = go.name;

            if (_search.Length == 0 || searchResults.Contains(go))
            {
                Color c = GUI.color;
                if (_dirtyBones.ContainsKey(go))
                    GUI.color = Color.magenta;
                if (_parent._collidersEditor._colliders.ContainsKey(go.transform))
                    GUI.color = CollidersEditor._colliderColor;
                if (_boneTarget == go.transform)
                    GUI.color = Color.cyan;

                if (_onlyModified)
                {
                    if (_dirtyBones.ContainsKey(go))
                    {
                        if (GUILayout.Button(displayedName + "*", GUILayout.ExpandWidth(false)))
                            ChangeBoneTarget(go.transform);
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (_search.Length == 0)
                    {
                        GUILayout.Space(indent * 20f);
                        int childCount = 0;
                        for (int i = 0; i < go.transform.childCount; i++)
                            if (_parent._childObjects.Contains(go.transform.GetChild(i).gameObject) == false)
                                childCount++;
                        if (childCount != 0)
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

                    if (GUILayout.Button(displayedName + (_dirtyBones.ContainsKey(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
                        ChangeBoneTarget(go.transform);
                    GUILayout.EndHorizontal();
                }

                GUI.color = c;
            }
            if (_search.Length != 0 || _openedBones.Contains(go))
            {
                for (int i = 0; i < go.transform.childCount; ++i)
                    DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
            }
        }

        private TransformData SetBoneDirty(GameObject go)
        {
            TransformData data;
            if (_dirtyBones.TryGetValue(go, out data) == false)
            {
                data = new TransformData();
                _dirtyBones.Add(go, data);
            }
            return data;
        }

        private void SetBoneNotDirtyIf(GameObject go)
        {
            if (_dirtyBones.ContainsKey(go))
            {
                TransformData data = _dirtyBones[go];
                if (data.position.hasValue == false && data.originalPosition.hasValue)
                {
                    go.transform.localPosition = data.originalPosition;
                    data.originalPosition.Reset();
                }
                if (data.rotation.hasValue == false && data.originalRotation.hasValue)
                {
                    go.transform.localRotation = data.originalRotation;
                    data.originalRotation.Reset();
                }
                if (data.scale.hasValue == false && data.originalScale.hasValue)
                {
                    go.transform.localScale = data.originalScale;
                    data.originalScale.Reset();
                }
                if (data.position.hasValue == false && data.rotation.hasValue == false && data.scale.hasValue == false)
                {
                    _dirtyBones.Remove(go);
                }
            }
        }

        private void OpenParents(GameObject child)
        {
            if (ReferenceEquals(child, _parent.transform.gameObject))
                return;
            child = child.transform.parent.gameObject;
            while (child.transform != _parent.transform)
            {
                _openedBones.Add(child);
                child = child.transform.parent.gameObject;
            }
            _openedBones.Add(child);
        }

        private bool GetScrollPosition(GameObject root, GameObject go, int indent, ref Vector2 scrollPosition)
        {
            if (_parent._childObjects.Contains(go))
                return false;
            scrollPosition = new Vector2(indent * 20f, scrollPosition.y + GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) + 4);
            if (ReferenceEquals(root, go))
                return true;
            if (_openedBones.Contains(root))
                for (int i = 0; i < root.transform.childCount; ++i)
                    if (GetScrollPosition(root.transform.GetChild(i).gameObject, go, indent + 1, ref scrollPosition))
                        return true;
            return false;
        }

        private void ApplyBoneManualCorrection()
        {
            if (_dirtyBones.Count == 0)
                return;
            bool shouldClean = false;
            foreach (KeyValuePair<GameObject, TransformData> kvp in _dirtyBones)
            {
                if (kvp.Key == null)
                {
                    shouldClean = true;
                    continue;
                }
                if (kvp.Value.scale.hasValue)
                    kvp.Key.transform.localScale = kvp.Value.scale;
                if (kvp.Value.rotation.hasValue)
                    kvp.Key.transform.localRotation = kvp.Value.rotation;
                if (kvp.Value.position.hasValue)
                    kvp.Key.transform.localPosition = kvp.Value.position;
            }
            if (shouldClean)
            {
                Dictionary<GameObject, TransformData> newDirtyBones = new Dictionary<GameObject, TransformData>();
                foreach (KeyValuePair<GameObject, TransformData> kvp in _dirtyBones)
                    if (kvp.Key != null)
                        newDirtyBones.Add(kvp.Key, kvp.Value);
                _dirtyBones = newDirtyBones;
            }
        }

        public override void UpdateGizmos()
        {
            if (GizmosEnabled() == false)
                return;

            Vector3 topLeftForward = _boneTarget.transform.position + (_boneTarget.up + -_boneTarget.right + _boneTarget.forward) * _cubeSize,
                topRightForward = _boneTarget.transform.position + (_boneTarget.up + _boneTarget.right + _boneTarget.forward) * _cubeSize,
                bottomLeftForward = _boneTarget.transform.position + (-_boneTarget.up + -_boneTarget.right + _boneTarget.forward) * _cubeSize,
                bottomRightForward = _boneTarget.transform.position + (-_boneTarget.up + _boneTarget.right + _boneTarget.forward) * _cubeSize,
                topLeftBack = _boneTarget.transform.position + (_boneTarget.up + -_boneTarget.right + -_boneTarget.forward) * _cubeSize,
                topRightBack = _boneTarget.transform.position + (_boneTarget.up + _boneTarget.right + -_boneTarget.forward) * _cubeSize,
                bottomLeftBack = _boneTarget.transform.position + (-_boneTarget.up + -_boneTarget.right + -_boneTarget.forward) * _cubeSize,
                bottomRightBack = _boneTarget.transform.position + (-_boneTarget.up + _boneTarget.right + -_boneTarget.forward) * _cubeSize;
            int i = 0;
            _cubeDebugLines[i++].SetPoints(topLeftForward, topRightForward);
            _cubeDebugLines[i++].SetPoints(topRightForward, bottomRightForward);
            _cubeDebugLines[i++].SetPoints(bottomRightForward, bottomLeftForward);
            _cubeDebugLines[i++].SetPoints(bottomLeftForward, topLeftForward);
            _cubeDebugLines[i++].SetPoints(topLeftBack, topRightBack);
            _cubeDebugLines[i++].SetPoints(topRightBack, bottomRightBack);
            _cubeDebugLines[i++].SetPoints(bottomRightBack, bottomLeftBack);
            _cubeDebugLines[i++].SetPoints(bottomLeftBack, topLeftBack);
            _cubeDebugLines[i++].SetPoints(topLeftBack, topLeftForward);
            _cubeDebugLines[i++].SetPoints(topRightBack, topRightForward);
            _cubeDebugLines[i++].SetPoints(bottomRightBack, bottomRightForward);
            _cubeDebugLines[i++].SetPoints(bottomLeftBack, bottomLeftForward);

            if (_isWorld)
            {
                _cubeDebugLines[i++].SetPoints(_boneTarget.transform.position, _boneTarget.transform.position + Vector3.right * _cubeSize * 2);
                _cubeDebugLines[i++].SetPoints(_boneTarget.transform.position, _boneTarget.transform.position + Vector3.up * _cubeSize * 2);
                _cubeDebugLines[i++].SetPoints(_boneTarget.transform.position, _boneTarget.transform.position + Vector3.forward * _cubeSize * 2);
            }
            else
            {
                _cubeDebugLines[i++].SetPoints(_boneTarget.transform.position, _boneTarget.transform.position + _boneTarget.right * _cubeSize * 2);
                _cubeDebugLines[i++].SetPoints(_boneTarget.transform.position, _boneTarget.transform.position + _boneTarget.up * _cubeSize * 2);
                _cubeDebugLines[i++].SetPoints(_boneTarget.transform.position, _boneTarget.transform.position + _boneTarget.forward * _cubeSize * 2);
            }

            foreach (VectorLine line in _cubeDebugLines)
                line.Draw();
        }

        private static void UpdateDebugLinesState(BonesEditor self)
        {
            bool e = self != null && self.GizmosEnabled();
            foreach (VectorLine line in _cubeDebugLines)
                line.active = e;
        }

        private bool GizmosEnabled()
        {
            return _isEnabled && PoseController._drawAdvancedMode && _drawGizmos && _boneTarget != null;
        }
        #endregion

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "bonePos",
                        name: "Bone Position",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<BonesEditor, Transform> pair = (HashedPair<BonesEditor, Transform>)parameter;
                            pair.key.SetBonePosition(pair.value, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((HashedPair<BonesEditor, Transform>)parameter).value.localPosition,
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"B Position ({((HashedPair<BonesEditor, Transform>)parameter).value.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "boneRot",
                        name: "Bone Rotation",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<BonesEditor, Transform> pair = (HashedPair<BonesEditor, Transform>)parameter;
                            pair.key.SetBoneRotation(pair.value, Quaternion.SlerpUnclamped((Quaternion)leftValue, (Quaternion)rightValue, factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((HashedPair<BonesEditor, Transform>)parameter).value.localRotation,
                        readValueFromXml: (parameter, node) => node.ReadQuaternion("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Quaternion)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"B Rotation ({((HashedPair<BonesEditor, Transform>)parameter).value.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "boneScale",
                        name: "Bone Scale",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<BonesEditor, Transform> pair = (HashedPair<BonesEditor, Transform>)parameter;
                            pair.key.SetBoneScale(pair.value, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((HashedPair<BonesEditor, Transform>)parameter).value.localScale,
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"B Scale ({((HashedPair<BonesEditor, Transform>)parameter).value.name})");
            }

            private static bool CheckIntegrity(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue)
            {
                if (parameter == null)
                    return false;
                HashedPair<BonesEditor, Transform> pair = (HashedPair<BonesEditor, Transform>)parameter;
                if (pair.key == null || pair.value == null)
                    return false;
                return true;
            }

            private static bool IsCompatibleWithTarget(ObjectCtrlInfo oci)
            {
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null;
            }

            private static object GetParameter(ObjectCtrlInfo oci)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new HashedPair<BonesEditor, Transform>(controller._bonesEditor, controller._bonesEditor._boneTarget);
            }

            private static object ReadParameterFromXml(ObjectCtrlInfo oci, XmlNode node)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new HashedPair<BonesEditor, Transform>(controller._bonesEditor, controller.transform.Find(node.Attributes["parameter"].Value));
            }

            private static void WriteParameterToXml(ObjectCtrlInfo oci, XmlTextWriter writer, object parameter)
            {
                writer.WriteAttributeString("parameter", ((HashedPair<BonesEditor, Transform>)parameter).value.GetPathFrom(oci.guideObject.transformTarget));
            }
        }
        #endregion

        private bool CanClickSpeedLimited()
        {
            if (_lastClickTime + _clickRepeatSeconds < Time.realtimeSinceStartup || _lastClickTime > Time.realtimeSinceStartup)
            {
                _lastClickTime = Time.realtimeSinceStartup;
                return true;
            }
            return false;
        }
        private float _lastClickTime;
        private float _clickRepeatSeconds = 0.03f;
    }
}