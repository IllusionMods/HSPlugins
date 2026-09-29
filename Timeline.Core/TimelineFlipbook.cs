using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using Studio;
using ToolBox.Extensions;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// Flipbook (the rewrite of ShalltyUtils' Mesh Sequencer): meshes of a character or item shown one
    /// after another, like the pages of a flipbook, as RendererEditor's on/off tracks in a group of their
    /// own. The tracks are RendererEditor's, so a scene plays the same with or without Timeline's help.
    /// </summary>
    public partial class Timeline
    {
        private const string _rendererEditorOwner = "RendererEditor";
        private const string _rendererEditorEnabled = "targetEnabled";

        internal bool FlipbookAvailable()
        {
            return _interpolableModelsList.Any(m => m.owner == _rendererEditorOwner && m.id == _rendererEditorEnabled);
        }

        /// <summary>The object whose meshes a flipbook would use: its root, or null when it has none.</summary>
        internal static Transform FlipbookRoot(ObjectCtrlInfo oci)
        {
            OCIChar character = oci as OCIChar;
            if (character != null && character.charInfo != null)
                return character.charInfo.transform;
            OCIItem item = oci as OCIItem;
            if (item != null && item.objectItem != null)
                return item.objectItem.transform;
            return null;
        }

        /// <summary>Every mesh under the object, as the path RendererEditor finds it by.</summary>
        internal static List<string> FlipbookMeshes(ObjectCtrlInfo oci)
        {
            var paths = new List<string>();
            Transform root = FlipbookRoot(oci);
            if (root == null)
                return paths;
            foreach (SkinnedMeshRenderer r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                paths.Add(r.transform.GetPathFrom(root));
            foreach (MeshFilter f in root.GetComponentsInChildren<MeshFilter>(true))
                paths.Add(f.transform.GetPathFrom(root));
            return paths.Where(p => string.IsNullOrEmpty(p) == false).Distinct().ToList();
        }

        /// <summary>
        /// One on/off track per mesh, in order: each is on for its frames and off the rest of the time,
        /// except the first before it starts and the last after it ends, when asked.
        /// </summary>
        internal void MakeFlipbook(List<string> meshes, float start, int framesEach, bool hiddenBefore, bool hiddenAfter)
        {
            if (_selectedOCI == null || meshes.Count == 0)
                return;
            int fps = Mathf.Max(1, _desiredFrameRate);
            float each = Mathf.Max(1, framesEach) / (float)fps;
            start = Mathf.Round(Mathf.Max(0f, start) * fps) / fps;
            var xml = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(xml, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("root");
                writer.WriteStartElement("interpolableGroup");
                writer.WriteAttributeString("name", "Flipbook");
                for (int i = 0; i < meshes.Count; ++i)
                {
                    float on = start + i * each, off = start + (i + 1) * each;
                    bool first = i == 0, last = i == meshes.Count - 1;
                    var keys = new SortedList<float, bool>();
                    if (on > 0.0001f)
                        keys[0f] = first && hiddenBefore == false;
                    keys[Snap(on, fps)] = true;
                    if (last == false || hiddenAfter)
                        keys[Snap(off, fps)] = false;

                    writer.WriteStartElement("interpolable");
                    writer.WriteAttributeString("enabled", "true");
                    writer.WriteAttributeString("owner", _rendererEditorOwner);
                    writer.WriteAttributeString("id", _rendererEditorEnabled);
                    writer.WriteAttributeString("parameterPath", meshes[i]);
                    writer.WriteAttributeString("parameterType", "0");
                    writer.WriteAttributeString("alias", meshes[i].Substring(meshes[i].LastIndexOf('/') + 1));
                    foreach (KeyValuePair<float, bool> key in keys)
                    {
                        writer.WriteStartElement("keyframe");
                        writer.WriteAttributeString("time", XmlConvert.ToString(key.Key));
                        writer.WriteAttributeString("value", key.Value ? "true" : "false");
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            RecordUndo("Flipbook");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml.ToString());
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            ReadInterpolableTree(doc.FirstChild, dic, _selectedOCI);
            UpdateInterpolablesView();
            Logger.LogMessage("Flipbook: " + meshes.Count + " meshes, " + framesEach + " frame(s) each, from " + start.ToString("0.00", CultureInfo.InvariantCulture) + " s.");
        }

        private static float Snap(float t, int fps)
        {
            return Mathf.Round(t * fps) / fps;
        }
    }
}
