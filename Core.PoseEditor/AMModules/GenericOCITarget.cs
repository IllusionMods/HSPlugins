using System;
using System.Collections.Generic;
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

        public bool fkEnabled { get { return this._fkEnabledFunc(); } }
        public bool ikEnabled { get { return this._ikEnabledFunc(); } }

        public GenericOCITarget(ObjectCtrlInfo oci)
        {
            this.oci = oci;
            if (this.oci is OCIChar)
            {
                this.type = Type.Character;
                this.ociChar = (OCIChar)this.oci;
#if HONEYSELECT
                this.isFemale = this.ociChar.charInfo.Sex == 1;
#elif KOIKATSU || PLAYHOME || AISHOUJO || HONEYSELECT2
                this.isFemale = this.ociChar.charInfo.sex == 1;
#endif
                foreach (OCIChar.BoneInfo bone in this.ociChar.listBones)
                {
                    if (bone.guideObject != null && bone.guideObject.transformTarget != null)
                        this.fkObjects.Add(bone.guideObject.transformTarget.gameObject, bone);
                }
                this._fkEnabledFunc = () => this.ociChar.oiCharInfo.enableFK;
                this._ikEnabledFunc = () => this.ociChar.oiCharInfo.enableIK;
            }
            else if (this.oci is OCIItem)
            {
                this.type = Type.Item;
                this.ociItem = (OCIItem)this.oci;
                if (this.ociItem.listBones != null)
                    foreach (OCIChar.BoneInfo bone in this.ociItem.listBones)
                    {
                        if (bone.guideObject != null && bone.guideObject.transformTarget != null)
                            this.fkObjects.Add(bone.guideObject.transformTarget.gameObject, bone);
                    }
                this._fkEnabledFunc = () => this.ociItem.itemInfo.enableFK;
                this._ikEnabledFunc = () => false;
            }
            else
                this.type = Type.Unknown;
        }

        public void RefreshFKBones()
        {
            if (this.type == Type.Character)
            {
                this.fkObjects.Clear();
                foreach (OCIChar.BoneInfo bone in this.ociChar.listBones)
                {
                    if (bone.guideObject != null && bone.guideObject.transformTarget != null)
                        this.fkObjects.Add(bone.guideObject.transformTarget.gameObject, bone);
                }
            }
        }

    }
}
