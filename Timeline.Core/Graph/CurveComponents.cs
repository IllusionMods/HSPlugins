using UnityEngine;

namespace Timeline.Graph
{
    /// <summary>
    /// Breaks a keyframe value into the float channels a curve editor can draw, and puts an edited
    /// channel back into a value of the original type.
    ///
    /// The type has to survive the round trip: interpolable delegates cast their values, so handing an
    /// int track a float would throw on the next frame. Quaternions are the one deliberate lie, they are
    /// shown and edited as euler angles because three readable curves are far more useful than four
    /// components nobody can reason about.
    /// </summary>
    internal static class CurveComponents
    {
        private static readonly string[] _xyzw = { "X", "Y", "Z", "W" };
        private static readonly string[] _rgba = { "R", "G", "B", "A" };

        /// <summary>Axis colours, the same reading as every 3D application: X red, Y green, Z blue.</summary>
        private static readonly Color[] _axisTints =
        {
            new Color(0.91f, 0.35f, 0.36f),
            new Color(0.51f, 0.80f, 0.39f),
            new Color(0.36f, 0.60f, 0.95f),
            new Color(0.80f, 0.78f, 0.86f)
        };

        /// <summary>Distinct hues for single value tracks, which have no axis to take a colour from.</summary>
        private static readonly Color[] _seriesTints =
        {
            new Color(0.95f, 0.72f, 0.30f),
            new Color(0.45f, 0.83f, 0.78f),
            new Color(0.85f, 0.55f, 0.85f),
            new Color(0.70f, 0.85f, 0.42f),
            new Color(0.95f, 0.55f, 0.45f),
            new Color(0.55f, 0.70f, 0.95f)
        };

        /// <summary>How many curves this value is worth. Zero means it cannot be drawn.</summary>
        public static int Count(object value)
        {
            if (value is float || value is double || value is int || value is bool)
                return 1;
            if (value is Vector2)
                return 2;
            if (value is Vector3 || value is Quaternion)
                return 3;
            if (value is Vector4 || value is Color)
                return 4;
            return 0;
        }

        public static string Name(object value, int index)
        {
            if (value is Color)
                return _rgba[index];
            if (Count(value) == 1)
                return "Value";
            return _xyzw[index];
        }

        public static float Get(object value, int index)
        {
            // A track is not guaranteed to hold one single type from end to end, and indexing a Vector2
            // with the Z of the keyframe before it would throw.
            if (index < 0 || index >= Count(value))
                return 0f;
            if (value is float)
                return (float)value;
            if (value is double)
                return (float)(double)value;
            if (value is int)
                return (int)value;
            if (value is bool)
                return (bool)value ? 1f : 0f;
            if (value is Vector2)
                return ((Vector2)value)[index];
            if (value is Vector3)
                return ((Vector3)value)[index];
            if (value is Vector4)
                return ((Vector4)value)[index];
            if (value is Quaternion)
                return Signed(((Quaternion)value).eulerAngles[index]);
            if (value is Color)
                return ((Color)value)[index];
            return 0f;
        }

        /// <summary>The same value with one channel replaced, still of the original type.</summary>
        public static object With(object value, int index, float component)
        {
            if (index < 0 || index >= Count(value))
                return value;
            if (value is float)
                return component;
            if (value is double)
                return (double)component;
            if (value is int)
                return Mathf.RoundToInt(component);
            if (value is bool)
                return component >= 0.5f;
            if (value is Vector2)
            {
                Vector2 v = (Vector2)value;
                v[index] = component;
                return v;
            }
            if (value is Vector3)
            {
                Vector3 v = (Vector3)value;
                v[index] = component;
                return v;
            }
            if (value is Vector4)
            {
                Vector4 v = (Vector4)value;
                v[index] = component;
                return v;
            }
            if (value is Quaternion)
            {
                Vector3 euler = ((Quaternion)value).eulerAngles;
                euler[index] = component;
                return Quaternion.Euler(euler);
            }
            if (value is Color)
            {
                Color c = (Color)value;
                c[index] = component;
                return c;
            }
            return value;
        }

        /// <summary>
        /// A whole value built from its channels at once, of the same type as the template.
        ///
        /// Not the same as calling <see cref="With"/> per channel: a rotation would then be taken apart
        /// and put back together three times, and euler angles do not survive that cleanly.
        /// </summary>
        public static object FromComponents(object template, float[] numbers)
        {
            int count = Count(template);
            if (count == 0 || numbers == null || numbers.Length < count)
                return template;

            if (template is Quaternion)
                return Quaternion.Euler(numbers[0], numbers[1], numbers[2]);
            if (template is Vector2)
                return new Vector2(numbers[0], numbers[1]);
            if (template is Vector3)
                return new Vector3(numbers[0], numbers[1], numbers[2]);
            if (template is Vector4)
                return new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]);
            if (template is Color)
                return new Color(Mathf.Clamp01(numbers[0]), Mathf.Clamp01(numbers[1]),
                                 Mathf.Clamp01(numbers[2]), Mathf.Clamp01(numbers[3]));
            return With(template, 0, numbers[0]);
        }

        /// <summary>
        /// Colour of one curve. <paramref name="series"/> only matters for single value tracks, where it
        /// keeps two tracks drawn at once from coming out the same colour.
        /// </summary>
        public static Color Tint(object value, int index, int series)
        {
            if (Count(value) == 1)
                return _seriesTints[Mathf.Abs(series) % _seriesTints.Length];
            return _axisTints[Mathf.Clamp(index, 0, _axisTints.Length - 1)];
        }

        /// <summary>Euler angles come back as 0..360, which puts a 360 unit cliff in the middle of a curve.</summary>
        private static float Signed(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
