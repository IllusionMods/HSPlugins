using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ToolBox;
using UnityEngine;
using UnityEngine.Rendering;

namespace RendererEditor.Targets
{
    public struct RendererTarget : ITarget
    {
        private static readonly string[] _shadowCastingModesNames;
        private static readonly string[] _reflectionProbeUsageNames;

        public TargetType targetType { get { return TargetType.Renderer; }}
        public bool enabled { get { return this._target.enabled; } set { this._target.enabled = value; } }
        public string name { get { return this._target.name; } }
        public Material[] sharedMaterials { get { return this._target.sharedMaterials; } }
        public Material[] materials { get { return this._target.materials; } }
        public Transform transform { get { return this._target.transform; } }
        public bool hasBounds { get{ return true; } }
        public Bounds bounds { get { return this._target.bounds; } }
        public Component target { get { return this._target; } }

        internal readonly Renderer _target;

        static RendererTarget()
        {
            _shadowCastingModesNames = Enum.GetNames(typeof(ShadowCastingMode));
            _reflectionProbeUsageNames = Enum.GetNames(typeof(ReflectionProbeUsage));
        }

        public RendererTarget(Renderer target)
        {
            this._target = target;
        }

        public void ApplyData(ATargetData data)
        {
            RendererData rendererData = (RendererData)data;

            SetShadowCastingMode(this, rendererData.shadowCastingMode.currentValue);
            SetReceiveShadows(this, rendererData.receiveShadows.currentValue);
            SetReflectionProbeUsage(this, rendererData.reflectionProbeUsage.currentValue);
        }

        public void DisplayParams(HashSet<ITarget> selectedTargets)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cast Shadows");
            GUILayout.FlexibleSpace();

            bool newReceiveShadows = GUILayout.Toggle(this._target.receiveShadows, "Receive Shadows");
            if (newReceiveShadows != this._target.receiveShadows)
                SetAllTargetsValue(selectedTargets, r => SetReceiveShadows(r, newReceiveShadows));

            GUILayout.EndHorizontal();
            ShadowCastingMode newMode = (ShadowCastingMode)GUILayout.SelectionGrid((int)this._target.shadowCastingMode, _shadowCastingModesNames, 4);
            if (newMode != this._target.shadowCastingMode)
                SetAllTargetsValue(selectedTargets, r => SetShadowCastingMode(r, newMode));

            GUILayout.Label("Reflection Probe Usage");
            ReflectionProbeUsage newUsage = (ReflectionProbeUsage)GUILayout.SelectionGrid((int)this._target.reflectionProbeUsage, _reflectionProbeUsageNames, 2);
            if (newUsage != this._target.reflectionProbeUsage)
                SetAllTargetsValue(selectedTargets, r => SetReflectionProbeUsage(r, newUsage));
        }

        public static void SetReceiveShadows(RendererTarget target, bool receiveShadows)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData targetData);
            ((RendererData)targetData).receiveShadows.currentValue = receiveShadows;
            target._target.receiveShadows = receiveShadows;
        }

        public static void SetShadowCastingMode(RendererTarget target, ShadowCastingMode shadowCastingMode)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData targetData);
            ((RendererData)targetData).shadowCastingMode.currentValue = shadowCastingMode;
            target._target.shadowCastingMode = shadowCastingMode;
        }

        public static void SetReflectionProbeUsage(RendererTarget target, ReflectionProbeUsage reflectionProbeUsage)
        {
            RendererEditor._self.SetTargetDirty(target, out ATargetData targetData);
            ((RendererData)targetData).reflectionProbeUsage.currentValue = reflectionProbeUsage;
            target._target.reflectionProbeUsage = reflectionProbeUsage;
        }

        public ATargetData GetNewData()
        {
            return new RendererData(this, true, this._target.shadowCastingMode, this._target.receiveShadows, this._target.reflectionProbeUsage);
        }

        public void ResetData(ATargetData data)
        {
            RendererData rendererData = (RendererData)data;
            this._target.shadowCastingMode = rendererData.shadowCastingMode.originalValue;
            this._target.receiveShadows = rendererData.receiveShadows.originalValue;
            this._target.reflectionProbeUsage = rendererData.reflectionProbeUsage.originalValue;
        }

        public void LoadXml(XmlNode node)
        {
            this._target.shadowCastingMode = (ShadowCastingMode)XmlConvert.ToInt32(node.Attributes["shadowCastingMode"].Value);
            this._target.receiveShadows = XmlConvert.ToBoolean(node.Attributes["receiveShadows"].Value);
            if (node.Attributes["reflectionProbeUsage"] != null)
                this._target.reflectionProbeUsage = (ReflectionProbeUsage)XmlConvert.ToInt32(node.Attributes["reflectionProbeUsage"].Value);
        }

        public void SaveXml(XmlTextWriter writer)
        {
            writer.WriteAttributeString("shadowCastingMode", XmlConvert.ToString((int)this._target.shadowCastingMode));
            writer.WriteAttributeString("receiveShadows", XmlConvert.ToString(this._target.receiveShadows));
            writer.WriteAttributeString("reflectionProbeUsage", XmlConvert.ToString((int)this._target.reflectionProbeUsage));
        }

        private static void SetAllTargetsValue(HashSet<ITarget> targets, Action<RendererTarget> setValueFunction)
        {
            foreach (ITarget target in targets)
                if (target.targetType == TargetType.Renderer)
                    setValueFunction((RendererTarget)target);
        }

        public static implicit operator RendererTarget(Renderer r)
        {
            return new RendererTarget(r);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RendererTarget))
            {
                return false;
            }

            RendererTarget target = (RendererTarget)obj;
            return EqualityComparer<Renderer>.Default.Equals(this._target, target._target);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<Renderer>.Default.GetHashCode(this._target);
        }
    }

    public class RendererData : ATargetData
    {
        public override TargetType targetDataType { get { return TargetType.Renderer; } }
        public override IDictionary<Material, MaterialData> dirtyMaterials { get { return this._dirtyMaterials; } }

        public EditablePair<ShadowCastingMode> shadowCastingMode;
        public EditablePair<bool> receiveShadows;
        public EditablePair<ReflectionProbeUsage> reflectionProbeUsage;

        private readonly Dictionary<Material, MaterialData> _dirtyMaterials = new Dictionary<Material, MaterialData>();

        public RendererData(ITarget target, bool currentEnabled, ShadowCastingMode originalShadowCastingMode, bool originalReceiveShadows, ReflectionProbeUsage originalReflectionProbeUsage) : base(target, currentEnabled)
        {
            this.shadowCastingMode.originalValue = originalShadowCastingMode;
            this.receiveShadows.originalValue = originalReceiveShadows;
            this.reflectionProbeUsage.originalValue = originalReflectionProbeUsage;
        }
    }
}