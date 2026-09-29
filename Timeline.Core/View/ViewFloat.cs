using System;
using System.Collections.Generic;
using Studio;
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
            /// <summary>
            /// A floating window like the Theme one: a title bar to drag it by, ▁ to send it to the
            /// taskbar, × to close it, and a body filled again whenever the scene changes.
            /// </summary>
            private sealed class FloatWin
            {
                public string name, icon, title;
                public float width;
                /// <summary>Where it opens the first time, from the Timeline window's top left corner, y upwards.</summary>
                public Vector2 offset;
                public bool open, min;
                public RectTransform win, body;
                public Vector2? pos;
                public Action<RectTransform> fill;
                /// <summary>Set while the body is being dragged in, so a refresh does not pull it away.</summary>
                public bool busy;
            }

            private readonly List<FloatWin> _floats = new List<FloatWin>();
            private int _floatSignature;

            private FloatWin MakeFloat(string name, string icon, string title, float width, Vector2 offset, Action<RectTransform> fill)
            {
                var w = new FloatWin { name = name, icon = icon, title = title, width = width, offset = offset, fill = fill };
                _floats.Add(w);
                return w;
            }

            /// <summary>Opens it, brings it back from the taskbar, or closes it.</summary>
            private void ToggleFloat(FloatWin w)
            {
                if (w.open == false)
                {
                    w.open = true;
                    w.min = false;
                }
                else if (w.min)
                    w.min = false;
                else
                    w.open = false;
                RenderFloat(w);
                RenderTaskbar();
                _headerDirty = true;
            }

            private void ShowFloat(FloatWin w)
            {
                w.open = true;
                w.min = false;
                RenderFloat(w);
                RenderTaskbar();
            }

            private void RenderFloat(FloatWin w)
            {
                if (w.open == false || w.min)
                {
                    if (w.win != null)
                        w.win.gameObject.SetActive(false);
                    return;
                }
                if (w.win == null)
                    BuildFloat(w);
                w.win.gameObject.SetActive(true);
                w.win.SetAsLastSibling();
                FillFloat(w);
            }

            private void FillFloat(FloatWin w)
            {
                for (int i = w.body.childCount - 1; i >= 0; --i)
                    UnityEngine.Object.Destroy(w.body.GetChild(i).gameObject);
                w.body.DetachChildren();
                w.fill(w.body);
            }

            private void BuildFloat(FloatWin w)
            {
                Image win = Kit.Box(w.name, canvas.transform, Pal.C(0x2C2F35), 6f);
                win.raycastTarget = true;
                w.win = win.rectTransform;
                w.win.anchorMin = w.win.anchorMax = new Vector2(0.5f, 0f);
                w.win.pivot = new Vector2(0f, 1f);
                w.win.anchoredPosition = w.pos ?? OnScreen(_win.anchoredPosition + w.offset, w.width);
                win.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                ContentSizeFitter fit = win.gameObject.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Kit.Col(win.gameObject, 0f);
                w.win.sizeDelta = new Vector2(w.width, 0f);

                Image head = Kit.Box("Fhead", w.win, Pal.C(0x181A1E));
                head.raycastTarget = true;
                Kit.Size(head.gameObject, -1f, 22f);
                RectTransform row = Kit.Node("Row", head.transform).Fill(8f, 0f, 3f, 1f);
                Kit.Row(row.gameObject, 2f);
                Kit.Box("Border", head.transform, Pal.C(0x111215)).rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f);
                Kit.Icon("Icon", row, w.icon, Pal.C(0xE4E7EC));
                RectTransform gap = Kit.Node("Gap", row);
                Kit.Size(gap.gameObject, 4f, 1f);
                Kit.Text("Ft", row, w.title, 11, Pal.C(0xE4E7EC), TextAnchor.MiddleLeft, true);
                Kit.Spacer(row);
                FB(row, "min", "Minimise to the taskbar", () => { w.min = true; RenderFloat(w); RenderTaskbar(); _headerDirty = true; });
                FB(row, "close", "Close", () => { w.open = false; RenderFloat(w); RenderTaskbar(); _headerDirty = true; });
                DragHandler drag = head.gameObject.AddComponent<DragHandler>();
                Vector2 start = Vector2.zero, mouse = Vector2.zero;
                drag.onBeginDrag = e =>
                {
                    start = w.win.anchoredPosition;
                    mouse = e.position / canvas.scaleFactor;
                };
                drag.onDrag = e =>
                {
                    w.win.anchoredPosition = start + e.position / canvas.scaleFactor - mouse;
                    w.pos = w.win.anchoredPosition;
                };

                w.body = Kit.Node("Body", w.win);
                Kit.Col(w.body.gameObject, 7f, new RectOffset(10, 10, 8, 10));
                Kit.Ring("Border", w.win, Pal.C(0x0D0E10), 6f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }

            /// <summary>Kept on the screen: the title bar in reach, whatever the resolution.</summary>
            private Vector2 OnScreen(Vector2 topLeft, float width)
            {
                Rect screen = ((RectTransform)canvas.transform).rect;
                return new Vector2(Mathf.Clamp(topLeft.x, -screen.width / 2f + 4f, screen.width / 2f - width - 4f),
                                   Mathf.Clamp(topLeft.y, 120f, screen.height - 4f));
            }

            /// <summary>The open windows follow the scene: filled again when it changes, unless being typed or dragged in.</summary>
            private void TickFloats()
            {
                unchecked
                {
                    GuideObject node = GuideObjectManager.Instance == null ? null : GuideObjectManager.Instance.selectObject;
                    int s = _stateSignature * 31 + (node == null ? 0 : node.GetHashCode()) + T._keySets.Count * 7;
                    foreach (Interpolable track in T._interpolables.Values)
                        s = s * 31 + track.keyframes.Count;
                    if (s == _floatSignature)
                        return;
                    _floatSignature = s;
                }
                GameObject focus = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
                foreach (FloatWin w in _floats)
                {
                    if (w.open == false || w.min || w.win == null || w.busy)
                        continue;
                    if (focus != null && focus.transform.IsChildOf(w.body) && focus.GetComponent<InputField>() != null)
                        continue;
                    FillFloat(w);
                }
            }

            public void RefreshFloats()
            {
                _floatSignature = 0;
            }

            private void AddFloatPills()
            {
                foreach (FloatWin w in _floats)
                {
                    if (w.open == false || w.min == false)
                        continue;
                    FloatWin captured = w;
                    Clickable pill = Pill(_taskbar, "Tpill", 22f, 10f, 6f, Pal.C(0x2C2F35), Pal.C(0x373A3F), 3f);
                    Kit.Ring("Border", pill.transform, Pal.C(0x0D0E10), 3f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    Kit.Icon("Icon", pill.transform, w.icon, Pal.C(0xE4E7EC));
                    Kit.Text("Text", pill.transform, w.title, 11, Pal.C(0xE4E7EC));
                    pill.tooltip = "Restore";
                    pill.onClick = () =>
                    {
                        captured.min = false;
                        RenderFloat(captured);
                        RenderTaskbar();
                        _headerDirty = true;
                    };
                }
            }

            /// <summary>The windows lived on the old canvas; they come back where they were after a rebuild.</summary>
            private void RebuildFloats()
            {
                foreach (FloatWin w in _floats)
                {
                    w.win = null;
                    w.body = null;
                    w.busy = false;
                    if (w.open)
                        RenderFloat(w);
                }
            }
        }
    }
}
