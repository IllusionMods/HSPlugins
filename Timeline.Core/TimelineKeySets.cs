using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ToolBox.Extensions;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// A named, coloured set of keyframes, to select the same keys again later: the keys of a gesture,
    /// the peaks of a wave. A keyframe is in one set at most, and carries it itself, so moving it,
    /// copying it into a clip or splitting its track keeps it in the set.
    /// </summary>
    internal sealed class KeySet
    {
        public string name;
        public Color color;
    }

    /// <summary>
    /// The sets of a scene, and how they are saved: each keyframe writes the index of its set, and the
    /// scene writes the list of sets once. A scene made with ShalltyUtils brings its Keyframe Groups in
    /// as sets.
    /// </summary>
    public partial class Timeline
    {
        internal static readonly Color[] _keySetColors =
        {
            new Color32(0x5C, 0x99, 0xF2, 255), new Color32(0xE8, 0x6B, 0xAE, 255), new Color32(0x82, 0xCC, 0x63, 255),
            new Color32(0xE8, 0xC3, 0x4A, 255), new Color32(0x9C, 0x7B, 0xE0, 255), new Color32(0x4F, 0xC3, 0xD9, 255),
            new Color32(0xE8, 0x5A, 0x5C, 255)
        };

        internal readonly List<KeySet> _keySets = new List<KeySet>();
        /// <summary>The sets of the file being read, by the index its keyframes carry. Null outside a read.</summary>
        private List<KeySet> _keySetRead;

        /// <summary>Every keyframe of the scene in a set, the set's own tracks only.</summary>
        internal List<KeyValuePair<float, Keyframe>> KeysIn(KeySet set)
        {
            var keys = new List<KeyValuePair<float, Keyframe>>();
            foreach (Interpolable track in _interpolables.Values)
            {
                foreach (KeyValuePair<float, Keyframe> pair in track.keyframes)
                {
                    if (pair.Value.keySet == set)
                        keys.Add(pair);
                }
            }
            return keys;
        }

        internal KeySet NewKeySet(IEnumerable<Keyframe> keys)
        {
            RecordUndo("New key set");
            var set = new KeySet { name = "Set " + (_keySets.Count + 1), color = _keySetColors[_keySets.Count % _keySetColors.Length] };
            _keySets.Add(set);
            foreach (Keyframe key in keys)
                key.keySet = set;
            return set;
        }

        internal void PutInKeySet(KeySet set, IEnumerable<Keyframe> keys)
        {
            RecordUndo("Key set");
            foreach (Keyframe key in keys)
                key.keySet = set;
        }

        internal void TakeOutOfKeySet(KeySet set, IEnumerable<Keyframe> keys)
        {
            RecordUndo("Key set");
            foreach (Keyframe key in keys)
            {
                if (key.keySet == set)
                    key.keySet = null;
            }
        }

        /// <summary>The set goes; its keys stay where they are.</summary>
        internal void DeleteKeySet(KeySet set)
        {
            RecordUndo("Delete key set");
            foreach (KeyValuePair<float, Keyframe> pair in KeysIn(set))
                pair.Value.keySet = null;
            _keySets.Remove(set);
        }

        #region Saving and reading
        /// <summary>The list of sets, once per file. Keyframes point into it by index.</summary>
        private void WriteKeySets(XmlTextWriter writer)
        {
            if (_keySets.Count == 0)
                return;
            writer.WriteStartElement("keySets");
            foreach (KeySet set in _keySets)
            {
                writer.WriteStartElement("set");
                writer.WriteAttributeString("name", set.name);
                writer.WriteValue("color", set.color);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        private void WriteKeySetOf(Keyframe keyframe, XmlTextWriter writer)
        {
            if (keyframe.keySet == null)
                return;
            int index = _keySets.IndexOf(keyframe.keySet);
            if (index >= 0)
                writer.WriteAttributeString("set", XmlConvert.ToString(index));
        }

        /// <summary>
        /// Gets the sets of a file ready for its keyframes. A set with the name of one the scene already
        /// had joins it, so loading a timeline onto an object again does not pile up copies.
        /// </summary>
        private void BeginKeySetRead(XmlNode root)
        {
            _keySetRead = new List<KeySet>();
            XmlNode list = root == null ? null : root.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "keySets");
            if (list == null)
                return;
            var before = new List<KeySet>(_keySets);
            foreach (XmlNode node in list.ChildNodes)
            {
                if (node.Name != "set")
                    continue;
                string name = node.Attributes["name"] != null ? node.Attributes["name"].Value : "Set " + (_keySets.Count + 1);
                KeySet set = before.FirstOrDefault(s => s.name == name);
                if (set == null)
                {
                    set = new KeySet { name = name, color = node.Attributes["colorR"] != null ? node.ReadColor("color") : _keySetColors[_keySets.Count % _keySetColors.Length] };
                    _keySets.Add(set);
                }
                _keySetRead.Add(set);
            }
        }

        private void ReadKeySetOf(Keyframe keyframe, XmlNode keyframeNode)
        {
            if (_keySetRead == null || keyframeNode.Attributes["set"] == null)
                return;
            int index;
            if (int.TryParse(keyframeNode.Attributes["set"].Value, out index) && index >= 0 && index < _keySetRead.Count)
                keyframe.keySet = _keySetRead[index];
        }

        private void EndKeySetRead()
        {
            _keySetRead = null;
        }
        #endregion

        #region ShalltyUtils' Keyframe Groups
        private const string _shalltyGuid = "com.shallty.shalltyutils";
        /// <summary>What ShalltyUtils saved with the scene being loaded, read before Timeline's own data.</summary>
        private string _shalltyGroups;

        /// <summary>
        /// Its Keyframe Groups, as sets. A group names its keys by their track's place in the order
        /// the tracks were loaded in, and the key's time on it; that order is the same here, as long as
        /// every plugin the scene uses is installed.
        /// </summary>
        private void ImportShalltyGroups()
        {
            string data = _shalltyGroups;
            _shalltyGroups = null;
            if (string.IsNullOrEmpty(data))
                return;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                List<Interpolable> order = _interpolables.Values.ToList();
                int groups = 0, keys = 0;
                foreach (XmlNode groupNode in doc.FirstChild.ChildNodes)
                {
                    if (groupNode.Name != "group")
                        continue;
                    var set = new KeySet
                    {
                        name = groupNode.Attributes["name"] != null ? groupNode.Attributes["name"].Value : "Group " + (groups + 1),
                        color = groupNode.Attributes["colorR"] != null ? groupNode.ReadColor("color") : _keySetColors[_keySets.Count % _keySetColors.Length]
                    };
                    set.color.a = 1f;
                    _keySets.Add(set);
                    ++groups;
                    foreach (XmlNode keyNode in groupNode.ChildNodes)
                    {
                        if (keyNode.Name != "keyframe" || keyNode.Attributes["interpolableIndex"] == null || keyNode.Attributes["keyframeKey"] == null)
                            continue;
                        int index = XmlConvert.ToInt32(keyNode.Attributes["interpolableIndex"].Value);
                        float time = XmlConvert.ToSingle(keyNode.Attributes["keyframeKey"].Value);
                        if (index < 0 || index >= order.Count)
                            continue;
                        Keyframe key = FindOccupant(order[index], time, null);
                        if (key == null)
                            continue;
                        key.keySet = set;
                        ++keys;
                    }
                }
                if (groups != 0)
                    Logger.LogMessage(groups + " Keyframe Group(s) from ShalltyUtils are now key sets (" + keys + " keys). Select › Key sets lists them.");
            }
            catch (System.Exception e)
            {
                Logger.LogWarning("Could not read the Keyframe Groups ShalltyUtils saved with this scene: " + e.Message);
            }
        }
        #endregion
    }
}
