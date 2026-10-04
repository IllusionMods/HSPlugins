using System;
using System.Collections.Generic;
using System.Linq;
using Timeline.Graph;
using Timeline.Nla;
using Timeline.View;
using UILib.EventHandlers;
using UILib.ContextMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timeline
{
    /// <summary>One line of a menu, as the playground's menu items are written.</summary>
    internal sealed class MenuItem
    {
        public string label;
        public Action act;
        public bool check;
        public string kb;
        public List<MenuItem> sub;
        public bool sep;
        public string head;
        public bool disabled;
        public Color? dot;
    }

    public partial class Timeline
    {
        internal sealed partial class View
        {
            private RectTransform _menuLayer;
            private readonly List<RectTransform> _openMenus = new List<RectTransform>();
            private string _openMenuName;
            private RectTransform _menuBlocker;

            #region Opening and closing
            /// <summary>A menu hanging under a control on the top row, at y 24 as in the playground.</summary>
            private void OpenMenuUnder(Transform control, List<MenuItem> items, string name, bool alignRight = false)
            {
                bool wasOpen = _openMenuName == name;
                CloseMenu();
                if (wasOpen)
                    return;
                Vector2 local = WinPoint((RectTransform)control, alignRight ? 1f : 0f);
                float x = alignRight ? local.x - 210f : local.x;
                OpenMenu(items, x, 24f);
                _openMenuName = name;
                _headerDirty = true;
            }

            /// <summary>The top left or right corner of a control in window coordinates, y downwards.</summary>
            private Vector2 WinPoint(RectTransform control, float side)
            {
                Vector3 corner = control.TransformPoint(new Vector3(Mathf.Lerp(control.rect.xMin, control.rect.xMax, side), control.rect.yMax, 0f));
                Vector3 inWin = _win.InverseTransformPoint(corner);
                return new Vector2(inWin.x, -inWin.y);
            }

            /// <summary>
            /// Opens a menu at a point in window coordinates, kept inside the window unless it belongs to
            /// one of the floating windows around it.
            /// </summary>
            public void OpenMenu(List<MenuItem> items, float x, float y, bool keepInWindow = true)
            {
                CloseMenu();
                EnsureBlocker();
                RectTransform menu = BuildMenu(items, 0);
                LayoutRebuilder.ForceRebuildLayoutImmediate(menu);
                float w = menu.rect.width, h = menu.rect.height;
                if (keepInWindow)
                {
                    x = Mathf.Clamp(x, 2f, winW - w - 2f);
                    y = Mathf.Clamp(y, 2f, winH - h - 2f);
                }
                PlaceInWindow(menu, x, y);
                _flip = x + w * 2f > winW;
                _openMenus.Add(menu);
            }

            private bool _flip;

            /// <summary>Places a menu by window coordinates, although it lives on the canvas above the window.</summary>
            private void PlaceInWindow(RectTransform menu, float x, float y)
            {
                Vector3 world = _win.TransformPoint(new Vector3(x, -y, 0f));
                Vector3 local = _menuLayer.InverseTransformPoint(world);
                menu.anchorMin = menu.anchorMax = new Vector2(0.5f, 0.5f);
                menu.pivot = new Vector2(0f, 1f);
                menu.localPosition = new Vector3(Mathf.Round(local.x), Mathf.Round(local.y), 0f);
            }

            public void CloseMenu()
            {
                foreach (RectTransform menu in _openMenus)
                {
                    if (menu != null)
                        UnityEngine.Object.Destroy(menu.gameObject);
                }
                _openMenus.Clear();
                if (_menuBlocker != null)
                    _menuBlocker.gameObject.SetActive(false);
                if (_openMenuName != null)
                {
                    _openMenuName = null;
                    _headerDirty = true;
                }
            }

            /// <summary>A click anywhere outside an open menu closes it, and does nothing else.</summary>
            private void EnsureBlocker()
            {
                if (_menuBlocker == null)
                {
                    Image blocker = Kit.Box("Blocker", _menuLayer, new Color(0f, 0f, 0f, 0f));
                    blocker.raycastTarget = true;
                    _menuBlocker = blocker.rectTransform;
                    _menuBlocker.Fill(-4000f, -4000f, -4000f, -4000f);
                    PointerDownHandler down = blocker.gameObject.AddComponent<PointerDownHandler>();
                    down.onPointerDown = e => CloseMenu();
                }
                _menuBlocker.gameObject.SetActive(true);
                // Above everything, the floating properties window included.
                _menuLayer.SetAsLastSibling();
                _menuBlocker.SetAsLastSibling();
            }
            #endregion

            #region Drawing a menu
            /// <summary>.cmenu: #1E2025, a hairline #0D0E10 border, radius 4, 4 pixels above and below.</summary>
            private RectTransform BuildMenu(List<MenuItem> items, int depth)
            {
                Image box = Kit.Box("Menu", _menuLayer, Pal.C(0x1E2025), 4f);
                box.raycastTarget = true;
                RectTransform rect = box.rectTransform;
                VerticalLayoutGroup col = Kit.Col(box.gameObject, 0f, new RectOffset(0, 0, 4, 4));
                col.childForceExpandWidth = true;
                ContentSizeFitter fit = box.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                LayoutElement min = box.gameObject.AddComponent<LayoutElement>();
                min.minWidth = 200f;
                Image border = Kit.Ring("Border", rect, Pal.C(0x0D0E10), 4f);
                border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                foreach (MenuItem item in items)
                {
                    if (item.sep)
                    {
                        RectTransform sepRow = Kit.Node("Sep", rect);
                        Kit.Size(sepRow.gameObject, -1f, 9f);
                        Kit.Box("Line", sepRow, Pal.C(0x2E3137)).rectTransform.Css(0f, 4f, 0f, 4f);
                        continue;
                    }
                    if (item.head != null)
                    {
                        RectTransform headRow = Kit.Node("Head", rect);
                        Kit.Size(headRow.gameObject, -1f, 18f);
                        Text head = Kit.Text("Text", headRow, Spaced(item.head), 10, Pal.C(0x6B6E74), TextAnchor.LowerLeft);
                        head.rectTransform.Fill(24f, 0f, 10f, 3f);
                        continue;
                    }
                    BuildMenuItem(rect, item, depth);
                }
                border.transform.SetAsLastSibling();
                return rect;
            }

            /// <summary>Letter spacing .8px, which uGUI text does not have: thin spaces stand in for it.</summary>
            private static string Spaced(string text)
            {
                return text;
            }

            /// <summary>.it: 24 high, text from 24 in, the accent under the pointer.</summary>
            private void BuildMenuItem(RectTransform menu, MenuItem item, int depth)
            {
                Image row = Kit.Box("Item", menu, new Color(0f, 0f, 0f, 0f));
                row.raycastTarget = true;
                Kit.Row(row.gameObject, 8f, 24f, 10f);
                Kit.Size(row.gameObject, -1f, 24f);
                Clickable click = row.gameObject.AddComponent<Clickable>();
                click.background = row;
                click.normal = new Color(0f, 0f, 0f, 0f);
                click.hover = item.disabled ? click.normal : Pal.accent;
                click.pressed = click.hover;

                var tinted = new List<Graphic>();
                var dimmed = new List<Graphic>();
                if (item.check)
                {
                    IconView check = Kit.Icon("Check", row.transform, "check", Pal.accent, 0.9f);
                    check.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
                    ((RectTransform)check.transform).Css(8f, 7f, float.NaN, float.NaN, 10f, 10f);

                }
                if (item.dot.HasValue)
                {
                    RectTransform holder = Kit.Node("Dot", row.transform);
                    Kit.Size(holder.gameObject, 8f, 8f);
                    Paint dot = holder.gameObject.AddComponent<Paint>();
                    dot.Begin();
                    dot.Circle(4f, 4f, 4f, item.dot.Value);
                    dot.End();
                }
                Text label = Kit.Text("Label", row.transform, item.label, 12, item.disabled ? Pal.C(0x55585E) : Pal.C(0xD5D8DD));
                tinted.Add(label);
                if (item.kb != null || item.sub != null)
                    Kit.Spacer(row.transform);
                if (item.kb != null)
                {
                    Text kb = Kit.Text("Kb", row.transform, item.kb, 11, Pal.C(0x6B6E74));
                    RectTransform pad = Kit.Node("Pad", row.transform);
                    pad.SetSiblingIndex(kb.transform.GetSiblingIndex());
                    Kit.Size(pad.gameObject, 10f, 1f);
                    dimmed.Add(kb);
                }
                if (item.sub != null)
                {
                    RectTransform pad = Kit.Node("Pad", row.transform);
                    Kit.Size(pad.gameObject, 6f, 1f);
                    tinted.Add(Kit.Text("Arr", row.transform, "›", 12, Pal.C(0xD5D8DD)));
                }

                if (item.disabled == false)
                {
                    click.Tint(Pal.C(0xD5D8DD), Pal.onAccent, tinted.ToArray());
                    foreach (Graphic g in tinted)
                    {
                        IconView icon = g as IconView;
                        if (icon != null && item.check)
                            icon.SetColor(Pal.accent);
                    }
                    // The check keeps the accent until hovered; the shortcut stays dim until hovered.
                    MenuTint tint = row.gameObject.AddComponent<MenuTint>();
                    tint.click = click;
                    tint.check = item.check ? row.transform.Find("Check").GetComponent<IconView>() : null;
                    tint.dimmed = dimmed;
                }

                if (item.sub != null)
                {
                    List<MenuItem> sub = item.sub;
                    RectTransform rowRect = row.rectTransform;
                    PointerEnterHandler enter = row.gameObject.AddComponent<PointerEnterHandler>();
                    enter.onPointerEnter = e => OpenSubmenu(rowRect, sub, depth + 1);
                }
                else
                {
                    PointerEnterHandler enter = row.gameObject.AddComponent<PointerEnterHandler>();
                    enter.onPointerEnter = e => CloseSubmenus(depth + 1);
                    Action act = item.act;
                    if (item.disabled == false)
                    {
                        click.onClick = () =>
                        {
                            CloseMenu();
                            if (act != null)
                                act();
                            Touch();
                        };
                    }
                }
            }

            private void OpenSubmenu(RectTransform row, List<MenuItem> items, int depth)
            {
                CloseSubmenus(depth);
                RectTransform menu = BuildMenu(items, depth);
                LayoutRebuilder.ForceRebuildLayoutImmediate(menu);
                Vector2 right = WinPoint(row, 1f), left = WinPoint(row, 0f);
                float w = menu.rect.width, h = menu.rect.height;
                bool flip = _flip || right.x + w > winW;
                float x = flip ? left.x - w : right.x;
                float y = Mathf.Clamp(right.y - 5f, 2f, Mathf.Max(2f, winH - h - 2f));
                PlaceInWindow(menu, x, y);
                while (_openMenus.Count > depth)
                    _openMenus.RemoveAt(_openMenus.Count - 1);
                _openMenus.Add(menu);
            }

            private void CloseSubmenus(int depth)
            {
                while (_openMenus.Count > depth)
                {
                    RectTransform last = _openMenus[_openMenus.Count - 1];
                    if (last != null)
                        UnityEngine.Object.Destroy(last.gameObject);
                    _openMenus.RemoveAt(_openMenus.Count - 1);
                }
            }
            #endregion

            #region The playground's menus
            private List<MenuItem> MenuFor(string name)
            {
                switch (name)
                {
                    case "View": return ViewItems();
                    case "Select": return SelectItems();
                    case "Key": return KeyItems();
                    case "Channel": return ChannelItems();
                    case "Marker": return MarkerItems();
                    case "Strip": return StripMenuItems();
                    case "Add": return new List<MenuItem> { new MenuItem { label = "Push down selected tracks", act = () => T.PushDownToStrip(new List<Interpolable>(T._selectedInterpolables)) } };
                }
                return new List<MenuItem>();
            }

            /// <summary>The NLA's Strip menu: push tracks or keys down into a strip, and leave editing one.</summary>
            private List<MenuItem> StripMenuItems()
            {
                var items = new List<MenuItem>();
                var tracks = new List<Interpolable>(T._selectedInterpolables);
                items.Add(new MenuItem { label = "Push " + tracks.Count + " track(s) down to a strip", disabled = tracks.Count == 0, act = () => T.PushDownToStrip(tracks) });
                items.Add(new MenuItem { label = "Push " + T._selectedKeyframes.Count + " key(s) down to a strip", disabled = T._selectedKeyframes.Count == 0, act = () => T.PushDownSelectionToStrip() });
                if (T._tweakStrip != null)
                    items.Add(new MenuItem { label = "Leave tweak mode", act = () => T.ExitTweakMode() });
                items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem { label = T._strips.Count + " strip(s) on the timeline. Click one to edit it.", disabled = true });
                return items;
            }

            private List<MenuItem> ViewItems()
            {
                var items = new List<MenuItem>
                {
                    new MenuItem { label = "Fit all", kb = "Home", act = FitAll },
                    new MenuItem { label = "Frame selected", kb = "F", act = FrameSelected },
                    new MenuItem { sep = true },
                };
                if (editor == "graph")
                {
                    items.Add(new MenuItem { label = "Show handles", check = showHandles, act = () => showHandles = !showHandles });
                    items.Add(new MenuItem { label = "Normalize", check = normalize, act = () => normalize = !normalize });
                    items.Add(new MenuItem { sep = true });
                }
                items.Add(new MenuItem { label = "Compact rows", check = compact, act = () => compact = !compact });
                items.Add(new MenuItem { label = "Summary row", check = showSummary, act = () => showSummary = !showSummary });
                items.Add(new MenuItem
                {
                    label = "Keys on group rows",
                    sub = new List<MenuItem>
                    {
                        new MenuItem { label = "Only while the group is collapsed", check = groupKeys == "collapsed", act = () => groupKeys = "collapsed" },
                        new MenuItem { label = "Always", check = groupKeys == "always", act = () => groupKeys = "always" }
                    }
                });
                items.Add(new MenuItem
                {
                    label = "Ruler shows",
                    sub = new List<MenuItem>
                    {
                        new MenuItem { label = "Seconds", check = showFrames == false, act = () => { showFrames = false; Touch(); } },
                        new MenuItem { label = "Frames", check = showFrames, act = () => { showFrames = true; Touch(); } }
                    }
                });
                items.Add(new MenuItem
                {
                    label = "List tracks",
                    sub = new List<MenuItem>
                    {
                        new MenuItem { label = "By object", check = Grouped == false, act = () => SetListMode("object") },
                        new MenuItem { label = "As grouped", check = Grouped, act = () => SetListMode("tree") }
                    }
                });
                items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem { label = "Motion path in the scene", check = showPath, act = () => showPath = !showPath });
                items.Add(new MenuItem { label = "Picker dots in the scene", check = _showPickerNodes, act = () => { _showPickerNodes = !_showPickerNodes; RefreshFloats(); } });
                var ranges = new List<MenuItem>();
                foreach (float r in new[] { 0f, 1f, 2f })
                {
                    float captured = r;
                    ranges.Add(new MenuItem { label = r == 0f ? "Whole scene" : r + " s either side of the playhead", check = Mathf.Approximately(pathRange, r), act = () => pathRange = captured });
                }
                items.Add(new MenuItem { label = "Motion path length", sub = ranges });
                items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem { label = "Properties sidebar", check = props, act = () => SetProps(!props) });
                items.Add(new MenuItem { label = "Theme…", act = ToggleTheme });
                items.Add(new MenuItem { label = "Picker…", act = TogglePicker });
                items.Add(new MenuItem { label = "Key sets…", act = ToggleKeySets });
                items.Add(new MenuItem { label = "Rigs…", act = ToggleRigs });
                var fps = new List<MenuItem>();
                foreach (int f in new[] { 24, 25, 30, 60 })
                {
                    int captured = f;
                    fps.Add(new MenuItem { label = f + " fps", check = T._desiredFrameRate == f, act = () => T.UpdateDesiredFrameRate(captured.ToString()) });
                }
                items.Add(new MenuItem { label = "Frames per second", sub = fps });
                return items;
            }

            private List<MenuItem> SelectItems()
            {
                return new List<MenuItem>
                {
                    new MenuItem { label = "All", kb = "A", act = () => T.SelectAllKeyframesInScope() },
                    new MenuItem { label = "None", kb = "Alt+A", act = () => T.SelectKeyframes() },
                    new MenuItem { label = "Invert", kb = "Ctrl+I", act = InvertSelection },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Keys at the playhead", act = () => SelectWhere((t, k) => Mathf.Abs(FrameSnap(t) - FrameSnap(T._playbackTime)) < 1e-4f) },
                    new MenuItem { label = "Before the playhead", act = () => SelectWhere((t, k) => t < T._playbackTime) },
                    new MenuItem { label = "After the playhead", act = () => SelectWhere((t, k) => t > T._playbackTime) },
                    new MenuItem { sep = true },
                    new MenuItem
                    {
                        label = "Same key type as active",
                        act = () =>
                        {
                            if (T._selectedKeyframes.Count == 0)
                                return;
                            KeyframeKind kind = T._selectedKeyframes[T._selectedKeyframes.Count - 1].Value.kind;
                            SelectWhere((t, k) => k.kind == kind);
                        }
                    },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Key sets", sub = KeySetItems() }
                };
            }

            private void SelectWhere(Func<float, Keyframe, bool> test)
            {
                var found = new List<KeyValuePair<float, Keyframe>>();
                foreach (Row r in _rows)
                {
                    if (r.type != RowType.Track)
                        continue;
                    foreach (KeyValuePair<float, Keyframe> k in r.tr.keyframes)
                    {
                        if (test(k.Key, k.Value))
                            found.Add(k);
                    }
                }
                T.SelectKeyframes(found);
            }

            private void InvertSelection()
            {
                var selected = new HashSet<Keyframe>(T._selectedKeyframes.Select(k => k.Value));
                SelectWhere((t, k) => selected.Contains(k) == false);
            }

            private List<MenuItem> KeyItems()
            {
                bool any = T._selectedKeyframes.Count != 0;
                var interp = new List<MenuItem>
                {
                    new MenuItem { label = "Constant", act = () => SetInterp(Interp.Constant) },
                    new MenuItem { label = "Linear", act = () => SetInterp(Interp.Linear) },
                    new MenuItem { label = "Bézier", act = () => SetInterp(Interp.Bezier) },
                    new MenuItem { label = "Ease in", act = () => SetInterp(Interp.EaseIn) },
                    new MenuItem { label = "Ease out", act = () => SetInterp(Interp.EaseOut) },
                    new MenuItem { label = "Ease in-out", act = () => SetInterp(Interp.EaseInOut) },
                    new MenuItem { label = "Bounce", act = () => SetInterp(Interp.Bounce) },
                    new MenuItem { sep = true },
                };
                foreach (EasingKind kind in TimelineEasing.All)
                {
                    EasingKind captured = kind;
                    string name = EasingName(kind);
                    interp.Add(new MenuItem
                    {
                        label = name,
                        sub = new List<MenuItem>
                        {
                            new MenuItem { label = name + " in", act = () => ApplyFamily(captured, EasingDirection.In) },
                            new MenuItem { label = name + " out", act = () => ApplyFamily(captured, EasingDirection.Out) },
                            new MenuItem { label = name + " in-out", act = () => ApplyFamily(captured, EasingDirection.InOut) }
                        }
                    });
                }
                var types = new List<MenuItem>();
                foreach (KeyframeKind kind in new[] { KeyframeKind.Keyframe, KeyframeKind.Breakdown, KeyframeKind.MovingHold, KeyframeKind.Extreme, KeyframeKind.Jitter })
                {
                    KeyframeKind captured = kind;
                    types.Add(new MenuItem { label = KindName(kind), dot = KindFill(kind), act = () => T.SetKeyframeKind(captured) });
                }
                var handles = new List<MenuItem> { new MenuItem { head = "BOTH SIDES" } };
                var leftOnly = new List<MenuItem>();
                var rightOnly = new List<MenuItem>();
                foreach (HandleType type in new[] { HandleType.AutoClamped, HandleType.Auto, HandleType.Vector, HandleType.Aligned, HandleType.Free })
                {
                    HandleType captured = type;
                    handles.Add(new MenuItem { label = HandleName(type), dot = HandleColor(type), act = () => T.SetSelectedHandleType(captured) });
                    leftOnly.Add(new MenuItem { label = HandleName(type), dot = HandleColor(type), act = () => SetHandleSide(captured, true) });
                    rightOnly.Add(new MenuItem { label = HandleName(type), dot = HandleColor(type), act = () => SetHandleSide(captured, false) });
                }
                handles.Add(new MenuItem { sep = true });
                handles.Add(new MenuItem { label = "Left side only", sub = leftOnly });
                handles.Add(new MenuItem { label = "Right side only", sub = rightOnly });

                var keyItems = new List<MenuItem>();
                // A track that switches gets its values first, as the playground's Set value.
                KeyValuePair<float, Keyframe>? active = ActiveKey();
                if (active.HasValue && IsStep(active.Value.Value.parent))
                {
                    var values = new List<MenuItem>();
                    foreach (KeyValuePair<string, object> option in StepOptions(active.Value.Value.parent, active.Value.Value.value))
                    {
                        object value = option.Value;
                        values.Add(new MenuItem { label = option.Key, check = Equals(value, active.Value.Value.value), act = () => SetStepValue(value) });
                    }
                    if (values.Count != 0)
                    {
                        keyItems.Add(new MenuItem { label = "Set value", sub = values });
                        keyItems.Add(new MenuItem { sep = true });
                    }
                }
                keyItems.AddRange(new List<MenuItem>
                {
                    new MenuItem { label = "Insert key at playhead", kb = "I", act = () => T.KeySelectedTracks() },
                    new MenuItem { label = "Copy", kb = "Ctrl+C", act = () => T.CopyKeyframes(), disabled = any == false },
                    new MenuItem { label = "Paste at playhead", kb = "Ctrl+V", act = () => T.PasteKeyframes() },
                    new MenuItem { label = "Cut", kb = "Ctrl+X", act = () => { T.CutKeyframes(); Touch(); }, disabled = any == false },
                    new MenuItem { label = "Delete", kb = "X", act = () => DeleteKeys(), disabled = any == false },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Interpolation", sub = interp, disabled = any == false },
                    new MenuItem { label = "Key type", sub = types, disabled = any == false },
                    new MenuItem { label = "Handle type", sub = handles, disabled = any == false },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Scale spacing…", kb = "Alt+Wheel", act = OpenSpacing, disabled = T._selectedKeyframes.Count < 2 },
                    new MenuItem { label = "Snap selected keys to frames", act = () => T.SnapSelectedKeyframes(GraphSnap.Frame, false), disabled = any == false },
                    new MenuItem { label = "Smooth selected keys", act = () => { T.SmoothSelectedKeyframes(); Touch(); }, disabled = any == false },
                    new MenuItem { label = "Flatten their handles", act = () => { T.FlattenSelectedHandles(); Touch(); }, disabled = any == false },
                    new MenuItem
                    {
                        label = "Simplify",
                        disabled = any == false && T._selectedInterpolables.Count == 0,
                        sub = new List<MenuItem>
                        {
                            new MenuItem { head = "THROW AWAY KEYS THAT ADD NOTHING" },
                            new MenuItem { label = "Lightly", act = () => { T.SimplifySelection(SimplifyStrength.Light); Touch(); } },
                            new MenuItem { label = "Moderately", act = () => { T.SimplifySelection(SimplifyStrength.Medium); Touch(); } },
                            new MenuItem { label = "Strongly", act = () => { T.SimplifySelection(SimplifyStrength.Strong); Touch(); } }
                        }
                    },
                    new MenuItem { label = "Remove repeated keys, in every track", act = () => { T.CleanupRedundantKeys(); Touch(); } },
                    new MenuItem { sep = true }
                });
                // Values of the selected keys, as ShalltyUtils' keyframe panel had them.
                Keyframe activeKey = active.HasValue ? active.Value.Value : null;
                bool numeric = activeKey != null && CurveComponents.Count(activeKey.value) > 0 && activeKey.value is bool == false;
                keyItems.Add(new MenuItem { label = "Give the selected keys this key's value", disabled = activeKey == null || T._selectedKeyframes.Count < 2, act = () => { T.SetSelectedValues(activeKey.value); Touch(); } });
                keyItems.Add(new MenuItem { label = "Reset to the default value", disabled = activeKey == null, act = () => { T.SetSelectedValues(DefaultValue(activeKey.value)); Touch(); } });
                keyItems.Add(new MenuItem { label = "Add / subtract…", disabled = numeric == false, act = () => OpenOffset(activeKey.value) });
                keyItems.Add(new MenuItem { label = "Randomize…", disabled = any == false, act = OpenRandomize });
                keyItems.Add(new MenuItem { label = "Saved curves", disabled = activeKey == null, sub = activeKey == null ? null : SavedCurveItems(activeKey) });
                return keyItems;
            }

            private List<MenuItem> ChannelItems()
            {
                var tracks = new List<Interpolable>(T._selectedInterpolables);
                bool any = tracks.Count != 0;
                var extrap = new List<MenuItem>();
                foreach (var pair in new[]
                {
                    new KeyValuePair<TrackExtrapolation, string>(TrackExtrapolation.Hold, "Hold"),
                    new KeyValuePair<TrackExtrapolation, string>(TrackExtrapolation.Linear, "Linear"),
                    new KeyValuePair<TrackExtrapolation, string>(TrackExtrapolation.Cyclic, "Cycle"),
                    new KeyValuePair<TrackExtrapolation, string>(TrackExtrapolation.CyclicOffset, "Cycle with offset")
                })
                {
                    TrackExtrapolation mode = pair.Key;
                    extrap.Add(new MenuItem
                    {
                        label = pair.Value,
                        check = any && tracks.TrueForAll(t => t.extrapolation == mode),
                        act = () => T.SetExtrapolation(tracks, mode)
                    });
                }
                return new List<MenuItem>
                {
                    new MenuItem { label = "Expand all", act = () => { collapsed.Clear(); T.ExpandAllGroups(true); } },
                    new MenuItem { label = "Collapse all", act = CollapseAll },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Hide selected from Graph", kb = "H", act = () => { foreach (Interpolable t in tracks) T._graphHiddenTracks.Add(t); }, disabled = any == false },
                    new MenuItem { label = "Show all", kb = "Alt+H", act = () => T._graphHiddenTracks.Clear() },
                    new MenuItem
                    {
                        label = "Lock or unlock selected",
                        disabled = any == false,
                        act = () =>
                        {
                            bool lockThem = tracks.TrueForAll(T._graphLockedTracks.Contains) == false;
                            foreach (Interpolable t in tracks)
                            {
                                if (lockThem)
                                    T._graphLockedTracks.Add(t);
                                else
                                    T._graphLockedTracks.Remove(t);
                            }
                        }
                    },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Split into X / Y / Z", disabled = tracks.Count != 1, act = () => T.SplitTransformInterpolable(tracks[0]) },
                    new MenuItem { label = "Merge X / Y / Z", disabled = tracks.Count != 1, act = () => T.MergeTransformInterpolables(tracks[0]) },
                    new MenuItem { label = "Convert X / Y / Z to one curve", disabled = tracks.Any(CanConvertToCurve) == false, act = () => { T.ConvertTracksToCurves(tracks); Touch(); } },
                    new MenuItem { label = "Move X / Y / Z tracks onto folder constraints", disabled = Compat.NodesConstraintsLink.Available == false, act = () => { T.ConvertAxisTracksToFolders(); Touch(); } },
                    new MenuItem { label = "Save the selected tracks to a file…", disabled = any == false, act = () => { _filesOnlySelected = true; OpenFiles(); } },
                    new MenuItem { label = "Bake…", act = OpenBake },
                    new MenuItem { label = "Flipbook from meshes…", act = OpenFlipbook },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Before first / after last key", sub = extrap, disabled = any == false },
                    new MenuItem { label = "Push down to strip", disabled = any == false, act = () => T.PushDownToStrip(tracks) },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Solo selected", disabled = any == false, act = () => { T.SoloSelectedTracks(); Touch(); } },
                    new MenuItem { label = "Clear solo", disabled = T._soloInterpolables.Count == 0, act = () => { T.ClearSolo(); Touch(); } },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Copy the selected tracks' keys", disabled = any == false, act = () => T.CopySelectedTracks() },
                    new MenuItem { label = "Paste them onto the selected tracks", disabled = any == false, act = () => { T.PasteIntoSelectedTracks(); Touch(); } }
                };
            }

            private List<MenuItem> MarkerItems()
            {
                return new List<MenuItem>
                {
                    new MenuItem { label = "Add at playhead", kb = "M", act = () => T.AddMarker(T._playbackTime) },
                    new MenuItem { label = "Jump to next marker", act = () => T.SeekMarker(true) },
                    new MenuItem { label = "Jump to previous marker", act = () => T.SeekMarker(false) },
                    new MenuItem { sep = true },
                    new MenuItem
                    {
                        label = "Delete all markers",
                        disabled = T._markers.Count == 0,
                        act = () =>
                        {
                            T.RecordUndo("Delete markers");
                            T._markers.Clear();
                            T.UpdateMarkers();
                        }
                    }
                };
            }

            private void CollapseAll()
            {
                foreach (Row r in BuildRowList(true))
                {
                    if (r.type == RowType.Object)
                        collapsed.Add(r.key);
                }
                T.ExpandAllGroups(false);
            }

            /// <summary>Menus the Classic window already builds, shown in this window's style.</summary>
            private static List<MenuItem> FromContext(List<AContextMenuElement> elements)
            {
                var items = new List<MenuItem>();
                foreach (AContextMenuElement element in elements)
                {
                    LeafElement leaf = element as LeafElement;
                    GroupElement group = element as GroupElement;
                    if (leaf != null)
                    {
                        LeafElement captured = leaf;
                        items.Add(new MenuItem { label = leaf.text, act = () => { if (captured.onClick != null) captured.onClick(null); } });
                    }
                    else if (group != null)
                        items.Add(new MenuItem { label = group.text, sub = FromContext(group.elements) });
                }
                return items;
            }
            #endregion

            #region Names and colours shared with the grid
            public static string KindName(KeyframeKind kind)
            {
                switch (kind)
                {
                    case KeyframeKind.Breakdown: return "Breakdown";
                    case KeyframeKind.MovingHold: return "Moving hold";
                    case KeyframeKind.Extreme: return "Extreme";
                    case KeyframeKind.Jitter: return "Jitter";
                    default: return "Keyframe";
                }
            }

            /// <summary>KFILL: the plain keyframe follows the theme, the others mean something and do not.</summary>
            public static Color KindFill(KeyframeKind kind)
            {
                switch (kind)
                {
                    case KeyframeKind.Breakdown: return Pal.Hex(0x4FC3D9);
                    case KeyframeKind.MovingHold: return Pal.Hex(0x8A8F99);
                    case KeyframeKind.Extreme: return Pal.Hex(0xE86BAE);
                    case KeyframeKind.Jitter: return Pal.Hex(0xD9C24F);
                    default: return KeyframeColor;
                }
            }

            /// <summary>KSIDE: how big each kind is drawn.</summary>
            public static float KindSide(KeyframeKind kind)
            {
                switch (kind)
                {
                    case KeyframeKind.Breakdown: return 7f;
                    case KeyframeKind.Extreme: return 12f;
                    case KeyframeKind.Jitter: return 6f;
                    default: return 10f;
                }
            }

            public static string HandleName(HandleType type)
            {
                switch (type)
                {
                    case HandleType.AutoClamped: return "Auto-clamped";
                    case HandleType.Auto: return "Auto";
                    case HandleType.Vector: return "Vector";
                    case HandleType.Aligned: return "Aligned";
                    default: return "Free";
                }
            }

            public static Color HandleColor(HandleType type)
            {
                switch (type)
                {
                    case HandleType.AutoClamped: return Pal.Hex(0xE8A08A);
                    case HandleType.Auto: return Pal.Hex(0xE8C34A);
                    case HandleType.Vector: return Pal.Hex(0x82CC63);
                    case HandleType.Aligned: return Pal.Hex(0xE86BAE);
                    default: return Pal.Hex(0xE4E7EC);
                }
            }

            public static string EasingName(EasingKind kind)
            {
                switch (kind)
                {
                    case EasingKind.Quadratic: return "Quad";
                    case EasingKind.Cubic: return "Cubic";
                    case EasingKind.Quartic: return "Quart";
                    case EasingKind.Quintic: return "Quint";
                    case EasingKind.Exponential: return "Expo";
                    case EasingKind.Circular: return "Circ";
                    default: return kind.ToString();
                }
            }
            #endregion

            #region Tooltip
            private static RectTransform _tipBox;
            private static Text _tipText;

            private void BuildTooltip()
            {
                Image box = Kit.Box("Tooltip", canvas.transform, Pal.C(0x1E2025), 3f);
                _tipBox = box.rectTransform;
                _tipBox.anchorMin = _tipBox.anchorMax = Vector2.zero;
                _tipBox.pivot = new Vector2(0f, 1f);
                Kit.Row(box.gameObject, 0f, 7f, 7f);
                ContentSizeFitter fit = box.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                box.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(7, 7, 4, 4);
                _tipText = Kit.Text("Text", box.transform, "", 11, Pal.C(0xD5D8DD));
                Kit.Ring("Border", box.transform, Pal.C(0x0D0E10), 3f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                box.gameObject.SetActive(false);
            }

            private void TickTooltip()
            {
                if (_tipBox == null)
                    return;
                bool show = Tooltip.text != null && Time.unscaledTime - Tooltip.since > 0.5f;
                if (show != _tipBox.gameObject.activeSelf)
                    _tipBox.gameObject.SetActive(show);
                if (show == false)
                    return;
                if (_tipText.text != Tooltip.text)
                    _tipText.text = Tooltip.text;
                _tipBox.SetAsLastSibling();
                Vector2 p = Input.mousePosition / canvas.scaleFactor;
                _tipBox.anchoredPosition = new Vector2(Mathf.Round(p.x + 12f), Mathf.Round(p.y - 18f));
            }
            #endregion
        }
    }

    /// <summary>What a pointer is over, for the one tooltip the view draws.</summary>
    internal static class Tooltip
    {
        public static string text;
        public static float since;

        public static void Show(string value)
        {
            text = value;
            since = Time.unscaledTime;
        }

        public static void Hide()
        {
            text = null;
        }
    }

    /// <summary>A menu line's secondary colours: the check stays the accent and the shortcut stays dim until hovered.</summary>
    internal class MenuTint : MonoBehaviour
    {
        public Clickable click;
        public IconView check;
        public List<Graphic> dimmed;
        private bool _last;

        private void Update()
        {
            bool over = click != null && click.isOver;
            if (over == _last)
                return;
            _last = over;
            if (check != null)
                check.SetColor(over ? Pal.onAccent : Pal.accent);
            if (dimmed != null)
            {
                foreach (Graphic g in dimmed)
                    g.color = over ? Pal.onAccent : Pal.C(0x6B6E74);
            }
        }
    }
}
