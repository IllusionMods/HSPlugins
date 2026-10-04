using System.Linq;
using System.Collections.Generic;
using Timeline.Graph;
using Timeline.Nla;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// The verbs Blender's graph editor has that were missing here: framing, selecting by column,
    /// snapping keyframes onto something, and flattening what a keyframe does to its neighbours.
    ///
    /// None of them are new capabilities, every one can be done by hand with enough dragging. What they
    /// are is the difference between an editor you can work in and one you can only tinker with: "put
    /// these forty keyframes on whole frames" should be one click, not forty drags.
    /// </summary>
    public partial class Timeline
    {
        /// <summary>What snapping lands a time on.</summary>
        internal enum GraphSnap
        {
            /// <summary>The block and division lines, which is what shift has always snapped to.</summary>
            Grid,
            /// <summary>Whole frames at the framerate in the Timing menu.</summary>
            Frame,
            /// <summary>Whole seconds.</summary>
            Second,
            /// <summary>Markers only, and nothing at all when none is near.</summary>
            Marker
        }

        /// <summary>Tracks kept out of the graph, and tracks drawn but not draggable. Neither affects playback.</summary>
        private readonly HashSet<Interpolable> _graphHiddenTracks = new HashSet<Interpolable>();
        private readonly HashSet<Interpolable> _graphLockedTracks = new HashSet<Interpolable>();

        #region Snapping

        private float SnapTimeTo(float time, GraphSnap target)
        {
            float marker;
            if (TryNearestMarker(time, out marker))
                return marker;

            switch (target)
            {
                case GraphSnap.Marker:
                    return time;
                case GraphSnap.Frame:
                    int rate = Mathf.Max(_desiredFrameRate, 1);
                    return Mathf.Round(time * rate) / rate;
                case GraphSnap.Second:
                    return Mathf.Round(time);
                default:
                    float beat = _blockLength / Mathf.Max(_divisions, 1);
                    return Mathf.Round(time / beat) * beat;
            }
        }

        /// <summary>
        /// Puts every selected keyframe on the nearest whatever, all at once.
        ///
        /// Keyframes are moved from the latest to the earliest when they are going forwards and the
        /// other way when they are going back, so one never lands on a slot another has not vacated yet.
        /// </summary>
        private void SnapSelectedKeyframes(GraphSnap target, bool toCursor)
        {
            if (_selectedKeyframes.Count == 0)
            {
                Logger.LogMessage("Select some keyframes first.");
                return;
            }

            RecordUndo("Snap keyframes");
            var ordered = new List<KeyValuePair<float, Keyframe>>(_selectedKeyframes);
            ordered.Sort((a, b) => b.Key.CompareTo(a.Key)); // latest first

            int moved = 0;
            int blocked = 0;
            var done = new HashSet<Keyframe>();
            // Two passes in opposite orders: whichever direction a keyframe is travelling, one of them
            // reaches it after the slot it wants has been freed. The set is what stops the second pass
            // moving something the first one already placed - the times in this list are the ones the
            // keyframes had before any of this, so they no longer say where anything is.
            for (int pass = 0; pass < 2; ++pass)
            {
                foreach (KeyValuePair<float, Keyframe> pair in ordered)
                {
                    if (done.Contains(pair.Value))
                        continue;
                    float destination = toCursor ? _playbackTime : SnapTimeTo(pair.Key, target);
                    destination = Mathf.Clamp(destination, 0f, _duration);
                    if (Mathf.Abs(destination - pair.Key) < 0.0001f)
                    {
                        done.Add(pair.Value); // already where it wants to be
                        continue;
                    }
                    if (TryMoveKeyframe(pair.Value, destination))
                    {
                        done.Add(pair.Value);
                        ++moved;
                    }
                    else if (pass == 1)
                    {
                        ++blocked;
                    }
                }
                ordered.Reverse();
            }

            UpdateGrid();
            RefreshInterpolation();
            Logger.LogMessage($"Moved {moved} keyframe(s)" +
                              (blocked == 0 ? "." : $", {blocked} had nowhere to go: another keyframe was already there."));
        }

        /// <summary>
        /// Spreads the selected keyframes out, or draws them together, around the first of them: a factor
        /// of 2 doubles every gap. Timeline 1 did this with Alt and the wheel. All or nothing: false, and
        /// nothing moves, when a keyframe would land on one that is not moving or is on a locked track.
        /// </summary>
        private bool ScaleSelectedSpacing(float factor, bool recordUndo)
        {
            if (_selectedKeyframes.Count < 2 || factor <= 0f)
                return false;
            float min = _selectedKeyframes.Min(k => k.Key);
            var moves = new Dictionary<Keyframe, float>();
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                moves[pair.Value] = min + (pair.Key - min) * factor;
            foreach (KeyValuePair<Keyframe, float> move in moves)
            {
                if (_graphLockedTracks.Contains(move.Key.parent))
                    return false;
                Keyframe occupant = FindOccupant(move.Key.parent, move.Value, move.Key);
                if (occupant != null && moves.ContainsKey(occupant) == false)
                    return false;
            }
            // Two keys of one track drawn so close they would be one.
            foreach (IGrouping<Interpolable, KeyValuePair<Keyframe, float>> track in moves.GroupBy(m => m.Key.parent))
            {
                List<float> times = track.Select(m => m.Value).OrderBy(t => t).ToList();
                for (int i = 1; i < times.Count; ++i)
                {
                    if (Mathf.Approximately(times[i - 1], times[i]))
                        return false;
                }
            }

            if (recordUndo)
                RecordUndo("Scale key spacing");
            // Spreading moves the furthest first and drawing together the nearest first, so no key
            // lands on a selected one that has not moved yet.
            var ordered = _selectedKeyframes.Select(p => p.Value).ToList();
            ordered.Sort((a, b) => factor > 1f ? moves[b].CompareTo(moves[a]) : moves[a].CompareTo(moves[b]));
            foreach (Keyframe k in ordered)
                TryMoveKeyframe(k, moves[k], false);
            for (int i = 0; i < _selectedKeyframes.Count; i++)
            {
                Keyframe k = _selectedKeyframes[i].Value;
                _selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(moves[k], k);
            }
            UpdateGrid();
            UpdateKeyframeWindow(false);
            RefreshInterpolation();
            return true;
        }

        /// <summary>
        /// Takes the slope out of every selected keyframe, both sides of it.
        ///
        /// A keyframe's easing lives on the segment that leaves it, so flattening one means zeroing the
        /// tangent leaving it and the tangent arriving from the keyframe before. Doing only one of the
        /// two is what produces the corner nobody can find afterwards.
        /// </summary>
        private void FlattenSelectedHandles()
        {
            if (_selectedKeyframes.Count == 0)
            {
                Logger.LogMessage("Select some keyframes first.");
                return;
            }

            RecordUndo("Flatten handles");
            int flattened = 0;
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                Keyframe keyframe = pair.Value;
                SortedList<float, Keyframe> keyframes = keyframe.parent.keyframes;
                int index = keyframes.IndexOfValue(keyframe);
                if (index < 0)
                    continue;

                if (keyframe.handles != null)
                {
                    // Free on both sides, keeping whatever reach they had and losing the rise. Going
                    // through Materialise first is what keeps the reach: an automatic handle has no
                    // stored length until it is asked to remember one.
                    MaterialiseHandle(keyframes, index, true, HandleType.Free);
                    MaterialiseHandle(keyframes, index, false, HandleType.Free);
                    for (int c = 0; c < keyframe.handles.left.Length; ++c)
                    {
                        keyframe.handles.left[c] = new Vector2(keyframe.handles.left[c].x, 0f);
                        keyframe.handles.right[c] = new Vector2(keyframe.handles.right[c].x, 0f);
                    }
                    HandleMath.SyncEasingAt(keyframes, index - 1);
                    HandleMath.SyncEasingAt(keyframes, index);
                }
                else
                {
                    // A step, or a curve shaped by hand: there the easing curve is still in charge.
                    Flatten(keyframe, true);
                    if (index > 0)
                        Flatten(keyframes.Values[index - 1], false);
                }
                ++flattened;
            }

            UpdateGrid();
            RefreshInterpolation();
            Logger.LogMessage($"Flattened {flattened} keyframe(s).");
        }

        /// <summary>
        /// Blender's Smooth Keys: pulls each selected keyframe a little towards the average of its two
        /// neighbours, which takes the tremble out of a recorded take without throwing keyframes away.
        ///
        /// Every new value is worked out from the old ones before any of them is written. Smoothing in
        /// place would feed each keyframe the already smoothed one before it, which drags the whole
        /// track towards its own start.
        /// </summary>
        private void SmoothSelectedKeyframes()
        {
            if (_selectedKeyframes.Count == 0)
            {
                Logger.LogMessage("Select some keyframes first.");
                return;
            }

            RebuildSelectedKeyframeSet();
            var pending = new List<KeyValuePair<Keyframe, object>>();
            var tracks = new HashSet<Interpolable>();
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                tracks.Add(pair.Value.parent);

            foreach (Interpolable track in tracks)
            {
                IList<Keyframe> frames = track.keyframes.Values;
                for (int i = 1; i + 1 < frames.Count; ++i)
                {
                    Keyframe keyframe = frames[i];
                    if (_selectedKeyframeSet.Contains(keyframe) == false)
                        continue;
                    object previous = frames[i - 1].value;
                    object next = frames[i + 1].value;
                    if (ValueBlend.IsBlendable(keyframe.value) == false ||
                        previous == null || next == null ||
                        previous.GetType() != keyframe.value.GetType() || next.GetType() != keyframe.value.GetType())
                        continue;

                    // The average of the neighbours, then half way from here to it: a full move to the
                    // average erases the shape in one pass instead of easing it.
                    object average = ValueBlend.Lerp(previous, next, 0.5f);
                    pending.Add(new KeyValuePair<Keyframe, object>(keyframe, ValueBlend.Lerp(keyframe.value, average, 0.5f)));
                }
            }

            if (pending.Count == 0)
            {
                Logger.LogMessage("Nothing to smooth: a keyframe needs one on each side of it, and a value that can be averaged.");
                return;
            }

            RecordUndo("Smooth keyframes");
            foreach (KeyValuePair<Keyframe, object> pair in pending)
                pair.Key.value = pair.Value;

            UpdateGrid();
            RefreshInterpolation();
            Logger.LogMessage($"Smoothed {pending.Count} keyframe(s). Run it again to smooth further.");
        }

        private static void Flatten(Keyframe keyframe, bool outgoing)
        {
            if (keyframe.curve == null || keyframe.curve.length < 2)
                return;
            UnityEngine.Keyframe[] keys = keyframe.curve.keys;
            if (outgoing)
                keys[0].outTangent = 0f;
            else
                keys[keys.Length - 1].inTangent = 0f;
            keyframe.curve = new AnimationCurve(keys);
        }
        #endregion

        #region Selecting

        /// <summary>
        /// Replaces the selection outright.
        ///
        /// The ordinary path adds keyframes one at a time and searches the selection for each one to see
        /// whether it is already in there, which is fine for the handful a click produces and quadratic
        /// for the twelve thousand a baked rig has. These are lists built from the tracks themselves, so
        /// there is nothing to check for: a keyframe cannot appear twice.
        /// </summary>
        /// <summary>The tracks Select All and the like work on: the selected ones, or else every one the window lists.</summary>
        private List<Interpolable> GraphScopeTracks()
        {
            if (_selectedInterpolables.Count != 0)
                return new List<Interpolable>(_selectedInterpolables);
            return _interpolables.Values.Where(t => t.oci == null || t.oci == _selectedOCI).ToList();
        }

        private void SelectKeyframesOutright(List<KeyValuePair<float, Keyframe>> keyframes)
        {
            _selectedKeyframes.Clear();
            _selectedKeyframes.AddRange(keyframes);
            RebuildSelectedKeyframeSet();
        }

        private void SelectAllKeyframesInScope()
        {
            var all = new List<KeyValuePair<float, Keyframe>>();
            foreach (Interpolable track in GraphScopeTracks())
            {
                foreach (KeyValuePair<float, Keyframe> pair in track.keyframes)
                    all.Add(pair);
            }
            SelectKeyframesOutright(all);
            AfterSelectionChanged($"Selected {all.Count} keyframe(s).");
        }

        private void AfterSelectionChanged(string message)
        {
            UpdateKeyframeWindow(false);
            Logger.LogMessage(message);
        }
        #endregion

        #region Framing
        #endregion
    }
}
