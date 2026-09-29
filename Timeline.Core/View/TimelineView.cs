using System.Collections.Generic;
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
        private View _view;

        /// <summary>
        /// The Timeline window, rebuilt from the playground.
        ///
        /// It is a view over the plugin's own state: the tracks, keyframes, selection, markers and undo
        /// history all stay where they were, and this reads them and calls the same operations the
        /// Classic window calls. Nested in Timeline so it can, without widening what the plugin exposes.
        ///
        /// It is laid out the way the playground's stylesheet lays it out, piece for piece: the names of
        /// the parts are the names of the rules they copy (hdr, chan, props, status), so the two can be
        /// read side by side.
        /// </summary>
        internal sealed partial class View
        {
            private readonly Timeline T;

            public Canvas canvas;
            private CanvasGroup _group;
            private RectTransform _canvasRect;
            private RectTransform _win;
            private RectTransform _body;

            #region State, as the playground's ui object
            public string editor = "dope";
            public float t0;
            public float pps = 100f;
            public float scrollY;
            public bool props;
            public string tab = "Keyframe";
            public string snap = "frame";
            public bool showPath;
            public bool showHandles = true;
            public bool normalize;
            public bool onlySel;
            public bool showSummary = true;
            public bool compact;
            public string groupKeys = "collapsed";
            public float chanW = 260f;
            public float winW = 1180f, winH = 590f;
            public string hover = "";
            public readonly HashSet<object> collapsed = new HashSet<object>();
            public string search = "";
            #endregion

            public const float RUL = 26f, LANE = 26f, HDR = 26f, STATUS = 22f;
            public float ROW { get { return compact ? 18f : 22f; } }

            public View(Timeline timeline)
            {
                T = timeline;
            }

            public bool visible
            {
                get { return canvas != null && canvas.gameObject.activeSelf; }
                set
                {
                    if (canvas == null)
                        return;
                    canvas.gameObject.SetActive(value);
                    if (value)
                    {
                        _rowsDirty = true;
                        _headerDirty = true;
                        _propsDirty = true;
                    }
                    else
                    {
                        CloseMenu();
                        if (_pathDrawer != null)
                            _pathDrawer.enabled = false;
                    }
                }
            }

            #region Build
            public void Build()
            {
                Pal.Set(ConfigBackgroundColor.Value, ConfigAccentColor.Value, ConfigTextColor.Value, ConfigPlayheadColor.Value);

                canvas = UIUtility.CreateNewUISystem("Timeline View");
                canvas.sortingOrder = 1001;
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                    scaler.enabled = false;
                canvas.scaleFactor = EffectiveUIScale();
                canvas.referencePixelsPerUnit = 100f;
                canvas.pixelPerfect = true;
                _group = canvas.gameObject.AddComponent<CanvasGroup>();
                _canvasRect = (RectTransform)canvas.transform;

                BuildWindow();
                BuildTooltip();
                if (_stateLoaded == false)
                {
                    _stateLoaded = true;
                    LoadState();
                }
                canvas.gameObject.SetActive(false);
                FitTime();
            }

            /// <summary>Throws the window away and builds it again, which is how a new theme is applied.</summary>
            public void Rebuild()
            {
                if (canvas == null)
                    return;
                bool wasVisible = visible;
                bool themeOpen = _themeOpen, themeMin = _themeMin;
                Vector2 position = _win.anchoredPosition;
                Object.Destroy(canvas.gameObject);
                canvas = null;
                _rowWidgets.Clear();
                _tabs.Clear();
                // Everything below lived on the old canvas.
                _openMenus.Clear();
                _openMenuName = null;
                _menuBlocker = null;
                _pwin = null;
                _pwinBody = null;
                _taskbar = null;
                _propsFloat = false;
                _propsMin = false;
                _statusShown = null;
                _twin = null;
                _tbody = null;
                _addp = null;
                _apList = null;
                _guide = null;
                _guideState = -1;
                Build();
                _win.anchoredPosition = position;
                LayoutShadow();
                visible = wasVisible;
                _themeOpen = themeOpen;
                _themeMin = themeMin;
                if (_themeOpen)
                    RenderTheme();
                RebuildFloats();
                RenderTaskbar();
            }

            /// <summary>.win: the window itself, 1180 by 590, rounded by 6, with a hairline border.</summary>
            private void BuildWindow()
            {
                Image shadow = Kit.Box("Shadow", canvas.transform, new Color(0f, 0f, 0f, 0.45f));
                shadow.sprite = Kit.Soft(6f, 20f);
                shadow.type = Image.Type.Sliced;
                _shadow = shadow.rectTransform;

                Image win = Kit.Box("Window", canvas.transform, Pal.C(0x21242A), 6f);
                _win = win.rectTransform;
                _win.anchorMin = _win.anchorMax = new Vector2(0.5f, 0f);
                _win.pivot = new Vector2(0f, 1f);
                _win.sizeDelta = new Vector2(winW, winH);
                _win.anchoredPosition = new Vector2(-winW / 2f, winH + 40f);
                win.raycastTarget = true;
                // overflow:hidden with a radius: the children are cut to the rounded corners.
                win.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                BuildHeader();

                _body = Kit.Node("Body", _win).Css(0f, HDR, 0f, STATUS);
                BuildChannels();
                BuildGrid();
                BuildProps();
                BuildStatus();
                BuildGrip();
                LayoutBody();

                _menuLayer = Kit.Node("Menus", canvas.transform).Fill();

                // The border sits on top of everything, as a border does.
                Kit.Ring("Border", _win, Pal.C(0x111215), 6f);
                LayoutShadow();
            }

            private RectTransform _shadow;
            private bool _stateLoaded;

            private void LayoutShadow()
            {
                if (_shadow == null)
                    return;
                // box-shadow: 0 18px 40px rgba(0,0,0,.45), approximated by a soft box behind the window.
                _shadow.anchorMin = _shadow.anchorMax = _win.anchorMin;
                _shadow.pivot = _win.pivot;
                _shadow.sizeDelta = _win.sizeDelta + new Vector2(40f, 40f);
                _shadow.anchoredPosition = _win.anchoredPosition + new Vector2(-20f, 20f - 18f);
                _shadow.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
                _shadow.SetSiblingIndex(0);
                // The taskbar sits on the window's top edge, wherever the window is moved to.
                if (_taskbar != null)
                {
                    _taskbar.anchorMin = _taskbar.anchorMax = _win.anchorMin;
                    _taskbar.pivot = Vector2.zero;
                    _taskbar.anchoredPosition = _win.anchoredPosition + new Vector2(8f, 6f);
                }
            }

            /// <summary>.grip: the resize corner, two diagonal strokes.</summary>
            private void BuildGrip()
            {
                RectTransform grip = Kit.Node("Grip", _win).Css(float.NaN, float.NaN, 0f, 0f, 14f, 14f);
                Paint p = grip.gameObject.AddComponent<Paint>();
                p.raycastTarget = true;
                p.Begin();
                Color c = Pal.C(0x6B6E74);
                p.Line(14f * 0.45f + 7.7f, 14f, 14f, 14f * 0.45f + 7.7f, 1f, c);
                p.Line(14f * 0.68f + 4.5f, 14f, 14f, 14f * 0.68f + 4.5f, 1f, c);
                p.End();
                DragHandler drag = grip.gameObject.AddComponent<DragHandler>();
                Vector2 startSize = Vector2.zero, startMouse = Vector2.zero;
                drag.onBeginDrag = e =>
                {
                    startSize = new Vector2(winW, winH);
                    startMouse = e.position / canvas.scaleFactor;
                };
                drag.onDrag = e =>
                {
                    Vector2 d = e.position / canvas.scaleFactor - startMouse;
                    Vector2 max = _canvasRect.rect.size - new Vector2(20f, 20f);
                    winW = Mathf.Round(Mathf.Clamp(startSize.x + d.x, 760f, max.x));
                    winH = Mathf.Round(Mathf.Clamp(startSize.y - d.y, 360f, max.y));
                    ApplyWindowSize();
                };
            }

            private void ApplyWindowSize()
            {
                _win.sizeDelta = new Vector2(winW, winH);
                LayoutShadow();
                _headerDirty = true;
                _rowsDirty = true;
                LayoutBody();
            }
            #endregion

            #region Per frame
            public void Tick()
            {
                if (canvas == null)
                    return;
                if (Mathf.Abs(canvas.scaleFactor - EffectiveUIScale()) > 0.0001f)
                    canvas.scaleFactor = EffectiveUIScale();
                if (visible == false)
                    return;

                float alpha = Mathf.Clamp(ConfigUIOpacity.Value, 0.1f, 1f);
                if (ConfigUIOpacityAway.Value)
                    alpha = PointerOverWindow() ? 1f : alpha;
                _group.alpha = alpha;

                CheckState();
                if (_headerDirty)
                    RenderHeader();
                if (_rowsDirty)
                    RenderRows();
                TickReveal();
                if (_propsDirty)
                    RenderProps();
                TickFloats();
                TickHeader();
                DrawGrid();
                TickStatus();
                TickKeys();
                TickTooltip();
                TickPath();
                TickSaveState();
                TickAdd();
                TickGuide();
            }

            private bool _headerDirty = true, _rowsDirty = true, _propsDirty = true;
            private int _stateSignature;

            /// <summary>
            /// Notices changes made elsewhere: through the Classic code paths, by undo, by Studio. The grid
            /// is drawn every frame anyway; the rows and the properties are rebuilt only when this changes.
            /// </summary>
            private void CheckState()
            {
                unchecked
                {
                    int s = 17;
                    s = s * 31 + T._interpolables.Count;
                    s = s * 31 + T._selectedInterpolables.Count;
                    foreach (Interpolable i in T._selectedInterpolables)
                        s = s * 31 + i.GetHashCode();
                    s = s * 31 + T._selectedKeyframes.Count;
                    foreach (KeyValuePair<float, Keyframe> k in T._selectedKeyframes)
                        s = s * 31 + k.Value.GetHashCode() + k.Key.GetHashCode();
                    s = s * 31 + (T._selectedOCI == null ? 0 : T._selectedOCI.GetHashCode());
                    s = s * 31 + T._undoStack.Count * 7 + T._redoStack.Count;
                    s = s * 31 + T._graphHiddenTracks.Count * 3 + T._graphLockedTracks.Count;
                    s = s * 31 + (T._isPlaying ? 1 : 0);
                    s = s * 31 + Pal.version;
                    if (s != _stateSignature)
                    {
                        _stateSignature = s;
                        _rowsDirty = true;
                        _headerDirty = true;
                        _propsDirty = true;
                    }
                }
            }

            private Interpolable _reveal;

            /// <summary>Scrolls the list to a track once its row exists, when it is out of sight.</summary>
            public void Reveal(Interpolable track)
            {
                _reveal = track;
            }

            private void TickReveal()
            {
                if (_reveal == null || _rowsDirty)
                    return;
                Interpolable track = _reveal;
                _reveal = null;
                foreach (Row row in _rows)
                {
                    if (row.type != RowType.Track || row.tr != track)
                        continue;
                    float view = _rowsClip.rect.height;
                    if (row.y < scrollY || row.y + row.h > scrollY + view)
                        SetScroll(row.y - view / 2f + row.h / 2f);
                    return;
                }
            }

            /// <summary>The window itself, for plugins that place things next to it.</summary>
            public RectTransform window { get { return _win; } }

            public void Touch()
            {
                _rowsDirty = true;
                _headerDirty = true;
                _propsDirty = true;
            }

            private bool PointerOverWindow()
            {
                return _win != null && RectTransformUtility.RectangleContainsScreenPoint(_win, Input.mousePosition, null);
            }

            /// <summary>
            /// The keys, only while the mouse is over the window and nothing is being typed into: outside
            /// it the keyboard belongs to Studio and the other plugins.
            /// </summary>
            private void TickKeys()
            {
                if (PointerOverAny() == false || Typing())
                    return;
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (NlaKeys(ctrl, shift))
                    return;
                if (ctrl && Input.GetKeyDown(KeyCode.Z))
                {
                    if (shift)
                        T.Redo();
                    else
                        T.Undo();
                }
                else if (ctrl && Input.GetKeyDown(KeyCode.C))
                    T.CopyKeyframes();
                else if (ctrl && Input.GetKeyDown(KeyCode.V))
                    T.PasteKeyframes();
                else if (ctrl && Input.GetKeyDown(KeyCode.X))
                    T.CutKeyframes();
                else if (ctrl && Input.GetKeyDown(KeyCode.I))
                    InvertSelection();
                else if ((Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Delete)) && ctrl == false && T._selectedKeyframes.Count != 0)
                    DeleteKeys();
                else if (Input.GetKeyDown(KeyCode.Space) && ctrl == false)
                    TogglePlay();
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                    StepFrame(-1);
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                    StepFrame(1);
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                    T.JumpToKey(1);
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    T.JumpToKey(-1);
                else if (Input.GetKeyDown(KeyCode.Home))
                    FitAll();
                else if (Input.GetKeyDown(KeyCode.F) && ctrl == false)
                    FrameSelected();
                else if (Input.GetKeyDown(KeyCode.I) && ctrl == false)
                    T.KeySelectedTracks();
                else if (Input.GetKeyDown(KeyCode.H) && ctrl == false)
                {
                    if (alt)
                        T._graphHiddenTracks.Clear();
                    else
                        foreach (Interpolable t in T._selectedInterpolables)
                            T._graphHiddenTracks.Add(t);
                    Touch();
                }
                else if (Input.GetKeyDown(KeyCode.M) && ctrl == false)
                    T.AddMarker(T._playbackTime);
                else if (Input.GetKeyDown(KeyCode.A) && ctrl == false && shift == false)
                {
                    if (alt)
                        T.SelectKeyframes();
                    else
                        T.SelectAllKeyframesInScope();
                }
            }

            private static bool Typing()
            {
                GameObject selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
                if (selected == null)
                    return false;
                InputField field = selected.GetComponent<InputField>();
                return field != null && field.isFocused;
            }
            #endregion

            #region Playback and time
            public float X(float t)
            {
                return (t - t0) * pps;
            }

            public float Tt(float x)
            {
                return x / pps + t0;
            }

            private void FitTime()
            {
                float w = GridWidth();
                pps = (w - 30f) / Mathf.Max(T._duration, 0.01f);
                t0 = -14f / pps;
            }

            public void FitAll()
            {
                FitTime();
                if (editor == "graph")
                    FitGraph(false);
            }

            public void FrameSelected()
            {
                if (T._selectedKeyframes.Count == 0)
                {
                    FitAll();
                    return;
                }
                float a = float.MaxValue, b = float.MinValue;
                foreach (KeyValuePair<float, Keyframe> k in T._selectedKeyframes)
                {
                    a = Mathf.Min(a, k.Key);
                    b = Mathf.Max(b, k.Key);
                }
                if (b - a < 0.2f)
                {
                    a -= 0.5f;
                    b += 0.5f;
                }
                float span = (b - a) * 1.2f;
                pps = Mathf.Clamp(GridWidth() / span, 8f, 4000f);
                t0 = a - (b - a) * 0.1f;
                if (editor == "graph")
                    FitGraph(true);
            }

            public void TogglePlay()
            {
                if (T._isPlaying)
                    Pause();
                else
                    Play();
            }

            private void StepFrame(int direction)
            {
                isPlaying = false;
                float frame = 1f / Mathf.Max(T._desiredFrameRate, 1);
                float t = Mathf.Round(T._playbackTime / frame) * frame + direction * frame;
                T.SeekPlaybackTime(Mathf.Clamp(t, 0f, T._duration));
            }

            public void SetTime(float t)
            {
                isPlaying = false;
                T.SeekPlaybackTime(Mathf.Clamp(t, 0f, T._duration));
            }

            public float FrameSnap(float t)
            {
                float fps = Mathf.Max(T._desiredFrameRate, 1);
                return Mathf.Round(t * fps) / fps;
            }

            public static string Fmt(float t)
            {
                return (Mathf.Round(t * 100f) / 100f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }

            public static string Timecode(float t)
            {
                int m = Mathf.FloorToInt(t / 60f);
                float s = t - m * 60f;
                return m.ToString("00") + ":" + s.ToString("00.00", System.Globalization.CultureInfo.InvariantCulture);
            }
            #endregion
        }

        #region Hooks into the plugin
        private void BuildView()
        {
            try
            {
                _view = new View(this);
                _view.Build();
            }
            catch (System.Exception e)
            {
                // The Classic window still works, so a failure here should not take the plugin with it.
                Logger.LogError("The new Timeline window could not be built, falling back to the Classic one: " + e);
                _view = null;
            }
        }

        private float _viewErrorAt = -10f;

        private void TickView()
        {
            if (_view == null)
                return;
            try
            {
                _view.Tick();
            }
            catch (System.Exception e)
            {
                // Once every few seconds at most: an error in a per frame path would otherwise fill the log.
                if (Time.unscaledTime - _viewErrorAt > 5f)
                {
                    _viewErrorAt = Time.unscaledTime;
                    Logger.LogError("Timeline window: " + e);
                }
            }
        }
        #endregion
    }
}
