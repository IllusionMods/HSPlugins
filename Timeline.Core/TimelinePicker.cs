using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Studio;
using ToolBox.Extensions;
using UnityEngine;

namespace Timeline
{
    /// <summary>A page of the picker: buttons laid out like the character, each linked to a node.</summary>
    internal sealed class PickerPage
    {
        public string name;
        public Color color;
        /// <summary>Whether its buttons' nodes are drawn as dots in the scene, to click and box select.</summary>
        public bool showNodes = true;
        public readonly List<PickerButton> buttons = new List<PickerButton>();
    }

    /// <summary>
    /// A picker button. Its box is in page units, y downwards; the window scales the page to fit. The
    /// node is found again by its object's place in the scene and its path under that object, as
    /// Studio has no lasting id for a node.
    /// </summary>
    internal sealed class PickerButton
    {
        public string label;
        public Color color;
        public Rect box;
        public GuideObject target;
        /// <summary>Where the node was when the scene was read, kept for a node that did not load.</summary>
        public int objectIndex = -1;
        public string path = "";
    }

    /// <summary>
    /// The picker (the rewrite of ShalltyUtils' GuideObject Picker): pages of buttons that select nodes,
    /// saved with the scene. A scene made with ShalltyUtils brings its picker pages along.
    /// </summary>
    public partial class Timeline
    {
        internal readonly List<PickerPage> _pickerPages = new List<PickerPage>();
        internal int _pickerPage;
        /// <summary>ShalltyUtils' picker pages in the scene being loaded, read before Timeline's own data.</summary>
        private string _shalltyPicker;

        /// <summary>Selects a picker button's node in Studio, the object it belongs to first.</summary>
        internal void PickNode(GuideObject node, bool add)
        {
            if (node == null || node.transformTarget == null)
                return;
            GuideObject root = node.parentGuide ?? node;
            TreeNodeObject treeNode = Studio.Studio.Instance.dicInfo.FirstOrDefault(p => p.Value.guideObject == root).Key;
            if (treeNode == null)
                return;
            TreeNodeCtrl tree = Studio.Studio.Instance.treeNodeCtrl;
            GuideObjectManager guides = GuideObjectManager.Instance;
            if (node == root)
            {
                if (add)
                    tree.AddSelectNode(treeNode);
                else
                    tree.SelectSingle(treeNode);
                return;
            }
            if (add)
            {
                if (guides.selectObjects.Contains(node) == false)
                    guides.AddSelectMultiple(node);
                return;
            }
            if (tree.hashSelectNode.Count != 1 || tree.hashSelectNode.Contains(treeNode) == false)
                tree.SelectSingle(treeNode, false);
            guides.StopSelectObject();
            guides.selectObject = node;
        }

        /// <summary>The label a new button gets: the item's name for an object, the bone's otherwise.</summary>
        internal static string NodeLabel(GuideObject node)
        {
            if (node.parentGuide == null)
            {
                TreeNodeObject tree = Studio.Studio.Instance.dicInfo.FirstOrDefault(p => p.Value.guideObject == node).Key;
                if (tree != null)
                    return tree.textName;
            }
            return node.transformTarget.name;
        }

        #region Saving and reading
        private void WritePicker(XmlTextWriter writer, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            if (_pickerPages.Count == 0)
                return;
            writer.WriteStartElement("picker");
            writer.WriteAttributeString("page", XmlConvert.ToString(_pickerPage));
            foreach (PickerPage page in _pickerPages)
            {
                writer.WriteStartElement("page");
                writer.WriteAttributeString("name", page.name);
                writer.WriteValue("color", page.color);
                if (page.showNodes == false)
                    writer.WriteAttributeString("showNodes", XmlConvert.ToString(false));
                foreach (PickerButton button in page.buttons)
                {
                    writer.WriteStartElement("button");
                    writer.WriteAttributeString("label", button.label);
                    writer.WriteValue("color", button.color);
                    writer.WriteAttributeString("x", XmlConvert.ToString(button.box.x));
                    writer.WriteAttributeString("y", XmlConvert.ToString(button.box.y));
                    writer.WriteAttributeString("w", XmlConvert.ToString(button.box.width));
                    writer.WriteAttributeString("h", XmlConvert.ToString(button.box.height));
                    int index = button.objectIndex;
                    string path = button.path;
                    if (button.target != null && button.target.transformTarget != null)
                        NodeAddress(button.target, dic, out index, out path);
                    writer.WriteAttributeString("objectIndex", XmlConvert.ToString(index));
                    writer.WriteAttributeString("path", path);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        /// <summary>A node as its object's place in the scene and its path under that object.</summary>
        private static void NodeAddress(GuideObject node, List<KeyValuePair<int, ObjectCtrlInfo>> dic, out int index, out string path)
        {
            GuideObject root = node.parentGuide ?? node;
            index = dic.FindIndex(p => p.Value.guideObject == root);
            path = index < 0 || node == root ? "" : node.transformTarget.GetPathFrom(root.transformTarget);
        }

        private GuideObject FindNode(List<KeyValuePair<int, ObjectCtrlInfo>> dic, int index, string path)
        {
            if (index < 0 || index >= dic.Count || dic[index].Value == null || dic[index].Value.guideObject == null)
                return null;
            Transform root = dic[index].Value.guideObject.transformTarget;
            Transform t = string.IsNullOrEmpty(path) ? root : root.Find(path);
            GuideObject node;
            return t != null && _allGuideObjects.TryGetValue(t, out node) ? node : null;
        }

        /// <summary>Pages of a scene being loaded, or imported next to the ones already there.</summary>
        private void ReadPicker(XmlNode root, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            XmlNode picker = root.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "picker");
            if (picker == null)
            {
                ImportShalltyPicker(dic);
                return;
            }
            _shalltyPicker = null;
            bool fresh = _pickerPages.Count == 0;
            foreach (XmlNode pageNode in picker.ChildNodes)
            {
                if (pageNode.Name != "page")
                    continue;
                var page = new PickerPage
                {
                    name = pageNode.Attributes["name"] != null ? pageNode.Attributes["name"].Value : "Page " + (_pickerPages.Count + 1),
                    color = pageNode.Attributes["colorR"] != null ? pageNode.ReadColor("color") : _keySetColors[_pickerPages.Count % _keySetColors.Length],
                    showNodes = pageNode.Attributes["showNodes"] == null || pageNode.ReadBool("showNodes")
                };
                foreach (XmlNode b in pageNode.ChildNodes)
                {
                    if (b.Name != "button")
                        continue;
                    var button = new PickerButton
                    {
                        label = b.Attributes["label"] != null ? b.Attributes["label"].Value : "",
                        color = b.Attributes["colorR"] != null ? b.ReadColor("color") : Color.gray,
                        box = new Rect(b.ReadFloat("x"), b.ReadFloat("y"), b.ReadFloat("w"), b.ReadFloat("h")),
                        objectIndex = b.ReadInt("objectIndex"),
                        path = b.Attributes["path"] != null ? b.Attributes["path"].Value : ""
                    };
                    button.target = FindNode(dic, button.objectIndex, button.path);
                    page.buttons.Add(button);
                }
                _pickerPages.Add(page);
            }
            if (fresh && picker.Attributes["page"] != null)
                _pickerPage = Mathf.Clamp(XmlConvert.ToInt32(picker.Attributes["page"].Value), 0, Mathf.Max(0, _pickerPages.Count - 1));
        }

        /// <summary>
        /// ShalltyUtils' pages, laid out as it laid them out: its buttons sit on a large sheet in
        /// pixels, y upwards, so each page is moved to start at its top left button.
        /// </summary>
        private void ImportShalltyPicker(List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            string data = _shalltyPicker;
            _shalltyPicker = null;
            if (string.IsNullOrEmpty(data))
                return;
            int pages, buttons, linked;
            if (ImportShalltyPages(data, (index, path) => FindNode(dic, index, path), out pages, out buttons, out linked) && pages != 0)
                Logger.LogMessage(pages + " picker page(s) from ShalltyUtils are now in View › Picker (" + linked + " of " + buttons + " buttons found their node).");
        }

        /// <summary>
        /// Pages in ShalltyUtils' format, from a scene or a rig; find says which node a button's object
        /// and path lead to, as a scene counts objects in order and a rig by their old key.
        /// </summary>
        private bool ImportShalltyPages(string data, Func<int, string, GuideObject> find, out int pages, out int buttons, out int linked)
        {
            pages = buttons = linked = 0;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                foreach (XmlNode pageNode in doc.FirstChild.ChildNodes)
                {
                    if (pageNode.Name != "page")
                        continue;
                    var page = new PickerPage
                    {
                        name = pageNode.Attributes["pageText"] != null ? pageNode.Attributes["pageText"].Value : "Page " + (_pickerPages.Count + 1),
                        color = pageNode.Attributes["pageColorR"] != null ? pageNode.ReadColor("pageColor") : _keySetColors[_pickerPages.Count % _keySetColors.Length],
                        showNodes = pageNode.Attributes["showNodes"] == null || pageNode.ReadBool("showNodes")
                    };
                    page.color.a = 1f;
                    foreach (XmlNode list in pageNode.ChildNodes)
                    {
                        if (list.Name != "pageButtons")
                            continue;
                        foreach (XmlNode b in list.ChildNodes)
                        {
                            if (b.Name != "button")
                                continue;
                            Vector2 min = b.ReadVector2("offsetMin"), max = b.ReadVector2("offsetMax");
                            var button = new PickerButton
                            {
                                label = b.Attributes["buttonText"] != null ? b.Attributes["buttonText"].Value : "",
                                color = b.Attributes["buttonColorR"] != null ? b.ReadColor("buttonColor") : Color.gray,
                                objectIndex = b.Attributes["objectIndex"] != null ? b.ReadInt("objectIndex") : -1,
                                path = b.Attributes["guideObjectPath"] != null ? b.Attributes["guideObjectPath"].Value : ""
                            };
                            button.color.a = 1f;
                            button.box = ShalltyBox(min, max);
                            button.target = find(button.objectIndex, button.path);
                            if (button.target != null)
                                ++linked;
                            page.buttons.Add(button);
                        }
                    }
                    if (page.buttons.Count == 0)
                        continue;
                    FlipShalltyBoxes(page);
                    _pickerPages.Add(page);
                    ++pages;
                    buttons += page.buttons.Count;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.LogWarning("Could not read picker pages saved by ShalltyUtils: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// A button of ShalltyUtils' sheet, whose corners are in pixels with y upwards. Kept that way
        /// until the whole page is known, then turned over by FlipShalltyBoxes.
        /// </summary>
        internal static Rect ShalltyBox(Vector2 offsetMin, Vector2 offsetMax)
        {
            return new Rect(offsetMin.x, offsetMax.y, Mathf.Max(8f, offsetMax.x - offsetMin.x), Mathf.Max(8f, offsetMax.y - offsetMin.y));
        }

        /// <summary>Moves a page of ShalltyBox boxes to start at its top left button, y downwards.</summary>
        internal static void FlipShalltyBoxes(PickerPage page)
        {
            if (page.buttons.Count == 0)
                return;
            float left = page.buttons.Min(b => b.box.x), top = page.buttons.Max(b => b.box.y);
            foreach (PickerButton button in page.buttons)
                button.box = new Rect(button.box.x - left + 8f, top - button.box.y + 8f, button.box.width, button.box.height);
        }
        #endregion
    }
}
