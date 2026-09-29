using UnityEngine;

namespace Timeline.Nla
{
    internal enum StripBlendMode
    {
        /// <summary>Takes over the value entirely, faded in by weight.</summary>
        Replace,
        /// <summary>
        /// Adds the clip's movement away from its own first frame on top of what is underneath.
        ///
        /// This is Blender's Combine rather than its Add: a rotation is composed as a rotation, not
        /// added component by component, which is the whole difference between a layered turn that
        /// works and one that flips halfway through.
        /// </summary>
        Add,
        /// <summary>The same movement taken away instead of put on, for cancelling a layer underneath.</summary>
        Subtract,
        /// <summary>
        /// Scales what is underneath by the clip's own value: a clip of 1.5 makes it half again as much,
        /// whatever it was. Rotations compose, which for them is the same operation.
        /// </summary>
        Multiply
    }

    /// <summary>
    /// Mixing of interpolable values for layered strips.
    ///
    /// Only numbers, vectors, colours and rotations can be mixed. Everything else, animation and pose
    /// among them, is discrete: there is no halfway between two poses, so those fall back to whichever
    /// contribution has the most weight rather than producing nonsense.
    /// </summary>
    internal static class ValueBlend
    {
        public static bool IsBlendable(object value)
        {
            return value is float || value is Vector2 || value is Vector3 || value is Vector4 ||
                   value is Color || value is Quaternion;
        }

        public static object Lerp(object from, object to, float factor)
        {
            if (from is float)
                return Mathf.LerpUnclamped((float)from, (float)to, factor);
            if (from is Vector2)
                return Vector2.LerpUnclamped((Vector2)from, (Vector2)to, factor);
            if (from is Vector3)
                return Vector3.LerpUnclamped((Vector3)from, (Vector3)to, factor);
            if (from is Vector4)
                return Vector4.LerpUnclamped((Vector4)from, (Vector4)to, factor);
            if (from is Color)
                return Color.LerpUnclamped((Color)from, (Color)to, factor);
            if (from is Quaternion)
                return Quaternion.SlerpUnclamped((Quaternion)from, (Quaternion)to, factor);
            return from;
        }

        /// <summary>
        /// Mixes one strip's contribution into what the lanes below produced.
        /// <paramref name="reference"/> is the clip's value at its own first frame, which is what makes
        /// Add meaningful: a clip authored from a neutral pose contributes its movement, not its position.
        /// </summary>
        public static object Blend(object under, object over, object reference, float weight, StripBlendMode mode)
        {
            if (weight <= 0f)
                return under;
            if (mode == StripBlendMode.Replace)
                return Lerp(under, over, Mathf.Clamp01(weight));

            object mixed;
            switch (mode)
            {
                case StripBlendMode.Subtract:
                    mixed = Add(under, reference, over); // the same delta, the other way round
                    break;
                case StripBlendMode.Multiply:
                    mixed = Multiply(under, over);
                    break;
                default:
                    mixed = Add(under, over, reference);
                    break;
            }
            // Every mode fades the same way: weight only ever says how much of the mixed result to take,
            // so a fade in and an influence below one mean what they say whichever mode is picked.
            return Lerp(under, mixed, Mathf.Clamp01(weight));
        }

        /// <summary>
        /// What is underneath, scaled by the clip.
        ///
        /// A colour's alpha and a rotation are the two that do not scale component by component: alpha
        /// multiplies like the rest, but a rotation is scaled by composing it, which is what multiplying
        /// two quaternions already means.
        /// </summary>
        private static object Multiply(object under, object over)
        {
            if (under is float)
                return (float)under * (float)over;
            if (under is Vector2)
                return Vector2.Scale((Vector2)under, (Vector2)over);
            if (under is Vector3)
                return Vector3.Scale((Vector3)under, (Vector3)over);
            if (under is Vector4)
                return Vector4.Scale((Vector4)under, (Vector4)over);
            if (under is Color)
                return (Color)under * (Color)over;
            if (under is Quaternion)
                return (Quaternion)under * (Quaternion)over;
            return over;
        }

        public static object Add(object under, object over, object reference)
        {
            if (under is float)
                return (float)under + ((float)over - (float)reference);
            if (under is Vector2)
                return (Vector2)under + ((Vector2)over - (Vector2)reference);
            if (under is Vector3)
                return (Vector3)under + ((Vector3)over - (Vector3)reference);
            if (under is Vector4)
                return (Vector4)under + ((Vector4)over - (Vector4)reference);
            if (under is Color)
                return (Color)under + ((Color)over - (Color)reference);
            if (under is Quaternion)
            {
                // The delta rotation the clip travels away from its first frame, applied on top.
                Quaternion delta = Quaternion.Inverse((Quaternion)reference) * (Quaternion)over;
                return (Quaternion)under * delta;
            }
            return over;
        }
    }
}
