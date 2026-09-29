using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Studio;

namespace Timeline
{
    /// <summary>
    /// Tracks a scene has but this game cannot play: their plugin is not installed, or not yet.
    ///
    /// They used to be dropped on load without a word, and the next save wrote the scene without them,
    /// so opening a scene once without a plugin was enough to lose everything that plugin had animated
    /// in it. Now each one is kept as the XML it was read from and written back unchanged, pointed at
    /// its object again by the object's place in the scene at the time of saving.
    /// </summary>
    public partial class Timeline
    {
        private sealed class OrphanTrack
        {
            public XmlNode node;
            public ObjectCtrlInfo oci;
            public string owner;
            public string id;
            /// <summary>Keyframes of it that were in a key set, and which.</summary>
            public List<KeyValuePair<XmlElement, KeySet>> sets;
        }

        private readonly List<OrphanTrack> _orphanTracks = new List<OrphanTrack>();

        private void KeepOrphanTrack(XmlNode node, ObjectCtrlInfo oci, string owner, string id)
        {
            var orphan = new OrphanTrack { node = node.CloneNode(true), oci = oci, owner = owner, id = id, sets = new List<KeyValuePair<XmlElement, KeySet>>() };
            foreach (XmlNode child in orphan.node.ChildNodes)
            {
                XmlElement key = child as XmlElement;
                if (key == null || key.Name != "keyframe" || key.HasAttribute("set") == false)
                    continue;
                int index;
                if (_keySetRead != null && int.TryParse(key.GetAttribute("set"), out index) && index >= 0 && index < _keySetRead.Count)
                    orphan.sets.Add(new KeyValuePair<XmlElement, KeySet>(key, _keySetRead[index]));
                key.RemoveAttribute("set");
            }
            _orphanTracks.Add(orphan);
        }

        /// <summary>Says once per load what was kept, so a missing plugin is noticed rather than guessed at.</summary>
        private void ReportOrphanTracks()
        {
            if (_orphanTracks.Count == 0)
                return;
            string kinds = string.Join(", ", _orphanTracks.Select(o => o.owner).Distinct().ToArray());
            Logger.LogMessage($"{_orphanTracks.Count} track(s) belong to plugins that are not installed ({kinds}). " +
                              "They are kept and saved back with the scene unchanged, but will not play until the plugin is installed.");
        }

        private void WriteOrphanTracks(XmlTextWriter writer, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            foreach (OrphanTrack orphan in _orphanTracks)
            {
                if (orphan.sets != null)
                {
                    foreach (KeyValuePair<XmlElement, KeySet> pair in orphan.sets)
                    {
                        int index = _keySets.IndexOf(pair.Value);
                        if (index >= 0)
                            pair.Key.SetAttribute("set", XmlConvert.ToString(index));
                        else
                            pair.Key.RemoveAttribute("set");
                    }
                }
                XmlElement element = orphan.node.CloneNode(true) as XmlElement;
                if (element == null)
                    continue;
                if (orphan.oci != null)
                {
                    int objectIndex = dic.FindIndex(e => e.Value == orphan.oci);
                    // Its object was deleted from the scene, and the track went with it, as any other would.
                    if (objectIndex == -1)
                        continue;
                    element.SetAttribute("objectIndex", XmlConvert.ToString(objectIndex));
                }
                writer.WriteRaw(element.OuterXml);
            }
        }
    }
}
