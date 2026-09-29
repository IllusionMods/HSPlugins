using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Timeline.Graph;
using Timeline.View;
using UILib;
using UILib.EventHandlers;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            #region Add panel
            private RectTransform _addp;
            private RectTransform _apList;
            private Text _apDesc;
            private Text _apTo;
            private InputField _apSearch;
            private bool _addKey;
            private string _addQuery = "";
            private ObjectCtrlInfoRef _apFor;
            private Studio.GuideObject _apNode;

            /// <summary>Which object the list was built for, so it is rebuilt when the selection changes.</summary>
            private sealed class ObjectCtrlInfoRef
            {
                public Studio.ObjectCtrlInfo oci;
            }

            /// <summary>openAdd(): the searchable list of what can be animated on the selected object.</summary>
            private void ToggleAdd()
            {
                if (_addp != null && _addp.gameObject.activeSelf)
                {
                    _addp.gameObject.SetActive(false);
                    return;
                }
                if (_addp == null)
                    BuildAdd();
                _addp.gameObject.SetActive(true);
                _addp.SetAsLastSibling();
                _addQuery = "";
                _apSearch.text = "";
                _apDesc.text = "Point at an item to see what it moves. Pick as many as you need, then press Done.";
                RenderAddList();
                _apSearch.ActivateInputField();
            }

            /// <summary>.addp: 330 by 450, under the + Add button, over the list and the grid.</summary>
            private void BuildAdd()
            {
                Image box = Kit.Box("Addp", _body, Pal.C(0x1E2025), 6f);
                box.raycastTarget = true;
                _addp = box.rectTransform.Css(4f, 2f, float.NaN, float.NaN, 330f, Mathf.Min(450f, GridHeight() - 8f));
                box.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                // .ap-head
                RectTransform head = Kit.Node("Head", _addp).Css(0f, 0f, 0f, float.NaN, float.NaN, 28f);
                Kit.Row(head.gameObject, 8f, 10f, 6f);
                Kit.Box("Border", head, Pal.C(0x111215)).rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Kit.Text("B", head, "Add track", 12, Pal.C(0xE4E7EC), TextAnchor.MiddleLeft, true);
                _apTo = Kit.Text("To", head, "", 11, Pal.C(0x9A9DA2));
                Kit.Flex(_apTo.gameObject, 22f);
                Clickable x = Pill(head, "X", 18f, 0f, 0f, new Color(0f, 0f, 0f, 0f), Pal.C(0x373A3F), 3f);
                Kit.Size(x.gameObject, 18f, 18f);
                x.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                IconView xi = Kit.Icon("Icon", x.transform, "close", Pal.C(0x9A9DA2), 0.9f);
                x.Tint(Pal.C(0x9A9DA2), Pal.C(0xE4E7EC), xi);
                x.tooltip = "Close";
                x.onClick = () => _addp.gameObject.SetActive(false);

                // .search
                RectTransform search = Kit.Node("Search", _addp).Css(0f, 28f, 0f, float.NaN, float.NaN, 26f);
                Kit.Row(search.gameObject, 6f, 6f, 6f);
                Kit.Box("Border", search, Pal.C(0x111215)).rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, 1f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Image field = Kit.Box("F", search, Pal.C(0x15171B), 3f);
                Kit.Row(field.gameObject, 6f, 6f, 6f);
                Kit.Flex(field.gameObject, 20f);
                Kit.Icon("Icon", field.transform, "search", Pal.C(0x6B6E74));
                _apSearch = Field(field.transform, "Q", "", 11, Pal.C(0xE4E7EC), -1f);
                Text ph = Kit.Text("Placeholder", _apSearch.transform, "Search: hand, eyes, camera…", 11, Pal.C(0x6B6E74));
                ph.rectTransform.Fill();
                _apSearch.placeholder = ph;
                _apSearch.onValueChanged.AddListener(q =>
                {
                    _addQuery = q;
                    RenderAddList();
                });
                _apSearch.onEndEdit.AddListener(q =>
                {
                    // Enter adds the first match, as in the playground.
                    if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
                    {
                        InterpolableModel first = AddCandidates().FirstOrDefault(m => Added(m) == false);
                        if (first != null)
                            AddModel(first);
                    }
                });

                // .ap-list
                ScrollRect scroll = Kit.Node("List", _addp).Css(0f, 54f, 0f, 80f).gameObject.AddComponent<ScrollRect>();
                RectTransform viewport = (RectTransform)scroll.transform;
                viewport.gameObject.AddComponent<RectMask2D>();
                viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
                _apList = Kit.Node("Content", viewport);
                _apList.anchorMin = new Vector2(0f, 1f);
                _apList.anchorMax = new Vector2(1f, 1f);
                _apList.pivot = new Vector2(0.5f, 1f);
                _apList.sizeDelta = Vector2.zero;
                _apList.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Kit.Col(_apList.gameObject, 0f, new RectOffset(0, 0, 2, 6));
                scroll.viewport = viewport;
                scroll.content = _apList;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 30f;
                scroll.inertia = false;

                // .ap-desc
                Image desc = Kit.Box("Desc", _addp, Pal.C(0x1A1C20));
                desc.rectTransform.Css(0f, float.NaN, 0f, 32f, float.NaN, 48f);
                Kit.Box("Border", desc.transform, Pal.C(0x111215)).rectTransform.Css(0f, 0f, 0f, float.NaN, float.NaN, 1f);
                _apDesc = Kit.Paragraph("Text", desc.transform, "", 11, Pal.C(0x9A9DA2));
                _apDesc.rectTransform.Fill(10f, 6f, 10f, 4f);

                // .ap-foot
                RectTransform foot = Kit.Node("Foot", _addp).Css(0f, float.NaN, 0f, 0f, float.NaN, 32f);
                Kit.Row(foot.gameObject, 6f, 10f, 6f);
                Kit.Box("Border", foot, Pal.C(0x111215)).rectTransform.Css(0f, 0f, 0f, float.NaN, float.NaN, 1f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                RenderAddFoot(foot);
                Kit.Ring("Border", _addp, Pal.C(0x0D0E10), 6f);
            }

            private void RenderAddFoot(RectTransform foot)
            {
                for (int i = foot.childCount - 1; i >= 0; --i)
                {
                    if (foot.GetChild(i).name != "Border")
                        UnityEngine.Object.Destroy(foot.GetChild(i).gameObject);
                }
                Cb(foot, "Key the current value at the playhead", _addKey, () =>
                {
                    _addKey = !_addKey;
                    RenderAddFoot(foot);
                });
                Clickable done = Btn(foot, "Done", false, () => _addp.gameObject.SetActive(false), null);
                done.normal = Pal.accent;
                done.hover = Pal.C(0xF0B558);
                done.GetComponentInChildren<Text>().color = Pal.onAccent;
                done.Refresh();
            }

            private IEnumerable<InterpolableModel> AddCandidates()
            {
                string q = _addQuery.Trim().ToLowerInvariant();
                foreach (KeyValuePair<string, List<InterpolableModel>> owner in T._interpolableModelsDictionary.OrderBy(p => T._hardCodedOwnerOrder.TryGetValue(p.Key, out int order) ? order : int.MaxValue))
                {
                    foreach (InterpolableModel model in owner.Value)
                    {
                        if (model.IsCompatibleWithTarget(T._selectedOCI) == false)
                            continue;
                        if (q.Length != 0 && (model.name + " " + owner.Key).ToLowerInvariant().Contains(q) == false)
                            continue;
                        yield return model;
                    }
                }
            }

            private bool Added(InterpolableModel model)
            {
                return ExistingFor(model) != null;
            }

            /// <summary>
            /// The track this model would add right now, if there is one already. A model that follows
            /// the selected node (an IK or FK node, a bone) makes a different track for each node, so
            /// the track is looked up as Timeline keys them - model, parameter and object - and not by
            /// the model alone, which took one IK node's track for every other one's.
            /// </summary>
            private Interpolable ExistingFor(InterpolableModel model)
            {
                Interpolable probe;
                try
                {
                    probe = new Interpolable(T._selectedOCI, model);
                }
                catch (Exception)
                {
                    return null;
                }
                Interpolable existing;
                return T._interpolables.TryGetValue(probe.GetHashCode(), out existing) ? existing : null;
            }

            /// <summary>addListHtml(): by plugin, each line the track, the plugin, and whether it is already added.</summary>
            private void RenderAddList()
            {
                if (_apList == null)
                    return;
                for (int i = _apList.childCount - 1; i >= 0; --i)
                    UnityEngine.Object.Destroy(_apList.GetChild(i).gameObject);
                _apList.DetachChildren();
                _apTo.text = "for " + (T._selectedOCI == null ? "the scene" : ObjectName(T._selectedOCI));
                _apFor = new ObjectCtrlInfoRef { oci = T._selectedOCI };
                _apNode = Studio.GuideObjectManager.Instance.selectObject;

                if (T._selectedOCI == null)
                {
                    Text note = Kit.Paragraph("Note", _apList, "Nothing is selected in the scene, so only scene-wide tracks are listed. Select a character or an item to see theirs.", 11, Pal.C(0x9A9DA2));
                    note.rectTransform.sizeDelta = new Vector2(0f, 40f);
                    WrapPad(note, 10f, 8f);
                }
                string owner = null;
                int count = 0;
                foreach (InterpolableModel model in AddCandidates())
                {
                    if (model.owner != owner)
                    {
                        owner = model.owner;
                        RectTransform cat = Kit.Node("Cat", _apList);
                        Kit.Size(cat.gameObject, -1f, 21f);
                        Kit.Text("T", cat, owner.ToUpperInvariant(), 10, Pal.C(0x6B6E74), TextAnchor.LowerLeft).rectTransform.Fill(10f, 8f, 10f, 3f);
                    }
                    AddItem(model);
                    ++count;
                }
                if (count == 0)
                {
                    Text none = Kit.Paragraph("Note", _apList, _addQuery.Trim().Length == 0 ? "Nothing can be animated on this." : "Nothing matches “" + _addQuery + "”.", 11, Pal.C(0x9A9DA2));
                    WrapPad(none, 10f, 8f);
                }
            }

            private static void WrapPad(Text text, float x, float y)
            {
                LayoutElement le = text.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 36f;
                text.rectTransform.offsetMin = new Vector2(x, 0f);
            }

            /// <summary>.ap-it: 22 high, the accent under the pointer, and on the right ＋ or ✓ Added.</summary>
            private void AddItem(InterpolableModel model)
            {
                bool added = Added(model);
                Image row = Kit.Box("Item", _apList, new Color(0f, 0f, 0f, 0f));
                row.raycastTarget = true;
                Kit.Row(row.gameObject, 7f, 10f, 10f);
                Kit.Size(row.gameObject, -1f, 22f);
                Clickable c = row.gameObject.AddComponent<Clickable>();
                c.background = row;
                c.normal = new Color(0f, 0f, 0f, 0f);
                c.hover = Pal.accent;
                c.pressed = c.hover;
                // A node track names the node it would be added for, the one selected in the scene.
                string label = model.name;
                try
                {
                    label = NodeTrackName(new Interpolable(T._selectedOCI, model)) ?? model.name;
                }
                catch (Exception)
                {
                }
                Text name = Kit.Text("Nm", row.transform, Kit.Escape(label), 12, Pal.C(0xE4E7EC));
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                Kit.Flex(name.gameObject, 22f);
                Text st = Kit.Text("St", row.transform, added ? "✓ Added" : "＋", 11, added ? Pal.Hex(0x82CC63) : Pal.accent, TextAnchor.MiddleRight);
                Kit.Size(st.gameObject, 50f, 22f);
                c.Tint(Pal.C(0xE4E7EC), Pal.onAccent, name);
                PointerEnterHandler enter = row.gameObject.AddComponent<PointerEnterHandler>();
                enter.onPointerEnter = e =>
                {
                    st.color = Pal.onAccent;
                    _apDesc.text = "<color=" + Pal.Html(Pal.C(0xE4E7EC)) + "><b>" + Kit.Escape(model.name) + "</b></color>  from " + model.owner +
                                   (added ? ". Already added: click to select it." : ". Adds a track for it" + (T._selectedOCI == null ? "." : " on " + ObjectName(T._selectedOCI) + "."));
                };
                enter.onPointerExit = e => st.color = added ? Pal.Hex(0x82CC63) : Pal.accent;
                c.onClick = () => AddModel(model);
            }

            private void AddModel(InterpolableModel model)
            {
                Interpolable existing = ExistingFor(model);
                if (existing != null)
                {
                    T.SelectInterpolable(existing);
                    _addp.gameObject.SetActive(false);
                    Touch();
                    return;
                }
                Interpolable track = T.AddInterpolable(model);
                if (track != null && _addKey)
                    T.AddKeyframe(track, T._playbackTime);
                if (track != null)
                    T.SelectAddInterpolable(track);
                RenderAddList();
                Touch();
            }

            private void TickAdd()
            {
                // A node track is added for the selected node, so picking another node changes the list too.
                if (_addp != null && _addp.gameObject.activeSelf && (_apFor == null || _apFor.oci != T._selectedOCI || _apNode != Studio.GuideObjectManager.Instance.selectObject))
                    RenderAddList();
            }
            #endregion

            #region Guide
            private RectTransform _guide;
            private int _guideState = -1;

            /// <summary>renderGuide(): three steps over the empty grid, while nothing is animated yet.</summary>
            private void TickGuide()
            {
                bool none = T._interpolables.Count == 0;
                if (_guide != null)
                    _guide.anchoredPosition = new Vector2(chanW + (GridWidth() - 440f) / 2f, -(70f - HDR));
                int state = none ? (T._selectedOCI == null ? 1 : 2 + (T._selectedOCI.GetHashCode() & 0xFFFF)) : 0;
                if (state == _guideState)
                    return;
                _guideState = state;
                if (_guide != null)
                    UnityEngine.Object.Destroy(_guide.gameObject);
                _guide = null;
                if (none == false)
                    return;
                Image box = Kit.Box("Guide", _body, Pal.C(0x25282D), 6f);
                box.raycastTarget = true;
                _guide = box.rectTransform;
                Kit.Col(box.gameObject, 8f, new RectOffset(16, 16, 14, 14));
                ContentSizeFitter fit = box.gameObject.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                _guide.anchorMin = _guide.anchorMax = new Vector2(0f, 1f);
                _guide.pivot = new Vector2(0f, 1f);
                _guide.sizeDelta = new Vector2(440f, 0f);
                _guide.anchoredPosition = new Vector2(chanW + (GridWidth() - 440f) / 2f, -(70f - HDR));
                Kit.Ring("Border", _guide, Pal.C(0x111215), 6f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                Kit.Text("B", _guide, "Nothing is animated yet", 13, Pal.C(0xE4E7EC), TextAnchor.MiddleLeft, true);
                bool selected = T._selectedOCI != null;
                string done = Pal.Html(Pal.Hex(0x82CC63)), em = Pal.Html(Pal.accent);
                string k = "<color=" + Pal.Html(Pal.C(0xE4E7EC)) + ">";
                string[] steps =
                {
                    "Select what to animate in the scene: a character, an item or one of their nodes." +
                    (selected ? " <color=" + em + ">Selected: " + Kit.Escape(ObjectName(T._selectedOCI)) + "</color>" : ""),
                    "Press " + k + "＋ Add</color> above the channel list and pick what should move. Or press " + k + "◆ Key</color> to key what is selected straight away.",
                    "Move the playhead, pose again and press " + k + "◆ Key</color>. Two keys make a motion."
                };
                for (int i = 0; i < steps.Length; ++i)
                {
                    string text = (i + 1) + ".  " + steps[i];
                    if (i == 0 && selected)
                        text = "<color=" + done + ">" + text + "</color>";
                    Text li = Kit.Paragraph("Li", _guide, text, 12, Pal.C(0xC9CDD3));
                    li.lineSpacing = 1.25f;
                }
                RectTransform line = Line(_guide);
                Clickable add = Btn(line, "＋ Add track", false, ToggleAdd, null);
                add.normal = Pal.accent;
                add.hover = Pal.C(0xF0B558);
                add.GetComponentInChildren<Text>().color = Pal.onAccent;
                add.Refresh();
            }
            #endregion

            #region Timeline files
            /// <summary>openFiles(): save this object's timeline under a name, and open or delete the saved ones.</summary>
            private bool _filesOnlySelected;

            private void OpenFiles()
            {
                RectTransform dialog = OpenDialog("Timeline files", winW / 2f - 170f, 40f);
                Cap(dialog, "SAVE THIS TIMELINE AS", null);
                RectTransform line = Line(dialog);
                string[] files = Directory.Exists(_singleFilesFolder) ? Directory.GetFiles(_singleFilesFolder, "*.xml") : new string[0];
                InputField name = Fld(line, null, 0, "Take " + (files.Length + 1), null, null);
                Clickable save = Btn(line, "Save", false, () =>
                {
                    T.SaveSingleFile(name.text.Trim(), _filesOnlySelected);
                    OpenFiles();
                }, null);
                save.normal = Pal.accent;
                save.hover = Pal.C(0xF0B558);
                save.GetComponentInChildren<Text>().color = Pal.onAccent;
                save.Refresh();
                line = Line(dialog);
                Cb(line, "Only the selected tracks, from any object", _filesOnlySelected, () => { _filesOnlySelected = !_filesOnlySelected; OpenFiles(); });

                Cap(dialog, "SAVED", null);
                if (files.Length == 0)
                    Note(dialog, "Nothing saved yet.");
                foreach (string path in files.OrderByDescending(File.GetLastWriteTime).Take(12))
                {
                    string file = Path.GetFileNameWithoutExtension(path);
                    RectTransform row = Kit.Node("Frow", dialog);
                    Kit.Row(row.gameObject, 6f);
                    Kit.Size(row.gameObject, -1f, 24f);
                    Text fn = Kit.Text("Fn", row, Kit.Escape(file), 12, Pal.C(0xE4E7EC));
                    Kit.Flex(fn.gameObject, 24f);
                    Kit.Text("Fd", row, File.GetLastWriteTime(path).ToString("d MMM HH:mm"), 10, Pal.C(0x6B6E74));
                    Btn(row, "Open", false, () =>
                    {
                        T.LoadSingleFile(file);
                        CloseMenu();
                        Touch();
                    }, "Load it onto the selected object");
                    string captured = path;
                    Btn(row, "✕", false, () => Confirm("Delete “" + file + "”", "The file is removed from the Timeline folder. This cannot be undone.", "Delete", () =>
                    {
                        try
                        {
                            File.Delete(captured);
                        }
                        catch (Exception e)
                        {
                            Logger.LogMessage("Could not delete the file: " + e.Message);
                        }
                        OpenFiles();
                    }), "Delete").GetComponentInChildren<Text>().color = Pal.Hex(0xF08A7E);
                }
                Note(dialog, "Saved as .xml files in the Timeline folder, for the selected object. A Studio scene save carries its whole timeline too.");
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Close", false, CloseMenu, null);
                PlaceDialog(dialog);
            }
            #endregion

            #region Bake
            /// <summary>
            /// openBake(): turn the selected tracks into a key on every frame, or every few; or bake the
            /// selected character's animation, a route, or dynamic bones into keys.
            /// </summary>
            private void OpenBake()
            {
                if (T._bakingLive)
                {
                    Toast("Already baking. Pause playback to stop it.");
                    return;
                }
                List<Interpolable> tracks = T._selectedInterpolables.Where(CanBake).ToList();
                // What makes sense for what is selected, when no track is.
                BakeSource source = tracks.Count != 0 ? BakeSource.Keys
                                  : T._selectedOCI is Studio.OCIRoute ? BakeSource.Route
                                  : T._selectedOCI is Studio.OCIChar ? BakeSource.Animation : BakeSource.Keys;
                int range = 0, stepIndex = 0;
                AnimationParts parts = AnimationParts.Body | AnimationParts.Head | AnimationParts.Fingers;
                bool recPos = true, recRot = true, recScale = false;
                int warmUp = 0;
                int loops = 0;
                string seconds = "";
                int[] steps = { 1, 2, 5, 15 };
                RectTransform dialog = OpenDialog("Bake", winW / 2f - 150f, 60f);
                Action render = null;
                RectTransform body = Kit.Node("Body", dialog);
                Kit.Col(body.gameObject, 6f);
                render = () =>
                {
                    for (int i = body.childCount - 1; i >= 0; --i)
                        UnityEngine.Object.Destroy(body.GetChild(i).gameObject);
                    body.DetachChildren();
                    Cap(body, "FROM", null);
                    Segw(body, new[] { "Keys", "Animation", "Route", "Dyn. bones", "Nodes" }, (int)source, v => { source = (BakeSource)v; render(); });
                    switch (source)
                    {
                        case BakeSource.Keys:
                            Cap(body, "RANGE", tracks.Count + (tracks.Count == 1 ? " track" : " tracks"));
                            Segw(body, new[] { "First to last key", "Whole scene" }, range, v => { range = v; render(); });
                            break;
                        case BakeSource.Animation:
                            Cap(body, "PARTS", null);
                            RectTransform line = Line(body);
                            foreach (AnimationParts part in new[] { AnimationParts.Body, AnimationParts.Head, AnimationParts.Fingers })
                            {
                                AnimationParts captured = part;
                                string label = part == AnimationParts.Body ? "Body (IK)" : part == AnimationParts.Head ? "Head, neck" : "Fingers";
                                Cb(line, label, (parts & part) != 0, () => { parts ^= captured; render(); });
                            }
                            Cap(body, "LENGTH", null);
                            Segw(body, new[] { "1 loop", "2 loops", "3 loops", "4 loops" }, loops, v => { loops = v; render(); });
                            line = Line(body);
                            Fld(line, "Or seconds", 0x9A9DA2, seconds, "s", s => seconds = s);
                            break;
                        case BakeSource.Nodes:
                            Cap(body, "RECORD", null);
                            RectTransform rec = Line(body);
                            Cb(rec, "Position", recPos, () => { recPos = !recPos; render(); });
                            Cb(rec, "Rotation", recRot, () => { recRot = !recRot; render(); });
                            Cb(rec, "Scale", recScale, () => { recScale = !recScale; render(); });
                            break;
                    }
                    if (source == BakeSource.DynamicBones || source == BakeSource.Nodes)
                    {
                        Cap(body, "WARM UP FIRST", null);
                        Segw(body, new[] { "No", "1 loop", "2 loops", "3 loops" }, warmUp, v => { warmUp = v; render(); });
                    }
                    Cap(body, "A KEY EVERY", null);
                    Segw(body, new[] { "Frame", "2 frames", "5 frames", "15 frames" }, stepIndex, v => { stepIndex = v; render(); });
                    Note(body, BakeNote(source));
                };
                render();
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Cancel", false, CloseMenu, null);
                Clickable ok = Btn(buttons, "Bake", false, () =>
                {
                    CloseMenu();
                    switch (source)
                    {
                        case BakeSource.Keys:
                            if (tracks.Count == 0)
                                Toast("Select the tracks to bake first.");
                            else
                                Bake(tracks, range == 1, steps[stepIndex]);
                            break;
                        case BakeSource.Animation:
                            float secs;
                            float.TryParse(seconds, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out secs);
                            T.BakeAnimation(parts, steps[stepIndex], loops + 1, secs);
                            break;
                        case BakeSource.Route:
                            T.BakeRoute(steps[stepIndex]);
                            break;
                        case BakeSource.DynamicBones:
                            T.BakeDynamicBones(steps[stepIndex], warmUp);
                            break;
                        case BakeSource.Nodes:
                            T.BakeNodes(recPos, recRot, recScale, steps[stepIndex], warmUp);
                            break;
                    }
                    Touch();
                }, null);
                ok.normal = Pal.accent;
                ok.hover = Pal.C(0xF0B558);
                ok.GetComponentInChildren<Text>().color = Pal.onAccent;
                ok.Refresh();
                PlaceDialog(dialog);
            }

            private static string BakeNote(BakeSource source)
            {
                switch (source)
                {
                    case BakeSource.Animation:
                        return "Plays the Studio animation of the character selected in the scene from the playhead, as many loops as picked, and keys its IK nodes and the FK bones picked above. With seconds it instead moves the playhead that long and keys whatever pose the scene's time gives, for animations that follow it (MMDD). Turn IK and FK on to see the baked keys play.";
                    case BakeSource.Route:
                        return "Plays the route selected in the Workspace along with the scene and keys where the objects on it go. Its Loop has to be off. Pause to stop early.";
                    case BakeSource.Nodes:
                        return "Plays the scene from the playhead to its end and keys where the nodes selected in the scene really are, whatever moves them: a constraint, physics, another plugin. Control + click selects several nodes. Warming up plays the scene first so physics has settled.";
                    case BakeSource.DynamicBones:
                        return "Plays the scene from the playhead to its end and keys the FK bones the dynamic bones (hair, skirt, chest) of the selected character or item swing, so the motion comes out the same every time. Pause to stop early.";
                    default:
                        return "Replaces the keys with plain ones that follow the motion exactly. Undo brings the old keys back.";
                }
            }

            private void Bake(List<Interpolable> tracks, bool wholeScene, int step)
            {
                int fps = Mathf.Clamp(T._desiredFrameRate, 1, 240);
                int rate = Mathf.Max(1, Mathf.RoundToInt(fps / (float)step));
                int planned = 0;
                foreach (Interpolable tr in tracks)
                {
                    Vector2 r = wholeScene ? new Vector2(0f, T._duration) : T.BakeRange(tr, new Vector2(float.NaN, float.NaN));
                    planned += Mathf.RoundToInt((r.y - r.x) * rate) + 1;
                }
                if (planned > _maxBakedKeyframes)
                {
                    Logger.LogMessage("That would write " + planned + " keyframes, over the " + _maxBakedKeyframes + " limit. Bake fewer tracks, or a key every few frames.");
                    return;
                }
                T.RecordUndo("Bake to frames");
                foreach (Interpolable tr in tracks)
                {
                    Vector2 r = wholeScene ? new Vector2(0f, T._duration) : T.BakeRange(tr, new Vector2(float.NaN, float.NaN));
                    if (r.y - r.x > 0.0001f)
                        T.BakeTrack(tr, r.x, r.y, rate);
                }
                T.SelectKeyframes();
                T.RefreshInterpolation();
                Logger.LogMessage("Baked " + tracks.Count + " track(s), a key every " + (step == 1 ? "frame" : step + " frames") + ".");
                Touch();
            }
            #endregion

            #region Shortcuts
            private static readonly string[][] _shortcuts =
            {
                new[] { "Space", "Play / pause" },
                new[] { "← →  ·  ↑ ↓", "Back / forward one frame · jump to the next / previous key" },
                new[] { "Middle-click", "Add a key where the mouse is (Graph: on every drawn channel)" },
                new[] { "Ctrl + middle", "Delete the key under the mouse" },
                new[] { "Click · Ctrl+click · drag empty space", "Select · add to the selection · box select" },
                new[] { "Shift while dragging", "Flip snapping for this drag" },
                new[] { "Ctrl / Alt while dragging (Graph)", "Time only / value only" },
                new[] { "Scroll · Ctrl+scroll · Shift+scroll", "Scroll the rows · zoom time (Graph: scroll zooms) · scroll sideways" },
                new[] { "Alt+drag, middle-drag", "Pan the Graph view" },
                new[] { "Home · F", "Fit everything · fit the selection" },
                new[] { "A · Alt+A · Ctrl+I", "Select all · none · invert" },
                new[] { "I · X / Delete", "Key at the playhead · delete the selection" },
                new[] { "Ctrl+C · Ctrl+V", "Copy keys · paste at the playhead" },
                new[] { "Ctrl+Z · Ctrl+Shift+Z", "Undo · Redo" },
                new[] { "M", "Add a marker at the playhead" },
                new[] { "Ctrl+drag on the ruler", "Mark a range to trim (Ctrl+click clears it); right-click the ruler to trim" },
                new[] { "H · Alt+H", "Hide the selected channels from the Graph · show all (NLA: mute the strip)" },
                new[] { "Shift+D · Y", "NLA: duplicate a strip · split it at the playhead" },
                new[] { "Right-click", "A menu everywhere: keys, ruler, channels, strips" },
            };

            /// <summary>The shortcuts table, as the playground's page lists it. They work only over the window.</summary>
            private void OpenKeys()
            {
                RectTransform dialog = OpenDialog("Shortcuts", winW / 2f - 230f, 30f);
                Note(dialog, "Only while the mouse is over the Timeline window. Outside it, the keys belong to Studio and the other plugins.");
                foreach (string[] row in _shortcuts)
                {
                    RectTransform line = Kit.Node("Row", dialog);
                    Kit.Row(line.gameObject, 10f);
                    Kit.Size(line.gameObject, -1f, 20f);
                    Text key = Kit.Text("K", line, row[0], 11, Pal.accent);
                    Kit.Size(key.gameObject, 190f, 20f);
                    Text what = Kit.Text("W", line, row[1], 11, Pal.C(0xD5D8DD));
                    what.gameObject.AddComponent<LayoutElement>().preferredWidth = 260f;
                    what.horizontalOverflow = HorizontalWrapMode.Wrap;
                }
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Close", false, CloseMenu, null);
                PlaceDialog(dialog);
            }
            #endregion
        }
    }
}
