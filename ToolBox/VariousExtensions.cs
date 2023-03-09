using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
#if !EMOTIONCREATORS
using Studio;
#endif
using UnityEngine;

namespace ToolBox.Extensions {
    internal static class VariousExtensions
    {
        public static void Resize<T>(this List<T> self, int newSize)
        {
            int diff = self.Count - newSize;
            if (diff < 0)
                while (self.Count != newSize)
                    self.Add(default(T));
            else if (diff > 0)
                while (self.Count != newSize)
                    self.RemoveRange(newSize, diff);
        }

        public static int IndexOf<T>(this T[] self, T obj)
        {
            for (int i = 0; i < self.Length; i++)
            {
                if (self[i].Equals(obj))
                    return i;
            }
            return -1;
        }

        public static void AddRange<T, T2>(this IDictionary<T, T2> self, IDictionary<T, T2> toAdd)
        {
            foreach (KeyValuePair<T, T2> pair in toAdd)
                self.Add(pair.Key, pair.Value);
        }

#if !EMOTIONCREATORS
        public static bool IsVisible(this TreeNodeObject self)
        {
            if (self.parent != null)
                return self.visible && self.parent.IsVisible();
            return self.visible;
        }
#endif

        public static int LastIndexOf(this byte[] self, byte[] needle)
        {
            int limit = needle.Length - 1;
            for (int i = self.Length - 1; i > limit; i--)
            {
                int j;
                int i2 = i;
                for (j = needle.Length - 1; j >= 0; --j)
                {
                    if (self[i2] != needle[j])
                        break;
                    --i2;
                }
                if (j == -1)
                    return i2 + 1;
            }
            return -1;
        }

        public static Color GetContrastingColor(this Color self)
        {
            float luminance = 0.299f * self.r + 0.587f * self.g + 0.114f * self.b;
            if (luminance > 0.5f)
                return Color.black;
            return Color.white;
        }

#if KOIKATSU || AISHOUJO || HONEYSELECT2
        private static MethodInfo _initTransforms = null;
        public static void InitTransforms(this DynamicBone self)
        {
            if (_initTransforms == null)
                _initTransforms = self.GetType().GetMethod("InitTransforms", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _initTransforms.Invoke(self, null);
        }

        private static MethodInfo _setupParticles = null;
        public static void SetupParticles(this DynamicBone self)
        {
            if (_setupParticles == null)
                _setupParticles = self.GetType().GetMethod("SetupParticles", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _setupParticles.Invoke(self, null);
        }
#endif

	    public static string RemoveInvalidChars(this string self)
	    {
            StringBuilder builder = new StringBuilder(self);
		    foreach (char c in Path.GetInvalidFileNameChars())
			    builder = builder.Replace(c, '_');
		    return builder.ToString();
	    }

        public static string RelativePath(string from, string to)
        {
            if (from.EndsWith("/") == false && from.EndsWith("\\") == false)
                from += "\\";
            Uri toUri = new Uri(to);
            Uri fromUri = new Uri(from);
            return Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString().Replace('/', '\\'));
        }

        private static readonly char[] _b64 = 
        {
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
            '0','1','2','3','4','5','6','7','8','9', 
            '-','_'
        };

        private static readonly Dictionary<char, byte> _revB64 = new Dictionary<char, byte>()
        {
            {'A', 0}, {'B', 1}, {'C', 2}, {'D', 3}, {'E', 4}, {'F', 5}, {'G', 6}, {'H', 7}, {'I', 8}, {'J', 9}, {'K', 10}, {'L', 11}, {'M', 12}, {'N', 13}, {'O', 14}, {'P', 15}, {'Q', 16}, {'R', 17}, {'S', 18}, {'T', 19}, {'U', 20}, {'V', 21}, {'W', 22}, {'X', 23}, {'Y', 24}, {'Z', 25},
            {'a', 26}, {'b', 27}, {'c', 28}, {'d', 29}, {'e', 30}, {'f', 31}, {'g', 32}, {'h', 33}, {'i', 34}, {'j', 35}, {'k', 36}, {'l', 37}, {'m', 38}, {'n', 39}, {'o', 40}, {'p', 41}, {'q', 42}, {'r', 43}, {'s', 44}, {'t', 45}, {'u', 46}, {'v', 47}, {'w', 48}, {'x', 49}, {'y', 50}, {'z', 51},
            {'0', 52}, {'1', 53}, {'2', 54}, {'3', 55}, {'4', 56}, {'5', 57}, {'6', 58}, {'7', 59}, {'8', 60}, {'9', 61},
            {'-', 62}, {'_', 63}
        };

        public static string ToBase64(this byte[] array)
        {
            StringBuilder builder = new StringBuilder();

            int i = 0;
            while (i < array.Length)
            {
                byte b1 = array[i++];
                int index = b1 >> 2;
                builder.Append(_b64[index]);
                if (i < array.Length)
                {
                    byte b2 = array[i++];
                    index = (b1 & 0b00000011) << 4 | (b2 >> 4);
                    builder.Append(_b64[index]);
                    if (i < array.Length)
                    {
                        byte b3 = array[i++];
                        index = (b2 & 0b00001111) << 2 | (b3 >> 6);
                        builder.Append(_b64[index]);
                        index = b3 & 0b00111111;
                        builder.Append(_b64[index]);
                    }
                }
            }
            return builder.ToString();
        }

        public static byte[] FromBase64(this string s)
        {
            List<byte> array = new List<byte>((s.Length * 3) / 4 + 4);
            int i = 0;
            while (i < s.Length)
            {
                byte i1 = _revB64[s[i++]];
                byte i2 = _revB64[s[i++]];
                byte b = (byte)((i1 << 2) | (i2 >> 4));
                array.Add(b);
                if (i < s.Length)
                {
                    byte i3 = _revB64[s[i++]];
                    b = (byte)((i2 << 4) | (i3 >> 2));
                    array.Add(b);
                    if (i < s.Length)
                    {
                        byte i4 = _revB64[s[i++]];
                        b = (byte)((i3 << 6) | i4);
                        array.Add(b);
                    }
                }
            }
            return array.ToArray();
        }

        public static bool IsReadable(this Texture2D self)
        {
            try
            {
                self.GetPixel(0, 0);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Traverses an object hierarchy and return a flattened list of elements.
        /// </summary>
        public static IEnumerable<TSource> Map<TSource>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TSource>> getChildrenFunction)
        {
            var sourceList = source.ToList();
            var results = sourceList.AsEnumerable();

            foreach (var element in sourceList) 
                results = results.Concat(getChildrenFunction(element).Map(getChildrenFunction));

            return results;
        }
    }

    public delegate void Action<T1, T2, T3, T4, T5>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

    public class HashedPair<T, T2>
    {
        public readonly T key;
        public readonly T2 value;

        private readonly int _hashCode;

        public HashedPair(T key, T2 value)
        {
            this.key = key;
            this.value = value;

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (this.key != null ? this.key.GetHashCode() : 0);
                this._hashCode = hash * 31 + (this.value != null ? this.value.GetHashCode() : 0);
            }
        }

        public override int GetHashCode()
        {
            return this._hashCode;
        }

        public override string ToString()
        {
            return $"key: {this.key}, value: {this.value}";
        }
    }

    public class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _compareFunc;

        public LambdaComparer(Func<T, T, bool> compareFunc)
        {
            this._compareFunc = compareFunc;
        }

        public bool Equals(T x, T y)
        {
            return this._compareFunc(x, y);
        }

        public int GetHashCode(T obj)
        {
            return EqualityComparer<T>.Default.GetHashCode(obj);
        }
    }
}