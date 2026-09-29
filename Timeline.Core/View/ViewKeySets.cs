using System.Collections.Generic;
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
            private FloatWin _setsWin;

            private FloatWin SetsWin
            {
                get { return _setsWin ?? (_setsWin = MakeFloat("Swin", "sets", "Key sets", 264f, new Vector2(330f, 240f), FillSets)); }
            }

            public void ToggleKeySets()
            {
                ToggleFloat(SetsWin);
            }

            private List<Keyframe> SelectedKeys()
            {
                return T._selectedKeyframes.Select(k => k.Value).ToList();
            }

            /// <summary>newSet(): the selected keys become a set, taken out of any other.</summary>
            private void NewKeySet()
            {
                List<Keyframe> keys = SelectedKeys();
                if (keys.Count == 0)
                {
                    Toast("Select the keys for the set first.");
                    return;
                }
                KeySet set = T.NewKeySet(keys);
                ShowFloat(SetsWin);
                Toast("“" + set.name + "”: " + keys.Count + (keys.Count > 1 ? " keys" : " key"));
                RefreshFloats();
            }

            /// <summary>selectSet(): its keys become the selection, or join it with Ctrl.</summary>
            private void SelectKeySet(KeySet set, bool add)
            {
                List<KeyValuePair<float, Keyframe>> keys = T.KeysIn(set);
                if (keys.Count == 0)
                {
                    Toast("“" + set.name + "” has no keys left.");
                    return;
                }
                if (add)
                    T.SelectAddKeyframes(keys.Where(k => T._selectedKeyframes.All(s => s.Value != k.Value)).ToList());
                else
                    T.SelectKeyframes(keys);
                Touch();
            }

            private List<MenuItem> KeySetItems()
            {
                var items = new List<MenuItem>();
                foreach (KeySet set in T._keySets)
                {
                    KeySet captured = set;
                    items.Add(new MenuItem { label = set.name + "  (" + T.KeysIn(set).Count + ")", dot = set.color, act = () => SelectKeySet(captured, false) });
                }
                if (items.Count != 0)
                    items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem { label = "New set from the selected keys", act = NewKeySet, disabled = T._selectedKeyframes.Count == 0 });
                items.Add(new MenuItem { label = "Key sets window…", act = ToggleKeySets });
                return items;
            }

            /// <summary>renderSets(): one row per set, its colour, name, key count and what to do with it.</summary>
            private void FillSets(RectTransform body)
            {
                RectTransform line = Line(body);
                Clickable add = Btn(line, "New set from the selected keys", true, NewKeySet, null);
                add.normal = Pal.accent;
                add.hover = Pal.C(0xF0B558);
                add.GetComponentInChildren<Text>().color = Pal.onAccent;
                add.Refresh();

                if (T._keySets.Count == 0)
                    Note(body, "No sets yet. Select some keys and press the button above.");
                foreach (KeySet set in T._keySets)
                {
                    KeySet captured = set;
                    RectTransform row = Kit.Node("Srow", body);
                    Kit.Row(row.gameObject, 4f);
                    Kit.Size(row.gameObject, -1f, 24f);
                    Image dot = Kit.Box("Sdot", row, set.color, 3f);
                    Kit.Size(dot.gameObject, 12f, 12f);
                    dot.raycastTarget = true;
                    Clickable pick = dot.gameObject.AddComponent<Clickable>();
                    pick.background = dot;
                    pick.normal = pick.hover = pick.pressed = set.color;
                    pick.tooltip = "Change colour";
                    pick.onClick = () => PickColour("Key set colour", captured.color, c =>
                    {
                        T.RecordUndo("Key set colour");
                        captured.color = c;
                        RefreshFloats();
                    });
                    Fld(row, null, 0, set.name, null, text =>
                    {
                        string name = text.Trim();
                        if (name.Length == 0 || name == captured.name)
                            return;
                        T.RecordUndo("Rename key set");
                        captured.name = name;
                        RefreshFloats();
                    });
                    Text count = Kit.Text("Cnt", row, T.KeysIn(set).Count.ToString(), 10, Pal.C(0x6B6E74), TextAnchor.MiddleRight);
                    Kit.Size(count.gameObject, 20f, 22f);
                    SmallBtn(row, "Select", "Select its keys (Ctrl+click adds them)", () =>
                        SelectKeySet(captured, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)));
                    SmallBtn(row, "+", "Put the selected keys in this set", () =>
                    {
                        List<Keyframe> keys = SelectedKeys();
                        if (keys.Count == 0)
                        {
                            Toast("Select keys first.");
                            return;
                        }
                        T.PutInKeySet(captured, keys);
                        Toast(keys.Count + (keys.Count > 1 ? " keys" : " key") + " in “" + captured.name + "”");
                        RefreshFloats();
                    });
                    SmallBtn(row, "−", "Take the selected keys out", () =>
                    {
                        T.TakeOutOfKeySet(captured, SelectedKeys());
                        RefreshFloats();
                    });
                    SmallBtn(row, "✕", "Delete the set (its keys stay)", () =>
                    {
                        T.DeleteKeySet(captured);
                        Toast("Set “" + captured.name + "” deleted; its keys stay.");
                        RefreshFloats();
                    }).GetComponentInChildren<Text>().color = Pal.Hex(0xF08A7E);
                }
                Note(body, "A key is in one set at a time: putting it in another moves it. Keys in a set have a bar of its colour under them in the Dope Sheet. Keyframe Groups of scenes made with ShalltyUtils open here as sets.");
            }

            private static Clickable SmallBtn(RectTransform parent, string text, string tip, System.Action act)
            {
                Clickable b = Btn(parent, text, false, act, tip);
                HorizontalLayoutGroup row = b.GetComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(6, 6, 0, 0);
                return b;
            }

            /// <summary>A bar of the set's colour under a key that is in one.</summary>
            private void KeySetBar(Paint p, Keyframe k, float x, float cy, float side, float alpha)
            {
                if (k.keySet == null || T._keySets.Contains(k.keySet) == false)
                    return;
                p.globalAlpha = alpha;
                p.Rect(Mathf.Round(x) - 4f, Mathf.Round(cy + side * 0.72f) + 1f, 9f, 2f, k.keySet.color);
                p.globalAlpha = 1f;
            }
        }
    }
}
