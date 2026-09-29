using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Studio;
using Timeline.Compat;
using ToolBox.Extensions;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// A constraint of a rig as a file holds it: which objects it links, by their key when the rig was
    /// saved, the paths under them, and its settings. An end outside the rig is a link to fill in.
    /// </summary>
    internal sealed class RigConstraint
    {
        public int parentKey, childKey;
        public string parentPath = "", childPath = "";
        public int missingIndex = -1, missingKind = -1;
        public string missingName = "";
        public readonly Dictionary<string, object> values = new Dictionary<string, object>();
        /// <summary>The object picked for the end outside the rig.</summary>
        public ObjectCtrlInfo linked;

        public bool Outside { get { return parentKey == -1 || childKey == -1; } }
    }

    /// <summary>A rig read from its file and not created yet, waiting for its outside links.</summary>
    internal sealed class PendingRig
    {
        public string name;
        public ObjectInfo root;
        /// <summary>New key of each object to the key it had when saved.</summary>
        public readonly Dictionary<int, int> newToOld = new Dictionary<int, int>();
        public readonly Dictionary<int, string> names = new Dictionary<int, string>();
        public readonly List<RigConstraint> constraints = new List<RigConstraint>();
        public string picker = "";

        /// <summary>One entry per outside object, with every constraint that reaches it.</summary>
        public List<List<RigConstraint>> Links()
        {
            return constraints.Where(c => c.Outside).GroupBy(c => c.missingIndex).Select(g => g.ToList()).ToList();
        }
    }

    /// <summary>
    /// Rigs (the rewrite of ShalltyUtils' Folder Constraints Rig): a folder or item with everything
    /// under it, the NodesConstraints constraints that touch it and its picker pages, saved to a .fcr
    /// file and made again in another scene. Constraints that reach outside the rig are linked again
    /// by hand when it is loaded. The file is ShalltyUtils' own format, so rigs saved with it load here.
    /// </summary>
    public partial class Timeline
    {
        internal PendingRig _rigPending;
        internal bool _rigMoveTracks;

        private static readonly string[] _rigBools = { "enabled", "position", "mirrorPosition", "rotation", "mirrorRotation", "scale", "mirrorScale", "lookAt", "resetOriginalPosition", "resetOriginalRotation", "resetOriginalScale" };
        private static readonly string[] _rigFloats = { "positionChangeFactor", "rotationChangeFactor", "scaleChangeFactor", "positionDamp", "rotationDamp", "scaleDamp" };
        private static readonly string[] _rigVectors = { "positionOffset", "scaleOffset", "originalParentPosition", "originalParentScale" };
        private static readonly string[] _rigRotations = { "rotationOffset", "originalParentRotation" };
        private static readonly string[] _rigLocks = { "positionLocks", "rotationLocks", "scaleLocks" };

        internal static string RigFolder
        {
            get
            {
                // Where ShalltyUtils kept them, next to its own dll, when it is there: old rigs are at hand.
                foreach (string folder in new[] { _assemblyLocation, Path.GetDirectoryName(_assemblyLocation) })
                {
                    string shallty = folder == null ? null : Path.Combine(folder, "ShalltyUtils");
                    if (shallty != null && Directory.Exists(shallty))
                        return shallty;
                }
                string own = Path.Combine(_assemblyLocation, Path.Combine(Name, "Rigs"));
                if (Directory.Exists(own) == false)
                    Directory.CreateDirectory(own);
                return own;
            }
        }

        /// <summary>Whether an object can be the root of a rig: a folder, an item or a camera.</summary>
        internal static bool CanBeRig(ObjectCtrlInfo oci)
        {
            return oci != null && oci.objectInfo != null && (oci.objectInfo.kind == 1 || oci.objectInfo.kind == 3 || oci.objectInfo.kind == 5);
        }

        #region Saving
        internal void SaveRig(ObjectCtrlInfo root, string file)
        {
            Studio.Studio studio = Studio.Studio.Instance;
            List<ObjectCtrlInfo> inside = new List<ObjectCtrlInfo> { root };
            foreach (TreeNodeObject node in Descendants(root.treeNodeObject))
            {
                ObjectCtrlInfo oci;
                if (studio.dicInfo.TryGetValue(node, out oci))
                    inside.Add(oci);
            }
            var set = new HashSet<ObjectCtrlInfo>(inside);

            var self = new List<KeyValuePair<object, ObjectCtrlInfo[]>>();
            var parentOutside = new List<KeyValuePair<object, ObjectCtrlInfo[]>>();
            var childOutside = new List<KeyValuePair<object, ObjectCtrlInfo[]>>();
            foreach (object c in NodesConstraintsLink.Constraints())
            {
                ObjectCtrlInfo parent = OwnerOf(NodesConstraintsLink.Get<Transform>(c, "parentTransform"));
                ObjectCtrlInfo child = OwnerOf(NodesConstraintsLink.Get<Transform>(c, "childTransform"));
                if (parent == null || child == null)
                    continue;
                bool p = set.Contains(parent), ch = set.Contains(child);
                var pair = new KeyValuePair<object, ObjectCtrlInfo[]>(c, new[] { parent, child });
                if (p && ch)
                    self.Add(pair);
                else if (ch)
                    parentOutside.Add(pair);
                else if (p)
                    childOutside.Add(pair);
            }

            studio.SavePreprocessingLoop(root.treeNodeObject);
            using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                byte[] constraints = Encoding.UTF8.GetBytes(WriteRigConstraints(self, parentOutside, childOutside));
                writer.Write(constraints.Length);
                writer.Write(constraints);
                byte[] picker = Encoding.UTF8.GetBytes(WriteRigPicker(set));
                writer.Write(picker.Length);
                writer.Write(picker);
                writer.Write(studio.dicObjectCtrl.Count);
                foreach (KeyValuePair<int, ObjectCtrlInfo> pair in studio.dicObjectCtrl)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value.treeNodeObject.textName);
                }
                WriteRigObject(writer, root.objectInfo);
            }
            Logger.LogMessage("Rig saved: " + inside.Count + " object(s), " + (self.Count + parentOutside.Count + childOutside.Count) + " constraint(s).");
        }

        private static IEnumerable<TreeNodeObject> Descendants(TreeNodeObject node)
        {
            foreach (TreeNodeObject child in node.child)
            {
                yield return child;
                foreach (TreeNodeObject deeper in Descendants(child))
                    yield return deeper;
            }
        }

        /// <summary>The object a transform belongs to: the nearest one up the hierarchy that is an object's own.</summary>
        private static ObjectCtrlInfo OwnerOf(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                Transform captured = t;
                ObjectCtrlInfo oci = Studio.Studio.Instance.dicObjectCtrl.Values.FirstOrDefault(o => o.guideObject != null && o.guideObject.transformTarget == captured);
                if (oci != null)
                    return oci;
            }
            return null;
        }

        private static string PathUnder(Transform t, ObjectCtrlInfo owner)
        {
            return t == owner.guideObject.transformTarget ? "" : t.GetPathFrom(owner.guideObject.transformTarget);
        }

        private static string WriteRigConstraints(List<KeyValuePair<object, ObjectCtrlInfo[]>> self, List<KeyValuePair<object, ObjectCtrlInfo[]>> parentOutside, List<KeyValuePair<object, ObjectCtrlInfo[]>> childOutside)
        {
            using (var text = new StringWriter())
            {
                using (var writer = new XmlTextWriter(text))
                {
                    writer.WriteStartElement("constraintsData");
                    foreach (KeyValuePair<object, ObjectCtrlInfo[]> c in self)
                    {
                        writer.WriteStartElement("selfContainedConstraint");
                        writer.WriteValue("parentOCIdicKey", c.Value[0].objectInfo.dicKey);
                        writer.WriteValue("childOCIdicKey", c.Value[1].objectInfo.dicKey);
                        WriteRigConstraint(writer, c);
                        writer.WriteEndElement();
                    }
                    // Each object outside the rig gets one index, however many constraints reach it.
                    var outside = new List<ObjectCtrlInfo>();
                    foreach (KeyValuePair<object, ObjectCtrlInfo[]> c in parentOutside)
                    {
                        if (outside.Contains(c.Value[0]) == false)
                            outside.Add(c.Value[0]);
                        writer.WriteStartElement("parentlessConstraint");
                        writer.WriteValue("parentOCIdicKey", -1);
                        writer.WriteValue("childOCIdicKey", c.Value[1].objectInfo.dicKey);
                        writer.WriteValue("missingLinkIndex", outside.IndexOf(c.Value[0]));
                        writer.WriteValue("missingLinkKind", c.Value[0].objectInfo.kind);
                        writer.WriteAttributeString("missingLinkName", c.Value[0].treeNodeObject.textName);
                        WriteRigConstraint(writer, c);
                        writer.WriteEndElement();
                    }
                    foreach (KeyValuePair<object, ObjectCtrlInfo[]> c in childOutside)
                    {
                        if (outside.Contains(c.Value[1]) == false)
                            outside.Add(c.Value[1]);
                        writer.WriteStartElement("childrenlessConstraint");
                        writer.WriteValue("parentOCIdicKey", c.Value[0].objectInfo.dicKey);
                        writer.WriteValue("childOCIdicKey", -1);
                        writer.WriteValue("missingLinkIndex", outside.IndexOf(c.Value[1]));
                        writer.WriteValue("missingLinkKind", c.Value[1].objectInfo.kind);
                        writer.WriteAttributeString("missingLinkName", c.Value[1].treeNodeObject.textName);
                        WriteRigConstraint(writer, c);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                return text.ToString();
            }
        }

        private static void WriteRigConstraint(XmlTextWriter writer, KeyValuePair<object, ObjectCtrlInfo[]> pair)
        {
            object c = pair.Key;
            writer.WriteAttributeString("parentPath", PathUnder(NodesConstraintsLink.Get<Transform>(c, "parentTransform"), pair.Value[0]));
            writer.WriteAttributeString("childPath", PathUnder(NodesConstraintsLink.Get<Transform>(c, "childTransform"), pair.Value[1]));
            foreach (string name in _rigBools)
                writer.WriteValue(name, NodesConstraintsLink.Get<bool>(c, name));
            writer.WriteValue("dynamic", NodesConstraintsLink.Get<bool>(c, "fixDynamicBone"));
            foreach (string name in _rigFloats)
                writer.WriteValue(name, NodesConstraintsLink.Get<float>(c, name));
            foreach (string name in _rigVectors)
                writer.WriteValue(name, NodesConstraintsLink.Get<Vector3>(c, name));
            foreach (string name in _rigRotations)
                writer.WriteValue(name, NodesConstraintsLink.Get<Quaternion>(c, name));
            foreach (string name in _rigLocks)
            {
                bool[] axes = NodesConstraintsLink.GetLock(c, name);
                writer.WriteValue(name + "X", axes[0]);
                writer.WriteValue(name + "Y", axes[1]);
                writer.WriteValue(name + "Z", axes[2]);
            }
            writer.WriteAttributeString("alias", NodesConstraintsLink.Get<string>(c, "alias") ?? "");
        }

        /// <summary>The picker buttons that point into the rig, as pages in ShalltyUtils' format.</summary>
        private string WriteRigPicker(HashSet<ObjectCtrlInfo> inside)
        {
            var pages = new List<KeyValuePair<PickerPage, List<PickerButton>>>();
            foreach (PickerPage page in _pickerPages)
            {
                List<PickerButton> buttons = page.buttons.Where(b => b.target != null && inside.Contains(NodeOwner(b.target))).ToList();
                if (buttons.Count != 0)
                    pages.Add(new KeyValuePair<PickerPage, List<PickerButton>>(page, buttons));
            }
            if (pages.Count == 0)
                return "";
            using (var text = new StringWriter())
            {
                using (var writer = new XmlTextWriter(text))
                {
                    writer.WriteStartElement("guideObjectPickerData");
                    foreach (KeyValuePair<PickerPage, List<PickerButton>> page in pages)
                    {
                        writer.WriteStartElement("page");
                        writer.WriteAttributeString("pageText", page.Key.name);
                        writer.WriteValue("pageColor", page.Key.color);
                        writer.WriteValue("showNodes", true);
                        writer.WriteStartElement("pageButtons");
                        foreach (PickerButton b in page.Value)
                        {
                            ObjectCtrlInfo owner = NodeOwner(b.target);
                            writer.WriteStartElement("button");
                            writer.WriteAttributeString("buttonText", b.label);
                            writer.WriteValue("buttonColor", b.color);
                            writer.WriteValue("anchorMin", Vector2.zero);
                            writer.WriteValue("anchorMax", Vector2.zero);
                            // Its sheet runs y upwards.
                            writer.WriteValue("offsetMin", new Vector2(b.box.xMin, -b.box.yMax));
                            writer.WriteValue("offsetMax", new Vector2(b.box.xMax, -b.box.yMin));
                            writer.WriteAttributeString("originalTransform", b.target.transformTarget.name);
                            writer.WriteValue("objectIndex", owner.objectInfo.dicKey);
                            writer.WriteAttributeString("guideObjectPath", PathUnder(b.target.transformTarget, owner));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                return text.ToString();
            }
        }
        #endregion

        #region The objects, in ShalltyUtils' binary layout
        private static void WriteRigObject(BinaryWriter w, ObjectInfo info)
        {
            w.Write(info.kind);
            w.Write(info.dicKey);
            WriteChange(w, info.changeAmount);
            w.Write((int)info.treeState);
            w.Write(info.visible);
            switch (info.kind)
            {
                case 1:
                    OIItemInfo item = (OIItemInfo)info;
                    w.Write(item.group);
                    w.Write(item.category);
                    w.Write(item.no);
                    w.Write(item.animeSpeed);
#if KOIKATSU
                    for (int i = 0; i < 8; i++)
                        w.Write(JsonUtility.ToJson(item.color[i]));
                    for (int i = 0; i < 3; i++)
                        WritePattern(w, item.pattern[i]);
                    w.Write(item.alpha);
                    w.Write(JsonUtility.ToJson(item.lineColor));
                    w.Write(item.lineWidth);
                    w.Write(JsonUtility.ToJson(item.emissionColor));
                    w.Write(item.emissionPower);
                    w.Write(item.lightCancel);
#endif
                    WritePattern(w, item.panel);
                    w.Write(item.enableFK);
                    w.Write(item.bones.Count);
                    foreach (KeyValuePair<string, OIBoneInfo> bone in item.bones)
                    {
                        w.Write(bone.Key);
                        w.Write(bone.Value.dicKey);
                        WriteChange(w, bone.Value.changeAmount);
                    }
                    w.Write(item.enableDynamicBone);
                    w.Write(item.animeNormalizedTime);
                    WriteRigChildren(w, item.child);
                    break;
                case 3:
                    OIFolderInfo folder = (OIFolderInfo)info;
                    w.Write(folder.name);
                    WriteRigChildren(w, folder.child);
                    break;
                case 5:
                    OICameraInfo camera = (OICameraInfo)info;
                    w.Write(camera.name);
                    w.Write(camera.active);
                    break;
            }
        }

        private static void WriteRigChildren(BinaryWriter w, List<ObjectInfo> children)
        {
            List<ObjectInfo> kept = children.Where(c => c.kind == 1 || c.kind == 3 || c.kind == 5).ToList();
            w.Write(kept.Count);
            foreach (ObjectInfo child in kept)
                WriteRigObject(w, child);
        }

        private static void WriteChange(BinaryWriter w, ChangeAmount change)
        {
            w.Write(change.pos.x);
            w.Write(change.pos.y);
            w.Write(change.pos.z);
            w.Write(change.rot.x);
            w.Write(change.rot.y);
            w.Write(change.rot.z);
            w.Write(change.scale.x);
            w.Write(change.scale.y);
            w.Write(change.scale.z);
        }

        private static void ReadChange(BinaryReader r, ChangeAmount change)
        {
            change.pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            change.rot = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            change.scale = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }

        private static void WritePattern(BinaryWriter w, PatternInfo pattern)
        {
            w.Write(pattern._key.Value);
            w.Write(pattern._filePath.Value);
            w.Write(pattern.clamp);
            w.Write(JsonUtility.ToJson(pattern.uv));
            w.Write(pattern.rot);
        }

        private static void ReadPattern(BinaryReader r, PatternInfo pattern)
        {
            pattern._key.Value = r.ReadInt32();
            pattern._filePath.Value = r.ReadString();
            pattern.clamp = r.ReadBoolean();
            pattern.uv = JsonUtility.FromJson<Vector4>(r.ReadString());
            pattern.rot = r.ReadSingle();
        }

        /// <summary>An object and everything under it, each with a new key; kind has been read already.</summary>
        private static ObjectInfo ReadRigObject(BinaryReader r, int kind, PendingRig rig)
        {
            ObjectInfo info;
            switch (kind)
            {
                case 1: info = new OIItemInfo(-1, -1, -1, Studio.Studio.GetNewIndex()); break;
                case 3: info = new OIFolderInfo(Studio.Studio.GetNewIndex()); break;
                case 5: info = new OICameraInfo(Studio.Studio.GetNewIndex()); break;
                default: throw new InvalidDataException("Unknown object kind " + kind + " in the rig.");
            }
            rig.newToOld.Add(info.dicKey, r.ReadInt32());
            ReadChange(r, info.changeAmount);
            info.treeState = (TreeNodeObject.TreeState)r.ReadInt32();
            info.visible = r.ReadBoolean();
            switch (kind)
            {
                case 1:
                    OIItemInfo item = (OIItemInfo)info;
                    item.group = r.ReadInt32();
                    item.category = r.ReadInt32();
                    item.no = r.ReadInt32();
                    item.animeSpeed = r.ReadSingle();
#if KOIKATSU
                    for (int i = 0; i < 8; i++)
                        item.color[i] = JsonUtility.FromJson<Color>(r.ReadString());
                    for (int i = 0; i < 3; i++)
                        ReadPattern(r, item.pattern[i]);
                    item.alpha = r.ReadSingle();
                    item.lineColor = JsonUtility.FromJson<Color>(r.ReadString());
                    item.lineWidth = r.ReadSingle();
                    item.emissionColor = JsonUtility.FromJson<Color>(r.ReadString());
                    item.emissionPower = r.ReadSingle();
                    item.lightCancel = r.ReadSingle();
#endif
                    ReadPattern(r, item.panel);
                    item.enableFK = r.ReadBoolean();
                    int bones = r.ReadInt32();
                    for (int i = 0; i < bones; i++)
                    {
                        string name = r.ReadString();
                        var bone = new OIBoneInfo(Studio.Studio.GetNewIndex());
                        r.ReadInt32();
                        ReadChange(r, bone.changeAmount);
                        item.bones[name] = bone;
                    }
                    item.enableDynamicBone = r.ReadBoolean();
                    item.animeNormalizedTime = r.ReadSingle();
                    ReadRigChildren(r, item.child, rig);
                    break;
                case 3:
                    ((OIFolderInfo)info).name = r.ReadString();
                    ReadRigChildren(r, ((OIFolderInfo)info).child, rig);
                    break;
                case 5:
                    ((OICameraInfo)info).name = r.ReadString();
                    ((OICameraInfo)info).active = r.ReadBoolean();
                    break;
            }
            return info;
        }

        private static void ReadRigChildren(BinaryReader r, List<ObjectInfo> children, PendingRig rig)
        {
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
                children.Add(ReadRigObject(r, r.ReadInt32(), rig));
        }
        #endregion

        #region Loading
        /// <summary>Reads a rig file; it is made in the scene once its outside links are picked.</summary>
        internal PendingRig ReadRig(string file)
        {
            var rig = new PendingRig { name = Path.GetFileNameWithoutExtension(file) };
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream))
            {
                string constraints = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                rig.picker = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                int names = reader.ReadInt32();
                for (int i = 0; i < names; i++)
                {
                    int key = reader.ReadInt32();
                    rig.names[key] = reader.ReadString();
                }
                rig.root = ReadRigObject(reader, reader.ReadInt32(), rig);
                if (string.IsNullOrEmpty(constraints) == false)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(constraints);
                    foreach (XmlNode node in doc.FirstChild.ChildNodes)
                        rig.constraints.Add(ReadRigConstraint(node));
                }
            }
            return rig;
        }

        private static RigConstraint ReadRigConstraint(XmlNode node)
        {
            Func<string, bool> has = name => node.Attributes[name] != null;
            var c = new RigConstraint
            {
                parentKey = node.ReadInt("parentOCIdicKey"),
                childKey = node.ReadInt("childOCIdicKey"),
                parentPath = has("parentPath") ? node.Attributes["parentPath"].Value : "",
                childPath = has("childPath") ? node.Attributes["childPath"].Value : ""
            };
            if (has("missingLinkIndex"))
            {
                c.missingIndex = node.ReadInt("missingLinkIndex");
                c.missingKind = has("missingLinkKind") ? node.ReadInt("missingLinkKind") : -1;
                c.missingName = has("missingLinkName") ? node.Attributes["missingLinkName"].Value : "";
            }
            foreach (string name in _rigBools)
                if (has(name))
                    c.values[name] = node.ReadBool(name);
            if (has("dynamic"))
                c.values["fixDynamicBone"] = node.ReadBool("dynamic");
            foreach (string name in _rigFloats)
                if (has(name))
                    c.values[name] = node.ReadFloat(name);
            foreach (string name in _rigVectors)
                if (has(name + "X"))
                    c.values[name] = node.ReadVector3(name);
            foreach (string name in _rigRotations)
                if (has(name + "X"))
                    c.values[name] = node.ReadQuaternion(name);
            foreach (string name in _rigLocks)
                if (has(name + "X"))
                    c.values[name] = new[] { node.ReadBool(name + "X"), node.ReadBool(name + "Y"), node.ReadBool(name + "Z") };
            c.values["alias"] = has("alias") ? node.Attributes["alias"].Value : "";
            return c;
        }

        /// <summary>Makes the rig: its objects, their names, the constraints, the picker pages, and the animation moved over if asked.</summary>
        internal void CreateRig(PendingRig rig)
        {
            Studio.Studio studio = Studio.Studio.Instance;
            studio.sceneInfo.dicObject.Add(rig.root.dicKey, rig.root);
            switch (rig.root.kind)
            {
                case 1: AddObjectItem.Load((OIItemInfo)rig.root, null, null); break;
                case 3: AddObjectFolder.Load((OIFolderInfo)rig.root, null, null); break;
                case 5: AddObjectCamera.Load((OICameraInfo)rig.root, null, null); break;
            }
            studio.treeNodeCtrl.RefreshHierachy();

            var loaded = new Dictionary<int, ObjectCtrlInfo>();
            foreach (KeyValuePair<int, int> pair in rig.newToOld)
            {
                ObjectCtrlInfo oci;
                if (studio.dicObjectCtrl.TryGetValue(pair.Key, out oci) == false)
                    continue;
                loaded[pair.Value] = oci;
                string name;
                if (rig.names.TryGetValue(pair.Value, out name))
                    oci.treeNodeObject.textName = name;
            }

            if (_rigMoveTracks)
                RecordUndo("Move animation onto the rig");
            int made = 0, moved = 0;
            foreach (RigConstraint c in rig.constraints)
            {
                ObjectCtrlInfo parentOci = c.parentKey == -1 ? c.linked : Lookup(loaded, c.parentKey);
                ObjectCtrlInfo childOci = c.childKey == -1 ? c.linked : Lookup(loaded, c.childKey);
                Transform parent = parentOci == null || parentOci.guideObject == null ? null : Under(parentOci.guideObject.transformTarget, c.parentPath);
                Transform child = childOci == null || childOci.guideObject == null ? null : Under(childOci.guideObject.transformTarget, c.childPath);
                if (parent == null || child == null)
                    continue;
                object made1 = NodesConstraintsLink.Add(Value(c, "enabled", true), parent, child,
                        Value(c, "position", true), Value(c, "positionOffset", Vector3.zero),
                        Value(c, "rotation", true), Value(c, "rotationOffset", Quaternion.identity),
                        Value(c, "scale", true), Value(c, "scaleOffset", Vector3.one), Value(c, "alias", ""));
                if (made1 == null)
                    continue;
                ++made;
                foreach (KeyValuePair<string, object> v in c.values)
                {
                    if (v.Value is bool[])
                        NodesConstraintsLink.SetLock(made1, v.Key, (bool[])v.Value);
                    else if (v.Key != "alias")
                        NodesConstraintsLink.Set(made1, v.Key, v.Value);
                }
                if (_rigMoveTracks && c.linked != null)
                {
                    // The animation of the outside bone goes to the rig object on the other end.
                    ObjectCtrlInfo rigSide = c.parentKey == -1 ? childOci : parentOci;
                    Transform bone = c.parentKey == -1 ? parent : child;
                    moved += MoveTracksToRig(c.linked, bone, rigSide, Value(c, "alias", ""));
                }
            }

            int pages = 0, buttons = 0, linked = 0;
            if (string.IsNullOrEmpty(rig.picker) == false)
                ImportShalltyPages(rig.picker, (key, path) =>
                {
                    ObjectCtrlInfo oci = Lookup(loaded, key);
                    if (oci == null || oci.guideObject == null)
                        return null;
                    Transform t = Under(oci.guideObject.transformTarget, path);
                    GuideObject node;
                    return t != null && _allGuideObjects.TryGetValue(t, out node) ? node : null;
                }, out pages, out buttons, out linked);

            if (_rigMoveTracks)
            {
                UpdateInterpolablesView();
                RefreshInterpolation();
            }
            Logger.LogMessage("Rig “" + rig.name + "” made: " + loaded.Count + " object(s), " + made + " of " + rig.constraints.Count + " constraint(s)" +
                              (pages != 0 ? ", " + pages + " picker page(s)" : "") + (moved != 0 ? ", " + moved + " track(s) moved onto it" : "") + ".");
        }

        private static ObjectCtrlInfo Lookup(Dictionary<int, ObjectCtrlInfo> loaded, int key)
        {
            ObjectCtrlInfo oci;
            return loaded.TryGetValue(key, out oci) ? oci : null;
        }

        private static Transform Under(Transform root, string path)
        {
            return string.IsNullOrEmpty(path) ? root : root.Find(path);
        }

        private static T Value<T>(RigConstraint c, string name, T fallback)
        {
            object v;
            return c.values.TryGetValue(name, out v) && v is T ? (T)v : fallback;
        }
        #endregion

        #region Moving the animation onto the rig
        private static readonly Dictionary<string, string> _boneToNode = new Dictionary<string, string>
        {
            { "bonePos", "guideObjectPos" }, { "boneRot", "guideObjectRot" }, { "boneScale", "guideObjectScale" },
            { "boneXPos", "guideObjectPosX" }, { "boneYPos", "guideObjectPosY" }, { "boneZPos", "guideObjectPosZ" },
            { "boneXRot", "guideObjectRotX" }, { "boneYRot", "guideObjectRotY" }, { "boneZRot", "guideObjectRotZ" }
        };

        private static readonly HashSet<string> _nodeTrackIds = new HashSet<string>
        {
            "guideObjectPos", "guideObjectRot", "guideObjectScale",
            "guideObjectPosX", "guideObjectPosY", "guideObjectPosZ", "guideObjectRotX", "guideObjectRotY", "guideObjectRotZ",
            "guideObjectScaleX", "guideObjectScaleY", "guideObjectScaleZ"
        };

        /// <summary>
        /// The tracks that animate a bone of an object outside the rig, as node tracks or as a pose
        /// editor's bone tracks, move to the rig object that now drives or follows it.
        /// </summary>
        private int MoveTracksToRig(ObjectCtrlInfo outside, Transform bone, ObjectCtrlInfo rigOci, string alias)
        {
            if (bone == null || rigOci == null || rigOci.guideObject == null)
                return 0;
            int moved = 0;
            foreach (Interpolable track in _interpolables.Values.Where(t => t.oci == outside).ToList())
            {
                InterpolableModel model = null;
                GuideObject node = track.parameter as GuideObject;
                if (node != null && node.transformTarget == bone && track.owner == _ownerId && _nodeTrackIds.Contains(track.id))
                    model = GetModel(_ownerId, track.id);
                else if (node == null && BoneOf(track.parameter) == bone)
                {
                    string id;
                    if (_boneToNode.TryGetValue(track.id, out id))
                        model = GetModel(_ownerId, id);
                }
                if (model == null)
                    continue;
                var moved1 = new Interpolable(rigOci, rigOci.guideObject, model);
                if (_interpolables.ContainsKey(moved1.GetHashCode()))
                    continue;
                foreach (KeyValuePair<float, Keyframe> pair in track.keyframes)
                    moved1.keyframes.Add(pair.Key, new Keyframe(pair.Value, moved1));
                moved1.alias = string.IsNullOrEmpty(alias) ? track.alias : (string.IsNullOrEmpty(track.alias) ? moved1.name : track.alias) + " | " + alias;
                moved1.color = track.color;
                moved1.extrapolation = track.extrapolation;
                _interpolables.Add(moved1.GetHashCode(), moved1);
                _interpolablesTree.AddLeaf(moved1, null);
                RemoveInterpolables(new[] { track });
                ++moved;
            }
            return moved;
        }

        /// <summary>The bone a pose editor's bone track moves: the transform its parameter pairs up.</summary>
        private static Transform BoneOf(object parameter)
        {
            if (parameter == null)
                return null;
            FieldInfo value = parameter.GetType().GetField("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return value == null ? null : value.GetValue(parameter) as Transform;
        }
        #endregion
    }
}
