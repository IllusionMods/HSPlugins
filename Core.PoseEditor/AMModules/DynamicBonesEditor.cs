using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Studio;
using ToolBox.Extensions;
using UnityEngine;
using Vectrosity;
#if HONEYSELECT || PLAYHOME || KOIKATSU
using DynamicBoneColliderBase = DynamicBoneCollider;
#endif

namespace HSPE.AMModules
{
    public class DynamicBonesEditor : AdvancedModeModule
    {
        #region Constants
#if HONEYSELECT || KOIKATSU || PLAYHOME
        private const float _dynamicBonesDragRadiusBase = 0.025f;
#elif AISHOUJO || HONEYSELECT2
        private const float _dynamicBonesDragRadiusBase = 0.25f;
#endif
        #endregion

        #region Private Types
        private class DynamicBoneData
        {
            public EditableValue<bool> originalEnabled;
            public bool currentEnabled;
            public EditableValue<Vector3> originalGravity;
            public Vector3 currentGravity;
            public EditableValue<Vector3> originalForce;
            public Vector3 currentForce;
#if AISHOUJO || HONEYSELECT2
            public EditableValue<DynamicBone.UpdateMode> originalUpdateMode;
            public DynamicBone.UpdateMode currentUpdateMode;
#endif
            public EditableValue<DynamicBone.FreezeAxis> originalFreezeAxis;
            public DynamicBone.FreezeAxis currentFreezeAxis;
            public EditableValue<float> originalWeight;
            public float currentWeight;
            public EditableValue<float> originalDamping;
            public float currentDamping;
            public EditableValue<float> originalElasticity;
            public float currentElasticity;
            public EditableValue<float> originalStiffness;
            public float currentStiffness;
            public EditableValue<float> originalInert;
            public float currentInert;
            public EditableValue<float> originalRadius;
            public float currentRadius;

            public DynamicBoneData()
            {
            }

            public DynamicBoneData(DynamicBoneData other)
            {
                originalEnabled = other.originalEnabled;
                originalWeight = other.originalWeight;
                originalGravity = other.originalGravity;
                originalForce = other.originalForce;
#if AISHOUJO || HONEYSELECT2
                this.originalUpdateMode = other.originalUpdateMode;
#endif
                originalFreezeAxis = other.originalFreezeAxis;
                originalDamping = other.originalDamping;
                originalElasticity = other.originalElasticity;
                originalStiffness = other.originalStiffness;
                originalInert = other.originalInert;
                originalRadius = other.originalRadius;

                currentEnabled = other.currentEnabled;
                currentWeight = other.currentWeight;
                currentGravity = other.currentGravity;
                currentForce = other.currentForce;
#if AISHOUJO || HONEYSELECT2
                this.currentUpdateMode= other.currentUpdateMode;
#endif
                currentFreezeAxis = other.currentFreezeAxis;
                currentDamping = other.currentDamping;
                currentElasticity = other.currentElasticity;
                currentStiffness = other.currentStiffness;
                currentInert = other.currentInert;
                currentRadius = other.currentRadius;
            }
        }

        private class DebugLines
        {
            private readonly List<DebugDynamicBone> _debugLines = new List<DebugDynamicBone>();

            public void Draw(List<DynamicBone> dynamicBones, DynamicBone target, Dictionary<DynamicBone, DynamicBoneData> dirtyDynamicBones)
            {
                int i = 0;
                foreach (DynamicBone db in dynamicBones)
                {
                    if (db.m_Root == null)
                        continue;
                    DebugDynamicBone debug;
                    if (i < _debugLines.Count)
                    {
                        debug = _debugLines[i];
                        debug.SetActive(true);
                    }
                    else
                    {
                        debug = new DebugDynamicBone(db);
                        _debugLines.Add(debug);
                    }
                    debug.SetActive(_showAllDebugDB || db == target);
                    debug.Draw(db, db == target, dirtyDynamicBones.ContainsKey(db));
                    ++i;
                }
                for (; i < _debugLines.Count; ++i)
                {
                    DebugDynamicBone debug = _debugLines[i];
                    if (debug.IsActive())
                        debug.SetActive(false);
                }
            }

            public void Destroy()
            {
                if (_debugLines != null)
                    for (int i = 0; i < _debugLines.Count; i++)
                    {
                        _debugLines[i].Destroy();
                    }
            }

            public void SetActive(bool active, int limit = -1)
            {
                if (_debugLines == null)
                    return;
                if (limit == -1)
                    limit = _debugLines.Count;
                for (int i = 0; i < limit; i++)
                {
                    _debugLines[i].SetActive(active);
                }
            }
        }

        private class DebugDynamicBone
        {
            private VectorLine _gravity;
            private VectorLine _force;
            private VectorLine _both;
            private VectorLine _circle;

            public DebugDynamicBone(DynamicBone db)
            {
                Transform end = (db.m_Root ?? db.transform).GetFirstLeaf();

                float scale = 10f;
                Vector3 origin = end.position;
                Vector3 final = origin + (db.m_Gravity) * scale;

                _gravity = VectorLine.SetLine(_redColor, origin, final);
                _gravity.endCap = "vector";
                _gravity.lineWidth = 4f;

                origin = final;
                final += db.m_Force * scale;

                _force = VectorLine.SetLine(_blueColor, origin, final);
                _force.endCap = "vector";
                _force.lineWidth = 4f;

                origin = end.position;

                _both = VectorLine.SetLine(_greenColor, origin, final);
                _both.endCap = "vector";
                _both.lineWidth = 4f;

                _circle = VectorLine.SetLine(_greenColor, new Vector3[37]);
                _circle.lineWidth = 4f;
                _circle.MakeCircle(end.position, Camera.main.transform.forward, _dynamicBonesDragRadius);
            }

            public void Draw(DynamicBone db, bool isTarget, bool isDirty)
            {
                Transform end = (db.m_Root ?? db.transform).GetFirstLeaf();

                float scale = 10f;
                Vector3 origin = end.position;
                Vector3 final = origin + (db.m_Gravity) * scale;

                _gravity.points3[0] = origin;
                _gravity.points3[1] = final;

                origin = final;
                final += db.m_Force * scale;

                _force.points3[0] = origin;
                _force.points3[1] = final;

                origin = end.position;

                Color c;
                if (isTarget)
                    c = Color.cyan;
                else if (isDirty)
                    c = Color.magenta;
                else
                    c = _greenColor;

                _both.points3[0] = origin;
                _both.points3[1] = final;

                _circle.MakeCircle(end.position, Camera.main.transform.forward, _dynamicBonesDragRadius);

                float distance = 1 - (Mathf.Clamp((end.position - Camera.main.transform.position).sqrMagnitude, 5, 20) - 5) / 15;
                if (distance < 0.2f)
                    distance = 0.2f;
                c.a = distance;

                _both.color = c;
                _circle.color = c;
                Color gravityColor = _gravity.color;
                gravityColor.a = distance;
                _gravity.color = gravityColor;
                Color forceColor = _force.color;
                forceColor.a = distance;
                _force.color = forceColor;

                _gravity.Draw();
                _force.Draw();
                _both.Draw();
                _circle.Draw();
            }

            public void Destroy()
            {
                VectorLine.Destroy(ref _gravity);
                VectorLine.Destroy(ref _force);
                VectorLine.Destroy(ref _both);
                VectorLine.Destroy(ref _circle);
            }

            public void SetActive(bool active)
            {
                _gravity.active = active;
                _force.active = active;
                _both.active = active;
                _circle.active = active;
            }

            public bool IsActive()
            {
                return _gravity.active;
            }
        }
        #endregion

        #region Private Variables
        private Vector2 _dynamicBonesScroll;
        private DynamicBone _dynamicBoneTarget;
        internal readonly List<DynamicBone> _dynamicBones = new List<DynamicBone>();
        private readonly Dictionary<DynamicBone, DynamicBoneData> _dirtyDynamicBones = new Dictionary<DynamicBone, DynamicBoneData>();
        private Vector3 _dragDynamicBoneStartPosition;
        private Vector3 _dragDynamicBoneEndPosition;
        private Vector3 _lastDynamicBoneGravity;
        private DynamicBone _draggedDynamicBone;
        private static DebugLines _debugLines;
        private bool _firstRefresh;
        private readonly GenericOCITarget _target;
        private Vector2 _dynamicBonesScroll2;
        private readonly Dictionary<Transform, DynamicBoneData> _headlessDirtyDynamicBones = new Dictionary<Transform, DynamicBoneData>();
        private int _headlessReconstructionTimeout;
        private static readonly string[] _freezeAxisNames;
#if AISHOUJO || HONEYSELECT2
        private static readonly string[] _updateModeNames;
#endif
        private readonly HashSet<DynamicBone> _toUpdate = new HashSet<DynamicBone>();
        private Action _preDragAction = null;
        private static DynamicBoneData _copiedDynamicBoneData = null;
        private bool _isBusy = false;
        private static float _dynamicBonesDragRadius = _dynamicBonesDragRadiusBase;
        internal static bool _showAllDebugDB = true;
        #endregion

        #region Public Fields
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.DynamicBonesEditor; } }
        public override string displayName { get { return "Dynamic Bones"; } }
        public bool isDraggingDynamicBone { get; private set; }
        public override bool isEnabled
        {
            set
            {
                base.isEnabled = value;
                UpdateDebugLinesState(this);
            }
        }
        public override bool shouldDisplay { get { return _dynamicBones.Count(b => b.m_Root != null) > 0; } }
        #endregion

        #region Unity Methods
        static DynamicBonesEditor()
        {
            _freezeAxisNames = Enum.GetNames(typeof(DynamicBone.FreezeAxis));
#if AISHOUJO || HONEYSELECT2
            _updateModeNames = Enum.GetNames(typeof(DynamicBone.UpdateMode));
#endif
        }

        public DynamicBonesEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            _target = target;
            _parent.onUpdate += Update;
            _parent.onLateUpdate += LateUpdate;
            if (_debugLines == null)
                _debugLines = new DebugLines();
#if HONEYSELECT
            if (this._target.type == GenericOCITarget.Type.Character && this._target.ociChar.charInfo.Sex == 1)
            {
                this._parent.ExecuteDelayed(() =>
                {
                    DynamicBone leftButtCheek = this._parent.gameObject.AddComponent<DynamicBone>();
                    leftButtCheek.m_Root = this._parent.transform.FindDescendant("cf_J_SiriDam01_L");
                    leftButtCheek.m_Damping = 0.1f;
                    leftButtCheek.m_DampingDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    //this._leftButtCheek.m_Elasticity = 0.3f;
                    leftButtCheek.m_Elasticity = 0.06f;
                    leftButtCheek.m_ElasticityDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    //this._leftButtCheek.m_Stiffness = 0.65f;
                    leftButtCheek.m_Stiffness = 0.06f;
                    leftButtCheek.m_StiffnessDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    leftButtCheek.m_Radius = 0.0003f;
                    leftButtCheek.m_RadiusDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    leftButtCheek.m_Colliders = new List<DynamicBoneColliderBase>();
                    leftButtCheek.m_Exclusions = new List<Transform>();
                    leftButtCheek.m_notRolls = new List<Transform>();

                    DynamicBone rightButtCheek = this._parent.gameObject.AddComponent<DynamicBone>();
                    rightButtCheek.m_Root = this._parent.transform.FindDescendant("cf_J_SiriDam01_R");
                    rightButtCheek.m_Damping = 0.1f;
                    rightButtCheek.m_DampingDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    //this._rightButtCheek.m_Elasticity = 0.3f;
                    rightButtCheek.m_Elasticity = 0.06f;
                    rightButtCheek.m_ElasticityDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    //this._rightButtCheek.m_Stiffness = 0.65f;
                    rightButtCheek.m_Stiffness = 0.06f;
                    rightButtCheek.m_StiffnessDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    rightButtCheek.m_Radius = 0.0003f;
                    rightButtCheek.m_RadiusDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    rightButtCheek.m_Colliders = new List<DynamicBoneColliderBase>();
                    rightButtCheek.m_Exclusions = new List<Transform>();
                    rightButtCheek.m_notRolls = new List<Transform>();
                }, 2);
            }
#endif
            RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed2(RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed2(RefreshDynamicBoneList, 3);
            _incIndex = -3;
        }

        private void Update()
        {
            if (!_firstRefresh)
            {
                RefreshDynamicBoneList();
                _firstRefresh = true;
            }
            if (_toUpdate.Count != 0)
            {
                foreach (DynamicBone dynamicBone in _toUpdate)
                {
                    if (dynamicBone == null)
                        continue;
                    UpdateDynamicBone(dynamicBone);
                }
                _toUpdate.Clear();
            }
        }

        private void LateUpdate()
        {
            if (_headlessReconstructionTimeout >= 0)
            {
                _headlessReconstructionTimeout--;
                foreach (KeyValuePair<Transform, DynamicBoneData> pair in _headlessDirtyDynamicBones.ToList())
                {
                    if (pair.Key == null)
                        continue;
                    foreach (DynamicBone db in _dynamicBones)
                    {
                        if (db != null && _dirtyDynamicBones.ContainsKey(db) == false && (db.m_Root == pair.Key || db.transform == pair.Key))
                        {
                            _dirtyDynamicBones.Add(db, pair.Value);

                            if (pair.Value.originalEnabled.hasValue)
                            {
                                pair.Value.originalEnabled = db.enabled;
                                db.enabled = pair.Value.currentEnabled;
                            }
                            if (pair.Value.originalWeight.hasValue)
                            {
                                pair.Value.originalWeight = db.GetWeight();
                                db.SetWeight(pair.Value.currentWeight);
                            }
                            if (pair.Value.originalGravity.hasValue)
                            {
                                pair.Value.originalGravity = db.m_Gravity;
                                db.m_Gravity = pair.Value.currentGravity;
                            }
                            if (pair.Value.originalForce.hasValue)
                            {
                                pair.Value.originalForce = db.m_Force;
                                db.m_Force = pair.Value.currentForce;
                            }
#if AISHOUJO
                            if (pair.Value.originalUpdateMode.hasValue)
                            {
                                pair.Value.originalUpdateMode = db.m_UpdateMode;
                                db.m_UpdateMode = pair.Value.currentUpdateMode;
                            }
#endif
                            if (pair.Value.originalFreezeAxis.hasValue)
                            {
                                pair.Value.originalFreezeAxis = db.m_FreezeAxis;
                                db.m_FreezeAxis = pair.Value.currentFreezeAxis;
                            }
                            if (pair.Value.originalDamping.hasValue)
                            {
                                pair.Value.originalDamping = db.m_Damping;
                                db.m_Damping = pair.Value.currentDamping;
                            }
                            if (pair.Value.originalElasticity.hasValue)
                            {
                                pair.Value.originalElasticity = db.m_Elasticity;
                                db.m_Elasticity = pair.Value.currentElasticity;
                            }
                            if (pair.Value.originalStiffness.hasValue)
                            {
                                pair.Value.originalStiffness = db.m_Stiffness;
                                db.m_Stiffness = pair.Value.currentStiffness;
                            }
                            if (pair.Value.originalInert.hasValue)
                            {
                                pair.Value.originalInert = db.m_Inert;
                                db.m_Inert = pair.Value.currentInert;
                            }
                            if (pair.Value.originalRadius.hasValue)
                            {
                                pair.Value.originalRadius = db.m_Radius;
                                db.m_Radius = pair.Value.currentRadius;
                            }
                            _headlessDirtyDynamicBones.Remove(pair.Key);
                            NotifyDynamicBoneForUpdate(db);
                            break;
                        }
                    }
                }
            }
            else if (_headlessDirtyDynamicBones.Count != 0)
                _headlessDirtyDynamicBones.Clear();
            else
                _isBusy = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _parent.onUpdate -= Update;
            _parent.onLateUpdate -= LateUpdate;
        }

        #endregion

        #region Public Methods
        public override void OnCharacterReplaced()
        {
            RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(RefreshDynamicBoneList, 2);
        }

        public override void OnLoadClothesFile()
        {
            RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(RefreshDynamicBoneList, 2);
        }

#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        public override void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force)
#elif KOIKATSU
        public override void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force)
#endif
        {
            RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(RefreshDynamicBoneList, 2);
        }
#endif

        //public override void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        //{
        //    MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 10);
        //}

        public override void DrawAdvancedModeChanged()
        {
            UpdateDebugLinesState(this);
        }

        public static void SelectionChanged(DynamicBonesEditor self)
        {
            UpdateDebugLinesState(self);
        }

        public override void GUILogic()
        {
            Color c = GUI.color;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            _dynamicBonesScroll = GUILayout.BeginScrollView(_dynamicBonesScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            foreach (DynamicBone db in _dynamicBones)
            {
                if (db.m_Root == null)
                    continue;
                if (IsDynamicBoneDirty(db))
                    GUI.color = Color.magenta;
                if (ReferenceEquals(db, _dynamicBoneTarget))
                    GUI.color = Color.cyan;
                string dName = db.m_Root.name;
                string newName;
                if (BonesEditor._boneAliases.TryGetValue(dName, out newName))
                    dName = newName;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(dName + (IsDynamicBoneDirty(db) ? "*" : "")))
                    _dynamicBoneTarget = db;
                GUILayout.Space(GUI.skin.verticalScrollbar.fixedWidth);
                GUILayout.EndHorizontal();
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            _showAllDebugDB = GUILayout.Toggle(_showAllDebugDB, _showAllDebugDB ? "◀ All Gizmos ▶" : "◀ Current Gizmo ▶", GUI.skin.button, new GUILayoutOption[0]);
            if (GUILayout.Button("Copy to FK"))
                CopyToFK();
            if (GUILayout.Button("Force refresh list"))
                RefreshDynamicBoneList();

            {
                GUI.color = Color.red;
                if (GUILayout.Button("Reset all") && _dynamicBoneTarget != null)
                    ResetAll();
                GUI.color = c;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

            _dynamicBonesScroll2 = GUILayout.BeginScrollView(_dynamicBonesScroll2, false, true);

            {
                GUILayout.BeginHorizontal();
                bool e = false;
                if (_dynamicBoneTarget != null)
                    e = _dynamicBoneTarget.enabled;
                e = GUILayout.Toggle(e, "Enabled");
                if (_dynamicBoneTarget != null && _dynamicBoneTarget.enabled != e)
                    SetDynamicBoneEnabled(_dynamicBoneTarget, e);
                GUILayout.FlexibleSpace();
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && _dynamicBoneTarget != null && IsDynamicBoneDirty(_dynamicBoneTarget))
                {
                    DynamicBoneData data = _dirtyDynamicBones[_dynamicBoneTarget];
                    _dynamicBoneTarget.enabled = data.originalEnabled;
                    data.currentEnabled = data.originalEnabled;
                    data.originalEnabled.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();
            }

            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Gravity");
                GUILayout.FlexibleSpace();
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && _dynamicBoneTarget != null && IsDynamicBoneDirty(_dynamicBoneTarget))
                {
                    DynamicBoneData data = _dirtyDynamicBones[_dynamicBoneTarget];
                    _dynamicBoneTarget.m_Gravity = data.originalGravity;
                    data.currentGravity = data.originalGravity;
                    data.originalGravity.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                Vector3 g = Vector3.zero;
                if (_dynamicBoneTarget != null)
                    g = _dynamicBoneTarget.m_Gravity;
                g = Vector3Editor(g, _redColor);
                if (_dynamicBoneTarget != null && _dynamicBoneTarget.m_Gravity != g)
                    SetDynamicBoneGravity(_dynamicBoneTarget, g);
                GUILayout.EndVertical();
            }

            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Force");
                GUILayout.FlexibleSpace();
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && _dynamicBoneTarget != null && IsDynamicBoneDirty(_dynamicBoneTarget))
                {
                    DynamicBoneData data = _dirtyDynamicBones[_dynamicBoneTarget];
                    _dynamicBoneTarget.m_Force = data.originalForce;
                    data.currentForce = data.originalForce;
                    data.originalForce.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                Vector3 f = Vector3.zero;
                if (_dynamicBoneTarget != null)
                    f = _dynamicBoneTarget.m_Force;
                f = Vector3Editor(f, _blueColor);
                if (_dynamicBoneTarget != null && _dynamicBoneTarget.m_Force != f)
                    SetDynamicBoneForce(_dynamicBoneTarget, f);

                GUILayout.EndVertical();
            }

#if AISHOUJO || HONEYSELECT2
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("UpdateMode\t", GUILayout.ExpandWidth(false));

                DynamicBone.UpdateMode um = DynamicBone.UpdateMode.Normal;
                if (this._dynamicBoneTarget != null)
                    um = this._dynamicBoneTarget.m_UpdateMode;
                um = (DynamicBone.UpdateMode)GUILayout.SelectionGrid((int)um, _updateModeNames, 2);

                if (this._dynamicBoneTarget != null && this._dynamicBoneTarget.m_UpdateMode != um)
                    this.SetDynamicBoneUpdateMode(this._dynamicBoneTarget, um);

                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_UpdateMode = data.originalUpdateMode;
                    data.currentUpdateMode = data.originalUpdateMode;
                    data.originalUpdateMode.Reset();
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }
#endif

            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("FreezeAxis\t", GUILayout.ExpandWidth(false));

                DynamicBone.FreezeAxis fa = DynamicBone.FreezeAxis.None;
                if (_dynamicBoneTarget != null)
                    fa = _dynamicBoneTarget.m_FreezeAxis;
                fa = (DynamicBone.FreezeAxis)GUILayout.SelectionGrid((int)fa, _freezeAxisNames, 4);

                if (_dynamicBoneTarget != null && _dynamicBoneTarget.m_FreezeAxis != fa)
                    SetDynamicBoneFreezeAxis(_dynamicBoneTarget, fa);

                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && _dynamicBoneTarget != null && IsDynamicBoneDirty(_dynamicBoneTarget))
                {
                    DynamicBoneData data = _dirtyDynamicBones[_dynamicBoneTarget];
                    _dynamicBoneTarget.m_FreezeAxis = data.originalFreezeAxis;
                    data.currentFreezeAxis = data.originalFreezeAxis;
                    data.originalFreezeAxis.Reset();
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }

            {
                float w = 0f;
                if (_dynamicBoneTarget != null)
                    w = _dynamicBoneTarget.GetWeight();

                w = FloatEditor(w, 0f, 1f, "Weight\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (_dynamicBoneTarget != null && _dirtyDynamicBones.TryGetValue(_dynamicBoneTarget, out data) && data.originalWeight.hasValue)
                    {
                        _dynamicBoneTarget.SetWeight(data.originalWeight);
                        data.currentWeight = data.originalWeight;
                        data.originalWeight.Reset();
                        return data.currentWeight;
                    }
                    return value;
                });

                if (_dynamicBoneTarget != null && !Mathf.Approximately(_dynamicBoneTarget.GetWeight(), w))
                    SetDynamicBoneWeight(_dynamicBoneTarget, w);

            }

            {
                float v = 0f;
                if (_dynamicBoneTarget != null)
                    v = _dynamicBoneTarget.m_Damping;

                v = FloatEditor(v, 0f, 1f, "Damping\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (_dynamicBoneTarget != null && _dirtyDynamicBones.TryGetValue(_dynamicBoneTarget, out data) && data.originalDamping.hasValue)
                    {
                        _dynamicBoneTarget.m_Damping = data.originalDamping;
                        data.currentDamping = data.originalDamping;
                        data.originalDamping.Reset();
                        NotifyDynamicBoneForUpdate(_dynamicBoneTarget);
                        return data.currentDamping;
                    }
                    return value;
                });
                if (_dynamicBoneTarget != null && !Mathf.Approximately(_dynamicBoneTarget.m_Damping, v))
                    SetDynamicBoneDamping(_dynamicBoneTarget, v);
            }

            {
                float v = 0f;
                if (_dynamicBoneTarget != null)
                    v = _dynamicBoneTarget.m_Elasticity;

                v = FloatEditor(v, 0f, 1f, "Elasticity\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (_dynamicBoneTarget != null && _dirtyDynamicBones.TryGetValue(_dynamicBoneTarget, out data) && data.originalElasticity.hasValue)
                    {
                        _dynamicBoneTarget.m_Elasticity = data.originalElasticity;
                        data.currentElasticity = data.originalElasticity;
                        data.originalElasticity.Reset();
                        NotifyDynamicBoneForUpdate(_dynamicBoneTarget);
                        return data.currentElasticity;
                    }
                    return value;
                });
                if (_dynamicBoneTarget != null && !Mathf.Approximately(_dynamicBoneTarget.m_Elasticity, v))
                    SetDynamicBoneElasticity(_dynamicBoneTarget, v);
            }

            {
                float v = 0f;
                if (_dynamicBoneTarget != null)
                    v = _dynamicBoneTarget.m_Stiffness;

                v = FloatEditor(v, 0f, 1f, "Stiffness\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (_dynamicBoneTarget != null && _dirtyDynamicBones.TryGetValue(_dynamicBoneTarget, out data) && data.originalStiffness.hasValue)
                    {
                        _dynamicBoneTarget.m_Stiffness = data.originalStiffness;
                        data.currentStiffness = data.originalStiffness;
                        data.originalStiffness.Reset();
                        NotifyDynamicBoneForUpdate(_dynamicBoneTarget);
                        return data.currentStiffness;
                    }
                    return value;
                });
                if (_dynamicBoneTarget != null && !Mathf.Approximately(_dynamicBoneTarget.m_Stiffness, v))
                    SetDynamicBoneStiffness(_dynamicBoneTarget, v);
            }

            {
                float v = 0f;
                if (_dynamicBoneTarget != null)
                    v = _dynamicBoneTarget.m_Inert;

                v = FloatEditor(v, 0f, 1f, "Inertia\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (_dynamicBoneTarget != null && _dirtyDynamicBones.TryGetValue(_dynamicBoneTarget, out data) && data.originalInert.hasValue)
                    {
                        _dynamicBoneTarget.m_Inert = data.originalInert;
                        data.currentInert = data.originalInert;
                        data.originalInert.Reset();
                        NotifyDynamicBoneForUpdate(_dynamicBoneTarget);
                        return data.currentInert;
                    }
                    return value;
                });
                if (_dynamicBoneTarget != null && !Mathf.Approximately(_dynamicBoneTarget.m_Inert, v))
                    SetDynamicBoneInertia(_dynamicBoneTarget, v);
            }

            {
                float v = 0f;
                if (_dynamicBoneTarget != null)
                    v = _dynamicBoneTarget.m_Radius;

                v = FloatEditor(v, 0f, 1f, "Radius\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (_dynamicBoneTarget != null && _dirtyDynamicBones.TryGetValue(_dynamicBoneTarget, out data) && data.originalRadius.hasValue)
                    {
                        _dynamicBoneTarget.m_Radius = data.originalRadius;
                        data.currentRadius = data.originalRadius;
                        data.originalRadius.Reset();
                        NotifyDynamicBoneForUpdate(_dynamicBoneTarget);
                        return data.currentRadius;
                    }
                    return value;
                });
                if (_dynamicBoneTarget != null && !Mathf.Approximately(_dynamicBoneTarget.m_Radius, v))
                    SetDynamicBoneRadius(_dynamicBoneTarget, v);
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Go to root", GUILayout.ExpandWidth(false)) && _dynamicBoneTarget != null)
            {
                _parent.EnableModule(_parent._bonesEditor);
                _parent._bonesEditor.GoToObject(_dynamicBoneTarget.m_Root.gameObject);
            }

            if (GUILayout.Button("Go to tail", GUILayout.ExpandWidth(false)) && _dynamicBoneTarget != null)
            {
                _parent.EnableModule(_parent._bonesEditor);
                _parent._bonesEditor.GoToObject(_dynamicBoneTarget.m_Root.GetDeepestLeaf().gameObject);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Copy Settings", GUILayout.ExpandWidth(false)) && _dynamicBoneTarget != null)
            {
                _preDragAction = () =>
                {
                    _copiedDynamicBoneData = new DynamicBoneData();
                    _copiedDynamicBoneData.currentEnabled = _dynamicBoneTarget.enabled;
                    _copiedDynamicBoneData.currentWeight = _dynamicBoneTarget.GetWeight();
                    _copiedDynamicBoneData.currentGravity = _dynamicBoneTarget.m_Gravity;
                    _copiedDynamicBoneData.currentForce = _dynamicBoneTarget.m_Force;
#if AISHOUJO || HONEYSELECT2
                    _copiedDynamicBoneData.currentUpdateMode = this._dynamicBoneTarget.m_UpdateMode;
#endif
                    _copiedDynamicBoneData.currentFreezeAxis = _dynamicBoneTarget.m_FreezeAxis;
                    _copiedDynamicBoneData.currentDamping = _dynamicBoneTarget.m_Damping;
                    _copiedDynamicBoneData.currentElasticity = _dynamicBoneTarget.m_Elasticity;
                    _copiedDynamicBoneData.currentStiffness = _dynamicBoneTarget.m_Stiffness;
                    _copiedDynamicBoneData.currentInert = _dynamicBoneTarget.m_Inert;
                    _copiedDynamicBoneData.currentRadius = _dynamicBoneTarget.m_Radius;

                };
            }

            if (GUILayout.Button("Paste Settings", GUILayout.ExpandWidth(false)) && _copiedDynamicBoneData != null && _dynamicBoneTarget != null)
            {
                _preDragAction = () =>
                {
                    if (_dynamicBoneTarget.enabled != _copiedDynamicBoneData.currentEnabled)
                        SetDynamicBoneEnabled(_dynamicBoneTarget, _copiedDynamicBoneData.currentEnabled);
                    if (_dynamicBoneTarget.GetWeight() != _copiedDynamicBoneData.currentWeight)
                        SetDynamicBoneWeight(_dynamicBoneTarget, _copiedDynamicBoneData.currentWeight);
                    if (_dynamicBoneTarget.m_Gravity != _copiedDynamicBoneData.currentGravity)
                        SetDynamicBoneGravity(_dynamicBoneTarget, _copiedDynamicBoneData.currentGravity);
                    if (_dynamicBoneTarget.m_Force != _copiedDynamicBoneData.currentForce)
                        SetDynamicBoneForce(_dynamicBoneTarget, _copiedDynamicBoneData.currentForce);
#if AISHOUJO || HONEYSELECT2
                    if (this._dynamicBoneTarget.m_UpdateMode != _copiedDynamicBoneData.currentUpdateMode)
                        this.SetDynamicBoneUpdateMode(this._dynamicBoneTarget, _copiedDynamicBoneData.currentUpdateMode);
#endif
                    if (_dynamicBoneTarget.m_FreezeAxis != _copiedDynamicBoneData.currentFreezeAxis)
                        SetDynamicBoneFreezeAxis(_dynamicBoneTarget, _copiedDynamicBoneData.currentFreezeAxis);
                    if (_dynamicBoneTarget.m_Damping != _copiedDynamicBoneData.currentDamping)
                        SetDynamicBoneDamping(_dynamicBoneTarget, _copiedDynamicBoneData.currentDamping);
                    if (_dynamicBoneTarget.m_Elasticity != _copiedDynamicBoneData.currentElasticity)
                        SetDynamicBoneElasticity(_dynamicBoneTarget, _copiedDynamicBoneData.currentElasticity);
                    if (_dynamicBoneTarget.m_Stiffness != _copiedDynamicBoneData.currentStiffness)
                        SetDynamicBoneStiffness(_dynamicBoneTarget, _copiedDynamicBoneData.currentStiffness);
                    if (_dynamicBoneTarget.m_Inert != _copiedDynamicBoneData.currentInert)
                        SetDynamicBoneInertia(_dynamicBoneTarget, _copiedDynamicBoneData.currentInert);
                    if (_dynamicBoneTarget.m_Radius != _copiedDynamicBoneData.currentRadius)
                        SetDynamicBoneRadius(_dynamicBoneTarget, _copiedDynamicBoneData.currentRadius);
                };
            }

            GUI.color = Color.red;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && _dynamicBoneTarget != null)
                SetDynamicBoneNotDirty(_dynamicBoneTarget);
            GUI.color = c;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            IncEditor(150, true);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void SetDynamicBoneEnabled(DynamicBone db, bool v)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalEnabled.hasValue == false)
                data.originalEnabled = db.enabled;
            data.currentEnabled = v;
            db.enabled = v;
        }

        private void SetDynamicBoneRadius(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalRadius.hasValue == false)
                data.originalRadius = db.m_Radius;
            data.currentRadius = v;
            db.m_Radius = v;
            if (immediateUpdate)
                UpdateDynamicBone(db);
            else
                NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneInertia(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalInert.hasValue == false)
                data.originalInert = db.m_Inert;
            data.currentInert = v;
            db.m_Inert = v;
            if (immediateUpdate)
                UpdateDynamicBone(db);
            else
                NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneStiffness(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalStiffness.hasValue == false)
                data.originalStiffness = db.m_Stiffness;
            data.currentStiffness = v;
            db.m_Stiffness = v;
            if (immediateUpdate)
                UpdateDynamicBone(db);
            else
                NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneElasticity(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalElasticity.hasValue == false)
                data.originalElasticity = db.m_Elasticity;
            data.currentElasticity = v;
            db.m_Elasticity = v;
            if (immediateUpdate)
                UpdateDynamicBone(db);
            else
                NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneDamping(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalDamping.hasValue == false)
                data.originalDamping = db.m_Damping;
            data.currentDamping = v;
            db.m_Damping = v;
            if (immediateUpdate)
                UpdateDynamicBone(db);
            else
                NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneWeight(DynamicBone db, float w)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalWeight.hasValue == false)
                data.originalWeight = db.GetWeight();
            data.currentWeight = w;
            db.SetWeight(w);
        }

        private void SetDynamicBoneFreezeAxis(DynamicBone db, DynamicBone.FreezeAxis fa)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalFreezeAxis.hasValue == false)
                data.originalFreezeAxis = db.m_FreezeAxis;
            data.currentFreezeAxis = fa;
            db.m_FreezeAxis = fa;
        }

        private void SetDynamicBoneForce(DynamicBone db, Vector3 f)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalForce.hasValue == false)
                data.originalForce = db.m_Force;
            data.currentForce = f;
            db.m_Force = f;
        }

#if AISHOUJO || HONEYSELECT2
        private void SetDynamicBoneUpdateMode(DynamicBone db, DynamicBone.UpdateMode um)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalUpdateMode.hasValue == false)
                data.originalUpdateMode = db.m_UpdateMode;
            data.currentUpdateMode = um;
            db.m_UpdateMode = um;
        }
#endif

        private void SetDynamicBoneGravity(DynamicBone db, Vector3 g)
        {
            DynamicBoneData data = SetDynamicBoneDirty(db);
            if (data.originalGravity.hasValue == false)
                data.originalGravity = db.m_Gravity;
            data.currentGravity = g;
            db.m_Gravity = g;
        }

        public void LoadFrom(DynamicBonesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in other._dirtyDynamicBones)
                {
                    DynamicBone db = null;
                    foreach (DynamicBone bone in _dynamicBones)
                    {
                        if (kvp.Key.m_Root != null && bone.m_Root != null)
                        {
                            if (kvp.Key.m_Root.GetPathFrom(other._parent.transform).Equals(bone.m_Root.GetPathFrom(_parent.transform)) && _dirtyDynamicBones.ContainsKey(bone) == false)
                            {
                                db = bone;
                                break;
                            }
                        }
                        else
                        {
                            if (kvp.Key.name.Equals(bone.name) && _dirtyDynamicBones.ContainsKey(bone) == false)
                            {
                                db = bone;
                                break;
                            }
                        }
                    }
                    if (db != null)
                    {
                        if (kvp.Value.originalEnabled.hasValue)
                            db.enabled = kvp.Key.enabled;
                        if (kvp.Value.originalForce.hasValue)
                            db.m_Force = kvp.Key.m_Force;
#if AISHOUJO || HONEYSELECT2
                        if (kvp.Value.originalUpdateMode.hasValue)
                            db.m_UpdateMode = kvp.Key.m_UpdateMode;
#endif
                        if (kvp.Value.originalFreezeAxis.hasValue)
                            db.m_FreezeAxis = kvp.Key.m_FreezeAxis;
                        if (kvp.Value.originalGravity.hasValue)
                            db.m_Gravity = kvp.Key.m_Gravity;
                        if (kvp.Value.originalWeight.hasValue)
                            db.SetWeight(kvp.Key.GetWeight());
                        if (kvp.Value.originalDamping.hasValue)
                            db.m_Damping = kvp.Key.m_Damping;
                        if (kvp.Value.originalElasticity.hasValue)
                            db.m_Elasticity = kvp.Key.m_Elasticity;
                        if (kvp.Value.originalStiffness.hasValue)
                            db.m_Stiffness = kvp.Key.m_Stiffness;
                        if (kvp.Value.originalInert.hasValue)
                            db.m_Inert = kvp.Key.m_Inert;
                        if (kvp.Value.originalRadius.hasValue)
                            db.m_Radius = kvp.Key.m_Radius;
                        _dirtyDynamicBones.Add(db, new DynamicBoneData(kvp.Value));
                    }
                    _dynamicBonesScroll = other._dynamicBonesScroll;
                }
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (_dirtyDynamicBones.Count != 0)
            {
                xmlWriter.WriteStartElement("dynamicBones");
                foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in _dirtyDynamicBones)
                {
                    if (kvp.Key == null)
                        continue;
                    xmlWriter.WriteStartElement("dynamicBone");
                    xmlWriter.WriteAttributeString("root", CalculateRoot(kvp.Key));

                    if (kvp.Value.originalEnabled.hasValue)
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(kvp.Key.enabled));
                    if (kvp.Value.originalWeight.hasValue)
                        xmlWriter.WriteAttributeString("weight", XmlConvert.ToString(kvp.Key.GetWeight()));
                    if (kvp.Value.originalGravity.hasValue)
                    {
                        xmlWriter.WriteAttributeString("gravityX", XmlConvert.ToString(kvp.Key.m_Gravity.x));
                        xmlWriter.WriteAttributeString("gravityY", XmlConvert.ToString(kvp.Key.m_Gravity.y));
                        xmlWriter.WriteAttributeString("gravityZ", XmlConvert.ToString(kvp.Key.m_Gravity.z));
                    }
                    if (kvp.Value.originalForce.hasValue)
                    {
                        xmlWriter.WriteAttributeString("forceX", XmlConvert.ToString(kvp.Key.m_Force.x));
                        xmlWriter.WriteAttributeString("forceY", XmlConvert.ToString(kvp.Key.m_Force.y));
                        xmlWriter.WriteAttributeString("forceZ", XmlConvert.ToString(kvp.Key.m_Force.z));
                    }
#if AISHOUJO || HONEYSELECT2
                    if (kvp.Value.originalUpdateMode.hasValue)
                        xmlWriter.WriteAttributeString("updateMode", XmlConvert.ToString((int)kvp.Key.m_UpdateMode));
#endif
                    if (kvp.Value.originalFreezeAxis.hasValue)
                        xmlWriter.WriteAttributeString("freezeAxis", XmlConvert.ToString((int)kvp.Key.m_FreezeAxis));
                    if (kvp.Value.originalDamping.hasValue)
                        xmlWriter.WriteAttributeString("damping", XmlConvert.ToString(kvp.Key.m_Damping));
                    if (kvp.Value.originalElasticity.hasValue)
                        xmlWriter.WriteAttributeString("elasticity", XmlConvert.ToString(kvp.Key.m_Elasticity));
                    if (kvp.Value.originalStiffness.hasValue)
                        xmlWriter.WriteAttributeString("stiffness", XmlConvert.ToString(kvp.Key.m_Stiffness));
                    if (kvp.Value.originalInert.hasValue)
                        xmlWriter.WriteAttributeString("inert", XmlConvert.ToString(kvp.Key.m_Inert));
                    if (kvp.Value.originalRadius.hasValue)
                        xmlWriter.WriteAttributeString("radius", XmlConvert.ToString(kvp.Key.m_Radius));
                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            //this.RefreshDynamicBoneList();

            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            ResetAll();
            RefreshDynamicBoneList();
            bool changed = false;
            List<XmlNode> potentialChildrenNodes = new List<XmlNode>();

            XmlNode dynamicBonesNode = xmlNode.FindChildNode("dynamicBones");
            if (dynamicBonesNode != null)
            {
                foreach (XmlNode node in dynamicBonesNode.ChildNodes)
                {
                    try
                    {
                        string root = node.Attributes["root"].Value;
                        DynamicBone db = SearchDynamicBone(root, true);
                        if (db == null)
                        {
                            potentialChildrenNodes.Add(node);
                            continue;
                        }
                        if (LoadSingleBone(db, node))
                            changed = true;
                    }
                    catch (Exception e)
                    {
                        HSPE.Logger.LogError("Couldn't load dynamic bone for object " + _parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
            return changed;
        }

        public DynamicBone SearchDynamicBone(string root, bool ignoreDirty = false)
        {
            foreach (DynamicBone bone in _dynamicBones)
            {
                if (bone == null)
                    continue;
                if (bone.m_Root)
                {
                    if ((bone.m_Root.GetPathFrom(_parent.transform).Equals(root) || bone.m_Root.name.Equals(root)) && (ignoreDirty == false || _dirtyDynamicBones.ContainsKey(bone) == false))
                        return bone;
                }
                else
                {
                    if ((bone.transform.GetPathFrom(_parent.transform).Equals(root) || bone.name.Equals(root)) && (ignoreDirty == false || _dirtyDynamicBones.ContainsKey(bone) == false))
                        return bone;
                }
            }
            return null;
        }

        public string CalculateRoot(DynamicBone db)
        {
            return db.m_Root != null ? db.m_Root.GetPathFrom(_parent.transform) : db.transform.GetPathFrom(_parent.transform);
        }
        #endregion

        #region Private Methods
        private void UpdateDynamicBone(DynamicBone db)
        {
            db.InitTransforms();
            DynamicBoneData data;
            if (_dirtyDynamicBones.TryGetValue(db, out data) && data.originalGravity.hasValue)
            {
                db.m_Gravity = data.originalGravity;
                db.SetupParticles();
                db.m_Gravity = data.currentGravity;
            }
            else
                db.SetupParticles();
        }

        public override void UpdateGizmos()
        {
            if (GizmosEnabled() == false)
                return;
            _dynamicBonesDragRadius = _dynamicBonesDragRadiusBase * Studio.Studio.optionSystem.manipulateSize;
            DynamicBoneDraggingLogic();
            _debugLines.Draw(_dynamicBones, _dynamicBoneTarget, _dirtyDynamicBones);
        }

        private bool LoadSingleBone(DynamicBone db, XmlNode node)
        {
            bool loaded = false;
            DynamicBoneData data = new DynamicBoneData();

            if (node.Attributes["enabled"] != null)
            {
                bool e = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                data.originalEnabled = db.enabled;
                data.currentEnabled = e;
                db.enabled = e;
            }
            if (node.Attributes["weight"] != null)
            {
                float weight = XmlConvert.ToSingle(node.Attributes["weight"].Value);
                data.originalWeight = db.GetWeight();
                data.currentWeight = weight;
                db.SetWeight(weight);
            }
            if (node.Attributes["gravityX"] != null && node.Attributes["gravityY"] != null && node.Attributes["gravityZ"] != null)
            {
                Vector3 gravity;
                gravity.x = XmlConvert.ToSingle(node.Attributes["gravityX"].Value);
                gravity.y = XmlConvert.ToSingle(node.Attributes["gravityY"].Value);
                gravity.z = XmlConvert.ToSingle(node.Attributes["gravityZ"].Value);
                data.originalGravity = db.m_Gravity;
                data.currentGravity = gravity;
                db.m_Gravity = gravity;
            }
            if (node.Attributes["forceX"] != null && node.Attributes["forceY"] != null && node.Attributes["forceZ"] != null)
            {
                Vector3 force;
                force.x = XmlConvert.ToSingle(node.Attributes["forceX"].Value);
                force.y = XmlConvert.ToSingle(node.Attributes["forceY"].Value);
                force.z = XmlConvert.ToSingle(node.Attributes["forceZ"].Value);
                data.originalForce = db.m_Force;
                data.currentForce = force;
                db.m_Force = force;
            }
#if AISHOUJO || HONEYSELECT2
            if (node.Attributes["updateMode"] != null)
            {
                DynamicBone.UpdateMode updateMode = (DynamicBone.UpdateMode)XmlConvert.ToInt32(node.Attributes["updateMode"].Value);
                data.originalUpdateMode= db.m_UpdateMode;
                data.currentUpdateMode = updateMode;
                db.m_UpdateMode = updateMode;
            }
#endif
            if (node.Attributes["freezeAxis"] != null)
            {
                DynamicBone.FreezeAxis axis = (DynamicBone.FreezeAxis)XmlConvert.ToInt32(node.Attributes["freezeAxis"].Value);
                data.originalFreezeAxis = db.m_FreezeAxis;
                data.currentFreezeAxis = axis;
                db.m_FreezeAxis = axis;
            }
            if (node.Attributes["damping"] != null)
            {
                float damping = XmlConvert.ToSingle(node.Attributes["damping"].Value);
                data.originalDamping = db.m_Damping;
                data.currentDamping = damping;
                db.m_Damping = damping;
            }
            if (node.Attributes["elasticity"] != null)
            {
                float elasticity = XmlConvert.ToSingle(node.Attributes["elasticity"].Value);
                data.originalElasticity = db.m_Elasticity;
                data.currentElasticity = elasticity;
                db.m_Elasticity = elasticity;
            }
            if (node.Attributes["stiffness"] != null)
            {
                float stiffness = XmlConvert.ToSingle(node.Attributes["stiffness"].Value);
                data.originalStiffness = db.m_Stiffness;
                data.currentStiffness = stiffness;
                db.m_Stiffness = stiffness;
            }

            if (node.Attributes["inert"] != null)
            {
                float inert = XmlConvert.ToSingle(node.Attributes["inert"].Value);
                data.originalInert = db.m_Inert;
                data.currentInert = inert;
                db.m_Inert = inert;
            }
            if (node.Attributes["radius"] != null)
            {
                float radius = XmlConvert.ToSingle(node.Attributes["radius"].Value);
                data.originalRadius = db.m_Radius;
                data.currentRadius = radius;
                db.m_Radius = radius;
            }
            if (data.originalEnabled.hasValue ||
                data.originalWeight.hasValue ||
                data.originalGravity.hasValue ||
                data.originalForce.hasValue ||
#if AISHOUJO || HONEYSELECT2
                data.originalUpdateMode.hasValue ||
#endif
                data.originalFreezeAxis.hasValue ||
                data.originalDamping.hasValue ||
                data.originalElasticity.hasValue ||
                data.originalStiffness.hasValue ||
                data.originalInert.hasValue ||
                data.originalRadius.hasValue)
            {
                loaded = true;
                NotifyDynamicBoneForUpdate(db);
                _dirtyDynamicBones.Add(db, data);
            }
            return loaded;
        }

        private void ResetAll()
        {
            foreach (DynamicBone bone in _dynamicBones)
                SetDynamicBoneNotDirty(bone);
        }

        private void CopyToFK()
        {
            _preDragAction = () =>
            {
                List<GuideCommand.EqualsInfo> infos = new List<GuideCommand.EqualsInfo>();
                foreach (DynamicBone bone in _dynamicBones)
                {
                    foreach (object o in (IList)bone.GetPrivate("m_Particles"))
                    {
                        Transform t = (Transform)o.GetPrivate("m_Transform");
                        OCIChar.BoneInfo boneInfo;
                        if (t != null && _target.fkObjects.TryGetValue(t.gameObject, out boneInfo))
                        {
                            Vector3 oldValue = boneInfo.guideObject.changeAmount.rot;
                            boneInfo.guideObject.changeAmount.rot = t.localEulerAngles;
                            infos.Add(new GuideCommand.EqualsInfo()
                            {
                                dicKey = boneInfo.guideObject.dicKey,
                                oldValue = oldValue,
                                newValue = boneInfo.guideObject.changeAmount.rot
                            });
                        }
                    }
                }
                UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos.ToArray()));
            };
        }

        private void SetDynamicBoneNotDirty(DynamicBone bone)
        {
            DynamicBoneData data;
            if (_dynamicBones.Contains(bone) && _dirtyDynamicBones.TryGetValue(bone, out data))
            {
                if (data == null)
                    return;
                if (data.originalEnabled.hasValue)
                {
                    bone.enabled = data.originalEnabled;
                    data.currentEnabled = data.originalEnabled;
                    data.originalEnabled.Reset();
                }
                if (data.originalWeight.hasValue)
                {
                    bone.SetWeight(data.originalWeight);
                    data.currentWeight = data.originalWeight;
                    data.originalWeight.Reset();
                }
                if (data.originalGravity.hasValue)
                {
                    bone.m_Gravity = data.originalGravity;
                    data.currentGravity = data.originalGravity;
                    data.originalGravity.Reset();
                }
                if (data.originalForce.hasValue)
                {
                    bone.m_Force = data.originalForce;
                    data.currentGravity = data.originalGravity;
                    data.originalForce.Reset();
                }
#if AISHOUJO || HONEYSELECT2
                if (data.originalUpdateMode.hasValue)
                {
                    bone.m_UpdateMode = data.originalUpdateMode;
                    data.currentUpdateMode = data.originalUpdateMode;
                    data.originalUpdateMode.Reset();
                }
#endif
                if (data.originalFreezeAxis.hasValue)
                {
                    bone.m_FreezeAxis = data.originalFreezeAxis;
                    data.currentFreezeAxis = data.originalFreezeAxis;
                    data.originalFreezeAxis.Reset();
                }
                if (data.originalDamping.hasValue)
                {
                    bone.m_Damping = data.originalDamping;
                    data.currentDamping = data.originalDamping;
                    data.originalDamping.Reset();
                }
                if (data.originalElasticity.hasValue)
                {
                    bone.m_Elasticity = data.originalElasticity;
                    data.currentElasticity = data.originalElasticity;
                    data.originalElasticity.Reset();
                }
                if (data.originalStiffness.hasValue)
                {
                    bone.m_Stiffness = data.originalStiffness;
                    data.currentStiffness = data.originalStiffness;
                    data.originalStiffness.Reset();
                }
                if (data.originalInert.hasValue)
                {
                    bone.m_Inert = data.originalInert;
                    data.currentInert = data.originalInert;
                    data.originalInert.Reset();
                }
                if (data.originalRadius.hasValue)
                {
                    bone.m_Radius = data.originalRadius;
                    data.currentRadius = data.originalRadius;
                    data.originalRadius.Reset();
                }
                _dirtyDynamicBones.Remove(bone);
                NotifyDynamicBoneForUpdate(bone);
            }
        }

        private DynamicBoneData SetDynamicBoneDirty(DynamicBone bone)
        {
            DynamicBoneData data;
            if (_dirtyDynamicBones.TryGetValue(bone, out data) == false)
            {
                data = new DynamicBoneData();
                _dirtyDynamicBones.Add(bone, data);
            }
            return data;
        }

        private bool IsDynamicBoneDirty(DynamicBone bone)
        {
            return _dirtyDynamicBones.ContainsKey(bone);
        }

        private void NotifyDynamicBoneForUpdate(DynamicBone bone)
        {
            if (_toUpdate.Contains(bone) == false)
                _toUpdate.Add(bone);
        }

        private void DynamicBoneDraggingLogic()
        {
            if (_preDragAction != null)
                _preDragAction();
            _preDragAction = null;
            if (Input.GetMouseButtonDown(0))
            {
                float distanceFromCamera = float.PositiveInfinity;
                for (int i = 0; i < _dynamicBones.Count; i++)
                {
                    DynamicBone db = _dynamicBones[i];
                    if (db == null) continue;
                    if (!_showAllDebugDB && _dynamicBoneTarget != db) continue;
                    Transform leaf = (db.m_Root ?? db.transform).GetFirstLeaf();
                    Vector3 raycastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(leaf.position - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                    if ((raycastPos - leaf.position).sqrMagnitude < (_dynamicBonesDragRadius * _dynamicBonesDragRadius) &&
                        (raycastPos - Camera.main.transform.position).sqrMagnitude < distanceFromCamera)
                    {
                        isDraggingDynamicBone = true;
                        distanceFromCamera = (raycastPos - Camera.main.transform.position).sqrMagnitude;
                        _dragDynamicBoneStartPosition = raycastPos;
                        _lastDynamicBoneGravity = db.m_Force;
                        _draggedDynamicBone = db;
                        _dynamicBoneTarget = db;
                    }
                }
            }
            else if (Input.GetMouseButton(0) && isDraggingDynamicBone)
            {
                _dragDynamicBoneEndPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(_dragDynamicBoneStartPosition - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                SetDynamicBoneForce(_draggedDynamicBone, _lastDynamicBoneGravity + (_dragDynamicBoneEndPosition - _dragDynamicBoneStartPosition) * (_inc * 1000f) / 12f);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingDynamicBone = false;
            }
        }

        private void RefreshDynamicBoneList()
        {
            if (_parent == null) return;
            _parent._childObjects.RemoveWhere(gobj => gobj == null);

            _isBusy = true;
            DynamicBone[] dynamicBones = _parent.GetComponentsInChildren<DynamicBone>(true);
            List<DynamicBone> toDelete = null;
            foreach (DynamicBone db in _dynamicBones)
            {
                if (dynamicBones.Contains(db) == false)
                {
                    if (toDelete == null)
                        toDelete = new List<DynamicBone>();
                    toDelete.Add(db);
                }
            }
            foreach (KeyValuePair<DynamicBone, DynamicBoneData> pair in new Dictionary<DynamicBone, DynamicBoneData>(_dirtyDynamicBones)) //Putting every dirty one into headless, that should help
            {
                if (pair.Key != null)
                {
                    _headlessDirtyDynamicBones.Add(pair.Key.m_Root != null ? pair.Key.m_Root : pair.Key.transform, new DynamicBoneData(pair.Value));
                    _headlessReconstructionTimeout = 5;
                    SetDynamicBoneNotDirty(pair.Key);
                }
            }
            _dirtyDynamicBones.Clear();
            if (toDelete != null)
            {
                foreach (DynamicBone db in toDelete)
                    _dynamicBones.Remove(db);
            }
            List<DynamicBone> toAdd = null;
            foreach (DynamicBone db in dynamicBones)
            {
                if (_dynamicBones.Contains(db) == false && _parent._childObjects.All(child => db.transform.IsChildOf(child.transform) == false))
                {
                    if (toAdd == null)
                        toAdd = new List<DynamicBone>();
                    toAdd.Add(db);
                }
            }
            if (toAdd != null)
            {
                foreach (DynamicBone db in toAdd)
                    _dynamicBones.Add(db);
            }
            if (_dynamicBones.Count != 0 && _dynamicBoneTarget == null)
                _dynamicBoneTarget = _dynamicBones.FirstOrDefault(d => d.m_Root != null);
            foreach (KeyValuePair<DynamicBoneColliderBase, CollidersEditor> pair in CollidersEditor._loneColliders)
            {
                Dictionary<object, bool> dynamicBonesDic;
                if (pair.Value._dirtyColliders.TryGetValue(pair.Key, out CollidersEditor.ColliderDataBase data) == false || data.allDynamicBones.TryGetValue(_parent, out dynamicBonesDic) == false)
                    dynamicBonesDic = null;
                foreach (DynamicBone bone in _dynamicBones)
                {
                    bool update = false;
                    if (dynamicBonesDic != null)
                    {
                        if (dynamicBonesDic.ContainsKey(bone) == false)
                            dynamicBonesDic.Add(bone, pair.Value._addNewDynamicBonesAsDefault);

                        if (dynamicBonesDic[bone] == false)
                        {
                            if (bone.m_Colliders.Contains(pair.Key))
                            {
                                bone.m_Colliders.Remove(pair.Key);
                                update = true;
                            }
                        }
                        else
                        {
                            if (bone.m_Colliders.Contains(pair.Key) == false)
                            {
                                bone.m_Colliders.Add(pair.Key);
                                update = true;
                            }
                        }
                    }
                    else
                    {
                        pair.Value.SetIgnoreDynamicBone(pair.Key, _parent, bone, !pair.Value._addNewDynamicBonesAsDefault);
                        update = true;
                    }

                    if (update)
                        NotifyDynamicBoneForUpdate(bone);
                }
            }
        }

        private static void UpdateDebugLinesState(DynamicBonesEditor self)
        {
            if (_debugLines != null)
                _debugLines.SetActive(self != null && self.GizmosEnabled());
        }

        private bool GizmosEnabled()
        {
            return _isEnabled && PoseController._drawAdvancedMode;
        }
        #endregion

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            private class Parameter
            {
                public readonly DynamicBonesEditor editor;
                public readonly string dynamicBoneRootPath;

                public DynamicBone dynamicBone
                {
                    get
                    {
                        if (_dynamicBone == null)
                            _dynamicBone = editor.SearchDynamicBone(dynamicBoneRootPath);
                        return _dynamicBone;
                    }
                }
                private DynamicBone _dynamicBone;
                private readonly int _hashCode;

                public Parameter(DynamicBonesEditor editor, DynamicBone dynamicBone)
                {
                    this.editor = editor;
                    _dynamicBone = dynamicBone;
                    dynamicBoneRootPath = this.editor.CalculateRoot(_dynamicBone);

                    unchecked
                    {
                        int hash = 17;
                        hash = hash * 31 + (this.editor != null ? this.editor.GetHashCode() : 0);
                        _hashCode = hash * 31 + (dynamicBoneRootPath != null ? dynamicBoneRootPath.GetHashCode() : 0);
                    }
                }

                public Parameter(DynamicBonesEditor editor, string dynamicBoneRootPath)
                {
                    this.editor = editor;
                    this.dynamicBoneRootPath = dynamicBoneRootPath;

                    unchecked
                    {
                        int hash = 17;
                        hash = hash * 31 + (this.editor != null ? this.editor.GetHashCode() : 0);
                        _hashCode = hash * 31 + (this.dynamicBoneRootPath != null ? this.dynamicBoneRootPath.GetHashCode() : 0);
                    }
                }

                public override int GetHashCode()
                {
                    return _hashCode;
                }

                public override string ToString()
                {
                    return $"editor: [{editor}], dynamicBone: [{_dynamicBone}], dynamicBoneRootPath: [{dynamicBoneRootPath}], hashCode: [{_hashCode}]";
                }
            }


            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneEnabled",
                        name: "DB Enabled",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                bool newValue = (bool)leftValue;
                                if (p.dynamicBone.enabled != newValue)
                                    p.editor.SetDynamicBoneEnabled(p.dynamicBone, newValue);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.enabled,
                        readValueFromXml: (parameter, node) => node.ReadBool("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Enabled ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneGravity",
                        name: "DB Gravity",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                Vector3 newValue = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor);
                                if (p.dynamicBone.m_Gravity != newValue)
                                    p.editor.SetDynamicBoneGravity(p.dynamicBone, newValue);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.m_Gravity,
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Gravity ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneForce",
                        name: "DB Force",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                Vector3 newValue = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor);
                                if (p.dynamicBone.m_Force != newValue)
                                    p.editor.SetDynamicBoneForce(p.dynamicBone, newValue);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.m_Force,
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Force ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneFreezeAxis",
                        name: "DB FreezeAxis",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                DynamicBone.FreezeAxis newValue = (DynamicBone.FreezeAxis)leftValue;
                                if (p.dynamicBone.m_FreezeAxis != newValue)
                                    p.editor.SetDynamicBoneFreezeAxis(p.dynamicBone, newValue);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.m_FreezeAxis,
                        readValueFromXml: (parameter, node) => (DynamicBone.FreezeAxis)node.ReadInt("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB FreezeAxis ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneWeight",
                        name: "DB Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                float newValue = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
                                if (Mathf.Approximately(p.dynamicBone.GetWeight(), newValue) == false)
                                    p.editor.SetDynamicBoneWeight(p.dynamicBone, newValue);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.GetWeight(),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Weight ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneDamping",
                        name: "DB Damping",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                float newValue = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
                                if (Mathf.Approximately(p.dynamicBone.m_Damping, newValue) == false)
                                    p.editor.SetDynamicBoneDamping(p.dynamicBone, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor), true);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.m_Damping,
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Damping ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneElasticity",
                        name: "DB Elasticity",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                float newValue = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
                                if (Mathf.Approximately(p.dynamicBone.m_Elasticity, newValue) == false)
                                    p.editor.SetDynamicBoneElasticity(p.dynamicBone, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor), true);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.m_Elasticity,
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Elasticity ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneStiffness",
                        name: "DB Stiffness",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                float newValue = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
                                if (Mathf.Approximately(p.dynamicBone.m_Stiffness, newValue) == false)
                                    p.editor.SetDynamicBoneStiffness(p.dynamicBone, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor), true);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.m_Stiffness,
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Stiffness ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneInertia",
                        name: "DB Inertia",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                float newValue = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
                                if (Mathf.Approximately(p.dynamicBone.m_Inert, newValue) == false)
                                    p.editor.SetDynamicBoneInertia(p.dynamicBone, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor), true);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.m_Inert,
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Inertia ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "dynamicBoneRadius",
                        name: "DB Radius",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            if (p.editor._isBusy == false)
                            {
                                float newValue = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
                                if (Mathf.Approximately(p.dynamicBone.m_Radius, newValue) == false)
                                    p.editor.SetDynamicBoneRadius(p.dynamicBone, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor), true);
                            }
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).dynamicBone.m_Radius,
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"DB Radius ({((Parameter)parameter).dynamicBone.m_Root.name})"
                );
            }

            private static bool CheckIntegrity(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue)
            {
                if (parameter == null)
                    return false;
                Parameter p = (Parameter)parameter;
                if (p.editor == null)
                    return false;
                if (p.editor._isBusy)
                    return true;
                if (p.dynamicBone == null)
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
                return new Parameter(controller._dynamicBonesEditor, controller._dynamicBonesEditor._dynamicBoneTarget);
            }

            private static object ReadParameterFromXml(ObjectCtrlInfo oci, XmlNode node)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new Parameter(controller._dynamicBonesEditor, node.Attributes["parameter"].Value);
            }

            private static void WriteParameterToXml(ObjectCtrlInfo oci, XmlTextWriter writer, object o)
            {
                Parameter p = (Parameter)o;
                writer.WriteAttributeString("parameter", p.dynamicBoneRootPath);
            }
        }
        #endregion
    }
}
