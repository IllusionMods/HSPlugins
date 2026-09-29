using System.Collections.Generic;
using Timeline.Graph;
using Timeline.Nla;
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
            private RectTransform _status;
            private RectTransform _statusHints;
            private Text _statusRight;
            private string _statusShown;

            /// <summary>
            /// HINTS: what the mouse and keys do where the pointer is. Each hint is words and key names;
            /// a key name is written between square brackets and drawn as a kbd chip.
            /// </summary>
            private static readonly Dictionary<string, string[]> _hints = new Dictionary<string, string[]>
            {
                { "ruler", new[] { "[Click]/[drag] move the playhead", "[Drag] a marker to move it", "[Right-click] add, rename or delete markers" } },
                { "dope", new[] { "[Click] select", "[Ctrl]+click add to selection", "[Drag] move", "[Middle] add key", "[Ctrl]+middle delete", "[Shift] flips snapping", "[Scroll] rows · [Ctrl]+scroll zoom", "[Right-click] menu" } },
                { "graph", new[] { "[Drag] a key or handle", "[Ctrl] time only", "[Alt] value only", "[Scroll] zoom", "[Alt]+drag pan", "[Home] fit", "[F] frame selected" } },
                { "nla", new[] { "[Drag] a strip to move it, up or down to change lane", "[Drag] its ends to stretch", "[Shift+D] duplicate", "[Y] split", "[X] delete" } },
                { "chan", new[] { "[Click] select track", "[Ctrl]+click add", "Eye: show in Graph", "Lock: stop edits", "[Right-click] rename, group" } },
                { "props", new[] { "Type a value and press [Enter]", "[Esc] leaves the field" } },
            };

            /// <summary>.status: 22 high, #181A1E, a hairline above it, the hints on the left and the counts on the right.</summary>
            private void BuildStatus()
            {
                Image bg = Kit.Box("Status", _win, Pal.C(0x181A1E));
                _status = bg.rectTransform.Css(0f, float.NaN, 0f, 0f, float.NaN, STATUS);
                Kit.Box("Border", _status, Pal.C(0x111215)).rectTransform.Css(0f, 0f, 0f, float.NaN, float.NaN, 1f);
                _statusHints = Kit.Node("Hints", _status).Fill(10f, 1f, 200f, 0f);
                Kit.Row(_statusHints.gameObject, 14f);
                _statusHints.gameObject.AddComponent<RectMask2D>();
                _statusRight = Kit.Text("R", _status, "", 11, Pal.C(0x6B6E74), TextAnchor.MiddleRight);
                _statusRight.rectTransform.Css(float.NaN, 1f, 14f, 0f, 260f);
            }

            private void TickStatus()
            {
                string area = PointerArea();
                if (area != hover)
                    hover = area;
                string key = _hints.ContainsKey(hover) ? hover : editor;
                if (key != _statusShown)
                {
                    _statusShown = key;
                    RenderHints(_hints[key]);
                }
                if (TickToast())
                    return;
                int n = editor == "nla" ? T._selectedStrips.Count : T._selectedKeyframes.Count;
                string what = editor == "nla" ? "strip" : "key";
                string right = (n != 0 ? n + " " + what + (n > 1 ? "s" : "") + " selected" : "Nothing selected") + " · " + T._desiredFrameRate + " fps";
                if (_statusRight.text != right)
                    _statusRight.text = right;
            }

            private string PointerArea()
            {
                Vector2 mouse = Input.mousePosition;
                if (_props.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_props, mouse, null))
                    return "props";
                if (RectTransformUtility.RectangleContainsScreenPoint(_chan, mouse, null))
                    return "chan";
                string grid = GridHover();
                return grid ?? "";
            }

            private void RenderHints(string[] hints)
            {
                for (int i = _statusHints.childCount - 1; i >= 0; --i)
                    Object.Destroy(_statusHints.GetChild(i).gameObject);
                foreach (string hint in hints)
                {
                    RectTransform span = Kit.Node("Hint", _statusHints);
                    Kit.Row(span.gameObject, 0f);
                    int at = 0;
                    while (at < hint.Length)
                    {
                        int open = hint.IndexOf('[', at);
                        if (open < 0)
                        {
                            Kit.Text("T", span, hint.Substring(at), 11, Pal.C(0x9A9DA2));
                            break;
                        }
                        if (open > at)
                            Kit.Text("T", span, hint.Substring(at, open - at), 11, Pal.C(0x9A9DA2));
                        int close = hint.IndexOf(']', open);
                        // kbd: #2C2F35, radius 3, 1 by 4 padding, 10 pixel text in the main colour.
                        Image kbd = Kit.Box("Kbd", span, Pal.C(0x2C2F35), 3f);
                        Kit.Row(kbd.gameObject, 0f, 4f, 4f);
                        Kit.Size(kbd.gameObject, -1f, 14f);
                        Kit.Text("T", kbd.transform, hint.Substring(open + 1, close - open - 1), 10, Pal.C(0xE4E7EC));
                        at = close + 1;
                    }
                }
            }
        }
    }

    /// <summary>The pointer being let go over something: UILib has down, but not up.</summary>
    internal class PointerUpHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action<PointerEventData> onPointerUp;

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (onPointerUp != null)
                onPointerUp(eventData);
        }
    }
}
