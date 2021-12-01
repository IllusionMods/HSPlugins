using Studio;
using System;
using System.Collections.Generic;
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

    }
}
