using System.Collections.Generic;
using Studio;
using UnityEngine;

namespace Timeline.Nla
{
    /// <summary>What a strip does outside its own span.</summary>
    internal enum StripExtrapolation
    {
        /// <summary>Silent. The channels fall back to their own keyframes.</summary>
        Nothing,
        /// <summary>Holds the first frame before the strip and the last one after it.</summary>
        Hold,
        /// <summary>Holds the last frame after the strip, but stays silent before it.</summary>
        HoldForward
    }

    /// <summary>One channel of a clip: the keyframes it plays and the interpolable they drive.</summary>
    internal sealed class ClipChannel
    {
        public readonly Interpolable target;
        /// <summary>Times are clip local, starting at zero.</summary>
        public readonly SortedList<float, Keyframe> keyframes = new SortedList<float, Keyframe>();

        public ClipChannel(Interpolable target)
        {
            this.target = target;
        }
    }

    /// <summary>
    /// A reusable piece of animation, the equivalent of an action. It owns its keyframes outright, so
    /// placing it several times on the timeline costs one copy rather than duplicating keyframes.
    /// </summary>
    internal sealed class MotionClip
    {
        public string name = "Clip";
        public float length;
        public readonly List<ClipChannel> channels = new List<ClipChannel>();

        /// <summary>A clip of its own with the same keys, for a strip that should stop sharing.</summary>
        public MotionClip Copy(string newName)
        {
            var copy = new MotionClip { name = newName, length = length };
            foreach (ClipChannel channel in channels)
            {
                var c = new ClipChannel(channel.target);
                foreach (KeyValuePair<float, Keyframe> pair in channel.keyframes)
                    c.keyframes.Add(pair.Key, new Keyframe(pair.Value, channel.target));
                copy.channels.Add(c);
            }
            return copy;
        }
    }

    /// <summary>The left, right and factor triple a channel resolves to at some time.</summary>
    internal struct StripSample
    {
        public object left;
        public object right;
        public float factor;
    }

    /// <summary>
    /// A placement of a clip on the timeline: where it starts, how much of the clip it plays, how fast
    /// and how many times.
    ///
    /// Sampling deliberately produces a left/right/factor triple rather than a finished value, because
    /// that is exactly what the interpolable delegates already take. Handing them a computed value would
    /// work for numbers but would break every discrete interpolable, animation and pose among them,
    /// whose delegate does something other than a lerp.
    /// </summary>
    internal sealed class MotionStrip
    {
        public MotionClip clip;
        /// <summary>The object whose stack the strip is in, null for scene-wide tracks: every object has lanes of its own.</summary>
        public ObjectCtrlInfo owner;
        public float start;
        public float scale = 1f;
        public int repeat = 1;
        public bool enabled = true;
        public bool reverse;
        public StripExtrapolation extrapolation = StripExtrapolation.Hold;

        /// <summary>Lane index. Higher lanes are mixed in after lower ones.</summary>
        public int lane;
        public float influence = 1f;
        /// <summary>Influence keyed over time, at times from the strip's start; empty means the plain influence.</summary>
        public readonly SortedList<float, float> influenceKeys = new SortedList<float, float>();
        public float blendIn;
        public float blendOut;
        public StripBlendMode blendMode = StripBlendMode.Replace;

        /// <summary>
        /// Which part of the clip plays, in clip local time. Negative means the whole clip, so a strip
        /// keeps following its clip as the clip grows or shrinks until the range is set deliberately.
        /// </summary>
        public float clipStart = -1f;
        public float clipEnd = -1f;

        private float clipLength
        {
            get { return clip == null ? 0f : clip.length; }
        }

        public float effectiveClipStart
        {
            get { return clipStart < 0f ? 0f : Mathf.Clamp(clipStart, 0f, clipLength); }
        }

        public float effectiveClipEnd
        {
            get { return clipEnd < 0f ? clipLength : Mathf.Clamp(clipEnd, 0f, clipLength); }
        }

        /// <summary>Clip local duration of one pass, after trimming.</summary>
        public float clipSpan
        {
            get { return Mathf.Max(effectiveClipEnd - effectiveClipStart, 0.001f); }
        }

        /// <summary>Timeline duration of a single pass through the clip.</summary>
        public float singleLength
        {
            get { return clip == null ? 0f : Mathf.Max(clipSpan * Mathf.Max(scale, 0.01f), 0.01f); }
        }

        /// <summary>Timeline duration of the whole strip, repeats included.</summary>
        public float length
        {
            get { return singleLength * Mathf.Max(repeat, 1); }
        }

        public float end
        {
            get { return start + length; }
        }

        private bool holdsBefore
        {
            get { return extrapolation == StripExtrapolation.Hold; }
        }

        private bool holdsAfter
        {
            get { return extrapolation == StripExtrapolation.Hold || extrapolation == StripExtrapolation.HoldForward; }
        }

        /// <summary>
        /// How strongly the strip speaks at this time: its influence, tapered by the fade in and out.
        ///
        /// The time is clamped into the span before the taper is applied. Without that, a strip set to
        /// hold would speak at full influence right up to its start and then drop to zero as the fade in
        /// began, which reads as a jump.
        /// </summary>
        public float WeightAt(float time)
        {
            return WeightAt(time, true);
        }

        /// <summary>
        /// The same, with the backward hold switchable off by whoever knows what else is in the lane.
        ///
        /// Blender only lets the first strip in a track hold backwards, and for a reason: a later strip
        /// holding its first frame back over everything before it covers the earlier strips entirely.
        /// That is exactly what duplicating or splitting a strip did here - both copies held, the later
        /// one won, and only one of them ever played. The strip cannot tell whether it is first, so the
        /// sampler, which sees the whole lane, says so.
        /// </summary>
        public float WeightAt(float time, bool mayHoldBefore)
        {
            if (enabled == false || clip == null || clip.length <= 0f)
                return 0f;
            if (time < start && (holdsBefore == false || mayHoldBefore == false))
                return 0f;
            if (time >= end && holdsAfter == false)
                return 0f;

            float clamped = Mathf.Clamp(time, start, end);
            float weight = Mathf.Clamp01(InfluenceAt(clamped));
            if (blendIn > 0.001f && clamped < start + blendIn)
                weight *= (clamped - start) / blendIn;
            if (blendOut > 0.001f && clamped > end - blendOut)
                weight *= (end - clamped) / blendOut;
            return Mathf.Clamp01(weight);
        }

        /// <summary>The influence at a time: the keyed one where there are keys, straight between them.</summary>
        public float InfluenceAt(float time)
        {
            int count = influenceKeys.Count;
            if (count == 0)
                return influence;
            float local = time - start;
            IList<float> times = influenceKeys.Keys;
            IList<float> values = influenceKeys.Values;
            if (local <= times[0])
                return values[0];
            if (local >= times[count - 1])
                return values[count - 1];
            for (int i = 1; i < count; ++i)
            {
                if (local <= times[i])
                    return Mathf.Lerp(values[i - 1], values[i], (local - times[i - 1]) / Mathf.Max(times[i] - times[i - 1], 1e-5f));
            }
            return values[count - 1];
        }

        /// <summary>Where a clip time plays on the timeline in the strip's first pass: trimmed, scaled and reversed as it plays.</summary>
        public float ClipToTimeline(float local)
        {
            float from = effectiveClipStart;
            float offset = reverse ? from + clipSpan - local : local - from;
            return start + offset * Mathf.Max(scale, 0.01f);
        }

        /// <summary>The clip time a timeline time plays in the strip's first pass, the inverse of ClipToTimeline.</summary>
        public float TimelineToClip(float time)
        {
            float from = effectiveClipStart;
            float offset = (time - start) / Mathf.Max(scale, 0.01f);
            return reverse ? from + clipSpan - offset : from + offset;
        }

        /// <summary>Maps a timeline time to a clip local one, or reports that the strip is silent there.</summary>
        public bool TryLocalTime(float time, out float local)
        {
            return TryLocalTime(time, true, out local);
        }

        public bool TryLocalTime(float time, bool mayHoldBefore, out float local)
        {
            local = 0f;
            if (clip == null || clip.length <= 0f || enabled == false)
                return false;

            float from = effectiveClipStart;
            float span = clipSpan;

            if (time < start)
            {
                if (holdsBefore == false || mayHoldBefore == false)
                    return false;
                local = reverse ? from + span : from;
                return true;
            }
            if (time >= end)
            {
                if (holdsAfter == false)
                    return false;
                local = reverse ? from : from + span;
                return true;
            }

            float scaled = (time - start) / Mathf.Max(scale, 0.01f);
            float offset = repeat > 1 ? scaled % span : Mathf.Min(scaled, span);
            local = reverse ? from + span - offset : from + offset;
            return true;
        }

        /// <summary>
        /// Finds the pair of keyframes surrounding a clip local time and the eased factor between them.
        /// Mirrors what Timeline.Interpolate does for a track's own keyframes.
        /// </summary>
        public static bool TrySample(SortedList<float, Keyframe> keyframes, float time, out StripSample sample)
        {
            sample = default(StripSample);
            if (keyframes.Count == 0)
                return false;

            KeyValuePair<float, Keyframe> left = default(KeyValuePair<float, Keyframe>);
            KeyValuePair<float, Keyframe> right = default(KeyValuePair<float, Keyframe>);
            foreach (KeyValuePair<float, Keyframe> pair in keyframes)
            {
                if (pair.Key <= time)
                    left = pair;
                else
                {
                    right = pair;
                    break;
                }
            }

            if (left.Value != null && right.Value != null)
            {
                sample.left = left.Value.value;
                sample.right = right.Value.value;
                if (HandleMath.HasHandles(left.Value, right.Value))
                {
                    int index = keyframes.IndexOfKey(left.Key);
                    if (HandleMath.IsFactorSpace(left.Value.value))
                    {
                        // A rotation's handles shape the timing of the slerp, so there is still a factor.
                        sample.factor = HandleMath.Evaluate(keyframes, index, 0, time);
                    }
                    else
                    {
                        // Every component has a curve of its own, so there is no single factor to
                        // report. The value is worked out and handed over as a lerp that cannot move,
                        // the same as everywhere else that already knows what it wants.
                        object shaped = HandleMath.Value(keyframes, index, time);
                        sample.left = shaped;
                        sample.right = shaped;
                        sample.factor = 0f;
                    }
                    return true;
                }

                float normalized = (time - left.Key) / (right.Key - left.Key);
                sample.factor = left.Value.curve.Evaluate(normalized);
                return true;
            }

            Keyframe only = left.Value ?? right.Value;
            sample.left = only.value;
            sample.right = only.value;
            sample.factor = 0f;
            return true;
        }
    }
}
