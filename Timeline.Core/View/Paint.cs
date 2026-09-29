using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline.View
{
    /// <summary>
    /// A 2D canvas for uGUI: what the playground draws with a canvas context, this draws as a mesh.
    ///
    /// Coordinates are the canvas's, from the top left corner of the rect and growing downwards, so the
    /// playground's drawing code ports over line for line. Everything that is not an axis aligned
    /// rectangle gets a one pixel feathered edge, which is the antialiasing a mesh does not otherwise
    /// have: without it a diamond or a curve is a staircase at any scale. The feather is measured in
    /// screen pixels, so it stays one pixel wide whatever the interface scale is.
    ///
    /// Text is not part of the mesh. Labels come from a pool of child Text objects, placed per frame.
    /// </summary>
    internal class Paint : MaskableGraphic
    {
        /// <summary>
        /// A uGUI mesh holds at most 65000 vertices, and a graph of baked tracks draws far more than
        /// that. So the drawing is split into chunks: this graphic shows the first, and a child graphic
        /// each of the others. No shape is ever split between two chunks.
        /// </summary>
        private const int _chunkLimit = 60000;
        private readonly List<List<UIVertex>> _chunkVertices = new List<List<UIVertex>> { new List<UIVertex>() };
        private readonly List<List<int>> _chunkIndices = new List<List<int>> { new List<int>() };
        private readonly List<PaintChunk> _chunkViews = new List<PaintChunk>();
        private int _chunk;
        private List<UIVertex> _vertices;
        private List<int> _indices;
        private readonly List<Text> _labels = new List<Text>();
        private int _labelsUsed;
        private float _feather = 1f;
        private float _alpha = 1f;
        private Rect _clip;
        private bool _clipping;

        public float globalAlpha { get { return _alpha; } set { _alpha = value; } }

        public override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        /// <summary>Starts a new frame of drawing. Everything drawn before is dropped.</summary>
        public void Begin()
        {
            foreach (List<UIVertex> list in _chunkVertices)
                list.Clear();
            foreach (List<int> list in _chunkIndices)
                list.Clear();
            _chunk = 0;
            _vertices = _chunkVertices[0];
            _indices = _chunkIndices[0];
            _labelsUsed = 0;
            _alpha = 1f;
            _clipping = false;
            Canvas owner = canvas;
            float scale = owner != null ? owner.scaleFactor : 1f;
            _feather = 1f / Mathf.Max(scale, 0.01f);
        }

        /// <summary>Ends the frame: hides the labels that were not used and uploads the mesh.</summary>
        public void End()
        {
            // Chunks after the first are drawn by children, placed before the labels so text stays on top.
            for (int c = 1; c < _chunkVertices.Count; ++c)
            {
                bool used = c <= _chunk;
                if (c - 1 >= _chunkViews.Count)
                {
                    if (used == false)
                        break;
                    GameObject go = new GameObject("Chunk", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    RectTransform rt = (RectTransform)go.transform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    PaintChunk view = go.AddComponent<PaintChunk>();
                    view.raycastTarget = false;
                    _chunkViews.Add(view);
                }
                PaintChunk chunk = _chunkViews[c - 1];
                chunk.transform.SetSiblingIndex(c - 1);
                chunk.vertices = _chunkVertices[c];
                chunk.indices = _chunkIndices[c];
                if (chunk.gameObject.activeSelf != used)
                    chunk.gameObject.SetActive(used);
                if (used)
                    chunk.SetVerticesDirty();
            }
            for (int i = _labelsUsed; i < _labels.Count; ++i)
            {
                if (_labels[i].gameObject.activeSelf)
                    _labels[i].gameObject.SetActive(false);
            }
            SetVerticesDirty();
        }

        public float width { get { return rectTransform.rect.width; } }
        public float height { get { return rectTransform.rect.height; } }

        /// <summary>Only rectangles are clipped, and only by trimming: enough for rows scrolling under a ruler.</summary>
        public void Clip(float x, float y, float w, float h)
        {
            _clip = new Rect(x, y, w, h);
            _clipping = true;
        }

        public void Unclip()
        {
            _clipping = false;
        }

        /// <summary>Makes room for a shape of n vertices, moving on to a new chunk when this one is full.</summary>
        private void Reserve(int n)
        {
            if (_vertices.Count + n <= _chunkLimit)
                return;
            ++_chunk;
            if (_chunk == _chunkVertices.Count)
            {
                _chunkVertices.Add(new List<UIVertex>());
                _chunkIndices.Add(new List<int>());
            }
            _vertices = _chunkVertices[_chunk];
            _indices = _chunkIndices[_chunk];
        }

        #region Shapes
        /// <summary>An axis aligned rectangle. Not feathered: callers put these on whole pixels.</summary>
        public void Rect(float x, float y, float w, float h, Color color)
        {
            if (w <= 0f || h <= 0f)
                return;
            if (_clipping)
            {
                float x1 = Mathf.Min(x + w, _clip.xMax), y1 = Mathf.Min(y + h, _clip.yMax);
                x = Mathf.Max(x, _clip.xMin);
                y = Mathf.Max(y, _clip.yMin);
                w = x1 - x;
                h = y1 - y;
                if (w <= 0f || h <= 0f)
                    return;
            }
            color.a *= _alpha;
            Reserve(4);
            int i = _vertices.Count;
            Add(x, y, color);
            Add(x + w, y, color);
            Add(x + w, y + h, color);
            Add(x, y + h, color);
            Quad(i, i + 1, i + 2, i + 3);
        }

        /// <summary>A one pixel outline inside the given rectangle.</summary>
        public void StrokeRect(float x, float y, float w, float h, float line, Color color)
        {
            Rect(x, y, w, line, color);
            Rect(x, y + h - line, w, line, color);
            Rect(x, y + line, line, h - 2f * line, color);
            Rect(x + w - line, y + line, line, h - 2f * line, color);
        }

        public void DashedRect(float x, float y, float w, float h, float dash, Color color)
        {
            for (float d = 0f; d < w; d += dash * 2f)
            {
                Rect(x + d, y, Mathf.Min(dash, w - d), 1f, color);
                Rect(x + d, y + h - 1f, Mathf.Min(dash, w - d), 1f, color);
            }
            for (float d = 0f; d < h; d += dash * 2f)
            {
                Rect(x, y + d, 1f, Mathf.Min(dash, h - d), color);
                Rect(x + w - 1f, y + d, 1f, Mathf.Min(dash, h - d), color);
            }
        }

        /// <summary>A filled convex polygon, points in either winding.</summary>
        public void Convex(IList<Vector2> points, Color color)
        {
            int n = points.Count;
            if (n < 3)
                return;
            if (_clipping && OutsideClip(points))
                return;
            color.a *= _alpha;
            float area = 0f;
            for (int i = 0; i < n; ++i)
            {
                Vector2 a = points[i], b = points[(i + 1) % n];
                area += a.x * b.y - b.x * a.y;
            }
            // In canvas coordinates y grows downwards: a clockwise outline has a positive area, and for
            // it the left hand normal of each edge, (-dy, dx), points inwards. Flipped, it points out.
            float outward = area > 0f ? -1f : 1f;
            float half = _feather * 0.5f;
            Color clear = color;
            clear.a = 0f;

            Reserve(2 * n);
            int start = _vertices.Count;
            for (int i = 0; i < n; ++i)
            {
                Vector2 prev = points[(i + n - 1) % n], p = points[i], next = points[(i + 1) % n];
                Vector2 normal = MiterNormal(prev, p, next, outward);
                Add(p - normal * half, color);
                Add(p + normal * half, clear);
            }
            for (int i = 1; i < n - 1; ++i)
                Tri(start, start + 2 * i, start + 2 * (i + 1));
            for (int i = 0; i < n; ++i)
            {
                int j = (i + 1) % n;
                Quad(start + 2 * i, start + 2 * j, start + 2 * j + 1, start + 2 * i + 1);
            }
        }

        private readonly List<Vector2> _scratch = new List<Vector2>();

        public void Diamond(float cx, float cy, float side, Color color)
        {
            // A square of that side turned by 45 degrees, the way the playground draws its keys.
            float r = side * 0.70710678f;
            _scratch.Clear();
            _scratch.Add(new Vector2(cx, cy - r));
            _scratch.Add(new Vector2(cx + r, cy));
            _scratch.Add(new Vector2(cx, cy + r));
            _scratch.Add(new Vector2(cx - r, cy));
            Convex(_scratch, color);
        }

        public void Circle(float cx, float cy, float radius, Color color)
        {
            _scratch.Clear();
            int segments = Mathf.Clamp(Mathf.CeilToInt(radius / _feather * 1.2f), 10, 48);
            for (int i = 0; i < segments; ++i)
            {
                float a = i * Mathf.PI * 2f / segments;
                _scratch.Add(new Vector2(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius));
            }
            Convex(_scratch, color);
        }

        public void RoundRect(float x, float y, float w, float h, float radius, Color color)
        {
            radius = Mathf.Min(radius, w * 0.5f, h * 0.5f);
            if (radius <= 0.01f)
            {
                Rect(x, y, w, h, color);
                return;
            }
            _scratch.Clear();
            Corner(x + w - radius, y + radius, radius, -90f);
            Corner(x + w - radius, y + h - radius, radius, 0f);
            Corner(x + radius, y + h - radius, radius, 90f);
            Corner(x + radius, y + radius, radius, 180f);
            Convex(_scratch, color);
        }

        private void Corner(float cx, float cy, float r, float from)
        {
            const int steps = 5;
            for (int i = 0; i <= steps; ++i)
            {
                float a = (from + 90f * i / steps) * Mathf.Deg2Rad;
                _scratch.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
            }
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            _scratch.Clear();
            _scratch.Add(a);
            _scratch.Add(b);
            _scratch.Add(c);
            Convex(_scratch, color);
        }

        public void Line(float x0, float y0, float x1, float y1, float lineWidth, Color color)
        {
            _stroke.Clear();
            _stroke.Add(new Vector2(x0, y0));
            _stroke.Add(new Vector2(x1, y1));
            Stroke(_stroke, lineWidth, color, false);
        }

        private readonly List<Vector2> _stroke = new List<Vector2>();

        /// <summary>
        /// A line through the points, with mitred joins and feathered sides. Lines thinner than a pixel
        /// are drawn a pixel wide and fainter, which is how a canvas renders them too.
        /// </summary>
        public void Stroke(IList<Vector2> points, float lineWidth, Color color, bool closed)
        {
            int n = points.Count;
            if (n < 2)
                return;
            if (_clipping && OutsideClip(points))
                return;
            // A very long line is drawn in pieces, each small enough for one chunk.
            if ((closed ? n + 1 : n) * 4 > _chunkLimit)
            {
                int piece = _chunkLimit / 4 - 2;
                var part = new List<Vector2>(piece + 1);
                for (int from = 0; from < n - 1; from += piece)
                {
                    part.Clear();
                    for (int k = from; k <= Mathf.Min(from + piece, n - 1); ++k)
                        part.Add(points[k]);
                    Stroke(part, lineWidth, color, false);
                }
                return;
            }
            color.a *= _alpha;
            float core = lineWidth * 0.5f - _feather * 0.5f;
            if (lineWidth < _feather)
            {
                color.a *= lineWidth / _feather;
                core = 0f;
            }
            float outer = core + _feather;
            Color clear = color;
            clear.a = 0f;

            int count = closed ? n + 1 : n;
            Reserve(count * 4);
            int start = _vertices.Count;
            for (int k = 0; k < count; ++k)
            {
                int i = k % n;
                Vector2 p = points[i];
                Vector2 normal;
                if (closed || (i > 0 && i < n - 1))
                    normal = MiterNormal(points[(i + n - 1) % n], p, points[(i + 1) % n], 1f);
                else
                {
                    Vector2 d = i == 0 ? points[1] - points[0] : points[n - 1] - points[n - 2];
                    d.Normalize();
                    normal = new Vector2(-d.y, d.x);
                }
                Add(p + normal * outer, clear);
                Add(p + normal * core, color);
                Add(p - normal * core, color);
                Add(p - normal * outer, clear);
            }
            for (int k = 0; k < count - 1; ++k)
            {
                int a = start + 4 * k, b = start + 4 * (k + 1);
                Quad(a, b, b + 1, a + 1);
                Quad(a + 1, b + 1, b + 2, a + 2);
                Quad(a + 2, b + 2, b + 3, a + 3);
            }
        }

        private static Vector2 MiterNormal(Vector2 prev, Vector2 p, Vector2 next, float side)
        {
            Vector2 d0 = p - prev, d1 = next - p;
            if (d0.sqrMagnitude < 1e-8f)
                d0 = d1;
            if (d1.sqrMagnitude < 1e-8f)
                d1 = d0;
            d0.Normalize();
            d1.Normalize();
            Vector2 n0 = new Vector2(-d0.y, d0.x) * side, n1 = new Vector2(-d1.y, d1.x) * side;
            Vector2 m = n0 + n1;
            if (m.sqrMagnitude < 1e-8f)
                return n0;
            m.Normalize();
            float dot = Vector2.Dot(m, n0);
            // Limited, so a sharp corner does not throw a spike out to infinity.
            return m / Mathf.Max(dot, 0.35f);
        }

        private bool OutsideClip(IList<Vector2> points)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (Vector2 p in points)
            {
                x0 = Mathf.Min(x0, p.x);
                y0 = Mathf.Min(y0, p.y);
                x1 = Mathf.Max(x1, p.x);
                y1 = Mathf.Max(y1, p.y);
            }
            return x1 < _clip.xMin || x0 > _clip.xMax || y1 < _clip.yMin || y0 > _clip.yMax;
        }
        #endregion

        #region Text
        /// <summary>
        /// A label at a canvas position. The anchor says which point of the text lands there, as
        /// textAlign and textBaseline do: MiddleCenter centres it on the point.
        /// </summary>
        public Text Label(string text, float x, float y, int size, Color color, TextAnchor anchor, bool bold = false)
        {
            if (_clipping && (y < _clip.yMin - size || y > _clip.yMax + size))
                return null;
            Text label;
            if (_labelsUsed < _labels.Count)
                label = _labels[_labelsUsed];
            else
            {
                GameObject go = new GameObject("Label", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                label = go.AddComponent<Text>();
                label.raycastTarget = false;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.supportRichText = false;
                RectTransform r = label.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.sizeDelta = new Vector2(10f, 10f);
                _labels.Add(label);
            }
            ++_labelsUsed;
            if (label.gameObject.activeSelf == false)
                label.gameObject.SetActive(true);
            label.font = Kit.font;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            color.a *= _alpha;
            label.color = color;
            label.alignment = anchor;
            if (label.text != text)
                label.text = text;
            RectTransform rt = label.rectTransform;
            rt.pivot = PivotOf(anchor);
            rt.anchoredPosition = new Vector2(x, -y);
            return label;
        }

        /// <summary>Width of a label in this font, to lay things out around it.</summary>
        public static float Measure(string text, int size, bool bold = false)
        {
            return Kit.TextWidth(text, size, bold);
        }

        private static Vector2 PivotOf(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return new Vector2(0f, 1f);
                case TextAnchor.UpperCenter: return new Vector2(0.5f, 1f);
                case TextAnchor.UpperRight: return new Vector2(1f, 1f);
                case TextAnchor.MiddleLeft: return new Vector2(0f, 0.5f);
                case TextAnchor.MiddleCenter: return new Vector2(0.5f, 0.5f);
                case TextAnchor.MiddleRight: return new Vector2(1f, 0.5f);
                case TextAnchor.LowerLeft: return new Vector2(0f, 0f);
                case TextAnchor.LowerCenter: return new Vector2(0.5f, 0f);
                default: return new Vector2(1f, 0f);
            }
        }
        #endregion

        #region Mesh
        private void Add(float x, float y, Color color)
        {
            Add(new Vector2(x, y), color);
        }

        private void Add(Vector2 p, Color color)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = new Vector3(p.x, -p.y, 0f);
            v.color = color;
            _vertices.Add(v);
        }

        private void Tri(int a, int b, int c)
        {
            _indices.Add(a);
            _indices.Add(b);
            _indices.Add(c);
        }

        private void Quad(int a, int b, int c, int d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        public override void OnPopulateMesh(VertexHelper vh)
        {
            if (_vertices == null)
            {
                vh.Clear();
                return;
            }
            List<UIVertex> vertices = _chunkVertices[0];
            List<int> indices = _chunkIndices[0];
            vh.Clear();
            // Drawn from the top left corner of the rect, whatever its pivot.
            Rect r = rectTransform.rect;
            Vector3 origin = new Vector3(r.xMin, r.yMax, 0f);
            for (int i = 0; i < vertices.Count; ++i)
            {
                UIVertex v = vertices[i];
                v.position += origin;
                vh.AddVert(v);
            }
            for (int i = 0; i + 2 < indices.Count; i += 3)
                vh.AddTriangle(indices[i], indices[i + 1], indices[i + 2]);
        }
        #endregion
    }

    /// <summary>One more chunk of a Paint's drawing, see Paint._chunkLimit.</summary>
    internal class PaintChunk : MaskableGraphic
    {
        public List<UIVertex> vertices;
        public List<int> indices;

        public override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (vertices == null)
                return;
            Rect r = rectTransform.rect;
            Vector3 origin = new Vector3(r.xMin, r.yMax, 0f);
            for (int i = 0; i < vertices.Count; ++i)
            {
                UIVertex v = vertices[i];
                v.position += origin;
                vh.AddVert(v);
            }
            for (int i = 0; i + 2 < indices.Count; i += 3)
                vh.AddTriangle(indices[i], indices[i + 1], indices[i + 2]);
        }
    }
}
