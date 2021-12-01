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
            value = other.value;
            parent = other.parent;
            curve = new AnimationCurve(other.curve.keys);
        }
    }
}
