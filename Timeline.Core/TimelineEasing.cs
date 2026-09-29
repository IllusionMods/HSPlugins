using System.Collections.Generic;
using UnityEngine;

namespace Timeline
{
    /// <summary>Blender's easing equations, the ten that are not just "linear" or "step".</summary>
    public enum EasingKind
    {
        Sine,
        Quadratic,
        Cubic,
        Quartic,
        Quintic,
        Exponential,
        Circular,
        Back,
        Bounce,
        Elastic
    }

    public enum EasingDirection
    {
        In,
        Out,
        InOut
    }

    /// <summary>
    /// The easing equations, baked into the normalised curves a keyframe already stores.
    ///
    /// Timeline's five presets - linear, ease in, ease out, smooth and step - are all a two key
    /// AnimationCurve can say, because a cubic between two points is a cubic. Bounce and Elastic are not
    /// cubics, they oscillate, so they cannot be written as tangents; they have to be sampled. That is
    /// what this does: evaluate the equation across the segment and hand back a curve that traces it.
    ///
    /// The result is still an ordinary keyframe curve. Nothing downstream knows an equation was
    /// involved, the graph editor draws it from the samples like any other shape, and a scene saved with
    /// one loads in a Timeline that has never heard of Bounce.
    /// </summary>
    internal static class TimelineEasing
    {
        /// <summary>
        /// How finely each equation is sampled. The smooth ones are cubics or close to it and need very
        /// little; the two that oscillate need enough points to catch every turn, or a bounce comes out
        /// as a slope with a kink in it.
        /// </summary>
        private static int Samples(EasingKind kind)
        {
            return kind == EasingKind.Bounce || kind == EasingKind.Elastic ? 64 : 24;
        }

        public static string Name(EasingKind kind, EasingDirection direction)
        {
            string tail = direction == EasingDirection.In ? "in"
                        : direction == EasingDirection.Out ? "out" : "in and out";
            return kind + " " + tail;
        }

        /// <summary>
        /// The curve for one equation. Sampled, with tangents from the neighbouring samples, so the
        /// shape between two samples is a cubic through the right slope rather than a straight line.
        /// </summary>
        public static AnimationCurve Curve(EasingKind kind, EasingDirection direction)
        {
            int samples = Samples(kind);
            var values = new float[samples + 1];
            for (int i = 0; i <= samples; ++i)
                values[i] = Evaluate(kind, direction, i / (float)samples);

            // The ends are pinned: a keyframe curve that does not start at 0 and end at 1 would offset
            // the values it eases between, which is a different thing from shaping the transition.
            values[0] = 0f;
            values[samples] = 1f;

            var keys = new UnityEngine.Keyframe[samples + 1];
            float step = 1f / samples;
            for (int i = 0; i <= samples; ++i)
            {
                float slope;
                if (i == 0)
                    slope = (values[1] - values[0]) / step;
                else if (i == samples)
                    slope = (values[samples] - values[samples - 1]) / step;
                else
                    slope = (values[i + 1] - values[i - 1]) / (step * 2f);
                keys[i] = new UnityEngine.Keyframe(i * step, values[i], slope, slope);
            }
            return new AnimationCurve(keys);
        }

        public static float Evaluate(EasingKind kind, EasingDirection direction, float t)
        {
            t = Mathf.Clamp01(t);
            switch (direction)
            {
                case EasingDirection.Out:
                    // Out is In run backwards, which is what the word means and saves writing every
                    // equation twice.
                    return 1f - In(kind, 1f - t);
                case EasingDirection.InOut:
                    return t < 0.5f ? In(kind, t * 2f) * 0.5f : 1f - In(kind, 2f - t * 2f) * 0.5f;
                default:
                    return In(kind, t);
            }
        }

        private static float In(EasingKind kind, float t)
        {
            switch (kind)
            {
                case EasingKind.Sine:
                    return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                case EasingKind.Quadratic:
                    return t * t;
                case EasingKind.Cubic:
                    return t * t * t;
                case EasingKind.Quartic:
                    return t * t * t * t;
                case EasingKind.Quintic:
                    return t * t * t * t * t;
                case EasingKind.Exponential:
                    return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case EasingKind.Circular:
                    return 1f - Mathf.Sqrt(Mathf.Max(1f - t * t, 0f));
                case EasingKind.Back:
                    // The overshoot constants are the ones every implementation of this uses, Blender's
                    // included, so a curve made here matches one made there.
                    const float c1 = 1.70158f;
                    return (c1 + 1f) * t * t * t - c1 * t * t;
                case EasingKind.Bounce:
                    return 1f - BounceOut(1f - t);
                case EasingKind.Elastic:
                    if (t <= 0f)
                        return 0f;
                    if (t >= 1f)
                        return 1f;
                    const float c4 = 2f * Mathf.PI / 3f;
                    return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
                default:
                    return t;
            }
        }

        /// <summary>The four falls of a bouncing ball, each shorter and lower than the last.</summary>
        private static float BounceOut(float t)
        {
            const float n = 7.5625f;
            const float d = 2.75f;
            if (t < 1f / d)
                return n * t * t;
            if (t < 2f / d)
            {
                t -= 1.5f / d;
                return n * t * t + 0.75f;
            }
            if (t < 2.5f / d)
            {
                t -= 2.25f / d;
                return n * t * t + 0.9375f;
            }
            t -= 2.625f / d;
            return n * t * t + 0.984375f;
        }

        /// <summary>Every equation, in the order the menu lists them.</summary>
        public static IEnumerable<EasingKind> All
        {
            get
            {
                yield return EasingKind.Sine;
                yield return EasingKind.Quadratic;
                yield return EasingKind.Cubic;
                yield return EasingKind.Quartic;
                yield return EasingKind.Quintic;
                yield return EasingKind.Exponential;
                yield return EasingKind.Circular;
                yield return EasingKind.Back;
                yield return EasingKind.Bounce;
                yield return EasingKind.Elastic;
            }
        }
    }
}
