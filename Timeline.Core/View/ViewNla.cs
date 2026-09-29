using System;
using System.Collections.Generic;
using System.Linq;
using Studio;
using Timeline.Nla;
using Timeline.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            private struct StripHit
            {
                public MotionStrip strip;
                public float x0, x1, y0, y1;
            }

            private readonly List<StripHit> _stripHits = new List<StripHit>();

            private sealed class StripDrag
            {
                public MotionStrip strip;
                public string zone;
                public float x0, y0;
                public float start, end, scale;
                public int lane;
                public bool moved;
            }

            private StripDrag _sdrag;

            private static readonly Color _clipColor = Pal.Hex(0x7A6A9C);

            #region Rows
            /// <summary>
            /// The NLA's list, as Blender's: each object, its keys (which play on top), then its own lanes
            /// from the top of its stack down, with a new lane above them to drop a strip into.
            /// </summary>
            private List<Row> BuildNlaRows()
            {
                var rows = new List<Row>();
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
                    Row obj = new Row { type = RowType.Object, key = key, label = ObjectName(oci), h = ROW };
                    obj.tracks.AddRange(LeavesInOrder(T._interpolablesTree.tree).Where(t => t.oci == oci && Matches(t)));
                    rows.Add(obj);
                    if (collapsed.Contains(key))
                        continue;
                    Row action = new Row { type = RowType.Action, key = "action" + key.GetHashCode(), depth = 1, h = LANE, oci = oci };
                    action.tracks.AddRange(obj.tracks);
                    rows.Add(action);
                    for (int lane = T.TopLane(oci) + 1; lane >= 0; --lane)
                        rows.Add(new Row { type = RowType.Lane, key = "lane" + key.GetHashCode() + "_" + lane, lane = lane, depth = 1, h = LANE, oci = oci });
                }
                float y = 0f;
                foreach (Row r in rows)
                {
                    r.y = y;
                    y += r.h;
                }
                _rowsTotal = y;
                return rows;
            }
            #endregion

            #region Drawing, as drawNla()
            private void DrawNla(Paint p, float w, float GH)
            {
                _stripHits.Clear();
                p.Rect(0f, 0f, w, GH, Pal.C(0x21242A));
                p.Clip(0f, RUL, w, GH - RUL);
                foreach (Row r in _rows)
                {
                    float y = RUL + r.y - scrollY;
                    if (r.type == RowType.Lane || r.type == RowType.Action)
                        p.Rect(0f, y, w, r.h, Pal.C(0x23262B));
                    else if (r.type == RowType.Object)
                        p.Rect(0f, y, w, r.h, Pal.C(0x282B30));
                    p.Rect(0f, y + r.h - 1f, w, 1f, Pal.C(0x1B1D21));
                }
                TimeLines(p, w, RUL, GH);
                foreach (Row r in _rows)
                {
                    float y = RUL + r.y - scrollY, cy = y + r.h / 2f;
                    if (y > GH || y + r.h < RUL)
                        continue;
                    if (r.type == RowType.Action)
                    {
                        foreach (Column col in Columns(r.tracks))
                        {
                            float x = X(col.t);
                            if (x >= -10f && x <= w + 10f)
                                DiamondKey(p, x, cy, 7f, Pal.C(0x9A9DA2), false, 0.8f);
                        }
                        continue;
                    }
                    if (r.type != RowType.Lane)
                        continue;
                    List<MotionStrip> list = T._strips.Where(s => s.owner == r.oci && s.lane == r.lane && s.clip != null).OrderBy(s => s.start).ToList();
                    if (list.Count == 0)
                    {
                        p.DashedRect(X(0f), y + 4f, X(T._duration) - X(0f), r.h - 9f, 4f, Pal.C(0x44474C));
                        p.Label("New lane · drag a strip here, or Push down the keys", (X(0f) + X(T._duration)) / 2f, cy, 11, Pal.C(0x6B6E74), TextAnchor.MiddleCenter);
                        continue;
                    }
                    bool plays = T.StackOf(r.oci).Plays(r.lane);
                    // The hold regions first, so the rule about what holds is visible.
                    for (int i = 0; i < list.Count; ++i)
                    {
                        MotionStrip s = list[i];
                        p.globalAlpha = s.enabled && plays ? 0.16f : 0.06f;
                        if (s.extrapolation != StripExtrapolation.Nothing)
                        {
                            float to = i + 1 < list.Count ? list[i + 1].start : Tt(w + 10f);
                            if (to > s.end)
                                p.Rect(X(s.end), y + 5f, X(to) - X(s.end), r.h - 10f, _clipColor);
                        }
                        if (i == 0 && s.extrapolation == StripExtrapolation.Hold && s.start > Tt(-10f))
                            p.Rect(-10f, y + 5f, X(s.start) + 10f, r.h - 10f, _clipColor);
                        p.globalAlpha = 1f;
                    }
                    foreach (MotionStrip s in list)
                    {
                        float x0 = X(s.start), x1 = X(s.end), top = y + 3f, h = r.h - 6f;
                        bool sel = T._selectedStrips.Contains(s);
                        _stripHits.Add(new StripHit { strip = s, x0 = x0, x1 = x1, y0 = top, y1 = top + h });
                        if (x1 < -5f || x0 > w + 5f)
                            continue;
                        p.globalAlpha = s.enabled && plays ? 1f : 0.35f;
                        if (sel)
                            p.Rect(x0 - 2f, top - 2f, x1 - x0 + 4f, h + 4f, Pal.C(0xFFE2B0));
                        p.Rect(x0, top, x1 - x0, h, sel ? Pal.accent : _clipColor);
                        Color line = sel ? Pal.C(0x21242A) : new Color(0f, 0f, 0f, 0.35f);
                        for (int k = 1; k < s.repeat; ++k)
                        {
                            float xx = Mathf.Round(X(s.start + s.singleLength * k));
                            p.Rect(xx, top + 3f, 1f, h - 6f, line);
                        }
                        if (s.blendIn > 0f)
                            p.Line(x0, top + h, X(s.start + s.blendIn), top, 1f, line);
                        if (s.blendOut > 0f)
                            p.Line(X(s.end - s.blendOut), top, x1, top + h, 1f, line);
                        // Keyed influence, as a line across the strip: at the top is full influence.
                        if (s.influenceKeys.Count != 0)
                        {
                            float px = x0, py = top + h - s.InfluenceAt(s.start) * h;
                            for (float xx = x0 + 3f; xx <= x1; xx += 3f)
                            {
                                float yy = top + h - s.InfluenceAt(Tt(xx)) * h;
                                p.Line(px, py, xx, yy, 1f, Pal.C(0xFFE2B0));
                                px = xx;
                                py = yy;
                            }
                        }
                        Color ink = sel ? Pal.C(0x21242A) : Pal.C(0xE4E7EC);
                        string label = s.clip.name;
                        if (x1 - x0 > 20f)
                        {
                            p.Label(label, x0 + 7f, cy, 11, ink, TextAnchor.MiddleLeft, true);
                            float bx = x0 + 10f + Paint.Measure(label, 11, true);
                            var badges = new List<string>();
                            if (s.blendMode != StripBlendMode.Replace)
                                badges.Add(s.blendMode.ToString());
                            if (s.repeat > 1)
                                badges.Add("×" + s.repeat);
                            if (T.UsersOf(s.clip) > 1)
                                badges.Add("Linked " + T.UsersOf(s.clip));
                            if (s.reverse)
                                badges.Add("Reversed");
                            if (s.enabled == false)
                                badges.Add("Muted");
                            foreach (string b in badges)
                            {
                                float bw = Paint.Measure(b, 10) + 6f;
                                if (bx + bw > x1 - 2f)
                                    break;
                                p.Rect(bx, cy - 7f, bw, 14f, new Color(0f, 0f, 0f, 0.25f));
                                p.Label(b, bx + 3f, cy, 10, ink, TextAnchor.MiddleLeft);
                                bx += bw + 4f;
                            }
                        }
                        p.globalAlpha = 1f;
                    }
                }
                float endY = RUL + _rowsTotal - scrollY + 22f;
                if (endY < GH - 10f)
                    p.Label("Each object's lanes play from the lowest up and its keys on top of them all. Only the first strip in a lane holds backwards.", w / 2f, endY, 11, Pal.C(0x55585E), TextAnchor.MiddleCenter);
                p.Unclip();
            }
            #endregion

            #region Input
            /// <summary>stripHit(): the strip under the pointer, and whether it is the body or an end.</summary>
            private StripHit? StripAt(Vector2 p, out string zone)
            {
                zone = null;
                for (int i = _stripHits.Count - 1; i >= 0; --i)
                {
                    StripHit h = _stripHits[i];
                    if (p.y < h.y0 || p.y > h.y1 || p.x < h.x0 - 4f || p.x > h.x1 + 4f)
                        continue;
                    zone = Mathf.Abs(p.x - h.x0) < 5f ? "left" : Mathf.Abs(p.x - h.x1) < 5f ? "right" : "body";
                    return h;
                }
                return null;
            }

            private void NlaDown(PointerEventData e, Vector2 p)
            {
                if (e.button != PointerEventData.InputButton.Left)
                    return;
                bool add = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftShift);
                string zone;
                StripHit? hit = StripAt(p, out zone);
                if (hit.HasValue)
                {
                    MotionStrip s = hit.Value.strip;
                    T.SelectStrip(s, add);
                    tab = "Strip";
                    if (T.LaneLocked(s))
                    {
                        Toast("That lane is locked: unlock it in the list to move its strips.");
                        Touch();
                        return;
                    }
                    _sdrag = new StripDrag { strip = s, zone = zone, x0 = p.x, y0 = p.y, start = s.start, end = s.end, scale = s.scale, lane = s.lane };
                    Touch();
                    return;
                }
                if (add == false)
                {
                    T._selectedStrips.Clear();
                    Touch();
                }
            }

            private bool NlaDrag(PointerEventData e, Vector2 p)
            {
                if (_sdrag == null)
                    return false;
                StripDrag d = _sdrag;
                if (d.moved == false)
                {
                    if (Mathf.Abs(p.x - d.x0) + Mathf.Abs(p.y - d.y0) < 3f)
                        return true;
                    d.moved = true;
                    T.RecordUndo("Move strip");
                }
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                MotionStrip s = d.strip;
                float single = Mathf.Max(s.clipSpan * Mathf.Max(s.repeat, 1), 0.001f);
                if (d.zone == "body")
                {
                    s.start = Mathf.Max(0f, SnapT(d.start + Tt(p.x) - Tt(d.x0), shift));
                    // Higher lanes are higher in the list, so dragging down goes to a lower one.
                    int top = T._strips.Where(x => x != s && x.owner == s.owner).Select(x => x.lane).DefaultIfEmpty(-1).Max();
                    s.lane = Mathf.Clamp(d.lane - Mathf.RoundToInt((p.y - d.y0) / LANE), 0, top + 1);
                }
                else if (d.zone == "right")
                {
                    float end = Mathf.Max(s.start + 0.05f, SnapT(Tt(p.x), shift));
                    s.scale = (end - s.start) / single;
                }
                else
                {
                    float start = Mathf.Min(d.end - 0.05f, Mathf.Max(0f, SnapT(Tt(p.x), shift)));
                    s.start = start;
                    s.scale = (d.end - start) / single;
                }
                T.UpdateStrips();
                T.RefreshInterpolation();
                _rowsDirty = true;
                return true;
            }

            private bool NlaUp(PointerEventData e, Vector2 p)
            {
                if (_sdrag == null)
                    return false;
                StripDrag d = _sdrag;
                _sdrag = null;
                if (d.moved)
                {
                    T.SettleStrip(d.strip, d.start);
                    T.UpdateStrips();
                    T.RefreshInterpolation();
                }
                Touch();
                return true;
            }

            private void NlaMenu(PointerEventData e, Vector2 p)
            {
                string zone;
                StripHit? hit = StripAt(p, out zone);
                if (hit.HasValue && T._selectedStrips.Contains(hit.Value.strip) == false)
                    T.SelectStrip(hit.Value.strip, false);
                var items = new List<MenuItem>();
                if (hit.HasValue)
                {
                    items.Add(new MenuItem { head = "STRIP “" + hit.Value.strip.clip.name + "”" });
                    items.AddRange(StripItems());
                }
                else
                {
                    float t = FrameSnap(Tt(p.x));
                    Row r = RowAt(p.y);
                    if (r != null && r.type == RowType.Lane)
                        items.AddRange(LaneItems(r, t));
                    else if (r != null && r.type == RowType.Action)
                        items.AddRange(ActionItems(r));
                    else
                        items.Add(new MenuItem { label = "Push down selected tracks", act = () => T.PushDownToStrip(new List<Interpolable>(T._selectedInterpolables)) });
                    items.Add(new MenuItem { sep = true });
                    items.Add(new MenuItem { label = "Move playhead here", act = () => SetTime(t) });
                }
                OpenMenuAtPointer(e, items);
            }

            /// <summary>stripItems(): what can be done to the selected strip.</summary>
            private List<MenuItem> StripItems()
            {
                MotionStrip s = T._selectedStrips.Count == 0 ? null : T._selectedStrips[T._selectedStrips.Count - 1];
                bool none = s == null;
                var repeats = new List<MenuItem>();
                var blends = new List<MenuItem>();
                if (s != null)
                {
                    foreach (int n in new[] { 1, 2, 3, 4 })
                    {
                        int captured = n;
                        repeats.Add(new MenuItem { label = "×" + n, check = s.repeat == n, act = () => EditStrip(() => s.repeat = captured) });
                    }
                    foreach (StripBlendMode mode in new[] { StripBlendMode.Replace, StripBlendMode.Add, StripBlendMode.Subtract, StripBlendMode.Multiply })
                    {
                        StripBlendMode captured = mode;
                        blends.Add(new MenuItem { label = mode.ToString(), check = s.blendMode == mode, act = () => EditStrip(() => s.blendMode = captured) });
                    }
                }
                return new List<MenuItem>
                {
                    new MenuItem { label = "Duplicate", kb = "Shift+D", disabled = none, act = () => T.DuplicateStrip(s, false) },
                    new MenuItem { label = "Duplicate linked", disabled = none, act = () => T.DuplicateStrip(s, true) },
                    new MenuItem { label = "Split at playhead", kb = "Y", disabled = none, act = () => T.SplitStrip(s, FrameSnap(T._playbackTime)) },
                    new MenuItem { label = "Mute or unmute", kb = "H", disabled = none, act = () => EditStrip(() => s.enabled = !s.enabled) },
                    new MenuItem { label = "Delete", kb = "X", disabled = none, act = () => T.RemoveStrip(s) },
                    new MenuItem { label = T._tweakStrip == s && s != null ? "Finish editing clip keys" : "Edit clip keys", disabled = none, act = () => { if (T._tweakStrip == s) T.ExitTweakMode(); else T.EnterTweakMode(s); } },
                    new MenuItem { label = "Merge the " + T._selectedStrips.Count + " selected strips", disabled = T._selectedStrips.Count < 2, act = () => T.MergeStrips(new List<MotionStrip>(T._selectedStrips)) },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Rename clip…", disabled = none, act = () => RenameDialog("Rename clip", s.clip.name, v => T.RenameClip(s.clip, v)) },
                    new MenuItem { label = "Make single user", disabled = none || T.UsersOf(s.clip) < 2, act = () => T.MakeSingleUser(s) },
                    new MenuItem { label = "Repeat", disabled = none, sub = repeats },
                    new MenuItem { label = "Blend", disabled = none, sub = blends }
                };
            }

            /// <summary>A lane's menu: its settings, a strip of a clip the object has, and deleting it.</summary>
            private List<MenuItem> LaneItems(Row r, float t)
            {
                var items = new List<MenuItem>();
                ObjectCtrlInfo owner = r.oci;
                bool real = r.lane <= T.TopLane(owner);
                NlaLane lane = T.StackOf(owner).lanes.Find(l => l.index == r.lane);
                items.Add(new MenuItem { head = real ? "LANE “" + (lane != null ? lane.DisplayName : "Track " + (r.lane + 1)) + "”" : "NEW LANE" });
                var clips = new List<MenuItem>();
                foreach (MotionClip clip in T._clips.Where(c => OwnerOf(c) == owner))
                {
                    MotionClip captured = clip;
                    int users = T.UsersOf(clip);
                    clips.Add(new MenuItem { label = clip.name + (users == 0 ? "  (unused)" : ""), act = () => T.AddClipStrip(captured, t, r.lane) });
                }
                items.Add(new MenuItem { label = "Add a strip of", disabled = clips.Count == 0, sub = clips.Count == 0 ? null : clips });
                if (real)
                {
                    items.Add(new MenuItem { label = "Rename…", act = () => RenameDialog("Rename lane", lane != null ? lane.name : "", v => EditLane(r, l => l.name = v.Trim())) });
                    items.Add(new MenuItem { label = "Mute", check = lane != null && lane.mute, act = () => EditLane(r, l => l.mute = !l.mute) });
                    items.Add(new MenuItem { label = "Solo", check = lane != null && lane.solo, act = () => EditLane(r, l => l.solo = !l.solo) });
                    items.Add(new MenuItem { label = "Lock", check = lane != null && lane.locked, act = () => EditLane(r, l => l.locked = !l.locked) });
                    items.Add(new MenuItem { sep = true });
                    items.Add(new MenuItem { label = "Delete lane and its strips", act = () => { T.DeleteLane(owner, r.lane); ++_rowsVersion; Touch(); } });
                }
                var unused = T._clips.Where(c => OwnerOf(c) == owner && T.UsersOf(c) == 0).ToList();
                if (unused.Count != 0)
                {
                    var delete = new List<MenuItem>();
                    foreach (MotionClip clip in unused)
                    {
                        MotionClip captured = clip;
                        delete.Add(new MenuItem { label = clip.name, act = () => T.DeleteClip(captured) });
                    }
                    items.Add(new MenuItem { label = "Delete an unused clip", sub = delete });
                }
                return items;
            }

            /// <summary>The keys row's menu: how the object's own keys blend over its lanes, Blender's action blending.</summary>
            private List<MenuItem> ActionItems(Row r)
            {
                NlaStack stack = T.StackOf(r.oci);
                var blends = new List<MenuItem>();
                foreach (StripBlendMode mode in new[] { StripBlendMode.Replace, StripBlendMode.Add, StripBlendMode.Subtract, StripBlendMode.Multiply })
                {
                    StripBlendMode captured = mode;
                    blends.Add(new MenuItem { label = mode.ToString(), check = stack.actionBlend == mode, act = () => { stack.actionBlend = captured; ++_rowsVersion; T.RefreshInterpolation(); Touch(); } });
                }
                var influences = new List<MenuItem>();
                foreach (int percent in new[] { 100, 75, 50, 25, 0 })
                {
                    float value = percent / 100f;
                    influences.Add(new MenuItem { label = percent + " %", check = Mathf.Abs(stack.actionInfluence - value) < 0.005f, act = () => { stack.actionInfluence = value; ++_rowsVersion; T.RefreshInterpolation(); Touch(); } });
                }
                return new List<MenuItem>
                {
                    new MenuItem { head = "KEYS NOT IN A STRIP" },
                    new MenuItem { label = "Push them down to a strip", disabled = r.tracks.All(tr => tr.keyframes.Count == 0), act = () => T.PushDownToStrip(new List<Interpolable>(r.tracks)) },
                    new MenuItem { label = "Blend over the lanes", sub = blends },
                    new MenuItem { label = "Influence", sub = influences }
                };
            }

            /// <summary>The NLA's own keys: Shift+D, Y, H and X act on strips there.</summary>
            private bool NlaKeys(bool ctrl, bool shift)
            {
                if (editor != "nla" || T._selectedStrips.Count == 0 || ctrl)
                    return false;
                MotionStrip s = T._selectedStrips[T._selectedStrips.Count - 1];
                if (shift && Input.GetKeyDown(KeyCode.D))
                    T.DuplicateStrip(s, false);
                else if (Input.GetKeyDown(KeyCode.Y))
                    T.SplitStrip(s, FrameSnap(T._playbackTime));
                else if (Input.GetKeyDown(KeyCode.H))
                    EditStrip(() => s.enabled = !s.enabled);
                else if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Delete))
                    T.RemoveStrip(s);
                else
                    return false;
                Touch();
                return true;
            }
            #endregion
        }
    }
}
