using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Timeline.View;
using UILib.EventHandlers;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        /// <summary>
        /// One colour for every plain keyframe, whatever the theme. Clear means it follows the theme,
        /// which is how a new install starts.
        /// </summary>
        internal static ConfigEntry<Color> ConfigKeyframeColor { get; private set; }

        private void BindKeyframeColor(string themeSection)
        {
            ConfigKeyframeColor = Config.Bind(themeSection, "Keyframe", new Color(0f, 0f, 0f, 0f),
                    "Colour of an ordinary keyframe everywhere it is drawn. Fully transparent means it follows the theme. Breakdowns, holds, extremes and jitter keep their own colours, because the colour is what tells them apart.");
            ConfigKeyframeColor.SettingChanged += (sender, args) => { if (_view != null) _view.Touch(); };
        }

        internal sealed partial class View
        {
            private bool _themeOpen, _themeMin;
            private RectTransform _twin;
            private RectTransform _tbody;
            private Vector2? _twinPos;

            /// <summary>The plain keyframe's colour: the one picked in the Theme window, or the theme's.</summary>
            public static Color KeyframeColor
            {
                get
                {
                    Color c = ConfigKeyframeColor == null ? new Color(0f, 0f, 0f, 0f) : ConfigKeyframeColor.Value;
                    if (c.a < 0.01f)
                        return Pal.C(0xD6D9DE);
                    c.a = 1f;
                    return c;
                }
            }

            /// <summary>toggleTheme(): opens it, brings it back from the taskbar, or closes it.</summary>
            public void ToggleTheme()
            {
                if (_themeOpen == false)
                {
                    _themeOpen = true;
                    _themeMin = false;
                }
                else if (_themeMin)
                    _themeMin = false;
                else
                    _themeOpen = false;
                RenderTheme();
                RenderTaskbar();
                _headerDirty = true;
            }

            private bool ThemeShown()
            {
                return _themeOpen && _themeMin == false;
            }

            /// <summary>#twin: a floating window 264 wide, the theme settings in .tbody.</summary>
            private void RenderTheme()
            {
                if (_themeOpen == false || _themeMin)
                {
                    if (_twin != null)
                        _twin.gameObject.SetActive(false);
                    return;
                }
                if (_twin == null)
                {
                    Image win = Kit.Box("Twin", canvas.transform, Pal.C(0x2C2F35), 6f);
                    win.raycastTarget = true;
                    _twin = win.rectTransform;
                    _twin.anchorMin = _twin.anchorMax = new Vector2(0.5f, 0f);
                    _twin.pivot = new Vector2(0f, 1f);
                    _twin.anchoredPosition = _twinPos ?? _win.anchoredPosition + new Vector2(winW - 264f - 40f, 380f);
                    win.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                    ContentSizeFitter fit = win.gameObject.AddComponent<ContentSizeFitter>();
                    fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    Kit.Col(win.gameObject, 0f);
                    _twin.sizeDelta = new Vector2(264f, 0f);

                    Image head = Kit.Box("Fhead", _twin, Pal.C(0x181A1E));
                    head.raycastTarget = true;
                    Kit.Size(head.gameObject, -1f, 22f);
                    RectTransform row = Kit.Node("Row", head.transform).Fill(8f, 0f, 3f, 1f);
                    Kit.Row(row.gameObject, 2f);
                    Kit.Box("Border", head.transform, Pal.C(0x111215)).rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f);
                    Kit.Icon("Icon", row, "palette", Pal.C(0xE4E7EC));
                    RectTransform gap = Kit.Node("Gap", row);
                    Kit.Size(gap.gameObject, 4f, 1f);
                    Kit.Text("Ft", row, "Theme", 11, Pal.C(0xE4E7EC), TextAnchor.MiddleLeft, true);
                    Kit.Spacer(row);
                    FB(row, "min", "Minimise to the taskbar", () => { _themeMin = true; RenderTheme(); RenderTaskbar(); _headerDirty = true; });
                    FB(row, "close", "Close", () => { _themeOpen = false; RenderTheme(); RenderTaskbar(); _headerDirty = true; });
                    DragHandler drag = head.gameObject.AddComponent<DragHandler>();
                    Vector2 start = Vector2.zero, mouse = Vector2.zero;
                    drag.onBeginDrag = e =>
                    {
                        start = _twin.anchoredPosition;
                        mouse = e.position / canvas.scaleFactor;
                    };
                    drag.onDrag = e =>
                    {
                        _twin.anchoredPosition = start + e.position / canvas.scaleFactor - mouse;
                        _twinPos = _twin.anchoredPosition;
                    };

                    _tbody = Kit.Node("Tbody", _twin);
                    Kit.Col(_tbody.gameObject, 7f, new RectOffset(10, 10, 8, 10));
                    Kit.Ring("Border", _twin, Pal.C(0x0D0E10), 6f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                }
                _twin.gameObject.SetActive(true);
                _twin.SetAsLastSibling();
                FillTheme();
            }

            /// <summary>renderTheme(): presets, colours, windows, size.</summary>
            private void FillTheme()
            {
                for (int i = _tbody.childCount - 1; i >= 0; --i)
                    UnityEngine.Object.Destroy(_tbody.GetChild(i).gameObject);
                _tbody.DetachChildren();

                Color[] current = { ConfigBackgroundColor.Value, ConfigAccentColor.Value, ConfigTextColor.Value, ConfigPlayheadColor.Value };

                Cap(_tbody, "PRESETS", null);
                RectTransform presets = Kit.Node("Presets", _tbody);
                GridLayoutGroup grid = presets.gameObject.AddComponent<GridLayoutGroup>();
                grid.spacing = new Vector2(4f, 4f);
                grid.cellSize = new Vector2((244f - 8f) / 3f, 34f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                foreach (KeyValuePair<string, Color[]> preset in _themePresets)
                {
                    Color[] colors = preset.Value;
                    bool on = SameColors(colors, current);
                    Image box = Kit.Box("Preset", presets, Pal.C(0x25282D), 4f);
                    box.raycastTarget = true;
                    Clickable c = box.gameObject.AddComponent<Clickable>();
                    c.background = box;
                    c.normal = c.hover = c.pressed = Pal.C(0x25282D);
                    if (on)
                        Kit.Ring("On", box.transform, Pal.accent, 4f);
                    VerticalLayoutGroup col = Kit.Col(box.gameObject, 4f);
                    col.childAlignment = TextAnchor.MiddleCenter;
                    col.childForceExpandWidth = false;
                    col.childControlWidth = true;
                    RectTransform strip = Kit.Node("Sw4", box.transform);
                    Kit.Row(strip.gameObject, 0f);
                    Kit.Size(strip.gameObject, 48f, 8f);
                    strip.gameObject.AddComponent<RectMask2D>();
                    foreach (Color sw in colors)
                        Kit.Flex(Kit.Box("I", strip, sw).gameObject, 8f);
                    Text name = Kit.Text("Name", box.transform, preset.Key, 10, on ? Pal.C(0xE4E7EC) : Pal.C(0x9A9DA2), TextAnchor.MiddleCenter);
                    c.Tint(name.color, Pal.C(0xE4E7EC), name);
                    c.onClick = () => T.ApplyThemeColors(colors);
                }

                Cap(_tbody, "COLOURS", null);
                string[] labels = { "Background", "Accent", "Text", "Playhead" };
                for (int i = 0; i < 4; ++i)
                {
                    int captured = i;
                    ColourRow(labels[i], current[i], hex =>
                    {
                        Color parsed;
                        if (ParseHex(hex, out parsed) == false)
                            return;
                        Color[] next = (Color[])current.Clone();
                        next[captured] = parsed;
                        T.ApplyThemeColors(next);
                    });
                }
                // The one the playground does not have: every plain keyframe's colour, whatever the theme.
                Color key = ConfigKeyframeColor.Value;
                ColourRow("Keyframe", KeyframeColor, hex =>
                {
                    Color parsed;
                    if (hex.Trim().Length == 0 || hex.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase))
                        ConfigKeyframeColor.Value = new Color(0f, 0f, 0f, 0f);
                    else if (ParseHex(hex, out parsed))
                        ConfigKeyframeColor.Value = parsed;
                    RenderTheme();
                }, key.a < 0.01f ? "Auto" : null);

                Cap(_tbody, "WINDOWS", null);
                RectTransform line = Line(_tbody);
                Text opLabel = Kit.Text("L", line, "Opacity", 11, Pal.C(0x9A9DA2));
                Kit.Size(opLabel.gameObject, 66f, 22f);
                Text opValue = null;
                ThemeSlider(line, ConfigUIOpacity.Value, 0.3f, 1f, v =>
                {
                    ConfigUIOpacity.Value = Mathf.Round(v * 100f) / 100f;
                    if (opValue != null)
                        opValue.text = Mathf.RoundToInt(ConfigUIOpacity.Value * 100f) + " %";
                }, true);
                opValue = Kit.Text("V", line, Mathf.RoundToInt(ConfigUIOpacity.Value * 100f) + " %", 11, Pal.C(0xE4E7EC), TextAnchor.MiddleRight);
                Kit.Size(opValue.gameObject, 36f, 22f);
                RectTransform away = Line(_tbody);
                Cb(away, "Only while the mouse is away from them", ConfigUIOpacityAway.Value, () => { ConfigUIOpacityAway.Value = !ConfigUIOpacityAway.Value; RenderTheme(); });

                Cap(_tbody, "SIZE", null);
                line = Line(_tbody);
                Text scLabel = Kit.Text("L", line, "UI scale", 11, Pal.C(0x9A9DA2));
                Kit.Size(scLabel.gameObject, 66f, 22f);
                Text scValue = null;
                // Applied when the slider is let go: scaled while dragging, the window would grow away
                // from under the pointer.
                ThemeSlider(line, Mathf.InverseLerp(0.5f, 2f, ConfigUIScale.Value), 0f, 1f, v =>
                {
                    float scale = Mathf.Round(Mathf.Lerp(0.5f, 2f, v) * 20f) / 20f;
                    if (scValue != null)
                        scValue.text = Mathf.RoundToInt(scale * 100f) + " %";
                    _pendingViewScale = scale;
                }, false);
                scValue = Kit.Text("V", line, Mathf.RoundToInt(ConfigUIScale.Value * 100f) + " %", 11, Pal.C(0xE4E7EC), TextAnchor.MiddleRight);
                Kit.Size(scValue.gameObject, 36f, 22f);
                RectTransform screen = Line(_tbody);
                Cb(screen, "Grow with the screen resolution", ConfigUIScaleAuto.Value, () => { ConfigUIScaleAuto.Value = !ConfigUIScaleAuto.Value; RenderTheme(); });

                line = Line(_tbody);
                Btn(line, "Reset to default", true, () =>
                {
                    ConfigUIOpacity.Value = 1f;
                    ConfigUIOpacityAway.Value = false;
                    ConfigUIScale.Value = 1f;
                    ConfigUIScaleAuto.Value = true;
                    ConfigKeyframeColor.Value = new Color(0f, 0f, 0f, 0f);
                    T.ApplyThemeColors(_themePresets[0].Value);
                    RenderTheme();
                }, null);
                Note(_tbody, "Saved with the plugin settings. Type a colour as #RRGGBB. Keyframe accepts Auto to follow the theme.");
            }

            private float _pendingViewScale;

            /// <summary>.crow: a label, the colour, and its hex code to type into.</summary>
            private void ColourRow(string label, Color color, Action<string> commit, string shown = null)
            {
                RectTransform row = Kit.Node("Crow", _tbody);
                Kit.Row(row.gameObject, 6f);
                Kit.Size(row.gameObject, -1f, 22f);
                Text l = Kit.Text("L", row, label, 11, Pal.C(0x9A9DA2));
                Kit.Size(l.gameObject, 66f, 22f);
                Image sw = Kit.Box("Sw", row, color, 3f);
                Kit.Size(sw.gameObject, 26f, 20f);
                // input[type=color]: a click opens Studio's colour picker.
                sw.raycastTarget = true;
                Clickable pick = sw.gameObject.AddComponent<Clickable>();
                pick.background = sw;
                pick.normal = pick.hover = pick.pressed = color;
                pick.tooltip = "Pick a colour";
                pick.onClick = () => PickColour(label, color, c => commit("#" + ColorUtility.ToHtmlStringRGB(c)));
                Fld(row, null, 0, shown ?? "#" + ColorUtility.ToHtmlStringRGB(color), null, commit);
            }

            private static bool ParseHex(string text, out Color color)
            {
                string hex = text.Trim();
                if (hex.StartsWith("#") == false)
                    hex = "#" + hex;
                return ColorUtility.TryParseHtmlString(hex, out color) && hex.Length == 7;
            }

            private static bool SameColors(Color[] a, Color[] b)
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (ColorUtility.ToHtmlStringRGB(a[i]) != ColorUtility.ToHtmlStringRGB(b[i]))
                        return false;
                }
                return true;
            }

            /// <summary>A range input from min to max; live calls set while dragging, otherwise only on release.</summary>
            private void ThemeSlider(RectTransform parent, float value, float min, float max, Action<float> set, bool live)
            {
                RectTransform holder = Kit.Node("Slider", parent);
                Kit.Flex(holder.gameObject, 18f);
                Image hit = holder.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                RectTransform art = Kit.Node("Art", holder).Fill();
                SliderView view = art.gameObject.AddComponent<SliderView>();
                view.value = Mathf.InverseLerp(min, max, value);
                Action<UnityEngine.EventSystems.PointerEventData> apply = e =>
                {
                    Vector2 local;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(holder, e.position, null, out local);
                    Rect r = holder.rect;
                    view.value = Mathf.Clamp01((local.x - r.xMin - 6f) / Mathf.Max(1f, r.width - 12f));
                    view.Redraw();
                    set(Mathf.Lerp(min, max, view.value));
                };
                holder.gameObject.AddComponent<UILib.EventHandlers.PointerDownHandler>().onPointerDown = apply;
                DragHandler drag = holder.gameObject.AddComponent<DragHandler>();
                drag.onDrag = apply;
                if (live == false)
                {
                    holder.gameObject.AddComponent<PointerUpHandler>().onPointerUp = e =>
                    {
                        if (_pendingViewScale > 0f)
                        {
                            float scale = _pendingViewScale;
                            _pendingViewScale = 0f;
                            ConfigUIScale.Value = scale;
                        }
                    };
                }
            }

            private void AddThemePill()
            {
                if (_themeOpen == false || _themeMin == false)
                    return;
                Clickable pill = Pill(_taskbar, "Tpill", 22f, 10f, 6f, Pal.C(0x2C2F35), Pal.C(0x373A3F), 3f);
                Kit.Ring("Border", pill.transform, Pal.C(0x0D0E10), 3f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Kit.Icon("Icon", pill.transform, "palette", Pal.C(0xE4E7EC));
                Kit.Text("Text", pill.transform, "Theme", 11, Pal.C(0xE4E7EC));
                pill.tooltip = "Restore";
                pill.onClick = () =>
                {
                    _themeMin = false;
                    RenderTheme();
                    RenderTaskbar();
                    _headerDirty = true;
                };
            }
        }
    }
}
