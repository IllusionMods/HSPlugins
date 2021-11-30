using System;
using System.Collections.Generic;
using System.Xml;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace RendererEditor.Targets
{
    public struct ProjectorTarget : ITarget
    {
        private static readonly Dictionary<Projector, Material[]> _materialInstances = new Dictionary<Projector, Material[]>();

        public TargetType targetType { get { return TargetType.Projector; }}
        public bool enabled { get { return this._target.enabled; } set { this._target.enabled = value; } }
        public string name { get { return this._target.name; } }
        public Material[] sharedMaterials { get { return this.materials; } }
        public Material[] materials
        {
            get
            {
                Material[] res;
                if (_materialInstances.TryGetValue(this._target, out res) == false) //Emulating the behaviour of a Renderer of having an instanced material
                {
                    this._target.material = new Material(this._target.material);
                    res = new[] { this._target.material };
                    _materialInstances.Add(this._target, res);
                }
                return res;
            }
        }
        public Transform transform { get { return this._target.transform; } }
        public bool hasBounds { get { return false; } }
        public Bounds bounds { get { return default(Bounds); } }
        public Component target { get { return this._target; } }

        internal readonly Projector _target;

        public ProjectorTarget(Projector target)
        {
            this._target = target;
        }

        public void ApplyData(ATargetData data)
        {
            ProjectorData projectorData = (ProjectorData)data;

            SetNearClipPlane(this, projectorData.nearClipPlane.currentValue);
            SetFarClipPlane(this, projectorData.farClipPlane.currentValue);
            SetAspectRatio(this, projectorData.aspectRatio.currentValue);
            SetOrthographic(this, projectorData.orthographic.currentValue);
            SetOrthographicSize(this, projectorData.orthographicSize.currentValue);
            SetFieldOfView(this, projectorData.fieldOfView.currentValue);
        }

        public void DisplayParams(HashSet<ITarget> selectedTargets)
        {
            IMGUIExtensions.HorizontalSliderWithValue("Near Clip Plane\t", this._target.nearClipPlane, 0.01f, 10f, "0.0000", newValue =>
            {
                SetAllTargetsValue(selectedTargets, t => SetNearClipPlane(t, newValue));
            });

            IMGUIExtensions.HorizontalSliderWithValue("Far Clip Plane\t", this._target.farClipPlane, 0.02f, 100f, "0.0000", newValue =>
            {
                SetAllTargetsValue(selectedTargets, t => SetFarClipPlane(t, newValue));
            });

            IMGUIExtensions.HorizontalSliderWithValue("Aspect Ratio\t", this._target.aspectRatio, 0.1f, 10f, "0.00", newValue =>
            {
                SetAllTargetsValue(selectedTargets, t => SetAspectRatio(t, newValue));
            });

            bool newOrthographic = GUILayout.Toggle(this._target.orthographic, "Orthographic");
            if (newOrthographic != this._target.orthographic)
                SetAllTargetsValue(selectedTargets, t => SetOrthographic(t, newOrthographic));

            if (newOrthographic)
                IMGUIExtensions.HorizontalSliderWithValue("Orthographic Size\t", this._target.orthographicSize, 0.01f, 10f, "0.00", newValue =>
                {
                    SetAllTargetsValue(selectedTargets, t => SetOrthographicSize(t, newValue));
                });
            else
                IMGUIExtensions.HorizontalSliderWithValue("FOV\t\t", this._target.fieldOfView, 1f, 179f, "0", newValue =>
                {
                    SetAllTargetsValue(selectedTargets, t => SetFieldOfView(t, newValue));
                });
        }

        public static void SetNearClipPlane(ProjectorTarget target, float nearClipPlane)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData data);
            ((ProjectorData)data).nearClipPlane.currentValue = nearClipPlane;
            target._target.nearClipPlane = nearClipPlane;
        }

        public static void SetFarClipPlane(ProjectorTarget target, float farClipPlane)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData data);
            ((ProjectorData)data).farClipPlane.currentValue = farClipPlane;
            target._target.farClipPlane = farClipPlane;
        }

        public static void SetAspectRatio(ProjectorTarget target, float aspectRatio)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData data);
            ((ProjectorData)data).aspectRatio.currentValue = aspectRatio;
            target._target.aspectRatio = aspectRatio;
        }

        public static void SetOrthographic(ProjectorTarget target, bool orthographic)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData data);
            ((ProjectorData)data).orthographic.currentValue = orthographic;
            target._target.orthographic = orthographic;
        }

        public static void SetOrthographicSize(ProjectorTarget target, float orthographicSize)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData data);
            ((ProjectorData)data).orthographicSize.currentValue = orthographicSize;
            target._target.orthographicSize = orthographicSize;
        }

        public static void SetFieldOfView(ProjectorTarget target, float fieldOfView)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData data);
            ((ProjectorData)data).fieldOfView.currentValue = fieldOfView;
            target._target.fieldOfView = fieldOfView;
        }

        public ATargetData GetNewData()
        {
            return new ProjectorData(this, true, this._target.nearClipPlane, this._target.farClipPlane, this._target.aspectRatio, this._target.orthographic, this._target.orthographicSize, this._target.fieldOfView);
        }

        public void ResetData(ATargetData data)
        {
            ProjectorData projectorData = (ProjectorData)data;
            this._target.nearClipPlane = projectorData.nearClipPlane.originalValue;
            this._target.farClipPlane = projectorData.farClipPlane.originalValue;
            this._target.aspectRatio = projectorData.aspectRatio.originalValue;
            this._target.orthographic = projectorData.orthographic.originalValue;
            this._target.orthographicSize = projectorData.orthographicSize.originalValue;
            this._target.fieldOfView = projectorData.fieldOfView.originalValue;
        }

        public void LoadXml(XmlNode node)
        {
            this._target.nearClipPlane = XmlConvert.ToSingle(node.Attributes["nearClipPlane"].Value);
            this._target.farClipPlane = XmlConvert.ToSingle(node.Attributes["farClipPlane"].Value);
            this._target.aspectRatio = XmlConvert.ToSingle(node.Attributes["aspectRatio"].Value);
            this._target.orthographic = XmlConvert.ToBoolean(node.Attributes["orthographic"].Value);
            this._target.orthographicSize = XmlConvert.ToSingle(node.Attributes["orthographicSize"].Value);
            this._target.fieldOfView = XmlConvert.ToSingle(node.Attributes["fieldOfView"].Value);
        }

        public void SaveXml(XmlTextWriter writer)
        {
            writer.WriteAttributeString("nearClipPlane", XmlConvert.ToString(this._target.nearClipPlane));
            writer.WriteAttributeString("farClipPlane", XmlConvert.ToString(this._target.farClipPlane));
            writer.WriteAttributeString("aspectRatio", XmlConvert.ToString(this._target.aspectRatio));
            writer.WriteAttributeString("orthographic", XmlConvert.ToString(this._target.orthographic));
            writer.WriteAttributeString("orthographicSize", XmlConvert.ToString(this._target.orthographicSize));
            writer.WriteAttributeString("fieldOfView", XmlConvert.ToString(this._target.fieldOfView));
        }

        private static void SetAllTargetsValue(HashSet<ITarget> targets, Action<ProjectorTarget> setValueFunction)
        {
            foreach (ITarget target in targets)
                if (target.targetType == TargetType.Projector)
                    setValueFunction((ProjectorTarget)target);
        }

        public static implicit operator ProjectorTarget(Projector r)
        {
            return new ProjectorTarget(r);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProjectorTarget))
            {
                return false;
            }

            ProjectorTarget target = (ProjectorTarget)obj;
            return EqualityComparer<Projector>.Default.Equals(this._target, target._target);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<Projector>.Default.GetHashCode(this._target);
        }
    }

    public class ProjectorData : ATargetData
    {
        public override TargetType targetDataType { get { return TargetType.Projector; } }
        public override IDictionary<Material, MaterialData> dirtyMaterials { get { return this._dirtyMaterials; } }

        public EditablePair<float> nearClipPlane;
        public EditablePair<float> farClipPlane;
        public EditablePair<float> aspectRatio;
        public EditablePair<bool> orthographic;
        public EditablePair<float> orthographicSize;
        public EditablePair<float> fieldOfView;

        private readonly Dictionary<Material, MaterialData> _dirtyMaterials = new Dictionary<Material, MaterialData>();

        public ProjectorData(ITarget target, bool currentEnabled, float originalNearClipPlane, float originalFarClipPlane, float originalAspectRatio, bool originalOrthographic, float originalOrthographicSize, float originalFieldOfView
        ) : base(target, currentEnabled)
        {
            this.nearClipPlane.originalValue = originalNearClipPlane;
            this.farClipPlane.originalValue = originalFarClipPlane;
            this.aspectRatio.originalValue = originalAspectRatio;
            this.orthographic.originalValue = originalOrthographic;
            this.orthographicSize.originalValue = originalOrthographicSize;
            this.fieldOfView.originalValue = originalFieldOfView;
        }
    }
}