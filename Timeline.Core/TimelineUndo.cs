using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// Undo history for keyframe and track edits.
    ///
    /// This snapshots the whole set of tracks rather than recording per operation commands. Timeline
    /// mutates its data from roughly twenty places (dragging, pasting, scaling a selection, splitting a
    /// track, deleting an interpolable, editing a curve) and a command per operation means every one of
    /// them has to describe its own inverse correctly. With snapshots, a site that forgets to record
    /// simply is not undoable, which is a far gentler failure than an inverse that restores the wrong
    /// thing. Keyframes are small, so the memory cost of a bounded stack is not worth optimising away.
    /// </summary>
    public partial class Timeline
    {
        private const int _maxUndoSteps = 32;

        private sealed class TrackState
        {
            public Interpolable interpolable;
            public GroupNode<InterpolableGroup> parent;
            public bool enabled;
            public bool smooth;
            public TrackExtrapolation extrapolation;
            public Color color;
            public string alias;
            public float[] times;
            public object[] values;
            public AnimationCurve[] curves;
            public KeyframeKind[] kinds;
            public KeyframeHandles[] handles;
            public bool[] shapedByCurve;
            public KeySet[] sets;
        }

        /// <summary>
        /// A strip and the clip behind it. The clip's contents are captured too, because tweak mode and
        /// merging rewrite them; the object references are kept so identity survives a restore and two
        /// strips sharing a clip still share it afterwards.
        /// </summary>
        private sealed class StripState
        {
            public Nla.MotionStrip strip;
            public string clipName;
            public float clipLength;
            public float start;
            public float scale;
            public float influence;
            public float blendIn;
            public float blendOut;
            public int repeat;
            public int lane;
            public bool enabled;
            public Nla.StripExtrapolation extrapolation;
            public Nla.StripBlendMode blendMode;
            public bool reverse;
            public float clipStart;
            public float clipEnd;
            public Studio.ObjectCtrlInfo owner;
            public KeyValuePair<float, float>[] influenceKeys;
            public List<ChannelState> channels;
        }

        private sealed class ChannelState
        {
            public Nla.ClipChannel channel;
            public float[] times;
            /// <summary>
            /// Whole copies, so handles, types and everything else a keyframe carries come back with it.
            /// Listing fields one at a time is how the handles were left out of this the first time.
            /// </summary>
            public Keyframe[] copies;
        }

        private sealed class HistoryState
        {
            public List<TrackState> tracks;
            public List<StripState> strips;
            /// <summary>The strip being edited then, and the tracks' own keys it had put aside.</summary>
            public Nla.MotionStrip tweak;
            public List<KeyValuePair<Interpolable, KeyValuePair<float, Keyframe>[]>> tweakStash;
            /// <summary>Copies, not the markers themselves: they are edited in place.</summary>
            public List<TimelineMarker> markers;
            /// <summary>The sets themselves, so identity survives, with what they were called then.</summary>
            public List<KeySet> keySets;
            public string[] keySetNames;
            public Color[] keySetColors;
            public string label;
        }

        private readonly List<HistoryState> _undoStack = new List<HistoryState>();
        private readonly List<HistoryState> _redoStack = new List<HistoryState>();
        /// <summary>Guards against a restore recording itself.</summary>
        private bool _restoringHistory;
        private int _lastRecordedFrame = -1;

        /// <summary>
        /// Captures the current state so the next edit can be undone. Call before mutating, not after.
        /// </summary>
        private void RecordUndo(string label)
        {
            if (_restoringHistory)
                return;
            // One entry per user action. Operations nest freely here, deleting the last keyframe of a
            // track also removes the track, and adding keyframes to a multi selection loops. Coalescing
            // by frame means each of those is a single step rather than several.
            if (_lastRecordedFrame == Time.frameCount)
                return;
            _lastRecordedFrame = Time.frameCount;

            _undoStack.Add(Capture(label));
            if (_undoStack.Count > _maxUndoSteps)
                _undoStack.RemoveAt(0);
            _redoStack.Clear();
        }

        private void Undo()
        {
            if (_undoStack.Count == 0)
                return;

            HistoryState state = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(Capture(state.label));
            Restore(state);
        }

        private void Redo()
        {
            if (_redoStack.Count == 0)
                return;

            HistoryState state = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(Capture(state.label));
            Restore(state);
        }

        private void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private HistoryState Capture(string label)
        {
            var tracks = new List<TrackState>(_interpolables.Count);
            foreach (Interpolable interpolable in _interpolables.Values)
            {
                int count = interpolable.keyframes.Count;
                var track = new TrackState
                {
                    interpolable = interpolable,
                    parent = _interpolablesTree.GetLeafNode(interpolable)?.parent as GroupNode<InterpolableGroup>,
                    enabled = interpolable.enabled,
                    smooth = interpolable.smooth,
                    extrapolation = interpolable.extrapolation,
                    color = interpolable.color,
                    alias = interpolable.alias,
                    times = new float[count],
                    values = new object[count],
                    curves = new AnimationCurve[count],
                    kinds = new KeyframeKind[count],
                    handles = new KeyframeHandles[count],
                    shapedByCurve = new bool[count],
                    sets = new KeySet[count]
                };

                int i = 0;
                foreach (KeyValuePair<float, Keyframe> pair in interpolable.keyframes)
                {
                    track.times[i] = pair.Key;
                    // Values are immutable structs or strings once boxed, so the reference is a safe copy.
                    // Curves are not, they get edited in place, so those need a real copy.
                    track.values[i] = pair.Value.value;
                    track.curves[i] = new AnimationCurve(pair.Value.curve.keys);
                    track.kinds[i] = pair.Value.kind;
                    // Handles are edited in place too, so a reference would follow the edit it is
                    // supposed to be the memory of.
                    track.handles[i] = pair.Value.handles == null ? null : pair.Value.handles.Clone();
                    track.shapedByCurve[i] = pair.Value.shapedByCurve;
                    track.sets[i] = pair.Value.keySet;
                    ++i;
                }
                tracks.Add(track);
            }

            var markers = new List<TimelineMarker>(_markers.Count);
            foreach (TimelineMarker marker in _markers)
                markers.Add(new TimelineMarker { time = marker.time, name = marker.name });

            return new HistoryState
            {
                tracks = tracks, strips = CaptureStrips(), markers = markers, label = label,
                tweak = _tweakStrip,
                tweakStash = _tweakStash.Select(p => new KeyValuePair<Interpolable, KeyValuePair<float, Keyframe>[]>(p.Key,
                        p.Value.Select(k => new KeyValuePair<float, Keyframe>(k.Key, new Keyframe(k.Value))).ToArray())).ToList(),
                keySets = new List<KeySet>(_keySets),
                keySetNames = _keySets.ConvertAll(s => s.name).ToArray(),
                keySetColors = _keySets.ConvertAll(s => s.color).ToArray()
            };
        }

        private List<StripState> CaptureStrips()
        {
            var states = new List<StripState>(_strips.Count);
            foreach (Nla.MotionStrip strip in _strips)
            {
                var state = new StripState
                {
                    strip = strip,
                    clipName = strip.clip.name,
                    clipLength = strip.clip.length,
                    start = strip.start,
                    scale = strip.scale,
                    influence = strip.influence,
                    blendIn = strip.blendIn,
                    blendOut = strip.blendOut,
                    repeat = strip.repeat,
                    lane = strip.lane,
                    enabled = strip.enabled,
                    extrapolation = strip.extrapolation,
                    blendMode = strip.blendMode,
                    reverse = strip.reverse,
                    clipStart = strip.clipStart,
                    clipEnd = strip.clipEnd,
                    owner = strip.owner,
                    influenceKeys = strip.influenceKeys.ToArray(),
                    channels = new List<ChannelState>(strip.clip.channels.Count)
                };

                foreach (Nla.ClipChannel channel in strip.clip.channels)
                {
                    int count = channel.keyframes.Count;
                    var channelState = new ChannelState
                    {
                        channel = channel,
                        times = new float[count],
                        copies = new Keyframe[count]
                    };
                    int i = 0;
                    foreach (KeyValuePair<float, Keyframe> pair in channel.keyframes)
                    {
                        channelState.times[i] = pair.Key;
                        channelState.copies[i] = new Keyframe(pair.Value);
                        ++i;
                    }
                    state.channels.Add(channelState);
                }
                states.Add(state);
            }
            return states;
        }

        private void RestoreStrips(List<StripState> states)
        {
            if (states == null)
                return;

            _selectedStrips.Clear();
            _tweakStrip = null;
            _strips.Clear();

            foreach (StripState state in states)
            {
                Nla.MotionStrip strip = state.strip;
                strip.clip.name = state.clipName;
                strip.clip.length = state.clipLength;
                strip.start = state.start;
                strip.scale = state.scale;
                strip.influence = state.influence;
                strip.blendIn = state.blendIn;
                strip.blendOut = state.blendOut;
                strip.repeat = state.repeat;
                strip.lane = state.lane;
                strip.enabled = state.enabled;
                strip.extrapolation = state.extrapolation;
                strip.blendMode = state.blendMode;
                // Splitting trims the original, so without these an undone split left it cut in half.
                strip.reverse = state.reverse;
                strip.clipStart = state.clipStart;
                strip.clipEnd = state.clipEnd;
                strip.owner = state.owner;
                strip.influenceKeys.Clear();
                if (state.influenceKeys != null)
                {
                    foreach (KeyValuePair<float, float> key in state.influenceKeys)
                        strip.influenceKeys[key.Key] = key.Value;
                }
                AddToLibrary(strip.clip);

                foreach (ChannelState channelState in state.channels)
                {
                    channelState.channel.keyframes.Clear();
                    for (int i = 0; i < channelState.times.Length; ++i)
                        // A copy of the copy: the same snapshot can be restored more than once by undo
                        // and redo, so it must never be handed out to be edited.
                        channelState.channel.keyframes.Add(channelState.times[i],
                                new Keyframe(channelState.copies[i], channelState.channel.target));
                }
                _strips.Add(strip);
            }

        }

        private void Restore(HistoryState state)
        {
            _restoringHistory = true;
            try
            {
                // Selections point at Keyframe objects that are about to be replaced.
                _selectedKeyframes.Clear();
                _selectedInterpolables.Clear();

                // A group may have been deleted since the snapshot; re-parenting to it would strand the
                // track outside the tree, so anything dangling goes back at the root.
                var liveGroups = new HashSet<IGroupNode>();
                _interpolablesTree.Recurse((node, depth) =>
                {
                    if (node.type == INodeType.Group)
                        liveGroups.Add((IGroupNode)node);
                });

                foreach (Interpolable interpolable in new List<Interpolable>(_interpolables.Values))
                    _interpolablesTree.RemoveLeaf(interpolable);
                _interpolables.Clear();

                foreach (TrackState track in state.tracks)
                {
                    Interpolable interpolable = track.interpolable;
                    interpolable.enabled = track.enabled;
                    interpolable.smooth = track.smooth;
                    interpolable.extrapolation = track.extrapolation;
                    interpolable.color = track.color;
                    interpolable.alias = track.alias;

                    interpolable.keyframes.Clear();
                    for (int i = 0; i < track.times.Length; ++i)
                    {
                        interpolable.keyframes.Add(track.times[i],
                                new Keyframe(track.values[i], interpolable, new AnimationCurve(track.curves[i].keys))
                                {
                                    kind = track.kinds == null ? KeyframeKind.Keyframe : track.kinds[i],
                                    handles = track.handles == null || track.handles[i] == null
                                            ? null : track.handles[i].Clone(),
                                    shapedByCurve = track.shapedByCurve != null && track.shapedByCurve[i],
                                    keySet = track.sets == null ? null : track.sets[i]
                                });
                    }

                    int hash = interpolable.GetHashCode();
                    if (_interpolables.ContainsKey(hash))
                        continue;
                    _interpolables.Add(hash, interpolable);
                    _interpolablesTree.AddLeaf(interpolable, track.parent != null && liveGroups.Contains(track.parent) ? track.parent : null);
                }

                RestoreStrips(state.strips);
                // Tweak mode as it was: the strip then being edited, and the tracks' own keys put aside.
                _tweakStrip = state.tweak != null && _strips.Contains(state.tweak) ? state.tweak : null;
                _tweakStash.Clear();
                if (_tweakStrip != null && state.tweakStash != null)
                {
                    foreach (KeyValuePair<Interpolable, KeyValuePair<float, Keyframe>[]> pair in state.tweakStash)
                        _tweakStash[pair.Key] = pair.Value.Select(k => new KeyValuePair<float, Keyframe>(k.Key, new Keyframe(k.Value))).ToList();
                }

                if (state.keySets != null)
                {
                    _keySets.Clear();
                    for (int i = 0; i < state.keySets.Count; ++i)
                    {
                        state.keySets[i].name = state.keySetNames[i];
                        state.keySets[i].color = state.keySetColors[i];
                        _keySets.Add(state.keySets[i]);
                    }
                }

                _markers.Clear();
                if (state.markers != null)
                {
                    foreach (TimelineMarker marker in state.markers)
                        _markers.Add(new TimelineMarker { time = marker.time, name = marker.name });
                }

                UpdateInterpolablesView();
                UpdateGrid();
                RefreshInterpolation();
            }
            finally
            {
                _restoringHistory = false;
            }
        }
    }
}
