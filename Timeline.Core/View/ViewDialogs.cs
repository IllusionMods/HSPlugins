using System;
using System.Collections.Generic;
using Timeline.Graph;
using Timeline.Nla;
using Timeline.View;
using UILib.EventHandlers;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        /// <summary>Scene length and speed: the ⋯ menu and the "/ 10.00" after the time both open it.</summary>
        private void OpenTimingPanel()
        {
            if (_view != null)
                _view.OpenLength();
        }

        internal sealed partial class View
        {
            #region Dialogs
            /// <summary>.cmenu.dlg: a small form in the menu's box, with a title, rows and buttons at the bottom right.</summary>
            private RectTransform OpenDialog(string title, float x, float y)
            {
                CloseMenu();
                EnsureBlocker();
                Image box = Kit.Box("Dialog", _menuLayer, Pal.C(0x1E2025), 4f);
                box.raycastTarget = true;
                RectTransform rect = box.rectTransform;
                Kit.Col(box.gameObject, 8f, new RectOffset(12, 12, 10, 10));
                ContentSizeFitter fit = box.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                LayoutElement min = box.gameObject.AddComponent<LayoutElement>();
                min.minWidth = 270f;
                Kit.Ring("Border", rect, Pal.C(0x0D0E10), 4f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Kit.Text("Title", rect, title, 12, Pal.C(0xE4E7EC), TextAnchor.MiddleLeft, true);
                _openMenus.Add(rect);
                _dialogAt = new Vector2(x, y);
                return rect;
            }

            private Vector2 _dialogAt;

            /// <summary>The widest a dialog grows: past it, its notes wrap instead of pulling it wider.</summary>
            private const float _dialogMaxW = 400f;

            private void PlaceDialog(RectTransform dialog)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(dialog);
                // A note's preferred width is its whole text on one line, which a dialog fitted to its
                // content would take as its own. The width is set once here, at the most, so notes wrap,
                // and stays when the dialog fills itself again (the Bake one does, per source).
                float most = Mathf.Min(_dialogMaxW, winW - 4f);
                dialog.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                dialog.sizeDelta = new Vector2(Mathf.Min(most, dialog.rect.width), dialog.sizeDelta.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(dialog);
                LayoutRebuilder.ForceRebuildLayoutImmediate(dialog);
                float w = dialog.rect.width, h = dialog.rect.height;
                PlaceInWindow(dialog, Mathf.Clamp(_dialogAt.x, 2f, winW - w - 2f), Mathf.Clamp(_dialogAt.y, 2f, winH - h - 2f));
            }

            private static RectTransform DialogButtons(RectTransform dialog)
            {
                RectTransform row = Kit.Node("Dlg-f", dialog);
                Kit.Row(row.gameObject, 6f);
                Kit.Size(row.gameObject, -1f, 24f);
                Kit.Spacer(row);
                return row;
            }

            /// <summary>
            /// A question before something that cannot be taken back, in this window's style rather than
            /// the game's. Things undo can restore, such as deleting keys, do not ask at all.
            /// </summary>
            public void Confirm(string title, string message, string action, Action onOk)
            {
                RectTransform dialog = OpenDialog(title, winW / 2f - 150f, winH / 2f - 60f);
                Note(dialog, message);
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Cancel", false, CloseMenu, null);
                Clickable ok = Btn(buttons, action, false, () =>
                {
                    CloseMenu();
                    onOk();
                    Touch();
                }, null);
                ok.GetComponentInChildren<Text>().color = Pal.Hex(0xF08A7E);
                PlaceDialog(dialog);
            }

            /// <summary>Deletes the selected keys straight away, as the playground does: undo brings them back.</summary>
            public void DeleteKeys()
            {
                if (T._selectedKeyframes.Count == 0)
                    return;
                T.DeleteKeyframes(new List<KeyValuePair<float, Keyframe>>(T._selectedKeyframes));
                Touch();
            }

            /// <summary>openLength(): the scene's length, and how fast it plays.</summary>
            public void OpenLength()
            {
                Vector2 at = _tc != null ? WinPoint((RectTransform)_tc.transform.parent, 0f) : new Vector2(winW / 2f - 140f, 30f);
                RectTransform dialog = OpenDialog("Scene length", at.x, at.y + 24f);
                RectTransform line = Line(dialog);
                InputField length = Fld(line, "Length", 0x9A9DA2, Fmt(T._duration), "s", null);
                line = Line(dialog);
                InputField speed = Fld(line, "Speed", 0x9A9DA2, Time.timeScale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), "×", null);
                line = Line(dialog);
                InputField fps = Fld(line, "Frames per second", 0x9A9DA2, T._desiredFrameRate.ToString(), null, null);
                Note(dialog, "Keys after the new end stay where they are and play no more.");
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Cancel", false, CloseMenu, null);
                Clickable ok = Btn(buttons, "OK", false, () =>
                {
                    T.UpdateDuration(length.text);
                    float s;
                    if (float.TryParse(speed.text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out s) && s > 0f)
                        Time.timeScale = s;
                    T.UpdateDesiredFrameRate(fps.text);
                    CloseMenu();
                    Touch();
                }, null);
                ok.normal = Pal.accent;
                ok.hover = Pal.C(0xF0B558);
                ok.GetComponentInChildren<Text>().color = Pal.onAccent;
                ok.Refresh();
                PlaceDialog(dialog);
            }
            #endregion

        }
    }
}
