using System.Collections.Generic;
using System.Linq;
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
            private const float _pickerAreaW = 264f, _pickerAreaH = 172f;
            private FloatWin _pickWin;
            private bool _pickEdit;
            /// <summary>Where the page is scrolled to, kept when the window is filled again.</summary>
            private Vector2 _pickScroll;

            private FloatWin PickWin
            {
                get { return _pickWin ?? (_pickWin = MakeFloat("Kwin", "picker", "Picker", 284f, new Vector2(14f, 330f), FillPicker)); }
            }

            public void TogglePicker()
            {
                ToggleFloat(PickWin);
            }

            private PickerPage CurrentPage()
            {
                if (T._pickerPages.Count == 0)
                    return null;
                T._pickerPage = Mathf.Clamp(T._pickerPage, 0, T._pickerPages.Count - 1);
                return T._pickerPages[T._pickerPage];
            }

            /// <summary>renderPicker(): the page tabs, the page, and in edit mode the way to add buttons.</summary>
            private void FillPicker(RectTransform body)
            {
                if (T._pickerPages.Count == 0)
                    T._pickerPages.Add(new PickerPage { name = "Page 1", color = _keySetColors[0] });
                PickerPage page = CurrentPage();

                // .pk-tabs
                RectTransform tabs = Kit.Node("Tabs", body);
                Kit.Row(tabs.gameObject, 3f);
                Kit.Size(tabs.gameObject, -1f, 20f);
                for (int i = 0; i < T._pickerPages.Count; ++i)
                {
                    int index = i;
                    PickerPage p = T._pickerPages[i];
                    bool on = i == T._pickerPage;
                    Clickable tab = Pill(tabs, "Tab", 20f, 8f, 5f, on ? Pal.C(0x373A3F) : Pal.C(0x25282D), Pal.C(0x373A3F), 3f);
                    Image dot = Kit.Box("Dot", tab.transform, p.color, 2f);
                    Kit.Size(dot.gameObject, 7f, 7f);
                    Text name = Kit.Text("Name", tab.transform, Kit.Escape(p.name), 11, on ? Pal.C(0xE4E7EC) : Pal.C(0x9A9DA2));
                    tab.Tint(name.color, Pal.C(0xE4E7EC), name);
                    tab.onClick = () => { T._pickerPage = index; _pickScroll = Vector2.zero; FillFloat(PickWin); };
                    tab.onRightClick = e => OpenPickerMenu(e, PageItems(index));
                }
                if (_pickEdit)
                {
                    Clickable add = Pill(tabs, "Addpage", 20f, 6f, 0f, Pal.C(0x25282D), Pal.C(0x373A3F), 3f);
                    Kit.Icon("Icon", add.transform, "plus", Pal.C(0x9A9DA2));
                    add.tooltip = "Add a page";
                    add.onClick = () =>
                    {
                        int n = T._pickerPages.Count;
                        T._pickerPages.Add(new PickerPage { name = "Page " + (n + 1), color = _keySetColors[n % _keySetColors.Length] });
                        T._pickerPage = n;
                        FillFloat(PickWin);
                    };
                }
                Kit.Spacer(tabs);
                Clickable dots = Pill(tabs, "Dots", 20f, 5f, 0f, new Color(0f, 0f, 0f, 0f), Pal.C(0x373A3F), 3f);
                Kit.Icon("Icon", dots.transform, _showPickerNodes ? "eye" : "eyeoff", _showPickerNodes ? Pal.C(0xE4E7EC) : Pal.C(0x6B6E74));
                dots.tooltip = _showPickerNodes ? "Hide the buttons' dots in the scene" : "Show the buttons' dots in the scene: click one to select its node, alt + drag to box select";
                dots.onClick = () => { _showPickerNodes = !_showPickerNodes; FillFloat(PickWin); };
                Clickable edit = Pill(tabs, "Edit", 20f, 5f, 0f, _pickEdit ? Pal.accent : new Color(0f, 0f, 0f, 0f), _pickEdit ? Pal.accent : Pal.C(0x373A3F), 3f);
                Text pen = Kit.Text("Pen", edit.transform, "✎", 12, _pickEdit ? Pal.onAccent : Pal.C(0x9A9DA2), TextAnchor.MiddleCenter);
                edit.tooltip = _pickEdit ? "Done editing" : "Edit the buttons";
                edit.onClick = () => { _pickEdit = !_pickEdit; FillFloat(PickWin); };

                // .pk-area: the page, scaled down to fit the width, but only so far: a large page (as
                // ShalltyUtils' often are) scrolls rather than shrinking out of reading.
                Image area = Kit.Box("Area", body, Pal.C(0x1B1D22), 4f);
                Kit.Size(area.gameObject, -1f, _pickerAreaH);
                area.gameObject.AddComponent<RectMask2D>();
                area.raycastTarget = true;
                if (_pickEdit)
                    DrawDots(area.rectTransform);
                RectTransform sheet = Kit.Node("Sheet", area.transform);
                sheet.anchorMin = sheet.anchorMax = new Vector2(0f, 1f);
                sheet.pivot = new Vector2(0f, 1f);
                float right = _pickerAreaW, bottom = _pickerAreaH;
                foreach (PickerButton b in page.buttons)
                {
                    right = Mathf.Max(right, b.box.xMax + 8f);
                    bottom = Mathf.Max(bottom, b.box.yMax + 8f);
                }
                float scale = Mathf.Clamp(_pickerAreaW / right, 0.5f, 1f);
                sheet.localScale = new Vector3(scale, scale, 1f);
                sheet.sizeDelta = new Vector2(right, bottom);
                sheet.anchoredPosition = _pickScroll;
                ScrollRect scroll = area.gameObject.AddComponent<ScrollRect>();
                scroll.viewport = area.rectTransform;
                scroll.content = sheet;
                scroll.horizontal = right * scale > _pickerAreaW + 1f;
                scroll.vertical = bottom * scale > _pickerAreaH + 1f;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = false;
                scroll.scrollSensitivity = 24f;
                scroll.onValueChanged.AddListener(v => _pickScroll = sheet.anchoredPosition);
                GuideObject[] selected = GuideObjectManager.Instance == null ? null : GuideObjectManager.Instance.selectObjects;
                foreach (PickerButton b in page.buttons)
                    PickerButtonView(sheet, page, b, selected != null && b.target != null && selected.Contains(b.target), scale);
                if (page.buttons.Count == 0)
                {
                    Text empty = Kit.Text("Empty", area.transform, "This page has no buttons yet.\nPress ✎, select a node in the scene, then + Button.", 11, Pal.C(0x6B6E74), TextAnchor.MiddleCenter);
                    empty.rectTransform.Fill(20f, 0f, 20f, 0f);
                }

                if (_pickEdit)
                {
                    GuideObject node = T.SelectedNode();
                    RectTransform line = Line(body);
                    Clickable add = Btn(line, "+ Button for " + (node != null ? NodeLabel(node) : "the node selected in the scene"), true, () => AddPickerButton(page), null);
                    add.normal = Pal.accent;
                    add.hover = Pal.C(0xF0B558);
                    add.GetComponentInChildren<Text>().color = Pal.onAccent;
                    add.Refresh();
                    Note(body, "Drag a button to move it, its corner to size it. Right-click a button or a page for its name, colour and link. ✎ again when done.");
                }
                else
                    Note(body, "Click a button to select its node; Ctrl+click adds it to the selection. The buttons' nodes show as dots in the scene too: click one, or Alt + drag a box around several. Pages made with ShalltyUtils' GuideObject Picker open here.");
            }

            /// <summary>The grid shown while editing, 10 page units apart.</summary>
            private static void DrawDots(RectTransform area)
            {
                RectTransform art = Kit.Node("Dots", area).Fill();
                Paint p = art.gameObject.AddComponent<Paint>();
                p.raycastTarget = false;
                p.Begin();
                for (float x = 5f; x < _pickerAreaW; x += 10f)
                    for (float y = 5f; y < _pickerAreaH; y += 10f)
                        p.Rect(x, y, 1f, 1f, Pal.C(0x33363C));
                p.End();
            }

            /// <summary>.pk-b: a button, half see-through unless its node is selected.</summary>
            private void PickerButtonView(RectTransform sheet, PickerPage page, PickerButton b, bool on, float scale)
            {
                Color fill = b.color;
                fill.a = on ? 1f : _pickEdit ? 0.8f : 0.5f;
                Image box = Kit.Box("Pk", sheet, fill, 3f);
                box.raycastTarget = true;
                RectTransform rect = box.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(b.box.x, -b.box.y);
                rect.sizeDelta = new Vector2(b.box.width, b.box.height);
                if (on)
                    Kit.Ring("On", rect, Pal.C(0xE4E7EC), 3f, 2f);
                Text label = Kit.Text("Label", rect, Kit.Escape(b.label), 10, Pal.C(0x15171B), TextAnchor.MiddleCenter, true);
                label.rectTransform.Fill(2f, 0f, 2f, 0f);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 7;
                label.resizeTextMaxSize = 10;
                Clickable click = box.gameObject.AddComponent<Clickable>();
                click.background = box;
                Color hover = fill;
                hover.a = Mathf.Max(fill.a, 0.8f);
                click.normal = fill;
                click.hover = hover;
                click.pressed = hover;
                click.tooltip = b.target != null ? NodeLabel(b.target) : "Not linked to a node";
                click.onRightClick = e => OpenPickerMenu(e, ButtonItems(page, b));
                if (_pickEdit == false)
                {
                    click.onClick = () =>
                    {
                        if (b.target == null)
                        {
                            Toast("This button is not linked to a node.");
                            return;
                        }
                        T.PickNode(b.target, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
                        RefreshFloats();
                    };
                    return;
                }
                // Editing: drag to move, the corner to size, on a grid of 2 page units.
                RectTransform corner = Kit.Node("Rs", rect);
                corner.anchorMin = corner.anchorMax = corner.pivot = new Vector2(1f, 0f);
                corner.sizeDelta = new Vector2(8f, 8f);
                corner.anchoredPosition = Vector2.zero;
                Image grip = corner.gameObject.AddComponent<Image>();
                grip.color = new Color(0f, 0f, 0f, 0.45f);
                DragBox(box.gameObject, b, rect, false, scale);
                DragBox(corner.gameObject, b, rect, true, scale);
            }

            private void DragBox(GameObject handle, PickerButton b, RectTransform rect, bool size, float scale)
            {
                DragHandler drag = handle.AddComponent<DragHandler>();
                Rect start = b.box;
                Vector2 mouse = Vector2.zero;
                drag.onBeginDrag = e =>
                {
                    start = b.box;
                    mouse = e.position;
                    PickWin.busy = true;
                };
                drag.onDrag = e =>
                {
                    Vector2 d = (e.position - mouse) / (canvas.scaleFactor * scale);
                    float dx = Mathf.Round(d.x / 2f) * 2f, dy = Mathf.Round(-d.y / 2f) * 2f;
                    if (size)
                        b.box = new Rect(start.x, start.y, Mathf.Clamp(start.width + dx, 16f, 400f), Mathf.Clamp(start.height + dy, 12f, 300f));
                    else
                        b.box = new Rect(Mathf.Max(0f, start.x + dx), Mathf.Max(0f, start.y + dy), start.width, start.height);
                    rect.anchoredPosition = new Vector2(b.box.x, -b.box.y);
                    rect.sizeDelta = new Vector2(b.box.width, b.box.height);
                };
                drag.onEndDrag = e =>
                {
                    PickWin.busy = false;
                    FillFloat(PickWin);
                };
            }

            private void AddPickerButton(PickerPage page)
            {
                GuideObject node = T.SelectedNode();
                if (node == null)
                {
                    Toast("Select a node in the scene first: one of the circles or squares on the character.");
                    return;
                }
                int n = page.buttons.Count;
                page.buttons.Add(new PickerButton
                {
                    label = NodeLabel(node),
                    color = _keySetColors[n % _keySetColors.Length],
                    box = new Rect(8f + n % 4 * 62f, 8f + n / 4 % 5 * 30f, 56f, 24f),
                    target = node
                });
                FillFloat(PickWin);
            }

            private List<MenuItem> ButtonItems(PickerPage page, PickerButton b)
            {
                GuideObject node = T.SelectedNode();
                return new List<MenuItem>
                {
                    new MenuItem { head = "BUTTON “" + b.label + "”" },
                    new MenuItem { label = "Rename…", act = () => RenameDialog("Rename button", b.label, v => { b.label = v; FillFloat(PickWin); }) },
                    new MenuItem { label = "Colour…", act = () => PickColour("Button colour", b.color, c => { b.color = c; FillFloat(PickWin); }) },
                    new MenuItem
                    {
                        label = node != null ? "Link to " + NodeLabel(node) : "Link to the selected node",
                        disabled = node == null,
                        act = () => { b.target = node; FillFloat(PickWin); }
                    },
                    new MenuItem { sep = true },
                    new MenuItem { label = "Delete button", act = () => { page.buttons.Remove(b); FillFloat(PickWin); } }
                };
            }

            private List<MenuItem> PageItems(int index)
            {
                PickerPage page = T._pickerPages[index];
                return new List<MenuItem>
                {
                    new MenuItem { head = "PAGE “" + page.name + "”" },
                    new MenuItem { label = "Rename…", act = () => RenameDialog("Rename page", page.name, v => { page.name = v; FillFloat(PickWin); }) },
                    new MenuItem { label = "Colour…", act = () => PickColour("Page colour", page.color, c => { page.color = c; FillFloat(PickWin); }) },
                    new MenuItem { label = "Dots in the scene", check = page.showNodes, act = () => page.showNodes = !page.showNodes },
                    new MenuItem { sep = true },
                    new MenuItem
                    {
                        label = "Delete page",
                        disabled = T._pickerPages.Count < 2,
                        act = () => Confirm("Delete page", "“" + page.name + "” and its " + page.buttons.Count + " button(s) are removed.", "Delete", () =>
                        {
                            T._pickerPages.Remove(page);
                            T._pickerPage = 0;
                            FillFloat(PickWin);
                        })
                    }
                };
            }

            /// <summary>A menu where the pointer is, even over a window outside the Timeline one.</summary>
            private void OpenPickerMenu(PointerEventData e, List<MenuItem> items)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_win, e.position, null, out local);
                OpenMenu(items, local.x, -local.y, false);
            }

            private void RenameDialog(string title, string value, System.Action<string> apply)
            {
                RectTransform dialog = OpenDialog(title, winW / 2f - 140f, 60f);
                RectTransform line = Line(dialog);
                InputField field = Fld(line, null, 0, value, null, null);
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Cancel", false, CloseMenu, null);
                Clickable ok = Btn(buttons, "Rename", false, () =>
                {
                    string v = field.text.Trim();
                    CloseMenu();
                    if (v.Length != 0)
                        apply(v);
                }, null);
                ok.normal = Pal.accent;
                ok.hover = Pal.C(0xF0B558);
                ok.GetComponentInChildren<Text>().color = Pal.onAccent;
                ok.Refresh();
                PlaceDialog(dialog);
                field.ActivateInputField();
            }
        }
    }
}
