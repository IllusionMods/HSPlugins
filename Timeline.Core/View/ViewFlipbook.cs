using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Timeline.View;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            /// <summary>openFlipbook(): pick meshes of the selected object, put them in order, and time them.</summary>
            private void OpenFlipbook()
            {
                if (T.FlipbookAvailable() == false)
                {
                    Toast("Flipbook makes RendererEditor's on/off tracks, and RendererEditor is not installed.");
                    return;
                }
                if (FlipbookRoot(T._selectedOCI) == null)
                {
                    Toast("Select a character or an item in the scene first.");
                    return;
                }
                List<string> all = FlipbookMeshes(T._selectedOCI);
                var chosen = new List<string>();
                string search = "";
                string start = Fmt(FrameSnap(T._playbackTime));
                string frames = "4";
                bool hideBefore = true, hideAfter = false;

                RectTransform dialog = OpenDialog("Flipbook from meshes", winW / 2f - 170f, 30f);
                RectTransform body = Kit.Node("Body", dialog);
                Kit.Col(body.gameObject, 6f);
                LayoutElement width = body.gameObject.AddComponent<LayoutElement>();
                width.preferredWidth = 320f;
                Action render = null;
                render = () =>
                {
                    for (int i = body.childCount - 1; i >= 0; --i)
                        UnityEngine.Object.Destroy(body.GetChild(i).gameObject);
                    body.DetachChildren();

                    Cap(body, "MESHES OF " + ObjectName(T._selectedOCI).ToUpperInvariant(), all.Count + " in all");
                    RectTransform line = Line(body);
                    Fld(line, null, 0, search, null, text => { search = text.Trim(); render(); });
                    List<string> found = all.Where(p => chosen.Contains(p) == false && (search.Length == 0 || p.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                    foreach (string path in found.Take(8))
                    {
                        string captured = path;
                        RectTransform row = Kit.Node("Fbrow", body);
                        Kit.Row(row.gameObject, 6f);
                        Kit.Size(row.gameObject, -1f, 20f);
                        Text name = Kit.Text("Name", row, Kit.Escape(Leaf(path)), 11, Pal.C(0xD5D8DD));
                        Kit.Flex(name.gameObject, 20f);
                        SmallBtn(row, "Add", path, () => { chosen.Add(captured); render(); });
                    }
                    if (found.Count > 8)
                        Note(body, (found.Count - 8) + " more. Type part of a name above to find one.");

                    Cap(body, "IN THE ORDER THEY SHOW", chosen.Count == 0 ? "none yet" : chosen.Count.ToString());
                    for (int i = 0; i < chosen.Count; ++i)
                    {
                        int index = i;
                        RectTransform row = Kit.Node("Fbrow", body);
                        Kit.Row(row.gameObject, 4f);
                        Kit.Size(row.gameObject, -1f, 20f);
                        Text name = Kit.Text("Name", row, (i + 1) + ". " + Kit.Escape(Leaf(chosen[i])), 11, Pal.C(0xE4E7EC));
                        Kit.Flex(name.gameObject, 20f);
                        SmallBtn(row, "↑", "Earlier", () => { Swap(chosen, index, index - 1); render(); });
                        SmallBtn(row, "↓", "Later", () => { Swap(chosen, index, index + 1); render(); });
                        SmallBtn(row, "✕", "Take it out", () => { chosen.RemoveAt(index); render(); });
                    }

                    Cap(body, "TIMING", null);
                    line = Line(body);
                    Fld(line, "From", 0x9A9DA2, start, "s", text => start = text);
                    Fld(line, "Each for", 0x9A9DA2, frames, "frames", text => frames = text);
                    line = Line(body);
                    Cb(line, "Hidden before the first", hideBefore, () => { hideBefore = !hideBefore; render(); });
                    line = Line(body);
                    Cb(line, "Hidden after the last", hideAfter, () => { hideAfter = !hideAfter; render(); });
                    Note(body, "Each mesh becomes one of RendererEditor's on/off tracks in a Flipbook group, shown in turn.");
                };
                render();
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Cancel", false, CloseMenu, null);
                Clickable ok = Btn(buttons, "Create", false, () =>
                {
                    if (chosen.Count == 0)
                    {
                        Toast("Add at least one mesh.");
                        return;
                    }
                    float from;
                    int each;
                    if (float.TryParse(start.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out from) == false)
                        from = 0f;
                    if (int.TryParse(frames, out each) == false)
                        each = 4;
                    CloseMenu();
                    T.MakeFlipbook(new List<string>(chosen), from, Mathf.Max(1, each), hideBefore, hideAfter);
                    Touch();
                }, null);
                ok.normal = Pal.accent;
                ok.hover = Pal.C(0xF0B558);
                ok.GetComponentInChildren<Text>().color = Pal.onAccent;
                ok.Refresh();
                PlaceDialog(dialog);
            }

            private static string Leaf(string path)
            {
                return path.Substring(path.LastIndexOf('/') + 1);
            }

            private static void Swap(List<string> list, int a, int b)
            {
                if (b < 0 || b >= list.Count)
                    return;
                string t = list[a];
                list[a] = list[b];
                list[b] = t;
            }
        }
    }
}
