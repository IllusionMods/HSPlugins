using System;
using System.Collections.Generic;
using System.Linq;
using Studio;
using Timeline.Graph;
using Timeline.Nla;
using Timeline.View;
using UILib.EventHandlers;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        private enum RowType
        {
            Summary,
            Object,
            Group,
            Track,
            /// <summary>One component of a track, X, Y or Z, listed under it in the Graph.</summary>
            Axis,
            /// <summary>The NLA's row of keys that are not in any strip.</summary>
            Action,
            /// <summary>An NLA lane.</summary>
            Lane
        }

        /// <summary>One line of the channel list and the grid beside it, as buildRows() makes them.</summary>
        private sealed class Row
        {
            public RowType type;
            public object key;
            public string label;
            public readonly List<Interpolable> tracks = new List<Interpolable>();
            public Interpolable tr;
            public GroupNode<InterpolableGroup> group;
            public int depth;
            public float y, h;
            public int c;
            public int lane;
            /// <summary>The object whose NLA stack a lane or keys row belongs to.</summary>
            public ObjectCtrlInfo oci;
        }

        internal sealed partial class View
        {
            private RectTransform _chan;
            private RectTransform _rowsClip;
            private InputField _search;
            private Clickable _onlySelButton;
            private List<Row> _rows = new List<Row>();
            private float _rowsTotal;
            private readonly List<RowView> _rowWidgets = new List<RowView>();
            private RectTransform _chdiv;

            private static readonly object _sceneKey = new object();

            #region Building the list
            /// <summary>.chan: 260 wide, #2C2F35, a hairline on its right.</summary>
            private void BuildChannels()
            {
                Image bg = Kit.Box("Chan", _body, Pal.C(0x2C2F35));
                _chan = bg.rectTransform;
                Kit.Box("Border", _chan, Pal.C(0x111215)).rectTransform.Css(float.NaN, 0f, 0f, 0f, 1f);

                // .search: 26 high, the + Add button, the field and the filter.
                RectTransform searchRow = Kit.Node("Search", _chan).Css(0f, 0f, 1f, float.NaN, float.NaN, 26f);
                Kit.Row(searchRow.gameObject, 6f, 6f, 6f);
                Kit.Box("Border", searchRow, Pal.C(0x111215)).rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f)
                   .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                Clickable add = Pill(searchRow, "Add", 20f, 7f, 0f, Pal.accent, Pal.C(0xF0B558), 3f);
                Kit.Text("Text", add.transform, "＋ Add", 11, Pal.onAccent, TextAnchor.MiddleLeft, true);
                add.tooltip = "Add a track for what is selected in the scene";
                Transform addT = add.transform;
                add.onClick = ToggleAdd;

                Image field = Kit.Box("F", searchRow, Pal.C(0x15171B), 3f);
                Kit.Row(field.gameObject, 6f, 6f, 6f);
                Kit.Flex(field.gameObject, 20f);
                Kit.Icon("Icon", field.transform, "search", Pal.C(0x6B6E74));
                _search = Field(field.transform, "Input", "", 11, Pal.C(0xE4E7EC), -1f);
                Text placeholder = Kit.Text("Placeholder", _search.transform, "Search channels", 11, Pal.C(0x6B6E74));
                placeholder.rectTransform.Fill();
                _search.placeholder = placeholder;
                _search.onValueChanged.AddListener(s =>
                {
                    search = s;
                    scrollY = 0f;
                    _rowsDirty = true;
                });

                _onlySelButton = Pill(searchRow, "OnlySel", 20f, 0f, 0f, new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0f), 3f);
                Kit.Size(_onlySelButton.gameObject, 20f, 20f);
                _onlySelButton.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                IconView target = Kit.Icon("Icon", _onlySelButton.transform, "target", Pal.C(0x6B6E74));
                _onlySelButton.Tint(Pal.C(0x6B6E74), Pal.C(0xE4E7EC), target);
                _onlySelButton.tooltip = "Only show what is selected in the scene";
                _onlySelButton.onClick = () =>
                {
                    onlySel = !onlySel;
                    scrollY = 0f;
                    RefreshOnlySel();
                    _rowsDirty = true;
                };

                // .rowsclip
                _rowsClip = Kit.Node("Rows", _chan).Css(0f, 26f, 1f, 0f);
                _rowsClip.gameObject.AddComponent<RectMask2D>();
                Image catcher = _rowsClip.gameObject.AddComponent<Image>();
                catcher.color = new Color(0f, 0f, 0f, 0f);
                ScrollHandler wheel = _rowsClip.gameObject.AddComponent<ScrollHandler>();
                wheel.onScroll = e => SetScroll(scrollY - e.scrollDelta.y * 22f * 1.5f);
                PointerDownHandler empty = _rowsClip.gameObject.AddComponent<PointerDownHandler>();
                empty.onPointerDown = e =>
                {
                    if (e.button == PointerEventData.InputButton.Left)
                        T.ClearSelectedInterpolables();
                    else if (e.button == PointerEventData.InputButton.Right)
                        OpenMenuAtPointer(e, ChannelItems());
                };

                // .chdiv: drag to widen the list.
                Image div = Kit.Box("Chdiv", _body, new Color(0f, 0f, 0f, 0f));
                div.raycastTarget = true;
                _chdiv = div.rectTransform;
                Clickable divHover = div.gameObject.AddComponent<Clickable>();
                divHover.background = div;
                divHover.normal = new Color(0f, 0f, 0f, 0f);
                divHover.hover = Pal.Accent(0.35f);
                divHover.pressed = divHover.hover;
                divHover.tooltip = "Drag to widen the channel list";
                DragHandler drag = div.gameObject.AddComponent<DragHandler>();
                float startW = 0f, startX = 0f;
                drag.onBeginDrag = e =>
                {
                    startW = chanW;
                    startX = e.position.x / canvas.scaleFactor;
                };
                drag.onDrag = e =>
                {
                    float span = Tt(GridWidth()) - t0;
                    chanW = Mathf.Round(Mathf.Clamp(startW + e.position.x / canvas.scaleFactor - startX, 180f, 520f));
                    LayoutBody();
                    pps = GridWidth() / span;
                };
                RefreshOnlySel();
            }

            private void RefreshOnlySel()
            {
                _onlySelButton.normal = onlySel ? Pal.accent : new Color(0f, 0f, 0f, 0f);
                _onlySelButton.hover = _onlySelButton.normal;
                _onlySelButton.tintNormal = onlySel ? Pal.onAccent : Pal.C(0x6B6E74);
                _onlySelButton.tintHover = onlySel ? Pal.onAccent : Pal.C(0xE4E7EC);
                _onlySelButton.Refresh();
            }

            /// <summary>Places the four columns of the body: list, divider, grid, properties.</summary>
            private void LayoutBody()
            {
                float propsW = props ? 300f : 0f;
                _chan.Css(0f, 0f, float.NaN, 0f, chanW);
                _chdiv.Css(chanW - 3f, 0f, float.NaN, 0f, 6f);
                _gridRect.Css(chanW, 0f, propsW, 0f);
                // Popped out, the panel is in its own window and none of this concerns it.
                if (_propsFloat == false)
                {
                    _props.Css(float.NaN, 0f, 0f, 0f, 300f);
                    _props.gameObject.SetActive(props);
                }
            }

            public float GridWidth()
            {
                return winW - chanW - (props ? 300f : 0f);
            }

            public float GridHeight()
            {
                return winH - HDR - STATUS;
            }

            private void SetScroll(float y)
            {
                scrollY = Mathf.Clamp(y, 0f, Mathf.Max(0f, _rowsTotal - (GridHeight() - RUL)));
            }
            #endregion

            #region Rows
            private void RenderRows()
            {
                _rowsDirty = false;
                _rows = BuildRowList(false);
                SetScroll(scrollY);
            }

            private bool Matches(Interpolable track)
            {
                if (track.ShouldShow() == false)
                    return false;
                string q = search.Trim();
                if (q.Length == 0)
                    return true;
                string hay = (TrackName(track) + " " + track.name + " " + track.owner).ToLowerInvariant();
                return hay.Contains(q.ToLowerInvariant());
            }

            public static string TrackName(Interpolable track)
            {
                if (string.IsNullOrEmpty(track.alias) == false)
                    return track.alias;
                return NodeTrackName(track) ?? track.name;
            }

            private static readonly string[] _bonePrefixes = { "cf_j_", "cf_t_", "cf_s_", "cf_d_", "cf_n_", "cm_j_", "cm_t_", "cf_J_", "cf_T_", "cf_N_", "N_" };

            /// <summary>
            /// A node track named after its node first, then what it moves: "IK Left Hand · Position",
            /// "FK arm00_L · Rotation X". The stored name, "GO Position (cf_t_hand_L(work))", puts the
            /// node last, where the narrow list cuts it off, and every node's track then reads the same.
            /// The object's own node is just "Position".
            /// </summary>
            private static string NodeTrackName(Interpolable track)
            {
                GuideObject node = track.parameter as GuideObject;
                if (node == null || node.transformTarget == null || track.owner != _ownerId || track.id.StartsWith("guideObject") == false)
                    return null;
                string prop = track.id.Substring("guideObject".Length);
                string axis = "";
                if (prop.Length > 0 && "XYZ".IndexOf(prop[prop.Length - 1]) >= 0)
                {
                    axis = " " + prop[prop.Length - 1];
                    prop = prop.Substring(0, prop.Length - 1);
                }
                string what = prop == "Pos" ? "Position" : prop == "Rot" ? "Rotation" : prop == "Scale" ? "Scale" : null;
                if (what == null)
                    return null;
                what += axis;
                if (track.oci != null && track.oci.guideObject == node)
                    return what;

                string kind = "";
                OCIChar character = track.oci as OCIChar;
                if (character != null)
                {
                    if (character.listIKTarget.Any(ik => ik.guideObject == node))
                        kind = "IK ";
                    else if (character.listBones.Any(b => b.guideObject == node))
                        kind = "FK ";
                }
                string bone = node.transformTarget.name;
                string fancy = FancyBoneName(bone);
                if (fancy == bone)
                {
                    fancy = bone.Replace("(work)", "");
                    foreach (string prefix in _bonePrefixes)
                    {
                        if (fancy.StartsWith(prefix))
                        {
                            fancy = fancy.Substring(prefix.Length);
                            break;
                        }
                    }
                }
                return kind + fancy + " · " + what;
            }

            public static string ObjectName(ObjectCtrlInfo oci)
            {
                if (oci == null)
                    return "Scene";
                try
                {
                    if (oci.treeNodeObject != null)
                        return oci.treeNodeObject.textName;
                }
                catch (Exception)
                {
                    // A half deleted object; the name is only for display.
                }
                return "Object";
            }

            /// <summary>
            /// buildRows(): the summary, then each object, its groups and its tracks. The tree the plugin
            /// keeps is shared by every object, so each object walks it and keeps only its own tracks;
            /// a group holding tracks of two characters appears under both, with each one's own.
            /// </summary>
            private List<Row> BuildRowList(bool ignoreCollapse)
            {
                if (editor == "nla")
                    return BuildNlaRows();
                var rows = new List<Row>();
                float row = ROW;
                Row summary = null;
                if (showSummary && editor != "nla")
                {
                    summary = new Row { type = RowType.Summary, key = "sum", label = "Summary", h = row };
                    rows.Add(summary);
                }

                var owners = new List<ObjectCtrlInfo>();
                bool scene = false;
                foreach (Interpolable track in LeavesInOrder(T._interpolablesTree.tree))
                {
                    if (Matches(track) == false)
                        continue;
                    if (track.oci == null)
                        scene = true;
                    else if (owners.Contains(track.oci) == false)
                        owners.Add(track.oci);
                }
                if (scene)
                    owners.Add(null);

                foreach (ObjectCtrlInfo oci in owners)
                {
                    if (onlySel && T._selectedOCI != null && oci != T._selectedOCI)
                        continue;
                    object key = oci ?? _sceneKey;
                    Row obj = new Row { type = RowType.Object, key = key, label = ObjectName(oci), depth = 0, h = row };
                    rows.Add(obj);
                    int start = rows.Count;
                    Walk(T._interpolablesTree.tree, oci, 1, rows, obj.tracks, ignoreCollapse || collapsed.Contains(key) == false);
                    if (summary != null)
                        summary.tracks.AddRange(obj.tracks);
                    if (obj.tracks.Count == 0)
                        rows.Remove(obj);
                }
                if (summary != null && summary.tracks.Count == 0)
                    rows.Remove(summary);

                float y = 0f;
                foreach (Row r in rows)
                {
                    r.y = y;
                    y += r.h;
                }
                _rowsTotal = y;
                return rows;
            }

            private static IEnumerable<Interpolable> LeavesInOrder(List<INode> nodes)
            {
                foreach (INode node in nodes)
                {
                    if (node.type == INodeType.Leaf)
                        yield return ((LeafNode<Interpolable>)node).obj;
                    else
                    {
                        foreach (Interpolable i in LeavesInOrder(((GroupNode<InterpolableGroup>)node).children))
                            yield return i;
                    }
                }
            }

            /// <summary>Adds the rows for one object's part of the tree. Returns its tracks either way.</summary>
            private void Walk(List<INode> nodes, ObjectCtrlInfo oci, int depth, List<Row> rows, List<Interpolable> tracks, bool emit)
            {
                float row = ROW;
                foreach (INode node in nodes)
                {
                    if (node.type == INodeType.Leaf)
                    {
                        Interpolable track = ((LeafNode<Interpolable>)node).obj;
                        if (track.oci != oci || Matches(track) == false)
                            continue;
                        tracks.Add(track);
                        if (emit)
                        {
                            rows.Add(new Row { type = RowType.Track, key = track, tr = track, depth = depth, h = row });
                            // The Graph lists a position or rotation's axes under it, to show or hide each.
                            if (editor == "graph" && Dim(track) > 1 && _axOpen.Contains(track))
                            {
                                for (int c = 0; c < Dim(track); ++c)
                                    rows.Add(new Row { type = RowType.Axis, key = track.GetHashCode() * 4 + c, tr = track, c = c, depth = depth + 1, h = row });
                            }
                        }
                        continue;
                    }
                    GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)node;
                    var inGroup = new List<Interpolable>();
                    Row groupRow = new Row { type = RowType.Group, key = group, label = group.obj.name, group = group, depth = depth, h = row };
                    int at = rows.Count;
                    if (emit)
                        rows.Add(groupRow);
                    Walk(group.children, oci, depth + 1, rows, inGroup, emit && group.obj.expanded);
                    if (inGroup.Count == 0)
                    {
                        if (emit)
                            rows.RemoveAt(at);
                        continue;
                    }
                    groupRow.tracks.AddRange(inGroup);
                    tracks.AddRange(inGroup);
                }
            }

            private readonly HashSet<Interpolable> _axOpen = new HashSet<Interpolable>();

            private bool IsCollapsed(Row r)
            {
                if (r.type == RowType.Track)
                    return _axOpen.Contains(r.tr) == false;
                if (r.type == RowType.Group)
                    return r.group.obj.expanded == false;
                return r.type == RowType.Object && collapsed.Contains(r.key);
            }

            private Interpolable ActiveTrack()
            {
                return T._selectedInterpolables.Count == 0 ? null : T._selectedInterpolables[T._selectedInterpolables.Count - 1];
            }
            #endregion

            #region Row widgets
            /// <summary>One pooled row of the list: .row and everything it can hold.</summary>
            private sealed class RowView
            {
                public RectTransform rect;
                public Image bg;
                public Image bar;
                public Image border;
                public Clickable click;
                public IconView twisty;
                public RectTransform twistyHit;
                public Image swatch;
                public Text name;
                public Text count;
                public IconView eye;
                public IconView lockIcon;
                public IconView solo;
                public Row row;
                public int version = -1;
            }

            private int _rowsVersion;

            private RowView MakeRowView()
            {
                var v = new RowView();
                Image bg = Kit.Box("Row", _rowsClip, new Color(0f, 0f, 0f, 0f));
                bg.raycastTarget = true;
                v.bg = bg;
                v.rect = bg.rectTransform;
                v.click = bg.gameObject.AddComponent<Clickable>();
                v.click.background = bg;
                v.bar = Kit.Box("Act", v.rect, Pal.accent);
                v.bar.rectTransform.Css(0f, 0f, float.NaN, 0f, 2f);
                v.border = Kit.Box("Border", v.rect, Pal.C(0x2A2D32));
                v.border.rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f);

                // Twisty, 14 wide.
                Image twHit = Kit.Box("Tw", v.rect, new Color(0f, 0f, 0f, 0f));
                twHit.raycastTarget = true;
                v.twistyHit = twHit.rectTransform;
                v.twisty = Kit.Icon("Icon", v.twistyHit, "right", Pal.C(0x9A9DA2));
                ((RectTransform)v.twisty.transform).anchorMin = ((RectTransform)v.twisty.transform).anchorMax = new Vector2(0.5f, 0.5f);
                RowView captured = v;
                twHit.gameObject.AddComponent<PointerDownHandler>().onPointerDown = e =>
                {
                    if (e.button == PointerEventData.InputButton.Left)
                        ToggleCollapse(captured.row);
                };

                v.swatch = Kit.Box("Sw", v.rect, Color.white, 1f);
                v.name = Kit.Text("Nm", v.rect, "", 12, Pal.C(0xE4E7EC));
                v.name.horizontalOverflow = HorizontalWrapMode.Wrap;
                v.name.verticalOverflow = VerticalWrapMode.Truncate;
                v.count = Kit.Text("Cnt", v.rect, "", 10, Pal.C(0x6B6E74), TextAnchor.MiddleRight);

                v.eye = RowIcon(v, "Eye", () => ToggleEye(captured.row), "Show in Graph");
                v.lockIcon = RowIcon(v, "Lock", () => ToggleLock(captured.row), "Lock against edits");
                v.solo = RowIcon(v, "Solo", () => ToggleSolo(captured.row), "Solo: only the soloed lanes of this object play");

                v.click.onDown = e => RowDown(captured, e);
                RowDragHandler drag = bg.gameObject.AddComponent<RowDragHandler>();
                drag.view = this;
                drag.row = () => captured.row;
                return v;
            }

            /// <summary>.ic: an 18 by 18 icon button on the right of a row.</summary>
            private IconView RowIcon(RowView v, string name, Action act, string tip)
            {
                Image hit = Kit.Box(name, v.rect, new Color(0f, 0f, 0f, 0f), 3f);
                hit.raycastTarget = true;
                Clickable c = hit.gameObject.AddComponent<Clickable>();
                c.background = hit;
                c.normal = new Color(0f, 0f, 0f, 0f);
                c.hover = Pal.C(0x44474C);
                c.pressed = c.hover;
                c.tooltip = tip;
                c.onClick = act;
                IconView icon = Kit.Icon("Icon", hit.transform, "eye", Pal.C(0x6B6E74));
                RectTransform ir = (RectTransform)icon.transform;
                ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0.5f);
                ir.anchoredPosition = Vector2.zero;
                return icon;
            }

            /// <summary>Puts the rows that are in view on the pooled widgets, every frame.</summary>
            private void PlaceRows()
            {
                float viewTop = scrollY, viewBottom = scrollY + GridHeight() - 26f;
                int used = 0;
                Interpolable active = ActiveTrack();
                foreach (Row r in _rows)
                {
                    if (r.y + r.h < viewTop || r.y > viewBottom)
                        continue;
                    if (used == _rowWidgets.Count)
                        _rowWidgets.Add(MakeRowView());
                    RowView v = _rowWidgets[used++];
                    if (v.rect.gameObject.activeSelf == false)
                        v.rect.gameObject.SetActive(true);
                    v.rect.Css(0f, Mathf.Round(r.y - scrollY), 0f, float.NaN, float.NaN, r.h);
                    if (v.row != r || v.version != _rowsVersion)
                    {
                        v.row = r;
                        v.version = _rowsVersion;
                        FillRow(v, r, active);
                    }
                }
                for (int i = used; i < _rowWidgets.Count; ++i)
                {
                    if (_rowWidgets[i].rect.gameObject.activeSelf)
                        _rowWidgets[i].rect.gameObject.SetActive(false);
                    _rowWidgets[i].row = null;
                }
            }

            /// <summary>chanRowHtml(): what one row shows, and in which colours.</summary>
            private void FillRow(RowView v, Row r, Interpolable active)
            {
                float pad = 6f + r.depth * 14f;
                float h = r.h;
                bool track = r.type == RowType.Track;
                bool isActive = track && r.tr == active;
                bool isSelected = track && T._selectedInterpolables.Contains(r.tr);
                bool hidden = track && T._graphHiddenTracks.Contains(r.tr);
                bool locked = track && T._graphLockedTracks.Contains(r.tr);
                bool off = track && r.tr.enabled == false;

                Color bg = new Color(0f, 0f, 0f, 0f);
                if (r.type == RowType.Summary)
                    bg = Pal.C(0x2A2D32);
                else if (r.type == RowType.Object)
                    bg = Pal.C(0x33353B);
                else if (r.type == RowType.Action || r.type == RowType.Lane)
                    bg = Pal.C(0x2A2D32);
                else if (r.type == RowType.Axis && r.tr == active)
                    bg = Pal.Accent(0.08f);
                else if (r.type == RowType.Group && r.tracks.Count != 0 && r.tracks.TrueForAll(T._selectedInterpolables.Contains))
                    bg = Pal.Accent(0.08f);
                else if (isActive)
                    bg = Pal.Accent(0.18f);
                else if (isSelected)
                    bg = Pal.Accent(0.08f);
                v.click.normal = bg;
                v.click.hover = r.type == RowType.Summary || r.type == RowType.Object ? bg : Pal.C(0x33363C);
                v.click.pressed = v.click.hover;
                v.click.Refresh();
                v.bar.gameObject.SetActive(isActive);

                float x = pad;
                bool axisTwisty = track && editor == "graph" && Dim(r.tr) > 1;
                bool twisty = r.type == RowType.Object || r.type == RowType.Group || axisTwisty;
                v.twistyHit.gameObject.SetActive(twisty);
                v.twistyHit.Css(x, 0f, float.NaN, 0f, 14f);
                if (twisty)
                    v.twisty.Set(IsCollapsed(r) ? "right" : "down", Pal.C(0x9A9DA2));
                x += 14f;

                v.swatch.gameObject.SetActive(track);
                if (track)
                {
                    v.swatch.rectTransform.Css(x, (h - 14f) / 2f, float.NaN, float.NaN, 4f, 14f);
                    v.swatch.color = TrackColor(r.tr);
                    v.swatch.canvasRenderer.SetAlpha(hidden ? 0.4f : 1f);
                    x += 10f;
                }

                // .axl: the axis letter on its colour.
                Transform axl = v.rect.Find("Axl");
                if (r.type == RowType.Axis)
                {
                    if (axl == null)
                    {
                        Image chip = Kit.Box("Axl", v.rect, Color.white, 2f);
                        Text letter = Kit.Text("T", chip.transform, "", 10, Pal.C(0x15171B), TextAnchor.MiddleCenter, true);
                        letter.rectTransform.Fill();
                        axl = chip.transform;
                    }
                    axl.gameObject.SetActive(true);
                    ((RectTransform)axl).Css(x, (h - 14f) / 2f, float.NaN, float.NaN, 14f, 14f);
                    axl.GetComponent<Image>().color = Pal.Hex(AxisColor(r.c));
                    axl.GetComponentInChildren<Text>().text = "XYZ"[Mathf.Min(r.c, 2)].ToString();
                    axl.GetComponent<Image>().canvasRenderer.SetAlpha(T.IsComponentHidden(r.tr, r.c) ? 0.4f : 1f);
                    x += 20f;
                }
                else if (axl != null)
                    axl.gameObject.SetActive(false);

                bool icons = (r.type == RowType.Track || r.type == RowType.Object || r.type == RowType.Group || r.type == RowType.Axis) && editor != "nla";
                // A lane with strips mutes, solos and locks, as Blender's NLA tracks do.
                bool laneIcons = r.type == RowType.Lane && r.lane <= T.TopLane(r.oci);
                float right = 6f + (icons ? 36f : 0f) + (laneIcons ? 54f : 0f);
                bool showCount = twisty && axisTwisty == false && IsCollapsed(r);
                v.count.gameObject.SetActive(showCount);
                if (showCount)
                {
                    v.count.text = r.tracks.Count.ToString();
                    v.count.rectTransform.Css(float.NaN, 0f, right, 0f, 30f);
                    right += 34f;
                }

                string label;
                Color color = Pal.C(0xE4E7EC);
                bool bold = false;
                switch (r.type)
                {
                    case RowType.Summary:
                        label = "Summary";
                        color = Pal.C(0x9A9DA2);
                        bold = true;
                        break;
                    case RowType.Object:
                        label = Kit.Escape(r.label);
                        bold = true;
                        break;
                    case RowType.Group:
                        label = Kit.Escape(r.label);
                        color = Pal.C(0xD5D8DD);
                        break;
                    case RowType.Axis:
                        label = CurveComponents.Name(r.tr.keyframes.Values[0].value, r.c) + " " + Kit.Escape(TrackName(r.tr));
                        if (T.IsComponentHidden(r.tr, r.c))
                            color = new Color(color.r, color.g, color.b, 0.4f);
                        break;
                    case RowType.Action:
                    {
                        NlaStack stack = T.StackOf(r.oci);
                        bool plain = stack.actionBlend == StripBlendMode.Replace && stack.actionInfluence >= 0.999f;
                        label = "Keys " + Kit.Dim("· on top" + (plain ? "" : " · " + stack.actionBlend + " " + Mathf.RoundToInt(stack.actionInfluence * 100f) + "%"));
                        break;
                    }
                    case RowType.Lane:
                    {
                        NlaStack stack = T.StackOf(r.oci);
                        NlaLane lane = stack.lanes.Find(l => l.index == r.lane);
                        if (r.lane > T.TopLane(r.oci))
                            label = Kit.Dim("New lane");
                        else
                            label = Kit.Escape(lane != null ? lane.DisplayName : "Track " + (r.lane + 1)) + (stack.Plays(r.lane) ? "" : " " + Kit.Dim("· off"));
                        break;
                    }
                    default:
                        label = Kit.Escape(TrackName(r.tr));
                        string prop = PropOf(r.tr);
                        if (prop != null)
                            label += " " + Kit.Dim("· " + prop);
                        break;
                }
                if (off)
                    label = "<color=#" + ColorUtility.ToHtmlStringRGBA(new Color(color.r, color.g, color.b, 0.6f)) + ">" + label + "</color>";
                v.name.text = label;
                v.name.color = hidden ? new Color(color.r, color.g, color.b, 0.4f) : color;
                v.name.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
                v.name.rectTransform.Css(x, 0f, right, 0f);

                v.eye.transform.parent.gameObject.SetActive(icons);
                v.lockIcon.transform.parent.gameObject.SetActive(icons);
                if (icons)
                {
                    List<Interpolable> set = track || r.type == RowType.Axis ? new List<Interpolable> { r.tr } : r.tracks;
                    bool allHidden = set.Count != 0 && set.TrueForAll(T._graphHiddenTracks.Contains);
                    bool allLocked = set.Count != 0 && set.TrueForAll(T._graphLockedTracks.Contains);
                    ((RectTransform)v.eye.transform.parent).Css(float.NaN, (h - 18f) / 2f, 6f + 18f, float.NaN, 18f, 18f);
                    ((RectTransform)v.lockIcon.transform.parent).Css(float.NaN, (h - 18f) / 2f, 6f, float.NaN, 18f, 18f);
                    if (r.type == RowType.Axis)
                        allHidden = T.IsComponentHidden(r.tr, r.c);
                    v.eye.Set(allHidden ? "eyeoff" : "eye", allHidden ? Pal.C(0x6B6E74) : Pal.C(0xC9CDD3));
                    v.lockIcon.Set(allLocked ? "lock" : "unlock", allLocked ? Pal.accent : Pal.C(0x6B6E74));
                    // An axis row has the eye only: locking is for the whole track.
                    v.lockIcon.transform.parent.gameObject.SetActive(r.type != RowType.Axis);
                }
                v.solo.transform.parent.gameObject.SetActive(laneIcons);
                if (laneIcons)
                {
                    NlaLane lane = T.StackOf(r.oci).lanes.Find(l => l.index == r.lane);
                    bool mute = lane != null && lane.mute, solo = lane != null && lane.solo, lck = lane != null && lane.locked;
                    v.eye.transform.parent.gameObject.SetActive(true);
                    v.lockIcon.transform.parent.gameObject.SetActive(true);
                    ((RectTransform)v.solo.transform.parent).Css(float.NaN, (h - 18f) / 2f, 6f + 36f, float.NaN, 18f, 18f);
                    ((RectTransform)v.eye.transform.parent).Css(float.NaN, (h - 18f) / 2f, 6f + 18f, float.NaN, 18f, 18f);
                    ((RectTransform)v.lockIcon.transform.parent).Css(float.NaN, (h - 18f) / 2f, 6f, float.NaN, 18f, 18f);
                    v.solo.Set("target", solo ? Pal.accent : Pal.C(0x6B6E74));
                    v.eye.Set(mute ? "eyeoff" : "eye", mute ? Pal.C(0x6B6E74) : Pal.C(0xC9CDD3));
                    v.lockIcon.Set(lck ? "lock" : "unlock", lck ? Pal.accent : Pal.C(0x6B6E74));
                    v.eye.transform.parent.GetComponent<Clickable>().tooltip = "Mute the lane";
                    v.lockIcon.transform.parent.GetComponent<Clickable>().tooltip = "Lock the lane's strips against edits";
                }
                else if (icons)
                {
                    v.eye.transform.parent.GetComponent<Clickable>().tooltip = "Show in Graph";
                    v.lockIcon.transform.parent.GetComponent<Clickable>().tooltip = "Lock against edits";
                }
            }

            /// <summary>The track colour: the one picked for it, or a quiet grey for the default white.</summary>
            public static Color TrackColor(Interpolable track)
            {
                Color c = track.color;
                if (c.r > 0.98f && c.g > 0.98f && c.b > 0.98f)
                    return Pal.Hex(0x8A8F99);
                c.a = 1f;
                return c;
            }

            /// <summary>What part of the object a track drives, when its name says so: "Position", "Rotation".</summary>
            private static string PropOf(Interpolable track)
            {
                return null;
            }
            #endregion

            #region Row input
            private void RowDown(RowView v, PointerEventData e)
            {
                Row r = v.row;
                if (r == null)
                    return;
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (e.button == PointerEventData.InputButton.Right)
                {
                    if (r.type == RowType.Track && T._selectedInterpolables.Contains(r.tr) == false)
                        T.SelectInterpolable(r.tr);
                    else if (r.type != RowType.Track && r.tracks.Count != 0 && r.tracks.TrueForAll(T._selectedInterpolables.Contains) == false)
                        T.SelectInterpolable(r.tracks.ToArray());
                    OpenMenuAtPointer(e, RowItems(r));
                    return;
                }
                if (e.button != PointerEventData.InputButton.Left)
                    return;
                switch (r.type)
                {
                    case RowType.Axis:
                    case RowType.Track:
                        if (ctrl)
                            T.SelectAddInterpolable(r.tr);
                        else
                        {
                            T.SelectInterpolable(r.tr);
                            if (ConfigSyncSelection.Value)
                                T.SelectLinkedGuideObject(r.tr);
                        }
                        break;
                    case RowType.Group:
                    case RowType.Object:
                    case RowType.Summary:
                        if (ctrl)
                            T.SelectAddInterpolable(r.tracks.Where(t => T._selectedInterpolables.Contains(t) == false).ToArray());
                        else
                            T.SelectInterpolable(r.tracks.ToArray());
                        break;
                }
                Touch();
            }

            /// <summary>The right click menu of a row: rename, group, order, and the Channel menu's verbs.</summary>
            private List<MenuItem> RowItems(Row r)
            {
                var items = new List<MenuItem>();
                var tracks = new List<Interpolable>(T._selectedInterpolables);
                if (r.type == RowType.Track || r.type == RowType.Axis)
                {
                    items.Add(new MenuItem { head = "TRACK “" + TrackName(r.tr) + "”" });
                    items.Add(new MenuItem { label = "Rename", act = () => BeginRename(r) });
                    items.Add(new MenuItem { label = "Select its keys", act = () => T.SelectKeyframes(tracks.SelectMany(t => t.keyframes).ToList()) });
                    items.Add(new MenuItem { sep = true });
                    items.Add(new MenuItem { label = "Group selected tracks", act = () => { T._interpolablesTree.GroupTogether(tracks, new InterpolableGroup { name = "New group" }); T.UpdateInterpolablesView(); } });
                    bool inGroup = tracks.Any(t => { LeafNode<Interpolable> leaf = T._interpolablesTree.GetLeafNode(t); return leaf != null && leaf.parent != null; });
                    items.Add(new MenuItem { label = "Take selected out of their group", disabled = inGroup == false, act = () => { T._interpolablesTree.ParentTo(tracks.Select(t => (INode)T._interpolablesTree.GetLeafNode(t)), null); T.UpdateInterpolablesView(); } });
                    items.Add(new MenuItem { label = "Move up", act = () => { T._interpolablesTree.MoveUp(tracks.Select(t => (INode)T._interpolablesTree.GetLeafNode(t))); T.UpdateInterpolablesView(); } });
                    items.Add(new MenuItem { label = "Move down", act = () => { T._interpolablesTree.MoveDown(tracks.Select(t => (INode)T._interpolablesTree.GetLeafNode(t))); T.UpdateInterpolablesView(); } });
                    items.Add(new MenuItem { sep = true });
                    items.AddRange(ChannelItems().Skip(3));
                    items.Add(new MenuItem { sep = true });
                    items.Add(new MenuItem
                    {
                        label = tracks.Count > 1 ? "Delete " + tracks.Count + " tracks" : "Delete track",
                        act = () => Confirm(tracks.Count > 1 ? "Delete " + tracks.Count + " tracks" : "Delete track",
                                           (tracks.Count > 1 ? "These tracks and all their keys are removed." : "“" + TrackName(tracks[0]) + "” and all its keys are removed.") + " Undo brings them back.",
                                           "Delete", () => T.RemoveInterpolables(tracks))
                    });
                }
                else if (r.type == RowType.Group)
                {
                    GroupNode<InterpolableGroup> group = r.group;
                    items.Add(new MenuItem { head = "GROUP “" + r.label + "”" });
                    items.Add(new MenuItem { label = "Rename", act = () => BeginRename(r) });
                    items.Add(new MenuItem
                    {
                        label = "Ungroup",
                        act = () =>
                        {
                            // The tracks and groups inside move up to wherever this group was.
                            GroupNode<InterpolableGroup> parent = group.parent as GroupNode<InterpolableGroup>;
                            T._interpolablesTree.ParentTo(new List<INode>(group.children), parent);
                            T._interpolablesTree.Remove(group);
                            T.UpdateInterpolablesView();
                        }
                    });
                    items.Add(new MenuItem { label = "Move up", act = () => { T._interpolablesTree.MoveUp(group); T.UpdateInterpolablesView(); } });
                    items.Add(new MenuItem { label = "Move down", act = () => { T._interpolablesTree.MoveDown(group); T.UpdateInterpolablesView(); } });
                    items.Add(new MenuItem { sep = true });
                    items.Add(new MenuItem
                    {
                        label = "Delete the group and its tracks",
                        act = () => Confirm("Delete group", "“" + r.label + "” and the " + r.tracks.Count + " track(s) in it are removed, with all their keys.", "Delete", () =>
                        {
                            List<Interpolable> inside = new List<Interpolable>(r.tracks);
                            T._interpolablesTree.Remove(group);
                            T.RemoveInterpolables(inside);
                        })
                    });
                }
                else if (r.type == RowType.Lane)
                    items.AddRange(LaneItems(r, FrameSnap(T._playbackTime)));
                else if (r.type == RowType.Action)
                    items.AddRange(ActionItems(r));
                else
                    items.AddRange(ChannelItems());
                return items;
            }

            /// <summary>.rin: the name turns into a field, in place, until Enter or a click elsewhere.</summary>
            private void BeginRename(Row r)
            {
                RowView v = _rowWidgets.FirstOrDefault(w => w.row == r && w.rect.gameObject.activeSelf);
                if (v == null)
                    return;
                Image box = Kit.Box("Rin", v.rect, Pal.C(0x15171B), 3f);
                box.rectTransform.Css(v.name.rectTransform.offsetMin.x - 2f, (r.h - 18f) / 2f, 6f + 40f, float.NaN, float.NaN, 18f);
                Kit.Ring("Ring", box.transform, Pal.accent, 3f);
                string current = r.type == RowType.Group ? r.label : string.IsNullOrEmpty(r.tr.alias) ? r.tr.name : r.tr.alias;
                InputField field = Field(box.transform, "Input", current, 12, Pal.C(0xE4E7EC), -1f);
                ((RectTransform)field.transform).Fill(5f, 0f, 5f, 0f);
                field.onEndEdit.AddListener(text =>
                {
                    string name = text.Trim();
                    if (r.type == RowType.Group)
                    {
                        if (name.Length != 0)
                            r.group.obj.name = name;
                    }
                    else
                    {
                        // Every selected track takes the name when the one renamed is among them.
                        List<Interpolable> renamed = T._selectedInterpolables.Contains(r.tr) ? new List<Interpolable>(T._selectedInterpolables) : new List<Interpolable> { r.tr };
                        foreach (Interpolable t in renamed)
                            t.alias = name == t.name ? "" : name;
                    }
                    UnityEngine.Object.Destroy(box.gameObject);
                    T.UpdateInterpolablesView();
                    ++_rowsVersion;
                    Touch();
                });
                field.ActivateInputField();
                field.Select();
            }

            private void ToggleCollapse(Row r)
            {
                if (r == null)
                    return;
                if (r.type == RowType.Track)
                {
                    if (_axOpen.Remove(r.tr) == false)
                        _axOpen.Add(r.tr);
                }
                else if (r.type == RowType.Group)
                    r.group.obj.expanded = !r.group.obj.expanded;
                else if (r.type == RowType.Object)
                {
                    if (collapsed.Contains(r.key))
                        collapsed.Remove(r.key);
                    else
                        collapsed.Add(r.key);
                }
                _rowsDirty = true;
                ++_rowsVersion;
            }

            private void ToggleEye(Row r)
            {
                if (r != null && r.type == RowType.Lane)
                {
                    EditLane(r, l => l.mute = !l.mute);
                    return;
                }
                if (r != null && r.type == RowType.Axis)
                {
                    T.ToggleComponent(r.tr, r.c);
                    ++_rowsVersion;
                    Touch();
                    return;
                }
                ToggleSet(r, T._graphHiddenTracks);
            }

            private void ToggleSolo(Row r)
            {
                if (r != null && r.type == RowType.Lane)
                    EditLane(r, l => l.solo = !l.solo);
            }

            /// <summary>Changes a lane's settings, then shows and plays the result.</summary>
            private void EditLane(Row r, Action<NlaLane> change)
            {
                change(T.StackOf(r.oci).Lane(r.lane));
                ++_rowsVersion;
                T.RefreshInterpolation();
                Touch();
            }

            private void ToggleLock(Row r)
            {
                if (r != null && r.type == RowType.Lane)
                {
                    EditLane(r, l => l.locked = !l.locked);
                    return;
                }
                ToggleSet(r, T._graphLockedTracks);
            }

            private void ToggleSet(Row r, HashSet<Interpolable> set)
            {
                if (r == null)
                    return;
                List<Interpolable> tracks = r.type == RowType.Track ? new List<Interpolable> { r.tr } : r.tracks;
                bool all = tracks.Count != 0 && tracks.TrueForAll(set.Contains);
                foreach (Interpolable t in tracks)
                {
                    if (all)
                        set.Remove(t);
                    else
                        set.Add(t);
                }
                ++_rowsVersion;
                Touch();
            }

            private void OpenMenuAtPointer(PointerEventData e, List<MenuItem> items)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_win, e.position, null, out local);
                OpenMenu(items, local.x, -local.y);
            }

            #endregion
        }
    }
}
