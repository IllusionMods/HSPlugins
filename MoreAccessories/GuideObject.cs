using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

namespace MoreAccessories
{
    public class GuideObject : MonoBehaviour
    {
        public Action<Vector3> onPositionDelta;
        public Action<Vector3> onRotationDelta;
        public Action<Vector3> onScaleDelta;

        private Transform _transformTarget;
        private readonly Transform[] _roots = new Transform[3];
        private readonly List<GuideBase> _guides = new List<GuideBase>();
        private readonly bool[] _enables = {
            true,
            true,
            true,
            true
        };

        private bool isActive { get { return this._transformTarget != null; } }

        private bool _visible = true;

        public virtual void SetMode(int mode, bool layer = true)
        {
            for (int i = 0; i < 3; i++)
                this._roots[i].gameObject.SetActive(this.isActive && this._enables[i] && mode == i);
            if (layer)
            {

                bool flag2 = !this.isActive && this._enables.Any(b => b) || (this.isActive && !this._enables[mode]);
                this.SetLayer(this.gameObject, LayerMask.NameToLayer(flag2 ? "Studio_Col" : "Studio_ColSelect"));
            }
        }

        public virtual void SetTransformTarget(Transform transformTarget, bool layer = true)
        {
            this._transformTarget = transformTarget;
            this.SetMode(MoreAccessories._self._smMoreAccessories._guideObjectMode, layer);
            this.gameObject.SetActive(this.isActive);
        }

        public bool visible
        {
            get { return this._visible; }
            set
            {
                if (this._visible != value)
                {
                    this._visible = value;
                    for (int i = 0; i < this._guides.Count; i++)
                    {
                        this._guides[i].draw = this._visible;
                    }
                }
            }
        }

        public virtual void CalcScale()
        {
        }

        public virtual void SetLayer(GameObject _object, int layer)
        {
            if (_object == null)
                return;
            _object.layer = layer;
            for (int i = 0; i < _object.transform.childCount; i++)
                this.SetLayer(_object.transform.GetChild(i).gameObject, layer);
        }

        //public virtual void ForceUpdate()
        //{
        //    this.CalcPosition();
        //    this.CalcRotation();
        //}

        public virtual void Awake()
        {
            this._roots[0] = this.transform.Find("move");
            this._roots[1] = this.transform.Find("rotation");
            this._roots[2] = this.transform.Find("scale");

            GuideMove.MoveAxis[] moveValues = (GuideMove.MoveAxis[])Enum.GetValues(typeof(GuideMove.MoveAxis));
            for (int i = 0; i < this._roots[0].childCount; ++i)
            {
                GuideMove move = this._roots[0].GetChild(i).gameObject.AddComponent<GuideMove>();
                move.onPositionDelta = this.OnPositionDelta;
                move.axis = moveValues[i];
                this._guides.Add(move);
            }

            GuideRotation.RotationAxis[] rotationValues = (GuideRotation.RotationAxis[])Enum.GetValues(typeof(GuideRotation.RotationAxis));
            for (int i = 0; i < this._roots[1].childCount; ++i)
            {
                GuideRotation rotation = this._roots[1].GetChild(i).gameObject.AddComponent<GuideRotation>();
                rotation.onRotationDelta = this.OnRotationDelta;
                rotation.axis = rotationValues[i];
                this._guides.Add(rotation);
            }

            GuideScale.ScaleAxis[] scaleValues = (GuideScale.ScaleAxis[])Enum.GetValues(typeof(GuideScale.ScaleAxis));
            for (int i = 0; i < this._roots[2].childCount; ++i)
            {
                GuideScale scale = this._roots[2].GetChild(i).gameObject.AddComponent<GuideScale>();
                scale.onScaleDelta = this.OnScaleDelta;
                scale.axis = scaleValues[i];
                this._guides.Add(scale);
            }
        }

        private void OnPositionDelta(Vector3 position)
        {
            if (this.onPositionDelta != null)
                this.onPositionDelta(position);
        }

        private void OnRotationDelta(Vector3 rotation)
        {
            if (this.onRotationDelta != null)
                this.onRotationDelta(rotation);
        }

        private void OnScaleDelta(Vector3 scale)
        {
            if (this.onScaleDelta != null)
                this.onScaleDelta(scale);
        }

        public virtual void Start()
        {
            this._visible = true;
            this.SetTransformTarget(null);
        }

        public virtual void LateUpdate()
        {
            this.transform.position = this._transformTarget.position;
            this.transform.rotation = this._transformTarget.rotation;
            this._roots[0].transform.rotation = this._transformTarget.parent.rotation;
        }
    }


    public class GuideMove : GuideBase, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        public Action<Vector3> onPositionDelta;

        public void OnPointerDown(PointerEventData eventData)
        {
            Camera.main.GetComponent<CameraControl>().NoCtrlCondition = () => this.isDrag;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            this._oldPos = eventData.pressPosition;
        }

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            bool flag = false;
            Vector3 b = (this.axis != MoveAxis.XYZ) ? this.AxisMove(eventData.delta, ref flag) : (this.WorldPos(eventData.position) - this.WorldPos(this._oldPos));
            if (this.onPositionDelta != null)
                this.onPositionDelta(b);
            this._oldPos = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public virtual Vector3 WorldPos(Vector2 screenPos)
        {
            Plane plane = new Plane(Camera.main.transform.forward * -1f, this.transform.position);
            Ray ray = RectTransformUtility.ScreenPointToRay(Camera.main, screenPos);
            float distance;
            return (!plane.Raycast(ray, out distance)) ? this.transform.position : ray.GetPoint(distance);
        }

        public virtual Vector3 AxisMove(Vector2 delta, ref bool snap)
        {
            Vector3 vector = Camera.main.transform.TransformVector(delta.x * 0.0005f, delta.y * 0.0005f, 0f);
            switch (this.axis)
            {
                case MoveAxis.X:
                    vector = Vector3.Scale(Vector3.right, vector);
                    break;
                case MoveAxis.Y:
                    vector = Vector3.Scale(Vector3.up, vector);
                    break;
                case MoveAxis.Z:
                    vector = Vector3.Scale(Vector3.forward, vector);
                    break;
            }
            return vector;
        }

        public MoveAxis axis;
        private Vector2 _oldPos = Vector2.zero;

        public enum MoveAxis
        {
            XYZ,
            X,
            Y,
            Z,
        }
    }

    public class GuideRotation : GuideBase, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        public Action<Vector3> onRotationDelta;

        public void OnPointerDown(PointerEventData eventData)
        {
            Camera.main.GetComponent<CameraControl>().NoCtrlCondition = () => this.isDrag;
        }


        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            this._prevScreenPos = eventData.position;
            this._prevPlanePos = this.PlanePos(eventData.position);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            Vector3 zero = Vector3.zero;
            float f = Vector3.Dot(Camera.main.transform.forward, this.transform.right);
            if (Mathf.Abs(f) > 0.1f)
            {
                Vector3 position = this.PlanePos(eventData.position);
                Vector3 vector = Quaternion.Euler(0f, 90f, 0f) * this.transform.InverseTransformPoint(this._prevPlanePos);
                Vector3 vector2 = Quaternion.Euler(0f, 90f, 0f) * this.transform.InverseTransformPoint(position);
                float value = this.VectorToAngle(new Vector2(vector.x, vector.y), new Vector2(vector2.x, vector2.y));
                zero[(int)this.axis] = value;
                this._prevPlanePos = position;
            }
            else
            {
                Vector3 position2 = eventData.position;
                position2.z = Vector3.Distance(this._prevPlanePos, Camera.main.transform.position);
                Vector3 position3 = this._prevScreenPos;
                position3.z = Vector3.Distance(this._prevPlanePos, Camera.main.transform.position);
                Vector3 b = Camera.main.ScreenToWorldPoint(position2) - Camera.main.ScreenToWorldPoint(position3);
                Vector3 position4 = this._prevPlanePos + b;
                Vector3 vector3 = Quaternion.Euler(0f, 90f, 0f) * this.transform.InverseTransformPoint(this._prevPlanePos);
                Vector3 vector4 = Quaternion.Euler(0f, 90f, 0f) * this.transform.InverseTransformPoint(position4);
                this._prevPlanePos = position4;
                float value2 = this.VectorToAngle(new Vector2(vector3.x, vector3.y), new Vector2(vector4.x, vector4.y));
                zero[(int)this.axis] = value2;
                this._prevPlanePos = position4;
            }
            this._prevScreenPos = eventData.position;
            if (this.onRotationDelta != null)
                this.onRotationDelta(zero);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public virtual Vector3 PlanePos(Vector2 screenPos)
        {
            Plane plane = new Plane(this.transform.right, this.transform.position);
            if (!plane.GetSide(Camera.main.transform.position))
            {
                plane.SetNormalAndPosition(this.transform.right * -1f, this.transform.position);
            }
            Ray ray = RectTransformUtility.ScreenPointToRay(Camera.main, screenPos);
            float distance;
            return (!plane.Raycast(ray, out distance)) ? this.transform.position : ray.GetPoint(distance);
        }

        public virtual float VectorToAngle(Vector2 v1, Vector2 v2)
        {
            float current = Mathf.Atan2(v1.x, v1.y) * 57.29578f;
            float target = Mathf.Atan2(v2.x, v2.y) * 57.29578f;
            return Mathf.DeltaAngle(current, target);
        }

        public RotationAxis axis;
        private Vector2 _prevScreenPos = Vector2.zero;
        private Vector3 _prevPlanePos = Vector3.zero;

        public enum RotationAxis
        {
            X,
            Y,
            Z
        }
    }

    public class GuideScale : GuideBase, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        public Action<Vector3> onScaleDelta;

        public void OnPointerDown(PointerEventData eventData)
        {
            Camera.main.GetComponent<CameraControl>().NoCtrlCondition = () => this.isDrag;
        }


        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            this._prevPos = eventData.position;
        }


        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            Vector3 b;
            if (this.axis == ScaleAxis.XYZ)
            {
                Vector2 delta = eventData.delta;
                float d = (delta.x + delta.y) * this._speed;
                b = Vector3.one * d;
            }
            else
            {
                b = this.AxisPos(eventData.position) - this.AxisPos(this._prevPos);
                this._prevPos = eventData.position;
            }
            if (this.onScaleDelta != null)
                this.onScaleDelta(b);
        }


        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public virtual Vector3 AxisPos(Vector2 screenPos)
        {
            Vector3 position = this.transform.position;
            Plane plane = new Plane(Camera.main.transform.forward * -1f, position);
            Ray ray = RectTransformUtility.ScreenPointToRay(Camera.main, screenPos);
            float distance;
            Vector3 a = (!plane.Raycast(ray, out distance)) ? position : ray.GetPoint(distance);
            Vector3 vector = a - position;
            Vector3 onNormal = this.transform.up;
            switch (this.axis)
            {
                case ScaleAxis.X:
                    onNormal = Vector3.right;
                    break;
                case ScaleAxis.Y:
                    onNormal = Vector3.up;
                    break;
                case ScaleAxis.Z:
                    onNormal = Vector3.forward;
                    break;
            }
            return Vector3.Project(vector, onNormal);
        }

        public ScaleAxis axis;
        private readonly float _speed = 0.003f;
        private Vector2 _prevPos = Vector2.zero;

        public enum ScaleAxis
        {
            XYZ,
            X,
            Y,
            Z,
        }
    }

    public class GuideBase : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Material material { get { return (!this._renderer) ? null : this._renderer.material; } }

        public bool draw
        {
            get { return this._draw; }
            set
            {
                if (this._draw != value)
                {
                    this._draw = value;
                    if (this._renderer)
                    {
                        this._renderer.enabled = this._draw;
                    }
                    if (this._collider)
                    {
                        this._collider.enabled = this._draw;
                    }
                }
            }
        }


        public virtual Color ConvertColor(Color color)
        {
            color.r *= 0.75f;
            color.g *= 0.75f;
            color.b *= 0.75f;
            color.a = 0.25f;
            return color;
        }

        protected Color colorNow
        {
            set
            {
                if (this.material)
                {
                    this.material.color = value;
                }
            }
        }

        public bool isDrag { get; private set; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            this.colorNow = this._colorHighlighted;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!this.isDrag)
                this.colorNow = this._colorNormal;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            this.isDrag = true;
            Camera.main.GetComponent<CameraControl>().NoCtrlCondition = () => this.isDrag;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            this.isDrag = false;
            this.colorNow = this._colorNormal;
        }

        public virtual void OnDisable()
        {
            this.colorNow = this._colorNormal;
        }

        public virtual void Start()
        {
            this._renderer = this.gameObject.GetComponent<Renderer>();
            if (this._renderer == null)
                this._renderer = this.gameObject.GetComponentInChildren<Renderer>();
            this._collider = this._renderer.GetComponent<Collider>();
            this._colorNormal = this.ConvertColor(this.material.color);
            this._colorHighlighted = this.material.color;
            this._colorHighlighted.a = 0.75f;
            if (this._renderer)
            {
                this._renderer.enabled = this._draw;
            }
            if (this._collider)
            {
                this._collider.enabled = this._draw;
            }
            this.colorNow = this._colorNormal;
            this.isDrag = false;
        }

        private Color _colorNormal;
        private Color _colorHighlighted;
        private Renderer _renderer;
        private Collider _collider;
        private bool _draw = true;
    }
}
