using System.Collections.Generic;
using Timeline.Graph;
using Timeline.Shared;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// What decides where a keyframe's handle points. Blender's five, and they mean the same here.
    /// </summary>
    public enum HandleType
    {
        /// <summary>Wherever it was dragged. The two sides of a keyframe are unrelated.</summary>
        Free,
        /// <summary>Wherever it was dragged, with the other side kept pointing the opposite way.</summary>
        Aligned,
        /// <summary>Straight at the neighbouring keyframe, which makes that half of the segment straight.</summary>
        Vector,
        /// <summary>Worked out from the two neighbours, so the curve runs smoothly through the keyframe.</summary>
        Auto,
        /// <summary>The same, but flattened where the keyframe is a peak, so the curve never overshoots it.</summary>
        AutoClamped
    }

    /// <summary>
    /// A keyframe's two handles.
    ///
    /// Offsets are from the keyframe itself, in real units: seconds across, and whatever the track holds
    /// down. One pair per component, so the X of a position can be shaped without touching its Y. The
    /// stored offsets only mean anything for <see cref="HandleType.Free"/> and
    /// <see cref="HandleType.Aligned"/>; the other three are worked out from the neighbours every time
    /// they are asked for, which is what keeps them right after a neighbour moves.
    /// </summary>
    public sealed class KeyframeHandles
    {
        public HandleType leftType = HandleType.Auto;
        public HandleType rightType = HandleType.Auto;
        public Vector2[] left;
        public Vector2[] right;

        public KeyframeHandles(int slots)
        {
            left = new Vector2[Mathf.Max(slots, 1)];
            right = new Vector2[Mathf.Max(slots, 1)];
        }

        public KeyframeHandles Clone()
        {
            var copy = new KeyframeHandles(left.Length)
            {
                leftType = leftType,
                rightType = rightType
            };
            System.Array.Copy(left, copy.left, left.Length);
            System.Array.Copy(right, copy.right, right.Length);
            return copy;
        }
    }

    /// <summary>
    /// Bezier handles, and the exact translation between them and the easing curves Timeline has always
    /// stored.
    ///
    /// The translation is exact in one direction and only that direction, which is the whole story of
    /// this file. An easing curve is a two key AnimationCurve, which is a cubic Hermite, and a cubic
    /// Bezier whose handles sit a third of the way along the segment is the same curve written
    /// differently. So every keyframe ever saved converts to handles with nothing lost. Going back is
    /// lossy, because a handle can be dragged along the time axis and a Hermite cannot say that; scenes
    /// are written in both forms so an older Timeline still plays an approximation.
    ///
    /// Rotations are the exception. A rotation track holds quaternions and interpolates by slerping
    /// between them, which traces a path no set of per axis curves describes. Its handles therefore
    /// shape the <i>timing</i> of that slerp rather than three separate values - the same thing its
    /// easing curve already did. Per axis handles on a rotation are what "Split into X / Y / Z" is for.
    /// </summary>
    internal static class HandleMath
    {
        /// <summary>Rotations shape when, not what: their handles run from 0 to 1, not in degrees.</summary>
        public static bool IsFactorSpace(object value)
        {
            return value is Quaternion;
        }

        /// <summary>
        /// How many handle pairs a value needs, or none at all.
        ///
        /// None is the important answer. A pose, an animation or a name has no halfway point, so there
        /// is no curve between two of them to shape: those keyframes hand the interpolable both values
        /// and a factor and let it decide when to switch. Give them handles and they would be handed one
        /// computed value instead, and would never switch at all. Booleans and whole numbers are left
        /// out for the same reason, that a curve through them says nothing the delegate does not
        /// already decide by rounding.
        /// </summary>
        public static int Slots(object value)
        {
            if (Nla.ValueBlend.IsBlendable(value) == false)
                return 0;
            return IsFactorSpace(value) ? 1 : Mathf.Max(CurveComponents.Count(value), 1);
        }

        /// <summary>
        /// Whether a keyframe's easing can become handles without losing anything.
        ///
        /// Two keys and finite tangents is a cubic, which converts exactly. More keys means somebody
        /// shaped the easing by hand in the Keyframe window, and an infinite tangent is a step: neither
        /// is a cubic, so neither is a Bezier, and both keep using the easing curve they already have.
        /// Blender has no handle shape for a constant either.
        /// </summary>
        public static bool CanConvert(Keyframe keyframe)
        {
            if (keyframe == null || keyframe.curve == null || keyframe.curve.length != 2)
                return false;
            if (Slots(keyframe.value) == 0)
                return false;
            UnityEngine.Keyframe[] keys = keyframe.curve.keys;
            foreach (UnityEngine.Keyframe key in keys)
            {
                if (float.IsInfinity(key.inTangent) || float.IsNaN(key.inTangent) ||
                    float.IsInfinity(key.outTangent) || float.IsNaN(key.outTangent))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Gives a whole track handles, reading them out of the easing curves it already has.
        ///
        /// Nothing about how the track plays changes: the handles describe the very same cubics. Called
        /// once when a scene is loaded and once when a track is first touched, so from then on the
        /// handles are what everything works from.
        /// </summary>
        /// <summary>How many segments the last conversion actually read in, so a load can say so.</summary>
        public static int converted;

        public static void Convert(SortedList<float, Keyframe> keyframes)
        {
            int count = keyframes.Count;
            if (count < 2)
                return;

            IList<Keyframe> frames = keyframes.Values;
            for (int i = 0; i + 1 < count; ++i)
            {
                // Already converted: its handles are the truth now and re-reading the easing curve would
                // throw away every handle that has been dragged since.
                if (HasHandles(frames[i], frames[i + 1]))
                    continue;
                ConvertSegment(keyframes, i);
                if (HasHandles(frames[i], frames[i + 1]))
                    ++converted;
            }

            // The ends have nothing beyond them, so their outer handle has no segment to describe. They
            // still need the object, or the segment they do belong to would have only one side of it.
            EnsureEnd(frames, 0);
            EnsureEnd(frames, count - 1);
        }

        /// <summary>
        /// Gives an end keyframe its handles, but only when the keyframe beside it has them: on a track
        /// whose shape is not a curve at all there is nothing for them to describe, and creating them
        /// would tell everything downstream to use handles that say nothing.
        /// </summary>
        private static void EnsureEnd(IList<Keyframe> frames, int index)
        {
            int neighbour = index == 0 ? 1 : index - 1;
            if (neighbour < 0 || neighbour >= frames.Count || frames[neighbour].handles == null)
                return;
            EnsureHandles(frames[index], Slots(frames[index].value));
        }

        /// <summary>
        /// Reads one segment's easing curve into the handles either side of it, whatever those handles
        /// were before. This is the exact translation: the curve is a cubic Hermite and a Bezier with
        /// its control points a third of the way along the segment is the same cubic.
        ///
        /// Used when a scene is loaded, and again whenever the Keyframe window edits a curve directly,
        /// which is the one place the older representation is still the one being changed.
        /// </summary>
        public static void ConvertSegment(SortedList<float, Keyframe> keyframes, int index)
        {
            if (index < 0 || index + 1 >= keyframes.Count)
                return;

            Keyframe left = keyframes.Values[index];
            Keyframe right = keyframes.Values[index + 1];
            if (CanConvert(left) == false)
            {
                // Not a cubic, so there is nothing to read. Saying so here is what keeps a step or a
                // Bounce from being quietly ignored in favour of whatever handles happened to be there.
                left.shapedByCurve = left.handles != null;
                return;
            }
            left.shapedByCurve = false;

            float span = keyframes.Keys[index + 1] - keyframes.Keys[index];
            if (span <= 0.0001f)
                return;

            UnityEngine.Keyframe[] keys = left.curve.keys;
            float outTangent = keys[0].outTangent;
            float inTangent = keys[keys.Length - 1].inTangent;

            int slots = Slots(left.value);
            EnsureHandles(left, slots);
            EnsureHandles(right, Slots(right.value));

            bool factor = IsFactorSpace(left.value);
            for (int c = 0; c < slots; ++c)
            {
                float rise = factor ? 1f : CurveComponents.Get(right.value, c) - CurveComponents.Get(left.value, c);
                float x;
                float y;
                BezierMath.FromTangent(span, rise, outTangent, true, out x, out y);
                left.handles.right[c] = new Vector2(x, y);
                BezierMath.FromTangent(span, rise, inTangent, false, out x, out y);
                if (c < right.handles.left.Length)
                    right.handles.left[c] = new Vector2(x, y);
            }
            // Free, because the two sides came from two different easing curves and were never related
            // to each other. Calling them Auto would quietly reshape the animation.
            left.handles.rightType = HandleType.Free;
            right.handles.leftType = HandleType.Free;
        }

        private static void EnsureHandles(Keyframe keyframe, int slots)
        {
            if (slots == 0)
                return;
            if (keyframe.handles == null || keyframe.handles.left.Length != slots)
                keyframe.handles = new KeyframeHandles(slots);
        }

        /// <summary>
        /// Whether a segment is described by handles rather than by its easing curve.
        ///
        /// The left keyframe owns the segment, so it is the one whose <see cref="Keyframe.shapedByCurve"/>
        /// decides. That flag is what lets a step, or one of the sampled easing equations, sit on a track
        /// that is otherwise working in handles: its segment goes back to reading the curve, and the
        /// handles either side of it go on describing the segments that are still curves.
        /// </summary>
        public static bool HasHandles(Keyframe left, Keyframe right)
        {
            return left != null && right != null &&
                   left.handles != null && right.handles != null && left.shapedByCurve == false;
        }

        #region Effective offsets
        /// <summary>
        /// Where a keyframe's handles actually point, which for three of the five types is not what is
        /// stored but what the neighbours imply. Worked out on demand rather than written down, so a
        /// handle stays right when the keyframe beside it moves.
        /// </summary>
        public static void Offsets(SortedList<float, Keyframe> keyframes, int index, int component,
                                   out Vector2 left, out Vector2 right)
        {
            left = Vector2.zero;
            right = Vector2.zero;
            IList<float> times = keyframes.Keys;
            IList<Keyframe> frames = keyframes.Values;
            Keyframe keyframe = frames[index];
            if (keyframe.handles == null)
                return;
            bool factor = IsFactorSpace(keyframe.value);

            float time = times[index];
            float value = factor ? 0f : CurveComponents.Get(keyframe.value, component);

            bool hasPrevious = index > 0;
            bool hasNext = index + 1 < frames.Count;
            float previousTime = hasPrevious ? times[index - 1] : time;
            float nextTime = hasNext ? times[index + 1] : time;
            // In factor space a segment always runs 0 to 1, so the neighbour on either side is a whole
            // unit away whichever keyframe it is.
            float previousValue = hasPrevious ? (factor ? value - 1f : CurveComponents.Get(frames[index - 1].value, component)) : value;
            float nextValue = hasNext ? (factor ? value + 1f : CurveComponents.Get(frames[index + 1].value, component)) : value;

            float back = hasPrevious ? (time - previousTime) / 3f : 0f;
            float forward = hasNext ? (nextTime - time) / 3f : 0f;

            float slope = AutoSlope(keyframe.handles, hasPrevious, hasNext,
                                    previousTime, previousValue, time, value, nextTime, nextValue);

            left = Side(keyframe.handles, component, false, back, slope,
                        hasPrevious ? (value - previousValue) / 3f : 0f);
            right = Side(keyframe.handles, component, true, forward, slope,
                         hasNext ? (nextValue - value) / 3f : 0f);
        }

        private static Vector2 Side(KeyframeHandles handles, int component, bool outgoing,
                                    float reach, float autoSlope, float vectorRise)
        {
            HandleType type = outgoing ? handles.rightType : handles.leftType;
            float sign = outgoing ? 1f : -1f;
            switch (type)
            {
                case HandleType.Free:
                case HandleType.Aligned:
                    Vector2[] store = outgoing ? handles.right : handles.left;
                    return component < store.Length ? store[component] : Vector2.zero;
                case HandleType.Vector:
                    return new Vector2(sign * reach, sign * vectorRise);
                default:
                    return new Vector2(sign * reach, sign * reach * autoSlope);
            }
        }

        /// <summary>
        /// The slope an automatic handle takes: the line through the two neighbours, which is what makes
        /// the curve pass smoothly through the keyframe between them.
        ///
        /// Clamped flattens that line to nothing wherever the keyframe is higher or lower than both of
        /// its neighbours. Without it a peak gets a sloped handle and the curve sails past the value you
        /// keyframed before coming back to it, which on a limb reads as an overshoot nobody asked for.
        /// </summary>
        private static float AutoSlope(KeyframeHandles handles, bool hasPrevious, bool hasNext,
                                       float previousTime, float previousValue,
                                       float time, float value, float nextTime, float nextValue)
        {
            bool clamped = handles.leftType == HandleType.AutoClamped || handles.rightType == HandleType.AutoClamped;
            if (hasPrevious == false || hasNext == false)
            {
                // An end keyframe has only one side to take a slope from. Clamped ends flat, which is
                // what stops an animation drifting out of its first pose before it starts.
                if (clamped)
                    return 0f;
                if (hasNext && nextTime - time > 0.0001f)
                    return (nextValue - value) / (nextTime - time);
                if (hasPrevious && time - previousTime > 0.0001f)
                    return (value - previousValue) / (time - previousTime);
                return 0f;
            }

            float span = nextTime - previousTime;
            if (span <= 0.0001f)
                return 0f;
            if (clamped && (value - previousValue) * (nextValue - value) <= 0f)
                return 0f; // a peak or a trough: level, so the curve stops here rather than passing through
            return (nextValue - previousValue) / span;
        }
        #endregion

        #region Evaluation
        /// <summary>
        /// The curve between two keyframes, at a time, for one component.
        ///
        /// A Bezier is written in terms of its own parameter rather than of time, so the time has to be
        /// solved for first. The handles are clamped to their own segment before that, which is what
        /// guarantees there is exactly one answer: a handle reaching past the keyframe on the other side
        /// would fold the curve back on itself and a moment in time would have two values.
        /// </summary>
        public static float Evaluate(SortedList<float, Keyframe> keyframes, int leftIndex, int component, float time)
        {
            IList<float> times = keyframes.Keys;
            IList<Keyframe> frames = keyframes.Values;
            Keyframe left = frames[leftIndex];
            Keyframe right = frames[leftIndex + 1];
            bool factor = IsFactorSpace(left.value);

            float x0 = times[leftIndex];
            float x3 = times[leftIndex + 1];
            float y0 = factor ? 0f : CurveComponents.Get(left.value, component);
            float y3 = factor ? 1f : CurveComponents.Get(right.value, component);

            Vector2 leftOut;
            Vector2 rightIn;
            Offsets(keyframes, leftIndex, component, out _, out leftOut);
            Offsets(keyframes, leftIndex + 1, component, out rightIn, out _);

            float span = x3 - x0;
            if (span <= 0.0001f)
                return y3;

            float x1 = x0 + Mathf.Clamp(leftOut.x, 0f, span);
            float x2 = x3 + Mathf.Clamp(rightIn.x, -span, 0f);
            // Both handles reaching most of the way across can still cross each other, which folds the
            // curve. Blender pulls them back proportionally; so does this.
            if (x1 > x2)
            {
                float middle = (x1 + x2) * 0.5f;
                x1 = Mathf.Min(x1, middle);
                x2 = Mathf.Max(x2, middle);
            }
            float y1 = y0 + leftOut.y;
            float y2 = y3 + rightIn.y;

            float u = BezierMath.SolveU(x0, x1, x2, x3, Mathf.Clamp(time, x0, x3));
            return BezierMath.Cubic(y0, y1, y2, y3, u);
        }

        /// <summary>
        /// Reused rather than allocated per call. This runs once per component per track twice a frame,
        /// so a rig would otherwise hand the collector a few thousand four element arrays a second for
        /// nothing. Unity's main thread is the only caller.
        /// </summary>
        private static readonly float[] _componentBuffer = new float[4];

        /// <summary>The whole value a segment is worth at a time, built component by component.</summary>
        public static object Value(SortedList<float, Keyframe> keyframes, int leftIndex, float time)
        {
            Keyframe left = keyframes.Values[leftIndex];
            int components = CurveComponents.Count(left.value);
            if (components == 0 || components > _componentBuffer.Length)
                return left.value;

            for (int c = 0; c < components; ++c)
                _componentBuffer[c] = Evaluate(keyframes, leftIndex, c, time);
            return CurveComponents.FromComponents(left.value, _componentBuffer);
        }
        #endregion

        #region Writing back
        /// <summary>
        /// Rebuilds the easing curve a segment would have had, so a scene stays readable by a Timeline
        /// that knows nothing about handles.
        ///
        /// This is the lossy direction. A Hermite pins its control points a third of the way along the
        /// segment; a handle that has been dragged along the time axis cannot say that, so the slope it
        /// implies is written and the reach is dropped. What an older Timeline plays is the same curve
        /// wherever the handles were left near their default reach, and a fair approximation otherwise.
        /// </summary>
        public static void SyncEasing(SortedList<float, Keyframe> keyframes)
        {
            for (int i = 0; i + 1 < keyframes.Count; ++i)
                SyncEasingAt(keyframes, i);
        }

        /// <summary>
        /// One segment's easing curve, rebuilt from its handles.
        ///
        /// Worth having on its own: dragging a handle only changes the two segments either side of a
        /// keyframe, and rebuilding the whole track for that would allocate a curve per keyframe on
        /// every frame of the drag. On a baked track that is hundreds of them, sixty times a second.
        /// </summary>
        public static void SyncEasingAt(SortedList<float, Keyframe> keyframes, int index)
        {
            if (index < 0 || index + 1 >= keyframes.Count)
                return;

            Keyframe left = keyframes.Values[index];
            Keyframe right = keyframes.Values[index + 1];
            if (HasHandles(left, right) == false)
                return;

            float span = keyframes.Keys[index + 1] - keyframes.Keys[index];
            if (span <= 0.0001f)
                return;

            bool factor = IsFactorSpace(left.value);
            float rise = factor ? 1f : CurveComponents.Get(right.value, 0) - CurveComponents.Get(left.value, 0);

            Vector2 leftOut;
            Vector2 rightIn;
            Offsets(keyframes, index, 0, out _, out leftOut);
            Offsets(keyframes, index + 1, 0, out rightIn, out _);

            left.curve = new AnimationCurve(
                    new UnityEngine.Keyframe(0f, 0f, 0f, NormalisedSlope(leftOut, span, rise)),
                    new UnityEngine.Keyframe(1f, 1f, NormalisedSlope(rightIn, span, rise), 0f));
        }

        /// <summary>
        /// A handle as the easing curve measures slope: rise over run, both as fractions of the segment.
        /// A segment whose two keyframes hold the same value has no rise to measure against, so the only
        /// honest answer is the straight line.
        /// </summary>
        private static float NormalisedSlope(Vector2 offset, float span, float rise)
        {
            return BezierMath.ToTangent(offset.x, offset.y, span, rise);
        }
        #endregion
    }
}
