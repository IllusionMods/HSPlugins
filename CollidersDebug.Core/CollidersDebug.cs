using System.Collections.Generic;
using BepInEx;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using ToolBox;
using UniRx;
using UnityEngine;
using Vectrosity;

namespace CollidersDebug
{
    [BepInPlugin(_guid, _name, _versionNum)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]

    public class CollidersDebug : GenericPlugin
    {
        private const string _name = "ColllidersDebug";
        private const string _guid = "com.joan6694.illusionplugins.collidersdebug";
        private const string _versionNum = "1.0.1";
        private const int _uniqueId = ('C' << 24) | ('O' << 16) | ('L' << 8) | ('D');
        private static readonly Color _colliderColor = Color.Lerp(Color.green, Color.white, 0.5f);

        #region Types
        private class ColliderDebugLines
        {
            private const int _circlesPrecision = 37;
            private const int _centerCirclesCount = 9;
            private const int _centerCircleCountPlusOne = _centerCirclesCount + 1;
            private const int _centerLinesCount = 8;
            private const int _capsCirclesCount = 6;
            private const int _capsCirclesCountMinusOne = _capsCirclesCount - 1;

            private readonly List<VectorLine> _centerCircles = new List<VectorLine>();
            private readonly List<VectorLine> _capsCircles = new List<VectorLine>();
            private readonly List<VectorLine> _centerLines = new List<VectorLine>();
            private readonly List<VectorLine> _capsLines = new List<VectorLine>();

            public ColliderDebugLines()
            {
                for (int i = 0; i < _centerCirclesCount; ++i)
                    this._centerCircles.Add(VectorLine.SetLine(_colliderColor, new Vector3[_circlesPrecision]));
                for (int i = 0; i < _centerLinesCount; ++i)
                    this._centerLines.Add(VectorLine.SetLine(_colliderColor, Vector3.zero, Vector3.one));
                for (int i = 0; i < _capsCirclesCount; ++i)
                {
                    this._capsCircles.Add(VectorLine.SetLine(_colliderColor, new Vector3[_circlesPrecision]));
                    this._capsCircles.Add(VectorLine.SetLine(_colliderColor, new Vector3[_circlesPrecision]));

                    if (i != 0)
                        for (int j = 0; j < _centerLinesCount; ++j)
                        {
                            this._capsLines.Add(VectorLine.SetLine(_colliderColor, Vector3.zero, Vector3.one));
                            this._capsLines.Add(VectorLine.SetLine(_colliderColor, Vector3.zero, Vector3.one));
                        }
                }
                foreach (VectorLine line in this._centerCircles)
                    line.lineWidth = 2f;
                foreach (VectorLine line in this._capsCircles)
                    line.lineWidth = 2f;
                foreach (VectorLine line in this._centerLines)
                    line.lineWidth = 2f;
                foreach (VectorLine line in this._capsLines)
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
                for (int i = 0; i < _centerCirclesCount; ++i)
                {
                    VectorLine line = this._centerCircles[i];
                    line.MakeCircle(Vector3.Lerp(position1, position2, (i + 1f) / _centerCircleCountPlusOne), dir, radius);
                    line.Draw();
                }

                Vector3[] prev = new Vector3[_centerLinesCount];
                for (int i = 0; i < _centerLinesCount; ++i)
                {
                    float angle = 360 * (i / (float)_centerLinesCount) * Mathf.Deg2Rad;
                    Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                    prev[i] = offset;
                    VectorLine line = this._centerLines[i];
                    line.points3[0] = position1 + offset;
                    line.points3[1] = position2 + offset;
                    line.Draw();
                }

                Vector3 prevCenter1 = Vector3.zero;
                Vector3 prevCenter2 = Vector3.zero;
                int k = 0;
                int l = 0;
                for (int i = 0; i < _capsCirclesCount; ++i)
                {
                    float v = (i / (float)_capsCirclesCountMinusOne) * 0.95f;
                    float angle = Mathf.Asin(v);
                    float radius2 = radius * Mathf.Cos(angle);
                    Vector3 center1 = position1 - dir * v * radius;
                    Vector3 center2 = position2 + dir * v * radius;
                    VectorLine circle = this._capsCircles[k++];
                    circle.MakeCircle(center1, dir, radius2);
                    circle.Draw();

                    circle = this._capsCircles[k++];
                    circle.MakeCircle(center2, dir, radius2);
                    circle.Draw();

                    if (i != 0)
                        for (int j = 0; j < _centerLinesCount; ++j)
                        {
                            float angle2 = 360 * (j / (float)_centerLinesCount) * Mathf.Deg2Rad;
                            Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2))) * radius2;
                            VectorLine line = this._capsLines[l++];
                            line.points3[0] = prevCenter1 + prev[j];
                            line.points3[1] = center1 + offset;
                            line.Draw();

                            line = this._capsLines[l++];
                            line.points3[0] = prevCenter2 + prev[j];
                            line.points3[1] = center2 + offset;
                            line.Draw();

                            prev[j] = offset;
                        }
                    prevCenter1 = center1;
                    prevCenter2 = center2;
                }
            }

            public void SetActive(bool active)
            {
                foreach (VectorLine line in this._centerCircles)
                    line.active = active;
                foreach (VectorLine line in this._capsCircles)
                    line.active = active;
                foreach (VectorLine line in this._centerLines)
                    line.active = active;
                foreach (VectorLine line in this._capsLines)
                    line.active = active;
            }

            public bool IsActive()
            {
                return this._centerCircles[0].active;
            }

            public void Destroy()
            {
                VectorLine.Destroy(this._capsLines);
                VectorLine.Destroy(this._centerLines);
                VectorLine.Destroy(this._capsCircles);
                VectorLine.Destroy(this._centerCircles);
            }
        }
        #endregion

        #region Private Variables
        private ColliderDebugLines _debugLines;
        private DynamicBoneCollider[] _colliders;
        private DynamicBoneCollider _target;

        private SidebarToggle _toggle;
        private Rect _windowRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 250, 300, 500);
        private Vector2 _scroll;
        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            MakerAPI.MakerStartedLoading += (sender, e) =>
            {
                this._debugLines = new ColliderDebugLines();
                this.RefreshColliders();
            };
            MakerAPI.RegisterCustomSubCategories += (sender, e) =>
            {
                this._toggle = new SidebarToggle("Debug Colliders", false, this);
                e.AddSidebarControl(this._toggle).ValueChanged.Subscribe(b => this.RefreshColliders());
            };
            MakerAPI.MakerExiting += (sender, e) =>
            {
                this._toggle = null;
                this._debugLines.Destroy();
            };
        }

        protected override void OnGUI()
        {
            if (MakerAPI.InsideMaker == false || this._toggle.Value == false)
                return;
            this._windowRect = GUILayout.Window(_uniqueId, this._windowRect, this.WindowFunc, "Colliders");
        }

        private void WindowFunc(int id)
        {
            this._scroll = GUILayout.BeginScrollView(this._scroll, GUI.skin.box);
            foreach (DynamicBoneCollider col in this._colliders)
            {
                if (col == null)
                {
                    this.RefreshColliders();
                    break;
                }
                Color c = GUI.color;
                if (col.enabled == false)
                    GUI.color = Color.grey;
                if (col == this._target)
                    GUI.color = Color.cyan;
                if (GUILayout.Button(col.name))
                    this._target = col;
                GUI.color = c;
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        protected override void LateUpdate()
        {
            if (MakerAPI.InsideMaker && this._debugLines != null)
            {
                if (this._toggle != null && this._toggle.Value && this._target != null)
                {
                    if (this._target.enabled != this._debugLines.IsActive())
                        this._debugLines.SetActive(this._target.enabled);
                    if (this._target.enabled)
                        this._debugLines.Update(this._target);
                }
                else
                {
                    if (this._debugLines.IsActive())
                        this._debugLines.SetActive(false);
                }
            }
        }
        #endregion

        #region Private Methods
        private void RefreshColliders()
        {
            this._colliders = MakerAPI.GetCharacterControl().GetComponentsInChildren<DynamicBoneCollider>(true);
        }
        #endregion
    }
}
