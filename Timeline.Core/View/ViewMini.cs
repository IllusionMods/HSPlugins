using System;
using System.Collections.Generic;
using Timeline.Graph;
using Timeline.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            /// <summary>Set while a handle is dragged in the small graph, so the panel is not rebuilt from under it.</summary>
            private bool _miniDragging;

            /// <summary>
            /// drawMini(): the curve either side of the selected key, with its handles when the segments
            /// are Bézier, and the playhead running across it. The handles drag, as they do in the Graph,
            /// and with several keys selected they shape all of them the same way on the same side.
            /// </summary>
            internal sealed class MiniGraph : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
            {
                public View view;
                public Interpolable track;
                public Keyframe key;
                public int component;

                private Paint _art;
                private Paint _over;
                private float _tL, _tR, _v0, _v1;
                private bool _frozen;
                private float _shownTime = float.NaN;
                private bool _dragging;
                private bool _outgoing;
                private bool _moved;

                private struct HandleSpot
                {
                    public bool outgoing;
                    public float x, y;
                }

                private readonly List<HandleSpot> _spots = new List<HandleSpot>();

                public void Init(View owner, Interpolable tr, Keyframe k, int c)
                {
                    view = owner;
                    track = tr;
                    key = k;
                    component = c;
                    RectTransform self = (RectTransform)transform;
                    _art = Kit.Node("Art", self).Fill().gameObject.AddComponent<Paint>();
                    _over = Kit.Node("Playhead", self).Fill().gameObject.AddComponent<Paint>();
                    Redraw();
                }

                private float W { get { return ((RectTransform)transform).rect.width; } }
                private float H { get { return ((RectTransform)transform).rect.height; } }

                private float Mx(float t)
                {
                    return 10f + (t - _tL) / (_tR - _tL) * (W - 20f);
                }

                private float My(float v)
                {
                    return 8f + (_v1 - v) / (_v1 - _v0) * (H - 26f);
                }

                private float Tx(float x)
                {
                    return _tL + (x - 10f) / (W - 20f) * (_tR - _tL);
                }

                private float Vy(float y)
                {
                    return _v1 - (y - 8f) / (H - 26f) * (_v1 - _v0);
                }

                private void OnRectTransformDimensionsChange()
                {
                    if (_art != null)
                        Redraw();
                }

                public void Redraw()
                {
                    SortedList<float, Keyframe> keyframes = track.keyframes;
                    int i = keyframes.IndexOfValue(key);
                    if (i < 0 || W <= 20f)
                        return;
                    IList<float> times = keyframes.Keys;
                    IList<Keyframe> keys = keyframes.Values;
                    int c = component;
                    bool hasPrev = i > 0, hasNext = i < keys.Count - 1;
                    float kt = times[i], kv = CurveComponents.Get(key.value, c);
                    bool factor = HandleMath.IsFactorSpace(key.value);
                    bool prevBez = hasPrev && HandleMath.HasHandles(keys[i - 1], key) && factor == false;
                    bool nextBez = hasNext && HandleMath.HasHandles(key, keys[i + 1]) && factor == false;
                    Vector2 leftH = Vector2.zero, rightH = Vector2.zero;
                    if (key.handles != null)
                        HandleMath.Offsets(keyframes, i, c, out leftH, out rightH);

                    if (_frozen == false)
                    {
                        _tL = hasPrev ? times[i - 1] : kt - 1f;
                        _tR = hasNext ? times[i + 1] : kt + 1f;
                        float lo = float.MaxValue, hi = float.MinValue;
                        for (int s = 0; s <= 80; ++s)
                        {
                            float v = ValueAt(track, _tL + (_tR - _tL) * s / 80f, c);
                            lo = Mathf.Min(lo, v);
                            hi = Mathf.Max(hi, v);
                        }
                        if (prevBez)
                        {
                            lo = Mathf.Min(lo, kv + leftH.y);
                            hi = Mathf.Max(hi, kv + leftH.y);
                        }
                        if (nextBez)
                        {
                            lo = Mathf.Min(lo, kv + rightH.y);
                            hi = Mathf.Max(hi, kv + rightH.y);
                        }
                        // Never zoomed in further than a few percent of the value itself, or a hair's
                        // difference between two baked keys fills the box as if it were a leap.
                        float minRange = Mathf.Max(0.001f, 0.05f * Mathf.Max(Mathf.Abs(lo), Mathf.Abs(hi)));
                        if (hi - lo < minRange)
                        {
                            float middle = (hi + lo) * 0.5f;
                            lo = middle - minRange * 0.5f;
                            hi = middle + minRange * 0.5f;
                        }
                        float pad = (hi - lo) * 0.15f;
                        _v0 = lo - pad;
                        _v1 = hi + pad;
                    }

                    Paint p = _art;
                    p.Begin();
                    float w = W, h = H;
                    p.Rect(0f, h - 18f, w, 1f, Pal.C(0x2A2D32));
                    var points = new List<Vector2>();
                    int steps = Mathf.Clamp(Mathf.RoundToInt(w / 2f), 40, 200);
                    for (int s = 0; s <= steps; ++s)
                    {
                        float t = _tL + (_tR - _tL) * s / steps;
                        points.Add(new Vector2(Mx(t), My(ValueAt(track, t, c))));
                    }
                    p.Stroke(points, 2f, CurveColor(track, c), false);
                    _spots.Clear();
                    if (prevBez)
                        Handle(p, Mx(kt), My(kv), Mx(kt + leftH.x), My(kv + leftH.y), HandleColor(key.handles.leftType), false);
                    if (nextBez)
                        Handle(p, Mx(kt), My(kv), Mx(kt + rightH.x), My(kv + rightH.y), HandleColor(key.handles.rightType), true);
                    if (hasPrev)
                        DiamondKey(p, Mx(times[i - 1]), My(CurveComponents.Get(keys[i - 1].value, c)), 7f, Pal.C(0xD6D9DE), false, 0.8f);
                    if (hasNext)
                        DiamondKey(p, Mx(times[i + 1]), My(CurveComponents.Get(keys[i + 1].value, c)), 7f, Pal.C(0xD6D9DE), false, 0.8f);
                    DiamondKey(p, Mx(kt), My(kv), 8f, Pal.C(0xD6D9DE), true, 1f);
                    if (hasPrev)
                        p.Label(Fmt(times[i - 1]) + " s", 6f, h - 4f, 9, Pal.C(0x6B6E74), TextAnchor.LowerLeft);
                    if (hasNext)
                        p.Label(Fmt(times[i + 1]) + " s", w - 6f, h - 4f, 9, Pal.C(0x6B6E74), TextAnchor.LowerRight);
                    p.Label(Fmt(kt) + " s", Mx(kt), h - 4f, 9, Pal.C(0x6B6E74), TextAnchor.LowerCenter);
                    p.End();
                    _shownTime = float.NaN;
                }

                private void Handle(Paint p, float x, float y, float hx, float hy, Color color, bool outgoing)
                {
                    p.Line(x, y, hx, hy, 1f, new Color(201f / 255f, 205f / 255f, 211f / 255f, 0.8f));
                    p.Circle(hx, hy, 5f, Pal.C(0x111215));
                    p.Circle(hx, hy, 4f, color);
                    _spots.Add(new HandleSpot { outgoing = outgoing, x = hx, y = hy });
                }

                /// <summary>The playhead: a line across the graph where it is, and a dot on the curve.</summary>
                private void Update()
                {
                    if (view == null || _over == null)
                        return;
                    float t = view.T._playbackTime;
                    if (t == _shownTime)
                        return;
                    _shownTime = t;
                    _over.Begin();
                    if (t >= _tL && t <= _tR)
                    {
                        float x = Mathf.Round(Mx(t));
                        _over.Rect(x - 0.5f, 0f, 1f, H - 18f, Pal.playhead);
                        _over.Circle(Mx(t), My(ValueAt(track, t, component)), 3.5f, Pal.playhead);
                    }
                    _over.End();
                }

                private Vector2 Local(PointerEventData e)
                {
                    RectTransform rect = (RectTransform)transform;
                    Vector2 local;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, null, out local);
                    return new Vector2(local.x - rect.rect.xMin, rect.rect.yMax - local.y);
                }

                public void OnPointerDown(PointerEventData e)
                {
                    if (e.button != PointerEventData.InputButton.Left)
                        return;
                    Vector2 p = Local(e);
                    float best = 64f;
                    _dragging = false;
                    foreach (HandleSpot spot in _spots)
                    {
                        float d = (spot.x - p.x) * (spot.x - p.x) + (spot.y - p.y) * (spot.y - p.y);
                        if (d <= best)
                        {
                            best = d;
                            _dragging = true;
                            _outgoing = spot.outgoing;
                        }
                    }
                }

                public void OnBeginDrag(PointerEventData e)
                {
                    if (_dragging == false)
                        return;
                    _moved = false;
                    // The scale stays put while dragging, or the curve would slide under the pointer.
                    _frozen = true;
                    view._miniDragging = true;
                }

                public void OnDrag(PointerEventData e)
                {
                    if (_dragging == false)
                        return;
                    if (view.T._graphLockedTracks.Contains(track))
                        return;
                    if (_moved == false)
                    {
                        _moved = true;
                        view.T.RecordUndo("Move handle");
                    }
                    SortedList<float, Keyframe> keyframes = track.keyframes;
                    int i = keyframes.IndexOfValue(key);
                    if (i < 0)
                        return;
                    Vector2 p = Local(e);
                    float kt = keyframes.Keys[i], kv = CurveComponents.Get(key.value, component);
                    float span = _outgoing ? keyframes.Keys[i + 1] - kt : kt - keyframes.Keys[i - 1];
                    float dt = Tx(p.x) - kt;
                    dt = _outgoing ? Mathf.Clamp(dt, 0.001f, span) : Mathf.Clamp(dt, -span, -0.001f);
                    float dv = Vy(p.y) - kv;
                    view.T.SetHandle(key, component, _outgoing, new Vector2(dt, dv));
                    Sync(keyframes, i);

                    // The same shape on every other selected key, on the same side: reach as a share of each
                    // key's own segment, rise as a share of its own rise.
                    Keyframe other = keyframes.Values[_outgoing ? i + 1 : i - 1];
                    float rise = CurveComponents.Get(other.value, component) - kv;
                    float reach = Mathf.Abs(dt) / span;
                    foreach (KeyValuePair<float, Keyframe> pair in view.T._selectedKeyframes)
                    {
                        Keyframe k = pair.Value;
                        if (k == key || view.T._graphLockedTracks.Contains(k.parent) || HandleMath.IsFactorSpace(k.value))
                            continue;
                        SortedList<float, Keyframe> theirs = k.parent.keyframes;
                        int j = theirs.IndexOfValue(k);
                        int o = _outgoing ? j + 1 : j - 1;
                        if (j < 0 || o < 0 || o >= theirs.Count || component >= CurveComponents.Count(k.value))
                            continue;
                        bool bez = _outgoing ? HandleMath.HasHandles(k, theirs.Values[o]) : HandleMath.HasHandles(theirs.Values[o], k);
                        if (bez == false)
                            continue;
                        float theirSpan = Mathf.Abs(theirs.Keys[o] - theirs.Keys[j]);
                        float theirRise = CurveComponents.Get(theirs.Values[o].value, component) - CurveComponents.Get(k.value, component);
                        float theirDv = Mathf.Abs(rise) > 1e-9f && Mathf.Abs(theirRise) > 1e-9f ? dv / rise * theirRise : dv;
                        view.T.SetHandle(k, component, _outgoing, new Vector2((_outgoing ? 1f : -1f) * reach * theirSpan, theirDv));
                        Sync(theirs, j);
                    }
                    view.T.RefreshInterpolation();
                    Redraw();
                }

                private static void Sync(SortedList<float, Keyframe> keyframes, int i)
                {
                    HandleMath.SyncEasingAt(keyframes, i);
                    HandleMath.SyncEasingAt(keyframes, i - 1);
                }

                public void OnEndDrag(PointerEventData e)
                {
                    if (_dragging == false)
                        return;
                    _dragging = false;
                    _frozen = false;
                    view._miniDragging = false;
                    view.Touch();
                }

                private void OnDisable()
                {
                    if (_dragging && view != null)
                        view._miniDragging = false;
                    _dragging = false;
                }
            }
        }
    }
}
