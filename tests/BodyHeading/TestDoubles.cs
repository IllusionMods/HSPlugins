// System.Numerics supplies independent managed quaternion math for these tests.
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
    }

    public struct Quaternion
    {
        private System.Numerics.Quaternion value;
        public float x => value.X;
        public float y => value.Y;
        public float z => value.Z;
        public float w => value.W;
        public Quaternion(float x, float y, float z, float w) { value = new System.Numerics.Quaternion(x, y, z, w); }
        private Quaternion(System.Numerics.Quaternion value) { this.value = value; }
        public static Quaternion identity => new Quaternion(System.Numerics.Quaternion.Identity);
        public static Quaternion AngleAxis(float degrees, Vector3 axis) => new Quaternion(System.Numerics.Quaternion.CreateFromAxisAngle(new System.Numerics.Vector3(axis.x, axis.y, axis.z), degrees * MathF.PI / 180));
        public static Quaternion Euler(float x, float y, float z) => AngleAxis(y, Vector3.up) * AngleAxis(x, new Vector3(1, 0, 0)) * AngleAxis(z, Vector3.forward);
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => new Quaternion(System.Numerics.Quaternion.Slerp(a.value, b.value, t));
        public static float Angle(Quaternion a, Quaternion b) => 2 * MathF.Acos(Math.Clamp(MathF.Abs(System.Numerics.Quaternion.Dot(a.value, b.value)), 0, 1)) * 180 / MathF.PI;
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(a.value * b.value);
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            var result = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(v.x, v.y, v.z), q.value);
            return new Vector3(result.X, result.Y, result.Z);
        }
    }

    public static class Mathf
    {
        public const float Rad2Deg = 180 / MathF.PI;
        public static float Sqrt(float value) => MathF.Sqrt(value);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float Clamp01(float value) => Math.Clamp(value, 0, 1);
    }

    public class Transform
    {
        public Transform parent;
        public Quaternion rotation = Quaternion.identity;
        public bool IsChildOf(Transform ancestor)
        {
            for (Transform node = this; node != null; node = node.parent)
                if (node == ancestor) return true;
            return false;
        }
    }

    public class GameObject { public Transform transform = new Transform(); }
}

namespace HSPE
{
    public class PoseController
    {
        public UnityEngine.Transform transform = new UnityEngine.Transform();
        public List<UnityEngine.GameObject> _childObjects = new List<UnityEngine.GameObject>();
    }
}

namespace HSPE.AMModules
{
    public class GenericOCITarget
    {
        public enum Type { Character, Item }
        public Type type = Type.Character;
        public OCIChar ociChar = new OCIChar();
    }

    public class OCIChar { public FinalIK finalIK = new FinalIK(); }
    public class FinalIK { public References references = new References(); }
    public class References { public UnityEngine.Transform pelvis = new UnityEngine.Transform(); }
}
