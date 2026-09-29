using System;
using System.Collections.Generic;
using System.Linq;
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
        internal sealed partial class View
        {
            private RectTransform _props;
            private RectTransform _pbody;
            private readonly Dictionary<string, Clickable> _tabs = new Dictionary<string, Clickable>();
            private ScrollRect _pscroll;
            private int _handleAxis;
            private bool _propsFloat;
            private bool _propsMin;
            private RectTransform _pwin;
            private RectTransform _taskbar;
            private static readonly string[] _tabNames = { "Keyframe", "Track", "Strip" };

            #region Frame
            /// <summary>.props: 300 wide on the right, #2C2F35, a hairline on its left, tabs along the top.</summary>
            private void BuildProps()
            {
                Image bg = Kit.Box("Props", _body, Pal.C(0x2C2F35));
                bg.raycastTarget = true;
                _props = bg.rectTransform;
                Kit.Box("Border", _props, Pal.C(0x111215)).rectTransform.Css(0f, 0f, float.NaN, 0f, 1f);

                // .ptabs
                Image tabs = Kit.Box("Ptabs", _props, Pal.C(0x25282D));
                tabs.rectTransform.Css(1f, 0f, 0f, float.NaN, float.NaN, 26f);
                Kit.Row(tabs.gameObject, 0f);
                Kit.Box("Border", tabs.transform, Pal.C(0x111215)).rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f)
                   .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                foreach (string name in _tabNames)
                {
                    Clickable tab = Pill(tabs.transform, name, 26f, 0f, 0f, new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0f), 0f);
                    Kit.Flex(tab.gameObject, 26f);
                    tab.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                    Text label = Kit.Text("Text", tab.transform, name, 11, Pal.C(0x9A9DA2), TextAnchor.MiddleCenter);
                    Image line = Kit.Box("On", tab.transform, Pal.accent);
                    line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    line.rectTransform.Css(8f, float.NaN, 8f, 0f, float.NaN, 2f);
                    tab.Tint(Pal.C(0x9A9DA2), Pal.C(0xE4E7EC), label);
                    string captured = name;
                    tab.onClick = () =>
                    {

                        this.tab = captured;
                        _propsDirty = true;
                    };
                    _tabs[name] = tab;
                }
                // .ptab-btn: pop out, or dock back when already out.
                Clickable pop = Pill(tabs.transform, "Pop", 26f, 0f, 0f, new Color(0f, 0f, 0f, 0f), Pal.C(0x2C2F35), 0f);
                Kit.Size(pop.gameObject, 26f, 26f);
                pop.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                IconView popIcon = Kit.Icon("Icon", pop.transform, "popout", Pal.C(0x9A9DA2));
                pop.Tint(Pal.C(0x9A9DA2), Pal.C(0xE4E7EC), popIcon);
                pop.tooltip = "Pop out into its own window";
                pop.onClick = () => { if (_propsFloat) DockProps(); else FloatProps(); };
                _popButton = pop;

                _pscroll = Kit.Node("Scroll", _props).Css(1f, 26f, 0f, 0f).gameObject.AddComponent<ScrollRect>();
                RectTransform viewport = (RectTransform)_pscroll.transform;
                viewport.gameObject.AddComponent<RectMask2D>();
                Image catcher = viewport.gameObject.AddComponent<Image>();
                catcher.color = new Color(0f, 0f, 0f, 0f);
                _pbody = Kit.Node("Pbody", viewport);
                _pbody.anchorMin = new Vector2(0f, 1f);
                _pbody.anchorMax = new Vector2(1f, 1f);
                _pbody.pivot = new Vector2(0.5f, 1f);
                _pbody.sizeDelta = Vector2.zero;
                ContentSizeFitter fit = _pbody.gameObject.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Kit.Col(_pbody.gameObject, 8f, new RectOffset(10, 10, 8, 12));
                _pscroll.viewport = viewport;
                _pscroll.content = _pbody;
                _pscroll.horizontal = false;
                _pscroll.movementType = ScrollRect.MovementType.Clamped;
                _pscroll.scrollSensitivity = 30f;
                _pscroll.inertia = false;
            }

            private Clickable _popButton;

            private void RenderProps()
            {
                if (_miniDragging)
                    return;
                _propsDirty = false;
                foreach (KeyValuePair<string, Clickable> pair in _tabs)
                {
                    bool on = pair.Key == tab;
                    pair.Value.normal = on ? Pal.C(0x2C2F35) : new Color(0f, 0f, 0f, 0f);
                    pair.Value.hover = pair.Value.normal;
                    pair.Value.tintNormal = on ? Pal.C(0xE4E7EC) : Pal.C(0x9A9DA2);
                    pair.Value.transform.Find("On").gameObject.SetActive(on);
                    pair.Value.Refresh();
                }
                if (_popButton != null)
                    _popButton.tooltip = _propsFloat ? "Dock back into the Timeline window" : "Pop out into its own window";
                if (props == false && _propsFloat == false)
                    return;
                // Typing into a field of the panel keeps it: rebuilding would take the field away mid word.
                GameObject focused = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
                if (focused != null && focused.transform.IsChildOf(_pbody) && focused.GetComponent<InputField>() != null && focused.GetComponent<InputField>().isFocused)
                {
                    _propsDirty = true;
                    return;
                }

                float scroll = _pscroll.verticalNormalizedPosition;
                for (int i = _pbody.childCount - 1; i >= 0; --i)
                    UnityEngine.Object.Destroy(_pbody.GetChild(i).gameObject);
                _pbody.DetachChildren();
                switch (tab)
                {
                    case "Track":
                        TrackPanel();
                        break;
                    case "Strip":
                        StripPanel();
                        break;
                    default:
                        KeyPanel();
                        break;
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(_pbody);
                _pscroll.verticalNormalizedPosition = scroll;
            }

            private void EmptyPanel(string title, string message)
            {
                RectTransform box = Kit.Node("Empty", _pbody);
                Kit.Col(box.gameObject, 6f, new RectOffset(6, 6, 18, 18));
                Kit.Text("B", box, title, 13, Pal.C(0xE4E7EC), TextAnchor.UpperLeft, true);
                Kit.Paragraph("Text", box, message, 12, Pal.C(0x9A9DA2));
            }
            #endregion

            #region Keyframe panel
            private KeyValuePair<float, Keyframe>? ActiveKey()
            {
                if (T._selectedKeyframes.Count == 0)
                    return null;
                return T._selectedKeyframes[T._selectedKeyframes.Count - 1];
            }

            private enum Interp
            {
                Constant,
                Linear,
                Bezier,
                EaseIn,
                EaseOut,
                EaseInOut,
                Bounce,
                Custom
            }

            private static readonly Interp[] _tiles = { Interp.Constant, Interp.Linear, Interp.Bezier, Interp.EaseIn, Interp.EaseOut, Interp.EaseInOut, Interp.Bounce };

            private static string InterpName(Interp i)
            {
                switch (i)
                {
                    case Interp.Constant: return "Constant";
                    case Interp.Linear: return "Linear";
                    case Interp.Bezier: return "Bézier";
                    case Interp.EaseIn: return "Ease in";
                    case Interp.EaseOut: return "Ease out";
                    case Interp.EaseInOut: return "Ease in-out";
                    case Interp.Bounce: return "Bounce";
                    default: return "Custom";
                }
            }

            private static float InterpCurve(Interp i, float u)
            {
                switch (i)
                {
                    case Interp.Linear: return u;
                    case Interp.Bezier: return u * u * (3f - 2f * u);
                    case Interp.EaseIn: return u * u * u;
                    case Interp.EaseOut: return 1f - Mathf.Pow(1f - u, 3f);
                    case Interp.EaseInOut: return u < 0.5f ? 4f * u * u * u : 1f - Mathf.Pow(-2f * u + 2f, 3f) / 2f;
                    case Interp.Bounce: return TimelineEasing.Evaluate(EasingKind.Bounce, EasingDirection.Out, u);
                    default: return u;
                }
            }

            /// <summary>keyPanel()</summary>
            private void KeyPanel()
            {
                KeyValuePair<float, Keyframe>? active = ActiveKey();
                if (active.HasValue == false)
                {
                    EmptyPanel("No keyframe selected", "Click a diamond in the Dope Sheet or a point in the Graph. Middle-click a row to add a key, or press I to key the selected tracks at the playhead.");
                    return;
                }
                Keyframe k = active.Value.Value;
                float time = active.Value.Key;
                Interpolable tr = k.parent;
                int i = tr.keyframes.IndexOfValue(k);
                int count = tr.keyframes.Count;
                int n = T._selectedKeyframes.Count;
                bool step = IsStep(tr);
                int dim = CurveComponents.Count(k.value);

                // .phead
                RectTransform head = PHead(TrackColor(tr), TrackName(tr), ObjectName(tr.oci) + GroupPath(tr));
                Clickable ktype = Pill(head, "Ktype", 20f, 6f, 5f, Pal.C(0x15171B), Pal.C(0x15171B), 3f);
                RectTransform dotHolder = Kit.Node("Dot", ktype.transform);
                Kit.Size(dotHolder.gameObject, 8f, 8f);
                Paint dot = dotHolder.gameObject.AddComponent<Paint>();
                dot.Begin();
                dot.Diamond(4f, 4f, 7f, KindFill(k.kind));
                dot.End();
                Text kname = Kit.Text("Text", ktype.transform, KindName(k.kind), 11, Pal.C(0xD5D8DD));
                IconView kdown = Kit.Icon("Down", ktype.transform, "down", Pal.C(0xD5D8DD));
                ktype.Tint(Pal.C(0xD5D8DD), Pal.C(0xE4E7EC), kname, kdown);
                ktype.tooltip = "Key type: how the key looks and what it is for, not how it moves";
                Transform ktypeT = ktype.transform;
                ktype.onClick = () =>
                {
                    var items = new List<MenuItem>();
                    foreach (KeyframeKind kind in new[] { KeyframeKind.Keyframe, KeyframeKind.Breakdown, KeyframeKind.MovingHold, KeyframeKind.Extreme, KeyframeKind.Jitter })
                    {
                        KeyframeKind captured = kind;
                        items.Add(new MenuItem { label = KindName(kind), dot = KindFill(kind), check = k.kind == kind, act = () => T.SetKeyframeKind(captured) });
                    }
                    Vector2 at = WinPoint((RectTransform)ktypeT, 0f);
                    OpenMenu(items, at.x, at.y + 22f);
                };
                if (n > 1)
                    Chip(head, n + " keys");

                // TIME
                RectTransform sect = Sect();
                Cap(sect, "TIME", "key " + (i + 1) + " of " + count);
                RectTransform line = Line(sect);
                Btn(line, "◂", false, () => { T.SelectPreviousKeyframe(); Touch(); }, "Previous key on this track");
                Fld(line, null, 0, Fmt(time), "s", s =>
                {
                    float t;
                    if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out t))
                        T.SaveKeyframeTime(Mathf.Clamp(t, 0f, T._duration));
                });
                Btn(line, "▸", false, () => { T.SelectNextKeyframe(); Touch(); }, "Next key on this track");
                Btn(line, "To playhead", false, () => T.SaveKeyframeTime(FrameSnap(T._playbackTime)), null);

                // VALUE
                sect = Sect();
                Cap(sect, "VALUE", n > 1 ? "of this key only" : null);
                line = Line(sect);
                if (dim == 0 || k.value is bool)
                {
                    // A value that switches picks from its options rather than being typed.
                    UnityEngine.Object.DestroyImmediate(line.gameObject);
                    OptChips(sect, tr, k);
                }
                else
                {
                    for (int c = 0; c < dim; ++c)
                    {
                        int captured = c;
                        string ax = dim > 1 ? CurveComponents.Name(k.value, c) : "Value";
                        float v = CurveComponents.Get(k.value, c);
                        Fld(line, ax, dim > 1 ? AxisColor(c) : 0, v.ToString(dim == 1 ? "0.00" : "0.000", System.Globalization.CultureInfo.InvariantCulture), null, s =>
                        {
                            float parsed;
                            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                            {
                                T.RecordUndo("Edit value");
                                k.value = CurveComponents.With(k.value, captured, parsed);
                                T.RefreshInterpolation();
                                T.UpdateGrid();
                            }
                        });
                    }
                }
                line = Line(sect);
                Btn(line, "Set from current pose", true, () => T.UseCurrentValue(), "Use what the scene shows now, including anything you dragged");
                line = Line(sect);
                if (n > 1)
                    Btn(line, "Give all " + n + " this value", true, () => T.SetSelectedValues(k.value), "Every selected key holding the same kind of value takes this key's");
                Btn(line, "Default", n <= 1, () => T.SetSelectedValues(DefaultValue(k.value)), "Zero, no turn, black or off, for every selected key");
                line = Line(sect);
                if (dim > 0 && k.value is bool == false)
                    Btn(line, "Add / subtract…", true, () => OpenOffset(k.value), "An amount added to or taken from every selected key");
                Btn(line, "Randomize…", true, OpenRandomize, "A little randomness in the selected keys' values or times");

                if (step)
                {
                    sect = Sect();
                    Cap(sect, "INTERPOLATION", null);
                    Note(sect, "Switches straight to this value at the key. A list or an on/off value has nothing in between, so there is no curve and there are no handles.");
                }
                else
                {
                    string family;
                    Interp current = InterpOf(k, out family);
                    sect = Sect();
                    Cap(sect, "INTERPOLATION", (family ?? InterpName(current)) + " · to the next key" + (i == count - 1 ? " (this is the last one)" : ""));
                    RectTransform tiles = Kit.Node("Tiles", sect);
                    GridLayoutGroup grid = tiles.gameObject.AddComponent<GridLayoutGroup>();
                    grid.spacing = new Vector2(2f, 2f);
                    grid.cellSize = new Vector2((280f - 7f * 2f) / 8f, 26f);
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 8;
                    foreach (Interp tile in _tiles)
                    {
                        Interp captured = tile;
                        Tile(tiles, tile, current == tile, InterpName(tile), () => SetInterp(captured));
                    }
                    // The ninth tile is the easing picked from the grid, drawn as itself, or the way to the grid.
                    // The ninth tile always opens the grid; lit up while one of its easings is in use,
                    // whose name the caption above already gives.
                    Transform moreT = Tile(tiles, Interp.Custom, family != null, family != null ? family + " · more easings ▾" : "More easings ▾", null).transform;
                    moreT.GetComponent<Clickable>().onClick = () =>
                    {
                        Vector2 at = WinPoint((RectTransform)moreT, 0f);
                        OpenEasingGrid(at.x - 150f, at.y + 14f, family);
                    };
                    line = Line(sect);
                    Transform savedT = Btn(line, "Saved curves ▾", true, null, "Curves saved to files, to give to the selected keys").transform;
                    savedT.GetComponent<Clickable>().onClick = () =>
                    {
                        Vector2 at = WinPoint((RectTransform)savedT, 0f);
                        OpenMenu(SavedCurveItems(k), at.x, at.y + 24f);
                    };

                    // HANDLES
                    sect = Sect();
                    RectTransform cap = Cap(sect, "HANDLES", null);
                    int prevIndex = i - 1;
                    bool prevBez = i > 0 && HandleMath.HasHandles(tr.keyframes.Values[i - 1], k);
                    bool nextBez = i < count - 1 && HandleMath.HasHandles(k, tr.keyframes.Values[i + 1]);
                    if (_handleAxisKey != k)
                    {
                        _handleAxisKey = k;
                        _handleAxis = MovingAxis(tr, i, dim);
                    }
                    int ca = Mathf.Min(_handleAxis, Mathf.Max(0, dim - 1));
                    if (dim > 1 && count > 1)
                        AxisChips(cap, dim, ca);
                    if (count < 2)
                        Note(sect, "This track has a single key, so there is no segment to shape.");
                    else if (prevBez == false && nextBez == false)
                    {
                        // The shape and the playhead still show; there are just no handles to drag.
                        Mini(sect, tr, i, ca);
                        Note(sect, "The segments on either side of this key are not Bézier curves. Pick Bézier above to get handles.");
                    }
                    else
                    {
                        Mini(sect, tr, i, ca);
                        line = Line(sect);
                        Hsel(line, k, prevBez, true);
                        Hsel(line, k, nextBez, false);
                        Note(sect, n > 1 ? "Dragging a handle shapes all " + n + " selected keys the same way, on the same side."
                                         : "Drag the round handles, here or in the Graph. Dragging an automatic handle makes it Free.");
                    }
                }

                sect = Sect();
                line = Line(sect);
                Btn(line, "Copy", true, () => T.CopyKeyframes(), null);
                Btn(line, "Paste", true, () => T.PasteKeyframes(), null);
                Btn(line, "Delete", true, () => DeleteKeys(), null).GetComponentInChildren<Text>().color = Pal.Hex(0xF08A7E);
            }

            private static readonly uint[] _axisColors = { 0xE85A5C, 0x82CC63, 0x5C99F2, 0xC9CDD3 };
            private Keyframe _handleAxisKey;

            /// <summary>The component that changes the most around a key, which is the one worth showing first.</summary>
            private static int MovingAxis(Interpolable tr, int i, int dim)
            {
                IList<Keyframe> keys = tr.keyframes.Values;
                int best = 0;
                float most = -1f;
                for (int c = 0; c < Mathf.Min(dim, 3); ++c)
                {
                    float v = CurveComponents.Get(keys[i].value, c), change = 0f;
                    if (i > 0)
                        change += Mathf.Abs(v - CurveComponents.Get(keys[i - 1].value, c));
                    if (i + 1 < keys.Count)
                        change += Mathf.Abs(CurveComponents.Get(keys[i + 1].value, c) - v);
                    if (change > most)
                    {
                        most = change;
                        best = c;
                    }
                }
                return best;
            }

            private static uint AxisColor(int c)
            {
                return _axisColors[Mathf.Clamp(c, 0, 3)];
            }

            private string GroupPath(Interpolable tr)
            {
                LeafNode<Interpolable> leaf = T._interpolablesTree.GetLeafNode(tr);
                if (leaf == null || leaf.parent == null)
                    return "";
                var names = new List<string>();
                for (IGroupNode g = leaf.parent; g != null; g = g.parent)
                {
                    GroupNode<InterpolableGroup> group = g as GroupNode<InterpolableGroup>;
                    if (group != null && group.obj != null)
                        names.Insert(0, group.obj.name);
                }
                return names.Count == 0 ? "" : " › " + string.Join(" › ", names.ToArray());
            }

            /// <summary>.hsel: one side's handle type, with its colour dot.</summary>
            private void Hsel(RectTransform line, Keyframe k, bool enabled, bool left)
            {
                HandleType type = k.handles == null ? HandleType.Auto : left ? k.handles.leftType : k.handles.rightType;
                Clickable h = Pill(line, "Hsel", 22f, 7f, 6f, Pal.C(0x15171B), Pal.C(0x1E2025), 3f);
                Kit.Flex(h.gameObject, 22f);
                if (left)
                    Kit.Text("L", h.transform, "◂", 11, Pal.C(0xD5D8DD));
                RectTransform dotHolder = Kit.Node("Dot", h.transform);
                Kit.Size(dotHolder.gameObject, 8f, 8f);
                Paint dot = dotHolder.gameObject.AddComponent<Paint>();
                dot.Begin();
                dot.Circle(4f, 4f, 4f, enabled ? HandleColor(type) : Pal.C(0x44474C));
                dot.End();
                Kit.Text("Text", h.transform, enabled ? HandleName(type) : "None", 11, Pal.C(0xD5D8DD));
                if (left == false)
                    Kit.Text("R", h.transform, "▸", 11, Pal.C(0xD5D8DD));
                Kit.Spacer(h.transform);
                Kit.Icon("Down", h.transform, "down", Pal.C(0x6B6E74));
                if (enabled == false)
                {
                    CanvasGroup g = h.gameObject.AddComponent<CanvasGroup>();
                    g.alpha = 0.35f;
                    g.blocksRaycasts = false;
                    h.disabled = true;
                    return;
                }
                Transform ht = h.transform;
                h.onClick = () =>
                {
                    var items = new List<MenuItem>();
                    foreach (HandleType t in new[] { HandleType.AutoClamped, HandleType.Auto, HandleType.Vector, HandleType.Aligned, HandleType.Free })
                    {
                        HandleType captured = t;
                        items.Add(new MenuItem { label = HandleName(t), dot = HandleColor(t), check = t == type, act = () => SetHandleSide(captured, left) });
                    }
                    Vector2 at = WinPoint((RectTransform)ht, 0f);
                    OpenMenu(items, at.x, at.y + 24f);
                };
            }

            private void SetHandleSide(HandleType type, bool left)
            {
                T.RecordUndo("Handle type");
                foreach (KeyValuePair<float, Keyframe> pair in T._selectedKeyframes)
                {
                    Interpolable track = pair.Value.parent;
                    HandleMath.Convert(track.keyframes);
                    int index = track.keyframes.IndexOfValue(pair.Value);
                    if (index >= 0 && pair.Value.handles != null)
                        MaterialiseHandle(track.keyframes, index, left == false, type);
                }
                T.RefreshInterpolation();
                T.UpdateGrid();
                Touch();
            }

            /// <summary>drawMini(): the curve around this key, its neighbours, and its handles.</summary>
            private void Mini(RectTransform parent, Interpolable tr, int i, int c)
            {
                RectTransform holder = Kit.Node("Mini", parent);
                Kit.Size(holder.gameObject, -1f, 110f);
                Image bg = holder.gameObject.AddComponent<Image>();
                bg.sprite = Kit.Rounded(4f);
                bg.type = Image.Type.Sliced;
                bg.color = Pal.C(0x1B1D22);
                bg.raycastTarget = true;
                // Laid out first, so the graph is drawn at the width it will have.
                LayoutRebuilder.ForceRebuildLayoutImmediate(_pbody);
                holder.gameObject.AddComponent<MiniGraph>().Init(this, tr, tr.keyframes.Values[i], c);
            }

            private static Color CurveColor(Interpolable tr, int c)
            {
                if (tr.keyframes.Count != 0 && CurveComponents.Count(tr.keyframes.Values[0].value) > 1)
                    return Pal.Hex(AxisColor(c));
                return TrackColor(tr);
            }

            /// <summary>The value a track plays at a time, one component of it.</summary>
            private static float ValueAt(Interpolable tr, float t, int c)
            {
                IList<float> times = tr.keyframes.Keys;
                IList<Keyframe> keys = tr.keyframes.Values;
                if (keys.Count == 0)
                    return 0f;
                if (t <= times[0])
                    return CurveComponents.Get(keys[0].value, c);
                if (t >= times[keys.Count - 1])
                    return CurveComponents.Get(keys[keys.Count - 1].value, c);
                int i = 0;
                while (i < keys.Count - 2 && times[i + 1] <= t)
                    ++i;
                return SampleComponent(tr, i, t, c);
            }

            private void AxisChips(RectTransform cap, int dim, int current)
            {
                RectTransform chips = Kit.Node("Axchips", cap);
                Kit.Row(chips.gameObject, 3f);
                for (int j = 0; j < Mathf.Min(dim, 3); ++j)
                {
                    int captured = j;
                    bool on = j == current;
                    Clickable chip = Pill(chips, "Ax", 18f, 0f, 0f, on ? Pal.Hex(AxisColor(j)) : Pal.C(0x25282D), on ? Pal.Hex(AxisColor(j)) : Pal.C(0x25282D), 3f);
                    Kit.Size(chip.gameObject, 22f, 18f);
                    chip.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                    Kit.Text("Text", chip.transform, "XYZ"[j].ToString(), 10, on ? Pal.C(0x15171B) : Pal.C(0x9A9DA2), TextAnchor.MiddleCenter, true);
                    chip.onClick = () =>
                    {
                        _handleAxis = captured;
                        _propsDirty = true;
                    };
                }
            }
            #endregion

            #region Track panel
            private static readonly uint[] _trackColors = { 0xD9A441, 0x4FC3D9, 0x9C7BE0, 0xE86BAE, 0x82CC63, 0xE85A5C, 0x8A8F99 };

            /// <summary>trackPanel()</summary>
            private void TrackPanel()
            {
                Interpolable tr = ActiveTrack();
                if (tr == null)
                {
                    EmptyPanel("No track selected", "Click a row in the channel list.");
                    return;
                }
                PHead(TrackColor(tr), TrackName(tr), tr.name + " · " + tr.owner);

                RectTransform sect = Sect();
                Cap(sect, "NAME", "empty = default");
                RectTransform line = Line(sect);
                InputField alias = Fld(line, null, 0, tr.alias, null, s =>
                {
                    T.RecordUndo("Rename track");
                    tr.alias = s.Trim();
                    T.UpdateInterpolablesView();
                    Touch();
                });
                Text ph = Kit.Text("Placeholder", alias.transform, Kit.Escape(tr.name), 12, Pal.C(0x6B6E74));
                ph.rectTransform.Fill();
                alias.placeholder = ph;

                sect = Sect();
                Cap(sect, "GROUP", null);
                line = Line(sect);
                string group = GroupPath(tr);
                // .hsel: the group it is in, and a list to move it to another.
                Clickable picker = Pill(line, "Hsel", 22f, 7f, 6f, Pal.C(0x15171B), Pal.C(0x1E2025), 3f);
                Kit.Flex(picker.gameObject, 22f);
                Kit.Text("Text", picker.transform, Kit.Escape(group.Length == 0 ? "No group" : group.Substring(3)), 11, Pal.C(0xD5D8DD));
                Kit.Spacer(picker.transform);
                Kit.Icon("Down", picker.transform, "down", Pal.C(0x6B6E74));
                Transform pickerT = picker.transform;
                picker.onClick = () =>
                {
                    Vector2 at = WinPoint((RectTransform)pickerT, 0f);
                    OpenMenu(GroupItems(new List<Interpolable>(T._selectedInterpolables)), at.x, at.y + 24f);
                };

                sect = Sect();
                Cap(sect, "COLOUR", null);
                RectTransform swatches = Kit.Node("Swatches", sect);
                Kit.Row(swatches.gameObject, 4f);
                Kit.Size(swatches.gameObject, -1f, 22f);
                foreach (uint hex in _trackColors)
                {
                    Color c = Pal.Hex(hex);
                    bool on = ColorUtility.ToHtmlStringRGB(c) == ColorUtility.ToHtmlStringRGB(tr.color);
                    Image sw = Kit.Box("Sw", swatches, c, 3f);
                    sw.raycastTarget = true;
                    Kit.Size(sw.gameObject, 24f, 18f);
                    if (on)
                        Kit.Ring("On", sw.transform, Pal.C(0xE4E7EC), 3f, 2f);
                    Clickable click = sw.gameObject.AddComponent<Clickable>();
                    click.background = sw;
                    click.normal = click.hover = click.pressed = c;
                    click.onClick = () =>
                    {
                        T.RecordUndo("Track colour");
                        foreach (Interpolable t in T._selectedInterpolables)
                            t.color = c;
                        T.UpdateInterpolablesView();
                        Touch();
                    };
                }
                // Any other colour, from Studio's own picker.
                Clickable more = Pill(swatches, "More", 18f, 0f, 0f, Pal.C(0x25282D), Pal.C(0x373A3F), 3f);
                Kit.Size(more.gameObject, 24f, 18f);
                more.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                Kit.Icon("Icon", more.transform, "more", Pal.C(0x9A9DA2), 0.9f);
                more.tooltip = "Any colour";
                more.onClick = () => PickColour("Track colour", tr.color, c =>
                {
                    foreach (Interpolable t in T._selectedInterpolables)
                        t.color = c;
                    T.UpdateInterpolablesView();
                    ++_rowsVersion;
                    Touch();
                });

                sect = Sect();
                Cap(sect, "BEFORE FIRST / AFTER LAST KEY", null);
                Segw(sect, new[] { "Hold", "Linear", "Cycle", "Cycle +" }, (int)Extrap(tr.extrapolation), idx =>
                {
                    TrackExtrapolation mode = new[] { TrackExtrapolation.Hold, TrackExtrapolation.Linear, TrackExtrapolation.Cyclic, TrackExtrapolation.CyclicOffset }[idx];
                    T.SetExtrapolation(new List<Interpolable>(T._selectedInterpolables), mode);
                    Touch();
                });
                Note(sect, tr.extrapolation == TrackExtrapolation.Hold ? "Stays on the first and last value."
                         : tr.extrapolation == TrackExtrapolation.Linear ? "Keeps going in the direction of the end keys."
                         : tr.extrapolation == TrackExtrapolation.Cyclic ? "Repeats the keys forever, before and after."
                         : "Repeats, and each repeat starts where the last one ended.");

                sect = Sect();
                int count = tr.keyframes.Count;
                Cap(sect, "KEYS", count + (count == 1 ? " key" : " keys") + (count != 0 ? " · " + Fmt(tr.keyframes.Keys[0]) + " – " + Fmt(tr.keyframes.Keys[count - 1]) + " s" : ""));
                line = Line(sect);
                Btn(line, "Select all keys", true, () => T.SelectKeyframes(tr.keyframes.ToList()), null);
                Btn(line, "Push down to strip", true, () => T.PushDownToStrip(new List<Interpolable> { tr }), null);
                line = Line(sect);
                Btn(line, "Bake…", true, OpenBake, null);
                if (_splitsOfCombined.ContainsKey(SplitKey(tr.owner, tr.id)))
                    Btn(line, "Split into X / Y / Z", true, () => T.SplitTransformInterpolable(tr), null);
                else if (GetCombinedId(tr.owner, tr.id) != null)
                    Btn(line, "Merge X / Y / Z", true, () => T.MergeTransformInterpolables(tr), null);

                sect = Sect();
                line = Line(sect);
                // Applies to every selected track, so a batch switched off together comes back in one click.
                Cb(line, "Animated", tr.enabled, () =>
                {
                    bool on = !tr.enabled;
                    tr.enabled = on;
                    if (T._selectedInterpolables.Contains(tr))
                        foreach (Interpolable t in T._selectedInterpolables)
                            t.enabled = on;
                    Touch();
                });
                Cb(line, "In Graph", T._graphHiddenTracks.Contains(tr) == false, () => { if (T._graphHiddenTracks.Remove(tr) == false) T._graphHiddenTracks.Add(tr); Touch(); });
                Cb(line, "Locked", T._graphLockedTracks.Contains(tr), () => { if (T._graphLockedTracks.Remove(tr) == false) T._graphLockedTracks.Add(tr); Touch(); });
            }

            private static int Extrap(TrackExtrapolation e)
            {
                switch (e)
                {
                    case TrackExtrapolation.Linear: return 1;
                    case TrackExtrapolation.Cyclic: return 2;
                    case TrackExtrapolation.CyclicOffset: return 3;
                    default: return 0;
                }
            }
            #endregion

            #region Strip panel
            /// <summary>stripPanel(), over the plugin's own strips.</summary>
            private void StripPanel()
            {
                MotionStrip s = T._selectedStrips.Count == 0 ? null : T._selectedStrips[T._selectedStrips.Count - 1];
                if (s == null)
                {
                    EmptyPanel("No strip selected", "Click a strip in the NLA tab. To make one, select tracks and use Add › Push down, or the button in the Track tab.");
                    return;
                }
                int users = T.UsersOf(s.clip);
                NlaLane lane = T.StackOf(s.owner).lanes.Find(l => l.index == s.lane);
                PHead(Pal.Hex(0x9C7BE0), s.clip.name, (lane != null ? lane.DisplayName : "Track " + (s.lane + 1)) + " · " + s.clip.channels.Count + " track(s)" + (users > 1 ? " · clip played by " + users + " strips" : ""));

                RectTransform sect = Sect();
                Cap(sect, "PLACEMENT", null);
                RectTransform line = Line(sect);
                Fld(line, "Start", 0x9A9DA2, Fmt(s.start), "s", v => EditStrip(() => s.start = Parse(v, s.start)));
                Fld(line, "Speed", 0x9A9DA2, (1f / s.scale).ToString("0.00"), "×", v => EditStrip(() => { float sp = Parse(v, 1f / s.scale); if (sp > 0.01f) s.scale = 1f / sp; }));
                line = Line(sect);
                Fld(line, "Repeat", 0x9A9DA2, s.repeat.ToString(), null, v => EditStrip(() => s.repeat = Mathf.Max(1, Mathf.RoundToInt(Parse(v, s.repeat)))));

                // CLIP: which part of the clip plays, Blender's action extents.
                sect = Sect();
                Cap(sect, "CLIP", Fmt(s.clip.length) + " s long");
                line = Line(sect);
                Fld(line, "From", 0x9A9DA2, Fmt(s.effectiveClipStart), "s", v => EditStrip(() => s.clipStart = Mathf.Clamp(Parse(v, s.effectiveClipStart), 0f, s.effectiveClipEnd - 0.01f)));
                Fld(line, "To", 0x9A9DA2, Fmt(s.effectiveClipEnd), "s", v => EditStrip(() => s.clipEnd = Mathf.Clamp(Parse(v, s.effectiveClipEnd), s.effectiveClipStart + 0.01f, s.clip.length)));
                line = Line(sect);
                Btn(line, "Sync length", true, () => { T.SyncClipLength(s); Touch(); }, "The whole clip again, as long as its keys go");
                if (users > 1)
                    Btn(line, "Make single user", true, () => { T.MakeSingleUser(s); Touch(); }, "A copy of the clip for this strip alone, so editing it leaves the others be");

                sect = Sect();
                Cap(sect, "BLENDING", null);
                Segw(sect, new[] { "Replace", "Add", "Subtract", "Multiply" }, (int)s.blendMode, idx => EditStrip(() => s.blendMode = (StripBlendMode)idx));
                line = Line(sect);
                Kit.Text("L", line, "Influence", 11, Pal.C(0x9A9DA2)).gameObject.AddComponent<LayoutElement>().preferredWidth = 58f;
                float shown = s.InfluenceAt(T._playbackTime);
                // With influence keys the slider keys it at the playhead, as a keyed property does.
                Slider(line, shown, v =>
                {
                    if (s.influenceKeys.Count != 0)
                    {
                        T.KeyInfluence(s, T._playbackTime, v);
                        T.RefreshInterpolation();
                        Touch();
                    }
                    else
                        EditStrip(() => s.influence = v);
                });
                Kit.Text("V", line, Mathf.RoundToInt(shown * 100f) + " %", 11, Pal.C(0xE4E7EC), TextAnchor.MiddleRight).gameObject.AddComponent<LayoutElement>().preferredWidth = 36f;
                line = Line(sect);
                Btn(line, "◆ Key influence", true, () => { T.KeyInfluence(s, T._playbackTime, s.InfluenceAt(T._playbackTime)); T.RefreshInterpolation(); Touch(); }, "Key the influence at the playhead, to fade the strip over time");
                if (s.influenceKeys.Count != 0)
                    Btn(line, "Clear keys (" + s.influenceKeys.Count + ")", true, () => EditStrip(() => s.influenceKeys.Clear()), null);
                line = Line(sect);
                Fld(line, "Fade in", 0x9A9DA2, Fmt(s.blendIn), "s", v => EditStrip(() => s.blendIn = Mathf.Max(0f, Parse(v, s.blendIn))));
                Fld(line, "Fade out", 0x9A9DA2, Fmt(s.blendOut), "s", v => EditStrip(() => s.blendOut = Mathf.Max(0f, Parse(v, s.blendOut))));

                sect = Sect();
                line = Line(sect);
                Cb(line, "Reverse", s.reverse, () => EditStrip(() => s.reverse = !s.reverse));
                Cb(line, "Mute", s.enabled == false, () => EditStrip(() => s.enabled = !s.enabled));

                sect = Sect();
                line = Line(sect);
                Clickable tweak = Btn(line, T._tweakStrip == s ? "Finish editing clip keys" : "Edit clip keys", true, () =>
                {
                    if (T._tweakStrip == s)
                        T.ExitTweakMode();
                    else
                        T.EnterTweakMode(s);
                }, null);
                tweak.normal = Pal.accent;
                tweak.hover = Pal.C(0xF0B558);
                tweak.GetComponentInChildren<Text>().color = Pal.onAccent;
                tweak.Refresh();
            }

            private void EditStrip(Action change)
            {
                T.RecordUndo("Edit strip");
                change();
                T.RefreshInterpolation();
                T.UpdateGrid();
                Touch();
            }

            private static float Parse(string s, float fallback)
            {
                float v;
                return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : fallback;
            }
            #endregion

            #region Panel parts, as the stylesheet names them
            /// <summary>.phead: the swatch, the name in bold, a smaller line under it.</summary>
            private RectTransform PHead(Color swatch, string title, string small)
            {
                RectTransform head = Kit.Node("Phead", _pbody);
                Kit.Row(head.gameObject, 8f);
                Kit.Size(head.gameObject, -1f, 30f);
                Image sw = Kit.Box("Sw", head, swatch, 1f);
                Kit.Size(sw.gameObject, 4f, 28f);
                RectTransform texts = Kit.Node("Texts", head);
                Kit.Col(texts.gameObject, 4f);
                Kit.Flex(texts.gameObject);
                Text b = Kit.Text("B", texts, Kit.Escape(title), 13, Pal.C(0xE4E7EC), TextAnchor.MiddleLeft, true);
                b.horizontalOverflow = HorizontalWrapMode.Wrap;
                b.verticalOverflow = VerticalWrapMode.Truncate;
                Kit.Size(b.gameObject, -1f, 14f);
                Text s = Kit.Text("Small", texts, Kit.Escape(small), 11, Pal.C(0x9A9DA2));
                s.horizontalOverflow = HorizontalWrapMode.Wrap;
                s.verticalOverflow = VerticalWrapMode.Truncate;
                Kit.Size(s.gameObject, -1f, 12f);
                return head;
            }

            private static void Chip(RectTransform parent, string text)
            {
                Image chip = Kit.Box("Chip", parent, Pal.C(0x373A3F), 9f);
                Kit.Row(chip.gameObject, 0f, 8f, 8f);
                Kit.Size(chip.gameObject, -1f, 18f);
                Kit.Text("Text", chip.transform, text, 10, Pal.accent);
            }

            /// <summary>.sect: a column with a gap of 5.</summary>
            private RectTransform Sect()
            {
                RectTransform sect = Kit.Node("Sect", _pbody);
                Kit.Col(sect.gameObject, 5f);
                return sect;
            }

            /// <summary>.cap: the small caps caption, with a dimmer note on the right.</summary>
            private static RectTransform Cap(RectTransform parent, string text, string right)
            {
                RectTransform cap = Kit.Node("Cap", parent);
                Kit.Row(cap.gameObject, 8f);
                Kit.Size(cap.gameObject, -1f, 12f);
                Kit.Text("Text", cap, Tracked(text), 10, Pal.C(0x6B6E74));
                Kit.Spacer(cap);
                if (right != null)
                    Kit.Text("Right", cap, Kit.Escape(right), 10, Pal.C(0x9A9DA2), TextAnchor.MiddleRight);
                return cap;
            }

            /// <summary>letter-spacing .8px on the captions: a hair space between the letters.</summary>
            private static string Tracked(string text)
            {
                var b = new System.Text.StringBuilder();
                for (int i = 0; i < text.Length; ++i)
                {
                    b.Append(text[i]);
                    if (i < text.Length - 1 && text[i] != ' ')
                        b.Append(' ');
                }
                return b.ToString();
            }

            /// <summary>.line: a row with a gap of 4, 22 high.</summary>
            private static RectTransform Line(RectTransform parent)
            {
                RectTransform line = Kit.Node("Line", parent);
                Kit.Row(line.gameObject, 4f);
                Kit.Size(line.gameObject, -1f, 22f);
                return line;
            }

            private static void Note(RectTransform parent, string text)
            {
                Text note = Kit.Paragraph("Note", parent, text, 10, Pal.C(0x9A9DA2));
                note.lineSpacing = 1.2f;
            }

            /// <summary>.btn: 22 high, #373A3F, 11 pixel text; flex:1 when asked.</summary>
            private static Clickable Btn(RectTransform parent, string text, bool flex, Action act, string tip)
            {
                Clickable b = Pill(parent, "Btn", 22f, 9f, 5f, Pal.C(0x373A3F), Pal.C(0x44474C), 3f);
                b.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                if (flex)
                    Kit.Flex(b.gameObject, 22f);
                Kit.Text("Text", b.transform, Kit.Escape(text), 11, Pal.C(0xD5D8DD), TextAnchor.MiddleCenter);
                b.onClick = act;
                b.tooltip = tip;
                return b;
            }

            /// <summary>.fld: a dark field, an optional axis letter before the input and a unit after it.</summary>
            private static InputField Fld(RectTransform parent, string ax, uint axColor, string value, string unit, Action<string> commit)
            {
                Image box = Kit.Box("Fld", parent, Pal.C(0x15171B), 3f);
                Kit.Row(box.gameObject, 6f, 7f, 7f);
                Kit.Flex(box.gameObject, 22f);
                if (ax != null)
                {
                    bool plain = axColor == 0 || axColor == 0x9A9DA2;
                    Kit.Text("Ax", box.transform, ax, 10, plain ? Pal.C(0x9A9DA2) : Pal.Hex(axColor), TextAnchor.MiddleLeft, plain == false);
                }
                InputField field = Field(box.transform, "Input", value, 12, Pal.C(0xE4E7EC), -1f);
                if (unit != null)
                    Kit.Text("Unit", box.transform, unit, 10, Pal.C(0x9A9DA2));
                Image focus = Kit.Ring("Focus", box.transform, Pal.accent, 3f);
                focus.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                focus.gameObject.SetActive(false);
                FocusRing ring = field.gameObject.AddComponent<FocusRing>();
                ring.field = field;
                ring.ring = focus.gameObject;
                if (commit != null)
                    field.onEndEdit.AddListener(s => commit(s));
                return field;
            }

            /// <summary>.segw: a segmented choice, the picked one lifted.</summary>
            private static void Segw(RectTransform parent, string[] options, int current, Action<int> pick)
            {
                Image box = Kit.Box("Segw", parent, Pal.C(0x15171B), 4f);
                HorizontalLayoutGroup row = Kit.Row(box.gameObject, 2f, 2f, 2f);
                row.padding = new RectOffset(2, 2, 2, 2);
                Kit.Size(box.gameObject, -1f, 22f);
                for (int i = 0; i < options.Length; ++i)
                {
                    int captured = i;
                    bool on = i == current;
                    Clickable seg = Pill(box.transform, "Seg", 18f, 6f, 0f, on ? Pal.C(0x4B4D52) : new Color(0f, 0f, 0f, 0f), on ? Pal.C(0x4B4D52) : Pal.C(0x2C2F35), 3f);
                    seg.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                    Kit.Flex(seg.gameObject, 18f);
                    Text t = Kit.Text("Text", seg.transform, options[i], 11, on ? Pal.C(0xE4E7EC) : Pal.C(0x9A9DA2), TextAnchor.MiddleCenter);
                    seg.Tint(on ? Pal.C(0xE4E7EC) : Pal.C(0x9A9DA2), Pal.C(0xE4E7EC), t);
                    seg.onClick = () => pick(captured);
                }
            }

            /// <summary>.tile: one interpolation, drawn as its own little curve.</summary>
            private static Clickable Tile(RectTransform parent, Interp kind, bool on, string tip, Action act, EasingKind? family = null, EasingDirection direction = EasingDirection.In)
            {
                Image box = Kit.Box("Tile", parent, on ? Pal.C(0x33353B) : Pal.C(0x25282D), 4f);
                box.raycastTarget = true;
                Clickable c = box.gameObject.AddComponent<Clickable>();
                c.background = box;
                c.normal = on ? Pal.C(0x33353B) : Pal.C(0x25282D);
                c.hover = Pal.C(0x33353B);
                c.pressed = c.hover;
                c.tooltip = tip.Replace(" ▾", "");
                c.onClick = act;
                if (on)
                    Kit.Ring("On", box.transform, Pal.accent, 4f);
                RectTransform art = Kit.Node("Art", box.transform);
                art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
                art.sizeDelta = new Vector2(26f, 16f);
                Paint p = art.gameObject.AddComponent<Paint>();
                p.Begin();
                Color stroke = on ? Pal.accent : Pal.C(0xC9CDD3);
                // thumbPath(), in a 40 by 22 box drawn at 26 by 16.
                float sx = 26f / 40f, sy = 16f / 22f;
                if (kind == Interp.Custom && family.HasValue == false)
                {
                    p.Circle(12f * sx, 11f * sy, 2f * sx, Pal.C(0xC9CDD3));
                    p.Circle(20f * sx, 11f * sy, 2f * sx, Pal.C(0xC9CDD3));
                    p.Circle(28f * sx, 11f * sy, 2f * sx, Pal.C(0xC9CDD3));
                }
                else if (kind == Interp.Constant)
                {
                    var pts = new List<Vector2> { new Vector2(2f * sx, 19f * sy), new Vector2(38f * sx, 19f * sy), new Vector2(38f * sx, 4f * sy) };
                    p.Stroke(pts, 1.6f * sx, stroke, false);
                }
                else
                {
                    var pts = new List<Vector2>();
                    for (int i = 0; i <= 30; ++i)
                    {
                        float u = i / 30f;
                        float f = family.HasValue ? TimelineEasing.Evaluate(family.Value, direction, u) : InterpCurve(kind, u);
                        pts.Add(new Vector2((2f + u * 36f) * sx, (19f - f * 15f) * sy));
                    }
                    p.Stroke(pts, 1.6f * sx, stroke, false);
                }
                p.End();
                return c;
            }

            /// <summary>.cb: a 13 pixel box, filled with the accent when on.</summary>
            private static void Cb(RectTransform parent, string label, bool on, Action toggle)
            {
                Clickable cb = Pill(parent, "Cb", 22f, 0f, 6f, new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0f), 0f);
                Kit.Flex(cb.gameObject, 22f);
                Image box = Kit.Box("Box", cb.transform, on ? Pal.accent : Pal.C(0x15171B), 2f);
                Kit.Size(box.gameObject, 13f, 13f);
                if (on == false)
                    Kit.Ring("Ring", box.transform, Pal.C(0x3A3D43), 2f);
                else
                {
                    IconView check = Kit.Icon("Check", box.transform, "check", Pal.onAccent, 0.9f);
                    ((RectTransform)check.transform).Fill(1.5f, 1.5f, 1.5f, 1.5f);
                }
                Kit.Text("Text", cb.transform, label, 11, Pal.C(0xD5D8DD));
                cb.onClick = toggle;
            }

            /// <summary>The influence slider: a thin track, filled with the accent up to a round thumb.</summary>
            private void Slider(RectTransform parent, float value, Action<float> set)
            {
                RectTransform holder = Kit.Node("Slider", parent);
                Kit.Flex(holder.gameObject, 18f);
                Image hit = holder.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                RectTransform art = Kit.Node("Art", holder).Fill();
                SliderView view = art.gameObject.AddComponent<SliderView>();
                view.value = value;
                DragHandler drag = holder.gameObject.AddComponent<DragHandler>();
                Action<PointerEventData> apply = e =>
                {
                    Vector2 local;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(holder, e.position, null, out local);
                    Rect r = holder.rect;
                    view.value = Mathf.Clamp01((local.x - r.xMin - 6f) / Mathf.Max(1f, r.width - 12f));
                    view.Redraw();
                };
                holder.gameObject.AddComponent<PointerDownHandler>().onPointerDown = apply;
                drag.onDrag = apply;
                drag.onEndDrag = e => set(Mathf.Round(view.value * 100f) / 100f);
                holder.gameObject.AddComponent<PointerUpHandler>().onPointerUp = e => set(Mathf.Round(view.value * 100f) / 100f);
            }
            #endregion

            #region Floating properties
            /// <summary>floatProps(): the panel leaves the window for one of its own, and the grid takes the room.</summary>
            private void FloatProps()
            {
                float span = Tt(GridWidth()) - t0;
                _propsFloat = true;
                props = false;
                _propsMin = false;
                EnsurePWin();
                _props.SetParent(_pwinBody, false);
                _props.Fill();
                _props.gameObject.SetActive(true);
                _pwin.gameObject.SetActive(true);
                LayoutBody();
                _props.gameObject.SetActive(true);
                pps = GridWidth() / span;
                RenderTaskbar();
                Touch();
            }

            private void DockProps()
            {
                float span = Tt(GridWidth()) - t0;
                _propsFloat = false;
                _propsMin = false;
                props = true;
                _props.SetParent(_body, false);
                if (_pwin != null)
                    _pwin.gameObject.SetActive(false);
                LayoutBody();
                pps = GridWidth() / span;
                RenderTaskbar();
                Touch();
            }

            private RectTransform _pwinBody;

            /// <summary>.fwin: a floating window, 300 by 500, with a 22 pixel head to drag it by.</summary>
            private void EnsurePWin()
            {
                if (_pwin != null)
                    return;
                Image win = Kit.Box("Pwin", canvas.transform, Pal.C(0x2C2F35), 6f);
                win.raycastTarget = true;
                _pwin = win.rectTransform;
                _pwin.anchorMin = _pwin.anchorMax = new Vector2(0.5f, 0f);
                _pwin.pivot = new Vector2(0f, 1f);
                _pwin.sizeDelta = new Vector2(300f, 500f);
                _pwin.anchoredPosition = _savedPwin ?? _win.anchoredPosition + new Vector2(winW + 12f, 0f);
                win.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                Image head = Kit.Box("Fhead", _pwin, Pal.C(0x181A1E));
                head.raycastTarget = true;
                head.rectTransform.Css(0f, 0f, 0f, float.NaN, float.NaN, 22f);
                Kit.Box("Border", head.transform, Pal.C(0x111215)).rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f);
                RectTransform row = Kit.Node("Row", head.transform).Fill(8f, 0f, 3f, 1f);
                Kit.Row(row.gameObject, 2f);
                Kit.Icon("Icon", row, "side", Pal.C(0xE4E7EC));
                Kit.Text("Ft", row, "Properties", 11, Pal.C(0xE4E7EC), TextAnchor.MiddleLeft, true);
                Kit.Spacer(row);
                FB(row, "popout", "Dock back into the Timeline window", DockProps);
                FB(row, "min", "Minimise to the taskbar", () => { _propsMin = true; _pwin.gameObject.SetActive(false); RenderTaskbar(); });
                FB(row, "close", "Close", () => { _propsFloat = false; _pwin.gameObject.SetActive(false); _props.SetParent(_body, false); props = false; LayoutBody(); RenderTaskbar(); Touch(); });

                DragHandler drag = head.gameObject.AddComponent<DragHandler>();
                Vector2 start = Vector2.zero, mouse = Vector2.zero;
                drag.onBeginDrag = e =>
                {
                    start = _pwin.anchoredPosition;
                    mouse = e.position / canvas.scaleFactor;
                };
                drag.onDrag = e => _pwin.anchoredPosition = start + e.position / canvas.scaleFactor - mouse;

                _pwinBody = Kit.Node("Body", _pwin).Css(0f, 22f, 0f, 0f);
                Kit.Ring("Border", _pwin, Pal.C(0x0D0E10), 6f);
            }

            /// <summary>.fb: a 20 by 18 button on a floating window's head.</summary>
            private static void FB(RectTransform parent, string icon, string tip, Action act)
            {
                Clickable b = Pill(parent, "Fb", 18f, 0f, 0f, new Color(0f, 0f, 0f, 0f), Pal.C(0x373A3F), 3f);
                Kit.Size(b.gameObject, 20f, 18f);
                b.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                IconView view = Kit.Icon("Icon", b.transform, icon, Pal.C(0x9A9DA2));
                b.Tint(Pal.C(0x9A9DA2), Pal.C(0xE4E7EC), view);
                b.tooltip = tip;
                b.onClick = act;
            }

            /// <summary>.taskbar: a pill for each minimised window, above the Timeline window.</summary>
            private void RenderTaskbar()
            {
                if (_taskbar == null)
                {
                    _taskbar = Kit.Node("Taskbar", canvas.transform);
                    Kit.Row(_taskbar.gameObject, 4f);
                    ContentSizeFitter fit = _taskbar.gameObject.AddComponent<ContentSizeFitter>();
                    fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                _taskbar.SetAsLastSibling();
                for (int i = _taskbar.childCount - 1; i >= 0; --i)
                    UnityEngine.Object.Destroy(_taskbar.GetChild(i).gameObject);
                _taskbar.anchorMin = _taskbar.anchorMax = _win.anchorMin;
                _taskbar.pivot = new Vector2(0f, 0f);
                _taskbar.anchoredPosition = _win.anchoredPosition + new Vector2(8f, 6f);
                if (_propsFloat && _propsMin)
                {
                    Clickable pill = Pill(_taskbar, "Tpill", 22f, 10f, 6f, Pal.C(0x2C2F35), Pal.C(0x373A3F), 3f);
                    Kit.Ring("Border", pill.transform, Pal.C(0x0D0E10), 3f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    Kit.Icon("Icon", pill.transform, "side", Pal.C(0xE4E7EC));
                    Kit.Text("Text", pill.transform, "Properties", 11, Pal.C(0xE4E7EC));
                    pill.tooltip = "Restore";
                    pill.onClick = () =>
                    {
                        _propsMin = false;
                        _pwin.gameObject.SetActive(true);
                        RenderTaskbar();
                    };
                }
                AddThemePill();
                AddFloatPills();
            }
            #endregion
        }
    }

    /// <summary>The accent outline a field gets while it is being typed into: .fld:focus-within.</summary>
    internal class FocusRing : MonoBehaviour
    {
        public InputField field;
        public GameObject ring;

        private void Update()
        {
            bool on = field != null && field.isFocused;
            if (ring != null && ring.activeSelf != on)
                ring.SetActive(on);
        }
    }

    /// <summary>A range input with the accent colour, drawn: a 4 pixel track and a round thumb.</summary>
    internal class SliderView : Paint
    {
        public float value;

        public override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Redraw();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Redraw();
        }

        public void Redraw()
        {
            if (isActiveAndEnabled == false)
                return;
            float w = width, h = height, x = 6f + (w - 12f) * value;
            Begin();
            RoundRect(0f, h / 2f - 2f, w, 4f, 2f, Pal.C(0x44474C));
            RoundRect(0f, h / 2f - 2f, x, 4f, 2f, Pal.accent);
            Circle(x, h / 2f, 6f, Pal.accent);
            End();
        }
    }
}
