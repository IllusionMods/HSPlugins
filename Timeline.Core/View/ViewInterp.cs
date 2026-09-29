using System;
using System.Collections.Generic;
using Timeline.Graph;
using Timeline.View;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            #region The curves, as the playground defines them
            // EASE in the playground: easeIn is u³, easeOut 1-(1-u)³, both exact cubics, so a two key
            // curve says them exactly and they still turn into handles. The in-out one is two cubics
            // joined at the middle, which a single cubic cannot say, so it is sampled like the others.
            private static readonly AnimationCurve _easeIn = new AnimationCurve(new UnityEngine.Keyframe(0f, 0f, 0f, 0f), new UnityEngine.Keyframe(1f, 1f, 3f, 3f));
            private static readonly AnimationCurve _easeOut = new AnimationCurve(new UnityEngine.Keyframe(0f, 0f, 3f, 3f), new UnityEngine.Keyframe(1f, 1f, 0f, 0f));
            private static AnimationCurve _easeInOut;
            private static AnimationCurve _bounce;
            private static readonly Dictionary<string, AnimationCurve> _families = new Dictionary<string, AnimationCurve>();
            private static readonly EasingDirection[] _directions = { EasingDirection.In, EasingDirection.Out, EasingDirection.InOut };
            /// <summary>The family and direction the last CurveKind recognised, for drawing its tile.</summary>
            private EasingKind? _curFamily;
            private EasingDirection _curDirection;

            private static AnimationCurve EaseInOut
            {
                get { return _easeInOut ?? (_easeInOut = TimelineEasing.Curve(EasingKind.Cubic, EasingDirection.InOut)); }
            }

            private static AnimationCurve Bounce
            {
                get { return _bounce ?? (_bounce = TimelineEasing.Curve(EasingKind.Bounce, EasingDirection.Out)); }
            }

            private static AnimationCurve Family(EasingKind kind, EasingDirection direction)
            {
                string key = kind + "/" + direction;
                AnimationCurve curve;
                if (_families.TryGetValue(key, out curve) == false)
                {
                    curve = TimelineEasing.Curve(kind, direction);
                    _families[key] = curve;
                }
                return curve;
            }

            private static string DirectionName(EasingDirection d)
            {
                return d == EasingDirection.In ? "in" : d == EasingDirection.Out ? "out" : "in-out";
            }

            /// <summary>What a key's outgoing segment is, and its name, "Bounce out" for one of the families.</summary>
            private Interp InterpOf(Keyframe k, out string name)
            {
                name = null;
                _curFamily = null;
                SortedList<float, Keyframe> keys = k.parent.keyframes;
                int i = keys.IndexOfValue(k);
                if (i >= 0 && i + 1 < keys.Count && HandleMath.HasHandles(k, keys.Values[i + 1]))
                    return HandleKind(keys, i);
                return CurveKind(k.curve, out name);
            }

            /// <summary>
            /// A segment shaped by handles, named by the shape those handles make right now: the easing
            /// curve beside them is only a copy kept for older versions, and goes stale. Read on the
            /// component that moves the most, because one that does not move has no shape to read.
            /// </summary>
            private static Interp HandleKind(SortedList<float, Keyframe> keys, int i)
            {
                Keyframe k = keys.Values[i], next = keys.Values[i + 1];
                HandleType outType = k.handles.rightType, inType = next.handles.leftType;
                bool automatic = (outType == HandleType.Auto || outType == HandleType.AutoClamped) &&
                                 (inType == HandleType.Auto || inType == HandleType.AutoClamped);
                if (automatic)
                    return Interp.Bezier;

                float span = keys.Keys[i + 1] - keys.Keys[i];
                bool factor = HandleMath.IsFactorSpace(k.value);
                int dim = factor ? 1 : Mathf.Max(1, CurveComponents.Count(k.value));
                int best = 0;
                float rise = factor ? 1f : 0f;
                if (factor == false)
                {
                    for (int c = 0; c < dim; ++c)
                    {
                        float r = CurveComponents.Get(next.value, c) - CurveComponents.Get(k.value, c);
                        if (Mathf.Abs(r) > Mathf.Abs(rise))
                        {
                            rise = r;
                            best = c;
                        }
                    }
                }
                if (Mathf.Abs(rise) < 1e-6f || span <= 0.0001f)
                    return Interp.Bezier;
                Vector2 unusedLeft, outgoing, incoming, unusedRight;
                HandleMath.Offsets(keys, i, best, out unusedLeft, out outgoing);
                HandleMath.Offsets(keys, i + 1, best, out incoming, out unusedRight);
                float s0 = Shared.BezierMath.ToTangent(outgoing.x, outgoing.y, span, rise);
                float s1 = Shared.BezierMath.ToTangent(incoming.x, incoming.y, span, rise);
                // The reach along time says nothing in a tangent, so a handle pulled out further than a
                // third is not the preset any more, whatever its slope.
                bool thirds = Mathf.Abs(outgoing.x - span / 3f) < span * 0.02f && Mathf.Abs(incoming.x + span / 3f) < span * 0.02f;
                if (thirds)
                {
                    if (Near(s0, 1f) && Near(s1, 1f))
                        return Interp.Linear;
                    if (Near(s0, 0f) && Near(s1, 3f))
                        return Interp.EaseIn;
                    if (Near(s0, 3f) && Near(s1, 0f))
                        return Interp.EaseOut;
                }
                return Interp.Bezier;
            }

            private static bool Near(float a, float b)
            {
                return Mathf.Abs(a - b) < 0.02f;
            }

            /// <summary>Two easing curves that are the same shape, to a thousandth: exact float equality missed ones read back from a file.</summary>
            private static bool SameCurve(AnimationCurve a, AnimationCurve b)
            {
                if (a == null || b == null || a.length != b.length)
                    return false;
                UnityEngine.Keyframe[] x = a.keys, y = b.keys;
                for (int i = 0; i < x.Length; ++i)
                {
                    if (Mathf.Abs(x[i].time - y[i].time) > 1e-3f || Mathf.Abs(x[i].value - y[i].value) > 1e-3f)
                        return false;
                    if (SameTangent(x[i].inTangent, y[i].inTangent) == false || SameTangent(x[i].outTangent, y[i].outTangent) == false)
                        return false;
                }
                return true;
            }

            private static bool SameTangent(float a, float b)
            {
                if (float.IsInfinity(a) || float.IsInfinity(b))
                    return a == b;
                return Mathf.Abs(a - b) < 1e-2f;
            }

            private Interp CurveKind(AnimationCurve c, out string name)
            {
                name = null;
                _curFamily = null;
                if (c == null)
                    return Interp.Linear;
                if (SameCurve(c, T._stairsPreset))
                    return Interp.Constant;
                if (SameCurve(c, T._linePreset))
                    return Interp.Linear;
                if (SameCurve(c, _easeIn))
                    return Interp.EaseIn;
                if (SameCurve(c, _easeOut))
                    return Interp.EaseOut;
                if (SameCurve(c, EaseInOut))
                    return Interp.EaseInOut;
                if (SameCurve(c, Bounce))
                    return Interp.Bounce;
                if (SameCurve(c, T._hermitePreset))
                    return Interp.Bezier;
                foreach (EasingKind kind in TimelineEasing.All)
                {
                    foreach (EasingDirection d in _directions)
                    {
                        if (SameCurve(c, Family(kind, d)))
                        {
                            name = EasingName(kind) + " " + DirectionName(d);
                            _curFamily = kind;
                            _curDirection = d;
                            return Interp.Custom;
                        }
                    }
                }
                return Interp.Custom;
            }
            #endregion

            #region Applying
            private void SetInterp(Interp i)
            {
                switch (i)
                {
                    case Interp.Constant: ApplyCurve(T._stairsPreset, "Constant"); break;
                    case Interp.Linear: ApplyCurve(T._linePreset, "Linear"); break;
                    case Interp.Bezier: ApplyBezier(); break;
                    case Interp.EaseIn: ApplyCurve(_easeIn, "Ease in"); break;
                    case Interp.EaseOut: ApplyCurve(_easeOut, "Ease out"); break;
                    case Interp.EaseInOut: ApplyCurve(EaseInOut, "Ease in-out"); break;
                    case Interp.Bounce: ApplyCurve(Bounce, "Bounce"); break;
                }
            }

            private void ApplyFamily(EasingKind kind, EasingDirection direction)
            {
                ApplyCurve(Family(kind, direction), EasingName(kind) + " " + DirectionName(direction));
            }

            /// <summary>
            /// Gives every selected key's outgoing segment this easing.
            ///
            /// Each segment is read back into handles when the curve is a cubic, and otherwise marked as
            /// shaped by its curve, which is what makes Bounce or Elastic actually play: handles left
            /// over from before would otherwise go on being what the segment follows.
            /// </summary>
            private void ApplyCurve(AnimationCurve curve, string label)
            {
                if (T._selectedKeyframes.Count == 0)
                    return;
                T.RecordUndo(label);
                foreach (KeyValuePair<float, Keyframe> pair in T._selectedKeyframes)
                {
                    Keyframe k = pair.Value;
                    if (T._graphLockedTracks.Contains(k.parent))
                        continue;
                    k.curve = new AnimationCurve(curve.keys);
                    SortedList<float, Keyframe> keys = k.parent.keyframes;
                    int index = keys.IndexOfValue(k);
                    if (index + 1 < keys.Count)
                        HandleMath.ConvertSegment(keys, index);
                    else
                        k.shapedByCurve = k.handles != null && HandleMath.CanConvert(k) == false;
                }
                AfterInterpChange();
            }

            /// <summary>Bézier: a smooth curve through the key, with automatic, clamped handles on both sides.</summary>
            private void ApplyBezier()
            {
                if (T._selectedKeyframes.Count == 0)
                    return;
                T.RecordUndo("Bézier");
                foreach (KeyValuePair<float, Keyframe> pair in T._selectedKeyframes)
                {
                    Keyframe k = pair.Value;
                    if (T._graphLockedTracks.Contains(k.parent))
                        continue;
                    SortedList<float, Keyframe> keys = k.parent.keyframes;
                    int index = keys.IndexOfValue(k);
                    // A smooth cubic first, which clears the mark a sampled easing leaves and gives the
                    // segment handles to set the type of.
                    k.curve = new AnimationCurve(T._hermitePreset.keys);
                    if (index + 1 < keys.Count)
                        HandleMath.ConvertSegment(keys, index);
                    HandleMath.Convert(keys);
                    if (k.handles == null)
                        continue;
                    k.handles.rightType = HandleType.AutoClamped;
                    if (index + 1 < keys.Count && keys.Values[index + 1].handles != null)
                        keys.Values[index + 1].handles.leftType = HandleType.AutoClamped;
                    HandleMath.SyncEasingAt(keys, index);
                }
                AfterInterpChange();
            }

            private void AfterInterpChange()
            {
                T.RefreshInterpolation();
                // The Classic window's curve and graph are hidden, but they hold state of their own;
                // keeping them current must not be able to stop the edit from landing.
                try
                {
                    T.UpdateGrid();
                }
                catch (Exception e)
                {
                    Logger.LogDebug("Classic refresh after an interpolation change: " + e.Message);
                }
                Touch();
            }
            #endregion

            #region The easing grid
            /// <summary>
            /// openEasing(): a family per row, a direction per column, each cell drawn as its own curve.
            /// Thirty easings in one look, instead of thirty lines of menu.
            /// </summary>
            private void OpenEasingGrid(float x, float y, string current)
            {
                CloseMenu();
                EnsureBlocker();
                Image box = Kit.Box("Egrid", _menuLayer, Pal.C(0x1E2025), 4f);
                box.raycastTarget = true;
                RectTransform rect = box.rectTransform;
                Kit.Col(box.gameObject, 0f, new RectOffset(8, 8, 6, 6));
                ContentSizeFitter fit = box.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Kit.Ring("Border", rect, Pal.C(0x0D0E10), 4f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                // .erow.ehead
                RectTransform head = ERow(rect);
                Kit.Size(Kit.Node("E", head).gameObject, 50f, 22f);
                foreach (string d in new[] { "In", "Out", "In-out" })
                {
                    Text t = Kit.Text(d, head, d, 10, Pal.C(0x6B6E74), TextAnchor.MiddleCenter);
                    Kit.Size(t.gameObject, 36f, 22f);
                }
                foreach (EasingKind kind in TimelineEasing.All)
                {
                    RectTransform row = ERow(rect);
                    Text name = Kit.Text("En", row, EasingName(kind), 11, Pal.C(0x9A9DA2));
                    Kit.Size(name.gameObject, 50f, 22f);
                    foreach (EasingDirection d in _directions)
                    {
                        EasingKind k = kind;
                        EasingDirection dir = d;
                        string label = EasingName(kind) + " " + DirectionName(d);
                        ECell(row, kind, d, label == current, label, () =>
                        {
                            CloseMenu();
                            ApplyFamily(k, dir);
                        });
                    }
                }
                Text note = Kit.Paragraph("Enote", rect, "In starts slow, Out ends slow. Applies to every selected key.", 10, Pal.C(0x6B6E74));
                note.gameObject.AddComponent<LayoutElement>().preferredWidth = 170f;
                note.lineSpacing = 1.1f;

                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                float w = rect.rect.width, h = rect.rect.height;
                PlaceInWindow(rect, Mathf.Clamp(x, 2f, winW - w - 2f), Mathf.Clamp(y, 2f, winH - h - 2f));
                _openMenus.Add(rect);
            }

            /// <summary>.erow: a 50 pixel name and three 36 pixel cells, 3 apart, 22 high.</summary>
            private static RectTransform ERow(RectTransform parent)
            {
                RectTransform row = Kit.Node("Erow", parent);
                Kit.Row(row.gameObject, 3f);
                Kit.Size(row.gameObject, -1f, 22f);
                return row;
            }

            /// <summary>.ecell: the easing's own curve in a 30 by 18 box; the accent under the pointer.</summary>
            private static void ECell(RectTransform row, EasingKind kind, EasingDirection direction, bool on, string tip, Action act)
            {
                Image cell = Kit.Box("Ecell", row, new Color(0f, 0f, 0f, 0f), 3f);
                cell.raycastTarget = true;
                Kit.Size(cell.gameObject, 36f, 20f);
                Clickable c = cell.gameObject.AddComponent<Clickable>();
                c.background = cell;
                c.normal = new Color(0f, 0f, 0f, 0f);
                c.hover = Pal.accent;
                c.pressed = c.hover;
                c.tooltip = tip;
                c.onClick = act;
                if (on)
                    Kit.Ring("On", cell.transform, Pal.accent, 3f);
                RectTransform art = Kit.Node("Art", cell.transform);
                art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
                art.sizeDelta = new Vector2(30f, 18f);
                EaseThumb thumb = art.gameObject.AddComponent<EaseThumb>();
                thumb.kind = kind;
                thumb.direction = direction;
                thumb.tint = on ? Pal.accent : Pal.C(0xC9CDD3);
                c.Tint(thumb.tint, Pal.onAccent, thumb);
            }
            #endregion
        }
    }

    /// <summary>
    /// An easing drawn as its curve, the playground's thumbPath() in a 40 by 28 box (viewBox 0 -3 40 28)
    /// scaled to the cell. Overshooting easings run past the box, as they do there.
    /// </summary>
    internal class EaseThumb : Paint, ITint
    {
        public EasingKind kind;
        public EasingDirection direction;
        public Color tint = Color.white;
        private readonly List<Vector2> _points = new List<Vector2>();

        public override void OnEnable()
        {
            base.OnEnable();
            Redraw();
        }

        public override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Redraw();
        }

        public void SetTint(Color c)
        {
            if (tint == c)
                return;
            tint = c;
            Redraw();
        }

        public void Redraw()
        {
            if (isActiveAndEnabled == false)
                return;
            float sx = width / 40f, sy = height / 28f;
            _points.Clear();
            for (int i = 0; i <= 30; ++i)
            {
                float u = i / 30f;
                float f = TimelineEasing.Evaluate(kind, direction, u);
                _points.Add(new Vector2((2f + u * 36f) * sx, (19f - f * 15f + 3f) * sy));
            }
            Begin();
            Stroke(_points, 1.8f * sx, tint, false);
            End();
        }
    }
}
