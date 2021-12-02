using System;
using UnityEngine;

namespace kleberswf.tools.util
{
    public class Graphic
    {
        private readonly Color32[] _background;
        private readonly Color32[] _data;

        private readonly Texture2D _texture;
        public Texture2D Texture { get { return _texture; } }

        private readonly int _width;
        private readonly int _size;

        public Graphic(int width, int height, Color32 color)
        {
            _width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);
            _texture = new Texture2D(_width, height)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            _size = _width * height;
            _background = new Color32[_size];
            _data = new Color32[_size];
            Clear(color, true);
        }

        private static bool Color32Equal(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        public void Clear(Color32 color, bool apply)
        {
            if (Color32Equal(color, _background[0]) == false)
                SetBackgroundColor(color);

            Array.Copy(_background, _data, _size);
            if (apply) Apply();
        }

        public void DrawRect(int x, int y, int with, int height, Color32 color, int average, Color32 averageColor)
        {
            var w = x + with;
            for (; x < w; x++)
            {
                var yy = y;
                for (; yy < height; yy++) _data[yy * _width + x] = color;
                _data[average * _width + x] = averageColor;
                //_data[(average + 1) * _width + x] = averageColor;
            }
        }

        public void SetBackgroundColor(Color32 color) { for (var i = 0; i < _size; i++) _background[i] = color; }

        public void Apply()
        {
            _texture.SetPixels32(_data);
            _texture.Apply();
        }

        public void Destroy() { GameObject.DestroyImmediate(_texture); }
    }
}