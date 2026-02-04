using System;
using System.Collections.Generic;
using System.Linq;
using Studio;
using UnityEngine;

namespace HSPE.AMModules
{
    public class GenericOCITarget
    {
        public enum Type
        {
            Unknown,
            Character,
            Item
        }

        private readonly Func<bool> _fkEnabledFunc;
        private readonly Func<bool> _ikEnabledFunc;

        public readonly ObjectCtrlInfo oci;
        public readonly Type type;
        public readonly OCIChar ociChar;
        public readonly bool isFemale;
        public readonly OCIItem ociItem;
        public readonly Dictionary<GameObject, OCIChar.BoneInfo> fkObjects = new Dictionary<GameObject, OCIChar.BoneInfo>();

        public bool fkEnabled { get { return _fkEnabledFunc(); } }
        public bool ikEnabled { get { return _ikEnabledFunc(); } }

        public GenericOCITarget(ObjectCtrlInfo oci)
        {
            this.oci = oci;
            if (this.oci is OCIChar)
            {
                type = Type.Character;
                ociChar = (OCIChar)this.oci;
#if HONEYSELECT
                this.isFemale = this.ociChar.charInfo.Sex == 1;
#elif KOIKATSU || PLAYHOME || AISHOUJO || HONEYSELECT2
                isFemale = ociChar.charInfo.sex == 1;
#endif
                foreach (OCIChar.BoneInfo bone in ociChar.listBones)
                {
                    if (bone.guideObject != null && bone.guideObject.transformTarget != null)
                        fkObjects.Add(bone.guideObject.transformTarget.gameObject, bone);
                }
                _fkEnabledFunc = () => ociChar.oiCharInfo.enableFK;
                _ikEnabledFunc = () => ociChar.oiCharInfo.enableIK;
            }
            else if (this.oci is OCIItem)
            {
                type = Type.Item;
                ociItem = (OCIItem)this.oci;
                if (ociItem.listBones != null)
                    foreach (OCIChar.BoneInfo bone in ociItem.listBones)
                    {
                        if (bone.guideObject != null && bone.guideObject.transformTarget != null)
                            fkObjects.Add(bone.guideObject.transformTarget.gameObject, bone);
                    }
                _fkEnabledFunc = () => ociItem.itemInfo.enableFK;
                _ikEnabledFunc = () => false;
            }
            else
                type = Type.Unknown;
        }

        public void RefreshFKBones()
        {
            if (type == Type.Character)
            {
                fkObjects.Clear();
                foreach (OCIChar.BoneInfo bone in ociChar.listBones)
                {
                    if (bone.guideObject != null && bone.guideObject.transformTarget != null)
                        fkObjects.Add(bone.guideObject.transformTarget.gameObject, bone);
                }
            }
        }

        public OCIChar.BoneInfo GetBoneInfo(GameObject go)
        {
            if (fkObjects.TryGetValue(go, out OCIChar.BoneInfo info))
                return info;

            switch (type)
            {
                case Type.Character:
                    {
                        foreach (var bone in ociChar.listBones.Where(bone => bone.guideObject && bone.guideObject.transformTarget && bone.guideObject.transformTarget.gameObject == go))
                        {
                            if (!fkObjects.ContainsKey(go))
                                fkObjects.Add(go, bone);
                            return bone;
                        }

                        if (ociChar.guideObject)
                        {
                            // Expensive fallback, but fine since it only handles missing GuideObjects and caches them.
                            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                            foreach (GuideObject guide in ociChar.guideObject.transform.GetComponentsInChildren<GuideObject>(true))
                            {
                                if (guide.transformTarget != go.transform) continue;
                                if (!ociChar.oiCharInfo.bones.TryGetValue(guide.dicKey, out OIBoneInfo oiBoneInfo)) continue;
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                                var newBone = new OCIChar.BoneInfo(guide, oiBoneInfo, 0);
#else
                                    var newBone = new OCIChar.BoneInfo(guide, oiBoneInfo);
#endif
                                if (!fkObjects.ContainsKey(go))
                                    fkObjects.Add(go, newBone);
                                return newBone;
                            }
                        }

                        break;
                    }
                case Type.Item:
                    {
                        if (ociItem.listBones != null)
                        {
                            foreach (var bone in ociItem.listBones.Where(bone => bone.guideObject && bone.guideObject.transformTarget && bone.guideObject.transformTarget.gameObject == go))
                            {
                                if (!fkObjects.ContainsKey(go))
                                    fkObjects.Add(go, bone);
                                return bone;
                            }
                        }

                        break;
                    }
            }
            return null;
        }

    }
}
