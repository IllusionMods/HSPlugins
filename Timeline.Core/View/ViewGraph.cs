using System;
using System.Collections.Generic;
using System.Linq;
using Timeline.Graph;
using Timeline.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            /// <summary>The value range on screen, ui.gv in the playground.</summary>
            private float _gv0 = -1f, _gv1 = 1f;
            private bool _graphFitted;

            private struct GHit
            {
                public bool handle;
                public bool outgoing;
                public float x, y;
                public Interpolable track;
                public int c;
                public KeyValuePair<float, Keyframe> key;
            }

            private readonly List<GHit> _gHits = new List<GHit>();

            private float GTOP { get { return RUL + 16f; } }
            private float GBOT { get { return GridHeight() - 16f; } }

            private float VY(float v)
            {
                return GTOP + (_gv1 - v) / (_gv1 - _gv0) * (GBOT - GTOP);
            }

            private float YV(float y)
            {
                return _gv1 - (y - GTOP) / (GBOT - GTOP) * (_gv1 - _gv0);
            }

            #region What is drawn
            private static bool Drawable(Interpolable tr)
            {
                return tr.keyframes.Count != 0 && IsStep(tr) == false && CurveComponents.Count(tr.keyframes.Values[0].value) != 0;
            }

            /// <summary>graphTracks(): the selected tracks, or all of them, less the hidden and the unsearched.</summary>
            private List<Interpolable> GraphTracks()
            {
                var list = new List<Interpolable>(T._selectedInterpolables);
                if (list.Count == 0)
                {
                    foreach (Row r in _rows)
                    {
                        if (r.type == RowType.Track)
                            list.Add(r.tr);
                    }
                }
                return list.Where(t => T._graphHiddenTracks.Contains(t) == false && Drawable(t) && Matches(t)).ToList();
            }

            private static int Dim(Interpolable tr)
            {
                return tr.keyframes.Count == 0 ? 0 : Mathf.Min(3, CurveComponents.Count(tr.keyframes.Values[0].value));
            }

            /// <summary>normOf(): with Normalize on, each curve is scaled to fill -1 to 1.</summary>
            private void NormOf(Interpolable tr, int c, out float mid, out float half)
            {
                mid = 0f;
                half = 1f;
                if (normalize == false)
                    return;
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (Keyframe k in tr.keyframes.Values)
                {
                    float v = CurveComponents.Get(k.value, c);
                    lo = Mathf.Min(lo, v);
                    hi = Mathf.Max(hi, v);
                }
                if (lo > hi)
                    return;
                mid = (lo + hi) / 2f;
                half = Mathf.Max((hi - lo) / 2f, 1e-6f);
            }

            /// <summary>fitGraph(): the value range that shows every drawn key, or only the selected ones.</summary>
            private void FitGraph(bool onlySelected)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (Interpolable tr in GraphTracks())
                {
                    int dim = Dim(tr);
                    for (int c = 0; c < dim; ++c)
                    {
                        if (dim > 1 && T.IsComponentHidden(tr, c))
                            continue;
                        float mid, half;
                        NormOf(tr, c, out mid, out half);
                        foreach (KeyValuePair<float, Keyframe> k in tr.keyframes)
                        {
                            if (onlySelected && _selectedSet.Contains(k.Value) == false)
                                continue;
                            float v = (CurveComponents.Get(k.Value.value, c) - mid) / half;
                            lo = Mathf.Min(lo, v);
                            hi = Mathf.Max(hi, v);
                        }
                    }
                }
                if (lo > hi)
                {
                    lo = -1f;
                    hi = 1f;
                }
                if (hi - lo < 1e-3f)
                {
                    lo -= 0.5f;
                    hi += 0.5f;
                }
                float m = (hi - lo) * 0.12f;
                _gv0 = lo - m;
                _gv1 = hi + m;
                _graphFitted = true;
            }

            private float ValueStep()
            {
                float range = _gv1 - _gv0, px = GBOT - GTOP;
                float raw = range * 40f / Mathf.Max(px, 1f);
                float p = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(raw)));
                foreach (float m in new[] { 1f, 2f, 5f, 10f })
                {
                    if (m * p >= raw)
                        return m * p;
                }
                return 10f * p;
            }
            #endregion

            #region Drawing, as drawGraph()
            private void DrawGraph(Paint p, float w, float GH)
            {
                if (_graphFitted == false)
                    FitGraph(false);
                _gHits.Clear();
                p.Rect(0f, 0f, w, GH, Pal.C(0x1E2126));
                p.Clip(0f, RUL, w, GH - RUL);
                TimeLines(p, w, RUL, GH);

                float st = ValueStep();
                int digits = st < 0.01f ? 3 : st < 0.1f ? 2 : st < 1f ? 1 : 0;
                for (float v = Mathf.Ceil(_gv0 / st) * st; v <= _gv1; v += st)
                {
                    float y = Mathf.Round(VY(v));
                    bool zero = Mathf.Abs(v) < st / 2f;
                    p.Rect(0f, y, w, 1f, zero ? Pal.C(0x4A4D53) : Pal.C(0x2C2F35));
                    p.Label((zero ? 0f : v).ToString("F" + digits, System.Globalization.CultureInfo.InvariantCulture), 5f, y - 3f, 10, Pal.C(0x6B6E74), TextAnchor.LowerLeft);
                }

                List<Interpolable> tracks = GraphTracks();
                Interpolable active = ActiveTrack();
                // The active track last, so it is drawn over the others.
                tracks.Sort((a, b) => (a == active ? 1 : 0) - (b == active ? 1 : 0));
                foreach (Interpolable tr in tracks)
                {
                    int dim = Dim(tr);
                    bool act = tr == active;
                    for (int c = 0; c < dim; ++c)
                    {
                        if (dim > 1 && T.IsComponentHidden(tr, c))
                            continue;
                        Color color = dim > 1 ? Pal.Hex(AxisColor(c)) : TrackColor(tr);
                        p.globalAlpha = act ? 1f : T._selectedInterpolables.Contains(tr) ? 0.8f : 0.35f;
                        DrawCurve(p, tr, c, w, act ? 2f : 1.2f, color);
                        p.globalAlpha = 1f;
                    }
                }
                foreach (Interpolable tr in tracks)
                {
                    int dim = Dim(tr);
                    bool act = tr == active;
                    bool locked = T._graphLockedTracks.Contains(tr);
                    bool factor = HandleMath.IsFactorSpace(tr.keyframes.Values[0].value);
                    IList<float> times = tr.keyframes.Keys;
                    IList<Keyframe> keys = tr.keyframes.Values;
                    for (int c = 0; c < dim; ++c)
                    {
                        if (dim > 1 && T.IsComponentHidden(tr, c))
                            continue;
                        float mid, half;
                        NormOf(tr, c, out mid, out half);
                        Func<float, float> G = v => VY((v - mid) / half);
                        for (int i = 0; i < keys.Count; ++i)
                        {
                            Keyframe k = keys[i];
                            float x = X(times[i]), kv = CurveComponents.Get(k.value, c), y = G(kv);
                            if (x < -20f || x > w + 20f)
                                continue;
                            bool sel = _selectedSet.Contains(k);
                            if (sel && showHandles && locked == false && factor == false && k.handles != null)
                            {
                                Vector2 left, right;
                                HandleMath.Offsets(tr.keyframes, i, c, out left, out right);
                                if (i > 0 && HandleMath.HasHandles(keys[i - 1], k))
                                    DrawHandle(p, x, y, X(times[i] + left.x), G(kv + left.y), HandleColor(k.handles.leftType), tr, c, times[i], k, false);
                                if (i < keys.Count - 1 && HandleMath.HasHandles(k, keys[i + 1]))
                                    DrawHandle(p, x, y, X(times[i] + right.x), G(kv + right.y), HandleColor(k.handles.rightType), tr, c, times[i], k, true);
                            }
                            DiamondKey(p, x, y, act ? 8f : 7f, KeyframeColor, sel, act || T._selectedInterpolables.Contains(tr) ? 1f : 0.5f);
                            _gHits.Add(new GHit { x = x, y = y, track = tr, c = c, key = new KeyValuePair<float, Keyframe>(times[i], k) });
                        }
                        if (act)
                        {
                            float t = Tt(w - 14f);
                            string label = dim > 1 ? CurveComponents.Name(keys[0].value, c) : TrackName(tr);
                            p.Label(label, w - 8f, G(SampleOutside(tr, c, t)) - 4f, 11, dim > 1 ? Pal.Hex(AxisColor(c)) : TrackColor(tr), TextAnchor.LowerRight, true);
                        }
                    }
                }
                if (tracks.Count == 0)
                {
                    Interpolable step = T._selectedInterpolables.FirstOrDefault(IsStep);
                    string message = step != null ? TrackName(step) + " switches at each key, so it has no curve. Edit it in the Dope Sheet."
                                                  : "No curves to show. Select a track, or press the eye on a hidden one.";
                    p.Label(message, w / 2f, RUL + 60f, 12, Pal.C(0x6B6E74), TextAnchor.MiddleCenter);
                }
                DrawBox(p);
                p.Unclip();
            }

            private readonly List<Vector2> _curve = new List<Vector2>();

            /// <summary>drawCurve(): the curve as it plays, through the keys and on past them as the track extrapolates.</summary>
            private void DrawCurve(Paint p, Interpolable tr, int c, float w, float lineWidth, Color color)
            {
                float mid, half;
                NormOf(tr, c, out mid, out half);
                _curve.Clear();
                IList<float> times = tr.keyframes.Keys;
                float tA = Tt(-4f), tB = Tt(w + 4f);
                float first = times[0], last = times[times.Count - 1];
                if (tA < first)
                    Sample(tr, c, tA, Mathf.Min(first, tB), 40, mid, half);
                for (int i = 0; i < times.Count - 1; ++i)
                {
                    float a = times[i], b = times[i + 1];
                    if (b < tA || a > tB)
                        continue;
                    // A sample every 3 pixels. A segment narrower than that needs no more than its ends,
                    // which is most of them on a baked track.
                    int m = Mathf.Clamp(Mathf.CeilToInt((X(b) - X(a)) / 3f), 1, 400);
                    Sample(tr, c, a, b, m, mid, half);
                }
                if (tB > last)
                    Sample(tr, c, Mathf.Max(last, tA), tB, 40, mid, half);
                if (times.Count == 1)
                    Sample(tr, c, tA, tB, 2, mid, half);
                p.Stroke(_curve, lineWidth, color, false);
            }

            private void Sample(Interpolable tr, int c, float a, float b, int steps, float mid, float half)
            {
                for (int s = 0; s <= steps; ++s)
                {
                    float t = a + (b - a) * s / steps;
                    Vector2 point = new Vector2(X(t), VY((SampleOutside(tr, c, t) - mid) / half));
                    if (_curve.Count == 0 || (point - _curve[_curve.Count - 1]).sqrMagnitude > 0.01f)
                        _curve.Add(point);
                }
            }

            private void DrawHandle(Paint p, float x, float y, float hx, float hy, Color color, Interpolable tr, int c, float t, Keyframe k, bool outgoing)
            {
                p.Line(x, y, hx, hy, 1f, new Color(201f / 255f, 205f / 255f, 211f / 255f, 0.75f));
                p.Circle(hx, hy, 4.3f, Pal.C(0x111215));
                p.Circle(hx, hy, 3.8f - 0.3f, color);
                _gHits.Add(new GHit { handle = true, outgoing = outgoing, x = hx, y = hy, track = tr, c = c, key = new KeyValuePair<float, Keyframe>(t, k) });
            }
            #endregion

            #region Input
            private GHit? Nearest(Vector2 p, bool handles, float radius)
            {
                GHit? best = null;
                float bd = radius * radius;
                foreach (GHit h in _gHits)
                {
                    if (h.handle != handles)
                        continue;
                    float d = (h.x - p.x) * (h.x - p.x) + (h.y - p.y) * (h.y - p.y);
                    if (d <= bd)
                    {
                        bd = d;
                        best = h;
                    }
                }
                return best;
            }

            private sealed class GraphDrag
            {
                public string kind;
                public float x0, y0;
                public bool moved, middle;
                public float t00, gv0, gv1;
                public GHit hit;
                public float anchor;
                public Dictionary<Keyframe, float> times;
                public Dictionary<Keyframe, object> values;
                public float applied;
            }

            private GraphDrag _gdrag;

            /// <summary>graphDown()</summary>
            private bool GraphDown(PointerEventData e, Vector2 p)
            {
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                GHit? handle = showHandles ? Nearest(p, true, 7f) : null;
                GHit? point = handle.HasValue ? null : Nearest(p, false, 7f);
                if (e.button == PointerEventData.InputButton.Middle)
                {
                    if (ctrl && point.HasValue)
                    {
                        T.DeleteKeyframes(point.Value.key);
                        return true;
                    }
                    _gdrag = new GraphDrag { kind = "pan", x0 = p.x, y0 = p.y, t00 = t0, gv0 = _gv0, gv1 = _gv1, middle = true };
                    return true;
                }
                if (e.button != PointerEventData.InputButton.Left)
                    return false;
                if (alt && handle.HasValue == false && point.HasValue == false)
                {
                    _gdrag = new GraphDrag { kind = "pan", x0 = p.x, y0 = p.y, t00 = t0, gv0 = _gv0, gv1 = _gv1 };
                    return true;
                }
                if (handle.HasValue)
                {
                    _gdrag = new GraphDrag { kind = "handle", x0 = p.x, y0 = p.y, hit = handle.Value };
                    return true;
                }
                if (point.HasValue)
                {
                    GHit h = point.Value;
                    bool all = _selectedSet.Contains(h.key.Value);
                    if (ctrl || shift)
                        T.SelectAddKeyframes(h.key);
                    else if (all == false)
                        T.SelectKeyframes(h.key);
                    if ((ctrl || shift) == false && T._selectedInterpolables.Contains(h.track) == false)
                        T.SelectInterpolable(h.track);
                    _handleAxis = h.c;
                    _gdrag = new GraphDrag { kind = "keys", x0 = p.x, y0 = p.y, hit = h, anchor = h.key.Key };
                    Touch();
                    return true;
                }
                _drag = new Drag { kind = DragKind.Box, x0 = p.x, y0 = p.y, x1 = p.x, y1 = p.y, additive = ctrl || shift };
                return true;
            }

            private bool GraphDragMove(PointerEventData e, Vector2 p)
            {
                if (_gdrag == null)
                    return false;
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                GraphDrag d = _gdrag;
                switch (d.kind)
                {
                    case "pan":
                    {
                        if (Mathf.Abs(p.x - d.x0) + Mathf.Abs(p.y - d.y0) > 3f)
                            d.moved = true;
                        t0 = d.t00 - (p.x - d.x0) / pps;
                        float pu = (GBOT - GTOP) / (d.gv1 - d.gv0);
                        float dv = (p.y - d.y0) / pu;
                        _gv0 = d.gv0 + dv;
                        _gv1 = d.gv1 + dv;
                        break;
                    }
                    case "handle":
                    {
                        GHit h = d.hit;
                        if (d.moved == false)
                        {
                            if (Mathf.Abs(p.x - d.x0) + Mathf.Abs(p.y - d.y0) < 2f)
                                return true;
                            d.moved = true;
                            T.RecordUndo("Move handle");
                        }
                        float mid, half;
                        NormOf(h.track, h.c, out mid, out half);
                        float v = YV(p.y) * half + mid;
                        float kv = CurveComponents.Get(h.key.Value.value, h.c);
                        float dt = Tt(p.x) - h.key.Key;
                        dt = h.outgoing ? Mathf.Max(dt, 0.001f) : Mathf.Min(dt, -0.001f);
                        T.SetHandle(h.key.Value, h.c, h.outgoing, new Vector2(dt, v - kv));
                        SortedList<float, Keyframe> keys = h.track.keyframes;
                        int i = keys.IndexOfValue(h.key.Value);
                        HandleMath.SyncEasingAt(keys, i);
                        HandleMath.SyncEasingAt(keys, i - 1);
                        T.RefreshInterpolation();
                        break;
                    }
                    case "keys":
                    {
                        if (d.moved == false)
                        {
                            if (Mathf.Abs(p.x - d.x0) + Mathf.Abs(p.y - d.y0) < 3f)
                                return true;
                            d.times = new Dictionary<Keyframe, float>();
                            d.values = new Dictionary<Keyframe, object>();
                            foreach (KeyValuePair<float, Keyframe> k in T._selectedKeyframes)
                            {
                                if (T._graphLockedTracks.Contains(k.Value.parent))
                                    continue;
                                d.times[k.Value] = k.Key;
                                d.values[k.Value] = k.Value.value;
                            }
                            if (d.times.Count == 0)
                            {
                                _gdrag = null;
                                return true;
                            }
                            d.moved = true;
                            T.RecordUndo("Move keyframes");
                        }
                        float dt = Tt(p.x) - Tt(d.x0), dv = 0f;
                        if (alt)
                            dt = 0f;
                        if (ctrl == false)
                            dv = YV(p.y) - YV(d.y0);
                        if (dt != 0f)
                            dt = SnapT(d.anchor + dt, shift) - d.anchor;
                        foreach (KeyValuePair<Keyframe, object> o in d.values)
                        {
                            Keyframe k = o.Key;
                            if (d.hit.c >= CurveComponents.Count(o.Value))
                                continue;
                            float mid, half;
                            NormOf(k.parent, d.hit.c, out mid, out half);
                            k.value = CurveComponents.With(o.Value, d.hit.c, CurveComponents.Get(o.Value, d.hit.c) + dv * half);
                        }
                        if (Mathf.Approximately(dt, d.applied) == false)
                        {
                            var moves = new Dictionary<Keyframe, float>();
                            foreach (KeyValuePair<Keyframe, float> o in d.times)
                                moves[o.Key] = Mathf.Max(0f, o.Value + dt);
                            bool blocked = false;
                            foreach (KeyValuePair<Keyframe, float> move in moves)
                            {
                                Keyframe occupant = FindOccupant(move.Key.parent, move.Value, move.Key);
                                if (occupant != null && moves.ContainsKey(occupant) == false)
                                    blocked = true;
                            }
                            if (blocked == false)
                            {
                                var ordered = moves.ToList();
                                bool forward = dt >= d.applied;
                                ordered.Sort((a, b) => forward ? b.Value.CompareTo(a.Value) : a.Value.CompareTo(b.Value));
                                foreach (KeyValuePair<Keyframe, float> move in ordered)
                                    T.TryMoveKeyframe(move.Key, move.Value);
                                d.applied = dt;
                            }
                        }
                        T.RefreshInterpolation();
                        break;
                    }
                }
                return true;
            }

            private bool GraphUp(PointerEventData e, Vector2 p)
            {
                if (_gdrag == null)
                    return false;
                GraphDrag d = _gdrag;
                _gdrag = null;
                if (d.kind == "pan" && d.middle && d.moved == false)
                {
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    float t = Mathf.Clamp(SnapT(Tt(p.x), shift), 0f, T._duration);
                    foreach (Interpolable tr in GraphTracks())
                        T.AddKeyframe(tr, t);
                }
                if ((d.kind == "keys" || d.kind == "handle") && d.moved)
                {
                    try
                    {
                        T.UpdateGrid();
                        T.UpdateKeyframeWindow(false);
                    }
                    catch (Exception)
                    {
                        // Classic bookkeeping only.
                    }
                }
                Touch();
                return true;
            }

            /// <summary>Box selection over the points, in place of the Dope Sheet's rows.</summary>
            private List<KeyValuePair<float, Keyframe>> GraphBox(float x0, float x1, float y0, float y1)
            {
                var found = new List<KeyValuePair<float, Keyframe>>();
                var seen = new HashSet<Keyframe>();
                foreach (GHit h in _gHits)
                {
                    if (h.handle || h.x < x0 || h.x > x1 || h.y < y0 || h.y > y1)
                        continue;
                    if (seen.Add(h.key.Value))
                        found.Add(h.key);
                }
                return found;
            }

            private void GraphScroll(Vector2 p, float dy, bool ctrl)
            {
                float f = Mathf.Exp(-dy * 0.0015f);
                float t = Tt(p.x);
                pps = Mathf.Clamp(pps * f, 8f, 4000f);
                t0 = t - p.x / pps;
                if (ctrl)
                    return;
                float v = YV(p.y);
                float v0 = _gv0, v1 = _gv1;
                _gv0 = v - (v - v0) / f;
                _gv1 = v + (v1 - v) / f;
            }
            #endregion
        }
    }
}
