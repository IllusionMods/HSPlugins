using System;

namespace Timeline.Shared
{
    /// <summary>
    /// Ramer-Douglas-Peucker over a set of keyframes: which ones carry the shape and which ones only
    /// repeat what their neighbours already say.
    ///
    /// Kept apart from the track it is run on, and free of UnityEngine, because this is the part that is
    /// either right or quietly wrong. Given the same times and values it must always mark the same
    /// keyframes, which is a thing a test can hold it to.
    /// </summary>
    internal static class CurveReduction
    {
        /// <summary>
        /// Marks the keyframes to keep. <paramref name="values"/> is one array per keyframe, each holding
        /// that keyframe's components; the tolerance is in the same units as those components.
        /// </summary>
        public static bool[] Keep(float[] times, float[][] values, float tolerance)
        {
            int count = times == null ? 0 : times.Length;
            var keep = new bool[count];
            if (count == 0)
                return keep;

            keep[0] = true;
            keep[count - 1] = true;
            if (count > 2)
                Reduce(times, values, 0, count - 1, tolerance, keep);
            return keep;
        }

        /// <summary>
        /// How far the components of a whole set travel, which is what a tolerance is measured against so
        /// that one number means the same thing to a position in metres and a rotation in degrees.
        /// </summary>
        public static float Travel(float[][] values)
        {
            float travel = 0f;
            if (values == null || values.Length == 0)
                return 1f;
            int components = values[0] == null ? 0 : values[0].Length;
            for (int c = 0; c < components; ++c)
            {
                float min = float.PositiveInfinity;
                float max = float.NegativeInfinity;
                for (int i = 0; i < values.Length; ++i)
                {
                    if (values[i] == null || c >= values[i].Length)
                        continue;
                    min = Math.Min(min, values[i][c]);
                    max = Math.Max(max, values[i][c]);
                }
                if (min <= max)
                    travel = Math.Max(travel, max - min);
            }
            return travel <= 0.000001f ? 1f : travel;
        }

        private static void Reduce(float[] times, float[][] values, int first, int last, float tolerance, bool[] keep)
        {
            if (last <= first + 1)
                return;

            float span = times[last] - times[first];
            float worst = -1f;
            int worstIndex = -1;
            int components = values[first] == null ? 0 : values[first].Length;

            for (int i = first + 1; i < last; ++i)
            {
                float t = span <= 0f ? 0f : (times[i] - times[first]) / span;
                float distance = 0f;
                for (int c = 0; c < components; ++c)
                {
                    if (values[i] == null || c >= values[i].Length || c >= values[last].Length)
                        continue;
                    float straight = values[first][c] + (values[last][c] - values[first][c]) * t;
                    distance = Math.Max(distance, Math.Abs(values[i][c] - straight));
                }
                if (distance > worst)
                {
                    worst = distance;
                    worstIndex = i;
                }
            }

            if (worstIndex < 0 || worst < tolerance)
                return;

            keep[worstIndex] = true;
            Reduce(times, values, first, worstIndex, tolerance, keep);
            Reduce(times, values, worstIndex, last, tolerance, keep);
        }
    }
}
