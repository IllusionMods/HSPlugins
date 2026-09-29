using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Timeline.Graph;
using BepInEx.Configuration;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// Edits and settings the window asks for, which used to live next to the widgets of the old
    /// window that called them: moving the playhead, keying, key types, what a track does past its
    /// ends, single files, and the theme's settings.
    /// </summary>
    public partial class Timeline
    {
        #region Playhead

        /// <summary>
        /// Moves the playhead to the next or previous key on the selected tracks, or on every track when
        /// none is selected. Stays put at the last one rather than wrapping.
        /// </summary>
        private void JumpToKey(int direction)
        {
            IEnumerable<Interpolable> tracks = _selectedInterpolables.Count != 0
                    ? (IEnumerable<Interpolable>)_selectedInterpolables
                    : _interpolables.Values.Where(t => t.enabled);
            const float epsilon = 0.0005f;
            float best = direction > 0 ? float.MaxValue : float.MinValue;
            bool found = false;
            foreach (Interpolable track in tracks)
            {
                foreach (float time in track.keyframes.Keys)
                {
                    if (direction > 0 && time > _playbackTime + epsilon && time < best ||
                        direction < 0 && time < _playbackTime - epsilon && time > best)
                    {
                        best = time;
                        found = true;
                    }
                }
            }
            if (found == false)
                return;
            isPlaying = false;
            SeekPlaybackTime(best);
        }
        #endregion

        #region Keys and tracks
        /// <summary>The Key button: a key on every selected track, at the playhead, holding what the scene shows now.</summary>
        private void KeySelectedTracks()
        {
            if (_selectedInterpolables.Count == 0)
            {
                // Nothing picked in the list, but a node in the scene: key that, as the button says.
                if (SelectedNode() != null)
                {
                    KeyNode(-1, -1);
                    return;
                }
                Logger.LogMessage("Select the tracks to key first: click them in the channel list.");
                return;
            }
            foreach (Interpolable interpolable in new List<Interpolable>(_selectedInterpolables))
                AddKeyframe(interpolable, _playbackTime);
            UpdateGrid();
        }

        /// <summary>Marks the selected keyframes as a kind: marking only, they play the same.</summary>
        private void SetKeyframeKind(KeyframeKind kind)
        {
            if (_selectedKeyframes.Count == 0)
                return;
            RecordUndo("Keyframe type");
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                pair.Value.kind = kind;
            UpdateGrid();
        }

        /// <summary>What the tracks do before their first key and after their last.</summary>
        private void SetExtrapolation(List<Interpolable> tracks, TrackExtrapolation mode)
        {
            if (tracks.Count == 0)
                return;
            RecordUndo("Outside the keys");
            foreach (Interpolable track in tracks)
                track.extrapolation = mode;
            RefreshInterpolation();
        }

        private bool IsComponentHidden(Interpolable track, int component)
        {
            int mask;
            return _hiddenComponents.TryGetValue(track, out mask) && (mask & (1 << component)) != 0;
        }

        /// <summary>Shows or hides one axis of a track in the Graph.</summary>
        private void ToggleComponent(Interpolable track, int component)
        {
            int mask;
            _hiddenComponents.TryGetValue(track, out mask);
            mask ^= 1 << component;

            // Every axis off means the track itself is hidden, which is what the eye would have said.
            // Letting the two disagree would leave a row claiming to show something it does not.
            int components = track.keyframes.Count == 0 ? 0 : Graph.CurveComponents.Count(track.keyframes.Values[0].value);
            int all = components <= 0 ? 0 : (1 << components) - 1;
            if (all != 0 && (mask & all) == all)
            {
                _graphHiddenTracks.Add(track);
                mask = 0;
            }
            else
                _graphHiddenTracks.Remove(track);

            if (mask == 0)
                _hiddenComponents.Remove(track);
            else
                _hiddenComponents[track] = mask;
            UpdateInterpolablesView();
        }

        private void ExpandAllGroups(bool expanded)
        {
            ExpandGroups(_interpolablesTree.tree, expanded);
            UpdateInterpolablesView();
        }

        private static void ExpandGroups(List<INode> nodes, bool expanded)
        {
            foreach (INode node in nodes)
            {
                if (node.type != INodeType.Group)
                    continue;
                GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)node;
                group.obj.expanded = expanded;
                ExpandGroups(group.children, expanded);
            }
        }
        #endregion

        #region Handles and hidden axes
        /// <summary>
        /// Per track, a bit set for each hidden component. A track with no entry shows everything, so
        /// the common case costs nothing and scenes that never touch this behave exactly as before.
        /// </summary>
        private readonly Dictionary<Interpolable, int> _hiddenComponents = new Dictionary<Interpolable, int>();


        /// <summary>
        /// Writes one handle, and answers for the other one.
        ///
        /// Moving an automatic handle is what makes it yours, so it becomes Free. Aligned is the one
        /// type that has something to say about its opposite number: it keeps the two pointing exactly
        /// away from each other, so the curve runs straight through the keyframe, while letting each
        /// side keep its own reach.
        /// </summary>
        private void SetHandle(Keyframe keyframe, int component, bool outgoing, Vector2 offset)
        {
            KeyframeHandles handles = keyframe.handles;
            HandleType type = outgoing ? handles.rightType : handles.leftType;
            if (type != HandleType.Aligned)
            {
                type = HandleType.Free;
                if (outgoing)
                    handles.rightType = type;
                else
                    handles.leftType = type;
            }

            Vector2[] side = outgoing ? handles.right : handles.left;
            if (component < side.Length)
                side[component] = offset;

            if (type != HandleType.Aligned)
                return;

            Vector2[] opposite = outgoing ? handles.left : handles.right;
            if (component >= opposite.Length)
                return;
            // The opposite side keeps whatever length it had and is turned to face the other way. A
            // handle with no length yet gets a third of this one's, so there is something to see.
            float length = opposite[component].magnitude;
            if (length < 0.000001f)
                length = offset.magnitude / 3f;
            Vector2 direction = offset.sqrMagnitude < 0.000001f ? Vector2.zero : -offset.normalized;
            opposite[component] = direction * length;
            if (outgoing)
                handles.leftType = HandleType.Aligned;
            else
                handles.rightType = HandleType.Aligned;
        }


        private void SetSelectedHandleType(HandleType type)
        {
            if (_selectedKeyframes.Count == 0)
            {
                Logger.LogMessage("Select some keyframes first.");
                return;
            }

            RecordUndo("Handle type");
            int changed = 0;
            int skipped = 0;
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                Interpolable track = pair.Value.parent;
                HandleMath.Convert(track.keyframes);
                int index = track.keyframes.IndexOfValue(pair.Value);
                if (index < 0 || pair.Value.handles == null)
                {
                    // A step, or a curve somebody shaped by hand in the Keyframe window. Neither is a
                    // cubic, so neither has handles to give a type to.
                    ++skipped;
                    continue;
                }
                MaterialiseHandle(track.keyframes, index, true, type);
                MaterialiseHandle(track.keyframes, index, false, type);
                ++changed;
            }

            RefreshInterpolation();
            UpdateGrid();
            Logger.LogMessage($"Set {changed} keyframe(s)" +
                              (skipped == 0 ? "." : $", skipped {skipped} whose shape is not a curve (a step, or one shaped by hand)."));
        }


        /// <summary>
        /// Changes a handle's type, keeping the curve where it is.
        ///
        /// Going to Free or Aligned writes down where the handle was pointing a moment ago, so switching
        /// away from automatic is not also a change of shape: you get the shape you were looking at, and
        /// then it is yours to move. The other three do not store anything, so there is nothing to keep.
        /// </summary>
        private static void MaterialiseHandle(SortedList<float, Keyframe> keyframes, int index, bool outgoing, HandleType type)
        {
            Keyframe keyframe = keyframes.Values[index];
            if (keyframe.handles == null)
                return;

            if (type == HandleType.Free || type == HandleType.Aligned)
            {
                Vector2[] side = outgoing ? keyframe.handles.right : keyframe.handles.left;
                for (int c = 0; c < side.Length; ++c)
                {
                    Vector2 left;
                    Vector2 right;
                    HandleMath.Offsets(keyframes, index, c, out left, out right);
                    side[c] = outgoing ? right : left;
                }
            }

            if (outgoing)
                keyframe.handles.rightType = type;
            else
                keyframe.handles.leftType = type;
        }

        #endregion

        #region Sampling for the Graph
        /// <summary>One component of a track at any time, the part past its own keyframes included.</summary>
        private static float SampleOutside(Interpolable interpolable, int component, float time)
        {
            IList<float> times = interpolable.keyframes.Keys;
            int cycles;
            float trackTime = TrackCycle.Wrap(interpolable.keyframes, interpolable.extrapolation, time, out cycles);

            Keyframe linearFrom;
            Keyframe linearTo;
            float linearFactor;
            if (TrackCycle.TryLinear(interpolable.keyframes, interpolable.extrapolation, trackTime,
                                     out linearFrom, out linearTo, out linearFactor))
            {
                return Mathf.LerpUnclamped(CurveComponents.Get(linearFrom.value, component),
                                           CurveComponents.Get(linearTo.value, component), linearFactor);
            }

            int leftIndex = 0;
            for (int i = 0; i < times.Count; ++i)
            {
                if (times[i] > trackTime)
                    break;
                leftIndex = i;
            }

            float value = SampleComponent(interpolable, leftIndex, trackTime, component);
            if (cycles != 0 && interpolable.extrapolation == TrackExtrapolation.CyclicOffset)
            {
                IList<Keyframe> frames = interpolable.keyframes.Values;
                float travel = CurveComponents.Get(frames[frames.Count - 1].value, component) -
                               CurveComponents.Get(frames[0].value, component);
                value += travel * cycles;
            }
            return value;
        }

        private static float SampleComponent(Interpolable interpolable, int leftIndex, float time, int component)
        {
            IList<float> times = interpolable.keyframes.Keys;
            IList<Keyframe> frames = interpolable.keyframes.Values;
            Keyframe left = frames[leftIndex];
            if (leftIndex + 1 >= frames.Count)
                return CurveComponents.Get(left.value, component);

            Keyframe right = frames[leftIndex + 1];
            object smoothed;
            if (interpolable.smooth && KeyframeSpline.TryEvaluate(interpolable.keyframes, leftIndex, time, out smoothed))
                return CurveComponents.Get(smoothed, component);

            if (HandleMath.HasHandles(left, right))
            {
                if (HandleMath.IsFactorSpace(left.value) == false)
                    return HandleMath.Evaluate(interpolable.keyframes, leftIndex, component, time);
                // A rotation is still slerped, so the curve drawn per axis is the euler of that slerp
                // rather than three curves of its own.
                float shapedFactor = HandleMath.Evaluate(interpolable.keyframes, leftIndex, 0, time);
                return CurveComponents.Get(
                        Quaternion.SlerpUnclamped((Quaternion)left.value, (Quaternion)right.value, shapedFactor), component);
            }

            float span = times[leftIndex + 1] - times[leftIndex];
            float normalized = span <= 0f ? 0f : (time - times[leftIndex]) / span;
            float factor = left.curve.Evaluate(normalized);
            if (left.value is Quaternion && right.value is Quaternion)
            {
                // Lerping euler angles would cut corners the rotation never takes.
                Quaternion rotation = Quaternion.SlerpUnclamped((Quaternion)left.value, (Quaternion)right.value, factor);
                return CurveComponents.Get(rotation, component);
            }
            return Mathf.LerpUnclamped(CurveComponents.Get(left.value, component),
                                       CurveComponents.Get(right.value, component), factor);
        }

        #endregion

        #region Single files
        /// <summary>Loads a saved timeline, by its name in the Timeline folder, onto the selected object.</summary>
        private void LoadSingleFile(string name)
        {
            try
            {
                if (_selectedOCI == null)
                {
                    Logger.LogMessage("Can't load: No studio object is selected. This function loads timeline data for a single studio object.");
                    return;
                }
                string path = Path.Combine(_singleFilesFolder, name + ".xml");
                if (File.Exists(path))
                {
                    LoadSingle(path);
                    Logger.LogMessage("File was loaded successfully.");
                }
                else
                    Logger.LogMessage("Can't load: No file selected or the file no longer exists.");
            }
            catch (Exception e)
            {
                Logger.LogMessage("Can't load: " + e.Message);
                Logger.LogError(e);
            }
        }

        /// <summary>
        /// Saves the selected object's timeline under a name in the Timeline folder, or only the selected
        /// tracks, whatever objects they are on (as ShalltyUtils' interpolables files did). Either loads
        /// onto the selected object.
        /// </summary>
        private void SaveSingleFile(string name, bool onlySelectedTracks = false)
        {
            try
            {
                if (onlySelectedTracks && _selectedInterpolables.Count == 0)
                {
                    Logger.LogMessage("Can't save: select at least one track first.");
                    return;
                }
                if (onlySelectedTracks == false && _selectedOCI == null)
                {
                    Logger.LogMessage("Can't save: No studio object is selected. This function saves timeline data for a single studio object.");
                    return;
                }
                string selected = name == null ? null : name.Trim();
                if (string.IsNullOrEmpty(selected) || selected.Intersect(Path.GetInvalidFileNameChars()).Any())
                {
                    Logger.LogMessage("Can't save: Provided name is empty or contains invalid characters.");
                    return;
                }
                if (Directory.Exists(_singleFilesFolder) == false)
                    Directory.CreateDirectory(_singleFilesFolder);
                SaveSingle(Path.Combine(_singleFilesFolder, selected + ".xml"), onlySelectedTracks);
                Logger.LogMessage("File was saved successfully.");
            }
            catch (Exception e)
            {
                Logger.LogMessage("Can't save: " + e.Message);
                Logger.LogError(e);
            }
        }
        #endregion

        #region Theme settings
        internal static readonly Color _defaultBackground = new Color32(0x2B, 0x2B, 0x2B, 0xFF);
        internal static readonly Color _defaultAccent = new Color32(0x4D, 0x7F, 0xC4, 0xFF);
        internal static readonly Color _defaultText = new Color32(0xE6, 0xE6, 0xE6, 0xFF);
        internal static readonly Color _defaultPlayhead = new Color32(0x4D, 0x7F, 0xC4, 0xFF);

        /// <summary>
        /// The presets the Theme window offers, as background, accent, text and playhead. The first is
        /// what a new install starts with.
        /// </summary>
        internal static readonly KeyValuePair<string, Color[]>[] _themePresets =
        {
            ThemePreset("Default", 0x2B2B2B, 0x4D7FC4, 0xE6E6E6, 0x4D7FC4),
            ThemePreset("Amber", 0x21242A, 0xE8A33D, 0xE4E7EC, 0xE8483C),
            ThemePreset("Midnight", 0x1A2130, 0x5CC8FF, 0xDCE6F5, 0xFF6B6B),
            ThemePreset("Forest", 0x1F2622, 0x8BD17C, 0xE2EADF, 0xE8683C),
            ThemePreset("Rose", 0x2A2127, 0xF08AB8, 0xF0E4EA, 0xFFB347),
            ThemePreset("Light", 0xE4E6EA, 0xC27410, 0x1F2328, 0xD63B2F)
        };

        private static KeyValuePair<string, Color[]> ThemePreset(string name, int background, int accent, int text, int playhead)
        {
            return new KeyValuePair<string, Color[]>(name, new[] { HexColor(background), HexColor(accent), HexColor(text), HexColor(playhead) });
        }

        private static Color HexColor(int rgb)
        {
            return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        }

        internal static ConfigEntry<bool> ConfigUIOpacityAway { get; private set; }
        internal static ConfigEntry<bool> ConfigUIScaleAuto { get; private set; }
        /// <summary>Set while the four colours change together, so the window is rebuilt once, not four times.</summary>
        private bool _themeBatch;

        private static void BindThemeConfig(ConfigFile config, string themeSection)
        {
            ConfigUIOpacityAway = config.Bind(themeSection, "Opacity Only When Away", false,
                    "Keep the windows solid while the mouse is over them, and only let the scene show through when it is elsewhere.");
            ConfigUIScaleAuto = config.Bind("Config", "Interface Scale Follows Screen", true,
                    "Grows the interface with the screen resolution, so it is the same size on a 4K screen as on a 1080p one. Interface Scale multiplies on top.");
        }

        /// <summary>
        /// The scale the canvas actually uses. Following the screen only ever grows the interface: below
        /// 1080 lines it stays at the picked size, because shrinking it any further makes the text too
        /// small to read.
        /// </summary>
        internal static float EffectiveUIScale()
        {
            float scale = Mathf.Clamp(ConfigUIScale.Value, 0.5f, 2f);
            if (ConfigUIScaleAuto != null && ConfigUIScaleAuto.Value)
                scale *= Mathf.Max(1f, Screen.height / 1080f);
            return Mathf.Clamp(scale, 0.5f, 4f);
        }

        private void ApplyThemeColors(Color[] colors)
        {
            _themeBatch = true;
            try
            {
                ConfigBackgroundColor.Value = colors[0];
                ConfigAccentColor.Value = colors[1];
                ConfigTextColor.Value = colors[2];
                ConfigPlayheadColor.Value = colors[3];
            }
            finally
            {
                _themeBatch = false;
            }
            ApplyTimelineTheme();
        }

        /// <summary>The window draws everything from the four colours, so a new theme is a rebuild.</summary>
        private void ApplyTimelineTheme()
        {
            if (_view != null)
                _view.Rebuild();
        }
        #endregion
    }
}
