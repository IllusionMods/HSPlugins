using UnityEngine;
using Vectrosity;
using System.Collections.Generic;

namespace HSPE
{
    public static class Extensions
    {
        public static void SetPoints(this VectorLine self, params Vector3[] points)
        {
            for (int i = 0; i < self.points3.Count; i++)
                self.points3[i] = points[i];
        }

        public static void SetPoints(this VectorLine self, params Vector2[] points)
        {
            for (int i = 0; i < self.points3.Count; i++)
                self.points2[i] = points[i];
        }

        public static void RemoveIfNullKey<Key, Value>(this Dictionary<Key, Value> dict) where Key : UnityEngine.Object
        {
            List<Key> removeKeys = new List<Key>();

            foreach (var key in dict.Keys)
                if ((UnityEngine.Object)key == null)
                    removeKeys.Add(key);

            foreach (var removeKey in removeKeys)
                dict.Remove(removeKey);
        }
    }
}
