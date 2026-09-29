using System;
using System.Collections.Generic;
using Studio;
using Timeline.Graph;
using Timeline.Nla;
using Timeline.View;
using UILib.EventHandlers;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            private RectTransform _hdr;
            private RectTransform _hdrRow;
            private RectTransform _transport;
            private IconView _playIcon;
            private Image _playBg;
            private InputField _tc;
            private Text _durText;

            private static readonly string[] _editorNames = { "dope", "graph", "nla" };

            private static string EditorName(string editor)
            {
                return editor == "graph" ? "Graph" : editor == "nla" ? "NLA" : "Dope Sheet";
            }

            /// <summary>.hdr: one row, 26 high, #2C2F35 with a hairline under it. It is also what the window is dragged by.</summary>
            private void BuildHeader()
            {
                Image bg = Kit.Box("Hdr", _win, Pal.C(0x2C2F35));
                bg.raycastTarget = true;
                _hdr = bg.rectTransform.Css(0f, 0f, 0f, float.NaN, float.NaN, HDR);
                Kit.Box("Border", _hdr, Pal.C(0x111215)).rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f);

                DragHandler drag = _hdr.gameObject.AddComponent<DragHandler>();
                Vector2 startPos = Vector2.zero, startMouse = Vector2.zero;
                drag.onBeginDrag = e =>
                {
                    startPos = _win.anchoredPosition;
                    startMouse = e.position / canvas.scaleFactor;
                };
                drag.onDrag = e =>
                {
                    _win.anchoredPosition = startPos + e.position / canvas.scaleFactor - startMouse;
                    LayoutShadow();
                };

                // padding: 0 4px; gap: 2px, over the 25 pixels above the border.
                _hdrRow = Kit.Node("Row", _hdr).Fill(4f, 0f, 4f, 1f);
                Kit.Row(_hdrRow.gameObject, 2f);

                BuildTransport();
            }

            /// <summary>Everything on the row except playback, which is placed on its own afterwards.</summary>
            private void RenderHeader()
            {
                _headerDirty = false;
                for (int i = _hdrRow.childCount - 1; i >= 0; --i)
                    UnityEngine.Object.Destroy(_hdrRow.GetChild(i).gameObject);
                _hdrRow.DetachChildren();

                // .edsel
                Clickable edsel = Pill(_hdrRow, "Edsel", 20f, 7f, 5f, Pal.C(0x373A3F), Pal.C(0x44474C), 3f);
                edsel.GetComponent<LayoutElement>().minWidth = 0f;
                Kit.Icon("Icon", edsel.transform, editor, Pal.accent);
                Kit.Text("Text", edsel.transform, EditorName(editor), 11, Pal.C(0xE4E7EC));
                Kit.Icon("Down", edsel.transform, "down", Pal.C(0xE4E7EC));
                edsel.tooltip = "Switch editor";
                SetMargin(edsel.gameObject, 4f);
                Transform edselT = edsel.transform;
                edsel.onClick = () =>
                {
                    var items = new List<MenuItem>();
                    foreach (string e in _editorNames)
                    {
                        string captured = e;
                        items.Add(new MenuItem { label = EditorName(e), check = editor == e, act = () => SetEditor(captured) });
                    }
                    OpenMenuUnder(edselT, items, "@ed");
                };

                // .menus
                string[] menus = editor == "graph" ? new[] { "View", "Select", "Key", "Channel" }
                               : editor == "nla" ? new[] { "View", "Select", "Strip", "Add" }
                               : new[] { "View", "Select", "Key", "Channel", "Marker" };
                foreach (string name in menus)
                {
                    Clickable menu = Pill(_hdrRow, name, 20f, 7f, 0f, new Color(0f, 0f, 0f, 0f), Pal.C(0x3A3D43), 3f);
                    Kit.Text("Text", menu.transform, name, 12, Pal.C(0xD5D8DD));
                    string captured = name;
                    Transform t = menu.transform;
                    menu.onClick = () => OpenMenuUnder(t, MenuFor(captured), captured);
                    if (_openMenuName == name)
                        menu.normal = Pal.C(0x3A3D43);
                    menu.Refresh();
                }
                if (T._tweakStrip != null)
                {
                    Clickable pill = Pill(_hdrRow, "Tweak", 20f, 7f, 0f, Pal.accent, Pal.accent, 3f);
                    Kit.Text("Text", pill.transform, "Done editing clip keys", 12, Pal.onAccent, TextAnchor.MiddleLeft, true);
                    pill.tooltip = "Put the keys back into the clip";
                    pill.onClick = () => T.ExitTweakMode();
                    SetMargin(pill.gameObject, 6f);
                }

                Kit.Spacer(_hdrRow);

                // .right, gap 3
                RectTransform right = Kit.Node("Right", _hdrRow);
                Kit.Row(right.gameObject, 3f);
                Kit.Size(right.gameObject, -1f, 20f);

                Tog(right, "Path", "path", "Path", showPath, false, "Show the path of the selected tracks in the scene: faint is played, bright is to come, a dot per frame (bunched up is slow), the ring is where it is now", () => { showPath = !showPath; Touch(); });
                string snapName = snap == "second" ? "Second" : snap == "marker" ? "Marker" : snap == "off" ? "Off" : "Frame";
                Clickable snapTog = null;
                snapTog = Tog(right, "Snap", "magnet", snapName, snap != "off", true, "What dragging snaps to. Shift flips it while dragging.", () => OpenMenuUnder(snapTog.transform, SnapItems(), "@snap"));
                if (editor == "graph")
                {
                    Tog(right, "Handles", null, "Handles", showHandles, false, null, () => { showHandles = !showHandles; Touch(); });
                    Tog(right, "Normalize", null, "Normalize", normalize, false, null, () => { normalize = !normalize; Touch(); });
                }
                VSep(right);
                IBtn(right, "Theme", "palette", "Theme: colours and opacity", ThemeShown(), true, ToggleTheme);
                bool propsShown = props || _propsFloat && _propsMin == false;
                Tog(right, "Properties", "side", "Properties", propsShown, false, "Show or hide the properties sidebar", () => SetProps(!propsShown));
                VSep(right);
                IBtn(right, "Undo", "undo", "Undo (Ctrl+Z)", false, T._undoStack.Count != 0, () => T.Undo());
                IBtn(right, "Redo", "redo", "Redo (Ctrl+Shift+Z)", false, T._redoStack.Count != 0, () => T.Redo());
                Clickable more = null;
                more = IBtn(right, "More", "more", "More", false, true, () => OpenMenuUnder(more.transform, MoreItems(), "@more", true));
                IBtn(right, "Close", "close", "Hide Timeline", false, true, () => T.ToggleUiVisible());

                LayoutRebuilder.ForceRebuildLayoutImmediate(_hdrRow);
                PlaceTransport();
            }

            /// <summary>
            /// .transport: centred in the free space between the menus and the toggles, which changes
            /// width per editor. Never over either of them.
            /// </summary>
            private void PlaceTransport()
            {
                float a = 0f, b = _hdr.rect.width;
                RectTransform spacer = null, right = null;
                for (int i = 0; i < _hdrRow.childCount; ++i)
                {
                    RectTransform child = (RectTransform)_hdrRow.GetChild(i);
                    if (child.name == "Spacer")
                        spacer = child;
                    else if (child.name == "Right")
                        right = child;
                }
                if (spacer != null)
                    a = 4f + spacer.anchoredPosition.x - spacer.rect.width * spacer.pivot.x;
                if (right != null)
                    b = 4f + right.anchoredPosition.x - right.rect.width * right.pivot.x;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_transport);
                float w = _transport.rect.width;
                float x = Mathf.Round(Mathf.Clamp((a + b) / 2f - w / 2f, a + 6f, Mathf.Max(a + 6f, b - w - 6f)));
                _transport.anchoredPosition = new Vector2(x, -3f);
            }

            private void BuildTransport()
            {
                _transport = Kit.Node("Transport", _hdr);
                _transport.anchorMin = _transport.anchorMax = new Vector2(0f, 1f);
                _transport.pivot = new Vector2(0f, 1f);
                Kit.Row(_transport.gameObject, 4f);
                ContentSizeFitter fit = _transport.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // .tgrp
                Image group = Kit.Box("Tgrp", _transport, Pal.C(0x1B1D21), 3f);
                Kit.Row(group.gameObject, 0f);
                Kit.Size(group.gameObject, 110f, 20f);
                Tb2(group.transform, "start", "Go to start", () => SetTime(0f));
                Tb2(group.transform, "prevkey", "Previous key (Down)", () => T.JumpToKey(-1));
                Clickable play = Tb2(group.transform, "playT", "Play (Space)", TogglePlay);
                _playIcon = play.GetComponentInChildren<IconView>();
                _playBg = play.background;
                Tb2(group.transform, "nextkey", "Next key (Up)", () => T.JumpToKey(1));
                Tb2(group.transform, "end", "Go to end", () => SetTime(T._duration));

                // .tc, with the time and the length after it
                Image tc = Kit.Box("Tc", _transport, Pal.C(0x1B1D21), 3f);
                Kit.Row(tc.gameObject, 6f, 6f, 6f);
                Kit.Size(tc.gameObject, -1f, 20f);
                _tc = Field(tc.transform, "Time", "", 12, Pal.C(0xE4E7EC), 58f);
                _tc.onEndEdit.AddListener(text =>
                {
                    float t;
                    if (TryParseTime(text, out t))
                        SetTime(t);
                });
                Clickable dur = Pill(tc.transform, "Dur", 14f, 3f, 0f, new Color(0f, 0f, 0f, 0f), Pal.C(0x2C2F35), 2f);
                _durText = Kit.Text("Text", dur.transform, "/ 10.00", 10, Pal.C(0x6B6E74));
                dur.Tint(Pal.C(0x6B6E74), Pal.C(0xE4E7EC), _durText);
                dur.tooltip = "Scene length and playback speed";
                dur.onClick = () => T.OpenTimingPanel();

                // .keyb: ◆ Key, and ▾ for only part of the node selected in the scene
                RectTransform keyb = Kit.Node("Keyb", _transport);
                Kit.Row(keyb.gameObject, 0f);
                Kit.Size(keyb.gameObject, -1f, 20f);
                Kit.Ring("Ring", keyb, Pal.accent, 3f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Clickable key = Pill(keyb, "Key", 20f, 7f, 4f, new Color(0f, 0f, 0f, 0f), Pal.accent, 3f);
                IconView diamond = Kit.Icon("Icon", key.transform, "dope", Pal.accent, 0.75f);
                Text keyText = Kit.Text("Text", key.transform, "Key", 11, Pal.accent);
                key.Tint(Pal.accent, Pal.onAccent, diamond, keyText);
                key.tooltip = "Key the selected tracks at the playhead, or the node selected in the scene (I)";
                key.onClick = () => T.KeySelectedTracks();
                Clickable drop = Pill(keyb, "Kdrop", 20f, 4f, 0f, new Color(0f, 0f, 0f, 0f), Pal.accent, 3f);
                Image line = Kit.Box("Sep", drop.transform, Pal.Accent(0.45f));
                line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                line.rectTransform.Css(0f, 0f, float.NaN, 0f, 1f);
                IconView chevron = Kit.Icon("Icon", drop.transform, "down", Pal.accent);
                drop.Tint(Pal.accent, Pal.onAccent, chevron);
                drop.tooltip = "Key only part of the node selected in the scene";
                drop.onClick = () => OpenMenuUnder(drop.transform, KeyNodeItems(), "@key");
            }

            /// <summary>keyNodeItems(): all of the node, or one of position, rotation and scale, or one axis of it.</summary>
            private List<MenuItem> KeyNodeItems()
            {
                List<GuideObject> nodes = T.SelectedNodes();
                if (nodes.Count == 0)
                    return new List<MenuItem> { new MenuItem { head = "KEY PART OF A NODE" }, new MenuItem { label = "Select a node in the scene first", disabled = true } };
                var items = new List<MenuItem>
                {
                    new MenuItem { head = nodes.Count == 1 ? "KEY " + nodes[0].transformTarget.name.ToUpperInvariant() : "KEY " + nodes.Count + " NODES" },
                    new MenuItem { label = "All of it", kb = "I", act = () => { T.KeyNode(-1, -1); Touch(); } },
                    new MenuItem { sep = true }
                };
                for (int p = 0; p < 3; ++p)
                {
                    int prop = p;
                    if (nodes.TrueForAll(n => NodeHas(n, p) == false))
                    {
                        items.Add(new MenuItem { label = _nodePropNames[p], disabled = true });
                        continue;
                    }
                    var sub = new List<MenuItem>
                    {
                        new MenuItem { label = "X, Y and Z", act = () => { T.KeyNode(prop, -1); Touch(); } },
                        new MenuItem { sep = true }
                    };
                    for (int a = 0; a < 3; ++a)
                    {
                        int axis = a;
                        sub.Add(new MenuItem { label = "XYZ"[a] + " only", act = () => { T.KeyNode(prop, axis); Touch(); } });
                    }
                    items.Add(new MenuItem { label = _nodePropNames[p], sub = sub });
                }
                items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem { label = "One axis is keyed on its own track", disabled = true });
                items.Add(new MenuItem { label = "Control + click selects several nodes", disabled = true });
                return items;
            }

            private void TickHeader()
            {
                if (_playIcon != null)
                {
                    _playIcon.Set(T._isPlaying ? "pauseT" : "playT", T._isPlaying ? Pal.onAccent : Pal.C(0xC9CDD3), 1f);
                    Clickable play = _playIcon.transform.parent.GetComponent<Clickable>();
                    Color normal = T._isPlaying ? Pal.accent : new Color(0f, 0f, 0f, 0f);
                    if (play.normal != normal)
                    {
                        play.normal = normal;
                        play.hover = T._isPlaying ? Pal.accent : Pal.C(0x3A3D43);
                        play.tintNormal = play.tintHover = T._isPlaying ? Pal.onAccent : Pal.C(0xC9CDD3);
                        play.tooltip = T._isPlaying ? "Pause (Space)" : "Play (Space)";
                        play.Refresh();
                    }
                }
                if (_tc != null && _tc.isFocused == false)
                {
                    string text = Timecode(T._playbackTime);
                    if (_tc.text != text)
                        _tc.text = text;
                }
                if (_durText != null)
                {
                    string d = "/ " + Fmt(T._duration) + (Mathf.Approximately(Time.timeScale, 1f) ? "" : " · " + Time.timeScale.ToString("0.##") + "×");
                    if (_durText.text != d)
                        _durText.text = d;
                }
            }

            private static bool TryParseTime(string text, out float t)
            {
                text = text.Trim();
                int colon = text.IndexOf(':');
                float m = 0f, s;
                if (colon >= 0)
                {
                    if (float.TryParse(text.Substring(0, colon), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out m) == false)
                    {
                        t = 0f;
                        return false;
                    }
                    text = text.Substring(colon + 1);
                }
                bool ok = float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out s);
                t = m * 60f + s;
                return ok;
            }

            #region Parts
            /// <summary>A box that is a button: height, horizontal padding, gap, fill and hover fill.</summary>
            private static Clickable Pill(Transform parent, string name, float height, float pad, float gap, Color bg, Color bgHover, float radius)
            {
                Image box = Kit.Box(name, parent, bg, radius);
                box.raycastTarget = true;
                Kit.Row(box.gameObject, gap, pad, pad);
                Kit.Size(box.gameObject, -1f, height);
                Clickable click = box.gameObject.AddComponent<Clickable>();
                click.background = box;
                click.normal = bg;
                click.hover = bgHover;
                click.pressed = bgHover;
                return click;
            }

            private static void SetMargin(GameObject go, float right)
            {
                // margin-right, which a layout group does not have: a sized gap after the element.
                RectTransform gap = Kit.Node("Margin", go.transform.parent);
                gap.SetSiblingIndex(go.transform.GetSiblingIndex() + 1);
                Kit.Size(gap.gameObject, right - 2f, 1f);
            }

            /// <summary>.tog: 20 high, #25282D, dim text; on, it is filled with the accent.</summary>
            private Clickable Tog(Transform parent, string name, string icon, string label, bool on, bool chevron, string tip, Action act)
            {
                Color bg = on ? Pal.accent : Pal.C(0x25282D);
                Clickable tog = Pill(parent, name, 20f, 8f, 5f, bg, bg, 3f);
                Color fg = on ? Pal.onAccent : Pal.C(0x9A9DA2), fgHover = on ? Pal.onAccent : Pal.C(0xE4E7EC);
                var tinted = new List<Graphic>();
                if (icon != null)
                    tinted.Add(Kit.Icon("Icon", tog.transform, icon, fg));
                if (narrow == false || icon == null)
                    tinted.Add(Kit.Text("Text", tog.transform, label, 11, fg));
                if (chevron)
                    tinted.Add(Kit.Icon("Down", tog.transform, "down", fg));
                tog.Tint(fg, fgHover, tinted.ToArray());
                tog.tooltip = tip;
                tog.onClick = act;
                return tog;
            }

            /// <summary>.win.narrow hides the words on the toggles and keeps their icons.</summary>
            private bool narrow { get { return winW < 980f; } }

            /// <summary>.ibtn.ghost: 22 by 20, an icon on nothing until hovered.</summary>
            private static Clickable IBtn(Transform parent, string name, string icon, string tip, bool on, bool enabled, Action act)
            {
                Clickable b = Pill(parent, name, 20f, 0f, 0f, new Color(0f, 0f, 0f, 0f), Pal.C(0x2C2F35), 4f);
                Kit.Size(b.gameObject, 22f, 20f);
                b.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                IconView view = Kit.Icon("Icon", b.transform, icon, on ? Pal.accent : Pal.C(0x9A9DA2));
                b.Tint(on ? Pal.accent : Pal.C(0x9A9DA2), on ? Pal.accent : Pal.C(0xE4E7EC), view);
                if (enabled == false)
                {
                    b.disabled = true;
                    CanvasGroup g = b.gameObject.AddComponent<CanvasGroup>();
                    g.alpha = 0.35f;
                    g.blocksRaycasts = false;
                }
                b.tooltip = tip;
                b.onClick = act;
                return b;
            }

            private static void VSep(Transform parent)
            {
                // width 1, height 18, margin 0 4: the gap of 3 either side makes the rest.
                RectTransform left = Kit.Node("Gap", parent);
                Kit.Size(left.gameObject, 1f, 1f);
                Image sep = Kit.Box("Vsep", parent, Pal.C(0x3A3D43));
                Kit.Size(sep.gameObject, 1f, 18f);
                RectTransform right = Kit.Node("Gap", parent);
                Kit.Size(right.gameObject, 1f, 1f);
            }

            /// <summary>.tb2: 22 by 20, a 10 pixel icon.</summary>
            private static Clickable Tb2(Transform parent, string icon, string tip, Action act)
            {
                Clickable b = Pill(parent, icon, 20f, 0f, 0f, new Color(0f, 0f, 0f, 0f), Pal.C(0x3A3D43), 0f);
                Kit.Size(b.gameObject, 22f, 20f);
                b.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                IconView view = Kit.Icon("Icon", b.transform, icon, Pal.C(0xC9CDD3));
                b.Tint(Pal.C(0xC9CDD3), Pal.C(0xC9CDD3), view);
                b.tooltip = tip;
                b.onClick = act;
                return b;
            }

            /// <summary>A borderless text input: the playground's input inside a .fld or .tc.</summary>
            internal static InputField Field(Transform parent, string name, string text, int size, Color color, float width)
            {
                RectTransform rect = Kit.Node(name, parent);
                Image hit = rect.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                if (width >= 0f)
                    Kit.Size(rect.gameObject, width, size + 6f);
                else
                    Kit.Flex(rect.gameObject, size + 6f);
                Text label = Kit.Text("Text", rect, text, size, color);
                label.supportRichText = false;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.rectTransform.Fill();
                InputField field = rect.gameObject.AddComponent<InputField>();
                field.textComponent = label;
                field.targetGraphic = hit;
                field.transition = Selectable.Transition.None;
                field.lineType = InputField.LineType.SingleLine;
                field.caretColor = color;
                field.selectionColor = Pal.Accent(0.45f);
                field.customCaretColor = true;
                field.text = text;
                return field;
            }
            #endregion

            #region Menus of the row
            private List<MenuItem> SnapItems()
            {
                var items = new List<MenuItem>();
                foreach (var pair in new[] { new[] { "frame", "Whole frames" }, new[] { "second", "Whole seconds" }, new[] { "marker", "Markers only" }, new[] { "off", "Off" } })
                {
                    string v = pair[0];
                    items.Add(new MenuItem { label = pair[1], check = snap == v, act = () => { snap = v; Touch(); } });
                }
                items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem { label = "Hold Shift while dragging to flip it", disabled = true });
                return items;
            }

            private List<MenuItem> MoreItems()
            {
                var items = new List<MenuItem>
                {
                    new MenuItem { label = "Timeline files…", act = OpenFiles },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Scene length…", act = () => T.OpenTimingPanel() },
                };
                var speeds = new List<MenuItem>();
                foreach (float v in new[] { 0.25f, 0.5f, 1f, 1.5f, 2f })
                {
                    float captured = v;
                    speeds.Add(new MenuItem { label = v.ToString("0.##") + "×", check = Mathf.Approximately(Time.timeScale, v), act = () => Time.timeScale = captured });
                }
                items.Add(new MenuItem { label = "Playback speed", sub = speeds });
                var fps = new List<MenuItem>();
                foreach (int f in new[] { 24, 25, 30, 60 })
                {
                    int captured = f;
                    fps.Add(new MenuItem { label = f + " fps", check = T._desiredFrameRate == f, act = () => T.UpdateDesiredFrameRate(captured.ToString()) });
                }
                items.Add(new MenuItem { label = "Frames per second", sub = fps });
                items.Add(new MenuItem { label = "Record live changes", check = T._recording, act = () => T.SetRecording(T._recording == false) });
                items.Add(new MenuItem
                {
                    label = "Delete the whole timeline…",
                    act = () => Confirm("Delete the whole timeline", "Every track, key, group, key set and picker page of this scene goes.", "Delete", () => T.DeleteAllTimelineData())
                });
                items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem { label = "Theme…", act = ToggleTheme });
                items.Add(new MenuItem { label = "Picker…", act = TogglePicker });
                items.Add(new MenuItem { label = "Key sets…", act = ToggleKeySets });
                items.Add(new MenuItem { label = "Rigs…", act = ToggleRigs });
                items.Add(new MenuItem
                {
                    label = "Constraint tools…",
                    disabled = Compat.NodesConstraintsLink.Available == false,
                    act = () => T.ShowConstraintTools()
                });
                items.Add(new MenuItem { label = "Shortcuts and help", act = OpenKeys });
                return items;
            }

            private void SetEditor(string e)
            {
                if (editor == e)
                    return;
                editor = e;
                scrollY = 0f;
                if (e == "graph")
                    _graphFitted = false;
                if (e == "nla" && tab == "Keyframe")
                    tab = "Strip";
                if (e != "nla" && tab == "Strip")
                    tab = "Keyframe";
                CloseMenu();
                Touch();
            }

            public void SetProps(bool on)
            {
                if (_propsFloat)
                {
                    // Out in its own window, the toggle shows and hides that window.
                    _propsMin = on == false;
                    _pwin.gameObject.SetActive(on);
                    RenderTaskbar();
                    Touch();
                    return;
                }
                if (props == on)
                {
                    Touch();
                    return;
                }
                // The span of time on screen stays the same when the grid gets narrower or wider.
                float span = Tt(GridWidth()) - t0;
                props = on;
                LayoutBody();
                pps = GridWidth() / span;
                Touch();
            }
            #endregion
        }
    }
}
