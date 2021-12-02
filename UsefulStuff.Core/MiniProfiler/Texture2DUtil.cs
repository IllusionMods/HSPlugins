using UnityEngine;

namespace kleberswf.tools.util
{
    public static class Texture2DUtil
    {
        private static Color32[] _background;

        /// <summary>
        /// Clear an entire texture with a given color.
        /// </summary>
        /// <param name="texture">Texture2D to be cleared.</param>
        /// <param name="color">Color that all the pixels of the texture will have.</param>
        public static void Clear(Texture2D texture, Color32 color)
        {
            if (texture == null) return;
            var size = texture.width * texture.height;
            if (_background == null || _background.Length != size) _background = new Color32[size];
            if (size == 0) return;
            for (var i = 0; i < _background.Length; i++)
                _background[i] = color;
            texture.SetPixels32(_background);
            texture.Apply();
        }
    }
}