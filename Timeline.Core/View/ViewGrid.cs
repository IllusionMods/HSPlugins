using System;
using System.Collections.Generic;
using System.Linq;
using Timeline.Graph;
using Timeline.Nla;
using Timeline.View;
using UILib.EventHandlers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            private RectTransform _gridRect;
            private Paint _gp;

            private struct Hit
            {
                public float x, y;
                public Interpolable track;
                public List<KeyValuePair<float, Keyframe>> keys;
                public float t;
            }

            private readonly List<Hit> _hits = new List<Hit>();
            private readonly HashSet<Keyframe> _selectedSet = new HashSet<Keyframe>();

            private void BuildGrid()
            {
                _gridRect = Kit.Node("Grid", _body);
                // Everything drawn is cut to the grid: a curve zoomed or scrolled past its left edge
                // would otherwise run on over the channel list.
                _gridRect.gameObject.AddComponent<RectMask2D>();
                RectTransform surface = Kit.Node("Surface", _gridRect).Fill();
                _gp = surface.gameObject.AddComponent<Paint>();
                _gp.raycastTarget = true;
                GridInput input = _gridRect.gameObject.AddComponent<GridInput>();
                input.view = this;
            }

            #region Drawing, as drawDope()
            private void DrawGrid()
            {
                PlaceRows();
                float w = GridWidth(), GH = GridHeight();
                _selectedSet.Clear();
                foreach (KeyValuePair<float, Keyframe> k in T._selectedKeyframes)
                    _selectedSet.Add(k.Value);
                _hits.Clear();

                Paint p = _gp;
                p.Begin();
                if (editor == "graph")
                    DrawGraph(p, w, GH);
                else if (editor == "nla")
                    DrawNla(p, w, GH);
                else
                    DrawDope(p, w, GH);
                DrawTrimRange(p, w, GH);
                DrawRuler(p, w);
                DrawPlayhead(p, GH);
                DrawScrollbars(p, w, GH);
                p.End();
            }

            private void DrawDope(Paint p, float w, float GH)
            {
                p.Rect(0f, 0f, w, GH, Pal.C(0x21242A));
                p.Clip(0f, RUL, w, GH - RUL);
                Interpolable active = ActiveTrack();
                foreach (Row r in _rows)
                {
                    float y = RUL + r.y - scrollY;
                    if (y > GH || y + r.h < RUL)
                        continue;
                    RowBg(p, r, y, w, active);
                    p.Rect(0f, y + r.h - 1f, w, 1f, Pal.C(0x26292E));
                }
                TimeLines(p, w, RUL, GH);
                foreach (Row r in _rows)
                {
                    float y = RUL + r.y - scrollY;
                    if (y > GH || y + r.h < RUL)
                        continue;
                    float cy = y + r.h / 2f;
                    if (r.type == RowType.Track)
                    {
                        Interpolable tr = r.tr;
                        bool dim = T._graphHiddenTracks.Contains(tr) || tr.enabled == false;
                        float alpha = dim ? 0.4f : 1f;
                        if (IsStep(tr))
                        {
                            DrawStepRow(p, tr, cy, w, alpha);
                            continue;
                        }
                        IList<float> times = tr.keyframes.Keys;
                        IList<Keyframe> keys = tr.keyframes.Values;
                        // A bar between two keys that hold the same value, or after a moving hold.
                        for (int i = 0; i < keys.Count - 1; ++i)
                        {
                            if (keys[i].kind == KeyframeKind.MovingHold || SameValue(keys[i].value, keys[i + 1].value))
                            {
                                p.globalAlpha = alpha;
                                p.Rect(X(times[i]), cy - 2f, X(times[i + 1]) - X(times[i]), 4f, Pal.C(0x5A5E66));
                                p.globalAlpha = 1f;
                            }
                        }
                        for (int i = 0; i < keys.Count; ++i)
                        {
                            float x = X(times[i]);
                            if (x < -12f || x > w + 12f)
                                continue;
                            Keyframe k = keys[i];
                            DiamondKey(p, x, cy, KindSide(k.kind), KindFill(k.kind), _selectedSet.Contains(k), alpha);
                            KeySetBar(p, k, x, cy, KindSide(k.kind), alpha);
                            _hits.Add(new Hit { x = x, y = cy, track = tr, t = times[i], keys = new List<KeyValuePair<float, Keyframe>> { new KeyValuePair<float, Keyframe>(times[i], k) } });
                        }
                        if (T._graphLockedTracks.Contains(tr))
                            p.Rect(0f, y, w, r.h, Pal.Accent(0.05f));
                    }
                    else if (r.tracks.Count != 0 && (r.type == RowType.Summary || groupKeys == "always" || IsCollapsed(r)))
                    {
                        foreach (Column col in Columns(r.tracks))
                        {
                            float x = X(col.t);
                            if (x < -12f || x > w + 12f)
                                continue;
                            DiamondKey(p, x, cy, 7f, Pal.C(0x9A9DA2), col.selected, 1f);
                            _hits.Add(new Hit { x = x, y = cy, t = col.t, keys = col.keys });
                        }
                    }
                }
                if (_rows.Count == 0)
                {
                    string message = search.Trim().Length != 0 ? "Nothing matches the search"
                                   : "No tracks yet. Select something in the scene and press ＋ Add.";
                    p.Label(message, w / 2f, RUL + 60f, 12, Pal.C(0x55585E), TextAnchor.MiddleCenter);
                }
                DrawBox(p);
                p.Unclip();
            }

            private static bool IsStep(Interpolable track)
            {
                if (track.keyframes.Count == 0)
                    return false;
                object v = track.keyframes.Values[0].value;
                return v is bool || CurveComponents.Count(v) == 0;
            }

            private static bool SameValue(object a, object b)
            {
                if (a == null || b == null)
                    return false;
                int n = CurveComponents.Count(a);
                if (n == 0 || n != CurveComponents.Count(b))
                    return Equals(a, b);
                for (int c = 0; c < n; ++c)
                {
                    if (Mathf.Abs(CurveComponents.Get(a, c) - CurveComponents.Get(b, c)) > 1e-6f)
                        return false;
                }
                return true;
            }

            /// <summary>diamond(): a turned square with a dark edge, and an accent ring when selected.</summary>
            private static void DiamondKey(Paint p, float x, float y, float s, Color fill, bool selected, float alpha)
            {
                p.globalAlpha = alpha;
                x = Mathf.Round(x) + 0.5f;
                y = Mathf.Round(y) + 0.5f;
                if (selected)
                    p.Diamond(x, y, s + 4f, Pal.accent);
                // The 1 pixel #111215 stroke, drawn as a slightly larger dark diamond under the fill.
                p.Diamond(x, y, s + 1f, Pal.C(0x111215));
                p.Diamond(x, y, s - 1f, selected ? Pal.C(0xFFE2B0) : fill);
                p.globalAlpha = 1f;
            }

            private static void KeySquare(Paint p, float x, float y, float s, Color fill, bool selected, float alpha)
            {
                p.globalAlpha = alpha;
                float x0 = Mathf.Round(x - s / 2f), y0 = Mathf.Round(y - s / 2f);
                if (selected)
                    p.Rect(x0 - 2f, y0 - 2f, s + 5f, s + 5f, Pal.accent);
                p.Rect(x0, y0, s + 1f, s + 1f, Pal.C(0x111215));
                p.Rect(x0 + 1f, y0 + 1f, s - 1f, s - 1f, selected ? Pal.C(0xFFE2B0) : fill);
                p.globalAlpha = 1f;
            }

            /// <summary>
            /// drawStepRow(): a track that switches rather than eases, an on/off or a list, draws its value
            /// between the keys, and its keys are squares.
            /// </summary>
            private void DrawStepRow(Paint p, Interpolable tr, float cy, float w, float alpha)
            {
                IList<float> times = tr.keyframes.Keys;
                IList<Keyframe> keys = tr.keyframes.Values;
                for (int i = 0; i < keys.Count; ++i)
                {
                    float x0 = X(times[i]), x1 = i < keys.Count - 1 ? X(times[i + 1]) : w + 20f;
                    bool held = i == keys.Count - 1;
                    if (x1 < 0f || x0 > w)
                        continue;
                    object v = keys[i].value;
                    if (v is bool)
                    {
                        if ((bool)v)
                        {
                            p.globalAlpha = alpha * (held ? 0.18f : 0.35f);
                            p.Rect(x0, cy - 6f, x1 - x0, 12f, TrackColor(tr));
                        }
                        else
                        {
                            p.globalAlpha = alpha * 0.7f;
                            p.DashedRect(x0, cy - 6f, x1 - x0, 12f, 3f, Pal.C(0x6B6E74));
                        }
                    }
                    else
                    {
                        p.globalAlpha = alpha * (held ? 0.14f : 0.3f);
                        p.Rect(x0, cy - 6f, x1 - x0, 12f, _optHues[Mathf.Abs(StepIndex(v)) % _optHues.Length]);
                    }
                    string label = v is bool ? ((bool)v ? "On" : "Off") : Convert.ToString(v);
                    float lx = Mathf.Max(x0, 0f) + 8f;
                    p.globalAlpha = alpha * (held ? 0.6f : 1f);
                    if (x1 - lx > Paint.Measure(label, 10) + 8f)
                        p.Label(label, lx, cy + 0.5f, 10, Pal.C(0xD5D8DD), TextAnchor.MiddleLeft);
                    p.globalAlpha = 1f;
                }
                for (int i = 0; i < keys.Count; ++i)
                {
                    float x = X(times[i]);
                    if (x < -12f || x > w + 12f)
                        continue;
                    KeySquare(p, x, cy, KindSide(keys[i].kind) - 1f, KindFill(keys[i].kind), _selectedSet.Contains(keys[i]), alpha);
                    KeySetBar(p, keys[i], x, cy, KindSide(keys[i].kind), alpha);
                    _hits.Add(new Hit { x = x, y = cy, track = tr, t = times[i], keys = new List<KeyValuePair<float, Keyframe>> { new KeyValuePair<float, Keyframe>(times[i], keys[i]) } });
                }
            }

            private static readonly Color[] _optHues =
            {
                Pal.Hex(0x5C99F2), Pal.Hex(0xE86BAE), Pal.Hex(0x82CC63), Pal.Hex(0xE8C34A), Pal.Hex(0x9C7BE0), Pal.Hex(0x4FC3D9), Pal.Hex(0xE85A5C)
            };

            private static int StepIndex(object v)
            {
                if (v is int)
                    return (int)v;
                if (v is Enum)
                    return Convert.ToInt32(v);
                return v == null ? 0 : v.GetHashCode();
            }

            private struct Column
            {
                public float t;
                public bool selected;
                public List<KeyValuePair<float, Keyframe>> keys;
            }

            /// <summary>columns(): every key of these tracks, one entry per frame.</summary>
            private List<Column> Columns(List<Interpolable> tracks)
            {
                var byFrame = new Dictionary<int, Column>();
                float fps = Mathf.Max(T._desiredFrameRate, 1);
                foreach (Interpolable tr in tracks)
                {
                    foreach (KeyValuePair<float, Keyframe> k in tr.keyframes)
                    {
                        int f = Mathf.RoundToInt(k.Key * fps);
                        Column col;
                        if (byFrame.TryGetValue(f, out col) == false)
                            col = new Column { t = k.Key, keys = new List<KeyValuePair<float, Keyframe>>() };
                        col.keys.Add(k);
                        if (_selectedSet.Contains(k.Value))
                            col.selected = true;
                        byFrame[f] = col;
                    }
                }
                return byFrame.Values.ToList();
            }

            /// <summary>rowBg(): the summary, objects and the selected tracks have a tint across the grid.</summary>
            private void RowBg(Paint p, Row r, float y, float w, Interpolable active)
            {
                Color bg;
                if (r.type == RowType.Summary)
                    bg = Pal.C(0x25282D);
                else if (r.type == RowType.Object)
                    bg = Pal.C(0x282B30);
                else if (r.type == RowType.Track && r.tr == active)
                    bg = Pal.Accent(0.10f);
                else if (r.type == RowType.Track && T._selectedInterpolables.Contains(r.tr))
                    bg = Pal.Accent(0.05f);
                else
                    return;
                p.Rect(0f, y, w, r.h, bg);
            }

            private float NiceStep()
            {
                float fps = Mathf.Max(T._desiredFrameRate, 1);
                foreach (float s in new[] { 1f / fps, 0.1f, 0.2f, 0.5f, 1f, 2f, 5f })
                {
                    if (s * pps >= 48f)
                        return s;
                }
                return 10f;
            }

            /// <summary>timeLines(): major and minor lines, and the time outside the scene darkened.</summary>
            private void TimeLines(Paint p, float w, float top, float GH)
            {
                float st = NiceStep(), minor = st * pps >= 90f ? st / 5f : st / 2f;
                int a = Mathf.FloorToInt(Tt(0f) / minor), b = Mathf.CeilToInt(Tt(w) / minor);
                for (int i = a; i <= b; ++i)
                {
                    float t = i * minor, x = Mathf.Round(X(t));
                    bool major = Mathf.Abs(t / st - Mathf.Round(t / st)) < 1e-4f;
                    p.Rect(x, top, 1f, GH - top, major ? Pal.C(0x30333A) : Pal.C(0x282B30));
                }
                Color outside = new Color(0f, 0f, 0f, 0.22f);
                if (X(0f) > 0f)
                    p.Rect(0f, top, X(0f), GH - top, outside);
                if (X(T._duration) < w)
                    p.Rect(X(T._duration), top, w - X(T._duration), GH - top, outside);
                if (T._tweakStrip != null)
                {
                    float x0 = X(T._tweakStrip.start), x1 = X(T._tweakStrip.end);
                    Color shade = new Color(0f, 0f, 0f, 0.3f);
                    if (x0 > 0f)
                        p.Rect(0f, top, x0, GH - top, shade);
                    if (x1 < w)
                        p.Rect(x1, top, w - x1, GH - top, shade);
                    p.Rect(x0, top, 1f, GH - top, Pal.accent);
                    p.Rect(x1 - 1f, top, 1f, GH - top, Pal.accent);
                }
            }

            /// <summary>drawRuler(): #181A1E, ticks, the seconds, and the markers as small triangles.</summary>
            private void DrawRuler(Paint p, float w)
            {
                p.Rect(0f, 0f, w, RUL, Pal.C(0x181A1E));
                p.Rect(0f, RUL - 1f, w, 1f, Pal.C(0x111215));
                float st = NiceStep(), minor = st * pps >= 90f ? st / 5f : st / 2f;
                int a = Mathf.FloorToInt(Tt(0f) / minor), b = Mathf.CeilToInt(Tt(w) / minor);
                int digits = st < 0.1f ? 2 : st < 1f ? 1 : 0;
                for (int i = a; i <= b; ++i)
                {
                    float t = i * minor, x = Mathf.Round(X(t));
                    bool major = Mathf.Abs(t / st - Mathf.Round(t / st)) < 1e-4f;
                    p.Rect(x, RUL - (major ? 8f : 4f), 1f, major ? 8f : 4f, Pal.C(0x44474C));
                    if (major)
                    {
                        string label = t.ToString("F" + digits, System.Globalization.CultureInfo.InvariantCulture) + "s";
                        p.Label(label, x + 0.5f, 4f, 11, t < -1e-4f || t > T._duration + 1e-4f ? Pal.C(0x55585E) : Pal.C(0x9A9DA2), TextAnchor.UpperCenter);
                    }
                }
                foreach (TimelineMarker m in T._markers)
                {
                    float x = X(m.time);
                    if (x < -60f || x > w + 6f)
                        continue;
                    p.Triangle(new Vector2(x - 5f, RUL - 1f), new Vector2(x + 5f, RUL - 1f), new Vector2(x, RUL - 8f), Pal.accent);
                    if (string.IsNullOrEmpty(m.name) == false)
                        p.Label(m.name, x + 7f, RUL - 12f, 10, Pal.accent, TextAnchor.MiddleLeft);
                }
            }

            /// <summary>The range to trim: orange over the whole height, under the ruler and the playhead.</summary>
            private void DrawTrimRange(Paint p, float w, float GH)
            {
                if (T._hasTrimRange == false && T._isTrimRangeSelecting == false)
                    return;
                float x0 = Mathf.Max(-2f, X(T._trimRangeStart)), x1 = Mathf.Min(w + 2f, X(T._trimRangeEnd));
                if (x1 < x0)
                    return;
                p.Rect(x0, 0f, Mathf.Max(1f, x1 - x0), GH, _trimRangeFillColor);
                p.Rect(x0 - 1f, 0f, 2f, GH, _trimRangeEdgeColor);
                p.Rect(x1 - 1f, 0f, 2f, GH, _trimRangeEdgeColor);
            }

            /// <summary>drawPlayhead(): a 2 pixel line and the time on a rounded tag at the top.</summary>
            private void DrawPlayhead(Paint p, float GH)
            {
                float x = Mathf.Round(X(T._playbackTime));
                Color c = Pal.playhead;
                p.Rect(x - 1f, 0f, 2f, GH, c);
                string label = Fmt(T._playbackTime);
                float w = Paint.Measure(label, 11) + 10f;
                p.RoundRect(x - w / 2f, 3f, w, 18f, 3f, c);
                p.Label(label, x, 12.5f, 11, Color.white, TextAnchor.MiddleCenter);
            }

            private float _vBarY, _vBarH, _vBarTrack;
            private bool _vBar;
            private float _hBarX, _hBarW, _hBarTrack, _hSpan, _hVisible, _hMin;
            private bool _hBar;

            /// <summary>drawScrollbars(): thin bars for where the view sits, rows on the right and time along the bottom.</summary>
            private void DrawScrollbars(Paint p, float w, float GH)
            {
                Color c = Pal.C(0x5A5E66, 0.85f);
                float view = GH - RUL;
                _vBar = editor != "graph" && _rowsTotal > view;
                if (_vBar)
                {
                    float top = RUL + 2f, th = GH - RUL - 12f, h = Mathf.Max(20f, th * view / _rowsTotal);
                    float y = top + (th - h) * (scrollY / (_rowsTotal - view));
                    p.RoundRect(w - 7f, y, 5f, h, 2.5f, c);
                    _vBarY = y;
                    _vBarH = h;
                    _vBarTrack = th;
                }
                float a = Mathf.Min(-0.5f, t0), b = Mathf.Max(T._duration + 0.5f, Tt(w)), span = b - a, vis = Tt(w) - t0;
                _hBar = vis < span - 1e-6f;
                if (_hBar)
                {
                    float tw = w - 16f, wd = Mathf.Max(24f, tw * vis / span), x = 4f + (tw - wd) * ((t0 - a) / (span - vis));
                    p.RoundRect(x, GH - 7f, wd, 5f, 2.5f, c);
                    _hBarX = x;
                    _hBarW = wd;
                    _hBarTrack = tw;
                    _hSpan = span;
                    _hVisible = vis;
                    _hMin = a;
                }
            }

            private void DrawBox(Paint p)
            {
                if (_drag == null || _drag.kind != DragKind.Box || _drag.moved == false)
                    return;
                float x = Mathf.Min(_drag.x0, _drag.x1), y = Mathf.Min(_drag.y0, _drag.y1);
                float w = Mathf.Abs(_drag.x1 - _drag.x0), h = Mathf.Abs(_drag.y1 - _drag.y0);
                p.Rect(x, y, w, h, Pal.Accent(0.12f));
                p.StrokeRect(x, y, w, h, 1f, Pal.accent);
            }
            #endregion

            #region Input
            private enum DragKind
            {
                Scrub,
                Marker,
                Box,
                Keys,
                VBar,
                HBar,
                Trim
            }

            private sealed class Drag
            {
                public DragKind kind;
                public float x0, y0, x1, y1;
                public bool moved;
                public bool additive;
                public float startT;
                public TimelineMarker marker;
                public float scroll0, t00;
                public Dictionary<Keyframe, float> origin;
                public float applied;
            }

            private Drag _drag;

            public Vector2 GridPos(PointerEventData e)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_gridRect, e.position, null, out local);
                Rect r = _gridRect.rect;
                return new Vector2(local.x - r.xMin, r.yMax - local.y);
            }

            private Hit? DopeHit(Vector2 p)
            {
                Hit? best = null;
                float bd = float.MaxValue;
                foreach (Hit h in _hits)
                {
                    if (Mathf.Abs(h.y - p.y) > 10f)
                        continue;
                    float d = Mathf.Abs(h.x - p.x);
                    if (d <= 7f && d < bd)
                    {
                        bd = d;
                        best = h;
                    }
                }
                return best;
            }

            private Row RowAt(float y)
            {
                float yy = y - RUL + scrollY;
                foreach (Row r in _rows)
                {
                    if (yy >= r.y && yy < r.y + r.h)
                        return r;
                }
                return null;
            }

            public float SnapT(float t, bool flip)
            {
                string mode = snap;
                if (flip)
                    mode = mode == "off" ? "frame" : "off";
                if (mode == "off")
                    return t;
                foreach (TimelineMarker m in T._markers)
                {
                    if (Mathf.Abs(X(m.time) - X(t)) < 6f)
                        return m.time;
                }
                if (mode == "second")
                    return Mathf.Round(t);
                if (mode == "marker")
                    return t;
                return FrameSnap(t);
            }

            public void GridDown(PointerEventData e)
            {
                CloseMenu();
                Vector2 p = GridPos(e);
                float w = GridWidth(), GH = GridHeight();
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (e.button == PointerEventData.InputButton.Right)
                {
                    GridMenu(e, p);
                    return;
                }
                if (e.button == PointerEventData.InputButton.Middle && editor == "graph")
                {
                    if (p.y >= RUL)
                        GraphDown(e, p);
                    return;
                }
                if (e.button == PointerEventData.InputButton.Middle)
                {
                    if (p.y < RUL || editor == "nla")
                        return;
                    if (ctrl)
                    {
                        Hit? hit = DopeHit(p);
                        if (hit.HasValue)
                            T.DeleteKeyframes(hit.Value.keys);
                        return;
                    }
                    Row r = RowAt(p.y);
                    if (r != null && r.tracks.Count + (r.tr != null ? 1 : 0) != 0)
                    {
                        float t = SnapT(Tt(p.x), shift);
                        List<Interpolable> targets = r.type == RowType.Track ? new List<Interpolable> { r.tr } : r.tracks;
                        foreach (Interpolable track in targets)
                            T.AddKeyframe(track, Mathf.Clamp(t, 0f, T._duration));
                    }
                    return;
                }
                if (e.button != PointerEventData.InputButton.Left)
                    return;

                if (_vBar && p.x >= w - 10f && p.y > RUL)
                {
                    _drag = new Drag { kind = DragKind.VBar, y0 = p.y, scroll0 = scrollY };
                    return;
                }
                if (_hBar && p.y >= GH - 10f)
                {
                    _drag = new Drag { kind = DragKind.HBar, x0 = p.x, t00 = t0 };
                    return;
                }
                if (p.y < RUL && ctrl)
                {
                    // Control + drag marks the range to trim; control + click clears it.
                    T.BeginTrimRangeSelect(SnapT(Tt(p.x), shift));
                    _drag = new Drag { kind = DragKind.Trim };
                    Touch();
                    return;
                }
                if (p.y < RUL)
                {
                    // A marker under the pointer is dragged; anywhere else on the ruler moves the playhead.
                    TimelineMarker marker = p.y >= 10f ? T._markers.FirstOrDefault(m => Mathf.Abs(X(m.time) - p.x) < 6f) : null;
                    if (marker != null)
                    {
                        _drag = new Drag { kind = DragKind.Marker, x0 = p.x, startT = marker.time, marker = marker };
                        return;
                    }
                    _drag = new Drag { kind = DragKind.Scrub };
                    SetTime(SnapT(Tt(p.x), shift == false));
                    return;
                }

                if (editor == "graph")
                {
                    GraphDown(e, p);
                    return;
                }
                if (editor == "nla")
                {
                    NlaDown(e, p);
                    return;
                }
                Hit? key = DopeHit(p);
                if (key.HasValue)
                {
                    List<KeyValuePair<float, Keyframe>> keys = key.Value.keys;
                    bool allSelected = keys.TrueForAll(k => _selectedSet.Contains(k.Value));
                    if (ctrl)
                        T.SelectAddKeyframes(keys);
                    else if (allSelected == false)
                        T.SelectKeyframes(keys);
                    if (key.Value.track != null && ctrl == false && T._selectedInterpolables.Contains(key.Value.track) == false)
                        T.SelectInterpolable(key.Value.track);
                    var origin = new Dictionary<Keyframe, float>();
                    foreach (KeyValuePair<float, Keyframe> k in T._selectedKeyframes)
                        origin[k.Value] = k.Key;
                    _drag = new Drag { kind = DragKind.Keys, x0 = p.x, y0 = p.y, startT = key.Value.t, origin = origin };
                    Touch();
                    return;
                }
                _drag = new Drag { kind = DragKind.Box, x0 = p.x, y0 = p.y, x1 = p.x, y1 = p.y, additive = ctrl };
            }

            public void GridDrag(PointerEventData e)
            {
                if (GraphDragMove(e, GridPos(e)) || NlaDrag(e, GridPos(e)))
                    return;
                if (_drag == null)
                    return;
                Vector2 p = GridPos(e);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                switch (_drag.kind)
                {
                    case DragKind.Scrub:
                        SetTime(SnapT(Tt(p.x), shift == false));
                        break;
                    case DragKind.Trim:
                        T.UpdateTrimRangeSelect(SnapT(Tt(p.x), shift));
                        Touch();
                        break;
                    case DragKind.Marker:
                        if (_drag.moved == false)
                        {
                            if (Mathf.Abs(p.x - _drag.x0) < 3f)
                                return;
                            _drag.moved = true;
                            T.RecordUndo("Move marker");
                        }
                        _drag.marker.time = Mathf.Clamp(FrameSnap(_drag.startT + Tt(p.x) - Tt(_drag.x0)), 0f, T._duration);
                        T.UpdateMarkers();
                        break;
                    case DragKind.VBar:
                    {
                        float view = GridHeight() - RUL;
                        SetScroll(_drag.scroll0 + (p.y - _drag.y0) / Mathf.Max(1f, _vBarTrack - _vBarH) * (_rowsTotal - view));
                        break;
                    }
                    case DragKind.HBar:
                        t0 = _drag.t00 + (p.x - _drag.x0) / Mathf.Max(1f, _hBarTrack - _hBarW) * (_hSpan - _hVisible);
                        break;
                    case DragKind.Box:
                        _drag.x1 = p.x;
                        _drag.y1 = p.y;
                        if (Mathf.Abs(p.x - _drag.x0) > 3f || Mathf.Abs(p.y - _drag.y0) > 3f)
                            _drag.moved = true;
                        break;
                    case DragKind.Keys:
                        if (_drag.moved == false && Mathf.Abs(p.x - _drag.x0) < 3f)
                            return;
                        if (_drag.moved == false)
                        {
                            _drag.moved = true;
                            T.RecordUndo("Move keyframes");
                        }
                        float target = SnapT(_drag.startT + (p.x - _drag.x0) / pps, shift);
                        MoveKeysBy(target - _drag.startT);
                        break;
                }
            }

            public void GridUp(PointerEventData e)
            {
                if (GraphUp(e, GridPos(e)) || NlaUp(e, GridPos(e)))
                    return;
                if (_drag == null)
                    return;
                Drag d = _drag;
                _drag = null;
                if (d.kind == DragKind.Trim)
                {
                    T.FinishTrimRangeSelect();
                    Touch();
                    return;
                }
                if (d.kind == DragKind.Box)
                {
                    bool ctrl = d.additive;
                    if (d.moved == false)
                    {
                        if (ctrl == false)
                            T.SelectKeyframes();
                        return;
                    }
                    float x0 = Mathf.Min(d.x0, d.x1), x1 = Mathf.Max(d.x0, d.x1), y0 = Mathf.Min(d.y0, d.y1), y1 = Mathf.Max(d.y0, d.y1);
                    var found = new List<KeyValuePair<float, Keyframe>>();
                    var seen = new HashSet<Keyframe>();
                    if (editor == "graph")
                        found = GraphBox(x0, x1, y0, y1);
                    else
                    {
                        foreach (Hit h in _hits)
                        {
                            if (h.x < x0 || h.x > x1 || h.y < y0 - 6f || h.y > y1 + 6f)
                                continue;
                            foreach (KeyValuePair<float, Keyframe> k in h.keys)
                            {
                                if (seen.Add(k.Value))
                                    found.Add(k);
                            }
                        }
                    }
                    if (ctrl)
                        T.SelectAddKeyframes(found.Where(k => _selectedSet.Contains(k.Value) == false));
                    else
                        T.SelectKeyframes(found);
                }
                else if (d.kind == DragKind.Keys && d.moved)
                {
                    T.UpdateGrid();
                    T.UpdateKeyframeWindow(false);
                    T.RefreshInterpolation();
                }
                Touch();
            }

            /// <summary>
            /// Moves the dragged keys to their start time plus dt, all or nothing: a frame where any of
            /// them would land on a key that is not moving is skipped, so keys never swallow each other.
            /// </summary>
            private void MoveKeysBy(float dt)
            {
                if (Mathf.Approximately(dt, _drag.applied))
                    return;
                var moves = new Dictionary<Keyframe, float>();
                foreach (KeyValuePair<Keyframe, float> o in _drag.origin)
                    moves[o.Key] = o.Value + dt;
                foreach (KeyValuePair<Keyframe, float> move in moves)
                {
                    Keyframe occupant = FindOccupant(move.Key.parent, move.Value, move.Key);
                    if (occupant != null && moves.ContainsKey(occupant) == false)
                        return;
                    if (T._graphLockedTracks.Contains(move.Key.parent))
                        return;
                }
                // Moving in the direction of travel, furthest first, so no key steps on one that has not moved yet.
                var ordered = moves.ToList();
                bool forward = dt >= _drag.applied;
                ordered.Sort((a, b) => forward ? b.Value.CompareTo(a.Value) : a.Value.CompareTo(b.Value));
                foreach (KeyValuePair<Keyframe, float> move in ordered)
                    T.TryMoveKeyframe(move.Key, move.Value);
                _drag.applied = dt;
                T.Interpolate(true);
                T.Interpolate(false);
            }

            public void GridScroll(PointerEventData e)
            {
                Vector2 p = GridPos(e);
                float dy = -e.scrollDelta.y * 100f;
                float f = Mathf.Exp(-dy * 0.0015f);
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (shift)
                    t0 += dy / pps * 0.6f;
                else if (editor == "graph")
                    GraphScroll(p, dy, ctrl);
                else if (ctrl == false)
                    SetScroll(scrollY + dy * 0.5f);
                else
                {
                    float t = Tt(p.x);
                    pps = Mathf.Clamp(pps * f, 8f, 4000f);
                    t0 = t - p.x / pps;
                }
            }

            /// <summary>The grid's right click menu: the ruler's, or the keys'.</summary>
            private void GridMenu(PointerEventData e, Vector2 p)
            {
                float t = FrameSnap(Tt(p.x));
                if (p.y < RUL)
                {
                    TimelineMarker near = T._markers.FirstOrDefault(m => Mathf.Abs(X(m.time) - p.x) < 6f);
                    var items = new List<MenuItem>
                    {
                        new MenuItem { label = "Add marker here", kb = "M", act = () => T.AddMarker(t) },
                        new MenuItem { label = "Move playhead here", act = () => SetTime(t) }
                    };
                    items.AddRange(TrimItems());
                    if (near != null)
                    {
                        items.Add(new MenuItem { sep = true });
                        items.Add(new MenuItem { head = "MARKER “" + near.name + "”" });
                        items.Add(new MenuItem { label = "Rename…", act = () => RenameMarker(near, p) });
                        items.Add(new MenuItem { label = "Move to playhead", act = () => { T.RecordUndo("Move marker"); near.time = FrameSnap(T._playbackTime); T.UpdateMarkers(); } });
                        items.Add(new MenuItem { label = "Delete", act = () => { T.RecordUndo("Delete marker"); T._markers.Remove(near); T.UpdateMarkers(); } });
                    }
                    OpenMenuAtPointer(e, items);
                    return;
                }
                if (editor == "nla")
                {
                    NlaMenu(e, p);
                    return;
                }
                if (editor == "graph")
                {
                    GHit? point = Nearest(p, false, 7f);
                    if (point.HasValue && _selectedSet.Contains(point.Value.key.Value) == false)
                        T.SelectKeyframes(point.Value.key);
                }
                else
                {
                    Hit? hit = DopeHit(p);
                    if (hit.HasValue && hit.Value.keys.TrueForAll(k => _selectedSet.Contains(k.Value)) == false)
                        T.SelectKeyframes(hit.Value.keys);
                }
                Row r = RowAt(p.y);
                int n = T._selectedKeyframes.Count;
                var menu = new List<MenuItem> { new MenuItem { head = n != 0 ? n + (n > 1 ? " KEYS" : " KEY") + " SELECTED" : "KEYS" } };
                menu.AddRange(KeyItems());
                menu.Add(new MenuItem { sep = true });
                List<Interpolable> targets = editor == "graph" ? GraphTracks() : r == null ? new List<Interpolable>() : r.type == RowType.Track ? new List<Interpolable> { r.tr } : r.tracks;
                menu.Add(new MenuItem
                {
                    label = "Add key here" + (r != null && r.type == RowType.Track ? " on " + TrackName(r.tr) : ""),
                    kb = "Middle",
                    disabled = targets.Count == 0,
                    act = () => { foreach (Interpolable track in targets) T.AddKeyframe(track, Mathf.Clamp(t, 0f, T._duration)); }
                });
                menu.Add(new MenuItem { label = "Move playhead here", act = () => SetTime(t) });
                menu.Add(new MenuItem { label = "Frame selected", kb = "F", act = FrameSelected });
                OpenMenuAtPointer(e, menu);
            }

            /// <summary>The ruler's trim entries (#219): control + drag on the ruler marks the range.</summary>
            private List<MenuItem> TrimItems()
            {
                var items = new List<MenuItem> { new MenuItem { sep = true }, new MenuItem { head = "TRIM" } };
                if (T._hasTrimRange)
                {
                    items.Add(new MenuItem { label = "Trim to " + FormatTrimTime(T._trimRangeStart) + " – " + FormatTrimTime(T._trimRangeEnd) + "…", act = () => T.RequestTrim(true) });
                    items.Add(new MenuItem { label = "Trim (keep original times)…", act = () => T.RequestTrim(false) });
                }
                else
                    items.Add(new MenuItem { label = "Control + drag on the ruler marks a range", disabled = true });
                items.Add(new MenuItem { label = "Set trim start at playhead", act = () => { T.SetTrimRangeEdge(true, T.GetCursorTime()); Touch(); } });
                items.Add(new MenuItem { label = "Set trim end at playhead", act = () => { T.SetTrimRangeEdge(false, T.GetCursorTime()); Touch(); } });
                if (T._selectedKeyframes.Count > 1)
                    items.Add(new MenuItem { label = "Trim range from selected keys", act = () => { T.SetTrimRange(T._selectedKeyframes.Min(k => k.Key), T._selectedKeyframes.Max(k => k.Key)); Touch(); } });
                if (T._hasTrimRange)
                    items.Add(new MenuItem { label = "Clear trim range", act = () => { T.ClearTrimRange(); Touch(); } });
                return items;
            }

            /// <summary>renameMarker(): a field in a small box where the marker is.</summary>
            private void RenameMarker(TimelineMarker marker, Vector2 at)
            {
                Vector2 local = new Vector2(chanW + at.x, HDR + at.y);
                RectTransform dialog = OpenDialog("Marker name", local.x, local.y);
                RectTransform line = Line(dialog);
                InputField field = Fld(line, null, 0, marker.name, null, text =>
                {
                    if (text.Trim().Length != 0)
                    {
                        T.RecordUndo("Rename marker");
                        marker.name = text.Trim();
                        T.UpdateMarkers();
                    }
                    CloseMenu();
                });
                PlaceDialog(dialog);
                field.ActivateInputField();
            }

            /// <summary>Where the pointer is, for the hints along the bottom.</summary>
            private string GridHover()
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_gridRect, Input.mousePosition, null, out local) == false)
                    return null;
                Rect r = _gridRect.rect;
                if (r.Contains(local) == false)
                    return null;
                float y = r.yMax - local.y;
                if (y < RUL)
                    return "ruler";
                return editor;
            }
            #endregion
        }
    }

    /// <summary>Hands the grid's pointer events to the view.</summary>
    internal class GridInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public Timeline.View view;

        public void OnPointerDown(PointerEventData eventData)
        {
            view.GridDown(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            view.GridDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            view.GridUp(eventData);
        }

        public void OnScroll(PointerEventData eventData)
        {
            view.GridScroll(eventData);
        }
    }
}
