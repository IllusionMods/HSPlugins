using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Studio;
using ToolBox.Extensions;
using UnityEngine;

namespace Timeline.Compat
{
    /// <summary>
    /// The tracks ShalltyUtils adds to Timeline, provided here too, so that scenes made with it open and
    /// play without it.
    ///
    /// Written for this plugin from how those tracks behave and what they store; nothing is copied. What
    /// has to match exactly is what is in the scene file: the owner "ShalltyUtils", each id, the
    /// guideObjectPath or parameter attribute naming the node, and the single "value" each keyframe
    /// holds. The playback has to match too, down to a quirk: the bone rotation tracks read the bone's
    /// quaternion components and hand them to Quaternion.Euler as if they were angles. That is how
    /// those scenes were animated, so it is how they are played back here.
    /// </summary>
    internal static class ShalltyTracks
    {
        public const string Owner = "ShalltyUtils";

        public static void Register(Dictionary<Transform, GuideObject> guideObjects)
        {
            RegisterKinematics();
            RegisterGuideObjects(guideObjects);
            if (PoseEditor.Available)
                RegisterBones();
        }

        #region FK and IK
        private static void RegisterKinematics()
        {
            Timeline.AddInterpolableModelStatic(Owner, "fkEnabled", null, "FK Enabled",
                    (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        bool value = (bool)leftValue;
                        OCIChar character = (OCIChar)oci;
                        if (character.fkCtrl.enabled != value)
                        {
                            character.fkCtrl.enabled = value;
                            character.oiCharInfo.enableFK = value;
                            character.ActiveKinematicMode(OICharInfo.KinematicMode.FK, value, true);
                        }
                    },
                    null,
                    oci => oci is OCIChar,
                    (oci, parameter) => ((OCIChar)oci).fkCtrl.enabled && ((OCIChar)oci).oiCharInfo.enableFK,
                    (parameter, node) => node.ReadBool("value"),
                    (parameter, writer, o) => writer.WriteValue("value", (bool)o));

            Timeline.AddInterpolableModelStatic(Owner, "ikEnabled", null, "IK Enabled",
                    (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        bool value = (bool)leftValue;
                        OCIChar character = (OCIChar)oci;
                        if (character.oiCharInfo.enableIK != value)
                        {
                            character.oiCharInfo.enableIK = value;
                            character.ActiveKinematicMode(OICharInfo.KinematicMode.IK, value, true);
                        }
                    },
                    null,
                    oci => oci is OCIChar,
                    (oci, parameter) => ((OCIChar)oci).ikCtrl.enabled && ((OCIChar)oci).oiCharInfo.enableIK,
                    (parameter, node) => node.ReadBool("value"),
                    (parameter, writer, o) => writer.WriteValue("value", (bool)o));
        }
        #endregion

        #region Guide objects
        private static readonly string[] _axes = { "X", "Y", "Z" };

        /// <summary>(GO) X, Y, Z Position, Rotation and Scale: one axis of any node's guide object.</summary>
        private static void RegisterGuideObjects(Dictionary<Transform, GuideObject> guideObjects)
        {
            for (int a = 0; a < 3; ++a)
            {
                int axis = a;
                string letter = _axes[a];

                Register("guideObject" + letter + "Pos", "(GO) " + letter + " Position", "(GO) " + letter + " Pos", guideObjects,
                         (g, v) => g.changeAmount.pos = With(g.changeAmount.pos, axis, v),
                         g => g.changeAmount.pos[axis]);

                Register("guideObject" + letter + "Rot", "(GO) " + letter + " Rotation", "(GO) " + letter + " Rot", guideObjects,
                         null,
                         g => g.changeAmount.rot[axis],
                         (g, left, right, factor) =>
                         {
                             Vector3 current = g.changeAmount.rot;
                             Quaternion from = Quaternion.Euler(With(current, axis, left));
                             Quaternion to = Quaternion.Euler(With(current, axis, right));
                             g.changeAmount.rot = Quaternion.SlerpUnclamped(from, to, factor).eulerAngles;
                         });

                Register("guideObject" + letter + "Scale", "(GO) " + letter + " Scale", "(GO) " + letter + " Scale", guideObjects,
                         (g, v) => g.changeAmount.scale = With(g.changeAmount.scale, axis, v),
                         g => g.changeAmount.scale[axis]);
            }
        }

        private static void Register(string id, string name, string shortName, Dictionary<Transform, GuideObject> guideObjects,
                                     Action<GuideObject, float> set, Func<GuideObject, float> get,
                                     Action<GuideObject, float, float, float> blend = null)
        {
            InterpolableDelegate apply = (oci, parameter, leftValue, rightValue, factor) =>
            {
                GuideObject guide = (GuideObject)parameter;
                if (blend != null)
                    blend(guide, (float)leftValue, (float)rightValue, factor);
                else
                    set(guide, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor));
            };
            Timeline.AddInterpolableModelDynamic(Owner, id, name,
                    apply,
                    apply,
                    oci => oci != null,
                    (oci, parameter) => get((GuideObject)parameter),
                    (parameter, node) => node.ReadFloat("value"),
                    (parameter, writer, o) => writer.WriteValue("value", (float)o),
                    oci => GuideObjectManager.Instance.selectObject,
                    (oci, node) =>
                    {
                        XmlAttribute path = node.Attributes["guideObjectPath"];
                        if (path == null || oci == null || oci.guideObject == null)
                            return null;
                        Transform target = oci.guideObject.transformTarget.Find(path.Value);
                        GuideObject guide;
                        return target != null && guideObjects.TryGetValue(target, out guide) ? guide : null;
                    },
                    (oci, writer, o) => writer.WriteAttributeString("guideObjectPath", ((GuideObject)o).transformTarget.GetPathFrom(oci.guideObject.transformTarget)),
                    (oci, parameter, leftValue, rightValue) => parameter != null,
                    true,
                    (n, oci, parameter) => parameter is GuideObject ? shortName + " - (" + ((GuideObject)parameter).transformTarget.name + ")" : n);
        }

        private static Vector3 With(Vector3 v, int axis, float value)
        {
            v[axis] = value;
            return v;
        }
        #endregion

        #region Pose editor bones
        /// <summary>
        /// (KKPE) X, Y, Z Position, Rotation and Scale of the bone selected in the pose editor. The
        /// parameter is the character's bones editor and the bone, stored as the bone's path.
        /// </summary>
        private static void RegisterBones()
        {
            for (int a = 0; a < 3; ++a)
            {
                int axis = a;
                string letter = _axes[a];

                RegisterBone("bone" + letter + "Pos", "(KKPE) " + letter + " Position", "(KKPE) " + letter + " Pos",
                             (bone, left, right, factor) =>
                             {
                                 Vector3 position = PoseEditor.GetPosition(bone.editor, bone.bone);
                                 PoseEditor.SetPosition(bone.editor, bone.bone, With(position, axis, Mathf.LerpUnclamped(left, right, factor)));
                             },
                             bone => bone.bone.localPosition[axis]);

                // As ShalltyUtils has it: the stored value is a component of the local rotation quaternion,
                // and the other two angles are taken from the quaternion too. See the class summary.
                RegisterBone("bone" + letter + "Rot", "(KKPE) " + letter + " Rotation", "(KKPE) " + letter + " Rot",
                             (bone, left, right, factor) =>
                             {
                                 Quaternion q = bone.bone.localRotation;
                                 Vector3 current = new Vector3(q.x, q.y, q.z);
                                 Quaternion from = Quaternion.Euler(With(current, axis, left));
                                 Quaternion to = Quaternion.Euler(With(current, axis, right));
                                 PoseEditor.SetRotation(bone.editor, bone.bone, Quaternion.SlerpUnclamped(from, to, factor));
                             },
                             bone => axis == 0 ? bone.bone.localRotation.x : axis == 1 ? bone.bone.localRotation.y : bone.bone.localRotation.z);

                RegisterBone("bone" + letter + "Scale", "(KKPE) " + letter + " Scale", "(KKPE) " + letter + " Scale",
                             (bone, left, right, factor) =>
                             {
                                 Vector3 scale = PoseEditor.GetScale(bone.editor, bone.bone);
                                 PoseEditor.SetScale(bone.editor, bone.bone, With(scale, axis, Mathf.LerpUnclamped(left, right, factor)));
                             },
                             bone => bone.bone.localScale[axis]);
            }
        }

        private static void RegisterBone(string id, string name, string shortName, Action<BoneRef, float, float, float> apply, Func<BoneRef, float> get)
        {
            Timeline.AddInterpolableModelDynamic(Owner, id, name,
                    (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        BoneRef bone = parameter as BoneRef;
                        if (bone != null && bone.bone != null)
                            apply(bone, (float)leftValue, (float)rightValue, factor);
                    },
                    null,
                    oci => oci != null && oci.guideObject != null && PoseEditor.ControllerOf(oci) != null,
                    (oci, parameter) => get((BoneRef)parameter),
                    (parameter, node) => node.ReadFloat("value"),
                    (parameter, writer, o) => writer.WriteValue("value", (float)o),
                    oci =>
                    {
                        Component controller = PoseEditor.ControllerOf(oci);
                        object editor = PoseEditor.EditorOf(controller);
                        return new BoneRef(editor, PoseEditor.SelectedBone(editor));
                    },
                    (oci, node) =>
                    {
                        Component controller = PoseEditor.ControllerOf(oci);
                        XmlAttribute path = node.Attributes["parameter"];
                        if (controller == null || path == null)
                            return null;
                        return new BoneRef(PoseEditor.EditorOf(controller), controller.transform.Find(path.Value));
                    },
                    (oci, writer, parameter) => writer.WriteAttributeString("parameter", ((BoneRef)parameter).bone.GetPathFrom(oci.guideObject.transformTarget)),
                    (oci, parameter, leftValue, rightValue) => parameter is BoneRef && ((BoneRef)parameter).bone != null,
                    true,
                    (n, oci, parameter) => parameter is BoneRef && ((BoneRef)parameter).bone != null ? shortName + " (" + ((BoneRef)parameter).bone.name + ")" : n);
        }

        /// <summary>A bones editor and one of its bones: the parameter of a bone track, equal by both.</summary>
        internal sealed class BoneRef
        {
            public readonly object editor;
            public readonly Transform bone;

            public BoneRef(object editor, Transform bone)
            {
                this.editor = editor;
                this.bone = bone;
            }

            public override bool Equals(object obj)
            {
                BoneRef other = obj as BoneRef;
                return other != null && ReferenceEquals(other.editor, editor) && other.bone == bone;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((editor != null ? editor.GetHashCode() : 0) * 397) ^ (bone != null ? bone.GetHashCode() : 0);
                }
            }
        }

        /// <summary>
        /// The pose editor (KKPE, KKSPE, HS2PE) reached by name, so Timeline does not depend on it: the
        /// bone tracks simply do not exist in a game where it is not installed.
        /// </summary>
        private static class PoseEditor
        {
            private const BindingFlags _all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            private static bool _looked;
            private static Type _controller;
            private static FieldInfo _bonesEditor;
            private static FieldInfo _boneTarget;
            private static MethodInfo _getPosition, _setPosition, _setRotation, _getScale, _setScale;

            public static bool Available
            {
                get
                {
                    Look();
                    return _controller != null && _bonesEditor != null && _setPosition != null && _setRotation != null && _setScale != null;
                }
            }

            private static void Look()
            {
                if (_looked)
                    return;
                _looked = true;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type;
                    try
                    {
                        type = assembly.GetType("HSPE.PoseController", false);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    if (type == null)
                        continue;
                    _controller = type;
                    _bonesEditor = type.GetField("_bonesEditor", _all);
                    if (_bonesEditor == null)
                        return;
                    Type editor = _bonesEditor.FieldType;
                    _boneTarget = editor.GetField("_boneTarget", _all);
                    _getPosition = editor.GetMethod("GetBonePosition", _all, null, new[] { typeof(Transform) }, null);
                    _setPosition = editor.GetMethod("SetBonePosition", _all, null, new[] { typeof(Transform), typeof(Vector3) }, null);
                    _setRotation = editor.GetMethod("SetBoneRotation", _all, null, new[] { typeof(Transform), typeof(Quaternion) }, null);
                    _getScale = editor.GetMethod("GetBoneScale", _all, null, new[] { typeof(Transform) }, null);
                    _setScale = editor.GetMethod("SetBoneScale", _all, null, new[] { typeof(Transform), typeof(Vector3) }, null);
                    return;
                }
            }

            public static Component ControllerOf(ObjectCtrlInfo oci)
            {
                if (Available == false || oci == null || oci.guideObject == null || oci.guideObject.transformTarget == null)
                    return null;
                return oci.guideObject.transformTarget.GetComponent(_controller);
            }

            public static object EditorOf(Component controller)
            {
                return controller == null ? null : _bonesEditor.GetValue(controller);
            }

            public static Transform SelectedBone(object editor)
            {
                return editor == null || _boneTarget == null ? null : _boneTarget.GetValue(editor) as Transform;
            }

            public static Vector3 GetPosition(object editor, Transform bone)
            {
                return _getPosition != null ? (Vector3)_getPosition.Invoke(editor, new object[] { bone }) : bone.localPosition;
            }

            public static void SetPosition(object editor, Transform bone, Vector3 position)
            {
                _setPosition.Invoke(editor, new object[] { bone, position });
            }

            public static void SetRotation(object editor, Transform bone, Quaternion rotation)
            {
                _setRotation.Invoke(editor, new object[] { bone, rotation });
            }

            public static Vector3 GetScale(object editor, Transform bone)
            {
                return _getScale != null ? (Vector3)_getScale.Invoke(editor, new object[] { bone }) : bone.localScale;
            }

            public static void SetScale(object editor, Transform bone, Vector3 scale)
            {
                _setScale.Invoke(editor, new object[] { bone, scale });
            }
        }
        #endregion
    }
}
