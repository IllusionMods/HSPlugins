using System;
using System.Collections.Generic;
using System.Linq;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// ShalltyUtils' track tools, from ShalltyUtils by ShalltyB (github.com/ShalltyB/ShalltyUtils):
    /// throwing away keys that repeat what is already there, deleting the whole timeline, and turning a
    /// per axis track into a single curve.
    /// </summary>
    public partial class Timeline
    {
        /// <summary>
        /// Across every track: keys at the same time as another, and keys in the middle of a run of equal
        /// values. The ends of each run stay, so nothing moves differently.
        /// </summary>
        internal void CleanupRedundantKeys()
        {
            if (_interpolables.Count == 0)
                return;
            RecordUndo("Clean up keys");
            _selectedKeyframes.Clear();
            int removed = 0;
            foreach (Interpolable track in _interpolables.Values)
            {
                // Keys at the same time, to the microsecond.
                var seen = new HashSet<float>();
                for (int i = track.keyframes.Count - 1; i >= 0; --i)
                {
                    if (seen.Add((float)Math.Round(track.keyframes.Keys[i], 6)) == false)
                    {
                        track.keyframes.RemoveAt(i);
                        ++removed;
                    }
                }
                if (track.keyframes.Count < 3)
                    continue;

                // Keys in the middle of a run of equal values.
                List<KeyValuePair<float, Keyframe>> keys = track.keyframes.ToList();
                var toDelete = new List<KeyValuePair<float, Keyframe>>();
                int inSequence = 0;
                for (int i = 1; i < keys.Count; i++)
                {
                    if (Equals(keys[i - 1].Value.value, keys[i].Value.value))
                    {
                        if (++inSequence > 1)
                            toDelete.Add(keys[i - 1]);
                    }
                    else
                        inSequence = 0;
                }
                removed += toDelete.Count;
                foreach (KeyValuePair<float, Keyframe> pair in toDelete)
                    track.keyframes.Remove(pair.Key);
            }
            UpdateGrid();
            RefreshInterpolation();
            Logger.LogMessage(removed + " redundant key(s) removed.");
        }

        /// <summary>Every track, key, group, key set and picker page of the scene, and its timing back to the defaults.</summary>
        internal void DeleteAllTimelineData()
        {
            RecordUndo("Delete the timeline");
            Stop();
            _interpolables.Clear();
            _interpolablesTree.Clear();
            _orphanTracks.Clear();
            _keySets.Clear();
            _pickerPages.Clear();
            _pickerPage = 0;
            ClearTrimRange();
            ClearStrips();
            _selectedInterpolables.Clear();
            _selectedKeyframes.Clear();
            _duration = 10f;
            _blockLength = 10f;
            _divisions = 10;
            _desiredFrameRate = 60;
            UpdateInterpolablesView();
            UpdateKeyframeWindow();
            Logger.LogMessage("All Timeline data cleared from scene.");
        }

        private static readonly HashSet<string> _curveTrackIds = new HashSet<string>
        {
            "guideObjectPosX", "guideObjectPosY", "guideObjectPosZ", "guideObjectRotX", "guideObjectRotY", "guideObjectRotZ",
            "guideObjectXPos", "guideObjectYPos", "guideObjectZPos", "guideObjectXRot", "guideObjectYRot", "guideObjectZRot"
        };

        internal static bool CanConvertToCurve(Interpolable track)
        {
            return _curveTrackIds.Contains(track.id) && track.keyframes.Count >= 2;
        }

        /// <summary>
        /// A per axis track's keys become one curve from its first key to its last: two keys, 0 and 1, the
        /// first holding the values as its curve, straight between each. The shape is then edited as one
        /// curve in the graph instead of key by key.
        /// </summary>
        internal void ConvertTracksToCurves(IEnumerable<Interpolable> tracks)
        {
            List<Interpolable> list = tracks.Where(CanConvertToCurve).ToList();
            if (list.Count == 0)
            {
                Logger.LogMessage("First select at least one (GO POS/ROT XYZ) Interpolable!");
                return;
            }
            RecordUndo("Convert to curve");
            var stairs = new AnimationCurve(new UnityEngine.Keyframe(0f, 0f, 0f, 0f), new UnityEngine.Keyframe(1f, 1f, float.PositiveInfinity, 0f));
            foreach (Interpolable track in list)
            {
                float startTime = track.keyframes.Keys.First();
                float endTime = track.keyframes.Keys.Last();
                var points = new List<UnityEngine.Keyframe>();
                foreach (KeyValuePair<float, Keyframe> pair in track.keyframes)
                    points.Add(new UnityEngine.Keyframe(Mathf.InverseLerp(startTime, endTime, pair.Key), (float)pair.Value.value));
                var curve = new AnimationCurve(points.ToArray());
                SetCurveLinear(curve);
                for (int i = 0; i < curve.length; i++)
                    curve.SmoothTangents(i, 0f);
                _selectedKeyframes.RemoveAll(p => p.Value.parent == track);
                track.keyframes.Clear();
                track.keyframes.Add(startTime, new Keyframe(0f, track, curve));
                track.keyframes.Add(endTime, new Keyframe(1f, track, new AnimationCurve(stairs.keys)));
            }
            UpdateGrid();
            RefreshInterpolation();
        }

        /// <summary>Tangents that go straight to the next point and come straight from the previous one.</summary>
        private static void SetCurveLinear(AnimationCurve curve)
        {
            UnityEngine.Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (i > 0)
                    keys[i].inTangent = (float)(((double)keys[i].value - keys[i - 1].value) / ((double)keys[i].time - keys[i - 1].time));
                if (i < keys.Length - 1)
                    keys[i].outTangent = (float)(((double)keys[i + 1].value - keys[i].value) / ((double)keys[i + 1].time - keys[i].time));
            }
            curve.keys = keys;
        }

        #region Expand all
        /// <summary>A row above the workspace tree that opens or closes every folder in it.</summary>
        private void CreateTreeStateButton()
        {
            TreeNodeCtrl tree = Studio.Studio.Instance.treeNodeCtrl;
            GameObject row = Instantiate(tree.m_ObjectNode);
            row.SetActive(true);
            row.transform.SetParent(Singleton<WorkspaceCtrl>.Instance.buttonFolder.transform.parent, false);
            row.transform.localPosition = new Vector3(-350f, -30f, 0f);
            TreeNodeObject node = row.GetComponent<TreeNodeObject>();
            if (node == null)
                return;
            node.textName = "Expand All";
            node.treeState = TreeNodeObject.TreeState.Close;
            node.enableVisible = false;
            node.visible = true;
            node.baseColor = new Color(0f, 0f, 0f, 0f);
            node.colorSelect = node.baseColor;
            StartCoroutine(ShowStateNextFrame(node));
            node.imageState.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            Action apply = () =>
            {
                node.textName = node.treeState != TreeNodeObject.TreeState.Open ? "Expand All" : "Collapse All";
                foreach (TreeNodeObject top in tree.m_TreeNodeObject)
                    SetTreeStateRecursive(top, node.treeState);
            };
            node.m_ButtonSelect.gameObject.SetActive(true);
            node.m_ButtonSelect.onClick.ActuallyRemoveAllListeners();
            node.m_ButtonSelect.onClick.AddListener(() =>
            {
                node.treeState = node.treeState == TreeNodeObject.TreeState.Open ? TreeNodeObject.TreeState.Close : TreeNodeObject.TreeState.Open;
                apply();
            });
            node.m_ButtonState.onClick.AddListener(() => apply());
        }

        private static System.Collections.IEnumerator ShowStateNextFrame(TreeNodeObject node)
        {
            yield return null;
            node.SetStateVisible(true);
        }

        private static void SetTreeStateRecursive(TreeNodeObject node, TreeNodeObject.TreeState state)
        {
            node.SetTreeState(state);
            foreach (TreeNodeObject child in node.child)
                SetTreeStateRecursive(child, state);
        }
        #endregion
    }
}
