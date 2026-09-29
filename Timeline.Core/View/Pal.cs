using System.Collections.Generic;
using UnityEngine;

namespace Timeline.View
{
    /// <summary>
    /// Colours, exactly as the playground computes them.
    ///
    /// The playground's stylesheet is written in one reference palette (the amber one), and every colour
    /// in it goes through themed() on its way to the screen. This is that function. Writing the view in
    /// the same reference hex values the stylesheet uses is what keeps the two identical: a colour here is
    /// looked up by the value it has there, not by a name that has to be kept in step by hand.
    /// </summary>
    internal static class Pal
    {
        private const uint _refBg = 0x21242A, _refAccent = 0xE8A33D, _refText = 0xE4E7EC, _refPlayhead = 0xE8483C;
        /// <summary>Colours that mean something of their own and never follow the theme.</summary>
        private static readonly HashSet<uint> _fixed = new HashSet<uint> { 0x82CC63, 0xF08A7E };

        private static Color _bg = Hex(_refBg), _accent = Hex(_refAccent), _text = Hex(_refText), _playhead = Hex(_refPlayhead);
        private static bool _isReference = true;
        private static readonly Dictionary<uint, Color> _cache = new Dictionary<uint, Color>();

        public static int version { get; private set; }

        public static void Set(Color background, Color accent, Color text, Color playhead)
        {
            _bg = Opaque(background);
            _accent = Opaque(accent);
            _text = Opaque(text);
            _playhead = Opaque(playhead);
            _isReference = Same(_bg, _refBg) && Same(_accent, _refAccent) && Same(_text, _refText) && Same(_playhead, _refPlayhead);
            _cache.Clear();
            ++version;
        }

        /// <summary>A stylesheet colour, themed.</summary>
        public static Color C(uint rgb)
        {
            Color c;
            if (_cache.TryGetValue(rgb, out c))
                return c;
            c = Themed(rgb);
            _cache[rgb] = c;
            return c;
        }

        /// <summary>A stylesheet colour with an alpha, the way rgba() values are written there.</summary>
        public static Color C(uint rgb, float alpha)
        {
            Color c = C(rgb);
            c.a = alpha;
            return c;
        }

        /// <summary>rgba(232,163,61,a) in the stylesheet: the accent at some opacity.</summary>
        public static Color Accent(float alpha)
        {
            Color c = _accent;
            c.a = alpha;
            return c;
        }

        public static Color accent { get { return _accent; } }
        public static Color playhead { get { return _playhead; } }

        /// <summary>Text drawn on the accent: dark on a light accent, white on a dark one.</summary>
        public static Color onAccent { get { return Lum(_accent) > 135f / 255f ? C(0x1B1D21) : C(0xFFFFFF); } }

        /// <summary>A colour that is not themed at all: axes, key types, track colours.</summary>
        public static Color Hex(uint rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        public static string Html(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c);
        }

        private static Color Themed(uint d)
        {
            if (_fixed.Contains(d) || _isReference)
                return Hex(d);
            if (d == _refAccent)
                return _accent;
            if (d == 0xF0B558)
                return Mix(_accent, Color.white, 0.18f);
            if (d == 0xFFE2B0)
                return Mix(_accent, Color.white, 0.65f);
            if (d == _refPlayhead)
                return _playhead;

            float lb = Lum(Hex(_refBg)), lt = Lum(Hex(_refText)), ld = Lum(Hex(d));
            // Darker than the background moves away from the text colour, lighter moves towards it, so a
            // light theme works too.
            if (ld < lb)
                return Mix(_bg, Lum(_text) > Lum(_bg) ? Color.black : Color.white, 1f - ld / lb);
            return Mix(_bg, _text, Mathf.Min(1.1f, (ld - lb) / (lt - lb)));
        }

        private static Color Mix(Color a, Color b, float t)
        {
            // Unclamped, like the playground's mix(): t can run to 1.1, and toHex clamps afterwards.
            return new Color(Mathf.Clamp01(a.r + (b.r - a.r) * t), Mathf.Clamp01(a.g + (b.g - a.g) * t),
                             Mathf.Clamp01(a.b + (b.b - a.b) * t), 1f).Rounded();
        }

        private static Color Rounded(this Color c)
        {
            return new Color(Mathf.Round(c.r * 255f) / 255f, Mathf.Round(c.g * 255f) / 255f, Mathf.Round(c.b * 255f) / 255f, c.a);
        }

        private static float Lum(Color c)
        {
            return c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
        }

        private static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        private static bool Same(Color c, uint rgb)
        {
            return ColorUtility.ToHtmlStringRGB(c) == ColorUtility.ToHtmlStringRGB(Hex(rgb));
        }
    }
}
