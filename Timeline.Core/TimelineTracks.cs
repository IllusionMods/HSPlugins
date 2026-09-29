using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    /// <summary>
    /// Two things the channel list could not do: listen to one track on its own, and move a track's
    /// animation onto another track.
    /// </summary>
    public partial class Timeline
    {
        /// <summary>
        /// A track's name, without trusting it to survive being asked for.
        ///
        /// The name of a guide object track is built by reaching through to the transform it drives, and
        /// when that transform has been destroyed - an object deleted, a character swapped - the getter
        /// throws. Asking for it from anything that runs every frame then takes the rest of that frame
        /// with it, which is how a dead track can stop the interface, the playback and the recorder all
        /// at once.
        /// </summary>
        internal static string TrackName(Interpolable track)
        {
            if (track == null)
                return "";
            try
            {
                return track.name;
            }
            catch
            {
                return track.owner + "/" + track.id;
            }
        }

        #region Solo
        /// <summary>While anything is in here, nothing else plays. Empty means everything plays.</summary>
        private readonly HashSet<Interpolable> _soloInterpolables = new HashSet<Interpolable>();

        /// <summary>
        /// Whether a track is silenced because something else is soloed.
        ///
        /// Disabling the other seventy tracks to hear one of them is the alternative, and putting them all
        /// back afterwards is worse. Solo is a filter, so nothing about the tracks themselves changes.
        /// </summary>
        private bool IsMutedBySolo(Interpolable interpolable)
        {
            return _soloInterpolables.Count != 0 && _soloInterpolables.Contains(interpolable) == false;
        }

        private void SoloSelectedTracks()
        {
            _soloInterpolables.Clear();
            foreach (Interpolable interpolable in _selectedInterpolables)
                _soloInterpolables.Add(interpolable);
            AfterSoloChanged();
        }

        private void ClearSolo()
        {
            _soloInterpolables.Clear();
            AfterSoloChanged();
        }

        private void AfterSoloChanged()
        {
            UpdateInterpolablesView();
            RefreshInterpolation();
            Logger.LogMessage(_soloInterpolables.Count == 0
                    ? "Solo off, every track plays again."
                    : _soloInterpolables.Count + " track(s) soloed, the rest are silent until you clear it.");
        }
        #endregion

        #region Track clipboard
        private sealed class CopiedTrack
        {
            public string label;
            public readonly List<float> times = new List<float>();
            public readonly List<object> values = new List<object>();
            public readonly List<AnimationCurve> curves = new List<AnimationCurve>();
            public readonly List<KeyframeKind> kinds = new List<KeyframeKind>();
            public object template;
            public bool smooth;
        }

        private readonly List<CopiedTrack> _copiedTracks = new List<CopiedTrack>();

        private void CopySelectedTracks()
        {
            _copiedTracks.Clear();
            foreach (Interpolable interpolable in _selectedInterpolables)
            {
                if (interpolable.keyframes.Count == 0)
                    continue;
                CopiedTrack copy = new CopiedTrack
                {
                    label = DisplayName(interpolable),
                    template = interpolable.keyframes.Values[0].value,
                    smooth = interpolable.smooth
                };
                for (int i = 0; i < interpolable.keyframes.Count; ++i)
                {
                    copy.times.Add(interpolable.keyframes.Keys[i]);
                    copy.values.Add(interpolable.keyframes.Values[i].value);
                    copy.curves.Add(new AnimationCurve(interpolable.keyframes.Values[i].curve.keys));
                    copy.kinds.Add(interpolable.keyframes.Values[i].kind);
                }
                _copiedTracks.Add(copy);
            }
            Logger.LogMessage(_copiedTracks.Count == 0
                    ? "Nothing copied: those tracks have no keyframes."
                    : "Copied " + _copiedTracks.Count + " track(s).");
        }

        /// <summary>
        /// Pastes the copied tracks onto the selected ones, pair by pair in the order they were picked.
        /// One copied track pastes onto all of them, which is how the same motion lands on a row of
        /// objects; anything holding a different kind of value is skipped rather than broken.
        /// </summary>
        private void PasteIntoSelectedTracks()
        {
            if (_copiedTracks.Count == 0 || _selectedInterpolables.Count == 0)
            {
                Logger.LogMessage("Copy a track first, then select the ones to paste it into.");
                return;
            }

            RecordUndo("Paste tracks");
            int pasted = 0;
            int skipped = 0;
            for (int i = 0; i < _selectedInterpolables.Count; ++i)
            {
                Interpolable target = _selectedInterpolables[i];
                CopiedTrack source = _copiedTracks.Count == 1 ? _copiedTracks[0] : (i < _copiedTracks.Count ? _copiedTracks[i] : null);
                if (source == null)
                    break;

                object current = target.keyframes.Count != 0 ? target.keyframes.Values[0].value : SafeValue(target);
                if (current != null && source.template != null && current.GetType() != source.template.GetType())
                {
                    ++skipped;
                    continue;
                }

                target.keyframes.Clear();
                for (int k = 0; k < source.times.Count; ++k)
                    target.keyframes.Add(source.times[k], new Keyframe(source.values[k], target, new AnimationCurve(source.curves[k].keys)) { kind = source.kinds[k] });
                target.smooth = source.smooth;
                HandleMath.Convert(target.keyframes);
                ++pasted;
            }

            UpdateGrid();
            RefreshInterpolation();
            Logger.LogMessage($"Pasted into {pasted} track(s)" + (skipped == 0 ? "." : ", skipped " + skipped + " that hold a different kind of value."));
        }
        #endregion

        /// <summary>A track's current value, or null when the thing it drives cannot be read right now.</summary>
        private object SafeValue(Interpolable target)
        {
            try
            {
                return target.GetValue();
            }
            catch (System.Exception e)
            {
                Logger.LogWarning("Could not read " + target.id + ": " + e.Message);
                return null;
            }
        }
    }
}
