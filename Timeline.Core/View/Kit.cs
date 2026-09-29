using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timeline.View
{
    /// <summary>
    /// Building blocks named after the playground's CSS: a box with a radius and a colour, a text in
    /// Arial at a pixel size, a flex row with a gap. The view is written with these so that each piece
    /// can be read side by side with the rule it copies.
    /// </summary>
    internal static class Kit
    {
        private static Font _font;

        /// <summary>Arial, as in the playground and in Unity's own default, rather than each game's font.</summary>
        public static Font font
        {
            get
            {
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static float TextWidth(string text, int size, bool bold = false)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            FontStyle style = bold ? FontStyle.Bold : FontStyle.Normal;
            font.RequestCharactersInTexture(text, size, style);
            float width = 0f;
            foreach (char c in text)
            {
                CharacterInfo info;
                if (font.GetCharacterInfo(c, out info, size, style))
                    width += info.advance;
            }
            return width;
        }

        #region Nodes
        public static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Stretches to fill the parent, inset by the given edges (left, top, right, bottom).</summary>
        public static RectTransform Fill(this RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Positioned like an absolutely placed element: from the top left corner, with a size.</summary>
        public static RectTransform At(this RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>
        /// Anchored the CSS way: any of left, top, right, bottom, width and height, with NaN for the ones
        /// the rule leaves out.
        /// </summary>
        public static RectTransform Css(this RectTransform rect, float left, float top, float right, float bottom, float width = float.NaN, float height = float.NaN)
        {
            bool l = float.IsNaN(left) == false, r = float.IsNaN(right) == false, t = float.IsNaN(top) == false, b = float.IsNaN(bottom) == false;
            Vector2 min = new Vector2(l ? 0f : 1f, b ? 0f : 1f), max = new Vector2(r ? 1f : 0f, t ? 1f : 0f);
            if (l == false && r == false)
                min.x = max.x = 0f;
            if (t == false && b == false)
                min.y = max.y = 1f;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(l || r == false ? 0f : 1f, t || b == false ? 1f : 0f);
            float x0, x1, y0, y1;
            if (l && r) { x0 = left; x1 = -right; }
            else if (l) { x0 = left; x1 = left + width; }
            else if (r) { x0 = -right - width; x1 = -right; }
            else { x0 = 0f; x1 = width; }
            if (t && b) { y0 = bottom; y1 = -top; }
            else if (t) { y0 = -top - height; y1 = -top; }
            else if (b) { y0 = bottom; y1 = bottom + height; }
            else { y0 = -height; y1 = 0f; }
            rect.offsetMin = new Vector2(x0, y0);
            rect.offsetMax = new Vector2(x1, y1);
            return rect;
        }
        #endregion

        #region Boxes
        private static readonly Dictionary<int, Sprite> _rounded = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> _rings = new Dictionary<int, Sprite>();
        private const int _oversample = 4;

        /// <summary>A background with rounded corners, border-radius in pixels.</summary>
        public static Image Box(string name, Transform parent, Color color, float radius = 0f)
        {
            RectTransform rect = Node(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            SetRadius(image, radius);
            image.raycastTarget = false;
            return image;
        }

        public static void SetRadius(Image image, float radius)
        {
            if (radius <= 0f)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                return;
            }
            image.sprite = Rounded(radius);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
        }

        /// <summary>An inset one pixel outline, box-shadow: inset 0 0 0 1px in the stylesheet.</summary>
        public static Image Ring(string name, Transform parent, Color color, float radius, float width = 1f)
        {
            RectTransform rect = Node(name, parent);
            rect.Fill();
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = RingSprite(radius, width);
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Sprite Rounded(float radius)
        {
            int key = Mathf.RoundToInt(radius * 4f);
            Sprite sprite;
            if (_rounded.TryGetValue(key, out sprite) && sprite != null)
                return sprite;
            sprite = MakeSprite(radius, 0f, "Round" + key);
            _rounded[key] = sprite;
            return sprite;
        }

        public static Sprite RingSprite(float radius, float width)
        {
            int key = Mathf.RoundToInt(radius * 4f) * 1000 + Mathf.RoundToInt(width * 4f);
            Sprite sprite;
            if (_rings.TryGetValue(key, out sprite) && sprite != null)
                return sprite;
            sprite = MakeSprite(Mathf.Max(radius, width), width, "Ring" + key);
            _rings[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// A nine sliced rounded square, drawn here rather than shipped: four times oversampled and
        /// mipmapped like the image pack, so its corners stay smooth at any interface scale.
        /// </summary>
        /// <summary>A soft edged box for shadows: box-shadow with a blur, as a nine slice.</summary>
        public static Sprite Soft(float radius, float blur)
        {
            int key = -(Mathf.RoundToInt(radius) * 1000 + Mathf.RoundToInt(blur));
            Sprite sprite;
            if (_rounded.TryGetValue(key, out sprite) && sprite != null)
                return sprite;
            sprite = MakeSprite(radius + blur, 0f, "Soft" + key, blur);
            _rounded[key] = sprite;
            return sprite;
        }

        private static Sprite MakeSprite(float radius, float ring, string name, float blur = 0f)
        {
            int border = Mathf.CeilToInt(radius) + 1;
            int size = (border * 2 + 2) * _oversample;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[size * size];
            float half = size / 2f, r = radius * _oversample, w = ring * _oversample;
            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float qx = Mathf.Abs(px - half) - (half - r), qy = Mathf.Abs(py - half) - (half - r);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                    float d = w > 0f ? Mathf.Abs(outside + w * 0.5f) - w * 0.5f : outside;
                    float a = blur > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-d / (blur * _oversample))) : Mathf.Clamp01(0.5f - d / 1f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            float b = border * _oversample;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f * _oversample, 0,
                                          SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
        #endregion

        #region Text
        public static Text Text(string name, Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            RectTransform rect = Node(name, parent);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.color = color;
            label.alignment = anchor;
            label.text = text;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>Text that wraps inside its width, like a paragraph with line-height 1.4 or so.</summary>
        public static Text Paragraph(string name, Transform parent, string text, int size, Color color)
        {
            Text label = Text(name, parent, text, size, color, TextAnchor.UpperLeft);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.lineSpacing = 1.15f;
            return label;
        }

        /// <summary>The italic span of the stylesheet, which is only ever a dimmer colour there.</summary>
        public static string Dim(string text, uint rgb = 0x9A9DA2)
        {
            return "<color=" + Pal.Html(Pal.C(rgb)) + ">" + text + "</color>";
        }

        public static string Escape(string text)
        {
            return text == null ? "" : text.Replace("<", "<​");
        }
        #endregion

        #region Icons
        public static IconView Icon(string name, Transform parent, string icon, Color color, float scale = 1f)
        {
            RectTransform rect = Node(name, parent);
            IconView view = rect.gameObject.AddComponent<IconView>();
            view.Set(icon, color, scale);
            Vector2 size = Icons.Size(icon) * scale;
            rect.sizeDelta = size;
            LayoutElement le = rect.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = size.x;
            le.preferredHeight = le.minHeight = size.y;
            return view;
        }
        #endregion

        #region Layout
        /// <summary>display:flex; align-items:center; gap.</summary>
        public static HorizontalLayoutGroup Row(GameObject go, float gap, float padLeft = 0f, float padRight = 0f, TextAnchor align = TextAnchor.MiddleLeft)
        {
            HorizontalLayoutGroup row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = gap;
            row.padding = new RectOffset(Mathf.RoundToInt(padLeft), Mathf.RoundToInt(padRight), 0, 0);
            row.childAlignment = align;
            SetControl(row);
            return row;
        }

        /// <summary>display:flex; flex-direction:column; gap.</summary>
        public static VerticalLayoutGroup Col(GameObject go, float gap, RectOffset padding = null)
        {
            VerticalLayoutGroup col = go.AddComponent<VerticalLayoutGroup>();
            col.spacing = gap;
            col.padding = padding ?? new RectOffset();
            col.childAlignment = TextAnchor.UpperLeft;
            SetControl(col);
            col.childForceExpandWidth = true;
            col.childControlWidth = true;
            return col;
        }

        private static void SetControl(HorizontalOrVerticalLayoutGroup group)
        {
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
        }

        /// <summary>Fixed width and height in a flex row: flex:none with a size.</summary>
        public static LayoutElement Size(GameObject go, float width, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (width >= 0f)
                le.minWidth = le.preferredWidth = width;
            if (height >= 0f)
                le.minHeight = le.preferredHeight = height;
            le.flexibleWidth = 0f;
            return le;
        }

        /// <summary>flex:1 with min-width:0.</summary>
        public static LayoutElement Flex(GameObject go, float height = -1f, float grow = 1f)
        {
            LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = grow;
            le.minWidth = 0f;
            le.preferredWidth = 0f;
            if (height >= 0f)
                le.minHeight = le.preferredHeight = height;
            return le;
        }

        /// <summary>A flexible gap that pushes what follows to the far end: margin-left:auto.</summary>
        public static void Spacer(Transform parent)
        {
            Flex(Node("Spacer", parent).gameObject);
        }
        #endregion
    }

    /// <summary>A drawn graphic that takes a colour the way text does: currentColor in the playground's SVG.</summary>
    internal interface ITint
    {
        void SetTint(Color c);
    }

    /// <summary>One of the playground's icons as a graphic of its own.</summary>
    internal class IconView : Paint, ITint
    {
        private string _icon;
        private Color _color;
        private float _scale = 1f;
        private bool _dirty = true;

        public string icon { get { return _icon; } }

        public void Set(string icon, Color color, float scale = 1f)
        {
            if (_icon == icon && _color == color && Mathf.Approximately(_scale, scale))
                return;
            _icon = icon;
            _color = color;
            _scale = scale;
            _dirty = true;
            Redraw();
        }

        public void SetColor(Color color)
        {
            Set(_icon, color, _scale);
        }

        public void SetTint(Color c)
        {
            SetColor(c);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            _dirty = true;
            Redraw();
        }

        public override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            _dirty = true;
            Redraw();
        }

        public override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            _dirty = true;
            Redraw();
        }

        public void Redraw()
        {
            if (_dirty == false || isActiveAndEnabled == false)
                return;
            _dirty = false;
            Begin();
            if (Icons.Has(_icon))
            {
                Vector2 size = Icons.Size(_icon) * _scale;
                Icons.Draw(this, _icon, (width - size.x) * 0.5f, (height - size.y) * 0.5f, _color, _scale);
            }
            End();
        }
    }

    /// <summary>
    /// Hover, press and click for a box, the :hover rules of the stylesheet: a background colour and
    /// a text colour for each state, switched without a fade.
    /// </summary>
    internal class Clickable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Image background;
        public Color normal = new Color(0f, 0f, 0f, 0f);
        public Color hover = new Color(0f, 0f, 0f, 0f);
        public Color pressed = new Color(0f, 0f, 0f, 0f);
        public readonly List<Graphic> tinted = new List<Graphic>();
        public Color tintNormal;
        public Color tintHover;
        public bool hasTint;
        public bool disabled;
        public string tooltip;
        public Action onClick;
        public Action<PointerEventData> onRightClick;
        public Action<PointerEventData> onDown;
        private bool _over;
        private bool _down;

        public bool isOver { get { return _over; } }

        public void Tint(Color normalColor, Color hoverColor, params Graphic[] graphics)
        {
            hasTint = true;
            tintNormal = normalColor;
            tintHover = hoverColor;
            tinted.Clear();
            tinted.AddRange(graphics);
            Refresh();
        }

        public void Refresh()
        {
            if (background != null)
                background.color = disabled ? normal : _down ? pressed : _over ? hover : normal;
            if (hasTint)
            {
                Color c = _over && disabled == false ? tintHover : tintNormal;
                foreach (Graphic g in tinted)
                {
                    ITint drawn = g as ITint;
                    if (drawn != null)
                        drawn.SetTint(c);
                    else if (g != null)
                        g.color = c;
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _over = true;
            Refresh();
            if (string.IsNullOrEmpty(tooltip) == false)
                Tooltip.Show(tooltip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _over = false;
            _down = false;
            Refresh();
            if (string.IsNullOrEmpty(tooltip) == false)
                Tooltip.Hide();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (disabled)
                return;
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _down = true;
                Refresh();
            }
            if (onDown != null)
                onDown(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _down = false;
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (disabled)
                return;
            if (eventData.button == PointerEventData.InputButton.Left && onClick != null)
                onClick();
            else if (eventData.button == PointerEventData.InputButton.Right && onRightClick != null)
                onRightClick(eventData);
        }

        private void OnDisable()
        {
            // Taken away under the pointer, as a row that is rebuilt is: its tooltip goes with it.
            if (_over && string.IsNullOrEmpty(tooltip) == false)
                Tooltip.Hide();
            _over = false;
            _down = false;
        }
    }
}
