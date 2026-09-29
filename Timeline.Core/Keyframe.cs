using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// What a keyframe is for, which changes nothing about how it plays.
    ///
    /// Blender's keyframe types, and they earn their place for the same reason: on a rig of seventy
    /// tracks the grid is a wall of identical markers, and the only way to find the four poses the
    /// animation is actually built on is to have marked them as you went.
    /// </summary>
    public enum KeyframeKind
    {
        /// <summary>An ordinary keyframe. Everything made before this existed is one.</summary>
        Keyframe,
        /// <summary>A pose between two others, there to shape the transition rather than to be hit.</summary>
        Breakdown,
        /// <summary>A pose held on purpose, so the motion stops rather than drifting through it.</summary>
        MovingHold,
        /// <summary>The furthest point of a motion: the top of the jump, the end of the swing.</summary>
        Extreme,
        /// <summary>Noise laid on top of a motion that was already finished.</summary>
        Jitter
    }

    public class Keyframe
    {
        public object value;
        public readonly Interpolable parent;
        /// <summary>
        /// The easing of the segment leaving this keyframe, normalised in both axes.
        ///
        /// Still here once <see cref="handles"/> exists, and kept in step with them, for two reasons: it
        /// is what a scene is written in so an older Timeline can still read it, and it is what a
        /// keyframe whose shape is not a cubic - a step, or one shaped by hand in the Keyframe window -
        /// goes on being interpolated by.
        /// </summary>
        public AnimationCurve curve;
        /// <summary>
        /// Bezier handles, once this keyframe has them. Null means the easing curve is still in charge,
        /// which is the case for a step and for anything shaped by hand. See <see cref="HandleMath"/>.
        /// </summary>
        public KeyframeHandles handles;
        /// <summary>
        /// True when the segment leaving this keyframe is shaped by <see cref="curve"/> and not by the
        /// handles, because its shape is not a cubic: a step, or one of the sampled easing equations
        /// like Bounce. The handles are kept regardless, because the <i>other</i> side of this keyframe
        /// still belongs to the segment arriving at it, which may well be a curve.
        /// </summary>
        public bool shapedByCurve;
        /// <summary>Marking only: two keyframes of different kinds play exactly the same.</summary>
        public KeyframeKind kind = KeyframeKind.Keyframe;
        /// <summary>The key set this keyframe is in, if any. See <see cref="KeySet"/>.</summary>
        internal KeySet keySet;

        public Keyframe(object value, Interpolable parent, AnimationCurve curve)
        {
            this.value = value;
            this.parent = parent;
            this.curve = curve;
        }

        public Keyframe(Keyframe other) : this(other, other.parent)
        {
        }

        /// <summary>
        /// The same keyframe on a different track: copying into a clip, splitting a track into axes,
        /// pasting onto another object. Everything that shapes it comes along, handles included, or the
        /// copy would quietly play differently from what it was copied from.
        /// </summary>
        public Keyframe(Keyframe other, Interpolable parent)
        {
            value = other.value;
            this.parent = parent;
            curve = new AnimationCurve(other.curve.keys);
            handles = other.handles == null ? null : other.handles.Clone();
            shapedByCurve = other.shapedByCurve;
            kind = other.kind;
            keySet = other.keySet;
        }
    }

}
