using System.Collections.Generic;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// Smooth interpolation across keyframes.
    ///
    /// Normally each keyframe carries an easing curve for the segment that follows it, and the value is a
    /// plain lerp driven by that curve. Every segment therefore starts and ends at zero velocity unless
    /// the tangents are hand matched, which is what makes unedited animations look mechanical.
    ///
    /// This evaluates a cubic Hermite spline through the surrounding keyframes instead, so speed carries
    /// through a keyframe rather than resetting at it. Tangents use the Fritsch-Carlson rule, which is
    /// monotone: the curve never overshoots the values it passes through. That matters here because a lot
    /// of interpolables are clamped quantities (alpha, 0..1 sliders, scale) where an overshooting spline
    /// would produce values the game never accepts.
    /// </summary>
    internal static class KeyframeSpline
    {
        private const int _maxComponents = 4;

        /// <summary>Types whose values can be meaningfully smoothed. Discrete values are left alone.</summary>
        public static bool IsSupported(object value)
        {
            return value is float || value is Vector2 || value is Vector3 || value is Vector4 ||
                   value is Color || value is Quaternion;
        }

        /// <summary>
        /// Evaluates the spline at <paramref name="time"/>, where <paramref name="leftIndex"/> is the
        /// keyframe at or before it. Returns false when smoothing does not apply and the caller should
        /// fall back to the normal curve driven lerp.
        /// </summary>
        public static bool TryEvaluate(SortedList<float, Keyframe> keyframes, int leftIndex, float time, out object result)
        {
            result = null;
            int count = keyframes.Count;
            if (leftIndex < 0 || leftIndex + 1 >= count)
                return false;

            IList<float> times = keyframes.Keys;
            IList<Keyframe> values = keyframes.Values;

            object template = values[leftIndex].value;
            int components = ComponentCount(template);
            if (components == 0)
                return false;

            float t0 = times[leftIndex];
            float t1 = times[leftIndex + 1];
            float span = t1 - t0;
            if (span <= 0f)
                return false;

            var v0 = new float[_maxComponents];
            var v1 = new float[_maxComponents];
            var previous = new float[_maxComponents];
            var next = new float[_maxComponents];
            if (Decompose(values[leftIndex].value, v0) == false || Decompose(values[leftIndex + 1].value, v1) == false)
                return false;

            // Outside the ends there is no neighbour, so the segment's own slope is reused, which makes
            // the spline behave like a plain line there instead of inventing a curve.
            bool hasPrevious = leftIndex - 1 >= 0 && Decompose(values[leftIndex - 1].value, previous);
            bool hasNext = leftIndex + 2 < count && Decompose(values[leftIndex + 2].value, next);
            float previousSpan = hasPrevious ? t0 - times[leftIndex - 1] : span;
            float nextSpan = hasNext ? times[leftIndex + 2] - t1 : span;

            bool isQuaternion = template is Quaternion;
            if (isQuaternion)
            {
                // q and -q are the same rotation; align everything to the left keyframe so the spline
                // does not take the long way round.
                if (hasPrevious)
                    Align(previous, v0);
                Align(v1, v0);
                if (hasNext)
                    Align(next, v1);
            }

            float s = (time - t0) / span;
            var output = new float[_maxComponents];
            for (int c = 0; c < components; ++c)
            {
                float slope = (v1[c] - v0[c]) / span;
                float previousSlope = hasPrevious ? (v0[c] - previous[c]) / previousSpan : slope;
                float nextSlope = hasNext ? (next[c] - v1[c]) / nextSpan : slope;

                float m0 = Tangent(previousSlope, slope, previousSpan, span);
                float m1 = Tangent(slope, nextSlope, span, nextSpan);
                output[c] = Hermite(v0[c], v1[c], m0, m1, span, s);
            }

            result = Compose(template, output);
            return result != null;
        }

        /// <summary>
        /// Fritsch-Carlson tangent: the weighted harmonic mean of the two adjacent slopes, and flat
        /// wherever they disagree in sign. Flattening at a turning point is what prevents overshoot, and
        /// it also reads correctly, a thrown object really does slow to a stop at the top of its arc.
        /// </summary>
        private static float Tangent(float slopeBefore, float slopeAfter, float spanBefore, float spanAfter)
        {
            if (slopeBefore * slopeAfter <= 0f)
                return 0f;

            float weightBefore = 2f * spanAfter + spanBefore;
            float weightAfter = spanAfter + 2f * spanBefore;
            return (weightBefore + weightAfter) / (weightBefore / slopeBefore + weightAfter / slopeAfter);
        }

        private static float Hermite(float from, float to, float tangentFrom, float tangentTo, float span, float s)
        {
            float s2 = s * s;
            float s3 = s2 * s;
            return (2f * s3 - 3f * s2 + 1f) * from
                 + (s3 - 2f * s2 + s) * span * tangentFrom
                 + (-2f * s3 + 3f * s2) * to
                 + (s3 - s2) * span * tangentTo;
        }

        private static void Align(float[] value, float[] reference)
        {
            float dot = value[0] * reference[0] + value[1] * reference[1] + value[2] * reference[2] + value[3] * reference[3];
            if (dot >= 0f)
                return;
            for (int i = 0; i < _maxComponents; ++i)
                value[i] = -value[i];
        }

        private static int ComponentCount(object value)
        {
            if (value is float)
                return 1;
            if (value is Vector2)
                return 2;
            if (value is Vector3)
                return 3;
            if (value is Vector4 || value is Color || value is Quaternion)
                return 4;
            return 0;
        }

        private static bool Decompose(object value, float[] target)
        {
            if (value is float)
            {
                target[0] = (float)value;
                return true;
            }
            if (value is Vector2)
            {
                Vector2 v = (Vector2)value;
                target[0] = v.x;
                target[1] = v.y;
                return true;
            }
            if (value is Vector3)
            {
                Vector3 v = (Vector3)value;
                target[0] = v.x;
                target[1] = v.y;
                target[2] = v.z;
                return true;
            }
            if (value is Vector4)
            {
                Vector4 v = (Vector4)value;
                target[0] = v.x;
                target[1] = v.y;
                target[2] = v.z;
                target[3] = v.w;
                return true;
            }
            if (value is Color)
            {
                Color v = (Color)value;
                target[0] = v.r;
                target[1] = v.g;
                target[2] = v.b;
                target[3] = v.a;
                return true;
            }
            if (value is Quaternion)
            {
                Quaternion v = (Quaternion)value;
                target[0] = v.x;
                target[1] = v.y;
                target[2] = v.z;
                target[3] = v.w;
                return true;
            }
            return false;
        }

        private static object Compose(object template, float[] source)
        {
            if (template is float)
                return source[0];
            if (template is Vector2)
                return new Vector2(source[0], source[1]);
            if (template is Vector3)
                return new Vector3(source[0], source[1], source[2]);
            if (template is Vector4)
                return new Vector4(source[0], source[1], source[2], source[3]);
            if (template is Color)
                return new Color(source[0], source[1], source[2], source[3]);
            if (template is Quaternion)
            {
                float magnitude = Mathf.Sqrt(source[0] * source[0] + source[1] * source[1] +
                                             source[2] * source[2] + source[3] * source[3]);
                if (magnitude < 0.0001f)
                    return null;
                return new Quaternion(source[0] / magnitude, source[1] / magnitude,
                                      source[2] / magnitude, source[3] / magnitude);
            }
            return null;
        }
    }
}
