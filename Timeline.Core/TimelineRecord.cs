using System;
using System.Collections.Generic;
using Timeline.Graph;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    /// <summary>
    /// Record: arm it, move things, and the movement becomes keyframes.
    ///
    /// Paused it is auto keying - whatever you change gets a keyframe at the playhead. Playing it is a
    /// recorder - the movement is sampled onto the framerate grid as it happens, so dragging a hand
    /// through a take writes the take.
    ///
    /// The whole trick is telling a value the user changed from a value the timeline just wrote. Rather
    /// than hooking the gizmo, every watched track's value is remembered right after each interpolation
    /// pass; anything that differs by the time the next frame ends was changed by someone else, which in
    /// a Studio frame means the user. Guide objects help here: their tracks read the offset the user
    /// applied, not the pose an animation is playing, so a running animation does not look like input.
    /// </summary>
    public partial class Timeline
    {
        #region Private variables
        /// <summary>The grid is rebuilt at most this often while recording; it is not cheap.</summary>
        private const float _recordRefreshInterval = 0.25f;

        private bool _recording;
        private bool _recordedSomething;
        private bool _recordDirty;
        private float _recordLastRefresh;
        private float _recordLastWatchRebuild;
        private readonly List<Interpolable> _recordWatched = new List<Interpolable>();
        private readonly Dictionary<Interpolable, object> _recordBaseline = new Dictionary<Interpolable, object>();
        #endregion

        #region Setup

        private void SetRecording(bool on)
        {
            _recording = on;
            _recordedSomething = false;
            _recordDirty = false;
            RebuildRecordWatchList();
            RefreshRecordBaseline();
            if (on == false)
                return;

            // Saying how many tracks are armed is the difference between "it is not working" and "there
            // was nothing to record", which look identical from the outside.
            if (_recordWatched.Count == 0)
            {
                Logger.LogMessage("Recording, but no track is listening. Add tracks for the object first: " +
                                  "right click the empty part of the channel list, Add tracks for the rig.");
                return;
            }
            Logger.LogMessage(_recordWatched.Count + (_isPlaying
                    ? " track(s) armed. Move something while it plays and the movement is keyframed."
                    : " track(s) armed. Move something and it gets a keyframe at the playhead."));
        }

        /// <summary>
        /// What recording watches: the selected object's tracks, plus any track selected by hand, so a
        /// track on something else can be recorded deliberately.
        /// </summary>
        private void RebuildRecordWatchList()
        {
            _recordWatched.Clear();
            if (_recording == false)
                return;
            // The selected object's tracks and the ones that belong to no object (camera, time), plus
            // anything selected by hand.
            foreach (Interpolable interpolable in _interpolables.Values)
            {
                if (interpolable.oci == null || interpolable.oci == _selectedOCI)
                    _recordWatched.Add(interpolable);
            }
            foreach (Interpolable interpolable in _selectedInterpolables)
            {
                if (_recordWatched.Contains(interpolable) == false)
                    _recordWatched.Add(interpolable);
            }
            _recordLastWatchRebuild = Time.time;
        }

        /// <summary>
        /// Takes the values as read right now to be the reference. Called after every interpolation pass,
        /// which is the moment the scene holds what the timeline says rather than what anyone typed.
        /// </summary>
        private void RefreshRecordBaseline()
        {
            if (_recording == false)
                return;
            foreach (Interpolable track in _recordWatched)
            {
                object value = RecordValue(track);
                if (value != null)
                    _recordBaseline[track] = value;
            }
        }
        #endregion

        #region Sampling
        /// <summary>Runs once a frame, after the user has had their turn and before the timeline writes again.</summary>
        private void RecordSample()
        {
            if (_recording == false)
                return;
            if (Time.time - _recordLastWatchRebuild > 1f)
                RebuildRecordWatchList();

            float time = SnapToFrame(_playbackTime);
            foreach (Interpolable track in _recordWatched)
            {
                object current = RecordValue(track);
                if (current == null)
                    continue;
                object baseline;
                if (_recordBaseline.TryGetValue(track, out baseline) && SameRecordedValue(baseline, current))
                    continue;

                if (_recordedSomething == false)
                {
                    // One undo step for the whole take rather than one per frame.
                    RecordUndo("Record");
                    _recordedSomething = true;
                }
                WriteRecordedKeyframe(track, time, current);
                _recordBaseline[track] = current;
                _recordDirty = true;
            }

            // Redrawing the grid for every frame of a take would cost more than the recording does. Auto
            // keying is one keyframe at a time though, and waiting a quarter second to see it appear
            // reads as nothing having happened.
            if (_recordDirty && (_isPlaying == false || Time.time - _recordLastRefresh > _recordRefreshInterval))
            {
                _recordDirty = false;
                _recordLastRefresh = Time.time;
                UpdateGrid();
            }
        }

        private void WriteRecordedKeyframe(Interpolable track, float time, object value)
        {
            float tolerance = 0.5f / Mathf.Max(_desiredFrameRate, 1);
            for (int i = 0; i < track.keyframes.Count; ++i)
            {
                if (Mathf.Abs(track.keyframes.Keys[i] - time) >= tolerance)
                    continue;
                track.keyframes.Values[i].value = value; // that slot is taken, so this is a correction
                return;
            }

            // A new keyframe inherits the shape of the one before it, the same as adding one by hand.
            AnimationCurve curve = null;
            for (int i = track.keyframes.Count - 1; i >= 0; --i)
            {
                if (track.keyframes.Keys[i] < time)
                {
                    curve = new AnimationCurve(track.keyframes.Values[i].curve.keys);
                    break;
                }
            }
            track.keyframes.Add(time, new Keyframe(value, track, curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f)));
            // A recorded take is dense enough that its handles barely matter, but it still has to speak
            // the same language as everything else: half a track in handles and half in easing curves is
            // a track the graph cannot draw one shape for.
            HandleMath.Convert(track.keyframes);
        }

        /// <summary>Times land on the framerate grid, which is what keeps a take from holding 60 keyframes a second at random.</summary>
        private float SnapToFrame(float time)
        {
            int rate = Mathf.Max(_desiredFrameRate, 1);
            return Mathf.Clamp(Mathf.Round(time * rate) / rate, 0f, _duration);
        }

        private object RecordValue(Interpolable track)
        {
            try
            {
                return track.GetValue();
            }
            catch
            {
                return null; // a broken track reports itself elsewhere; recording just leaves it alone
            }
        }

        /// <summary>
        /// Whether two readings of a track are the same. Numbers get a tolerance, because a transform
        /// read back is never bit for bit what was written into it.
        /// </summary>
        private static bool SameRecordedValue(object left, object right)
        {
            if (left == null || right == null)
                return ReferenceEquals(left, right);
            int components = CurveComponents.Count(left);
            if (components == 0 || CurveComponents.Count(right) != components)
                return left.Equals(right);

            for (int i = 0; i < components; ++i)
            {
                float a = CurveComponents.Get(left, i);
                float b = CurveComponents.Get(right, i);
                if (Mathf.Abs(a - b) > 0.0005f * Mathf.Max(1f, Mathf.Abs(a)))
                    return false;
            }
            return true;
        }
        #endregion
    }
}
