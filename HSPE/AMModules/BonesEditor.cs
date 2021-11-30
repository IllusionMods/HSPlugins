using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Studio;
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
                this.position = other.position;
                this.rotation = other.rotation;
                this.scale = other.scale;
                this.originalPosition = other.originalPosition;
                this.originalRotation = other.originalRotation;
                this.originalScale = other.originalScale;
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
        #endregion

        #region Constructor
        public BonesEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            this._target = target;
            this._parent.onLateUpdate += this.LateUpdate;
            this._parent.onDisable += this.OnDisable;
            if (_cubeDebugLines.Count == 0)
            {
                Vector3 topLeftForward = (Vector3.up + Vector3.left + Vector3.forward) * _cubeSize,
                    topRightForward = (Vector3.up + Vector3.right + Vector3.forward) * _cubeSize,
                    bottomLeftForward = ((Vector3.down + Vector3.left + Vector3.forward) * _cubeSize),
                    bottomRightForward = ((Vector3.down + Vector3.right + Vector3.forward) * _cubeSize),
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

            if (this._target.type == GenericOCITarget.Type.Character)
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
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_s_hand_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_s_hand_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_j_foot_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_j_foot_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_FaceBase"), "Face");
#elif AISHOUJO || HONEYSELECT2
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Hand_s_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Hand_s_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Foot02_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Foot02_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_FaceRoot"), "Face");
#endif
            }

            //this._parent.StartCoroutine(this.EndOfFrame());
        }
        #endregion

        #region Unity Methods
        private void LateUpdate()
        {
            if (this._target.type == GenericOCITarget.Type.Item)
                this.ApplyBoneManualCorrection();
        }

        private void OnDisable()
        {
            if (this._dirtyBones.Count == 0)
                return;
            foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
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
            this._parent.onLateUpdate -= this.LateUpdate;
            this._parent.onDisable -= this.OnDisable;
        }

        #endregion

        #region Public Methods
        public override void IKExecutionOrderOnPostLateUpdate()
        {
            this.ApplyBoneManualCorrection();
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
                return this._parent.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_R");
            if (bone.name.EndsWith("_R"))
                return this._parent.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_L");
            if (bone.parent.name.EndsWith("_L"))
            {
                Transform twinParent = this._parent.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_R");
                if (twinParent != null)
                {
                    int index = bone.GetSiblingIndex();
                    if (index < twinParent.childCount)
                        return twinParent.GetChild(index);
                }
            }
            if (bone.parent.name.EndsWith("_R"))
            {
                Transform twinParent = this._parent.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_L");
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
            if (oldSearch.Length != 0 && this._boneTarget != null && (_search.Length == 0 || (_search.Length < oldSearch.Length && oldSearch.StartsWith(_search))))
            {
                string displayedName;
                bool aliased = true;
                if (_boneAliases.TryGetValue(this._boneTarget.name, out displayedName) == false)
                {
                    displayedName = this._boneTarget.name;
                    aliased = false;
                }
                if (this._boneTarget.name.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1 || (aliased && displayedName.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1))
                    this.OpenParents(this._boneTarget.gameObject);
            }
            GUILayout.EndHorizontal();
            this._boneEditionScroll = GUILayout.BeginScrollView(this._boneEditionScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            foreach (Transform child in this._parent.transform)
            {
                this.DisplayObjectTree(child.gameObject, 0);
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Alias", GUILayout.ExpandWidth(false));
            this._currentAlias = GUILayout.TextField(this._currentAlias, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
            {
                if (this._boneTarget != null)
                {
                    this._currentAlias = this._currentAlias.Trim();
                    if (this._currentAlias.Length == 0)
                    {
                        if (_boneAliases.ContainsKey(this._boneTarget.name))
                            _boneAliases.Remove(this._boneTarget.name);
                    }
                    else
                    {
                        if (_boneAliases.ContainsKey(this._boneTarget.name) == false)
                            _boneAliases.Add(this._boneTarget.name, this._currentAlias);
                        else
                            _boneAliases[this._boneTarget.name] = this._currentAlias;
                    }
                }
                else
                    this._currentAlias = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool newDrawGizmos = GUILayout.Toggle(this._drawGizmos, "Draw Gizmos");
            if (newDrawGizmos != this._drawGizmos)
            {
                this._drawGizmos = newDrawGizmos;
                UpdateDebugLinesState(this);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Unfold modified"))
            {
                foreach (KeyValuePair<GameObject, TransformData> pair in this._dirtyBones)
                    this.OpenParents(pair.Key);
            }
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
                if (this._boneTarget != null && this._target.fkEnabled)
                    this._target.fkObjects.TryGetValue(this._boneTarget.gameObject, out fkBoneInfo);
                OCIChar.BoneInfo fkTwinBoneInfo = null;
                if (this._symmetricalEdition && this._twinBoneTarget != null && this._target.fkEnabled)
                    this._target.fkObjects.TryGetValue(this._twinBoneTarget.gameObject, out fkTwinBoneInfo);
                GUILayout.BeginHorizontal(GUI.skin.box);
                TransformData transformData = null;
                if (this._boneTarget != null)
                    this._dirtyBones.TryGetValue(this._boneTarget.gameObject, out transformData);

                if (transformData != null && transformData.position.hasValue)
                    GUI.color = Color.magenta;
                if (this._boneEditionCoordType == CoordType.Position)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Position" + (transformData != null && transformData.position.hasValue ? "*" : "")))
                    this._boneEditionCoordType = CoordType.Position;
                GUI.color = co;

                if (transformData != null && transformData.rotation.hasValue)
                    GUI.color = Color.magenta;
                if (this._boneEditionCoordType == CoordType.Rotation)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Rotation" + (transformData != null && transformData.rotation.hasValue ? "*" : "")))
                    this._boneEditionCoordType = CoordType.Rotation;
                GUI.color = co;

                if (transformData != null && transformData.scale.hasValue)
                    GUI.color = Color.magenta;
                if (this._boneEditionCoordType == CoordType.Scale)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Scale" + (transformData != null && transformData.scale.hasValue ? "*" : "")))
                    this._boneEditionCoordType = CoordType.Scale;
                GUI.color = co;

                if (this._boneEditionCoordType == CoordType.RotateAround)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Rotate Around"))
                    this._boneEditionCoordType = CoordType.RotateAround;
                GUI.color = co;

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                switch (this._boneEditionCoordType)
                {
                    case CoordType.Position:
                        Vector3 position = this.GetBoneTargetPosition();
                        bool shouldSaveValue = false;

                        position = this.Vector3Editor(position, _positionInc, onValueChanged: () => shouldSaveValue = true);

                        if (shouldSaveValue)
                            this.SetBoneTargetPosition(position);

                        this._lastShouldSaveValue = shouldSaveValue;
                        break;
                    case CoordType.Rotation:
                        Quaternion rotation = this.GetBoneTargetRotation(fkBoneInfo);
                        shouldSaveValue = false;

                        rotation = this.QuaternionEditor(rotation, _rotationInc, onValueChanged: () => shouldSaveValue = true);

                        if (shouldSaveValue)
                        {
                            this.SetBoneTargetRotation(rotation);
                            this.SetBoneTargetRotationFKNode(rotation, false, fkBoneInfo, fkTwinBoneInfo);
                        }
                        else if (this._lastShouldSaveValue)
                            this.SetBoneTargetRotationFKNode(rotation, true, fkBoneInfo, fkTwinBoneInfo);
                        this._lastShouldSaveValue = shouldSaveValue;

                        break;
                    case CoordType.Scale:
                        Vector3 scale = this.GetBoneTargetScale();
                        shouldSaveValue = false;

                        scale = this.Vector3Editor(scale, _scaleInc, onValueChanged: () => shouldSaveValue = true);

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X/Y/Z");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_scaleInc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= _scaleInc * Vector3.one;
                        }
                        if (GUILayout.RepeatButton(_scaleInc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += _scaleInc * Vector3.one;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();

                        if (shouldSaveValue)
                            this.SetBoneTargetScale(scale);

                        this._lastShouldSaveValue = shouldSaveValue;
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
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.right;
                            if (this.RepeatControl())
                                angle = -_rotateAroundInc;
                        }
                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.right;
                            if (this.RepeatControl())
                                angle = _rotateAroundInc;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y (Yaw)");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.up;
                            if (this.RepeatControl())
                                angle = -_rotateAroundInc;
                        }
                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.up;
                            if (this.RepeatControl())
                                angle = _rotateAroundInc;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z (Roll)");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.forward;
                            if (this.RepeatControl())
                                angle = -_rotateAroundInc;
                        }
                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.forward;
                            if (this.RepeatControl())
                                angle = _rotateAroundInc;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        if (Event.current.rawType == EventType.Repaint)
                        {
                            if (this._boneTarget != null)
                            {
                                if (shouldSaveValue)
                                {
                                    Quaternion currentRotation = this.GetBoneTargetRotation(fkBoneInfo);

                                    Vector3 currentPosition = this.GetBoneTargetPosition();

                                    this._boneTarget.RotateAround(Studio.Studio.Instance.cameraCtrl.targetPos, axis, angle);

                                    if (fkBoneInfo != null)
                                    {
                                        if (this._oldFKRotationValue == null)
                                            this._oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                                        fkBoneInfo.guideObject.changeAmount.rot = this._boneTarget.localEulerAngles;
                                    }

                                    TransformData td = this.SetBoneDirty(this._boneTarget.gameObject);

                                    td.rotation = this._boneTarget.localRotation;
                                    if (!td.originalRotation.hasValue)
                                        td.originalRotation = currentRotation;

                                    td.position = this._boneTarget.localPosition;
                                    if (!td.originalPosition.hasValue)
                                        td.originalPosition = currentPosition;
                                }
                                else if (fkBoneInfo != null && this._lastShouldSaveValue)
                                {
                                    GuideCommand.EqualsInfo[] infos = new GuideCommand.EqualsInfo[1];
                                    infos[0] = new GuideCommand.EqualsInfo()
                                    {
                                        dicKey = fkBoneInfo.guideObject.dicKey,
                                        oldValue = this._oldFKRotationValue.Value,
                                        newValue = fkBoneInfo.guideObject.changeAmount.rot
                                    };
                                    UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos));
                                    this._oldFKRotationValue = null;
                                }
                            }
                            this._lastShouldSaveValue = shouldSaveValue;
                        }

                        break;
                }
                GUILayout.EndVertical();
                switch (this._boneEditionCoordType)
                {
                    case CoordType.Position:
                        this.IncEditor(ref _positionIncIndex, out _positionInc);
                        break;
                    case CoordType.Rotation:
                        this.IncEditor(ref _rotationIncIndex, out _rotationInc);
                        break;
                    case CoordType.Scale:
                        this.IncEditor(ref _scaleIncIndex, out _scaleInc);
                        break;
                    case CoordType.RotateAround:
                        this.IncEditor(ref _rotateAroundIncIndex, out _rotateAroundInc);
                        break;
                }

                GUILayout.EndHorizontal();

                if (this._boneEditionCoordType != CoordType.RotateAround)
                {
                    GUILayout.BeginHorizontal();
                    this._symmetricalEdition = GUILayout.Toggle(this._symmetricalEdition, "Symmetrical");
                    GUILayout.FlexibleSpace();
                    switch (this._boneEditionCoordType)
                    {
                        case CoordType.Position:
                            if (GUILayout.Button("Copy Position", GUILayout.ExpandWidth(false)) && this._boneTarget != null)
                                _clipboardPosition = this.GetBoneTargetPosition();
                            bool guiEnabled = GUI.enabled;
                            GUI.enabled = _clipboardPosition != null;
                            if (GUILayout.Button("Paste Position", GUILayout.ExpandWidth(false)) && _clipboardPosition != null && this._boneTarget != null)
                                this.SetBoneTargetPosition(_clipboardPosition.Value);
                            GUI.enabled = guiEnabled;
                            break;
                        case CoordType.Rotation:
                            if (GUILayout.Button("Copy Rotation", GUILayout.ExpandWidth(false)) && this._boneTarget != null)
                                _clipboardRotation = this.GetBoneTargetRotation(fkBoneInfo);
                            guiEnabled = GUI.enabled;
                            GUI.enabled = _clipboardRotation != null;
                            if (GUILayout.Button("Paste Rotation", GUILayout.ExpandWidth(false)) && _clipboardRotation != null && this._boneTarget != null)
                            {
                                this.SetBoneTargetRotation(_clipboardRotation.Value);
                                this.SetBoneTargetRotationFKNode(_clipboardRotation.Value, true, fkBoneInfo, fkTwinBoneInfo);
                            }
                            GUI.enabled = guiEnabled;
                            break;
                        case CoordType.Scale:
                            if (GUILayout.Button("Copy Scale", GUILayout.ExpandWidth(false)) && this._boneTarget != null && this._boneTarget != null)
                                _clipboardScale = this.GetBoneTargetScale();
                            guiEnabled = GUI.enabled;
                            GUI.enabled = _clipboardScale != null;
                            if (GUILayout.Button("Paste Scale", GUILayout.ExpandWidth(false)) && _clipboardScale != null && this._boneTarget != null)
                                this.SetBoneTargetScale(_clipboardScale.Value);
                            GUI.enabled = guiEnabled;
                            break;
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Pos.") && this._boneTarget != null)
                    this.ResetBonePos(this._boneTarget, this._twinBoneTarget, Event.current.control);
                if ((fkBoneInfo == null || fkBoneInfo.active == false) && GUILayout.Button("Reset Rot.") && this._boneTarget != null)
                    this.ResetBoneRot(this._boneTarget, this._twinBoneTarget, Event.current.control);
                if (GUILayout.Button("Reset Scale") && this._boneTarget != null)
                    this.ResetBoneScale(this._boneTarget, this._twinBoneTarget, Event.current.control);

                if (GUILayout.Button("Default") && this._boneTarget != null)
                {
                    TransformData td = this.SetBoneDirty(this._boneTarget.gameObject);
                    td.position = Vector3.zero;
                    if (!td.originalPosition.hasValue)
                        td.originalPosition = this._boneTarget.localPosition;

                    Quaternion symmetricalRotation = Quaternion.identity;
                    if (fkBoneInfo != null)
                    {
                        if (this._lastShouldSaveValue == false)
                            this._oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                        fkBoneInfo.guideObject.changeAmount.rot = Vector3.zero;

                        if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                        {
                            if (this._lastShouldSaveValue == false)
                                this._oldFKTwinRotationValue = fkTwinBoneInfo.guideObject.changeAmount.rot;
                            fkTwinBoneInfo.guideObject.changeAmount.rot = symmetricalRotation.eulerAngles;
                        }
                    }

                    td.rotation = Quaternion.identity;
                    if (!td.originalRotation.hasValue)
                        td.originalRotation = this._boneTarget.localRotation;

                    td.scale = Vector3.one;
                    if (!td.originalScale.hasValue)
                        td.originalScale = this._boneTarget.localScale;

                    if (this._symmetricalEdition && this._twinBoneTarget != null)
                    {
                        td = this.SetBoneDirty(this._twinBoneTarget.gameObject);
                        td.position = Vector3.zero;
                        if (!td.originalPosition.hasValue)
                            td.originalPosition = this._twinBoneTarget.localPosition;

                        td.rotation = symmetricalRotation;
                        if (!td.originalRotation.hasValue)
                            td.originalRotation = this._twinBoneTarget.localRotation;

                        td.scale = Vector3.one;
                        if (!td.originalScale.hasValue)
                            td.originalScale = this._twinBoneTarget.localScale;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Dirty Pos.") && this._boneTarget != null)
                    this.MakeDirtyBonePos(this._boneTarget, this._twinBoneTarget, Event.current.control);
                if ((fkBoneInfo == null || fkBoneInfo.active == false) && GUILayout.Button("Dirty Rot.") && this._boneTarget != null)
                    this.MakeDirtyBoneRot(this._boneTarget, this._twinBoneTarget, Event.current.control);
                if (GUILayout.Button("Dirty Scale") && this._boneTarget != null)
                    this.MakeDirtyBoneScale(this._boneTarget, this._twinBoneTarget, Event.current.control);
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();

                Dictionary<string, string> customShortcuts = this._target.type == GenericOCITarget.Type.Character ? (this._target.isFemale ? _femaleShortcuts : _maleShortcuts) : _itemShortcuts;

                GUIStyle style = GUI.skin.GetStyle("Label");
                TextAnchor bak = style.alignment;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Shortcuts", style);
                style.alignment = bak;

                if (GUILayout.Button("+ Add Shortcut") && this._boneTarget != null)
                {
                    string path = this._boneTarget.GetPathFrom(this._parent.transform);
                    if (path.Length != 0)
                    {
                        if (customShortcuts.ContainsKey(path) == false)
                            customShortcuts.Add(path, this._boneTarget.name);
                    }
                    this._removeShortcutMode = false;
                }

                Color color = GUI.color;
                if (this._removeShortcutMode)
                    GUI.color = AdvancedModeModule._redColor;
                if (GUILayout.Button(this._removeShortcutMode ? "Click on a shortcut" : "- Remove Shortcut"))
                    this._removeShortcutMode = !this._removeShortcutMode;
                GUI.color = color;

                GUILayout.EndHorizontal();

                this._shortcutsScroll = GUILayout.BeginScrollView(this._shortcutsScroll);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                int i = 0;
                int half = (this._boneEditionShortcuts.Count + customShortcuts.Count(e => this._parent.transform.Find(e.Key) != null)) / 2;
                foreach (KeyValuePair<Transform, string> kvp in this._boneEditionShortcuts)
                {
                    if (i == half)
                    {
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                    }

                    if (GUILayout.Button(kvp.Value))
                        this.GoToObject(kvp.Key.gameObject);
                    ++i;
                }
                string toRemove = null;
                foreach (KeyValuePair<string, string> kvp in customShortcuts)
                {
                    Transform shortcut = this._parent.transform.Find(kvp.Key);
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
                        if (this._removeShortcutMode)
                        {
                            toRemove = kvp.Key;
                            this._removeShortcutMode = false;
                        }
                        else
                            this.GoToObject(shortcut.gameObject);
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
            this._boneTarget = newTarget;
            this._currentAlias = _boneAliases.ContainsKey(this._boneTarget.name) ? _boneAliases[this._boneTarget.name] : "";
            this._twinBoneTarget = this.GetTwinBone(newTarget);
            if (this._boneTarget == this._twinBoneTarget)
                this._twinBoneTarget = null;
            UpdateDebugLinesState(this);
        }

        private Vector3 GetBoneTargetPosition()
        {
            if (this._boneTarget == null)
                return Vector3.zero;
            return this.GetBonePosition(this._boneTarget);
        }

        private Vector3 GetBonePosition(Transform bone)
        {
            TransformData data;
            if (this._dirtyBones.TryGetValue(bone.gameObject, out data) && data.position.hasValue)
                return data.position;
            return bone.localPosition;
        }

        private void SetBoneTargetPosition(Vector3 position)
        {
            if (this._boneTarget != null)
            {
                this.SetBonePosition(this._boneTarget, position);

                if (this._symmetricalEdition && this._twinBoneTarget != null)
                {
                    position.x *= -1f;
                    this.SetBonePosition(this._twinBoneTarget, position);
                }
            }
        }

        private void SetBonePosition(Transform bone, Vector3 position)
        {
            TransformData td = this.SetBoneDirty(bone.gameObject);
            td.position = position;
            if (!td.originalPosition.hasValue)
                td.originalPosition = bone.localPosition;
        }

        private Quaternion GetBoneTargetRotation(OCIChar.BoneInfo fkBoneInfo)
        {
            if (this._boneTarget == null)
                return Quaternion.identity;
            if (fkBoneInfo != null && fkBoneInfo.active)
                return fkBoneInfo.guideObject.transformTarget.localRotation;
            return this.GetBoneRotation(this._boneTarget);
        }

        private Quaternion GetBoneRotation(Transform bone)
        {
            TransformData data;
            if (this._dirtyBones.TryGetValue(bone.gameObject, out data) && data.rotation.hasValue)
                return data.rotation;
            return bone.localRotation;
        }

        private void SetBoneTargetRotation(Quaternion rotation)
        {
            if (this._boneTarget != null)
            {
                this.SetBoneRotation(this._boneTarget, rotation);

                if (this._symmetricalEdition && this._twinBoneTarget != null)
                    this.SetBoneRotation(this._twinBoneTarget, new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w));
            }
        }

        private void SetBoneRotation(Transform bone, Quaternion rotation)
        {
            TransformData td = this.SetBoneDirty(bone.gameObject);
            td.rotation = rotation;
            if (!td.originalRotation.hasValue)
                td.originalRotation = bone.localRotation;
        }

        private void SetBoneTargetRotationFKNode(Quaternion rotation, bool pushValue, OCIChar.BoneInfo fkBoneInfo, OCIChar.BoneInfo fkTwinBoneInfo)
        {
            if (this._boneTarget != null && fkBoneInfo != null)
            {
                if (this._oldFKRotationValue == null)
                    this._oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                fkBoneInfo.guideObject.changeAmount.rot = rotation.eulerAngles;

                if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                {
                    if (this._oldFKTwinRotationValue == null)
                        this._oldFKTwinRotationValue = fkTwinBoneInfo.guideObject.changeAmount.rot;
                    fkTwinBoneInfo.guideObject.changeAmount.rot = new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w).eulerAngles;
                }

                if (pushValue)
                {
                    GuideCommand.EqualsInfo[] infos;
                    if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                        infos = new GuideCommand.EqualsInfo[2];
                    else
                        infos = new GuideCommand.EqualsInfo[1];
                    infos[0] = new GuideCommand.EqualsInfo()
                    {
                        dicKey = fkBoneInfo.guideObject.dicKey,
                        oldValue = this._oldFKRotationValue.Value,
                        newValue = fkBoneInfo.guideObject.changeAmount.rot
                    };
                    if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                        infos[1] = new GuideCommand.EqualsInfo()
                        {
                            dicKey = fkTwinBoneInfo.guideObject.dicKey,
                            oldValue = this._oldFKTwinRotationValue.Value,
                            newValue = fkTwinBoneInfo.guideObject.changeAmount.rot
                        };
                    UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos));
                    this._oldFKRotationValue = null;
                    this._oldFKTwinRotationValue = null;
                }
            }
        }

        private Vector3 GetBoneTargetScale()
        {
            if (this._boneTarget == null)
                return Vector3.one;
            return this.GetBoneScale(this._boneTarget);
        }

        private Vector3 GetBoneScale(Transform bone)
        {
            TransformData data;
            if (this._dirtyBones.TryGetValue(this._boneTarget.gameObject, out data) && data.scale.hasValue)
                return data.scale;
            return this._boneTarget.localScale;
        }

        private void SetBoneTargetScale(Vector3 scale)
        {
            if (this._boneTarget != null)
            {
                this.SetBoneScale(this._boneTarget, scale);

                if (this._symmetricalEdition && this._twinBoneTarget != null)
                    this.SetBoneScale(this._twinBoneTarget, scale);
            }
        }

        private void SetBoneScale(Transform bone, Vector3 scale)
        {
            TransformData td = this.SetBoneDirty(bone.gameObject);
            td.scale = scale;
            if (!td.originalScale.hasValue)
                td.originalScale = bone.localScale;
        }

        public void GoToObject(GameObject go)
        {
            if (ReferenceEquals(go, this._parent.transform.gameObject))
                return;
            GameObject goBak = go;
            this.ChangeBoneTarget(go.transform);
            this.OpenParents(go);
            Vector2 scroll = new Vector2(0f, -GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) - 4);
            this.GetScrollPosition(this._parent.transform.gameObject, goBak, 0, ref scroll);
            scroll.y -= GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) + 4;
            this._boneEditionScroll = scroll;
        }

        public void LoadFrom(BonesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (GameObject openedBone in other._openedBones)
                {
                    Transform obj = this._parent.transform.Find(openedBone.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                        this._openedBones.Add(obj.gameObject);
                }
                foreach (KeyValuePair<GameObject, TransformData> kvp in other._dirtyBones)
                {
                    Transform obj = this._parent.transform.Find(kvp.Key.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                        this._dirtyBones.Add(obj.gameObject, new TransformData(kvp.Value));
                }
                this._boneEditionScroll = other._boneEditionScroll;
                this._shortcutsScroll = other._shortcutsScroll;
                this._drawGizmos = other._drawGizmos;
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this._dirtyBones.Count != 0)
            {
                xmlWriter.WriteStartElement("advancedObjects");
                foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
                {
                    if (kvp.Key == null)
                        continue;
                    string n = kvp.Key.transform.GetPathFrom(this._parent.transform);
                    xmlWriter.WriteStartElement("object");
                    xmlWriter.WriteAttributeString("name", n);

                    if (kvp.Value.position.hasValue)
                    {
                        xmlWriter.WriteAttributeString("posX", XmlConvert.ToString(kvp.Value.position.value.x));
                        xmlWriter.WriteAttributeString("posY", XmlConvert.ToString(kvp.Value.position.value.y));
                        xmlWriter.WriteAttributeString("posZ", XmlConvert.ToString(kvp.Value.position.value.z));
                    }
                    OCIChar.BoneInfo info;
                    if (kvp.Value.rotation.hasValue && (!this._target.fkEnabled || !this._target.fkObjects.TryGetValue(kvp.Key, out info) || info.active == false))
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
            this.ResetAll();
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
                        Transform obj = this._parent.transform.Find(name);
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
                            this._dirtyBones.Add(obj.gameObject, data);
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load bones for object " + this._parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }

            return changed;
        }
        #endregion

        #region Private Methods
        private void ResetAll()
        {
            while (this._dirtyBones.Count != 0)
            {
                KeyValuePair<GameObject, TransformData> pair = this._dirtyBones.First();
                this.ResetBonePos(pair.Key.transform);
                this.ResetBoneRot(pair.Key.transform);
                this.ResetBoneScale(pair.Key.transform);
            }
        }

        private void ResetBonePos(Transform bone, Transform twinBone = null, bool withChildren = false, int depth = 0) // Depth is a safeguard because for some reason I saw people getting stackoverflows on this function
        {
            if (depth == 64)
                return;
            TransformData data;
            if (this._dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.position.Reset();
                this.SetBoneNotDirtyIf(bone.gameObject);
            }
            if (this._symmetricalEdition && twinBone != null && this._dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.position.Reset();
                this.SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren && this._dirtyBones.Count != 0)
            {
                foreach (Transform childBone in bone)
                {
                    Transform twinChildBone = this.GetTwinBone(childBone);
                    if (twinChildBone == childBone)
                        twinChildBone = null;
                    this.ResetBonePos(childBone, twinChildBone, true, depth + 1);
                }
            }
        }

        private void ResetBoneRot(Transform bone, Transform twinBone = null, bool withChildren = false, int depth = 0)
        {
            if (depth == 64)
                return;
            TransformData data;
            OCIChar.BoneInfo info;
            if ((!this._target.fkEnabled || !this._target.fkObjects.TryGetValue(bone.gameObject, out info) || !info.active) && this._dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.rotation.Reset();
                this.SetBoneNotDirtyIf(bone.gameObject);
            }
            if (this._symmetricalEdition && twinBone != null && (!this._target.fkEnabled || !this._target.fkObjects.TryGetValue(twinBone.gameObject, out info) || !info.active) && this._dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.rotation.Reset();
                this.SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren && this._dirtyBones.Count != 0)
            {
                foreach (Transform childBone in bone)
                {
                    Transform twinChildBone = this.GetTwinBone(childBone);
                    if (twinChildBone == childBone)
                        twinChildBone = null;
                    this.ResetBoneRot(childBone, twinChildBone, true, depth + 1);
                }
            }
        }

        private void ResetBoneScale(Transform bone, Transform twinBone = null, bool withChildren = false, int depth = 0)
        {
            if (depth == 64)
                return;
            TransformData data;
            if (this._dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.scale.Reset();
                this.SetBoneNotDirtyIf(bone.gameObject);
            }
            if (this._symmetricalEdition && twinBone != null && this._dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.scale.Reset();
                this.SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren && this._dirtyBones.Count != 0)
            {
                foreach (Transform childBone in bone)
                {
                    Transform twinChildBone = this.GetTwinBone(childBone);
                    if (twinChildBone == childBone)
                        twinChildBone = null;
                    this.ResetBoneScale(childBone, twinChildBone, true, depth + 1);
                }
            }
        }

        private void MakeDirtyBonePos(Transform bone, Transform twinBone, bool withChildren = false)
        {
            this.SetBonePosition(bone, this.GetBonePosition(bone));
            if (this._symmetricalEdition && twinBone != null)
                this.SetBonePosition(twinBone, this.GetBonePosition(twinBone));
            if (withChildren)
            {
                for (int i = 0; i < bone.childCount; ++i)
                {
                    Transform child = bone.GetChild(i);
                    Transform twinChild = this.GetTwinBone(child);
                    if (twinChild == child)
                        twinChild = null;
                    this.MakeDirtyBonePos(child, twinChild, true);
                }
            }
        }

        private void MakeDirtyBoneRot(Transform bone, Transform twinBone, bool withChildren = false)
        {
            this.SetBoneRotation(bone, this.GetBoneRotation(bone));
            if (this._symmetricalEdition && twinBone != null)
                this.SetBoneRotation(twinBone, this.GetBoneRotation(twinBone));
            if (withChildren)
            {
                for (int i = 0; i < bone.childCount; ++i)
                {
                    Transform child = bone.GetChild(i);
                    Transform twinChild = this.GetTwinBone(child);
                    if (twinChild == child)
                        twinChild = null;
                    this.MakeDirtyBoneRot(child, twinChild, true);
                }
            }
        }

        private void MakeDirtyBoneScale(Transform bone, Transform twinBone, bool withChildren = false)
        {
            this.SetBoneScale(bone, this.GetBoneScale(bone));
            if (this._symmetricalEdition && twinBone != null)
                this.SetBoneScale(twinBone, this.GetBoneScale(twinBone));
            if (withChildren)
            {
                for (int i = 0; i < bone.childCount; ++i)
                {
                    Transform child = bone.GetChild(i);
                    Transform twinChild = this.GetTwinBone(child);
                    if (twinChild == child)
                        twinChild = null;
                    this.MakeDirtyBoneScale(child, twinChild, true);
                }
            }
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            if (this._parent._childObjects.Contains(go))
                return;
            string displayedName;
            bool aliased = true;
            if (_boneAliases.TryGetValue(go.name, out displayedName) == false)
            {
                displayedName = go.name;
                aliased = false;
            }

            if (_search.Length == 0 || go.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) != -1 || (aliased && displayedName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) != -1))
            {
                Color c = GUI.color;
                if (this._dirtyBones.ContainsKey(go))
                    GUI.color = Color.magenta;
                if (this._parent._collidersEditor._colliders.ContainsKey(go.transform))
                    GUI.color = CollidersEditor._colliderColor;
                if (this._boneTarget == go.transform)
                    GUI.color = Color.cyan;
                GUILayout.BeginHorizontal();
                if (_search.Length == 0)
                {
                    GUILayout.Space(indent * 20f);
                    int childCount = 0;
                    for (int i = 0; i < go.transform.childCount; ++i)
                        if (this._parent._childObjects.Contains(go.transform.GetChild(i).gameObject) == false)
                            ++childCount;
                    if (childCount != 0)
                    {
                        if (GUILayout.Toggle(this._openedBones.Contains(go), "", GUILayout.ExpandWidth(false)))
                        {
                            if (this._openedBones.Contains(go) == false)
                                this._openedBones.Add(go);
                        }
                        else
                        {
                            if (this._openedBones.Contains(go))
                                this._openedBones.Remove(go);
                        }
                    }
                    else
                        GUILayout.Space(20f);
                }
                if (GUILayout.Button(displayedName + (this._dirtyBones.ContainsKey(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
                {
                    this.ChangeBoneTarget(go.transform);
                }
                GUI.color = c;
                GUILayout.EndHorizontal();
            }
            if (_search.Length != 0 || this._openedBones.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        private TransformData SetBoneDirty(GameObject go)
        {
            TransformData data;
            if (this._dirtyBones.TryGetValue(go, out data) == false)
            {
                data = new TransformData();
                this._dirtyBones.Add(go, data);
            }
            return data;
        }

        private void SetBoneNotDirtyIf(GameObject go)
        {
            if (this._dirtyBones.ContainsKey(go))
            {
                TransformData data = this._dirtyBones[go];
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
                    this._dirtyBones.Remove(go);
                }
            }
        }

        private void OpenParents(GameObject child)
        {
            if (ReferenceEquals(child, this._parent.transform.gameObject))
                return;
            child = child.transform.parent.gameObject;
            while (child.transform != this._parent.transform)
            {
                this._openedBones.Add(child);
                child = child.transform.parent.gameObject;
            }
            this._openedBones.Add(child);
        }

        private bool GetScrollPosition(GameObject root, GameObject go, int indent, ref Vector2 scrollPosition)
        {
            if (this._parent._childObjects.Contains(go))
                return false;
            scrollPosition = new Vector2(indent * 20f, scrollPosition.y + GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) + 4);
            if (ReferenceEquals(root, go))
                return true;
            if (this._openedBones.Contains(root))
                for (int i = 0; i < root.transform.childCount; ++i)
                    if (this.GetScrollPosition(root.transform.GetChild(i).gameObject, go, indent + 1, ref scrollPosition))
                        return true;
            return false;
        }

        private void ApplyBoneManualCorrection()
        {
            if (this._dirtyBones.Count == 0)
                return;
            bool shouldClean = false;
            foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
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
                foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
                    if (kvp.Key != null)
                        newDirtyBones.Add(kvp.Key, kvp.Value);
                this._dirtyBones = newDirtyBones;
            }
        }

        public override void UpdateGizmos()
        {
            if (this.GizmosEnabled() == false)
                return;

            Vector3 topLeftForward = this._boneTarget.transform.position + (this._boneTarget.up + -this._boneTarget.right + this._boneTarget.forward) * _cubeSize,
                topRightForward = this._boneTarget.transform.position + (this._boneTarget.up + this._boneTarget.right + this._boneTarget.forward) * _cubeSize,
                bottomLeftForward = this._boneTarget.transform.position + ((-this._boneTarget.up + -this._boneTarget.right + this._boneTarget.forward) * _cubeSize),
                bottomRightForward = this._boneTarget.transform.position + ((-this._boneTarget.up + this._boneTarget.right + this._boneTarget.forward) * _cubeSize),
                topLeftBack = this._boneTarget.transform.position + (this._boneTarget.up + -this._boneTarget.right + -this._boneTarget.forward) * _cubeSize,
                topRightBack = this._boneTarget.transform.position + (this._boneTarget.up + this._boneTarget.right + -this._boneTarget.forward) * _cubeSize,
                bottomLeftBack = this._boneTarget.transform.position + (-this._boneTarget.up + -this._boneTarget.right + -this._boneTarget.forward) * _cubeSize,
                bottomRightBack = this._boneTarget.transform.position + (-this._boneTarget.up + this._boneTarget.right + -this._boneTarget.forward) * _cubeSize;
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

            _cubeDebugLines[i++].SetPoints(this._boneTarget.transform.position, this._boneTarget.transform.position + this._boneTarget.right * _cubeSize * 2);
            _cubeDebugLines[i++].SetPoints(this._boneTarget.transform.position, this._boneTarget.transform.position + this._boneTarget.up * _cubeSize * 2);
            _cubeDebugLines[i++].SetPoints(this._boneTarget.transform.position, this._boneTarget.transform.position + this._boneTarget.forward * _cubeSize * 2);

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
            return this._isEnabled && PoseController._drawAdvancedMode && this._drawGizmos && this._boneTarget != null;
        }
        #endregion

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
    }
}