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
        private const float _dynamicBonesDragRadius = 0.025f;
#elif AISHOUJO || HONEYSELECT2
        private const float _dynamicBonesDragRadius = 0.25f;
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
                this.originalEnabled = other.originalEnabled;
                this.originalWeight = other.originalWeight;
                this.originalGravity = other.originalGravity;
                this.originalForce = other.originalForce;
#if AISHOUJO || HONEYSELECT2
                this.originalUpdateMode = other.originalUpdateMode;
#endif
                this.originalFreezeAxis = other.originalFreezeAxis;
                this.originalDamping = other.originalDamping;
                this.originalElasticity = other.originalElasticity;
                this.originalStiffness = other.originalStiffness;
                this.originalInert = other.originalInert;
                this.originalRadius = other.originalRadius;

                this.currentEnabled = other.currentEnabled;
                this.currentWeight = other.currentWeight;
                this.currentGravity = other.currentGravity;
                this.currentForce = other.currentForce;
#if AISHOUJO || HONEYSELECT2
                this.currentUpdateMode= other.currentUpdateMode;
#endif
                this.currentFreezeAxis = other.currentFreezeAxis;
                this.currentDamping = other.currentDamping;
                this.currentElasticity = other.currentElasticity;
                this.currentStiffness = other.currentStiffness;
                this.currentInert = other.currentInert;
                this.currentRadius = other.currentRadius;
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
                    if (i < this._debugLines.Count)
                    {
                        debug = this._debugLines[i];
                        debug.SetActive(true);
                    }
                    else
                    {
                        debug = new DebugDynamicBone(db);
                        this._debugLines.Add(debug);
                    }
                    debug.Draw(db, db == target, dirtyDynamicBones.ContainsKey(db));
                    ++i;
                }
                for (; i < this._debugLines.Count; ++i)
                {
                    DebugDynamicBone debug = this._debugLines[i];
                    if (debug.IsActive())
                        debug.SetActive(false);
                }
            }

            public void Destroy()
            {
                if (this._debugLines != null)
                    for (int i = 0; i < this._debugLines.Count; i++)
                    {
                        this._debugLines[i].Destroy();
                    }
            }

            public void SetActive(bool active, int limit = -1)
            {
                if (this._debugLines == null)
                    return;
                if (limit == -1)
                    limit = this._debugLines.Count;
                for (int i = 0; i < limit; i++)
                {
                    this._debugLines[i].SetActive(active);
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

                this._gravity = VectorLine.SetLine(_redColor, origin, final);
                this._gravity.endCap = "vector";
                this._gravity.lineWidth = 4f;

                origin = final;
                final += db.m_Force * scale;

                this._force = VectorLine.SetLine(_blueColor, origin, final);
                this._force.endCap = "vector";
                this._force.lineWidth = 4f;

                origin = end.position;

                this._both = VectorLine.SetLine(_greenColor, origin, final);
                this._both.endCap = "vector";
                this._both.lineWidth = 4f;

                this._circle = VectorLine.SetLine(_greenColor, new Vector3[37]);
                this._circle.lineWidth = 4f;
                this._circle.MakeCircle(end.position, Camera.main.transform.forward, _dynamicBonesDragRadius);
            }

            public void Draw(DynamicBone db, bool isTarget, bool isDirty)
            {
                Transform end = (db.m_Root ?? db.transform).GetFirstLeaf();

                float scale = 10f;
                Vector3 origin = end.position;
                Vector3 final = origin + (db.m_Gravity) * scale;

                this._gravity.points3[0] = origin;
                this._gravity.points3[1] = final;

                origin = final;
                final += db.m_Force * scale;

                this._force.points3[0] = origin;
                this._force.points3[1] = final;

                origin = end.position;

                Color c;
                if (isTarget)
                    c = Color.cyan;
                else if (isDirty)
                    c = Color.magenta;
                else
                    c = _greenColor;

                this._both.points3[0] = origin;
                this._both.points3[1] = final;

                this._circle.MakeCircle(end.position, Camera.main.transform.forward, _dynamicBonesDragRadius);

                float distance = 1 - (Mathf.Clamp((end.position - Camera.main.transform.position).sqrMagnitude, 5, 20) - 5) / 15;
                if (distance < 0.2f)
                    distance = 0.2f;
                c.a = distance;

                this._both.color = c;
                this._circle.color = c;
                Color gravityColor = this._gravity.color;
                gravityColor.a = distance;
                this._gravity.color = gravityColor;
                Color forceColor = this._force.color;
                forceColor.a = distance;
                this._force.color = forceColor;

                this._gravity.Draw();
                this._force.Draw();
                this._both.Draw();
                this._circle.Draw();
            }

            public void Destroy()
            {
                VectorLine.Destroy(ref this._gravity);
                VectorLine.Destroy(ref this._force);
                VectorLine.Destroy(ref this._both);
                VectorLine.Destroy(ref this._circle);
            }

            public void SetActive(bool active)
            {
                this._gravity.active = active;
                this._force.active = active;
                this._both.active = active;
                this._circle.active = active;
            }

            public bool IsActive()
            {
                return this._gravity.active;
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
        public override bool shouldDisplay { get { return this._dynamicBones.Count(b => b.m_Root != null) > 0; } }
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
            this._target = target;
            this._parent.onUpdate += this.Update;
            this._parent.onLateUpdate += this.LateUpdate;
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
            this.RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 3);
            this._incIndex = -3;
        }

        private void Update()
        {
            if (!this._firstRefresh)
            {
                this.RefreshDynamicBoneList();
                this._firstRefresh = true;
            }
            if (this._toUpdate.Count != 0)
            {
                foreach (DynamicBone dynamicBone in this._toUpdate)
                {
                    if (dynamicBone == null)
                        continue;
                    this.UpdateDynamicBone(dynamicBone);
                }
                this._toUpdate.Clear();
            }
        }

        private void LateUpdate()
        {
            if (this._headlessReconstructionTimeout >= 0)
            {
                this._headlessReconstructionTimeout--;
                foreach (KeyValuePair<Transform, DynamicBoneData> pair in this._headlessDirtyDynamicBones.ToList())
                {
                    if (pair.Key == null)
                        continue;
                    foreach (DynamicBone db in this._dynamicBones)
                    {
                        if (db != null && this._dirtyDynamicBones.ContainsKey(db) == false && (db.m_Root == pair.Key || db.transform == pair.Key))
                        {
                            this._dirtyDynamicBones.Add(db, pair.Value);

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
                            this._headlessDirtyDynamicBones.Remove(pair.Key);
                            this.NotifyDynamicBoneForUpdate(db);
                            break;
                        }
                    }
                }
            }
            else if (this._headlessDirtyDynamicBones.Count != 0)
                this._headlessDirtyDynamicBones.Clear();
            else
                this._isBusy = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            this._parent.onUpdate -= this.Update;
            this._parent.onLateUpdate -= this.LateUpdate;
        }

        #endregion

        #region Public Methods
        public override void OnCharacterReplaced()
        {
            this.RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 2);
        }

        public override void OnLoadClothesFile()
        {
            this.RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 2);
        }

#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        public override void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force)
#elif KOIKATSU
        public override void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force)
#endif
        {
            this.RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 2);
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
            this._dynamicBonesScroll = GUILayout.BeginScrollView(this._dynamicBonesScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            foreach (DynamicBone db in this._dynamicBones)
            {
                if (db.m_Root == null)
                    continue;
                if (this.IsDynamicBoneDirty(db))
                    GUI.color = Color.magenta;
                if (ReferenceEquals(db, this._dynamicBoneTarget))
                    GUI.color = Color.cyan;
                string dName = db.m_Root.name;
                string newName;
                if (BonesEditor._boneAliases.TryGetValue(dName, out newName))
                    dName = newName;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(dName + (this.IsDynamicBoneDirty(db) ? "*" : "")))
                    this._dynamicBoneTarget = db;
                GUILayout.Space(GUI.skin.verticalScrollbar.fixedWidth);
                GUILayout.EndHorizontal();
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Copy to FK"))
                this.CopyToFK();
            if (GUILayout.Button("Force refresh list"))
                this.RefreshDynamicBoneList();

            {
                GUI.color = Color.red;
                if (GUILayout.Button("Reset all") && this._dynamicBoneTarget != null)
                    this.ResetAll();
                GUI.color = c;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

            this._dynamicBonesScroll2 = GUILayout.BeginScrollView(this._dynamicBonesScroll2, false, true);

            {
                GUILayout.BeginHorizontal();
                bool e = false;
                if (this._dynamicBoneTarget != null)
                    e = this._dynamicBoneTarget.enabled;
                e = GUILayout.Toggle(e, "Enabled");
                if (this._dynamicBoneTarget != null && this._dynamicBoneTarget.enabled != e)
                    this.SetDynamicBoneEnabled(this._dynamicBoneTarget, e);
                GUILayout.FlexibleSpace();
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.enabled = data.originalEnabled;
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
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Gravity = data.originalGravity;
                    data.currentGravity = data.originalGravity;
                    data.originalGravity.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                Vector3 g = Vector3.zero;
                if (this._dynamicBoneTarget != null)
                    g = this._dynamicBoneTarget.m_Gravity;
                g = this.Vector3Editor(g, _redColor);
                if (this._dynamicBoneTarget != null && this._dynamicBoneTarget.m_Gravity != g)
                    this.SetDynamicBoneGravity(this._dynamicBoneTarget, g);
                GUILayout.EndVertical();
            }

            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Force");
                GUILayout.FlexibleSpace();
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Force = data.originalForce;
                    data.currentForce = data.originalForce;
                    data.originalForce.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                Vector3 f = Vector3.zero;
                if (this._dynamicBoneTarget != null)
                    f = this._dynamicBoneTarget.m_Force;
                f = this.Vector3Editor(f, _blueColor);
                if (this._dynamicBoneTarget != null && this._dynamicBoneTarget.m_Force != f)
                    this.SetDynamicBoneForce(this._dynamicBoneTarget, f);

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
                if (this._dynamicBoneTarget != null)
                    fa = this._dynamicBoneTarget.m_FreezeAxis;
                fa = (DynamicBone.FreezeAxis)GUILayout.SelectionGrid((int)fa, _freezeAxisNames, 4);

                if (this._dynamicBoneTarget != null && this._dynamicBoneTarget.m_FreezeAxis != fa)
                    this.SetDynamicBoneFreezeAxis(this._dynamicBoneTarget, fa);

                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_FreezeAxis = data.originalFreezeAxis;
                    data.currentFreezeAxis = data.originalFreezeAxis;
                    data.originalFreezeAxis.Reset();
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }

            {
                float w = 0f;
                if (this._dynamicBoneTarget != null)
                    w = this._dynamicBoneTarget.GetWeight();

                w = this.FloatEditor(w, 0f, 1f, "Weight\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (this._dynamicBoneTarget != null && this._dirtyDynamicBones.TryGetValue(this._dynamicBoneTarget, out data) && data.originalWeight.hasValue)
                    {
                        this._dynamicBoneTarget.SetWeight(data.originalWeight);
                        data.currentWeight = data.originalWeight;
                        data.originalWeight.Reset();
                        return data.currentWeight;
                    }
                    return value;
                });

                if (this._dynamicBoneTarget != null && !Mathf.Approximately(this._dynamicBoneTarget.GetWeight(), w))
                    this.SetDynamicBoneWeight(this._dynamicBoneTarget, w);

            }

            {
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Damping;

                v = this.FloatEditor(v, 0f, 1f, "Damping\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (this._dynamicBoneTarget != null && this._dirtyDynamicBones.TryGetValue(this._dynamicBoneTarget, out data) && data.originalDamping.hasValue)
                    {
                        this._dynamicBoneTarget.m_Damping = data.originalDamping;
                        data.currentDamping = data.originalDamping;
                        data.originalDamping.Reset();
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                        return data.currentDamping;
                    }
                    return value;
                });
                if (this._dynamicBoneTarget != null && !Mathf.Approximately(this._dynamicBoneTarget.m_Damping, v))
                    this.SetDynamicBoneDamping(this._dynamicBoneTarget, v);
            }

            {
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Elasticity;

                v = this.FloatEditor(v, 0f, 1f, "Elasticity\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (this._dynamicBoneTarget != null && this._dirtyDynamicBones.TryGetValue(this._dynamicBoneTarget, out data) && data.originalElasticity.hasValue)
                    {
                        this._dynamicBoneTarget.m_Elasticity = data.originalElasticity;
                        data.currentElasticity = data.originalElasticity;
                        data.originalElasticity.Reset();
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                        return data.currentElasticity;
                    }
                    return value;
                });
                if (this._dynamicBoneTarget != null && !Mathf.Approximately(this._dynamicBoneTarget.m_Elasticity, v))
                    this.SetDynamicBoneElasticity(this._dynamicBoneTarget, v);
            }

            {
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Stiffness;

                v = this.FloatEditor(v, 0f, 1f, "Stiffness\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (this._dynamicBoneTarget != null && this._dirtyDynamicBones.TryGetValue(this._dynamicBoneTarget, out data) && data.originalStiffness.hasValue)
                    {
                        this._dynamicBoneTarget.m_Stiffness = data.originalStiffness;
                        data.currentStiffness = data.originalStiffness;
                        data.originalStiffness.Reset();
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                        return data.currentStiffness;
                    }
                    return value;
                });
                if (this._dynamicBoneTarget != null && !Mathf.Approximately(this._dynamicBoneTarget.m_Stiffness, v))
                    this.SetDynamicBoneStiffness(this._dynamicBoneTarget, v);
            }

            {
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Inert;

                v = this.FloatEditor(v, 0f, 1f, "Inertia\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (this._dynamicBoneTarget != null && this._dirtyDynamicBones.TryGetValue(this._dynamicBoneTarget, out data) && data.originalInert.hasValue)
                    {
                        this._dynamicBoneTarget.m_Inert = data.originalInert;
                        data.currentInert = data.originalInert;
                        data.originalInert.Reset();
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                        return data.currentInert;
                    }
                    return value;
                });
                if (this._dynamicBoneTarget != null && !Mathf.Approximately(this._dynamicBoneTarget.m_Inert, v))
                    this.SetDynamicBoneInertia(this._dynamicBoneTarget, v);
            }

            {
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Radius;

                v = this.FloatEditor(v, 0f, 1f, "Radius\t", onReset: (value) =>
                {
                    DynamicBoneData data;
                    if (this._dynamicBoneTarget != null && this._dirtyDynamicBones.TryGetValue(this._dynamicBoneTarget, out data) && data.originalRadius.hasValue)
                    {
                        this._dynamicBoneTarget.m_Radius = data.originalRadius;
                        data.currentRadius = data.originalRadius;
                        data.originalRadius.Reset();
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                        return data.currentRadius;
                    }
                    return value;
                });
                if (this._dynamicBoneTarget != null && !Mathf.Approximately(this._dynamicBoneTarget.m_Radius, v))
                    this.SetDynamicBoneRadius(this._dynamicBoneTarget, v);
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Go to root", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null)
            {
                this._parent.EnableModule(this._parent._bonesEditor);
                this._parent._bonesEditor.GoToObject(this._dynamicBoneTarget.m_Root.gameObject);
            }

            if (GUILayout.Button("Go to tail", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null)
            {
                this._parent.EnableModule(this._parent._bonesEditor);
                this._parent._bonesEditor.GoToObject(this._dynamicBoneTarget.m_Root.GetDeepestLeaf().gameObject);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Copy Settings", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null)
            {
                this._preDragAction = () =>
                {
                    _copiedDynamicBoneData = new DynamicBoneData();
                    _copiedDynamicBoneData.currentEnabled = this._dynamicBoneTarget.enabled;
                    _copiedDynamicBoneData.currentWeight = this._dynamicBoneTarget.GetWeight();
                    _copiedDynamicBoneData.currentGravity = this._dynamicBoneTarget.m_Gravity;
                    _copiedDynamicBoneData.currentForce = this._dynamicBoneTarget.m_Force;
#if AISHOUJO || HONEYSELECT2
                    _copiedDynamicBoneData.currentUpdateMode = this._dynamicBoneTarget.m_UpdateMode;
#endif
                    _copiedDynamicBoneData.currentFreezeAxis = this._dynamicBoneTarget.m_FreezeAxis;
                    _copiedDynamicBoneData.currentDamping = this._dynamicBoneTarget.m_Damping;
                    _copiedDynamicBoneData.currentElasticity = this._dynamicBoneTarget.m_Elasticity;
                    _copiedDynamicBoneData.currentStiffness = this._dynamicBoneTarget.m_Stiffness;
                    _copiedDynamicBoneData.currentInert = this._dynamicBoneTarget.m_Inert;
                    _copiedDynamicBoneData.currentRadius = this._dynamicBoneTarget.m_Radius;

                };
            }

            if (GUILayout.Button("Paste Settings", GUILayout.ExpandWidth(false)) && _copiedDynamicBoneData != null && this._dynamicBoneTarget != null)
            {
                this._preDragAction = () =>
                {
                    if (this._dynamicBoneTarget.enabled != _copiedDynamicBoneData.currentEnabled)
                        this.SetDynamicBoneEnabled(this._dynamicBoneTarget, _copiedDynamicBoneData.currentEnabled);
                    if (this._dynamicBoneTarget.GetWeight() != _copiedDynamicBoneData.currentWeight)
                        this.SetDynamicBoneWeight(this._dynamicBoneTarget, _copiedDynamicBoneData.currentWeight);
                    if (this._dynamicBoneTarget.m_Gravity != _copiedDynamicBoneData.currentGravity)
                        this.SetDynamicBoneGravity(this._dynamicBoneTarget, _copiedDynamicBoneData.currentGravity);
                    if (this._dynamicBoneTarget.m_Force != _copiedDynamicBoneData.currentForce)
                        this.SetDynamicBoneForce(this._dynamicBoneTarget, _copiedDynamicBoneData.currentForce);
#if AISHOUJO || HONEYSELECT2
                    if (this._dynamicBoneTarget.m_UpdateMode != _copiedDynamicBoneData.currentUpdateMode)
                        this.SetDynamicBoneUpdateMode(this._dynamicBoneTarget, _copiedDynamicBoneData.currentUpdateMode);
#endif
                    if (this._dynamicBoneTarget.m_FreezeAxis != _copiedDynamicBoneData.currentFreezeAxis)
                        this.SetDynamicBoneFreezeAxis(this._dynamicBoneTarget, _copiedDynamicBoneData.currentFreezeAxis);
                    if (this._dynamicBoneTarget.m_Damping != _copiedDynamicBoneData.currentDamping)
                        this.SetDynamicBoneDamping(this._dynamicBoneTarget, _copiedDynamicBoneData.currentDamping);
                    if (this._dynamicBoneTarget.m_Elasticity != _copiedDynamicBoneData.currentElasticity)
                        this.SetDynamicBoneElasticity(this._dynamicBoneTarget, _copiedDynamicBoneData.currentElasticity);
                    if (this._dynamicBoneTarget.m_Stiffness != _copiedDynamicBoneData.currentStiffness)
                        this.SetDynamicBoneStiffness(this._dynamicBoneTarget, _copiedDynamicBoneData.currentStiffness);
                    if (this._dynamicBoneTarget.m_Inert != _copiedDynamicBoneData.currentInert)
                        this.SetDynamicBoneInertia(this._dynamicBoneTarget, _copiedDynamicBoneData.currentInert);
                    if (this._dynamicBoneTarget.m_Radius != _copiedDynamicBoneData.currentRadius)
                        this.SetDynamicBoneRadius(this._dynamicBoneTarget, _copiedDynamicBoneData.currentRadius);
                };
            }

            GUI.color = Color.red;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null)
                this.SetDynamicBoneNotDirty(this._dynamicBoneTarget);
            GUI.color = c;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            this.IncEditor(150, true);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void SetDynamicBoneEnabled(DynamicBone db, bool v)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalEnabled.hasValue == false)
                data.originalEnabled = db.enabled;
            data.currentEnabled = v;
            db.enabled = v;
        }

        private void SetDynamicBoneRadius(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalRadius.hasValue == false)
                data.originalRadius = db.m_Radius;
            data.currentRadius = v;
            db.m_Radius = v;
            if (immediateUpdate)
                this.UpdateDynamicBone(db);
            else
                this.NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneInertia(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalInert.hasValue == false)
                data.originalInert = db.m_Inert;
            data.currentInert = v;
            db.m_Inert = v;
            if (immediateUpdate)
                this.UpdateDynamicBone(db);
            else
                this.NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneStiffness(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalStiffness.hasValue == false)
                data.originalStiffness = db.m_Stiffness;
            data.currentStiffness = v;
            db.m_Stiffness = v;
            if (immediateUpdate)
                this.UpdateDynamicBone(db);
            else
                this.NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneElasticity(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalElasticity.hasValue == false)
                data.originalElasticity = db.m_Elasticity;
            data.currentElasticity = v;
            db.m_Elasticity = v;
            if (immediateUpdate)
                this.UpdateDynamicBone(db);
            else
                this.NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneDamping(DynamicBone db, float v, bool immediateUpdate = false)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalDamping.hasValue == false)
                data.originalDamping = db.m_Damping;
            data.currentDamping = v;
            db.m_Damping = v;
            if (immediateUpdate)
                this.UpdateDynamicBone(db);
            else
                this.NotifyDynamicBoneForUpdate(db);
        }

        private void SetDynamicBoneWeight(DynamicBone db, float w)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalWeight.hasValue == false)
                data.originalWeight = db.GetWeight();
            data.currentWeight = w;
            db.SetWeight(w);
        }

        private void SetDynamicBoneFreezeAxis(DynamicBone db, DynamicBone.FreezeAxis fa)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
            if (data.originalFreezeAxis.hasValue == false)
                data.originalFreezeAxis = db.m_FreezeAxis;
            data.currentFreezeAxis = fa;
            db.m_FreezeAxis = fa;
        }

        private void SetDynamicBoneForce(DynamicBone db, Vector3 f)
        {
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
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
            DynamicBoneData data = this.SetDynamicBoneDirty(db);
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
                    foreach (DynamicBone bone in this._dynamicBones)
                    {
                        if (kvp.Key.m_Root != null && bone.m_Root != null)
                        {
                            if (kvp.Key.m_Root.GetPathFrom(other._parent.transform).Equals(bone.m_Root.GetPathFrom(this._parent.transform)) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                            {
                                db = bone;
                                break;
                            }
                        }
                        else
                        {
                            if (kvp.Key.name.Equals(bone.name) && this._dirtyDynamicBones.ContainsKey(bone) == false)
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
                        this._dirtyDynamicBones.Add(db, new DynamicBoneData(kvp.Value));
                    }
                    this._dynamicBonesScroll = other._dynamicBonesScroll;
                }
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this._dirtyDynamicBones.Count != 0)
            {
                xmlWriter.WriteStartElement("dynamicBones");
                foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in this._dirtyDynamicBones)
                {
                    if (kvp.Key == null)
                        continue;
                    xmlWriter.WriteStartElement("dynamicBone");
                    xmlWriter.WriteAttributeString("root", this.CalculateRoot(kvp.Key));

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
            this.ResetAll();
            this.RefreshDynamicBoneList();
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
                        DynamicBone db = this.SearchDynamicBone(root, true);
                        if (db == null)
                        {
                            potentialChildrenNodes.Add(node);
                            continue;
                        }
                        if (this.LoadSingleBone(db, node))
                            changed = true;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load dynamic bone for object " + this._parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
            return changed;
        }

        public DynamicBone SearchDynamicBone(string root, bool ignoreDirty = false)
        {
            foreach (DynamicBone bone in this._dynamicBones)
            {
                if (bone.m_Root)
                {
                    if ((bone.m_Root.GetPathFrom(this._parent.transform).Equals(root) || bone.m_Root.name.Equals(root)) && (ignoreDirty == false || this._dirtyDynamicBones.ContainsKey(bone) == false))
                        return bone;
                }
                else
                {
                    if ((bone.transform.GetPathFrom(this._parent.transform).Equals(root) || bone.name.Equals(root)) && (ignoreDirty == false || this._dirtyDynamicBones.ContainsKey(bone) == false))
                        return bone;
                }
            }
            return null;
        }

        public string CalculateRoot(DynamicBone db)
        {
            return db.m_Root != null ? db.m_Root.GetPathFrom(this._parent.transform) : db.transform.GetPathFrom(this._parent.transform);
        }
        #endregion

        #region Private Methods
        private void UpdateDynamicBone(DynamicBone db)
        {
            db.InitTransforms();
            DynamicBoneData data;
            if (this._dirtyDynamicBones.TryGetValue(db, out data) && data.originalGravity.hasValue)
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
            if (this.GizmosEnabled() == false)
                return;
            this.DynamicBoneDraggingLogic();
            _debugLines.Draw(this._dynamicBones, this._dynamicBoneTarget, this._dirtyDynamicBones);
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
                this.NotifyDynamicBoneForUpdate(db);
                this._dirtyDynamicBones.Add(db, data);
            }
            return loaded;
        }

        private void ResetAll()
        {
            foreach (DynamicBone bone in this._dynamicBones)
                this.SetDynamicBoneNotDirty(bone);
        }

        private void CopyToFK()
        {
            this._preDragAction = () =>
            {
                List<GuideCommand.EqualsInfo> infos = new List<GuideCommand.EqualsInfo>();
                foreach (DynamicBone bone in this._dynamicBones)
                {
                    foreach (object o in (IList)bone.GetPrivate("m_Particles"))
                    {
                        Transform t = (Transform)o.GetPrivate("m_Transform");
                        OCIChar.BoneInfo boneInfo;
                        if (t != null && this._target.fkObjects.TryGetValue(t.gameObject, out boneInfo))
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
            if (this._dynamicBones.Contains(bone) && this._dirtyDynamicBones.TryGetValue(bone, out data))
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
                this._dirtyDynamicBones.Remove(bone);
                this.NotifyDynamicBoneForUpdate(bone);
            }
        }

        private DynamicBoneData SetDynamicBoneDirty(DynamicBone bone)
        {
            DynamicBoneData data;
            if (this._dirtyDynamicBones.TryGetValue(bone, out data) == false)
            {
                data = new DynamicBoneData();
                this._dirtyDynamicBones.Add(bone, data);
            }
            return data;
        }

        private bool IsDynamicBoneDirty(DynamicBone bone)
        {
            return this._dirtyDynamicBones.ContainsKey(bone);
        }

        private void NotifyDynamicBoneForUpdate(DynamicBone bone)
        {
            if (this._toUpdate.Contains(bone) == false)
                this._toUpdate.Add(bone);
        }

        private void DynamicBoneDraggingLogic()
        {
            if (this._preDragAction != null)
                this._preDragAction();
            this._preDragAction = null;
            if (Input.GetMouseButtonDown(0))
            {
                float distanceFromCamera = float.PositiveInfinity;
                for (int i = 0; i < this._dynamicBones.Count; i++)
                {
                    DynamicBone db = this._dynamicBones[i];
                    Transform leaf = (db.m_Root ?? db.transform).GetFirstLeaf();
                    Vector3 raycastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(leaf.position - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                    if ((raycastPos - leaf.position).sqrMagnitude < (_dynamicBonesDragRadius * _dynamicBonesDragRadius) &&
                        (raycastPos - Camera.main.transform.position).sqrMagnitude < distanceFromCamera)
                    {
                        this.isDraggingDynamicBone = true;
                        distanceFromCamera = (raycastPos - Camera.main.transform.position).sqrMagnitude;
                        this._dragDynamicBoneStartPosition = raycastPos;
                        this._lastDynamicBoneGravity = db.m_Force;
                        this._draggedDynamicBone = db;
                        this._dynamicBoneTarget = db;
                    }
                }
            }
            else if (Input.GetMouseButton(0) && this.isDraggingDynamicBone)
            {
                this._dragDynamicBoneEndPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._dragDynamicBoneStartPosition - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                this.SetDynamicBoneForce(this._draggedDynamicBone, this._lastDynamicBoneGravity + (this._dragDynamicBoneEndPosition - this._dragDynamicBoneStartPosition) * (this._inc * 1000f) / 12f);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                this.isDraggingDynamicBone = false;
            }
        }

        private void RefreshDynamicBoneList()
        {
            this._isBusy = true;
            DynamicBone[] dynamicBones = this._parent.GetComponentsInChildren<DynamicBone>(true);
            List<DynamicBone> toDelete = null;
            foreach (DynamicBone db in this._dynamicBones)
            {
                if (dynamicBones.Contains(db) == false)
                {
                    if (toDelete == null)
                        toDelete = new List<DynamicBone>();
                    toDelete.Add(db);
                }
            }
            foreach (KeyValuePair<DynamicBone, DynamicBoneData> pair in new Dictionary<DynamicBone, DynamicBoneData>(this._dirtyDynamicBones)) //Putting every dirty one into headless, that should help
            {
                if (pair.Key != null)
                {
                    this._headlessDirtyDynamicBones.Add(pair.Key.m_Root != null ? pair.Key.m_Root : pair.Key.transform, new DynamicBoneData(pair.Value));
                    this._headlessReconstructionTimeout = 5;
                    this.SetDynamicBoneNotDirty(pair.Key);
                }
            }
            this._dirtyDynamicBones.Clear();
            if (toDelete != null)
            {
                foreach (DynamicBone db in toDelete)
                    this._dynamicBones.Remove(db);
            }
            List<DynamicBone> toAdd = null;
            foreach (DynamicBone db in dynamicBones)
            {
                if (this._dynamicBones.Contains(db) == false && this._parent._childObjects.All(child => db.transform.IsChildOf(child.transform) == false))
                {
                    if (toAdd == null)
                        toAdd = new List<DynamicBone>();
                    toAdd.Add(db);
                }
            }
            if (toAdd != null)
            {
                foreach (DynamicBone db in toAdd)
                    this._dynamicBones.Add(db);
            }
            if (this._dynamicBones.Count != 0 && this._dynamicBoneTarget == null)
                this._dynamicBoneTarget = this._dynamicBones.FirstOrDefault(d => d.m_Root != null);
            foreach (KeyValuePair<DynamicBoneColliderBase, CollidersEditor> pair in CollidersEditor._loneColliders)
            {
                HashSet<object> ignoredDynamicBones;
                if (pair.Value._dirtyColliders.TryGetValue(pair.Key, out CollidersEditor.ColliderDataBase data) == false || data.ignoredDynamicBones.TryGetValue(this._parent, out ignoredDynamicBones) == false)
                    ignoredDynamicBones = null;
                foreach (DynamicBone bone in this._dynamicBones)
                {
                    if (ignoredDynamicBones != null && ignoredDynamicBones.Contains(bone)) // Should be ignored
                    {
                        if (bone.m_Colliders.Contains(pair.Key))
                        {
                            bone.m_Colliders.Remove(pair.Key);
                            this.NotifyDynamicBoneForUpdate(bone);
                        }
                    }
                    else
                    {
                        if (bone.m_Colliders.Contains(pair.Key) == false)
                        {
                            bone.m_Colliders.Add(pair.Key);
                            this.NotifyDynamicBoneForUpdate(bone);
                        }
                    }
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
            return this._isEnabled && PoseController._drawAdvancedMode;
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
                        if (this._dynamicBone == null)
                            this._dynamicBone = this.editor.SearchDynamicBone(this.dynamicBoneRootPath);
                        return this._dynamicBone;
                    }
                }
                private DynamicBone _dynamicBone;
                private readonly int _hashCode;
                
                public Parameter(DynamicBonesEditor editor, DynamicBone dynamicBone)
                {
                    this.editor = editor;
                    this._dynamicBone = dynamicBone;
                    this.dynamicBoneRootPath = this.editor.CalculateRoot(this._dynamicBone);

                    unchecked
                    {
                        int hash = 17;
                        hash = hash * 31 + (this.editor != null ? this.editor.GetHashCode() : 0);
                        this._hashCode = hash * 31 + (this.dynamicBoneRootPath != null ? this.dynamicBoneRootPath.GetHashCode() : 0);
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
                        this._hashCode = hash * 31 + (this.dynamicBoneRootPath != null ? this.dynamicBoneRootPath.GetHashCode() : 0);
                    }
                }

                public override int GetHashCode()
                {
                    return this._hashCode;
                }

                public override string ToString()
                {
                    return $"editor: [{this.editor}], dynamicBone: [{this._dynamicBone}], dynamicBoneRootPath: [{this.dynamicBoneRootPath}], hashCode: [{this._hashCode}]";
                }
            }


            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
                        owner: HSPE._name,
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
