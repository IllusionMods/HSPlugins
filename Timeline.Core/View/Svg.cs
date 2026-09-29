using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Timeline.View
{
    /// <summary>
    /// SVG path data, flattened to polylines, so the playground's icons are drawn from the very same
    /// path strings rather than redrawn by eye. Handles every command those icons use: M L H V C S Q A Z,
    /// absolute and relative.
    /// </summary>
    internal static class Svg
    {
        public class Shape
        {
            public readonly List<List<Vector2>> parts = new List<List<Vector2>>();
            public readonly List<bool> closed = new List<bool>();
        }

        private static readonly Dictionary<string, Shape> _cache = new Dictionary<string, Shape>();

        public static Shape Parse(string d)
        {
            Shape shape;
            if (_cache.TryGetValue(d, out shape))
                return shape;
            shape = new Shape();
            var reader = new Reader(d);
            List<Vector2> current = null;
            Vector2 pos = Vector2.zero, start = Vector2.zero, lastControl = Vector2.zero;
            char command = 'M', previous = ' ';
            while (reader.More())
            {
                if (reader.PeekCommand())
                    command = reader.Command();
                bool rel = char.IsLower(command);
                char c = char.ToUpperInvariant(command);
                Vector2 o = rel ? pos : Vector2.zero;
                switch (c)
                {
                    case 'M':
                        pos = o + reader.Point();
                        start = pos;
                        current = new List<Vector2> { pos };
                        shape.parts.Add(current);
                        shape.closed.Add(false);
                        // Further pairs after a move are lines.
                        command = rel ? 'l' : 'L';
                        break;
                    case 'L':
                        pos = o + reader.Point();
                        current.Add(pos);
                        break;
                    case 'H':
                        pos = new Vector2((rel ? pos.x : 0f) + reader.Number(), pos.y);
                        current.Add(pos);
                        break;
                    case 'V':
                        pos = new Vector2(pos.x, (rel ? pos.y : 0f) + reader.Number());
                        current.Add(pos);
                        break;
                    case 'C':
                    {
                        Vector2 c1 = o + reader.Point(), c2 = o + reader.Point(), end = o + reader.Point();
                        Cubic(current, pos, c1, c2, end);
                        lastControl = c2;
                        pos = end;
                        break;
                    }
                    case 'S':
                    {
                        char p = char.ToUpperInvariant(previous);
                        Vector2 c1 = p == 'C' || p == 'S' ? pos * 2f - lastControl : pos;
                        Vector2 c2 = o + reader.Point(), end = o + reader.Point();
                        Cubic(current, pos, c1, c2, end);
                        lastControl = c2;
                        pos = end;
                        break;
                    }
                    case 'Q':
                    {
                        Vector2 q = o + reader.Point(), end = o + reader.Point();
                        Cubic(current, pos, pos + (q - pos) * (2f / 3f), end + (q - end) * (2f / 3f), end);
                        lastControl = q;
                        pos = end;
                        break;
                    }
                    case 'A':
                    {
                        float rx = reader.Number(), ry = reader.Number(), rotation = reader.Number();
                        bool large = reader.Flag(), sweep = reader.Flag();
                        Vector2 end = o + reader.Point();
                        Arc(current, pos, end, rx, ry, rotation, large, sweep);
                        pos = end;
                        break;
                    }
                    case 'Z':
                        shape.closed[shape.closed.Count - 1] = true;
                        pos = start;
                        break;
                    default:
                        throw new FormatException("Unsupported path command " + command);
                }
                previous = c == 'M' ? 'M' : command;
            }
            // A closing point that repeats the first one would make a zero length edge.
            for (int i = 0; i < shape.parts.Count; ++i)
            {
                List<Vector2> part = shape.parts[i];
                if (shape.closed[i] && part.Count > 2 && (part[0] - part[part.Count - 1]).sqrMagnitude < 1e-6f)
                    part.RemoveAt(part.Count - 1);
            }
            _cache[d] = shape;
            return shape;
        }

        private static void Cubic(List<Vector2> into, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            const int steps = 12;
            for (int i = 1; i <= steps; ++i)
            {
                float t = i / (float)steps, u = 1f - t;
                into.Add(u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3);
            }
        }

        /// <summary>The endpoint arc of the SVG spec, converted to its centre form and sampled.</summary>
        private static void Arc(List<Vector2> into, Vector2 p0, Vector2 p1, float rx, float ry, float rotation, bool large, bool sweep)
        {
            if (rx == 0f || ry == 0f)
            {
                into.Add(p1);
                return;
            }
            rx = Mathf.Abs(rx);
            ry = Mathf.Abs(ry);
            float phi = rotation * Mathf.Deg2Rad, cos = Mathf.Cos(phi), sin = Mathf.Sin(phi);
            Vector2 h = (p0 - p1) * 0.5f;
            float x1 = cos * h.x + sin * h.y, y1 = -sin * h.x + cos * h.y;
            float lambda = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry);
            if (lambda > 1f)
            {
                float s = Mathf.Sqrt(lambda);
                rx *= s;
                ry *= s;
            }
            float num = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1;
            float den = rx * rx * y1 * y1 + ry * ry * x1 * x1;
            float k = Mathf.Sqrt(Mathf.Max(0f, num / den)) * (large == sweep ? -1f : 1f);
            float cx1 = k * rx * y1 / ry, cy1 = -k * ry * x1 / rx;
            Vector2 mid = (p0 + p1) * 0.5f;
            Vector2 centre = new Vector2(cos * cx1 - sin * cy1 + mid.x, sin * cx1 + cos * cy1 + mid.y);
            float a0 = Mathf.Atan2((y1 - cy1) / ry, (x1 - cx1) / rx);
            float a1 = Mathf.Atan2((-y1 - cy1) / ry, (-x1 - cx1) / rx);
            float delta = a1 - a0;
            if (sweep && delta < 0f)
                delta += Mathf.PI * 2f;
            else if (sweep == false && delta > 0f)
                delta -= Mathf.PI * 2f;
            int steps = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(delta) / (Mathf.PI / 12f)));
            for (int i = 1; i <= steps; ++i)
            {
                float a = a0 + delta * i / steps;
                float ex = rx * Mathf.Cos(a), ey = ry * Mathf.Sin(a);
                into.Add(new Vector2(cos * ex - sin * ey + centre.x, sin * ex + cos * ey + centre.y));
            }
        }

        private class Reader
        {
            private readonly string _s;
            private int _i;

            public Reader(string s)
            {
                _s = s;
            }

            private void Skip()
            {
                while (_i < _s.Length && (char.IsWhiteSpace(_s[_i]) || _s[_i] == ','))
                    ++_i;
            }

            public bool More()
            {
                Skip();
                return _i < _s.Length;
            }

            public bool PeekCommand()
            {
                Skip();
                return _i < _s.Length && char.IsLetter(_s[_i]) && _s[_i] != 'e' && _s[_i] != 'E';
            }

            public char Command()
            {
                Skip();
                return _s[_i++];
            }

            public bool Flag()
            {
                Skip();
                return _s[_i++] == '1';
            }

            public Vector2 Point()
            {
                float x = Number();
                return new Vector2(x, Number());
            }

            public float Number()
            {
                Skip();
                int start = _i;
                if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+'))
                    ++_i;
                bool dot = false;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (char.IsDigit(c))
                        ++_i;
                    else if (c == '.' && dot == false)
                    {
                        dot = true;
                        ++_i;
                    }
                    else if ((c == 'e' || c == 'E') && _i + 1 < _s.Length)
                    {
                        ++_i;
                        if (_s[_i] == '-' || _s[_i] == '+')
                            ++_i;
                    }
                    else
                        break;
                }
                return float.Parse(_s.Substring(start, _i - start), CultureInfo.InvariantCulture);
            }
        }
    }
}
