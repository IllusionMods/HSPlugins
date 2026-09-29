using System.Collections.Generic;
using System.Linq;
using Studio;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// ◆ Key and its ▾: key the nodes selected in the scene, all of them or only part of each, making
    /// their tracks as needed. Several nodes can be selected at once (control + click), each keyed on
    /// its own object. One axis goes on a track of its own, so it can have its own timing.
    /// </summary>
    public partial class Timeline
    {
        internal static readonly string[] _nodePropIds = { "Pos", "Rot", "Scale" };
        internal static readonly string[] _nodePropNames = { "Position", "Rotation", "Scale" };

        /// <summary>The node selected in the scene, the first when there are several.</summary>
        internal GuideObject SelectedNode()
        {
            List<GuideObject> nodes = SelectedNodes();
            return nodes.Count == 0 ? null : nodes[0];
        }

        /// <summary>Every node selected in the scene that belongs to an object Timeline can key.</summary>
        internal List<GuideObject> SelectedNodes()
        {
            var nodes = new List<GuideObject>();
            GuideObjectManager guides = GuideObjectManager.Instance;
            if (guides == null || guides.selectObjects == null)
                return nodes;
            foreach (GuideObject node in guides.selectObjects)
            {
                if (node != null && node.transformTarget != null && NodeOwner(node) != null && nodes.Contains(node) == false)
                    nodes.Add(node);
            }
            return nodes;
        }

        /// <summary>The object a node belongs to: its own, or the one its parent node is.</summary>
        internal static ObjectCtrlInfo NodeOwner(GuideObject node)
        {
            for (GuideObject g = node; g != null; g = g.parentGuide)
            {
                GuideObject captured = g;
                ObjectCtrlInfo oci = Studio.Studio.Instance.dicObjectCtrl.Values.FirstOrDefault(o => o.guideObject == captured);
                if (oci != null)
                    return oci;
            }
            return null;
        }

        /// <summary>Which of position, rotation and scale a node lets you change, as Studio's gizmo does.</summary>
        internal static bool NodeHas(GuideObject node, int prop)
        {
            switch (prop)
            {
                case 0: return node.enablePos;
                case 1: return node.enableRot;
                default: return node.enableScale;
            }
        }

        /// <summary>
        /// Keys the selected nodes at the playhead. prop is 0 position, 1 rotation, 2 scale, or -1 for
        /// everything each node has; axis is 0 to 2 for one axis on its own track, or -1 for all three.
        /// </summary>
        internal void KeyNode(int prop, int axis)
        {
            List<GuideObject> nodes = SelectedNodes();
            if (nodes.Count == 0)
            {
                Logger.LogMessage("Select a node in the scene first: one of the circles or squares on the character or item. Control + click selects several.");
                return;
            }
            RecordUndo("Key node");
            var tracks = new List<Interpolable>();
            foreach (GuideObject node in nodes)
            {
                ObjectCtrlInfo oci = NodeOwner(node);
                string owner = _ownerId, prefix = "guideObject";
                object parameter = node;
                // The pose editor's bone node keys the pose editor's tracks for that bone.
                if (IsPoseEditorNode(node))
                {
                    if (PoseEditorBone(oci, out owner, out parameter) == false)
                        continue;
                    prefix = "bone";
                }
                for (int p = 0; p < 3; ++p)
                {
                    if (prop >= 0 && p != prop || NodeHas(node, p) == false)
                        continue;
                    string combinedId = prefix + _nodePropIds[p];
                    if (axis >= 0)
                        AddNodeTrack(tracks, oci, parameter, owner, combinedId + "XYZ"[axis]);
                    else if (HasAnySplit(parameter, owner, combinedId))
                    {
                        // Already split: all three axes, each on its own track.
                        for (int a = 0; a < 3; ++a)
                            AddNodeTrack(tracks, oci, parameter, owner, combinedId + "XYZ"[a]);
                    }
                    else
                        AddNodeTrack(tracks, oci, parameter, owner, combinedId);
                }
            }
            if (tracks.Count == 0)
            {
                Logger.LogMessage("The selected node" + (nodes.Count > 1 ? "s have" : " has") + " nothing of that kind to key.");
                return;
            }
            foreach (Interpolable track in tracks)
                KeyAt(track, _playbackTime);
            _selectedInterpolables.Clear();
            _selectedInterpolables.AddRange(tracks);
            UpdateInterpolablesView();
            UpdateGrid();
        }

        /// <summary>
        /// The node's track for that id: the one it already has, a new one, or, for one axis while the
        /// three are still one track, that track split so the axis has its own.
        /// </summary>
        private void AddNodeTrack(List<Interpolable> tracks, ObjectCtrlInfo oci, object parameter, string owner, string id)
        {
            InterpolableModel model = GetModel(owner, id);
            if (model == null)
                return;
            var track = new Interpolable(oci, parameter, model);
            Interpolable existing;
            if (_interpolables.TryGetValue(track.GetHashCode(), out existing) == false)
            {
                Interpolable combined = FindCombinedFor(track);
                if (combined != null)
                {
                    SplitTransformInterpolable(combined);
                    _interpolables.TryGetValue(track.GetHashCode(), out existing);
                }
                else
                {
                    _interpolables.Add(track.GetHashCode(), track);
                    _interpolablesTree.AddLeaf(track);
                    existing = track;
                }
            }
            if (existing != null && tracks.Contains(existing) == false)
                tracks.Add(existing);
        }

        /// <summary>A key with the current value at that time: a new one, or the one already there updated.</summary>
        private void KeyAt(Interpolable track, float time)
        {
            Keyframe existing = FindOccupant(track, time, null);
            if (existing == null)
            {
                AddKeyframe(track, time);
                return;
            }
            RecordUndo("Key");
            existing.value = track.GetValue();
        }
    }
}
