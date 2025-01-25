using System;
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
    public class CollidersEditor : AdvancedModeModule
    {
        #region Constants
        internal static readonly Color _colliderColor = Color.Lerp(_greenColor, Color.white, 0.5f);
        private static readonly HashSet<string> _loneColliderNames = new HashSet<string>
        {
                "Collider",
#if AISHOUJO || HONEYSELECT2
                "ColliderPlane"
#endif
        };
        #endregion

        #region Private Types
        private class ColliderDebugLines
        {
            private readonly List<VectorLine> _centerCircles;
            private readonly List<VectorLine> _capsCircles;
            private readonly List<VectorLine> _centerLines;
            private readonly List<VectorLine> _capsLines;

            public ColliderDebugLines()
            {
                const float radius = 1f;
                const float num = 2f * 0.5f - 1f;
                Vector3 position1 = Vector3.zero;
                Vector3 position2 = Vector3.zero;
                position1.x -= num;
                position2.x += num;
                Quaternion orientation = Quaternion.AngleAxis(90f, Vector3.up);
                Vector3 dir = Vector3.right;
                _centerCircles = new List<VectorLine>();
                for (int i = 1; i < 10; ++i)
                {
                    VectorLine circle = VectorLine.SetLine(CollidersEditor._colliderColor, new Vector3[37]);
                    circle.MakeCircle(Vector3.Lerp(position1, position2, i / 10f), dir, radius);
                    _centerCircles.Add(circle);
                }
                _centerLines = new List<VectorLine>();
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                    _centerLines.Add(VectorLine.SetLine(CollidersEditor._colliderColor, position1 + offset, position2 + offset));
                }
                Vector3[] prev = new Vector3[8];
                Vector3 prevCenter1 = Vector3.zero;
                Vector3 prevCenter2 = Vector3.zero;
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    prev[i] = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                }
                _capsCircles = new List<VectorLine>();
                _capsLines = new List<VectorLine>();
                for (int i = 0; i < 6; ++i)
                {
                    float v = (i / 5f) * 0.95f;
                    float angle = Mathf.Asin(v);
                    float radius2 = radius * Mathf.Cos(angle);
                    Vector3 center1 = position1 - dir * v * radius;
                    Vector3 center2 = position2 + dir * v * radius;
                    VectorLine circle = VectorLine.SetLine(CollidersEditor._colliderColor, new Vector3[37]);
                    circle.MakeCircle(center1, dir, radius2);
                    _capsCircles.Add(circle);

                    circle = VectorLine.SetLine(CollidersEditor._colliderColor, new Vector3[37]);
                    circle.MakeCircle(center2, dir, radius2);
                    _capsCircles.Add(circle);

                    if (i != 0)
                        for (int j = 0; j < 8; ++j)
                        {
                            float angle2 = 360 * (j / 8f) * Mathf.Deg2Rad;
                            Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2))) * radius2;
                            _capsLines.Add(VectorLine.SetLine(CollidersEditor._colliderColor, prevCenter1 + prev[j], center1 + offset));
                            _capsLines.Add(VectorLine.SetLine(CollidersEditor._colliderColor, prevCenter2 + prev[j], center2 + offset));
                            prev[j] = offset;
                        }
                    prevCenter1 = center1;
                    prevCenter2 = center2;
                }
                foreach (VectorLine line in _centerCircles)
                    line.lineWidth = 2f;
                foreach (VectorLine line in _capsCircles)
                    line.lineWidth = 2f;
                foreach (VectorLine line in _centerLines)
                    line.lineWidth = 2f;
                foreach (VectorLine line in _capsLines)
                    line.lineWidth = 2f;
            }

            public void Update(DynamicBoneCollider collider)
            {
                float radius = collider.m_Radius * Mathf.Abs(collider.transform.lossyScale.x);
                float num = collider.m_Height * 0.5f - collider.m_Radius;
                Vector3 position1 = collider.m_Center;
                Vector3 position2 = collider.m_Center;
                Quaternion orientation = Quaternion.identity;
                Vector3 dir = Vector3.zero;
                switch (collider.m_Direction)
                {
                    case DynamicBoneCollider.Direction.X:
                        position1.x -= num;
                        position2.x += num;
                        orientation = collider.transform.rotation * Quaternion.AngleAxis(90f, Vector3.up);
                        dir = Vector3.right;
                        break;
                    case DynamicBoneCollider.Direction.Y:
                        position1.y -= num;
                        position2.y += num;
                        orientation = collider.transform.rotation * Quaternion.AngleAxis(90f, Vector3.right);
                        dir = Vector3.up;
                        break;
                    case DynamicBoneCollider.Direction.Z:
                        position1.z -= num;
                        position2.z += num;
                        orientation = collider.transform.rotation;
                        dir = Vector3.forward;
                        break;
                }
                position1 = collider.transform.TransformPoint(position1);
                position2 = collider.transform.TransformPoint(position2);
                dir = collider.transform.TransformDirection(dir);
                for (int i = 0; i < 9; ++i)
                {
                    _centerCircles[i].MakeCircle(Vector3.Lerp(position1, position2, (i + 1) / 10f), dir, radius);
                }
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                    VectorLine line = _centerLines[i];
                    line.points3[0] = position1 + offset;
                    line.points3[1] = position2 + offset;
                }

                Vector3[] prev = new Vector3[8];
                Vector3 prevCenter1 = Vector3.zero;
                Vector3 prevCenter2 = Vector3.zero;
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    prev[i] = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                }
                int k = 0;
                int l = 0;
                for (int i = 0; i < 6; ++i)
                {
                    float v = (i / 5f) * 0.95f;
                    float angle = Mathf.Asin(v);
                    float radius2 = radius * Mathf.Cos(angle);
                    Vector3 center1 = position1 - dir * v * radius;
                    Vector3 center2 = position2 + dir * v * radius;
                    VectorLine circle = _capsCircles[k++];
                    circle.MakeCircle(center1, dir, radius2);

                    circle = _capsCircles[k++];
                    circle.MakeCircle(center2, dir, radius2);

                    if (i != 0)
                        for (int j = 0; j < 8; ++j)
                        {
                            float angle2 = 360 * (j / 8f) * Mathf.Deg2Rad;
                            Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2))) * radius2;
                            VectorLine line = _capsLines[l++];
                            line.points3[0] = prevCenter1 + prev[j];
                            line.points3[1] = center1 + offset;

                            line = _capsLines[l++];
                            line.points3[0] = prevCenter2 + prev[j];
                            line.points3[1] = center2 + offset;

                            prev[j] = offset;
                        }
                    prevCenter1 = center1;
                    prevCenter2 = center2;
                }
            }

            public void Draw()
            {
                for (int i = 0; i < 9; ++i)
                {
                    _centerCircles[i].Draw();
                }
                for (int i = 0; i < 8; ++i)
                {
                    VectorLine line = _centerLines[i];
                    line.Draw();
                }

                int k = 0;
                int l = 0;
                for (int i = 0; i < 6; ++i)
                {
                    VectorLine circle = _capsCircles[k++];
                    circle.Draw();

                    circle = _capsCircles[k++];
                    circle.Draw();

                    if (i != 0)
                        for (int j = 0; j < 8; ++j)
                        {
                            VectorLine line = _capsLines[l++];
                            line.Draw();

                            line = _capsLines[l++];
                            line.Draw();
                        }
                }
            }

            public void SetActive(bool active)
            {
                foreach (VectorLine line in _centerCircles)
                    line.active = active;
                foreach (VectorLine line in _capsCircles)
                    line.active = active;
                foreach (VectorLine line in _centerLines)
                    line.active = active;
                foreach (VectorLine line in _capsLines)
                    line.active = active;
            }

            public void Destroy()
            {
                VectorLine.Destroy(_capsLines);
                VectorLine.Destroy(_centerLines);
                VectorLine.Destroy(_capsCircles);
                VectorLine.Destroy(_centerCircles);
            }
        }
#if AISHOUJO || HONEYSELECT2
        private class ColliderPlaneDebugLines
        {
            public VectorLine leftLine;
            //public readonly VectorLine rightLine;
            //public readonly VectorLine topLine;
            //public readonly VectorLine bottomLine;

            public ColliderPlaneDebugLines()
            {
                this.leftLine = VectorLine.SetLine(_colliderColor, Vector3.zero, new Vector3(0f, 1f, 0f));
                this.leftLine.endCap = "vector";
                this.leftLine.lineWidth = 2f;
                //this.topLine = VectorLine.SetLine(_colliderColor, new Vector3(0f, 1f, 0f), Vector3.one);
                //this.rightLine = VectorLine.SetLine(_colliderColor, Vector3.one, new Vector3(1f, 0f, 0f));
                //this.bottomLine = VectorLine.SetLine(_colliderColor, new Vector3(1f, 0f, 0f), Vector3.zero);
            }

            public void Update(DynamicBonePlaneCollider collider)
            {
                Vector3 vector3 = Vector3.up;
                switch (collider.m_Direction)
                {
                    case DynamicBoneCollider.Direction.X:
                        vector3 = collider.transform.right;
                        break;
                    case DynamicBoneCollider.Direction.Y:
                        vector3 = collider.transform.up;
                        break;
                    case DynamicBoneCollider.Direction.Z:
                        vector3 = collider.transform.forward;
                        break;
                }
                Vector3 from = collider.transform.TransformPoint(collider.m_Center);
                this.leftLine.points3[0] = from;
                this.leftLine.points3[1] = from + vector3;
            }

            public void Draw()
            {
                this.leftLine.Draw();
            }

            public void SetActive(bool active)
            {
                this.leftLine.active = active;
            }

            public void Destroy()
            {
                VectorLine.Destroy(ref this.leftLine);
            }
        }
#endif

        internal class ColliderDataBase
        {
            public EditableValue<Vector3> originalCenter;
            public EditableValue<DynamicBoneCollider.Direction> originalDirection;
            public EditableValue<DynamicBoneCollider.Bound> originalBound;
            public Dictionary<PoseController, Dictionary<object, bool>> allDynamicBones = new Dictionary<PoseController, Dictionary<object, bool>>();

            public virtual bool hasValue { get { return originalCenter.hasValue || originalDirection.hasValue || originalBound.hasValue; } }

            public ColliderDataBase()
            {

            }

            public ColliderDataBase(ColliderDataBase other)
            {
                originalCenter = other.originalCenter;
                originalDirection = other.originalDirection;
                originalBound = other.originalBound;

                foreach (KeyValuePair<PoseController, Dictionary<object, bool>> pair in other.allDynamicBones)
                {

                    if (allDynamicBones.TryGetValue(pair.Key, out var dbs) == false)
                    {
                        dbs = new Dictionary<object, bool>();
                        allDynamicBones.Add(pair.Key, dbs);
                    }

                    foreach (KeyValuePair<object, bool> db in pair.Value)
                    {
                        if (dbs.Contains(db) == false)
                            dbs.Add(db.Key, db.Value);
                    }
                }
            }

            public virtual void ResetWithParent(DynamicBoneColliderBase collider)
            {
                if (originalCenter.hasValue)
                {
                    collider.m_Center = originalCenter;
                    originalCenter.Reset();
                }
                if (originalDirection.hasValue)
                {
                    collider.m_Direction = originalDirection;
                    originalDirection.Reset();
                }
                if (originalBound.hasValue)
                {
                    collider.m_Bound = originalBound;
                    originalBound.Reset();
                }
                DynamicBoneCollider normalCollider = collider as DynamicBoneCollider;
                //Set all the dynamic bones to enable
                foreach (KeyValuePair<PoseController, Dictionary<object, bool>> pair in allDynamicBones)
                {
                    List<object> dicToList = pair.Value.Keys.ToList();
                    foreach (object key in dicToList)
                        pair.Value[key] = true;

                    foreach (var kvp in pair.Value)
                    {
                        object dynamicBone = kvp.Key;
                        if (dynamicBone is DynamicBone db)
                        {
                            List<DynamicBoneColliderBase> colliders = db.m_Colliders;
                            if (colliders.Contains(collider) == false)
                                colliders.Add(collider);
                        }
                        else if (normalCollider != null)
                        {
                            List<DynamicBoneCollider> colliders = ((DynamicBone_Ver02)dynamicBone).Colliders;
                            if (colliders.Contains(normalCollider) == false)
                                colliders.Add(normalCollider);
                        }
                    }
                }
            }
        }

        private class ColliderData : ColliderDataBase
        {
            public EditableValue<float> originalRadius;
            public EditableValue<float> originalHeight;

            public override bool hasValue { get { return base.hasValue || originalRadius.hasValue || originalHeight.hasValue; } }

            public ColliderData()
            {
            }

            public ColliderData(ColliderData other) : base(other)
            {
                originalRadius = other.originalRadius;
                originalHeight = other.originalHeight;
            }

            public override void ResetWithParent(DynamicBoneColliderBase collider)
            {
                base.ResetWithParent(collider);
                DynamicBoneCollider normalCollider = (DynamicBoneCollider)collider;
                if (originalRadius.hasValue)
                {
                    normalCollider.m_Radius = originalRadius;
                    originalRadius.Reset();
                }
                if (originalHeight.hasValue)
                {
                    normalCollider.m_Height = originalHeight;
                    originalHeight.Reset();
                }

            }
        }

#if AISHOUJO || HONEYSELECT2
        private class ColliderPlaneData : ColliderDataBase
        {
            public ColliderPlaneData()
            {

            }

            public ColliderPlaneData(ColliderPlaneData other) : base(other)
            {

            }
        }
#endif
        #endregion

        #region Private Variables
        internal static readonly Dictionary<DynamicBoneColliderBase, CollidersEditor> _loneColliders = new Dictionary<DynamicBoneColliderBase, CollidersEditor>();
        private static ColliderDebugLines _colliderDebugLines;
#if AISHOUJO || HONEYSELECT2
        private static ColliderPlaneDebugLines _colliderPlaneDebugLines;
#endif
        private static readonly string[] _directionNames;
        private static readonly string[] _boundNames;

        private readonly GenericOCITarget _target;
        private Vector2 _collidersEditionScroll;
        internal readonly Dictionary<Transform, DynamicBoneColliderBase> _colliders = new Dictionary<Transform, DynamicBoneColliderBase>();
        internal readonly bool _isLoneCollider = false;
        private DynamicBoneColliderBase _colliderTarget;
#if AISHOUJO || HONEYSELECT2
        private bool _isTargetNormalCollider = true;
#endif
        internal readonly Dictionary<DynamicBoneColliderBase, ColliderDataBase> _dirtyColliders = new Dictionary<DynamicBoneColliderBase, ColliderDataBase>();
        private Vector2 _ignoredDynamicBonesScroll;
        private string _ignoredDynamicBonesFilter = "";
        #endregion

        #region Public Variables
        public bool _addNewDynamicBonesAsDefault = true;
        #endregion

        #region Public Accessors
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.CollidersEditor; } }
        public override string displayName { get { return "Colliders"; } }
        public override bool isEnabled
        {
            set
            {
                base.isEnabled = value;
                UpdateDebugLinesState(this);
            }
        }
        public override bool shouldDisplay { get { return _colliders.Count > 0; } }
        #endregion

        #region Unity Methods
        static CollidersEditor()
        {
            _directionNames = Enum.GetNames(typeof(DynamicBoneColliderBase.Direction));
            _boundNames = Enum.GetNames(typeof(DynamicBoneColliderBase.Bound));
        }

        public CollidersEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            _target = target;

            if (_loneColliderNames.Contains(_parent.name))
            {
                Transform colliderTransform = _parent.transform.Find("Collider");
                if (colliderTransform != null)
                {
                    DynamicBoneColliderBase collider = colliderTransform.GetComponent<DynamicBoneColliderBase>();
                    if (collider != null && _loneColliders.ContainsKey(collider) == false)
                    {
                        _parent.onUpdate += Update;
                        _isLoneCollider = true;
                        _loneColliders.Add(collider, this);
                        foreach (DynamicBone bone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                        {
                            if (bone.m_Colliders.Contains(collider) == false)
                                bone.m_Colliders.Add(collider);
                        }
                        if (collider is DynamicBoneCollider)
                        {
                            DynamicBoneCollider normalCollider = (DynamicBoneCollider)collider;
                            foreach (DynamicBone_Ver02 bone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                            {
                                if (bone.Colliders.Contains(normalCollider) == false)
                                    bone.Colliders.Add(normalCollider);
                            }
                        }
                    }
                }
            }

            foreach (DynamicBoneColliderBase c in _parent.GetComponentsInChildren<DynamicBoneColliderBase>(true))
                if (_colliders.ContainsKey(c.transform) == false)
                    _colliders.Add(c.transform, c);
            _colliderTarget = _colliders.FirstOrDefault().Value;
#if AISHOUJO || HONEYSELECT2
            this._isTargetNormalCollider = this._colliderTarget is DynamicBoneCollider;
#endif
            if (_colliderDebugLines == null)
            {
                _colliderDebugLines = new ColliderDebugLines();
                _colliderDebugLines.SetActive(false);
#if AISHOUJO || HONEYSELECT2
                _colliderPlaneDebugLines = new ColliderPlaneDebugLines();
                _colliderPlaneDebugLines.SetActive(false);
#endif
            }

            _incIndex = -2;
        }

        private void Update()
        {
            foreach (KeyValuePair<DynamicBoneColliderBase, ColliderDataBase> colliderPair in _dirtyColliders)
            {
                Dictionary<PoseController, Dictionary<object, bool>> newDynamicBones = null;
                foreach (KeyValuePair<PoseController, Dictionary<object, bool>> pair in colliderPair.Value.allDynamicBones)
                {
                    if (pair.Key == null || pair.Value == null || pair.Value.Count == 0 || pair.Value.Any(o => o.Key == null))
                    {
                        newDynamicBones = new Dictionary<PoseController, Dictionary<object, bool>>();
                        break;
                    }
                }
                if (newDynamicBones != null)
                {
                    foreach (KeyValuePair<PoseController, Dictionary<object, bool>> pair in colliderPair.Value.allDynamicBones)
                    {
                        if (pair.Key != null)
                        {
                            Dictionary<object, bool> newValue;
                            if (pair.Value.Count != 0)
                            {
                                newValue = new Dictionary<object, bool>();
                                foreach (var kvp in pair.Value)
                                {
                                    if (kvp.Key != null)
                                        newValue.Add(kvp.Key, kvp.Value);
                                }
                            }
                            else
                                newValue = pair.Value;

                            newDynamicBones.Add(pair.Key, newValue);
                        }
                    }
                    colliderPair.Value.allDynamicBones = newDynamicBones;
                }
            }
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_isLoneCollider)
            {
                DynamicBoneColliderBase collider = _parent.transform.GetChild(0).GetComponent<DynamicBoneColliderBase>();
                _loneColliders.Remove(collider);
                foreach (DynamicBone bone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                {
                    if (bone.m_Colliders.Contains(collider))
                        bone.m_Colliders.Remove(collider);
                }
#if AISHOUJO || HONEYSELECT2
                if (collider is DynamicBoneCollider)
#endif
                {
                    DynamicBoneCollider normalCollider = (DynamicBoneCollider)collider;
                    foreach (DynamicBone_Ver02 bone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                    {
                        if (bone.Colliders.Contains(normalCollider))
                            bone.Colliders.Remove(normalCollider);
                    }
                }
                _parent.onUpdate -= Update;
            }
        }

        #endregion

        #region Public Methods
        public override void DrawAdvancedModeChanged()
        {
            UpdateDebugLinesState(this);
        }

        public static void SelectionChanged(CollidersEditor self)
        {
            UpdateDebugLinesState(self);
        }

        public override void GUILogic()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            _collidersEditionScroll = GUILayout.BeginScrollView(_collidersEditionScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            Color c;
            foreach (KeyValuePair<Transform, DynamicBoneColliderBase> pair in _colliders)
            {
                if (pair.Key == null)
                    continue;
                c = GUI.color;
                if (IsColliderDirty(pair.Value))
                    GUI.color = Color.magenta;
                if (pair.Value == _colliderTarget)
                    GUI.color = Color.cyan;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(pair.Value.name + (IsColliderDirty(pair.Value) ? "*" : "")))
                {
                    _colliderTarget = pair.Value;
#if AISHOUJO || HONEYSELECT2
                    this._isTargetNormalCollider = this._colliderTarget is DynamicBoneCollider;
#endif
                }
                GUILayout.Space(GUI.skin.verticalScrollbar.fixedWidth);
                GUILayout.EndHorizontal();
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            {
                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset all"))
                    ResetAll();
                GUI.color = c;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);

#if AISHOUJO || HONEYSELECT2
            if (this._isTargetNormalCollider)
#endif
            {
                DrawFields((DynamicBoneCollider)_colliderTarget);
            }
#if AISHOUJO || HONEYSELECT2
            else
                this.DrawFields((DynamicBonePlaneCollider)this._colliderTarget);
#endif

            if (_isLoneCollider)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Affected Dynamic Bones", GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();

                bool addNewDbDefault = GUILayout.Toggle(_addNewDynamicBonesAsDefault, "Enable New Dynamic Bones", GUILayout.ExpandWidth(false));
                if (addNewDbDefault != _addNewDynamicBonesAsDefault)
                {
                    _addNewDynamicBonesAsDefault = addNewDbDefault;
                    SetColliderDirty(_colliderTarget);
                }

                if (GUILayout.Button("All off", GUILayout.ExpandWidth(false)))
                    SetIgnoreAllDynamicBones(_colliderTarget, true);
                if (GUILayout.Button("All on", GUILayout.ExpandWidth(false)))
                    SetIgnoreAllDynamicBones(_colliderTarget, false);
                GUILayout.EndHorizontal();
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Search : ", GUILayout.ExpandWidth(false));
                    _ignoredDynamicBonesFilter = GUILayout.TextField(_ignoredDynamicBonesFilter);
                    if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                        _ignoredDynamicBonesFilter = "";
                    GUILayout.EndHorizontal();

                    ColliderDataBase cd;
                    if (_dirtyColliders.TryGetValue(_colliderTarget, out cd) == false)
                        cd = null;

                    _ignoredDynamicBonesScroll = GUILayout.BeginScrollView(_ignoredDynamicBonesScroll);
                    foreach (PoseController controller in PoseController._poseControllers)
                    {
                        if (controller == null || controller._dynamicBonesEditor == null || controller._dynamicBonesEditor._dynamicBones == null || controller.target == null)
                            continue;

                        int total = controller._dynamicBonesEditor._dynamicBones.Where(d => d != null && d.m_Root != null && d.m_Root.name.IndexOf(_ignoredDynamicBonesFilter, StringComparison.OrdinalIgnoreCase) != -1).Count();
                        CharaPoseController charaPoseController = null;
                        if (CharaPoseController._charaPoseControllers.Contains(controller))
                        {
                            charaPoseController = (CharaPoseController)controller;
                            if (charaPoseController._boobsEditor != null)
                                total += charaPoseController._boobsEditor._dynamicBones.Where(d => d != null && d.Root != null && d.Root.name.IndexOf(_ignoredDynamicBonesFilter, StringComparison.OrdinalIgnoreCase) != -1).Count();
                        }
                        if (total == 0)
                            continue;

                        if (cd == null || cd.allDynamicBones.TryGetValue(controller, out Dictionary<object, bool> dbDic) == false)
                            dbDic = null;

                        GUILayout.BeginVertical(GUI.skin.box);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(controller.target.oci.treeNodeObject.textName + " (" + controller.target.oci.guideObject.transformTarget.name + ") ", GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Center camera on", GUILayout.ExpandWidth(false)))
                            Studio.Studio.Instance.cameraCtrl.targetPos = controller.target.oci.guideObject.transformTarget.position;
                        if (GUILayout.Button("All off", GUILayout.ExpandWidth(false)))
                        {
                            foreach (DynamicBone dynamicBone in controller._dynamicBonesEditor._dynamicBones)
                                SetIgnoreDynamicBone(_colliderTarget, controller, dynamicBone, true);

                            if (
#if AISHOUJO || HONEYSELECT2
                                this._isTargetNormalCollider &&
#endif
                                charaPoseController != null &&
                                charaPoseController._boobsEditor != null
                                )
                            {
                                foreach (DynamicBone_Ver02 dynamicBone in charaPoseController._boobsEditor._dynamicBones)
                                    SetIgnoreDynamicBone(_colliderTarget, controller, dynamicBone, true);

                            }
                        }
                        if (GUILayout.Button("All on", GUILayout.ExpandWidth(false)))
                        {
                            foreach (DynamicBone dynamicBone in controller._dynamicBonesEditor._dynamicBones)
                                SetIgnoreDynamicBone(_colliderTarget, controller, dynamicBone, false);

                            if (
#if AISHOUJO || HONEYSELECT2
                                this._isTargetNormalCollider &&
#endif
                                charaPoseController != null &&
                                charaPoseController._boobsEditor != null
                                )
                            {
                                foreach (DynamicBone_Ver02 dynamicBone in charaPoseController._boobsEditor._dynamicBones)
                                    SetIgnoreDynamicBone(_colliderTarget, controller, dynamicBone, false);

                            }
                        }
                        GUILayout.EndHorizontal();

                        int i = 1;
                        GUILayout.BeginHorizontal();
                        foreach (DynamicBone dynamicBone in controller._dynamicBonesEditor._dynamicBones)
                        {
                            if (dynamicBone == null || dynamicBone.m_Root == null || dynamicBone.m_Root.name.IndexOf(_ignoredDynamicBonesFilter, StringComparison.OrdinalIgnoreCase) == -1)
                                continue;

                            bool e;
                            if (dbDic == null)
                                e = true;
                            else
                            {
                                if (dbDic.TryGetValue(dynamicBone, out e) == false)
                                    dbDic.Add(dynamicBone, _addNewDynamicBonesAsDefault);

                                e = dbDic[dynamicBone];
                            }

                            bool newE = GUILayout.Toggle(e, dynamicBone.m_Root.name);
                            if (e != newE)
                                SetIgnoreDynamicBone(_colliderTarget, controller, dynamicBone, !newE);
                            if (i % 3 == 0)
                            {
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                            }
                            ++i;
                        }

                        if (
#if AISHOUJO || HONEYSELECT2
                                this._isTargetNormalCollider &&
#endif
                                charaPoseController != null &&
                                charaPoseController._boobsEditor != null
                                )
                        {
                            foreach (DynamicBone_Ver02 dynamicBone in charaPoseController._boobsEditor._dynamicBones)
                            {
                                if (dynamicBone == null || dynamicBone.Root == null || dynamicBone.Root.name.IndexOf(_ignoredDynamicBonesFilter, StringComparison.OrdinalIgnoreCase) == -1)
                                    continue;

                                bool e;
                                if (dbDic == null)
                                    e = true;
                                else
                                {
                                    if (dbDic.TryGetValue(dynamicBone, out e) == false)
                                        dbDic.Add(dynamicBone, _addNewDynamicBonesAsDefault);

                                    e = dbDic[dynamicBone];
                                }

                                bool newE = GUILayout.Toggle(e, dynamicBone.Root.name);
                                if (e != newE)
                                    SetIgnoreDynamicBone(_colliderTarget, controller, dynamicBone, !newE);
                                if (i % 3 == 0)
                                {
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                }
                                ++i;
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Go to bone", GUILayout.ExpandWidth(false)))
            {
                _parent.EnableModule(_parent._bonesEditor);
                _parent._bonesEditor.GoToObject(_colliderTarget.gameObject);
            }
            GUILayout.FlexibleSpace();
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                SetColliderNotDirty(_colliderTarget);
            GUI.color = c;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        public void LoadFrom(CollidersEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<DynamicBoneColliderBase, ColliderDataBase> kvp in other._dirtyColliders)
                {
                    Transform obj = _parent.transform.Find(kvp.Key.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                    {
                        DynamicBoneColliderBase col = obj.GetComponent<DynamicBoneColliderBase>();
                        ColliderDataBase newData;
#if AISHOUJO || HONEYSELECT2
                        if (col is DynamicBoneCollider)
#endif
                        {
                            DynamicBoneCollider collider = (DynamicBoneCollider)col;
                            DynamicBoneCollider otherCollider = (DynamicBoneCollider)kvp.Key;
                            ColliderData otherData = (ColliderData)kvp.Value;
                            newData = new ColliderData(otherData);

                            if (otherData.originalHeight.hasValue)
                                collider.m_Height = otherCollider.m_Height;
                            if (otherData.originalRadius.hasValue)
                                collider.m_Radius = otherCollider.m_Radius;
                        }
#if AISHOUJO || HONEYSELECT2
                        else
                            newData = new ColliderPlaneData((ColliderPlaneData)kvp.Value);
#endif
                        if (kvp.Value.originalCenter.hasValue)
                            col.m_Center = kvp.Key.m_Center;
                        if (kvp.Value.originalBound.hasValue)
                            col.m_Bound = kvp.Key.m_Bound;
                        if (kvp.Value.originalDirection.hasValue)
                            col.m_Direction = kvp.Key.m_Direction;

                        foreach (KeyValuePair<PoseController, Dictionary<object, bool>> pair in kvp.Value.allDynamicBones)
                            foreach (var pair2 in pair.Value)
                                SetIgnoreDynamicBone(col, pair.Key, pair2.Key, !pair2.Value);

                        _dirtyColliders[col] = newData;
                    }
                }
                _collidersEditionScroll = other._collidersEditionScroll;
                _addNewDynamicBonesAsDefault = other._addNewDynamicBonesAsDefault;
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (_dirtyColliders.Count != 0)
            {
                xmlWriter.WriteStartElement("colliders");
                foreach (KeyValuePair<DynamicBoneColliderBase, ColliderDataBase> kvp in _dirtyColliders)
                {
                    string n = kvp.Key.transform.GetPathFrom(_parent.transform);
                    xmlWriter.WriteStartElement("collider");
                    xmlWriter.WriteAttributeString("name", n);
                    bool isNormalCollider = kvp.Key is DynamicBoneCollider;
                    xmlWriter.WriteAttributeString("isNormalCollider", XmlConvert.ToString(isNormalCollider));

                    if (kvp.Value.originalCenter.hasValue)
                    {
                        xmlWriter.WriteAttributeString("centerX", XmlConvert.ToString(kvp.Key.m_Center.x));
                        xmlWriter.WriteAttributeString("centerY", XmlConvert.ToString(kvp.Key.m_Center.y));
                        xmlWriter.WriteAttributeString("centerZ", XmlConvert.ToString(kvp.Key.m_Center.z));
                    }
                    if (isNormalCollider)
                    {
                        DynamicBoneCollider col = (DynamicBoneCollider)kvp.Key;
                        ColliderData data = (ColliderData)kvp.Value;
                        if (data.originalRadius.hasValue)
                            xmlWriter.WriteAttributeString("radius", XmlConvert.ToString(col.m_Radius));
                        if (data.originalHeight.hasValue)
                            xmlWriter.WriteAttributeString("height", XmlConvert.ToString(col.m_Height));
                    }
                    if (kvp.Value.originalDirection.hasValue)
                        xmlWriter.WriteAttributeString("direction", XmlConvert.ToString((int)kvp.Key.m_Direction));
                    if (kvp.Value.originalBound.hasValue)
                        xmlWriter.WriteAttributeString("bound", XmlConvert.ToString((int)kvp.Key.m_Bound));

                    foreach (KeyValuePair<PoseController, Dictionary<object, bool>> pair in kvp.Value.allDynamicBones)
                    {
                        CharaPoseController charaPoseController = pair.Key as CharaPoseController;
                        foreach (KeyValuePair<object, bool> pair2 in pair.Value)
                        {
                            DynamicBone db = pair2.Key as DynamicBone;
                            if (db != null)
                            {
                                xmlWriter.WriteStartElement("dynamicBoneData");

                                xmlWriter.WriteAttributeString("poseControllerId", XmlConvert.ToString(pair.Key.GetInstanceID()));
                                xmlWriter.WriteAttributeString("root", pair.Key._dynamicBonesEditor.CalculateRoot(db));
                                xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(pair2.Value));

                                xmlWriter.WriteEndElement();
                            }
                            else if (charaPoseController != null && charaPoseController._boobsEditor != null)
                            {
                                DynamicBone_Ver02 db2 = pair2.Key as DynamicBone_Ver02;
                                if (db2 != null)
                                {
                                    xmlWriter.WriteStartElement("dynamicBoneData2");

                                    xmlWriter.WriteAttributeString("poseControllerId", XmlConvert.ToString(pair.Key.GetInstanceID()));
                                    xmlWriter.WriteAttributeString("id", charaPoseController._boobsEditor.GetID(db2));
                                    xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(pair2.Value));

                                    xmlWriter.WriteEndElement();
                                }
                            }
                        }
                    }

                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteStartElement("collidersEditor");
            xmlWriter.WriteAttributeString("addNewDynamicBonesAsDefault", XmlConvert.ToString(_addNewDynamicBonesAsDefault));
            xmlWriter.WriteEndElement();
            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            ResetAll();
            bool changed = false;
            XmlNode colliders = xmlNode.FindChildNode("colliders");
            if (colliders != null)
            {
                foreach (XmlNode node in colliders.ChildNodes)
                {
                    try
                    {
                        Transform t = _parent.transform.Find(node.Attributes["name"].Value);
                        if (t == null)
                            continue;
                        DynamicBoneColliderBase collider = t.GetComponent<DynamicBoneColliderBase>();
                        if (collider == null)
                            continue;
                        ColliderDataBase data;
#if AISHOUJO || HONEYSELECT2
                        bool isNormalCollider = node.Attributes["isNormalCollider"] == null || XmlConvert.ToBoolean(node.Attributes["isNormalCollider"].Value);
                        if (isNormalCollider)
#endif
                        {
                            DynamicBoneCollider normalCollider = (DynamicBoneCollider)collider;
                            ColliderData colliderData = new ColliderData();

                            if (node.Attributes["radius"] != null)
                            {
                                float radius = XmlConvert.ToSingle(node.Attributes["radius"].Value);
                                colliderData.originalRadius = normalCollider.m_Radius;
                                normalCollider.m_Radius = radius;
                            }
                            if (node.Attributes["height"] != null)
                            {
                                float height = XmlConvert.ToSingle(node.Attributes["height"].Value);
                                colliderData.originalHeight = normalCollider.m_Height;
                                normalCollider.m_Height = height;
                            }
                            data = colliderData;
                        }
#if AISHOUJO || HONEYSELECT2
                        else
                        {
                            data = new ColliderPlaneData();
                        }
#endif
                        if (node.Attributes["centerX"] != null && node.Attributes["centerY"] != null && node.Attributes["centerZ"] != null)
                        {
                            Vector3 center;
                            center.x = XmlConvert.ToSingle(node.Attributes["centerX"].Value);
                            center.y = XmlConvert.ToSingle(node.Attributes["centerY"].Value);
                            center.z = XmlConvert.ToSingle(node.Attributes["centerZ"].Value);
                            data.originalCenter = collider.m_Center;
                            collider.m_Center = center;
                        }
                        if (node.Attributes["direction"] != null)
                        {
                            int direction = XmlConvert.ToInt32(node.Attributes["direction"].Value);
                            data.originalDirection = collider.m_Direction;
                            collider.m_Direction = (DynamicBoneCollider.Direction)direction;
                        }
                        if (node.Attributes["bound"] != null)
                        {
                            int bound = XmlConvert.ToInt32(node.Attributes["bound"].Value);
                            data.originalBound = collider.m_Bound;
                            collider.m_Bound = (DynamicBoneCollider.Bound)bound;
                        }
                        if (node.HasChildNodes)
                            changed = true;

                        if (_isLoneCollider)
                        {
                            _parent.ExecuteDelayed(() =>
                            {
                                foreach (XmlNode childNode in node.ChildNodes)
                                {
                                    int id = XmlConvert.ToInt32(childNode.Attributes["poseControllerId"].Value);
                                    int lastLoadId = MainWindow._lastLoadId;
                                    switch (childNode.Name)
                                    {
                                        // OLD DATA FOR COMPATIBILITY
                                        case "ignoredDynamicBone":
                                            PoseController controller = PoseController._poseControllers.FirstOrDefault(e => e._oldInstanceId == id && e._loadId == lastLoadId);
                                            if (controller == null)
                                                break;
                                            DynamicBone db = controller._dynamicBonesEditor.SearchDynamicBone(childNode.Attributes["root"].Value);
                                            if (db == null)
                                                break;
                                            SetIgnoreDynamicBone(collider, controller, db, true);
                                            break;
                                        case "ignoredDynamicBone2":
                                            CharaPoseController controller2 = CharaPoseController._charaPoseControllers.FirstOrDefault(e => e._oldInstanceId == id && e._loadId == lastLoadId) as CharaPoseController;
                                            if (controller2 == null || controller2._boobsEditor == null)
                                                break;
                                            DynamicBone_Ver02 db2 = controller2._boobsEditor.GetDynamicBone(childNode.Attributes["id"].Value);
                                            SetIgnoreDynamicBone(collider, controller2, db2, true);
                                            break;

                                        // NEW DATA
                                        case "dynamicBoneData":
                                            PoseController newController = PoseController._poseControllers.FirstOrDefault(e => e._oldInstanceId == id && e._loadId == lastLoadId);
                                            if (newController == null)
                                                break;
                                            DynamicBone newDB = newController._dynamicBonesEditor.SearchDynamicBone(childNode.Attributes["root"].Value);
                                            if (newDB == null)
                                                break;
                                            SetIgnoreDynamicBone(collider, newController, newDB, !XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value));
                                            break;
                                        case "dynamicBoneData2":
                                            CharaPoseController newController2 = CharaPoseController._charaPoseControllers.FirstOrDefault(e => e._oldInstanceId == id && e._loadId == lastLoadId) as CharaPoseController;
                                            if (newController2 == null || newController2._boobsEditor == null)
                                                break;
                                            DynamicBone_Ver02 newDB2 = newController2._boobsEditor.GetDynamicBone(childNode.Attributes["id"].Value);
                                            SetIgnoreDynamicBone(collider, newController2, newDB2, !XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value));
                                            break;
                                    }
                                }
                            }, 5);
                        }
                        if (data.hasValue)
                        {
                            changed = true;
                            _dirtyColliders.Add(collider, data);
                        }
                    }
                    catch (Exception e)
                    {
                        HSPE.Logger.LogError("Couldn't load collider for object " + _parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
            XmlNode collidersEditor = xmlNode.FindChildNode("collidersEditor");
            if (collidersEditor != null)
            {
                _addNewDynamicBonesAsDefault = collidersEditor.Attributes["addNewDynamicBonesAsDefault"] == null || XmlConvert.ToBoolean(collidersEditor.Attributes["addNewDynamicBonesAsDefault"].Value);
                // Register all DynamicBones that where loaded before the import
                if (MainWindow._lastLoadType == MainWindow.LoadType.Import && _isLoneCollider)
                {
                    _parent.ExecuteDelayed2(() =>
                    {
                        DynamicBoneColliderBase collider = _parent.transform.GetChild(0).GetComponent<DynamicBoneColliderBase>();
                        SetNewDefaultAllDynamicBones(collider);
                    }, 5);
                }
            }
            return changed;
        }
        #endregion

        #region Private Methods
        private void DrawFieldsBase(DynamicBoneColliderBase collider)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("Center");
                GUILayout.BeginHorizontal();
                Vector3 center = Vector3Editor(collider.m_Center);
                if (center != collider.m_Center)
                    SetColliderCenter(collider, center);
                IncEditor();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Direction\t", GUILayout.ExpandWidth(false));
                DynamicBoneColliderBase.Direction direction = (DynamicBoneColliderBase.Direction)GUILayout.SelectionGrid((int)collider.m_Direction, _directionNames, 3);
                if (direction != collider.m_Direction)
                    SetColliderDirection(collider, direction);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Bound\t", GUILayout.ExpandWidth(false));
                DynamicBoneColliderBase.Bound bound = (DynamicBoneColliderBase.Bound)GUILayout.SelectionGrid((int)collider.m_Bound, _boundNames, 2);
                if (bound != collider.m_Bound)
                    SetColliderBound(collider, bound);
            }
            GUILayout.EndHorizontal();


        }

        private void DrawFields(DynamicBoneCollider collider)
        {
            DrawFieldsBase((DynamicBoneColliderBase)collider);

#if HONEYSELECT || KOIKATSU || PLAYHOME
            float radius = FloatEditor(collider.m_Radius, 0f, 1f, "Radius\t");
#elif AISHOUJO || HONEYSELECT2
            float radius = this.FloatEditor(collider.m_Radius, 0f, 10f, "Radius\t");
#endif
            if (Mathf.Approximately(radius, collider.m_Radius) == false)
                SetColliderRadius(collider, radius);

            float height = FloatEditor(collider.m_Height, 2 * collider.m_Radius, Mathf.Max(4f, 4f * collider.m_Radius), "Height\t");
            if (Mathf.Approximately(height, collider.m_Height) == false)
                SetColliderHeight(collider, height);

        }

#if AISHOUJO || HONEYSELECT2
        private void DrawFields(DynamicBonePlaneCollider collider)
        {
            this.DrawFieldsBase(collider);
        }
#endif

        private void SetColliderCenter(DynamicBoneColliderBase collider, Vector3 center)
        {
            ColliderDataBase data = SetColliderDirty(collider);
            if (data.originalCenter.hasValue == false)
                data.originalCenter = collider.m_Center;
            collider.m_Center = center;
        }

        private void SetColliderDirection(DynamicBoneColliderBase collider, DynamicBoneColliderBase.Direction direction)
        {
            ColliderDataBase data = SetColliderDirty(collider);
            if (data.originalDirection.hasValue == false)
                data.originalDirection = collider.m_Direction;
            collider.m_Direction = direction;
        }

        private void SetColliderBound(DynamicBoneColliderBase collider, DynamicBoneColliderBase.Bound bound)
        {
            ColliderDataBase data = SetColliderDirty(collider);
            if (data.originalBound.hasValue == false)
                data.originalBound = collider.m_Bound;
            collider.m_Bound = bound;
        }

        private void SetColliderRadius(DynamicBoneCollider collider, float radius)
        {
            ColliderData data = (ColliderData)SetColliderDirty(collider);
            if (data.originalRadius.hasValue == false)
                data.originalRadius = collider.m_Radius;
            collider.m_Radius = radius;
        }

        private void SetColliderHeight(DynamicBoneCollider collider, float height)
        {
            ColliderData data = (ColliderData)SetColliderDirty(collider);
            if (data.originalHeight.hasValue == false)
                data.originalHeight = collider.m_Height;
            collider.m_Height = height;
        }

        private void ResetAll()
        {
            _addNewDynamicBonesAsDefault = true;
            foreach (KeyValuePair<DynamicBoneColliderBase, ColliderDataBase> pair in new Dictionary<DynamicBoneColliderBase, ColliderDataBase>(_dirtyColliders))
                SetColliderNotDirty(pair.Key);
            _dirtyColliders.Clear();
        }

        private ColliderDataBase SetColliderDirty(DynamicBoneColliderBase collider)
        {
            ColliderDataBase data;
            if (_dirtyColliders.TryGetValue(collider, out data) == false)
            {
                if (collider is DynamicBoneCollider)
                    data = new ColliderData();
#if AISHOUJO || HONEYSELECT2
                else
                    data = new ColliderPlaneData();
#endif
                _dirtyColliders.Add(collider, data);
            }
            return data;
        }

        private bool IsColliderDirty(DynamicBoneColliderBase collider)
        {
            return _dirtyColliders.ContainsKey(collider);
        }

        private void SetColliderNotDirty(DynamicBoneColliderBase collider)
        {
            if (IsColliderDirty(collider))
            {
                ColliderDataBase data = _dirtyColliders[collider];
                data.ResetWithParent(collider);
                _dirtyColliders.Remove(collider);
            }
            _addNewDynamicBonesAsDefault = true;
        }

        public void SetIgnoreDynamicBone(DynamicBoneColliderBase collider, PoseController dynamicBoneParent, object dynamicBone, bool ignore)
        {
            ColliderDataBase colliderData = SetColliderDirty(collider);
            DynamicBoneCollider normalCollider = collider as DynamicBoneCollider;

            Dictionary<object, bool> dynamicBonesDic;
            if (colliderData.allDynamicBones.TryGetValue(dynamicBoneParent, out dynamicBonesDic) == false)
            {
                dynamicBonesDic = new Dictionary<object, bool>();
                colliderData.allDynamicBones.Add(dynamicBoneParent, dynamicBonesDic);
            }

            dynamicBonesDic[dynamicBone] = !ignore;

            if (dynamicBone is DynamicBone db)
            {
                List<DynamicBoneColliderBase> colliders = db.m_Colliders;
                if (ignore)
                {
                    int index = colliders.IndexOf(collider);
                    if (index != -1)
                        colliders.RemoveAt(index);
                }
                else
                {
                    if (colliders.Contains(collider) == false)
                        colliders.Add(collider);
                }
            }
            else if (normalCollider != null)
            {
                List<DynamicBoneCollider> colliders = ((DynamicBone_Ver02)dynamicBone).Colliders;
                if (ignore)
                {
                    int index = colliders.IndexOf(normalCollider);
                    if (index != -1)
                        colliders.RemoveAt(index);
                }
                else
                {
                    if (colliders.Contains(normalCollider) == false)
                        colliders.Add(normalCollider);
                }
            }
        }

        private void SetIgnoreAllDynamicBones(DynamicBoneColliderBase collider, bool ignore)
        {
#if AISHOUJO || HONEYSELECT2
            bool isColliderNormalCollider = collider is DynamicBoneCollider;
#endif

            foreach (PoseController controller in PoseController._poseControllers)
            {
                if (controller == null) continue;

                if (controller._dynamicBonesEditor != null && controller._dynamicBonesEditor._dynamicBones != null)
                {
                    foreach (DynamicBone dynamicBone in controller._dynamicBonesEditor._dynamicBones)
                    {
                        if (dynamicBone.m_Root == null)
                            continue;
                        SetIgnoreDynamicBone(collider, controller, dynamicBone, ignore);
                    }
                }

                CharaPoseController charaPoseController = null;
                if (CharaPoseController._charaPoseControllers.Contains(controller))
                    charaPoseController = (CharaPoseController)controller;

                if (
#if AISHOUJO || HONEYSELECT2
                        isColliderNormalCollider &&
#endif
                        charaPoseController != null && charaPoseController._boobsEditor != null && charaPoseController._boobsEditor._dynamicBones != null
                        )
                {
                    foreach (DynamicBone_Ver02 dynamicBone in charaPoseController._boobsEditor._dynamicBones)
                        SetIgnoreDynamicBone(collider, controller, dynamicBone, ignore);
                }
            }

        }


        private void SetNewDefaultAllDynamicBones(DynamicBoneColliderBase collider)
        {

#if AISHOUJO || HONEYSELECT2
            bool isColliderNormalCollider = collider is DynamicBoneCollider;
#endif

            foreach (PoseController controller in PoseController._poseControllers)
            {
                if (controller == null) continue;

                if (controller._dynamicBonesEditor != null && controller._dynamicBonesEditor._dynamicBones != null)
                {
                    foreach (DynamicBone dynamicBone in controller._dynamicBonesEditor._dynamicBones)
                    {
                        if (dynamicBone.m_Root == null)
                            continue;
                        SetNewDefaultDynamicBone(collider, controller, dynamicBone);
                    }
                }

                CharaPoseController charaPoseController = null;
                if (CharaPoseController._charaPoseControllers.Contains(controller))
                    charaPoseController = (CharaPoseController)controller;

                if (
#if AISHOUJO || HONEYSELECT2
                        isColliderNormalCollider &&
#endif
                        charaPoseController != null && charaPoseController._boobsEditor != null && charaPoseController._boobsEditor._dynamicBones != null
                        )
                {
                    foreach (DynamicBone_Ver02 dynamicBone in charaPoseController._boobsEditor._dynamicBones)
                        SetNewDefaultDynamicBone(collider, controller, dynamicBone);
                }
            }

        }

        public void SetNewDefaultDynamicBone(DynamicBoneColliderBase collider, PoseController dynamicBoneParent, object dynamicBone)
        {
            ColliderDataBase colliderData = SetColliderDirty(collider);
            DynamicBoneCollider normalCollider = collider as DynamicBoneCollider;

            Dictionary<object, bool> dynamicBonesDic;
            if (colliderData.allDynamicBones.TryGetValue(dynamicBoneParent, out dynamicBonesDic) == false)
            {
                dynamicBonesDic = new Dictionary<object, bool>();
                colliderData.allDynamicBones.Add(dynamicBoneParent, dynamicBonesDic);
            }

            if (dynamicBonesDic.ContainsKey(dynamicBone) == false)
                dynamicBonesDic.Add(dynamicBone, _addNewDynamicBonesAsDefault);
            else
                return;

            if (dynamicBone is DynamicBone db)
            {
                List<DynamicBoneColliderBase> colliders = db.m_Colliders;
                if (!_addNewDynamicBonesAsDefault)
                {
                    int index = colliders.IndexOf(collider);
                    if (index != -1)
                        colliders.RemoveAt(index);
                }
                else
                {
                    if (colliders.Contains(collider) == false)
                        colliders.Add(collider);
                }
            }
            else if (normalCollider != null)
            {
                List<DynamicBoneCollider> colliders = ((DynamicBone_Ver02)dynamicBone).Colliders;
                if (!_addNewDynamicBonesAsDefault)
                {
                    int index = colliders.IndexOf(normalCollider);
                    if (index != -1)
                        colliders.RemoveAt(index);
                }
                else
                {
                    if (colliders.Contains(normalCollider) == false)
                        colliders.Add(normalCollider);
                }
            }
        }

        public override void UpdateGizmos()
        {
            if (GizmosEnabled() == false)
                return;

            if (MainWindow._self._poseTarget == _parent)
            {
#if AISHOUJO || HONEYSELECT2
                if (this._isTargetNormalCollider)
#endif
                {
                    _colliderDebugLines.Update((DynamicBoneCollider)_colliderTarget);
                }
#if AISHOUJO || HONEYSELECT2
                else
                    _colliderPlaneDebugLines.Update((DynamicBonePlaneCollider)this._colliderTarget);
#endif
#if AISHOUJO || HONEYSELECT2
                if (this._isTargetNormalCollider)
#endif
                {
                    _colliderDebugLines.Draw();
                }
#if AISHOUJO || HONEYSELECT2
                else
                    _colliderPlaneDebugLines.Draw();
#endif
            }
        }

        private static void UpdateDebugLinesState(CollidersEditor self)
        {
            if (_colliderDebugLines != null)
            {
                bool enabled = self != null && self.GizmosEnabled();
                _colliderDebugLines.SetActive(
                        enabled
#if AISHOUJO || HONEYSELECT2
                        && self._isTargetNormalCollider
#endif
                                              );
#if AISHOUJO || HONEYSELECT2
                _colliderPlaneDebugLines.SetActive(enabled && self._isTargetNormalCollider == false);
#endif
            }
        }

        private bool GizmosEnabled()
        {
            return _isEnabled && PoseController._drawAdvancedMode && _colliderTarget != null;
        }
        #endregion

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "colliderBaseCenter",
                        name: "Collider Center",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<CollidersEditor, DynamicBoneColliderBase> pair = (HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter;
                            pair.key.SetColliderCenter(pair.value, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter).value.m_Center,
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameterBase,
                        readParameterFromXml: ReadParameterFromXmlBase,
                        writeParameterToXml: WriteParameterToXmlBase,
                        checkIntegrity: CheckIntegrityBase,
                        getFinalName: (name, oci, parameter) => $"Col Center ({((HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter).value.name})"
                );

                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "colliderBaseDirection",
                        name: "Collider Direction",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<CollidersEditor, DynamicBoneColliderBase> pair = (HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter;
                            pair.key.SetColliderDirection(pair.value, (DynamicBoneColliderBase.Direction)leftValue);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter).value.m_Direction,
                        readValueFromXml: (parameter, node) => (DynamicBoneColliderBase.Direction)node.ReadInt("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o),
                        getParameter: GetParameterBase,
                        readParameterFromXml: ReadParameterFromXmlBase,
                        writeParameterToXml: WriteParameterToXmlBase,
                        checkIntegrity: CheckIntegrityBase,
                        getFinalName: (name, oci, parameter) => $"Col Direction ({((HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter).value.name})"
                );

                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "colliderBaseBound",
                        name: "Collider Bound",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<CollidersEditor, DynamicBoneColliderBase> pair = (HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter;
                            pair.key.SetColliderBound(pair.value, (DynamicBoneColliderBase.Bound)leftValue);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter).value.m_Bound,
                        readValueFromXml: (parameter, node) => (DynamicBoneColliderBase.Bound)node.ReadInt("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o),
                        getParameter: GetParameterBase,
                        readParameterFromXml: ReadParameterFromXmlBase,
                        writeParameterToXml: WriteParameterToXmlBase,
                        checkIntegrity: CheckIntegrityBase,
                        getFinalName: (name, oci, parameter) => $"Col Bound ({((HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter).value.name})"
                );

                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "colliderRadius",
                        name: "Collider Radius",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<CollidersEditor, DynamicBoneCollider> pair = (HashedPair<CollidersEditor, DynamicBoneCollider>)parameter;
                            pair.key.SetColliderRadius(pair.value, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((HashedPair<CollidersEditor, DynamicBoneCollider>)parameter).value.m_Radius,
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"Col Radius ({((HashedPair<CollidersEditor, DynamicBoneCollider>)parameter).value.name})"
                );

                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "colliderHeight",
                        name: "Collider Height",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<CollidersEditor, DynamicBoneCollider> pair = (HashedPair<CollidersEditor, DynamicBoneCollider>)parameter;
                            pair.key.SetColliderHeight(pair.value, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((HashedPair<CollidersEditor, DynamicBoneCollider>)parameter).value.m_Height,
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity,
                        getFinalName: (name, oci, parameter) => $"Col Height ({((HashedPair<CollidersEditor, DynamicBoneCollider>)parameter).value.name})"
                );

            }

            private static bool CheckIntegrityBase(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue)
            {
                if (parameter == null)
                    return false;
                HashedPair<CollidersEditor, DynamicBoneColliderBase> pair = (HashedPair<CollidersEditor, DynamicBoneColliderBase>)parameter;
                if (pair.key == null || pair.value == null)
                    return false;
                return true;
            }

            private static bool CheckIntegrity(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue)
            {
                if (parameter == null)
                    return false;
                HashedPair<CollidersEditor, DynamicBoneCollider> pair = (HashedPair<CollidersEditor, DynamicBoneCollider>)parameter;
                if (pair.key == null || pair.value == null)
                    return false;
                return true;
            }

            private static bool IsCompatibleWithTarget(ObjectCtrlInfo oci)
            {
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null;
            }

            private static object GetParameterBase(ObjectCtrlInfo oci)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new HashedPair<CollidersEditor, DynamicBoneColliderBase>(controller._collidersEditor, controller._collidersEditor._colliderTarget);
            }
            private static object GetParameter(ObjectCtrlInfo oci)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new HashedPair<CollidersEditor, DynamicBoneCollider>(controller._collidersEditor, (DynamicBoneCollider)controller._collidersEditor._colliderTarget);
            }

            private static object ReadParameterFromXmlBase(ObjectCtrlInfo oci, XmlNode node)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new HashedPair<CollidersEditor, DynamicBoneColliderBase>(controller._collidersEditor, controller.transform.Find(node.Attributes["parameter"].Value).GetComponent<DynamicBoneCollider>());
            }

            private static object ReadParameterFromXml(ObjectCtrlInfo oci, XmlNode node)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new HashedPair<CollidersEditor, DynamicBoneCollider>(controller._collidersEditor, controller.transform.Find(node.Attributes["parameter"].Value).GetComponent<DynamicBoneCollider>());
            }

            private static void WriteParameterToXmlBase(ObjectCtrlInfo oci, XmlTextWriter writer, object o)
            {
                writer.WriteAttributeString("parameter", ((HashedPair<CollidersEditor, DynamicBoneColliderBase>)o).value.transform.GetPathFrom(oci.guideObject.transformTarget));
            }

            private static void WriteParameterToXml(ObjectCtrlInfo oci, XmlTextWriter writer, object o)
            {
                writer.WriteAttributeString("parameter", ((HashedPair<CollidersEditor, DynamicBoneCollider>)o).value.transform.GetPathFrom(oci.guideObject.transformTarget));
            }
        }
        #endregion
    }
}