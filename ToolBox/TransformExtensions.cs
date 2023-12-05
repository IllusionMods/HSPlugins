using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ToolBox.Extensions {
    internal static class TransformExtensions
    {
        public static string GetPathFrom(this Transform self, Transform root, bool includeRoot = false)
        {
            if (self == root)
                return "";
            Transform self2 = self;
            StringBuilder path = new StringBuilder(self2.name);
            self2 = self2.parent;
            while (self2 != null && self2 != root)
            {
                path.Insert(0, "/");
                path.Insert(0, self2.name);
                self2 = self2.parent;
            }
            if (self2 != null && includeRoot)
            {
                path.Insert(0, "/");
                path.Insert(0, root.name);
            }
            return path.ToString();
        }

        public static bool IsChildOf(this Transform self, string parent)
        {
            while (self != null)
            {
                if (self.name.Equals(parent))
                    return true;
                self = self.parent;
            }
            return false;
        }

        public static string GetPathFrom(this Transform self, string root, bool includeRoot = false)
        {
            if (self.name.Equals(root))
                return "";
            Transform self2 = self;
            StringBuilder path = new StringBuilder(self2.name);
            self2 = self2.parent;
            while (self2 != null && self2.name.Equals(root) == false)
            {
                path.Insert(0, "/");
                path.Insert(0, self2.name);
                self2 = self2.parent;
            }
            if (self2 != null && includeRoot)
            {
                path.Insert(0, "/");
                path.Insert(0, root);
            }
            return path.ToString();
        }

        public static List<int> GetListPathFrom(this Transform self, Transform root)
        {
            List<int> path = new List<int>();
            Transform self2 = self;
            while (self2 != root)
            {
                path.Add(self2.GetSiblingIndex());
                self2 = self2.parent;
            }
            path.Reverse();
            return path;
        }

        public static Transform Find(this Transform self, List<int> path)
        {
            Transform self2 = self;
            for (int i = 0; i < path.Count; i++)
                self2 = self2.GetChild(path[i]);
            return self2;
        }

        public static Transform FindDescendant(this Transform self, string name)
        {
            if (self.name.Equals(name))
                return self;
            foreach (Transform t in self)
            {
                Transform res = t.FindDescendant(name);
                if (res != null)
                    return res;
            }
            return null;
        }

        public static Transform GetFirstLeaf(this Transform self)
        {
            while (self.childCount != 0)
                self = self.GetChild(0);
            return self;
        }

        public static Transform GetDeepestLeaf(this Transform self)
        {
            int d = -1;
            Transform res = null;
            foreach (Transform transform in self)
            {
                int resD;
                Transform resT = GetDeepestLeaf(transform, 0, out resD);
                if (resD > d)
                {
                    d = resD;
                    res = resT;
                }
            }
            return res;
        }

        private static Transform GetDeepestLeaf(Transform t, int depth, out int resultDepth)
        {
            if (t.childCount == 0)
            {
                resultDepth = depth;
                return t;
            }
            Transform res = null;
            int d = 0;
            foreach (Transform child in t)
            {
                int resD;
                Transform resT = GetDeepestLeaf(child, depth + 1, out resD);
                if (resD > d)
                {
                    d = resD;
                    res = resT;
                }
            }
            resultDepth = d;
            return res;
        }

#if !AISHOUJO && !HONEYSELECT2
        public static IEnumerable<Transform> Children(this Transform self)
        {
            foreach (Transform t in self)
                yield return t;
        }
#endif
    }
}