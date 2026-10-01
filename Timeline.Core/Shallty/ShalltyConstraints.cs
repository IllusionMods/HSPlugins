using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using KKAPI.Utilities;
using Studio;
using Timeline.Compat;
using UnityEngine;
#if AISHOUJO || HONEYSELECT2
using AIChara;
#endif

namespace Timeline
{
    /// <summary>
    /// ShalltyUtils' constraint tools, from ShalltyUtils by ShalltyB (github.com/ShalltyB/ShalltyUtils),
    /// merged into Timeline. They sit in a small window beside NodesConstraints' own, as they did there:
    /// folder constraints for the IK nodes of a character (its "IK armature", with picker pages), for its
    /// soft bones, for a single bone, and a parent folder for the selected objects.
    ///
    /// A folder constraint is a folder that drives a bone through a NodesConstraints constraint, so the
    /// bone can be animated by animating the folder. With "Keep timeline" the bone's tracks move onto it.
    /// </summary>
    public partial class Timeline
    {
        internal static ConfigEntry<bool> ConfigKeepTimeline { get; private set; }
        internal static ConfigEntry<bool> ConfigHeightCompensation { get; private set; }

        private const int _constraintsWindowId = ('S' << 24) | ('U' << 16) | ('T' << 8);
        private Rect _constraintsWindowRect = new Rect(0f, 0f, 300f, 180f);
        private bool _constraintsWindowOpen = true;
        private bool _constraintSphere;
        private bool _constraintPos = true;
        private bool _constraintRot = true;
        private bool _constraintScale;
        private bool _constraintInverse;
        private bool _constraintWithParent;

        private static void BindConstraintConfig(ConfigFile config)
        {
            const string section = "Constraint tools";
            ConfigKeepTimeline = config.Bind(section, "Keep timeline", true, "When a folder constraint is made for a bone, the tracks animating that bone move onto the folder.");
            ConfigHeightCompensation = config.Bind(section, "Add Height Compensation", true, "Makes the IK armature follow the character's height, so the pose holds on characters of other heights.");
        }

        #region Window
        /// <summary>⋯ › Constraint tools: NodesConstraints' window, with the tools beside it open.</summary>
        internal void ShowConstraintTools()
        {
            _constraintsWindowOpen = true;
            _constraintsWindowRect.size = new Vector2(300f, _constraintsWindowRect.size.y);
            NodesConstraintsLink.ShowWindow();
        }

        private void DrawConstraintTools()
        {
            if (NodesConstraintsLink.WindowShown == false)
                return;
            Rect nodes = NodesConstraintsLink.WindowRect;
            _constraintsWindowRect.position = new Vector2(nodes.x + nodes.width, nodes.y);
            _constraintsWindowRect = GUILayout.Window(_constraintsWindowId, _constraintsWindowRect, ConstraintsWindow, "Timeline");
            IMGUIUtils.EatInputInRect(_constraintsWindowRect);
        }

        private void ConstraintsWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_constraintsWindowOpen ? "►" : "◄", GUILayout.ExpandHeight(true), GUILayout.Width(20f)))
            {
                _constraintsWindowOpen = !_constraintsWindowOpen;
                _constraintsWindowRect.size = new Vector2(_constraintsWindowOpen ? 300f : 30f, _constraintsWindowRect.size.y);
            }
            GUILayout.BeginVertical();
            if (_constraintsWindowOpen)
            {
                List<ObjectCtrlInfo> selected = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList();
                OCIChar character = selected.FirstOrDefault() as OCIChar;

                if (GUILayout.Button("Create FolderConstraints to IK Bones"))
                {
                    if (character == null)
                        Logger.LogMessage("First select a Character in the Workspace!");
                    else
                        CreateIKArmature(character);
                }
                if (GUILayout.Button("Create FolderConstraints to Misc Bones"))
                {
                    if (character == null)
                        Logger.LogMessage("First select a Character in the Workspace!");
                    else
                        CreateMiscBones(character);
                }

                GUILayout.Space(10f);
                string selectedItem = selected.Count > 1 ? "All Selected (" + selected.Count + ")" : selected.Count == 1 ? selected[0].treeNodeObject.textName : "(Nothing)";
                if (GUILayout.Button("Create Parent Folder to: " + selectedItem))
                {
                    if (selected.Count == 0)
                        Logger.LogMessage("First select an Item in the Workspace!");
                    else
                        CreateParentFolder(selected, "Parent", true, true);
                }

                GUILayout.Space(10f);
                GUILayout.BeginVertical(GUI.skin.box);
                Transform bone = NodesConstraintsLink.SelectedBone;
                if (GUILayout.Button("Create FolderConstraint to: " + (bone != null ? bone.name : "(Nothing)")))
                {
                    if (bone == null)
                        Logger.LogMessage("First select a GuideObject in the Workspace, or a Bone in the NodeConstraints window!");
                    else
                    {
                        ObjectCtrlInfo folder = CreateConstraint(bone, _constraintPos, _constraintRot, _constraintScale, _constraintSphere, _constraintInverse);
                        if (folder != null && _constraintWithParent && _constraintInverse == false && bone.parent != null)
                        {
                            ObjectCtrlInfo parentFolder = CreateConstraint(bone.parent, true, true, true, false, true);
                            if (parentFolder != null)
                                Studio.Studio.Instance.treeNodeCtrl.SetParent(folder.treeNodeObject, parentFolder.treeNodeObject.child[0]);
                        }
                    }
                }
                GUILayout.Space(5f);
                if (GUILayout.Button("Child type: " + (_constraintSphere ? "Sphere" : "Folder")))
                {
                    _constraintSphere = !_constraintSphere;
                    if (_constraintSphere == false)
                        _constraintScale = false;
                }
                GUILayout.BeginHorizontal();
                _constraintPos = GUILayout.Toggle(_constraintPos, "Position.");
                _constraintRot = GUILayout.Toggle(_constraintRot, "Rotation.");
                GUI.enabled = _constraintSphere;
                _constraintScale = GUILayout.Toggle(_constraintScale, "Scale.");
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                _constraintInverse = GUILayout.Toggle(_constraintInverse, "Reversed FolderConstraint.");
                GUI.enabled = _constraintInverse == false;
                _constraintWithParent = GUILayout.Toggle(_constraintWithParent, "Create with Parent.");
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Armatures
        private static readonly Color[] _armatureButtonColors = { Color.magenta, Color.red, Color.blue, Color.red, Color.red, Color.blue, Color.red, Color.red, Color.blue, Color.red, Color.red, Color.blue, Color.red };

        /// <summary>Where each IK node's button goes on the armature's picker pages, as ShalltyUtils laid them out.</summary>
        private static readonly Vector2[][] _armatureButtons =
        {
            new[] { new Vector2(270.1357f, 1759.771f), new Vector2(370.1357f, 1809.771f) },
            new[] { new Vector2(352f, 1858.5f), new Vector2(452f, 1908.5f) },
            new[] { new Vector2(429f, 1809f), new Vector2(529f, 1859f) },
            new[] { new Vector2(457.6542f, 1762.008f), new Vector2(557.6542f, 1812.008f) },
            new[] { new Vector2(170.1357f, 1859.771f), new Vector2(270.1357f, 1909.771f) },
            new[] { new Vector2(122.4637f, 1814.104f), new Vector2(222.4637f, 1864.104f) },
            new[] { new Vector2(77.36797f, 1753.761f), new Vector2(177.368f, 1803.761f) },
            new[] { new Vector2(349.254f, 1657.124f), new Vector2(449.254f, 1707.124f) },
            new[] { new Vector2(379.254f, 1607.124f), new Vector2(479.254f, 1657.124f) },
            new[] { new Vector2(399.254f, 1557.124f), new Vector2(499.254f, 1607.124f) },
            new[] { new Vector2(199.2539f, 1657.124f), new Vector2(299.2539f, 1707.124f) },
            new[] { new Vector2(174.2539f, 1607.124f), new Vector2(274.2539f, 1657.124f) },
            new[] { new Vector2(149.2539f, 1557.124f), new Vector2(249.2539f, 1607.124f) }
        };

        private static string CharacterName(OCIChar character)
        {
            ChaControl chaCtrl = KKAPI.Studio.StudioObjectExtensions.GetChaControl(character);
            return KKAPI.Chara.CharacterExtensions.GetFancyCharacterName(chaCtrl.chaFile);
        }

        /// <summary>
        /// A folder constraint on each IK node of the character, under one folder, with their tracks in
        /// one group and two picker pages: one for the folders that animate and one for the constraints.
        /// </summary>
        private void CreateIKArmature(OCIChar character)
        {
            TreeNodeCtrl tree = Studio.Studio.Instance.treeNodeCtrl;
            TreeNodeObject parent = character.treeNodeObject.parent;
            if (parent != null)
                tree.SetParent(character.treeNodeObject, null);

            string charaName = CharacterName(character);
            string armatureName = charaName + " | IK Armature";

            ObjectCtrlInfo armature = null;
            if (ConfigHeightCompensation.Value)
            {
                Transform height = FindBone(character.guideObject.transformTarget, "cf_n_height");
                if (height != null)
                {
                    bool keep = ConfigKeepTimeline.Value;
                    ConfigKeepTimeline.Value = false;
                    try
                    {
                        armature = CreateConstraint(height, false, false, true, true, true, "", false, character);
                    }
                    finally
                    {
                        ConfigKeepTimeline.Value = keep;
                    }
                    if (armature != null)
                    {
                        tree.DeleteNode(armature.treeNodeObject.child[0]);
                        armature.treeNodeObject.textName = armatureName;
                    }
                }
            }
            if (armature == null)
            {
                OCIFolder folder = AddObjectFolder.Add();
                folder.name = armatureName;
                folder.guideObject.transformTarget.name = armatureName;
                armature = folder;
            }
            armature.guideObject.changeAmount.Copy(character.guideObject.changeAmount);

            var newConstraints = new List<ObjectCtrlInfo>();
            foreach (OCIChar.IKInfo ik in character.listIKTarget)
            {
                ObjectCtrlInfo constraint = CreateConstraint(ik.targetObject, true, true, false, false, false, "", false, character);
                if (constraint == null)
                    continue;
                tree.SetParent(constraint.treeNodeObject, armature.treeNodeObject);
                newConstraints.Add(constraint);
            }

            if (parent != null)
            {
                tree.SetParent(character.treeNodeObject, parent);
                tree.SetParent(armature.treeNodeObject, parent);
            }
            armature.treeNodeObject.Select();

            List<Interpolable> tracks = _interpolables.Values.Where(t => newConstraints.Any(oci => ReferenceEquals(oci, t.oci))).ToList();
            Color groupColor = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            if (tracks.Count > 0)
            {
                foreach (Interpolable track in tracks)
                    track.color = groupColor;
                _interpolablesTree.GroupTogether(tracks, new InterpolableGroup { name = armatureName });
                // The armature's group holds the tracks of several constraint objects; only the grouped list shows it as one.
                _view?.ShowGroupedIfMixed();
            }

            var animPage = new PickerPage { name = charaName + " | Animation", color = groupColor, showNodes = true };
            var constraintsPage = new PickerPage { name = charaName + " | Constraints", color = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f), showNodes = false };
            Dictionary<TreeNodeObject, ObjectCtrlInfo> ocis = Studio.Studio.Instance.dicInfo;
            for (int i = 0; i < newConstraints.Count && i < _armatureButtons.Length; i++)
            {
                ObjectCtrlInfo oci = newConstraints[i];
                ObjectCtrlInfo constraint;
                if (oci.treeNodeObject.child.Count == 0 || ocis.TryGetValue(oci.treeNodeObject.child[0], out constraint) == false)
                    continue;
                Rect box = ShalltyBox(_armatureButtons[i][0], _armatureButtons[i][1]);
                animPage.buttons.Add(new PickerButton { label = oci.treeNodeObject.textName, color = _armatureButtonColors[i], box = box, target = oci.guideObject });
                constraintsPage.buttons.Add(new PickerButton { label = constraint.treeNodeObject.textName, color = _armatureButtonColors[i], box = box, target = constraint.guideObject });
            }
            foreach (PickerPage page in new[] { animPage, constraintsPage })
            {
                if (page.buttons.Count == 0)
                    continue;
                FlipShalltyBoxes(page);
                _pickerPages.Add(page);
            }
            if (animPage.buttons.Count != 0)
                _pickerPage = _pickerPages.IndexOf(animPage);

            UpdateInterpolablesView();
            if (_view != null)
                _view.RefreshFloats();
        }

        /// <summary>Folder constraints on the soft bones: bust, spine, buttocks, waist and thighs.</summary>
        private void CreateMiscBones(OCIChar character)
        {
            TreeNodeCtrl tree = Studio.Studio.Instance.treeNodeCtrl;
            TreeNodeObject parent = character.treeNodeObject.parent;
            if (parent != null)
                tree.SetParent(character.treeNodeObject, null);

            OCIFolder folder = AddObjectFolder.Add();
            folder.guideObject.changeAmount.Copy(character.guideObject.changeAmount);
            folder.name = CharacterName(character) + " | Misc Bones";
            folder.guideObject.transformTarget.name = folder.name;

            Transform root = character.guideObject.transformTarget;
            int missing = 0;
            MiscPart(root, folder, character, ref missing, "cf_d_bust00", true, true, true, true, null, new[]
            {
                new[] { "cf_d_bust01_R", "sphere", "scale", "BUST R" },
                new[] { "cf_d_bust01_L", "sphere", "scale", "BUST L" }
            });
            MiscPart(root, folder, character, ref missing, "cf_j_spine01", true, true, false, false, null, new[]
            {
                new[] { "cf_s_spine01", "sphere", "" },
                new[] { "cf_s_spine02", "sphere", "" }
            });
            MiscPart(root, folder, character, ref missing, "cf_j_waist02", true, true, true, false, "SIRI", new[]
            {
                new[] { "cf_d_siri01_R", "sphere", "scale", "SIRI R" },
                new[] { "cf_d_siri01_L", "sphere", "scale", "SIRI L" }
            });
            MiscPart(root, folder, character, ref missing, "cf_j_waist02", true, true, false, false, "WAIST", new[]
            {
                new[] { "cf_s_waist02", "folder", "", "WAIST" }
            });
            MiscPart(root, folder, character, ref missing, "cf_j_thigh00_L", true, true, true, false, null, new[]
            {
                new[] { "cf_s_thigh01_L", "sphere", "scale" },
                new[] { "cf_d_thigh02_L", "sphere", "scale" }
            });
            MiscPart(root, folder, character, ref missing, "cf_j_thigh00_R", true, true, true, false, null, new[]
            {
                new[] { "cf_s_thigh01_R", "sphere", "scale" },
                new[] { "cf_d_thigh02_R", "sphere", "scale" }
            });

            if (parent != null)
            {
                tree.SetParent(character.treeNodeObject, parent);
                tree.SetParent(folder.treeNodeObject, parent);
            }
            folder.treeNodeObject.Select();
            if (missing != 0)
                Logger.LogMessage(missing + " bone(s) this character does not have were left out.");
            UpdateInterpolablesView();
        }

        /// <summary>One part of the soft bones: a reversed constraint on its root bone, and its bones' constraints under it.</summary>
        private void MiscPart(Transform root, OCIFolder folder, OCIChar character, ref int missing, string rootBone, bool pos, bool rot, bool scale, bool sphere, string name, string[][] children)
        {
            Transform bone = FindBone(root, rootBone);
            if (bone == null)
            {
                ++missing;
                return;
            }
            ObjectCtrlInfo partFolder = CreateConstraint(bone, pos, rot, scale, sphere, true, name ?? "", false, character);
            if (partFolder == null)
                return;
            TreeNodeCtrl tree = Studio.Studio.Instance.treeNodeCtrl;
            // Each child: its bone, "sphere" or "folder", "scale" or not, and a name if not the bone's.
            foreach (string[] child in children)
            {
                Transform childBone = FindBone(root, child[0]);
                if (childBone == null)
                {
                    ++missing;
                    continue;
                }
                ObjectCtrlInfo childFolder = CreateConstraint(childBone, true, true, child[2] == "scale", child[1] == "sphere", false, child.Length > 3 ? child[3] : "", false, character);
                if (childFolder != null)
                    tree.SetParent(childFolder.treeNodeObject, partFolder.treeNodeObject.child[0]);
            }
            tree.SetParent(partFolder.treeNodeObject, folder.treeNodeObject);
        }

        /// <summary>A bone under the character by name, whatever the case: the games spell them differently.</summary>
        private static Transform FindBone(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase))
                    return t;
            }
            return null;
        }
        #endregion

        #region Folder constraints
        private static readonly Dictionary<string, string> _fancyBoneNames = new Dictionary<string, string>
        {
            { "cf_t_hips(work)", "HIPS" }, { "cf_t_shoulder_L(work)", "Left Shoulder" }, { "cf_t_elbo_L(work)", "Left Elbow" },
            { "cf_t_hand_L(work)", "Left Hand" }, { "cf_t_shoulder_R(work)", "Right Shoulder" }, { "cf_t_elbo_R(work)", "Right Elbow" },
            { "cf_t_hand_R(work)", "Right Hand" }, { "cf_t_waist_L(work)", "Left Thigh" }, { "cf_t_knee_L(work)", "Left Knee" },
            { "cf_t_leg_L(work)", "Left Foot" }, { "cf_t_waist_R(work)", "Right Thigh" }, { "cf_t_knee_R(work)", "Right Knee" },
            { "cf_t_leg_R(work)", "Right Foot" }, { "cf_j_head", "Head" }, { "cf_j_neck", "Neck" },
            { "cf_j_thumb01_R", "Right Thumb 1" }, { "cf_j_thumb02_R", "Right Thumb 2" }, { "cf_j_thumb03_R", "Right Thumb 3" },
            { "cf_j_index01_R", "Right Index 1" }, { "cf_j_index02_R", "Right Index 2" }, { "cf_j_index03_R", "Right Index 3" },
            { "cf_j_middle01_R", "Right Middle 1" }, { "cf_j_middle02_R", "Right Middle 2" }, { "cf_j_middle03_R", "Right Middle 3" },
            { "cf_j_ring01_R", "Right Ring 1" }, { "cf_j_ring02_R", "Right Ring 2" }, { "cf_j_ring03_R", "Right Ring 3" },
            { "cf_j_little01_R", "Right Little 1" }, { "cf_j_little02_R", "Right Little 2" }, { "cf_j_little03_R", "Right Little 3" },
            { "cf_j_thumb01_L", "Left Thumb 1" }, { "cf_j_thumb02_L", "Left Thumb 2" }, { "cf_j_thumb03_L", "Left Thumb 3" },
            { "cf_j_index01_L", "Left Index 1" }, { "cf_j_index02_L", "Left Index 2" }, { "cf_j_index03_L", "Left Index 3" },
            { "cf_j_middle01_L", "Left Middle 1" }, { "cf_j_middle02_L", "Left Middle 2" }, { "cf_j_middle03_L", "Left Middle 3" },
            { "cf_j_ring01_L", "Left Ring 1" }, { "cf_j_ring02_L", "Left Ring 2" }, { "cf_j_ring03_L", "Left Ring 3" },
            { "cf_j_little01_L", "Left Little 1" }, { "cf_j_little02_L", "Left Little 2" }, { "cf_j_little03_L", "Left Little 3" },
            { "cf_j_thigh00_R", "Right THIGH" }, { "cf_j_thigh00_L", "Left THIGH" }, { "cf_j_spine01", "SPINE" },
            { "cf_d_bust00", "BUST" }
        };

        private static string FancyBoneName(string name)
        {
            string fancy;
            return _fancyBoneNames.TryGetValue(name, out fancy) ? fancy : name;
        }

        /// <summary>
        /// Puts the objects under a new folder, placed where their parent was. Centred, the folder sits in
        /// the middle of them and they keep where they are in the world, their position keys included.
        /// </summary>
        private OCIFolder CreateParentFolder(List<ObjectCtrlInfo> childList, string name = "Parent", bool selectParent = true, bool centerParent = false)
        {
            if (childList.Count == 0)
                return null;
            TreeNodeCtrl tree = Studio.Studio.Instance.treeNodeCtrl;
            OCIFolder folder = AddObjectFolder.Add();
            folder.name = childList[0].treeNodeObject.textName + " | " + name;
            folder.guideObject.transformTarget.name = folder.name;
            if (childList[0].treeNodeObject.parent != null)
                tree.SetParent(folder.treeNodeObject, childList[0].treeNodeObject.parent);

            Vector3 centerPos = Vector3.zero;
            if (centerParent)
            {
                foreach (ObjectCtrlInfo child in childList)
                    centerPos += child.guideObject.changeAmount.pos;
                centerPos /= childList.Count;
                folder.guideObject.changeAmount.pos = Vector3.zero;
            }
            foreach (ObjectCtrlInfo child in childList)
                tree.SetParent(child.treeNodeObject, folder.treeNodeObject);
            if (centerParent)
                MoveGuideObject(folder.guideObject, centerPos, true);

            if (selectParent)
            {
                folder.treeNodeObject.Select();
                folder.treeNodeObject.SetTreeState(TreeNodeObject.TreeState.Open);
            }
            return folder;
        }

        /// <summary>
        /// Moves a node and its position keys by the same amount. With ignoreChildren the objects under it
        /// are moved back, keys too, so they stay where they were in the world.
        /// </summary>
        private void MoveGuideObject(GuideObject guideObject, Vector3 newPosition, bool ignoreChildren)
        {
            Vector3 moveBy = newPosition - guideObject.changeAmount.pos;
            ShiftPositionKeys(guideObject, moveBy);
            guideObject.changeAmount.pos = newPosition;
            if (ignoreChildren == false)
                return;
            GuideObject root = guideObject.parentGuide ?? guideObject;
            TreeNodeObject node = Studio.Studio.Instance.dicInfo.FirstOrDefault(p => ReferenceEquals(p.Value.guideObject, root)).Key;
            if (node == null)
                return;
            foreach (TreeNodeObject child in node.child)
            {
                ObjectCtrlInfo oci;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out oci) == false)
                    continue;
                oci.guideObject.changeAmount.pos -= moveBy;
                ShiftPositionKeys(oci.guideObject, -moveBy);
            }
        }

        private void ShiftPositionKeys(GuideObject guideObject, Vector3 by)
        {
            Interpolable track = _interpolables.Values.FirstOrDefault(t => t.id == "guideObjectPos" && ReferenceEquals(t.parameter, guideObject));
            if (track == null)
                return;
            foreach (Keyframe keyframe in track.keyframes.Values)
                keyframe.value = (Vector3)keyframe.value + by;
        }

        /// <summary>
        /// A folder that drives the bone (or, reversed, follows it) through a new constraint, with a hidden
        /// folder or sphere holding the constraint's end under it. Returns the folder to animate; with
        /// splitAxis the constraint's end sits under an X, a Y and a Z folder, one per axis.
        /// </summary>
        private ObjectCtrlInfo CreateConstraint(Transform targetBone, bool pos, bool rot, bool scale, bool isSphere, bool inverse = false, string name = "", bool splitAxis = false, ObjectCtrlInfo parentOci = null)
        {
            if (targetBone == null || NodesConstraintsLink.Available == false)
                return null;
            if (parentOci == null)
                parentOci = KKAPI.Studio.StudioAPI.GetSelectedObjects().FirstOrDefault();
            if (name == "")
                name = targetBone.name;
            name = FancyBoneName(name);

            string alias = name + " | Constraint";
            OCIChar character = parentOci as OCIChar;
            if (character != null)
                alias = name + " | " + CharacterName(character);

            ObjectCtrlInfo folder = inverse && isSphere ? (ObjectCtrlInfo)AddObjectItem.Add(0, 0, 0) : AddObjectFolder.Add();
            ObjectCtrlInfo sphere = isSphere ? (ObjectCtrlInfo)AddObjectItem.Add(0, 0, 0) : AddObjectFolder.Add();
            if (folder == null || sphere == null)
                return null;
            sphere.treeNodeObject.SetVisible(false);

            Vector3 sphereScale = sphere.guideObject.transformTarget.lossyScale, boneScale = targetBone.lossyScale;
            Vector3 scaleOffset = inverse ? new Vector3(sphereScale.x / boneScale.x, sphereScale.y / boneScale.y, sphereScale.z / boneScale.z)
                                          : new Vector3(boneScale.x / sphereScale.x, boneScale.y / sphereScale.y, boneScale.z / sphereScale.z);

            sphere.guideObject.changeAmount.pos = targetBone.position;
            sphere.guideObject.changeAmount.rot = targetBone.rotation.eulerAngles;
            folder.guideObject.changeAmount.Copy(sphere.guideObject.changeAmount);

            SetName(folder, name + " | Animation");
            SetName(sphere, name + " | (Constraint)");
            folder.guideObject.transformTarget.name = folder.treeNodeObject.textName;
            sphere.guideObject.transformTarget.name = folder.treeNodeObject.textName;

            TreeNodeCtrl tree = Studio.Studio.Instance.treeNodeCtrl;
            OCIFolder folderX = null, folderY = null, folderZ = null;
            object constraint;
            if (inverse == false)
            {
                constraint = NodesConstraintsLink.Add(true, sphere.guideObject.transformTarget, targetBone, pos, Vector3.zero, rot, Quaternion.identity, scale, scaleOffset, alias);
                tree.SetParent(sphere.treeNodeObject, folder.treeNodeObject);
                if (splitAxis)
                {
                    folderX = CreateParentFolder(new List<ObjectCtrlInfo> { sphere }, "X", false, true);
                    folderY = CreateParentFolder(new List<ObjectCtrlInfo> { sphere }, "Y", false, true);
                    folderZ = CreateParentFolder(new List<ObjectCtrlInfo> { sphere }, "Z", false, true);
                    sphere.guideObject.changeAmount.Reset();
                    folder.guideObject.changeAmount.Reset();
                    folderX.guideObject.changeAmount.Reset();
                    folderY.guideObject.changeAmount.Reset();
                    folderZ.guideObject.changeAmount.Reset();
                }
            }
            else
            {
                constraint = NodesConstraintsLink.Add(true, targetBone, sphere.guideObject.transformTarget, pos, Vector3.zero, rot, Quaternion.identity, scale, scaleOffset, alias);
                tree.SetParent(folder.treeNodeObject, sphere.treeNodeObject);
            }
            Logger.LogInfo("New constraint created to bone: " + targetBone.name);

            if (ConfigKeepTimeline.Value && parentOci != null)
            {
                if (splitAxis)
                    MoveAxisTracksToFolders(parentOci, targetBone, folder, folderX, folderY, folderZ, constraint);
                else
                    MoveTracksToRig(parentOci, targetBone, folder, name);
                UpdateInterpolablesView();
            }
            return inverse ? sphere : folder;
        }

        private static void SetName(ObjectCtrlInfo oci, string name)
        {
            OCIFolder folder = oci as OCIFolder;
            if (folder != null)
                folder.name = name;
            else
                oci.treeNodeObject.textName = name;
        }

        /// <summary>Which axis and which transform a per axis track animates, in Timeline's ids or ShalltyUtils'.</summary>
        private static bool AxisTrack(string id, out int axis, out bool rotation, out bool bone)
        {
            axis = -1;
            rotation = false;
            bone = false;
            string rest;
            if (id.StartsWith("guideObject"))
                rest = id.Substring("guideObject".Length);
            else if (id.StartsWith("bone"))
            {
                rest = id.Substring("bone".Length);
                bone = true;
            }
            else
                return false;
            // PosX in Timeline's ids and the pose editor's, XPos in ShalltyUtils'.
            if (rest.Length != 4)
                return false;
            bool letterFirst = "XYZ".IndexOf(rest[0]) >= 0;
            string kind = letterFirst ? rest.Substring(1) : rest.Substring(0, 3);
            char letter = letterFirst ? rest[0] : rest[3];
            if (kind != "Pos" && kind != "Rot")
                return false;
            axis = "XYZ".IndexOf(letter);
            rotation = kind == "Rot";
            return axis >= 0;
        }

        /// <summary>
        /// Each per axis track of the bone becomes a whole position or rotation track on the folder of its
        /// axis, holding that axis only, so the three folders stacked give back the three axes.
        /// </summary>
        private void MoveAxisTracksToFolders(ObjectCtrlInfo owner, Transform targetBone, ObjectCtrlInfo folder, ObjectCtrlInfo folderX, ObjectCtrlInfo folderY, ObjectCtrlInfo folderZ, object constraint)
        {
            InterpolableModel modelPos = GetModel(_ownerId, "guideObjectPos");
            InterpolableModel modelRot = GetModel(_ownerId, "guideObjectRot");
            ObjectCtrlInfo[] axisFolders = { folderX, folderY, folderZ };
            bool isPos = false, isRot = false;
            var added = new List<Interpolable>();
            foreach (Interpolable track in _interpolables.Values.Where(t => t.oci == owner).ToList())
            {
                int axis;
                bool rotation, bone;
                if (AxisTrack(track.id, out axis, out rotation, out bone) == false)
                    continue;
                GuideObject node = track.parameter as GuideObject;
                if (node != null ? node.transformTarget != targetBone : BoneOf(track.parameter) != targetBone)
                    continue;
                // A pose editor's bone moves on the folder itself, ShalltyUtils only split nodes per folder.
                ObjectCtrlInfo target = bone ? folder : axisFolders[axis];
                var moved = new Interpolable(target, target.guideObject, rotation ? modelRot : modelPos);
                foreach (KeyValuePair<float, Keyframe> pair in track.keyframes)
                {
                    float value = (float)pair.Value.value;
                    Vector3 v = Vector3.zero;
                    v[axis] = value;
                    object whole = rotation ? (object)Quaternion.Euler(v) : v;
                    moved.keyframes.Add(pair.Key, new Keyframe(whole, moved, new AnimationCurve(pair.Value.curve.keys)));
                }
                string letter = "XYZ"[axis] + (rotation ? " Rot" : " Pos");
                moved.alias = string.IsNullOrEmpty(track.alias) ? moved.name : letter + " | " + track.alias;
                track.enabled = false;
                if (rotation)
                    isRot = true;
                else
                    isPos = true;
                if (moved.keyframes.Count != 0 && _interpolables.ContainsKey(moved.GetHashCode()) == false)
                    added.Add(moved);
            }
            if (constraint != null)
            {
                NodesConstraintsLink.Set(constraint, "position", isPos);
                NodesConstraintsLink.Set(constraint, "rotation", isRot);
            }
            foreach (Interpolable track in added)
            {
                _interpolables.Add(track.GetHashCode(), track);
                _interpolablesTree.AddLeaf(track);
            }
        }

        /// <summary>
        /// Every node animated by per axis tracks gets a folder constraint with a folder per axis, all under
        /// one folder, each also following the node's parent; the tracks move onto them.
        /// </summary>
        internal void ConvertAxisTracksToFolders()
        {
            if (NodesConstraintsLink.Available == false)
            {
                Logger.LogMessage("This needs NodesConstraints.");
                return;
            }
            var childFolders = new List<ObjectCtrlInfo>();
            var done = new HashSet<GuideObject>();
            foreach (Interpolable track in _interpolables.Values.ToList())
            {
                int axis;
                bool rotation, bone;
                GuideObject node = track.parameter as GuideObject;
                if (node == null || AxisTrack(track.id, out axis, out rotation, out bone) == false || bone || done.Add(node) == false)
                    continue;
                ObjectCtrlInfo folder = CreateConstraint(node.transformTarget, false, false, false, false, false, "", true, track.oci);
                if (folder == null)
                    continue;
                if (node.transformTarget.parent != null)
                    NodesConstraintsLink.Add(true, node.transformTarget.parent, folder.guideObject.transformTarget, true, Vector3.zero, true, Quaternion.identity, true, Vector3.one,
                                             node.transformTarget.parent.name + " -> " + folder.guideObject.transformTarget.name);
                childFolders.Add(folder);
            }
            if (childFolders.Count == 0)
            {
                Logger.LogMessage("There are no (X/Y/Z) Interpolables!");
                return;
            }
            OCIFolder parentFolder = AddObjectFolder.Add();
            parentFolder.name = "Baked XYZ Interpolables | Timeline";
            parentFolder.guideObject.transformTarget.name = parentFolder.name;
            foreach (ObjectCtrlInfo child in childFolders)
                Studio.Studio.Instance.treeNodeCtrl.SetParent(child.treeNodeObject, parentFolder.treeNodeObject);
            parentFolder.guideObject.changeAmount.Reset();
            UpdateInterpolablesView();
        }
        #endregion
    }
}
