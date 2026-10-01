using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using Timeline.Graph;
using Timeline.View;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        #region Messages in the status line
        private static ManualLogSource _pluginLog;

        /// <summary>
        /// Timeline's messages, the "Select some keyframes first" kind, went to the corner of the game
        /// screen. With the new window open they go to its status line instead, as the playground's
        /// toasts do, and to the log file as information. The Classic window keeps the old behaviour.
        /// Warnings and errors are passed on untouched either way.
        /// </summary>
        private static ManualLogSource MakeRelayLog(ManualLogSource plugin)
        {
            _pluginLog = plugin;
            var relay = new ManualLogSource(plugin.SourceName);
            relay.LogEvent += (sender, e) =>
            {
                bool message = (e.Level & BepInEx.Logging.LogLevel.Message) != 0;
                if (message && _self != null && _self._view != null && _self._view.visible)
                {
                    _pluginLog.Log(BepInEx.Logging.LogLevel.Info, e.Data);
                    _self._view.Toast(Convert.ToString(e.Data));
                }
                else
                    _pluginLog.Log(e.Level, e.Data);
            };
            return relay;
        }
        #endregion

        internal static ConfigEntry<string> ConfigViewState { get; private set; }

        internal sealed partial class View
        {
            #region Toast
            private string _toast;
            private float _toastUntil;

            /// <summary>toast(): a message on the right of the status line, for a couple of seconds.</summary>
            public void Toast(string message)
            {
                if (string.IsNullOrEmpty(message))
                    return;
                _toast = message.Replace("\n", " ");
                _toastUntil = Time.unscaledTime + 2.6f + Mathf.Min(4f, _toast.Length / 40f);
            }

            /// <summary>The right hand text: a toast while one is up, else the counts.</summary>
            private bool TickToast()
            {
                bool showing = _toast != null && Time.unscaledTime < _toastUntil;
                if (_toast != null && showing == false)
                    _toast = null;
                _statusHints.gameObject.SetActive(showing == false);
                if (showing == false)
                {
                    _statusRight.rectTransform.Css(float.NaN, 1f, 14f, 0f, 260f);
                    _statusRight.color = Pal.C(0x6B6E74);
                    return false;
                }
                _statusRight.rectTransform.Css(10f, 1f, 14f, 0f);
                _statusRight.color = Pal.accent;
                if (_statusRight.text != _toast)
                    _statusRight.text = _toast;
                return true;
            }
            #endregion

            #region Remembered between sessions
            private float _stateSavedAt;

            /// <summary>Where the window was and how it was set up, as one line in the config file.</summary>
            private string SerializeState()
            {
                var parts = new List<string>
                {
                    "x=" + F(_win.anchoredPosition.x), "y=" + F(_win.anchoredPosition.y), "w=" + F(winW), "h=" + F(winH),
                    "chan=" + F(chanW), "props=" + (props ? 1 : 0), "float=" + (_propsFloat ? 1 : 0),
                    "editor=" + editor, "compact=" + (compact ? 1 : 0), "summary=" + (showSummary ? 1 : 0),
                    "groupKeys=" + groupKeys, "list=" + listMode, "frames=" + (showFrames ? 1 : 0), "snap=" + snap, "handles=" + (showHandles ? 1 : 0),
                    "normalize=" + (normalize ? 1 : 0), "path=" + (showPath ? 1 : 0), "pathRange=" + F(pathRange)
                };
                if (_pwin != null)
                    parts.Add("px=" + F(_pwin.anchoredPosition.x) + ";py=" + F(_pwin.anchoredPosition.y));
                if (_twinPos.HasValue)
                    parts.Add("tx=" + F(_twinPos.Value.x) + ";ty=" + F(_twinPos.Value.y));
                return string.Join(";", parts.ToArray());
            }

            private static string F(float v)
            {
                return v.ToString("0.##", CultureInfo.InvariantCulture);
            }

            /// <summary>Saved at most once a second, and only with the mouse up, so a drag is written once.</summary>
            private void TickSaveState()
            {
                if (ConfigViewState == null || Time.unscaledTime - _stateSavedAt < 1f || Input.GetMouseButton(0))
                    return;
                _stateSavedAt = Time.unscaledTime;
                string state = SerializeState();
                if (state != ConfigViewState.Value)
                    ConfigViewState.Value = state;
            }

            private Vector2? _savedPwin;

            private void LoadState()
            {
                if (ConfigViewState == null || string.IsNullOrEmpty(ConfigViewState.Value))
                    return;
                var d = new Dictionary<string, string>();
                foreach (string part in ConfigViewState.Value.Split(';'))
                {
                    int eq = part.IndexOf('=');
                    if (eq > 0)
                        d[part.Substring(0, eq)] = part.Substring(eq + 1);
                }
                Func<string, float, float> num = (k, fallback) =>
                {
                    string s;
                    float v;
                    return d.TryGetValue(k, out s) && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : fallback;
                };
                Func<string, string, string> str = (k, fallback) =>
                {
                    string s;
                    return d.TryGetValue(k, out s) ? s : fallback;
                };
                winW = Mathf.Max(760f, num("w", winW));
                winH = Mathf.Max(360f, num("h", winH));
                chanW = Mathf.Clamp(num("chan", chanW), 180f, 520f);
                props = num("props", props ? 1 : 0) > 0.5f;
                editor = str("editor", editor);
                if (editor != "dope" && editor != "graph" && editor != "nla")
                    editor = "dope";
                compact = num("compact", 0) > 0.5f;
                showSummary = num("summary", 1) > 0.5f;
                groupKeys = str("groupKeys", groupKeys);
                showFrames = num("frames", 0) > 0.5f;
                listMode = str("list", listMode);
                if (listMode != "object" && listMode != "tree")
                    listMode = "object";
                snap = str("snap", snap);
                showHandles = num("handles", 1) > 0.5f;
                normalize = num("normalize", 0) > 0.5f;
                showPath = num("path", 0) > 0.5f;
                pathRange = num("pathRange", 0f);
                if (d.ContainsKey("x"))
                    _win.anchoredPosition = new Vector2(num("x", 0f), num("y", 0f));
                if (d.ContainsKey("tx"))
                    _twinPos = new Vector2(num("tx", 0f), num("ty", 0f));
                if (d.ContainsKey("px"))
                    _savedPwin = new Vector2(num("px", 0f), num("py", 0f));
                ApplyWindowSize();
                if (num("float", 0) > 0.5f)
                    FloatProps();
            }
            #endregion

            #region Studio's colour picker
            /// <summary>
            /// Opens the game's own colour picker, the one Studio uses for everything else. Reached by
            /// name, because its shape differs between the games; where it cannot be found, the hex field
            /// beside every colour is still there.
            /// </summary>
            public static bool PickColour(string title, Color current, Action<Color> picked)
            {
                try
                {
                    object studio = Studio.Studio.Instance;
                    object palette = Member(studio, "colorPalette");
                    if (palette == null)
                        return false;
                    MethodInfo setup = palette.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                              .FirstOrDefault(m => m.Name == "Setup" && m.GetParameters().Length == 4);
                    if (setup == null)
                        return false;
                    PropertyInfo visible = palette.GetType().GetProperty("visible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (visible != null)
                        visible.SetValue(palette, false, null);
                    setup.Invoke(palette, new object[] { title, current, picked, false });
                    if (visible != null)
                        visible.SetValue(palette, true, null);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogDebug("Studio's colour picker could not be opened: " + e.Message);
                    return false;
                }
            }

            private static object Member(object target, string name)
            {
                if (target == null)
                    return null;
                const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo field = target.GetType().GetField(name, all);
                if (field != null)
                    return field.GetValue(target);
                PropertyInfo property = target.GetType().GetProperty(name, all);
                return property != null ? property.GetValue(target, null) : null;
            }
            #endregion

            #region Values of a track that switches
            /// <summary>
            /// The values a switching track can take, for the chips under VALUE: off and on, the names of
            /// an enum, or the numbers its keys already use. Anything else has no list to offer.
            /// </summary>
            private static List<KeyValuePair<string, object>> StepOptions(Interpolable tr, object current)
            {
                var options = new List<KeyValuePair<string, object>>();
                if (current is bool)
                {
                    options.Add(new KeyValuePair<string, object>("Off", false));
                    options.Add(new KeyValuePair<string, object>("On", true));
                }
                else if (current is Enum)
                {
                    foreach (object v in Enum.GetValues(current.GetType()))
                        options.Add(new KeyValuePair<string, object>(v.ToString(), v));
                }
                else if (current is int)
                {
                    foreach (int v in tr.keyframes.Values.Select(k => k.value).OfType<int>().Distinct().OrderBy(v => v))
                        options.Add(new KeyValuePair<string, object>(v.ToString(), v));
                }
                return options;
            }

            /// <summary>setOpt(): gives every selected key of a switching track this value.</summary>
            private void SetStepValue(object value)
            {
                T.RecordUndo("Set value");
                foreach (KeyValuePair<float, Keyframe> pair in T._selectedKeyframes)
                {
                    Keyframe k = pair.Value;
                    if (k.value != null && value != null && k.value.GetType() == value.GetType() && T._graphLockedTracks.Contains(k.parent) == false)
                        k.value = value;
                }
                T.RefreshInterpolation();
                Touch();
            }

            /// <summary>.opts: chips that wrap to as many lines as they need, the picked one lit.</summary>
            private void OptChips(RectTransform parent, Interpolable tr, Keyframe k)
            {
                List<KeyValuePair<string, object>> options = StepOptions(tr, k.value);
                if (options.Count == 0)
                {
                    RectTransform plain = Line(parent);
                    Btn(plain, Convert.ToString(k.value), true, null, null);
                    return;
                }
                const float width = 280f;
                RectTransform line = null;
                float used = width;
                for (int i = 0; i < options.Count; ++i)
                {
                    KeyValuePair<string, object> option = options[i];
                    bool on = Equals(option.Value, k.value);
                    bool swatch = k.value is Enum || k.value is int;
                    float chip = Kit.TextWidth(option.Key, 11) + 16f + (swatch ? 13f : 0f);
                    if (line == null || used + chip + 3f > width)
                    {
                        line = Kit.Node("Opts", parent);
                        Kit.Row(line.gameObject, 3f);
                        Kit.Size(line.gameObject, -1f, 22f);
                        used = 0f;
                    }
                    used += chip + 3f;
                    Clickable c = Pill(line, "Opt", 22f, 8f, 5f, on ? Pal.C(0x4B4D52) : Pal.C(0x15171B), on ? Pal.C(0x4B4D52) : Pal.C(0x15171B), 3f);
                    if (on)
                        Kit.Ring("On", c.transform, Pal.accent, 3f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    if (swatch)
                    {
                        Image dot = Kit.Box("I", c.transform, _optHues[i % _optHues.Length], 2f);
                        Kit.Size(dot.gameObject, 8f, 8f);
                    }
                    Text label = Kit.Text("Text", c.transform, Kit.Escape(option.Key), 11, on ? Pal.C(0xE4E7EC) : Pal.C(0x9A9DA2));
                    c.Tint(label.color, Pal.C(0xE4E7EC), label);
                    object value = option.Value;
                    c.onClick = () => SetStepValue(value);
                }
            }
            #endregion

            #region Moving tracks between groups
            /// <summary>Every group, depth first, with how deep it sits, for a picker.</summary>
            private List<KeyValuePair<GroupNode<InterpolableGroup>, int>> AllGroups()
            {
                var list = new List<KeyValuePair<GroupNode<InterpolableGroup>, int>>();
                Action<List<INode>, int> walk = null;
                walk = (nodes, depth) =>
                {
                    foreach (INode node in nodes)
                    {
                        GroupNode<InterpolableGroup> g = node as GroupNode<InterpolableGroup>;
                        if (g == null)
                            continue;
                        list.Add(new KeyValuePair<GroupNode<InterpolableGroup>, int>(g, depth));
                        walk(g.children, depth + 1);
                    }
                };
                walk(T._interpolablesTree.tree, 0);
                return list;
            }

            /// <summary>The group picker of the Track tab: no group, any group of this object, or a new one.</summary>
            private List<MenuItem> GroupItems(List<Interpolable> tracks)
            {
                var items = new List<MenuItem>
                {
                    new MenuItem { label = "No group", act = () => MoveToGroup(tracks, null) }
                };
                Studio.ObjectCtrlInfo oci = tracks.Count == 0 ? null : tracks[0].oci;
                foreach (KeyValuePair<GroupNode<InterpolableGroup>, int> pair in AllGroups())
                {
                    GroupNode<InterpolableGroup> g = pair.Key;
                    // Listed by object, a group shows under each object it holds, so only the ones already
                    // holding this object's tracks are offered. As grouped, any group can take any track.
                    bool fits = Grouped || LeavesInOrder(g.children).All(t => t.oci == oci);
                    if (fits == false)
                        continue;
                    bool current = tracks.Count != 0 && tracks.All(t => { LeafNode<Interpolable> leaf = T._interpolablesTree.GetLeafNode(t); return leaf != null && leaf.parent == g; });
                    items.Add(new MenuItem { label = new string(' ', pair.Value * 3) + g.obj.name, check = current, act = () => MoveToGroup(tracks, g) });
                }
                items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem { label = "New group", act = () => { T._interpolablesTree.GroupTogether(tracks, new InterpolableGroup { name = "New group" }); T.UpdateInterpolablesView(); NoteMixedGroup(); } });
                return items;
            }

            private void MoveToGroup(List<Interpolable> tracks, GroupNode<InterpolableGroup> group)
            {
                T._interpolablesTree.ParentTo(tracks.Select(t => (INode)T._interpolablesTree.GetLeafNode(t)).Where(n => n != null), group);
                T.UpdateInterpolablesView();
                _rowsDirty = true;
                Touch();
            }
            #endregion
        }
    }
}
