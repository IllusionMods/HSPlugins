using System.Linq;
using System.Collections.Generic;
using Studio;
using UnityEngine;
using UnityEngine.UI;

namespace HSPE
{
    internal static class BoneReorganizer
    {
        private static readonly List<Transform> _traversedTransforms = new List<Transform>();
        private static ScrollRect _kkpeFkScrollRect;

        public static void Init(ScrollRect kkpeFkScrollRect)
        {
            _kkpeFkScrollRect = kkpeFkScrollRect;
        }

        public static void Update()
        {
            if (_kkpeFkScrollRect != null && HSPE.ConfigReorderFKBones.Value.IsDown())
                UpdateAction();
        }

        #region private
        private static void UpdateAction()
        {
            var selectedItems = GetSelectedObjects();
            if (selectedItems.Any())
                foreach (ObjectCtrlInfo obj in selectedItems)
                {
                    SkinnedMeshRenderer[] meshes = obj.guideObject.transformTarget.GetComponentsInChildren<SkinnedMeshRenderer>();
                    List<Transform> rootBones = new List<Transform>();
                    foreach (SkinnedMeshRenderer mesh in meshes)
                        if (mesh.rootBone != null && !rootBones.Contains(mesh.rootBone))
                            rootBones.Add(mesh.rootBone);

                    foreach (Transform rootBone in rootBones)
                        DescendTransformTree(rootBone);

                    ReorganizeBones(obj);

                    _traversedTransforms.Clear();
                }
        }

        private static void DescendTransformTree(Transform transform)
        {
            _traversedTransforms.Add(transform);
            for (int i = 0; i < transform.childCount; i++)
                DescendTransformTree(transform.GetChild(i));
        }

        private static void ReorganizeBones(ObjectCtrlInfo obj)
        {
            if (!(obj is OCIItem))
                return;

            OCIItem item = (OCIItem)obj;

            //LogBonesOfItem(item, "bones original -----------");

            var kkpeBoneToggles = _kkpeFkScrollRect.content.GetComponentsInChildren<Toggle>();

            int curBoneIndex = 0;
            foreach (Transform bone in _traversedTransforms)
            {
                OCIChar.BoneInfo boneInfo = item.listBones.Find(b => b.guideObject.transformTarget.name.Equals(bone.name));
                if (boneInfo == null)
                    continue;

                int foundBoneIndex = item.listBones.IndexOf(boneInfo);
                OCIChar.BoneInfo backupBone = item.listBones[curBoneIndex];
                item.listBones[curBoneIndex] = boneInfo;
                item.listBones[foundBoneIndex] = backupBone;

                ReorganizeKKPEToggles(curBoneIndex, foundBoneIndex);

                curBoneIndex++;
            }

            //LogBonesOfItem(item, "bones corrected -----------");

            HSPE.Logger.LogMessage("Bone list reorganized");
        }

        private static void ReorganizeKKPEToggles(int curBoneIndex, int foundBoneIndex)
        {
            Transform backupToggleTransform = _kkpeFkScrollRect.content.transform.GetChild(curBoneIndex);
            _kkpeFkScrollRect.content.transform.GetChild(foundBoneIndex).SetSiblingIndex(curBoneIndex);
            backupToggleTransform.SetSiblingIndex(foundBoneIndex);
        }
        #endregion

        #region helper
        private static void LogBonesOfItem(OCIItem item, string separator = "")
        {
            if (!separator.IsNullOrEmpty())
                HSPE.Logger.LogMessage(separator);
            item.listBones.ForEach(x => HSPE.Logger.LogMessage(x.guideObject.transformTarget.name));
            if (!separator.IsNullOrEmpty())
                HSPE.Logger.LogMessage(separator);
        }

        // Copied from KKAPI to avoid new dependency
        // Summary:
        //     Get all objects (all types) currently selected in Studio's Workspace.
        private static IEnumerable<ObjectCtrlInfo> GetSelectedObjects()
        {
            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out var value))
                    yield return value;
        }
        #endregion
    }
}
