using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RendererEditor.Targets
{
    public abstract class ATargetData
    {
        public readonly ITarget target;
        public bool currentEnabled;
        public readonly int parentOCIKey;
        public readonly string targetPath;
        public abstract TargetType targetDataType { get; }
        public abstract IDictionary<Material, MaterialData> dirtyMaterials { get; }

        protected ATargetData(ITarget target, bool currentEnabled)
        {
            this.target = target;
            this.currentEnabled = currentEnabled;

            Transform t = this.target.transform;
            KeyValuePair<int, Studio.ObjectCtrlInfo> pair;
            while ((pair = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(k => k.Value.guideObject.transformTarget == t)).Value == null)
                t = t.parent;
            this.parentOCIKey = pair.Key;
            this.targetPath = RendererEditor._self.GetPath(this.target, t);
        }
    }
}
