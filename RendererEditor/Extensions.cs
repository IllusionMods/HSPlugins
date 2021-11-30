using UnityEngine;
using Vectrosity;

namespace RendererEditor
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

    }
}
