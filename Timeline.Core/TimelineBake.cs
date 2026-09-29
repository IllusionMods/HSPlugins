using System.Collections.Generic;
using Timeline.Graph;
using Timeline.Nla;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// Writing a track's shape out as one keyframe per frame - Blender's Sample Keyframes.
    ///
    /// It is the exact opposite of simplifying, and the pair is the point: thin a recorded take down to
    /// the keyframes that carry it, edit those, then bake again when the shape has to be literal. Baking
    /// is also how a curve stops depending on anything clever - easing, the spline, a repeat - and
    /// becomes plain values that nothing downstream can reinterpret.
    /// </summary>
    public partial class Timeline
    {
        /// <summary>
        /// One operation cannot write more than this. A rig of seventy tracks baked over a minute at
        /// sixty frames is a quarter of a million keyframes, each of which the grid draws a marker for.
        /// </summary>
        private const int _maxBakedKeyframes = 12000;

        /// <summary>
        /// The stretch to bake. A repeating track is baked across the whole timeline whatever was
        /// selected, because the repetitions are the part that has to become real; anything else is
        /// baked over the selection, or over its own keyframes when there is none.
        /// </summary>
        private Vector2 BakeRange(Interpolable track, Vector2 selected)
        {
            if (track.extrapolation != TrackExtrapolation.Hold)
                return new Vector2(0f, _duration);
            if (float.IsNaN(selected.x))
                return new Vector2(track.keyframes.Keys[0], track.keyframes.Keys[track.keyframes.Count - 1]);
            return selected;
        }

        /// <summary>
        /// A pose or an animation has no halfway point, so sampling one would write the same value over
        /// and over and call it a curve.
        /// </summary>
        private static bool CanBake(Interpolable track)
        {
            return track.keyframes.Count != 0 &&
                   CurveComponents.Count(track.keyframes.Values[0].value) != 0 &&
                   ValueBlend.IsBlendable(track.keyframes.Values[0].value);
        }

        private void BakeTrack(Interpolable track, float from, float to, int rate)
        {
            int firstFrame = Mathf.RoundToInt(from * rate);
            int lastFrame = Mathf.RoundToInt(to * rate);

            var baked = new List<KeyValuePair<float, object>>(lastFrame - firstFrame + 1);
            for (int frame = firstFrame; frame <= lastFrame; ++frame)
            {
                float time = frame / (float)rate;
                object value = ValueAt(track, time);
                if (value != null)
                    baked.Add(new KeyValuePair<float, object>(time, value));
            }

            // Everything inside the baked stretch is replaced, keyframes outside it are left alone so
            // baking a section does not quietly rewrite the rest of the track.
            for (int i = track.keyframes.Count - 1; i >= 0; --i)
            {
                float time = track.keyframes.Keys[i];
                if (time >= from - 0.0001f && time <= to + 0.0001f)
                    track.keyframes.RemoveAt(i);
            }

            foreach (KeyValuePair<float, object> pair in baked)
            {
                if (track.keyframes.ContainsKey(pair.Key))
                    continue;
                track.keyframes.Add(pair.Key, new Keyframe(pair.Value, track, AnimationCurve.Linear(0f, 0f, 1f, 1f)));
            }

            // The shape now lives in how close together the keyframes are, so the two things that used
            // to shape it have nothing left to say and would only change the result if they stayed on.
            track.smooth = false;
            track.extrapolation = TrackExtrapolation.Hold;
            // Baked keyframes are linear between each other, and saying that in handles rather than
            // leaving them without any is what keeps the track one thing rather than two.
            HandleMath.Convert(track.keyframes);
        }

        /// <summary>
        /// What a track is worth at a time, on its own: the same reading the interpolation makes, minus
        /// the strips, which speak for a track only while the scene is actually playing.
        /// </summary>
        internal static object ValueAt(Interpolable track, float time)
        {
            if (track.keyframes.Count == 0)
                return null;

            int cycles;
            float trackTime = TrackCycle.Wrap(track.keyframes, track.extrapolation, time, out cycles);
            IList<float> times = track.keyframes.Keys;
            IList<Keyframe> frames = track.keyframes.Values;

            Keyframe linearFrom;
            Keyframe linearTo;
            float linearFactor;
            if (TrackCycle.TryLinear(track.keyframes, track.extrapolation, trackTime, out linearFrom, out linearTo, out linearFactor))
            {
                return ValueBlend.IsBlendable(linearFrom.value) && linearTo.value != null &&
                       linearFrom.value.GetType() == linearTo.value.GetType()
                       ? ValueBlend.Lerp(linearFrom.value, linearTo.value, linearFactor)
                       : linearFrom.value;
            }

            int leftIndex = -1;
            for (int i = 0; i < times.Count; ++i)
            {
                if (times[i] > trackTime)
                    break;
                leftIndex = i;
            }

            object value;
            if (leftIndex < 0)
            {
                value = frames[0].value;
            }
            else if (leftIndex + 1 >= frames.Count)
            {
                value = frames[frames.Count - 1].value;
            }
            else
            {
                Keyframe left = frames[leftIndex];
                Keyframe right = frames[leftIndex + 1];
                object smoothed;
                if (track.smooth && KeyframeSpline.TryEvaluate(track.keyframes, leftIndex, trackTime, out smoothed))
                {
                    value = smoothed;
                }
                else if (HandleMath.HasHandles(left, right))
                {
                    value = HandleMath.IsFactorSpace(left.value)
                            ? ValueBlend.Lerp(left.value, right.value,
                                              HandleMath.Evaluate(track.keyframes, leftIndex, 0, trackTime))
                            : HandleMath.Value(track.keyframes, leftIndex, trackTime);
                }
                else
                {
                    float span = times[leftIndex + 1] - times[leftIndex];
                    float factor = left.curve.Evaluate(span <= 0f ? 0f : (trackTime - times[leftIndex]) / span);
                    value = ValueBlend.IsBlendable(left.value) && right.value != null &&
                            left.value.GetType() == right.value.GetType()
                            ? ValueBlend.Lerp(left.value, right.value, factor)
                            : (factor < 1f ? left.value : right.value);
                }
            }

            if (cycles != 0 && track.extrapolation == TrackExtrapolation.CyclicOffset)
                value = TrackCycle.Offset(value, frames[0].value, frames[frames.Count - 1].value, cycles);
            return value;
        }
    }
}
