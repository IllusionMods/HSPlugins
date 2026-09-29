using System;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using ToolBox.Extensions;
using Studio;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// ShalltyUtils' bone node, from ShalltyUtils by ShalltyB (github.com/ShalltyB/ShalltyUtils): a
    /// Studio node on the bone picked in the pose editor's advanced bones window, so it moves with the
    /// usual gizmo and selects, like any node, instead of with the pose editor's sliders. It stands in
    /// for the pose editor's own cube, which is hidden while it shows.
    ///
    /// Keying it keys the pose editor's bone tracks: the node itself is gone once another bone or
    /// object is picked, so a track on it would lead nowhere.
    /// </summary>
    public partial class Timeline
    {
        internal static ConfigEntry<bool> ConfigPoseEditorNode { get; private set; }

        /// <summary>The key the node is made with, which Studio's own nodes never use.</summary>
        private const int _poseEditorNodeKey = 169090;
        private static GuideObject _poseEditorNode;
        /// <summary>What the pose editor would show if its cube were not hidden for the node.</summary>
        private static bool _poseEditorGizmos = true;

        private const BindingFlags _peAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static Type _peController;
        private static FieldInfo _peBonesEditor, _peBoneTarget;
        private static MethodInfo _peSetPosition, _peSetRotation, _peSetScale;

        private static void BindPoseEditorNodeConfig(ConfigFile config)
        {
            ConfigPoseEditorNode = config.Bind("Pose editor", "Create bone GuideObject", true, "Makes a node on the bone picked in the pose editor's advanced bones window, to move it with the usual gizmo.");
        }

        /// <summary>Hooks the pose editor if it is there; it is not needed otherwise.</summary>
        private static void InitPoseEditorNode(Harmony harmony)
        {
            Type editor = AccessTools.TypeByName("HSPE.AMModules.BonesEditor");
            _peController = AccessTools.TypeByName("HSPE.PoseController");
            if (editor == null || _peController == null)
                return;
            _peBonesEditor = _peController.GetField("_bonesEditor", _peAll);
            _peBoneTarget = editor.GetField("_boneTarget", _peAll);
            _peSetPosition = editor.GetMethod("SetBoneTargetPosition", _peAll, null, new[] { typeof(Vector3) }, null);
            _peSetRotation = editor.GetMethod("SetBoneTargetRotation", _peAll, null, new[] { typeof(Quaternion) }, null);
            _peSetScale = editor.GetMethod("SetBoneTargetScale", _peAll, null, new[] { typeof(Vector3) }, null);
            MethodInfo changeTarget = editor.GetMethod("ChangeBoneTarget", _peAll);
            MethodInfo notDirty = editor.GetMethod("SetBoneNotDirtyIf", _peAll);
            MethodInfo gizmos = editor.GetMethod("GizmosEnabled", _peAll);
            if (_peBonesEditor == null || _peBoneTarget == null || _peSetPosition == null || _peSetRotation == null || _peSetScale == null ||
                changeTarget == null || notDirty == null || gizmos == null)
            {
                Logger.LogWarning("This version of the pose editor is not one the bone node knows how to work with.");
                _peController = null;
                return;
            }
            harmony.Patch(changeTarget, postfix: new HarmonyMethod(typeof(Timeline), nameof(ChangeBoneTargetPostfix)));
            harmony.Patch(notDirty, postfix: new HarmonyMethod(typeof(Timeline), nameof(SetBoneNotDirtyIfPostfix)));
            harmony.Patch(gizmos, postfix: new HarmonyMethod(typeof(Timeline), nameof(GizmosEnabledPostfix)));
            StudioSaveLoadApi.ObjectsSelected += (sender, e) => DeletePoseEditorNode();
            StudioSaveLoadApi.ObjectDeleted += (sender, e) => DeletePoseEditorNode();
        }

        private static void ChangeBoneTargetPostfix()
        {
            if (ConfigPoseEditorNode.Value && _poseEditorGizmos)
                CreatePoseEditorNode();
        }

        /// <summary>Resetting the bone in the pose editor moves it without telling the node, so it is made again.</summary>
        private static void SetBoneNotDirtyIfPostfix(GameObject go)
        {
            if (ConfigPoseEditorNode.Value && _poseEditorNode != null && GuideObjectManager.Instance.selectObject == _poseEditorNode && _poseEditorNode.transformTarget == go.transform)
                _self.ExecuteDelayed2(CreatePoseEditorNode);
        }

        private static bool GizmosEnabledPostfix(bool __result)
        {
            if (ConfigPoseEditorNode.Value == false)
                return __result;
            _poseEditorGizmos = __result;
            if (__result == false)
                DeletePoseEditorNode();
            return false;
        }

        private static void DeletePoseEditorNode()
        {
            if (_poseEditorNode != null)
                GuideObjectManager.Instance.Delete(_poseEditorNode, true);
            _poseEditorNode = null;
        }

        internal static bool IsPoseEditorNode(GuideObject node)
        {
            return node != null && node.dicKey == _poseEditorNodeKey;
        }

        private static void CreatePoseEditorNode()
        {
            DeletePoseEditorNode();
            ObjectCtrlInfo selected = KKAPI.Studio.StudioAPI.GetSelectedObjects().FirstOrDefault();
            Component controller = null;
            if (selected is OCIChar)
                controller = KKAPI.Studio.StudioObjectExtensions.GetChaControl((OCIChar)selected).GetComponent(_peController);
            else if (selected is OCIItem)
                controller = ((OCIItem)selected).objectItem.GetComponent(_peController);
            if (controller == null)
                return;
            object bonesEditor = _peBonesEditor.GetValue(controller);
            Transform bone = bonesEditor == null ? null : _peBoneTarget.GetValue(bonesEditor) as Transform;
            if (bone == null)
                return;

            var changeAmount = new ChangeAmount
            {
                pos = bone.localPosition,
                rot = bone.localRotation.eulerAngles,
                scale = bone.localScale
            };
            GuideObjectManager guides = GuideObjectManager.Instance;
            GameObject gameObject = Instantiate(guides.objectOriginal);
            gameObject.transform.SetParent(guides.transformWorkplace);
            GuideObject node = gameObject.GetComponent<GuideObject>();
            node.transformTarget = bone;
            node.dicKey = _poseEditorNodeKey;
            node.enablePos = true;
            node.enableRot = true;
            node.enableScale = true;
            node.enableMaluti = false;
            node.calcScale = false;
            node.scaleRate = 0.5f;
            node.scaleRot = 0.025f;
            node.scaleSelect = 0.05f;
            node.parentGuide = selected.guideObject;
            node.SetActive(false, true);
            node.changeAmount = changeAmount;
            changeAmount.onChangePos += () => _peSetPosition.Invoke(bonesEditor, new object[] { changeAmount.pos });
            changeAmount.onChangeRot += () => _peSetRotation.Invoke(bonesEditor, new object[] { Quaternion.Euler(changeAmount.rot) });
            changeAmount.onChangeScale += scale => _peSetScale.Invoke(bonesEditor, new object[] { scale });
            _poseEditorNode = node;
            guides.selectObject = node;
        }

        /// <summary>
        /// For the bone node, the pose editor's bone tracks stand in for node tracks: its owner, and the
        /// parameter it makes for the bone the pose editor has picked, which is the node's.
        /// </summary>
        private bool PoseEditorBone(ObjectCtrlInfo oci, out string owner, out object parameter)
        {
            owner = null;
            parameter = null;
            InterpolableModel model = _interpolableModelsList.FirstOrDefault(m => m.id == "bonePos" && m.owner != Compat.ShalltyTracks.Owner);
            if (model == null)
                return false;
            owner = model.owner;
            parameter = model.GetParameter(oci);
            return parameter != null;
        }
    }
}
