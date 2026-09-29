namespace Timeline.Shared
{
    /// <summary>
    /// The cubic arithmetic behind keyframe handles, with no Unity in it so it can be tested.
    ///
    /// Two things happen here and only one of them is obvious. The obvious one is evaluating a Bezier,
    /// which needs a solve first because a Bezier is written in terms of its own parameter and what we
    /// have is a time. The other is the claim the whole handles feature rests on: that an easing curve
    /// and a Bezier with its control points a third of the way along the segment are the same curve, so
    /// every keyframe ever saved converts with nothing lost. <see cref="FromTangent"/> is that claim
    /// written down, and the tests are it checked.
    /// </summary>
    public static class BezierMath
    {
        public static float Cubic(float a, float b, float c, float d, float u)
        {
            float v = 1f - u;
            return v * v * v * a + 3f * v * v * u * b + 3f * v * u * u * c + u * u * u * d;
        }

        public static float Derivative(float a, float b, float c, float d, float u)
        {
            float v = 1f - u;
            return 3f * v * v * (b - a) + 6f * v * u * (c - b) + 3f * u * u * (d - c);
        }

        /// <summary>
        /// The curve parameter that lands on a given x.
        ///
        /// Newton first, because it is usually right within three or four steps, and a bisection to fall
        /// back on, because Newton stalls wherever the curve is momentarily flat in x - which is exactly
        /// what a handle dragged to the very edge of its segment produces. The control points have to be
        /// inside the segment for there to be a single answer at all; the caller clamps them.
        /// </summary>
        public static float SolveU(float x0, float x1, float x2, float x3, float x)
        {
            float span = x3 - x0;
            if (span <= 0.0001f)
                return 0f;

            float u = Clamp01((x - x0) / span);
            for (int i = 0; i < 6; ++i)
            {
                float error = Cubic(x0, x1, x2, x3, u) - x;
                if (error < 0f ? -error < 0.00005f * span : error < 0.00005f * span)
                    return u;
                float derivative = Derivative(x0, x1, x2, x3, u);
                if (derivative < 0.000001f && derivative > -0.000001f)
                    break;
                u = Clamp01(u - error / derivative);
            }

            float low = 0f;
            float high = 1f;
            for (int i = 0; i < 24; ++i)
            {
                u = (low + high) * 0.5f;
                if (Cubic(x0, x1, x2, x3, u) < x)
                    low = u;
                else
                    high = u;
            }
            return u;
        }

        /// <summary>
        /// A handle offset from the tangent an easing curve states.
        ///
        /// An easing curve is normalised in both directions, so a tangent of 1 means "as steep as the
        /// segment itself". A third of the way along the segment is therefore a third of the segment's
        /// own rise multiplied by that tangent, and the resulting Bezier is the very same cubic the
        /// easing curve was: a Hermite and a Bezier with control points at a third are two spellings of
        /// one curve.
        /// </summary>
        public static void FromTangent(float span, float rise, float tangent, bool outgoing,
                                       out float offsetX, out float offsetY)
        {
            float sign = outgoing ? 1f : -1f;
            offsetX = sign * span / 3f;
            offsetY = sign * tangent * rise / 3f;
        }

        /// <summary>
        /// The tangent an easing curve would need to describe a handle, which is the lossy direction:
        /// the reach along x is dropped, because a Hermite has no way to say it.
        /// </summary>
        public static float ToTangent(float offsetX, float offsetY, float span, float rise)
        {
            if (Abs(rise) < 0.000001f || Abs(offsetX) < 0.000001f)
                return 1f;
            float slope = (offsetY / rise) / (offsetX / span);
            return slope < -1000f ? -1000f : slope > 1000f ? 1000f : slope;
        }

        /// <summary>
        /// A cubic Hermite on 0..1 between the values 0 and 1, which is exactly what an easing curve
        /// with two keys is. Here so the conversion can be checked against the thing it claims to equal.
        /// </summary>
        public static float Hermite(float outTangent, float inTangent, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            // The p0 and p1 terms with p0 = 0 and p1 = 1 leave only the second basis function and the
            // two tangent ones.
            return (-2f * t3 + 3f * t2) + (t3 - 2f * t2 + t) * outTangent + (t3 - t2) * inTangent;
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }

        private static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }
    }
}
