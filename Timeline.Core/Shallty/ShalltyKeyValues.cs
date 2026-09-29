using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Timeline.Graph;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Timeline
{
    /// <summary>
    /// ShalltyUtils' key value tools, from ShalltyUtils by ShalltyB (github.com/ShalltyB/ShalltyUtils):
    /// one value for every selected key, an amount added to or taken from them all, a type's default
    /// value, a little randomness in value or time, and easing curves saved to files to use again.
    /// </summary>
    public partial class Timeline
    {
        /// <summary>Every selected key of the same kind of value takes this one.</summary>
        internal void SetSelectedValues(object value)
        {
            if (value == null || _selectedKeyframes.Count == 0)
                return;
            RecordUndo("Set values");
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                if (pair.Value.value != null && pair.Value.value.GetType() == value.GetType())
                    pair.Value.value = value;
            }
            RefreshInterpolation();
            UpdateGrid();
        }

        /// <summary>Adds the amounts to each component of every selected key, or takes them away.</summary>
        internal void OffsetSelectedValues(float[] amounts, bool subtract)
        {
            if (_selectedKeyframes.Count == 0)
                return;
            RecordUndo(subtract ? "Subtract from values" : "Add to values");
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                object v = pair.Value.value;
                int count = Mathf.Min(CurveComponents.Count(v), amounts.Length);
                if (v is bool)
                    continue;
                for (int c = 0; c < count; ++c)
                    v = CurveComponents.With(v, c, CurveComponents.Get(v, c) + (subtract ? -amounts[c] : amounts[c]));
                pair.Value.value = v;
            }
            RefreshInterpolation();
            UpdateGrid();
        }

        /// <summary>The value a type starts from: zero, nothing turned, black, off.</summary>
        internal static object DefaultValue(object v)
        {
            if (v is bool) return false;
            if (v is int) return 0;
            if (v is float) return 0f;
            if (v is Vector2) return Vector2.zero;
            if (v is Vector3) return Vector3.zero;
            if (v is Vector4) return Vector4.zero;
            if (v is Quaternion) return Quaternion.identity;
            if (v is Color) return Color.black;
            return null;
        }

        /// <summary>
        /// Moves every selected key's value, or time, by a random amount up to the given one either way,
        /// which roughens a movement that is too perfect.
        /// </summary>
        internal void RandomizeSelectedKeys(float amount, bool values, bool times)
        {
            if (_selectedKeyframes.Count == 0 || amount <= 0f)
                return;
            RecordUndo("Randomize keys");
            var selected = new List<KeyValuePair<float, Keyframe>>(_selectedKeyframes);
            if (times)
            {
                var moved = new List<KeyValuePair<float, Keyframe>>();
                _selectedKeyframes.Clear();
                foreach (KeyValuePair<float, Keyframe> pair in selected)
                {
                    Interpolable track = pair.Value.parent;
                    track.keyframes.Remove(pair.Key);
                    float time = pair.Key;
                    // A free time within the scene; the key stays where it was if none turns up.
                    for (int tries = 0; tries < 50; ++tries)
                    {
                        float candidate = pair.Key + Random.Range(-amount, amount);
                        if (candidate >= 0f && candidate <= _duration && track.keyframes.ContainsKey(candidate) == false)
                        {
                            time = candidate;
                            break;
                        }
                    }
                    track.keyframes.Add(time, pair.Value);
                    moved.Add(new KeyValuePair<float, Keyframe>(time, pair.Value));
                }
                selected = moved;
                SelectKeyframes(moved);
            }
            if (values)
            {
                foreach (KeyValuePair<float, Keyframe> pair in selected)
                    pair.Value.value = RandomizeValue(pair.Value.value, amount);
            }
            RefreshInterpolation();
            UpdateGrid();
            UpdateKeyframeWindow();
        }

        private static object RandomizeValue(object obj, float amount)
        {
            if (obj is Quaternion)
                return Quaternion.Euler(((Quaternion)obj).eulerAngles + new Vector3(Random.Range(-amount, amount), Random.Range(-amount, amount), Random.Range(-amount, amount)));
            if (obj is int)
                return (int)obj + Mathf.RoundToInt(Random.Range(-amount, amount));
            if (obj is Color)
            {
                // Alpha stays: a colour a little off is the point, not one that flickers.
                Color c = (Color)obj;
                return new Color(c.r + Random.Range(-amount, amount), c.g + Random.Range(-amount, amount), c.b + Random.Range(-amount, amount), c.a);
            }
            if (obj is bool)
                return obj;
            int count = CurveComponents.Count(obj);
            for (int i = 0; i < count; ++i)
                obj = CurveComponents.With(obj, i, CurveComponents.Get(obj, i) + Random.Range(-amount, amount));
            return obj;
        }

        #region Curve presets
        internal sealed class CurvePreset
        {
            public string name;
            public string file;
            public AnimationCurve curve;
        }

        private string CurvePresetsFolder
        {
            get { return Path.Combine(_assemblyLocation, Path.Combine(Name, "Curve Presets")); }
        }

        /// <summary>The saved curves: Timeline's own, and those ShalltyUtils saved next to its dll.</summary>
        internal List<CurvePreset> LoadCurvePresets()
        {
            var presets = new List<CurvePreset>();
            var folders = new List<string> { CurvePresetsFolder };
            foreach (string dir in new[] { _assemblyLocation, Path.GetDirectoryName(_assemblyLocation) })
            {
                if (string.IsNullOrEmpty(dir) == false)
                    folders.Add(Path.Combine(dir, Path.Combine("ShalltyUtils", "Curve Presets")));
            }
            foreach (string folder in folders.Distinct())
            {
                if (Directory.Exists(folder) == false)
                    continue;
                foreach (string file in Directory.GetFiles(folder, "*.xml").OrderBy(f => new FileInfo(f).CreationTime))
                {
                    try
                    {
                        var document = new XmlDocument();
                        document.Load(file);
                        var keys = new List<UnityEngine.Keyframe>();
                        foreach (XmlNode node in document.FirstChild.ChildNodes)
                        {
                            if (node.Name != "curveKeyframe")
                                continue;
                            keys.Add(new UnityEngine.Keyframe(
                                    XmlConvert.ToSingle(node.Attributes["time"].Value),
                                    XmlConvert.ToSingle(node.Attributes["value"].Value),
                                    XmlConvert.ToSingle(node.Attributes["inTangent"].Value),
                                    XmlConvert.ToSingle(node.Attributes["outTangent"].Value)));
                        }
                        presets.Add(new CurvePreset
                        {
                            name = Path.GetFileNameWithoutExtension(file),
                            file = file,
                            curve = keys.Count == 0 ? AnimationCurve.Linear(0f, 0f, 1f, 1f) : new AnimationCurve(keys.ToArray())
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Could not load data for Keyframe Curve, exception:" + e.Message);
                    }
                }
            }
            return presets;
        }

        /// <summary>Saves a curve under a name, in the format ShalltyUtils used.</summary>
        internal void SaveCurvePreset(string name, AnimationCurve curve)
        {
            if (string.IsNullOrEmpty(name) || name.Trim().Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Logger.LogMessage("Add a valid name for the curve!");
                return;
            }
            if (Directory.Exists(CurvePresetsFolder) == false)
                Directory.CreateDirectory(CurvePresetsFolder);
            using (var writer = new XmlTextWriter(Path.Combine(CurvePresetsFolder, name.Trim() + ".xml"), Encoding.UTF8))
            {
                writer.WriteStartElement("root");
                foreach (UnityEngine.Keyframe key in curve.keys)
                {
                    writer.WriteStartElement("curveKeyframe");
                    writer.WriteAttributeString("time", XmlConvert.ToString(key.time));
                    writer.WriteAttributeString("value", XmlConvert.ToString(key.value));
                    writer.WriteAttributeString("inTangent", XmlConvert.ToString(key.inTangent));
                    writer.WriteAttributeString("outTangent", XmlConvert.ToString(key.outTangent));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        internal static void DeleteCurvePreset(CurvePreset preset)
        {
            if (File.Exists(preset.file))
                File.Delete(preset.file);
        }
        #endregion
    }
}
