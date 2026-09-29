using System.Collections.Generic;
using Timeline.Graph;
using Timeline.Shared;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// Thinning a track down to the keyframes that actually carry its shape.
    ///
    /// A recorded take holds one keyframe per frame, most of which say nothing the two around them do not
    /// already say. This is Ramer-Douglas-Peucker on the curve: keep the ends, find the keyframe that
    /// strays furthest from the straight line between them, and if it strays far enough keep it and
    /// recurse on both halves. Everything else goes.
    ///
    /// The distance is measured in value, per component, relative to how far the track travels overall,
    /// so one tolerance means the same thing to a position in metres and a rotation in degrees.
    /// </summary>
    public partial class Timeline
    {
        /// <summary>Fraction of a track's own travel a keyframe has to add to be worth keeping.</summary>
        internal enum SimplifyStrength
        {
            Light,
            Medium,
            Strong
        }

        private static float ToleranceOf(SimplifyStrength strength)
        {
            switch (strength)
            {
                case SimplifyStrength.Light: return 0.005f;
                case SimplifyStrength.Strong: return 0.05f;
                default: return 0.02f;
            }
        }

        /// <summary>
        /// Simplifies whatever is selected: the tracks holding the selected keyframes, or the selected
        /// tracks when no keyframe is. Only the selected part of a track is touched when a selection
        /// exists, so a take can be thinned a section at a time.
        /// </summary>
        private void SimplifySelection(SimplifyStrength strength)
        {
            var scope = new Dictionary<Interpolable, HashSet<Keyframe>>();
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                HashSet<Keyframe> keyframes;
                if (scope.TryGetValue(pair.Value.parent, out keyframes) == false)
                {
                    keyframes = new HashSet<Keyframe>();
                    scope.Add(pair.Value.parent, keyframes);
                }
                keyframes.Add(pair.Value);
            }
            if (scope.Count == 0)
            {
                foreach (Interpolable interpolable in _selectedInterpolables)
                {
                    if (interpolable.keyframes.Count > 2)
                        scope[interpolable] = null; // null means the whole track
                }
            }
            if (scope.Count == 0)
            {
                Logger.LogMessage("Select some keyframes, or the tracks to thin out.");
                return;
            }

            RecordUndo("Simplify");
            int before = 0;
            int after = 0;
            foreach (KeyValuePair<Interpolable, HashSet<Keyframe>> pair in scope)
            {
                before += pair.Key.keyframes.Count;
                SimplifyTrack(pair.Key, pair.Value, ToleranceOf(strength));
                after += pair.Key.keyframes.Count;
            }

            SelectKeyframes(new List<KeyValuePair<float, Keyframe>>());
            UpdateGrid();
            RefreshInterpolation();
            Logger.LogMessage($"Simplified {scope.Count} track(s): {before} keyframes down to {after}.");
        }

        private void SimplifyTrack(Interpolable track, HashSet<Keyframe> within, float tolerance)
        {
            int count = track.keyframes.Count;
            if (count < 3)
                return;

            var times = new List<float>(count);
            var frames = new List<Keyframe>(count);
            for (int i = 0; i < count; ++i)
            {
                times.Add(track.keyframes.Keys[i]);
                frames.Add(track.keyframes.Values[i]);
            }

            object template = frames[0].value;
            int components = CurveComponents.Count(template);
            if (components == 0)
                return; // a pose or an animation has no shape to measure

            // Everything is judged against the track own travel, so one tolerance means the same thing
            // whatever the track holds.
            var numbers = new float[count][];
            for (int i = 0; i < count; ++i)
            {
                numbers[i] = new float[components];
                for (int c = 0; c < components; ++c)
                    numbers[i][c] = CurveComponents.Get(frames[i].value, c);
            }
            float scale = CurveReduction.Travel(numbers);

            bool[] keep = CurveReduction.Keep(times.ToArray(), numbers, tolerance * scale);

            for (int i = count - 1; i >= 0; --i)
            {
                if (keep[i])
                    continue;
                // A selection limits the damage to the part the user pointed at.
                if (within != null && within.Contains(frames[i]) == false)
                    continue;
                track.keyframes.RemoveAt(i);
            }
        }

    }
}
