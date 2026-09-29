using System.Collections.Generic;
using Timeline.Nla;
using UnityEngine;

namespace Timeline
{
    /// <summary>What a track does before its first keyframe and after its last one.</summary>
    public enum TrackExtrapolation
    {
        /// <summary>Holds the end values, which is what Timeline has always done.</summary>
        Hold,
        /// <summary>Carries on in whatever direction the track was going when it ran out of keyframes.</summary>
        Linear,
        /// <summary>Plays the keyframed range over and over for the whole duration.</summary>
        Cyclic,
        /// <summary>The same, but each repetition starts where the last one ended.</summary>
        CyclicOffset
    }

    /// <summary>
    /// Blender's cyclic F-curve modifier, in the one form that earns its keep here: a cycle authored
    /// once and repeated for as long as the timeline runs.
    ///
    /// A strip can already repeat, but a strip is a whole clip pushed down out of the way. This is for
    /// the track you are still working on - keyframe two steps, set it cyclic, and the character walks
    /// for the rest of the scene while you keep editing the two steps.
    /// </summary>
    internal static class TrackCycle
    {
        /// <summary>
        /// Folds a time outside the keyframed range back into it.
        ///
        /// <paramref name="cycles"/> is how many whole repetitions that skipped, negative before the
        /// first keyframe, which is what an offsetting track stacks its values with.
        /// </summary>
        public static float Wrap(SortedList<float, Keyframe> keyframes, TrackExtrapolation mode, float time, out int cycles)
        {
            cycles = 0;
            if (mode != TrackExtrapolation.Cyclic && mode != TrackExtrapolation.CyclicOffset)
                return time;
            if (keyframes.Count < 2)
                return time;

            float first = keyframes.Keys[0];
            float last = keyframes.Keys[keyframes.Count - 1];
            if (time >= first && time <= last)
                return time;

            float span = last - first;
            if (span <= 0.0001f)
                return time;

            float turns = Mathf.Floor((time - first) / span);
            cycles = (int)turns;
            return time - turns * span;
        }

        /// <summary>
        /// The two keyframes and the factor that continue a track's own slope past its ends.
        ///
        /// The factor runs outside 0..1 on purpose: every interpolable applies its value with
        /// LerpUnclamped, so handing one 1.4 carries the last segment on rather than stopping at it.
        /// The easing of that segment is deliberately not applied - past the end there is no segment
        /// left to ease, only a direction to keep going in.
        /// </summary>
        public static bool TryLinear(SortedList<float, Keyframe> keyframes, TrackExtrapolation mode, float time,
                                     out Keyframe from, out Keyframe to, out float factor)
        {
            from = null;
            to = null;
            factor = 0f;
            if (mode != TrackExtrapolation.Linear || keyframes.Count < 2)
                return false;

            int last = keyframes.Count - 1;
            float start = keyframes.Keys[0];
            float end = keyframes.Keys[last];

            int a;
            int b;
            if (time < start)
            {
                a = 0;
                b = 1;
            }
            else if (time > end)
            {
                a = last - 1;
                b = last;
            }
            else
            {
                return false; // inside the keyframes, where the track speaks for itself
            }

            float span = keyframes.Keys[b] - keyframes.Keys[a];
            if (span <= 0.0001f)
                return false;

            from = keyframes.Values[a];
            to = keyframes.Values[b];
            factor = (time - keyframes.Keys[a]) / span;
            return true;
        }

        /// <summary>
        /// The value a number of whole repetitions along, so a cycle that travels keeps travelling
        /// instead of snapping back to where it started. Values that cannot be added to - a pose, an
        /// animation - come back untouched, which turns an offsetting track back into a plain cyclic one.
        /// </summary>
        public static object Offset(object value, object first, object last, int cycles)
        {
            if (cycles == 0 || value == null || first == null || last == null)
                return value;
            if (ValueBlend.IsBlendable(value) == false || first.GetType() != value.GetType() || last.GetType() != value.GetType())
                return value;

            object from = cycles > 0 ? first : last;
            object to = cycles > 0 ? last : first;
            // A guard rather than a limit: the timeline is finite, so reaching it means the cycle is
            // shorter than a rounding error and the loop would not end usefully.
            int count = Mathf.Min(Mathf.Abs(cycles), 1024);
            for (int i = 0; i < count; ++i)
                value = ValueBlend.Add(value, to, from);
            return value;
        }
    }
}
