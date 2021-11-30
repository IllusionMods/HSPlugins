using System.Collections.Generic;
using UnityEngine;

namespace Timeline
{
    public class Keyframe
    {
        public object value;
        public readonly Interpolable parent;
        public AnimationCurve curve;

        public Keyframe(object value, Interpolable parent, AnimationCurve curve)
        {
            this.value = value;
            this.parent = parent;
            this.curve = curve;
        }

        public Keyframe(Keyframe other)
        {
            this.value = other.value;
            this.parent = other.parent;
            this.curve = new AnimationCurve(other.curve.keys);
        }
    }
}
