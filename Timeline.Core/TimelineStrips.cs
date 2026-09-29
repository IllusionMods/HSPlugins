using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Studio;
using Timeline.Nla;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// Non linear animation, as Blender's NLA: reusable clips (its actions) placed on the timeline as
    /// strips, in lanes (its NLA tracks) that each object has its own stack of.
    ///
    /// A strip drives the same interpolables a track would, so nothing about how values reach the game
    /// changes. What changes is where the keyframes come from: instead of the track's own list, the
    /// surrounding pair is taken from the clip at a time the strip remaps. That is what makes a two
    /// second loop placeable five times, at different speeds, without copying keyframes.
    ///
    /// Each object's lanes are evaluated from the lowest up, then the keys still on its tracks - the
    /// action - lie on top of them all, Replace at full influence unless set otherwise. So a movement
    /// pushed down into a strip can be corrected by keying on top of it, as in Blender.
    ///
    /// The whole feature is inert while there are no strips, so scenes that never use it behave exactly
    /// as before.
    /// </summary>
    public partial class Timeline
    {
        private readonly List<MotionStrip> _strips = new List<MotionStrip>();
        /// <summary>Every clip there is. A clip stays when its last strip goes, as Blender keeps an action.</summary>
        private readonly List<MotionClip> _clips = new List<MotionClip>();
        /// <summary>Lane settings and action blending per object, made when first asked for.</summary>
        private readonly List<NlaStack> _nlaStacks = new List<NlaStack>();
        /// <summary>Rebuilt every evaluation: the interpolables a strip speaks for this frame.</summary>
        private readonly Dictionary<Interpolable, StripSample> _stripSamples = new Dictionary<Interpolable, StripSample>();

        #region Stacks
        /// <summary>The object's stack, made when first asked for. A null owner is the scene's.</summary>
        internal NlaStack StackOf(ObjectCtrlInfo owner)
        {
            NlaStack stack = FindStack(owner);
            if (stack == null)
            {
                stack = new NlaStack { owner = owner };
                _nlaStacks.Add(stack);
            }
            return stack;
        }

        private NlaStack FindStack(ObjectCtrlInfo owner)
        {
            foreach (NlaStack stack in _nlaStacks)
            {
                if (stack.owner == owner)
                    return stack;
            }
            return null;
        }

        /// <summary>The object a clip animates, or null when it animates several or none.</summary>
        internal static ObjectCtrlInfo OwnerOf(MotionClip clip)
        {
            ObjectCtrlInfo owner = null;
            bool first = true;
            foreach (ClipChannel channel in clip.channels)
            {
                if (channel.target == null)
                    continue;
                if (first)
                {
                    owner = channel.target.oci;
                    first = false;
                }
                else if (channel.target.oci != owner)
                    return null;
            }
            return owner;
        }

        /// <summary>The highest lane the object uses, -1 when it has no strip.</summary>
        internal int TopLane(ObjectCtrlInfo owner)
        {
            int top = -1;
            foreach (MotionStrip strip in _strips)
            {
                if (strip.owner == owner && strip.lane > top)
                    top = strip.lane;
            }
            return top;
        }

        internal bool LaneLocked(MotionStrip strip)
        {
            NlaStack stack = FindStack(strip.owner);
            if (stack == null)
                return false;
            NlaLane lane = stack.lanes.Find(l => l.index == strip.lane);
            return lane != null && lane.locked;
        }

        /// <summary>How many strips play a clip.</summary>
        internal int UsersOf(MotionClip clip)
        {
            int users = 0;
            foreach (MotionStrip strip in _strips)
            {
                if (strip.clip == clip)
                    ++users;
            }
            return users;
        }

        private string UniqueClipName(string name)
        {
            if (_clips.All(c => c.name != name))
                return name;
            for (int i = 1; ; ++i)
            {
                string candidate = name + "." + i.ToString("000");
                if (_clips.All(c => c.name != candidate))
                    return candidate;
            }
        }

        private void AddToLibrary(MotionClip clip)
        {
            if (clip != null && _clips.Contains(clip) == false)
                _clips.Add(clip);
        }
        #endregion

        #region Evaluation
        /// <summary>
        /// Works out what each object's stack has to say at the current time: lanes from the lowest up,
        /// then the action on top.
        /// </summary>
        private void SampleStrips()
        {
            _stripSamples.Clear();
            if (_strips.Count == 0)
                return;

            _blended.Clear();
            RebuildStripOrder();
            MotionStrip tweak = _tweakStrip;
            int lane = int.MinValue;
            ObjectCtrlInfo laneOwner = null;
            foreach (MotionStrip strip in _stripsByLane)
            {
                // Only the first strip in a lane may hold its first frame back over the time before it,
                // which is Blender's rule. A later one doing it covers every strip ahead of it in the
                // lane, and since the lane is processed in time order the later one would always win.
                bool firstInLane = strip.lane != lane || strip.owner != laneOwner;
                lane = strip.lane;
                laneOwner = strip.owner;
                if (strip == tweak)
                    continue;
                // While a strip is edited the lanes above it are off, as Blender turns off the upper stack.
                if (tweak != null && strip.owner == tweak.owner && strip.lane > tweak.lane)
                    continue;
                NlaStack stack = FindStack(strip.owner);
                if (stack != null && stack.Plays(strip.lane) == false)
                    continue;

                float weight = strip.WeightAt(_playbackTime, firstInLane);
                float local;
                if (weight <= 0.001f || strip.TryLocalTime(_playbackTime, firstInLane, out local) == false)
                    continue;

                foreach (ClipChannel channel in strip.clip.channels)
                {
                    if (channel.target == null || channel.target.enabled == false)
                        continue;
                    StripSample sample;
                    if (MotionStrip.TrySample(channel.keyframes, local, out sample) == false)
                        continue;

                    Contribution existing;
                    bool first = _blended.TryGetValue(channel.target, out existing) == false;

                    // A lone strip at full influence hands the delegate the untouched keyframe pair, which
                    // is what keeps discrete interpolables working: there is no halfway pose to compute.
                    if (first && weight >= 0.999f && strip.blendMode == StripBlendMode.Replace)
                    {
                        _blended[channel.target] = new Contribution { sample = sample, weight = weight, plain = true };
                        continue;
                    }

                    object value = ValueBlend.IsBlendable(sample.left)
                            ? ValueBlend.Lerp(sample.left, sample.right, sample.factor)
                            : null;

                    if (value == null)
                    {
                        // Discrete: the loudest contribution wins outright.
                        if (first || weight > existing.weight)
                            _blended[channel.target] = new Contribution { sample = sample, weight = weight, plain = true };
                        continue;
                    }

                    object under = first
                            ? RestValueOf(channel.target)
                            : existing.plain
                                    ? ValueBlend.Lerp(existing.sample.left, existing.sample.right, existing.sample.factor)
                                    : existing.value;

                    if (under == null)
                    {
                        // Nothing to blend against, which happens when the target cannot report its
                        // current value. Blending here would hand the delegate a null and it would throw
                        // on every frame, so the strip speaks on its own instead.
                        if (first || weight > existing.weight)
                            _blended[channel.target] = new Contribution { sample = sample, weight = weight, plain = true };
                        continue;
                    }

                    object reference = ReferenceOf(channel);
                    _blended[channel.target] = new Contribution
                    {
                        value = ValueBlend.Blend(under, value, reference, weight, strip.blendMode),
                        weight = Mathf.Max(weight, first ? 0f : existing.weight),
                        plain = false
                    };
                }
            }

            BlendActions();

            foreach (KeyValuePair<Interpolable, Contribution> pair in _blended)
            {
                Contribution contribution = pair.Value;
                if (contribution.plain)
                    _stripSamples[pair.Key] = contribution.sample;
                else
                    _stripSamples[pair.Key] = new StripSample { left = contribution.value, right = contribution.value, factor = 0f };
            }
            RefreshRestValues();
        }

        private readonly List<Interpolable> _actionTargets = new List<Interpolable>();

        /// <summary>
        /// The keys still on a track, the object's action, go on top of what its lanes made. Replace at
        /// full influence, the default, leaves the track's own keys to play as they are.
        /// </summary>
        private void BlendActions()
        {
            _actionTargets.Clear();
            foreach (KeyValuePair<Interpolable, Contribution> pair in _blended)
            {
                if (pair.Key.keyframes.Count != 0)
                    _actionTargets.Add(pair.Key);
            }
            foreach (Interpolable target in _actionTargets)
            {
                StripBlendMode mode;
                float influence;
                ActionBlendFor(target.oci, out mode, out influence);
                if (influence <= 0.001f)
                    continue;
                if (mode == StripBlendMode.Replace && influence >= 0.999f)
                {
                    _blended.Remove(target);
                    continue;
                }
                StripSample own;
                if (MotionStrip.TrySample(target.keyframes, _playbackTime, out own) == false)
                    continue;
                Contribution below = _blended[target];
                object under = below.plain ? ValueBlend.Lerp(below.sample.left, below.sample.right, below.sample.factor) : below.value;
                object value = ValueBlend.IsBlendable(own.left) ? ValueBlend.Lerp(own.left, own.right, own.factor) : null;
                if (value == null || under == null)
                {
                    // A switch has no halfway: the action has it from half its influence up.
                    if (influence >= 0.5f)
                        _blended.Remove(target);
                    continue;
                }
                _blended[target] = new Contribution
                {
                    value = ValueBlend.Blend(under, value, target.keyframes.Values[0].value, influence, mode),
                    weight = 1f,
                    plain = false
                };
            }
        }

        /// <summary>How an object's action blends: its stack's setting, or while a strip of it is edited, that strip's.</summary>
        private void ActionBlendFor(ObjectCtrlInfo owner, out StripBlendMode mode, out float influence)
        {
            MotionStrip tweak = _tweakStrip;
            if (tweak != null && tweak.owner == owner)
            {
                mode = tweak.blendMode;
                influence = tweak.WeightAt(_playbackTime, true);
                return;
            }
            NlaStack stack = FindStack(owner);
            mode = stack == null ? StripBlendMode.Replace : stack.actionBlend;
            influence = stack == null ? 1f : stack.actionInfluence;
        }

        /// <summary>
        /// What a channel rests at under all the lanes: the value it had the last time nothing drove it.
        /// Read once and kept, since reading it while a strip drives it would read the strip back and
        /// every fade would feed on itself.
        /// </summary>
        private readonly Dictionary<Interpolable, object> _restValues = new Dictionary<Interpolable, object>();
        private readonly List<Interpolable> _restRefresh = new List<Interpolable>();

        private object RestValueOf(Interpolable interpolable)
        {
            object value;
            if (_restValues.TryGetValue(interpolable, out value))
                return value;
            try
            {
                value = interpolable.GetValue();
            }
            catch (Exception)
            {
                value = null;
            }
            _restValues[interpolable] = value;
            return value;
        }

        /// <summary>A channel nothing drives this frame rests where it is now, posed by hand or not.</summary>
        private void RefreshRestValues()
        {
            _restRefresh.Clear();
            foreach (Interpolable interpolable in _restValues.Keys)
            {
                if (_blended.ContainsKey(interpolable) == false && interpolable.keyframes.Count == 0)
                    _restRefresh.Add(interpolable);
            }
            foreach (Interpolable interpolable in _restRefresh)
            {
                try
                {
                    _restValues[interpolable] = interpolable.GetValue();
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>The channel's value at its own first frame, which Add measures its movement from.</summary>
        private static object ReferenceOf(ClipChannel channel)
        {
            return channel.keyframes.Count == 0 ? null : channel.keyframes.Values[0].value;
        }

        private struct Contribution
        {
            public StripSample sample;
            public object value;
            public float weight;
            /// <summary>The keyframe pair is passed through untouched rather than a computed value.</summary>
            public bool plain;
        }

        private readonly Dictionary<Interpolable, Contribution> _blended = new Dictionary<Interpolable, Contribution>();

        /// <summary>
        /// Strips ordered so each object's lower lanes are mixed before the ones stacked on top of them.
        ///
        /// Kept as a list that is re-sorted every evaluation without allocating: Clear, AddRange and Sort
        /// reuse it once it has grown, where a LINQ OrderBy allocates a sorted buffer every time.
        /// </summary>
        private readonly List<MotionStrip> _stripsByLane = new List<MotionStrip>();

        /// <summary>
        /// Owner, then lane, then time. Lane alone left the order within a lane to an unstable sort, so
        /// which of two strips on the same lane counted as "later" - and therefore won - was down to chance.
        /// </summary>
        private static readonly Comparison<MotionStrip> _byLane = (a, b) =>
        {
            int byOwner = (a.owner == null ? 0 : a.owner.GetHashCode()).CompareTo(b.owner == null ? 0 : b.owner.GetHashCode());
            if (byOwner != 0)
                return byOwner;
            int byLane = a.lane.CompareTo(b.lane);
            return byLane != 0 ? byLane : a.start.CompareTo(b.start);
        };

        private void RebuildStripOrder()
        {
            _stripsByLane.Clear();
            _stripsByLane.AddRange(_strips);
            _stripsByLane.Sort(_byLane);
        }
        #endregion

        #region Authoring
        /// <summary>
        /// Moves the keyframes of the given tracks into a new clip per object, each dropped as a strip on
        /// a new lane at the top of that object's stack, as Blender's Push Down does.
        ///
        /// The keyframes leave the tracks, which is the point: they would otherwise be the action on top
        /// of the strip and hide it. The tracks themselves stay behind as empty channels so the strip has
        /// something to bind to and so the rows remain visible in the list.
        /// </summary>
        private void PushDownToStrip(List<Interpolable> tracks)
        {
            var byTrack = new Dictionary<Interpolable, List<KeyValuePair<float, Keyframe>>>();
            foreach (Interpolable track in tracks)
            {
                if (track.keyframes.Count != 0 && byTrack.ContainsKey(track) == false)
                    byTrack.Add(track, track.keyframes.ToList());
            }
            PushDown(byTrack);
        }

        /// <summary>
        /// The same thing for a part of a track rather than all of it: whatever keyframes are selected
        /// become the clip, and only those leave the tracks.
        ///
        /// Blender has no equivalent, its Push Down takes the whole action and you trim the strip
        /// afterwards. Selecting the keyframes first says the same thing in fewer steps.
        /// </summary>
        private void PushDownSelectionToStrip()
        {
            if (_selectedKeyframes.Count < 2)
            {
                Logger.LogMessage("Select at least two keyframes first: a strip needs something to play.");
                return;
            }
            var byTrack = new Dictionary<Interpolable, List<KeyValuePair<float, Keyframe>>>();
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                List<KeyValuePair<float, Keyframe>> keyframes;
                if (byTrack.TryGetValue(pair.Value.parent, out keyframes) == false)
                {
                    keyframes = new List<KeyValuePair<float, Keyframe>>();
                    byTrack.Add(pair.Value.parent, keyframes);
                }
                keyframes.Add(pair);
            }
            PushDown(byTrack);
            SelectKeyframes(new List<KeyValuePair<float, Keyframe>>());
        }

        private void PushDown(Dictionary<Interpolable, List<KeyValuePair<float, Keyframe>>> byTrack)
        {
            if (byTrack.Count == 0)
                return;
            var groups = byTrack.GroupBy(p => p.Key.oci).ToList();
            var ranges = groups.Select(g => new
            {
                group = g,
                from = g.Min(p => p.Value.Min(k => k.Key)),
                to = g.Max(p => p.Value.Max(k => k.Key))
            }).Where(r => r.to > r.from).ToList();
            if (ranges.Count == 0)
            {
                Logger.LogMessage("Those keyframes are all at the same time, there is no span to make a strip from.");
                return;
            }

            RecordUndo("Push down to strip");
            var toRemove = new List<KeyValuePair<float, Keyframe>>();
            MotionStrip last = null;
            foreach (var range in ranges)
            {
                ObjectCtrlInfo owner = range.group.Key;
                List<KeyValuePair<Interpolable, List<KeyValuePair<float, Keyframe>>>> tracks = range.group.ToList();
                var clip = new MotionClip
                {
                    name = UniqueClipName(tracks.Count == 1 ? DisplayName(tracks[0].Key) : (owner == null ? "Scene" : owner.treeNodeObject.textName) + " action"),
                    length = range.to - range.from
                };
                foreach (KeyValuePair<Interpolable, List<KeyValuePair<float, Keyframe>>> pair in tracks)
                {
                    var channel = new ClipChannel(pair.Key);
                    foreach (KeyValuePair<float, Keyframe> keyframe in pair.Value)
                    {
                        channel.keyframes[keyframe.Key - range.from] = new Keyframe(keyframe.Value, pair.Key);
                        toRemove.Add(keyframe);
                    }
                    clip.channels.Add(channel);
                }
                _clips.Add(clip);
                last = new MotionStrip { clip = clip, owner = owner, start = range.from, lane = TopLane(owner) + 1 };
                _strips.Add(last);
            }

            // The tracks have to survive as strip targets, so they are emptied rather than deleted.
            DeleteKeyframes(toRemove, false);
            if (last != null)
                SelectStrip(last, false);
            UpdateGrid();
        }

        /// <summary>
        /// A strip of a clip already there, at a time, Blender's Add Action Strip: on the lane asked for
        /// if it has room, otherwise on the first lane of its object that has.
        /// </summary>
        internal void AddClipStrip(MotionClip clip, float time, int lane = -1)
        {
            if (clip == null || clip.channels.Count == 0)
                return;
            RecordUndo("Add strip");
            ObjectCtrlInfo owner = OwnerOf(clip);
            var strip = new MotionStrip { clip = clip, owner = owner, start = Mathf.Max(0f, time), lane = lane };
            if (lane < 0 || OverlapsInLane(strip, lane))
            {
                for (int l = 0; l <= TopLane(owner) + 1; ++l)
                {
                    strip.lane = l;
                    if (OverlapsInLane(strip, l) == false)
                        break;
                }
            }
            _strips.Add(strip);
            SelectStrip(strip, false);
            UpdateGrid();
        }

        /// <summary>
        /// Cuts a strip in two at a time, Blender's Y. Both halves keep playing exactly what they played
        /// before, because the cut is expressed as a clip range rather than by copying keyframes.
        /// </summary>
        private void SplitStrip(MotionStrip strip, float time)
        {
            if (strip.clip == null || time <= strip.start + 0.01f || time >= strip.end - 0.01f)
            {
                Logger.LogMessage("Put the cursor inside the strip first.");
                return;
            }
            if (strip.repeat > 1)
            {
                Logger.LogMessage("A repeating strip cannot be split. Set its repeat back to 1, or merge it first.");
                return;
            }

            float local;
            if (strip.TryLocalTime(time, out local) == false)
                return;

            RecordUndo("Split strip");
            float from = strip.effectiveClipStart;
            float to = strip.effectiveClipEnd;

            MotionStrip second = CopyOf(strip, strip.clip);
            second.start = time;
            second.blendIn = 0f;
            // Played backwards, the later half of the strip is the earlier part of the clip.
            second.clipStart = strip.reverse ? from : local;
            second.clipEnd = strip.reverse ? local : to;
            // The keyed influence goes on playing at the same times.
            second.influenceKeys.Clear();
            float shift = time - strip.start;
            foreach (KeyValuePair<float, float> key in strip.influenceKeys)
            {
                if (key.Key >= shift)
                    second.influenceKeys[key.Key - shift] = key.Value;
            }

            if (strip.reverse)
                strip.clipStart = local;
            else
                strip.clipEnd = local;
            strip.blendOut = 0f; // that fade belonged to the end of the whole strip, which is now the other one

            _strips.Add(second);
            SelectStrip(second, false);
            UpdateGrid();
        }

        private static string DisplayName(Interpolable interpolable)
        {
            return string.IsNullOrEmpty(interpolable.alias) ? View.TrackName(interpolable) : interpolable.alias;
        }

        /// <summary>A strip with every setting of another, playing the given clip.</summary>
        private static MotionStrip CopyOf(MotionStrip strip, MotionClip clip)
        {
            var copy = new MotionStrip
            {
                clip = clip,
                owner = strip.owner,
                start = strip.start,
                scale = strip.scale,
                repeat = strip.repeat,
                enabled = strip.enabled,
                reverse = strip.reverse,
                extrapolation = strip.extrapolation,
                lane = strip.lane,
                influence = strip.influence,
                blendIn = strip.blendIn,
                blendOut = strip.blendOut,
                blendMode = strip.blendMode,
                clipStart = strip.clipStart,
                clipEnd = strip.clipEnd
            };
            foreach (KeyValuePair<float, float> key in strip.influenceKeys)
                copy.influenceKeys.Add(key.Key, key.Value);
            return copy;
        }

        /// <summary>
        /// A second placement straight after this one, playing exactly what it plays: with a clip of its
        /// own (Blender's Duplicate), or sharing this one's, so editing either edits both (its Linked
        /// Duplicate).
        /// </summary>
        private void DuplicateStrip(MotionStrip strip, bool linked)
        {
            RecordUndo(linked ? "Duplicate strip, linked" : "Duplicate strip");
            MotionClip clip = strip.clip;
            if (linked == false)
            {
                clip = strip.clip.Copy(UniqueClipName(strip.clip.name));
                _clips.Add(clip);
            }
            MotionStrip copy = CopyOf(strip, clip);
            copy.start = strip.end;

            // Straight after the original on its own lane if there is room, otherwise the first lane up
            // that has it. Dropping it onto a strip already there would leave two strips overlapping,
            // which the lane rules exist to prevent.
            for (int lane = strip.lane; lane <= TopLane(strip.owner) + 1; ++lane)
            {
                copy.lane = lane;
                if (OverlapsInLane(copy, lane) == false)
                    break;
            }

            _strips.Add(copy);
            SelectStrip(copy, false);
            UpdateGrid();
        }

        /// <summary>Gives a strip a copy of its clip, so editing it no longer edits the others.</summary>
        internal void MakeSingleUser(MotionStrip strip)
        {
            if (UsersOf(strip.clip) < 2)
                return;
            RecordUndo("Make single user");
            strip.clip = strip.clip.Copy(UniqueClipName(strip.clip.name));
            _clips.Add(strip.clip);
            UpdateGrid();
        }

        internal void RenameClip(MotionClip clip, string name)
        {
            name = name == null ? "" : name.Trim();
            if (name.Length == 0 || name == clip.name)
                return;
            RecordUndo("Rename clip");
            clip.name = UniqueClipName(name);
            UpdateGrid();
        }

        /// <summary>A clip no strip plays any more goes from the library.</summary>
        internal void DeleteClip(MotionClip clip)
        {
            if (UsersOf(clip) != 0)
                return;
            _clips.Remove(clip);
            UpdateGrid();
        }

        /// <summary>Keys the strip's influence at a time; keys are kept from the strip's start, so they move with it.</summary>
        internal void KeyInfluence(MotionStrip strip, float time, float value)
        {
            RecordUndo("Key influence");
            strip.influenceKeys[Mathf.Clamp(time - strip.start, 0f, strip.length)] = Mathf.Clamp01(value);
            UpdateGrid();
        }

        /// <summary>
        /// The clip's range back to its whole length, and its length back to where its keys end, as
        /// Blender's Sync Length.
        /// </summary>
        internal void SyncClipLength(MotionStrip strip)
        {
            RecordUndo("Sync length");
            float last = 0f;
            foreach (ClipChannel channel in strip.clip.channels)
            {
                if (channel.keyframes.Count != 0)
                    last = Mathf.Max(last, channel.keyframes.Keys[channel.keyframes.Count - 1]);
            }
            if (last > 0f)
                strip.clip.length = last;
            strip.clipStart = -1f;
            strip.clipEnd = -1f;
            UpdateGrid();
        }

        /// <summary>The strip currently opened for editing, whose clip is laid out on the tracks.</summary>
        private MotionStrip _tweakStrip;
        /// <summary>The tracks' own keys, put aside while a strip is edited on them.</summary>
        private readonly Dictionary<Interpolable, List<KeyValuePair<float, Keyframe>>> _tweakStash = new Dictionary<Interpolable, List<KeyValuePair<float, Keyframe>>>();

        /// <summary>
        /// Opens a strip for editing, Blender's tweak mode: the tracks' own keys are put aside, the clip is
        /// laid out on them where the strip plays it (trimmed and at its speed), and the strip goes quiet
        /// meanwhile, as do the lanes above it. The edited keys then play as the strip did, with its
        /// blending and influence.
        ///
        /// A reversed strip is laid out forwards: turned over, every key's curve would lead the wrong way.
        /// </summary>
        private void EnterTweakMode(MotionStrip strip)
        {
            if (_tweakStrip != null)
                ExitTweakMode();
            if (strip.clip == null)
                return;

            RecordUndo("Edit strip");
            _tweakStrip = strip;
            _tweakStash.Clear();
            _selectedKeyframes.Clear();
            float from = strip.effectiveClipStart, scale = Mathf.Max(strip.scale, 0.01f);
            foreach (ClipChannel channel in strip.clip.channels)
            {
                Interpolable target = channel.target;
                if (target == null || _tweakStash.ContainsKey(target))
                    continue;
                _tweakStash[target] = target.keyframes.ToList();
                target.keyframes.Clear();
                foreach (KeyValuePair<float, Keyframe> pair in channel.keyframes)
                {
                    float time = strip.start + (pair.Key - from) * scale;
                    if (target.keyframes.ContainsKey(time) == false)
                        target.keyframes.Add(time, new Keyframe(pair.Value, target));
                }
            }

            UpdateInterpolablesView();
            UpdateGrid();
        }

        /// <summary>
        /// Folds the edited keyframes back into the clip and gives the tracks their own keys back. The
        /// clip's length is re-derived from what is there now, so adding keyframes past the end grows it.
        /// </summary>
        private void ExitTweakMode()
        {
            MotionStrip strip = _tweakStrip;
            if (strip == null || strip.clip == null)
            {
                _tweakStrip = null;
                return;
            }

            RecordUndo("Finish editing strip");
            _tweakStrip = null;
            _selectedKeyframes.Clear();

            float from = strip.effectiveClipStart, scale = Mathf.Max(strip.scale, 0.01f);
            float length = 0f;
            foreach (ClipChannel channel in strip.clip.channels)
            {
                Interpolable target = channel.target;
                if (target == null)
                    continue;
                channel.keyframes.Clear();
                foreach (KeyValuePair<float, Keyframe> pair in target.keyframes)
                {
                    float local = Mathf.Max(0f, from + (pair.Key - strip.start) / scale);
                    channel.keyframes[local] = new Keyframe(pair.Value, target);
                    length = Mathf.Max(length, local);
                }
            }
            foreach (KeyValuePair<Interpolable, List<KeyValuePair<float, Keyframe>>> pair in _tweakStash)
            {
                pair.Key.keyframes.Clear();
                foreach (KeyValuePair<float, Keyframe> own in pair.Value)
                {
                    if (pair.Key.keyframes.ContainsKey(own.Key) == false)
                        pair.Key.keyframes.Add(own.Key, own.Value);
                }
            }
            _tweakStash.Clear();
            if (length > 0f)
                strip.clip.length = length;

            UpdateInterpolablesView();
            UpdateGrid();
        }

        /// <summary>
        /// Folds several strips of one object into one clip. Each contributing keyframe is written at the
        /// timeline time it actually plays, repeats expanded and speed applied, so back to back strips
        /// join seamlessly.
        ///
        /// Where strips overlap on the same channel the upper lane wins at any shared time. Influence and
        /// the fades are not baked: those are a blend between two sources, and once the sources have
        /// become one clip there is nothing left to blend against.
        /// </summary>
        private void MergeStrips(List<MotionStrip> strips)
        {
            List<MotionStrip> usable = strips.Where(s => s != null && s.clip != null).ToList();
            if (usable.Count < 2)
                return;
            if (usable.Any(s => s.owner != usable[0].owner))
            {
                Logger.LogMessage("Only strips of the same object can be merged: each object has a stack of its own.");
                return;
            }

            RecordUndo("Merge strips");

            float start = usable.Min(s => s.start);
            float end = usable.Max(s => s.end);
            var clip = new MotionClip { name = UniqueClipName("Merged"), length = Mathf.Max(end - start, 0.01f) };

            foreach (MotionStrip strip in usable.OrderBy(s => s.lane).ThenBy(s => s.start))
            {
                foreach (ClipChannel source in strip.clip.channels)
                {
                    if (source.target == null)
                        continue;
                    ClipChannel target = clip.channels.FirstOrDefault(c => c.target == source.target);
                    if (target == null)
                    {
                        target = new ClipChannel(source.target);
                        clip.channels.Add(target);
                    }

                    // Inverts the strip's own time mapping, so trimming and reverse come out right
                    // instead of the merged clip silently ignoring them.
                    float from = strip.effectiveClipStart;
                    float span = strip.clipSpan;
                    for (int pass = 0; pass < Mathf.Max(strip.repeat, 1); ++pass)
                    {
                        float passStart = strip.start + pass * strip.singleLength - start;
                        foreach (KeyValuePair<float, Keyframe> pair in source.keyframes)
                        {
                            if (pair.Key < from - 0.0001f || pair.Key > from + span + 0.0001f)
                                continue; // trimmed away

                            float offset = strip.reverse ? from + span - pair.Key : pair.Key - from;
                            float time = passStart + offset * strip.scale;
                            if (time < -0.0001f || time > clip.length + 0.0001f)
                                continue;
                            time = Mathf.Clamp(time, 0f, clip.length);
                            // Indexer rather than Add: a later lane overwrites an earlier one at the
                            // same time instead of throwing on the duplicate key.
                            target.keyframes[time] = new Keyframe(pair.Value, source.target);
                        }
                    }
                }
            }

            int lane = usable.Min(s => s.lane);
            foreach (MotionStrip strip in usable)
                _strips.Remove(strip);

            _clips.Add(clip);
            var merged = new MotionStrip { clip = clip, owner = usable[0].owner, start = start, lane = lane };
            _strips.Add(merged);
            SelectStrip(merged, false);
            UpdateGrid();
        }

        private void RemoveStrip(MotionStrip strip)
        {
            if (_tweakStrip == strip)
                ExitTweakMode();
            RecordUndo("Delete strip");
            _strips.Remove(strip);
            _selectedStrips.Remove(strip);
            if (_selectedStrips.Count == 0)
                CloseStripWindow();
            UpdateGrid();
        }

        /// <summary>Removes a lane of an object and its strips; the lanes above it move down one.</summary>
        internal void DeleteLane(ObjectCtrlInfo owner, int index)
        {
            if (_tweakStrip != null && _tweakStrip.owner == owner && _tweakStrip.lane == index)
                ExitTweakMode();
            RecordUndo("Delete lane");
            foreach (MotionStrip strip in _strips.Where(s => s.owner == owner && s.lane == index).ToList())
            {
                _strips.Remove(strip);
                _selectedStrips.Remove(strip);
            }
            foreach (MotionStrip strip in _strips)
            {
                if (strip.owner == owner && strip.lane > index)
                    --strip.lane;
            }
            NlaStack stack = FindStack(owner);
            if (stack != null)
            {
                stack.lanes.RemoveAll(l => l.index == index);
                foreach (NlaLane lane in stack.lanes)
                {
                    if (lane.index > index)
                        --lane.index;
                }
            }
            UpdateGrid();
        }
        #endregion

        #region Display

        /// <summary>
        /// Where a dragged strip lands. Two strips overlapping in one lane both speak at once and fight
        /// over the same value, so a strip dropped on top of another looks for a free lane of its object,
        /// a new one on top if need be.
        /// </summary>
        private void SettleStrip(MotionStrip strip, float previousStart)
        {
            if (OverlapsInLane(strip, strip.lane) == false)
                return;

            for (int lane = 0; lane <= TopLane(strip.owner) + 1; ++lane)
            {
                if (OverlapsInLane(strip, lane))
                    continue;
                strip.lane = lane;
                Logger.LogMessage("Strip moved to lane " + (lane + 1) + ", the one you dropped it on was taken.");
                return;
            }

            strip.start = previousStart;
            Logger.LogMessage("No lane free for that: the strip is back where it was.");
        }

        private bool OverlapsInLane(MotionStrip strip, int lane)
        {
            foreach (MotionStrip other in _strips)
            {
                if (other == strip || other.owner != strip.owner || other.lane != lane || other.clip == null)
                    continue;
                if (strip.start < other.end - 0.001f && other.start < strip.end - 0.001f)
                    return true;
            }
            return false;
        }

        #endregion

        #region Inspector
        private readonly List<MotionStrip> _selectedStrips = new List<MotionStrip>();

        private void SelectStrip(MotionStrip strip, bool additive)
        {
            if (additive == false)
                _selectedStrips.Clear();
            else
                _selectedStrips.Remove(strip);
            _selectedStrips.Add(strip);

            // A strip has no row of its own in the channel list, so the next best thing is to light up
            // the tracks it drives: you can see at a glance what it covers, and the graph editor, which
            // draws the selected tracks, turns into that strip's curves.
            if (additive == false && strip.clip != null)
            {
                _selectedInterpolables.Clear();
                foreach (ClipChannel channel in strip.clip.channels)
                {
                    if (channel.target != null)
                        _selectedInterpolables.Add(channel.target);
                }
            }
            UpdateStrips();
        }

        private void CloseStripWindow()
        {
            _selectedStrips.Clear();
            UpdateStrips();
        }

        /// <summary>Everything NLA of the scene, before another is loaded or the timeline is emptied.</summary>
        private void ClearStrips()
        {
            _tweakStrip = null;
            _tweakStash.Clear();
            _strips.Clear();
            _clips.Clear();
            _nlaStacks.Clear();
            _restValues.Clear();
            CloseStripWindow();
        }
        #endregion

        #region Scene data
        /// <summary>
        /// Clips once each, strips pointing at them, then the lanes' and actions' settings. A clip written
        /// inside every strip that played it came back as that many separate clips.
        /// </summary>
        private void WriteStrips(XmlTextWriter writer, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            var ids = new Dictionary<MotionClip, int>();
            foreach (MotionClip clip in _clips.Concat(_strips.Select(s => s.clip)).Distinct())
            {
                if (clip == null || clip.channels.Count == 0 || ids.ContainsKey(clip))
                    continue;
                int id = ids.Count;
                writer.WriteStartElement("nlaClip");
                writer.WriteAttributeString("id", XmlConvert.ToString(id));
                writer.WriteAttributeString("name", clip.name);
                writer.WriteAttributeString("length", XmlConvert.ToString(clip.length));
                // While a strip is edited its clip's keys are on the tracks: those are what it holds now.
                WriteClipChannels(writer, clip, dic, clip == (_tweakStrip == null ? null : _tweakStrip.clip) ? _tweakStrip : null);
                writer.WriteEndElement();
                ids.Add(clip, id);
            }

            foreach (MotionStrip strip in _strips)
            {
                int clipId;
                if (strip.clip == null || ids.TryGetValue(strip.clip, out clipId) == false)
                    continue;
                writer.WriteStartElement("strip");
                writer.WriteAttributeString("clip", XmlConvert.ToString(clipId));
                writer.WriteAttributeString("owner", XmlConvert.ToString(strip.owner == null ? -1 : dic.FindIndex(e => e.Value == strip.owner)));
                writer.WriteAttributeString("start", XmlConvert.ToString(strip.start));
                writer.WriteAttributeString("scale", XmlConvert.ToString(strip.scale));
                writer.WriteAttributeString("repeat", XmlConvert.ToString(strip.repeat));
                writer.WriteAttributeString("enabled", XmlConvert.ToString(strip.enabled));
                writer.WriteAttributeString("extrapolation", strip.extrapolation.ToString());
                writer.WriteAttributeString("lane", XmlConvert.ToString(strip.lane));
                writer.WriteAttributeString("influence", XmlConvert.ToString(strip.influence));
                writer.WriteAttributeString("blendIn", XmlConvert.ToString(strip.blendIn));
                writer.WriteAttributeString("blendOut", XmlConvert.ToString(strip.blendOut));
                writer.WriteAttributeString("blendMode", strip.blendMode.ToString());
                writer.WriteAttributeString("reverse", XmlConvert.ToString(strip.reverse));
                writer.WriteAttributeString("clipStart", XmlConvert.ToString(strip.clipStart));
                writer.WriteAttributeString("clipEnd", XmlConvert.ToString(strip.clipEnd));
                foreach (KeyValuePair<float, float> key in strip.influenceKeys)
                {
                    writer.WriteStartElement("influenceKey");
                    writer.WriteAttributeString("time", XmlConvert.ToString(key.Key));
                    writer.WriteAttributeString("value", XmlConvert.ToString(key.Value));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            foreach (NlaStack stack in _nlaStacks)
            {
                if (stack.IsDefault)
                    continue;
                int owner = stack.owner == null ? -1 : dic.FindIndex(e => e.Value == stack.owner);
                if (stack.owner != null && owner == -1)
                    continue;
                writer.WriteStartElement("nlaStack");
                writer.WriteAttributeString("owner", XmlConvert.ToString(owner));
                writer.WriteAttributeString("actionBlend", stack.actionBlend.ToString());
                writer.WriteAttributeString("actionInfluence", XmlConvert.ToString(stack.actionInfluence));
                foreach (NlaLane lane in stack.lanes)
                {
                    writer.WriteStartElement("lane");
                    writer.WriteAttributeString("index", XmlConvert.ToString(lane.index));
                    writer.WriteAttributeString("name", lane.name);
                    writer.WriteAttributeString("mute", XmlConvert.ToString(lane.mute));
                    writer.WriteAttributeString("solo", XmlConvert.ToString(lane.solo));
                    writer.WriteAttributeString("lock", XmlConvert.ToString(lane.locked));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        /// <summary>A clip's channels; with the strip being edited, its keys as they are on the tracks, mapped back.</summary>
        private static void WriteClipChannels(XmlTextWriter writer, MotionClip clip, List<KeyValuePair<int, ObjectCtrlInfo>> dic, MotionStrip editing)
        {
            foreach (ClipChannel channel in clip.channels)
            {
                if (channel.target == null)
                    continue;
                int objectIndex = -1;
                if (channel.target.oci != null)
                {
                    objectIndex = dic.FindIndex(e => e.Value == channel.target.oci);
                    if (objectIndex == -1)
                        continue;
                }

                // Same shape as a normal interpolable element, so the existing rebinding attributes
                // (guide object path, bone path) come along for free.
                writer.WriteStartElement("interpolable");
                writer.WriteAttributeString("owner", channel.target.owner);
                if (objectIndex != -1)
                    writer.WriteAttributeString("objectIndex", XmlConvert.ToString(objectIndex));
                writer.WriteAttributeString("id", channel.target.id);
                if (channel.target.writeParameterToXml != null)
                    channel.target.writeParameterToXml(channel.target.oci, writer, channel.target.parameter);

                IEnumerable<KeyValuePair<float, Keyframe>> keys = channel.keyframes;
                if (editing != null)
                {
                    float from = editing.effectiveClipStart, scale = Mathf.Max(editing.scale, 0.01f);
                    keys = channel.target.keyframes.Select(p => new KeyValuePair<float, Keyframe>(Mathf.Max(0f, from + (p.Key - editing.start) / scale), p.Value));
                }
                foreach (KeyValuePair<float, Keyframe> pair in keys)
                {
                    writer.WriteStartElement("keyframe");
                    writer.WriteAttributeString("time", XmlConvert.ToString(pair.Key));
                    channel.target.WriteValueToXml(writer, pair.Value.value);
                    foreach (UnityEngine.Keyframe curveKey in pair.Value.curve.keys)
                    {
                        writer.WriteStartElement("curveKeyframe");
                        writer.WriteAttributeString("time", XmlConvert.ToString(curveKey.time));
                        writer.WriteAttributeString("value", XmlConvert.ToString(curveKey.value));
                        writer.WriteAttributeString("inTangent", XmlConvert.ToString(curveKey.inTangent));
                        writer.WriteAttributeString("outTangent", XmlConvert.ToString(curveKey.outTangent));
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        private MotionClip ReadClip(XmlNode node, string name, float length, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            var clip = new MotionClip { name = name, length = length };
            foreach (XmlNode channelNode in node.ChildNodes)
            {
                if (channelNode.Name != "interpolable")
                    continue;
                Interpolable target = ResolveInterpolable(channelNode, dic);
                if (target == null)
                    continue;

                var channel = new ClipChannel(target);
                foreach (XmlNode keyframeNode in channelNode.ChildNodes)
                {
                    if (keyframeNode.Name != "keyframe")
                        continue;
                    float time = XmlConvert.ToSingle(keyframeNode.Attributes["time"].Value);
                    var curve = new AnimationCurve();
                    foreach (XmlNode curveNode in keyframeNode.ChildNodes)
                    {
                        if (curveNode.Name != "curveKeyframe")
                            continue;
                        curve.AddKey(new UnityEngine.Keyframe(
                                XmlConvert.ToSingle(curveNode.Attributes["time"].Value),
                                XmlConvert.ToSingle(curveNode.Attributes["value"].Value),
                                XmlConvert.ToSingle(curveNode.Attributes["inTangent"].Value),
                                XmlConvert.ToSingle(curveNode.Attributes["outTangent"].Value)));
                    }
                    if (channel.keyframes.ContainsKey(time) == false)
                        channel.keyframes.Add(time, new Keyframe(target.ReadValueFromXml(keyframeNode), target, curve));
                }
                if (channel.keyframes.Count != 0)
                    clip.channels.Add(channel);
            }
            return clip.channels.Count == 0 ? null : clip;
        }

        /// <summary>The object an attribute holds the place of; found is false when there is no such attribute or object.</summary>
        private static ObjectCtrlInfo ObjectAt(XmlNode node, string attribute, List<KeyValuePair<int, ObjectCtrlInfo>> dic, out bool found)
        {
            found = node.Attributes[attribute] != null;
            if (found == false)
                return null;
            int index = XmlConvert.ToInt32(node.Attributes[attribute].Value);
            if (index < 0)
                return null;
            if (index >= dic.Count)
            {
                found = false;
                return null;
            }
            return dic[index].Value;
        }

        private void ReadStrips(XmlNode root, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            var clips = new Dictionary<int, MotionClip>();
            foreach (XmlNode clipNode in root.ChildNodes)
            {
                if (clipNode.Name != "nlaClip")
                    continue;
                try
                {
                    MotionClip clip = ReadClip(clipNode, clipNode.Attributes["name"]?.Value ?? "Clip", XmlConvert.ToSingle(clipNode.Attributes["length"].Value), dic);
                    if (clip == null)
                        continue;
                    clip.name = UniqueClipName(clip.name);
                    clips[XmlConvert.ToInt32(clipNode.Attributes["id"].Value)] = clip;
                    _clips.Add(clip);
                }
                catch (Exception e)
                {
                    Logger.LogError("Couldn't read a clip:\n" + e);
                }
            }

            foreach (XmlNode stripNode in root.ChildNodes)
            {
                if (stripNode.Name != "strip")
                    continue;
                try
                {
                    MotionClip clip;
                    if (stripNode.Attributes["clip"] != null)
                    {
                        if (clips.TryGetValue(XmlConvert.ToInt32(stripNode.Attributes["clip"].Value), out clip) == false)
                            continue;
                    }
                    else
                    {
                        // Written before clips were saved on their own: the clip is inside the strip.
                        clip = ReadClip(stripNode, stripNode.Attributes["name"]?.Value ?? "Clip", XmlConvert.ToSingle(stripNode.Attributes["clipLength"].Value), dic);
                        if (clip == null)
                            continue;
                        clip.name = UniqueClipName(clip.name);
                        _clips.Add(clip);
                    }

                    var strip = new MotionStrip
                    {
                        clip = clip,
                        start = XmlConvert.ToSingle(stripNode.Attributes["start"].Value),
                        scale = XmlConvert.ToSingle(stripNode.Attributes["scale"].Value),
                        repeat = XmlConvert.ToInt32(stripNode.Attributes["repeat"].Value),
                        enabled = stripNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(stripNode.Attributes["enabled"].Value)
                    };
                    bool ownerFound;
                    strip.owner = ObjectAt(stripNode, "owner", dic, out ownerFound);
                    if (ownerFound == false)
                        strip.owner = OwnerOf(clip);

                    // Layering attributes, all optional so older strips still load.
                    if (stripNode.Attributes["lane"] != null)
                        strip.lane = Mathf.Max(0, XmlConvert.ToInt32(stripNode.Attributes["lane"].Value));
                    if (stripNode.Attributes["influence"] != null)
                        strip.influence = XmlConvert.ToSingle(stripNode.Attributes["influence"].Value);
                    if (stripNode.Attributes["blendIn"] != null)
                        strip.blendIn = XmlConvert.ToSingle(stripNode.Attributes["blendIn"].Value);
                    if (stripNode.Attributes["blendOut"] != null)
                        strip.blendOut = XmlConvert.ToSingle(stripNode.Attributes["blendOut"].Value);
                    if (stripNode.Attributes["blendMode"] != null)
                    {
                        try
                        {
                            strip.blendMode = (StripBlendMode)Enum.Parse(typeof(StripBlendMode), stripNode.Attributes["blendMode"].Value);
                        }
                        catch (Exception)
                        {
                            strip.blendMode = StripBlendMode.Replace;
                        }
                    }
                    if (stripNode.Attributes["reverse"] != null)
                        strip.reverse = XmlConvert.ToBoolean(stripNode.Attributes["reverse"].Value);
                    if (stripNode.Attributes["clipStart"] != null)
                        strip.clipStart = XmlConvert.ToSingle(stripNode.Attributes["clipStart"].Value);
                    if (stripNode.Attributes["clipEnd"] != null)
                        strip.clipEnd = XmlConvert.ToSingle(stripNode.Attributes["clipEnd"].Value);
                    if (stripNode.Attributes["extrapolation"] != null)
                    {
                        try
                        {
                            strip.extrapolation = (StripExtrapolation)Enum.Parse(typeof(StripExtrapolation), stripNode.Attributes["extrapolation"].Value);
                        }
                        catch (Exception)
                        {
                            strip.extrapolation = StripExtrapolation.Hold;
                        }
                    }
                    foreach (XmlNode keyNode in stripNode.ChildNodes)
                    {
                        if (keyNode.Name == "influenceKey")
                            strip.influenceKeys[XmlConvert.ToSingle(keyNode.Attributes["time"].Value)] = XmlConvert.ToSingle(keyNode.Attributes["value"].Value);
                    }
                    // A strip overlapping another of its object's in the lane read goes to one with room:
                    // strips of several objects used to share lanes, and now each object has its own.
                    if (OverlapsInLane(strip, strip.lane))
                    {
                        for (int lane = 0; lane <= TopLane(strip.owner) + 1; ++lane)
                        {
                            strip.lane = lane;
                            if (OverlapsInLane(strip, lane) == false)
                                break;
                        }
                    }
                    _strips.Add(strip);
                }
                catch (Exception e)
                {
                    Logger.LogError("Couldn't read a strip:\n" + e);
                }
            }

            foreach (XmlNode stackNode in root.ChildNodes)
            {
                if (stackNode.Name != "nlaStack")
                    continue;
                try
                {
                    bool found;
                    ObjectCtrlInfo owner = ObjectAt(stackNode, "owner", dic, out found);
                    if (found == false)
                        continue;
                    NlaStack stack = StackOf(owner);
                    try
                    {
                        stack.actionBlend = (StripBlendMode)Enum.Parse(typeof(StripBlendMode), stackNode.Attributes["actionBlend"].Value);
                    }
                    catch (Exception)
                    {
                        stack.actionBlend = StripBlendMode.Replace;
                    }
                    stack.actionInfluence = XmlConvert.ToSingle(stackNode.Attributes["actionInfluence"].Value);
                    foreach (XmlNode laneNode in stackNode.ChildNodes)
                    {
                        if (laneNode.Name != "lane")
                            continue;
                        NlaLane lane = stack.Lane(XmlConvert.ToInt32(laneNode.Attributes["index"].Value));
                        lane.name = laneNode.Attributes["name"]?.Value ?? "";
                        lane.mute = XmlConvert.ToBoolean(laneNode.Attributes["mute"].Value);
                        lane.solo = XmlConvert.ToBoolean(laneNode.Attributes["solo"].Value);
                        lane.locked = XmlConvert.ToBoolean(laneNode.Attributes["lock"].Value);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("Couldn't read an NLA stack:\n" + e);
                }
            }
        }

        /// <summary>
        /// Finds the live interpolable an element describes, creating it when the scene does not have it.
        /// A strip target with no keyframes of its own is never written by the tree writer, so on load it
        /// has to be brought back from the strip's own copy of the binding attributes.
        /// </summary>
        private Interpolable ResolveInterpolable(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            try
            {
                string ownerId = node.Attributes["owner"]?.Value;
                string id = node.Attributes["id"]?.Value;
                if (ownerId == null || id == null)
                    return null;

                ObjectCtrlInfo oci = null;
                if (node.Attributes["objectIndex"] != null)
                {
                    int objectIndex = XmlConvert.ToInt32(node.Attributes["objectIndex"].Value);
                    if (objectIndex < 0 || objectIndex >= dic.Count)
                        return null;
                    oci = dic[objectIndex].Value;
                }

                InterpolableModel model = _interpolableModelsList.Find(m => m.owner == ownerId && m.id == id);
                if (model == null)
                    return null;

                Interpolable interpolable = model.readParameterFromXml != null
                        ? new Interpolable(oci, model.readParameterFromXml(oci, node), model)
                        : new Interpolable(oci, model);

                Interpolable existing;
                if (_interpolables.TryGetValue(interpolable.GetHashCode(), out existing))
                    return existing;

                _interpolables.Add(interpolable.GetHashCode(), interpolable);
                _interpolablesTree.AddLeaf(interpolable);
                return interpolable;
            }
            catch (Exception e)
            {
                Logger.LogError("Couldn't resolve a strip channel:\n" + e);
                return null;
            }
        }
        #endregion
    }
}
