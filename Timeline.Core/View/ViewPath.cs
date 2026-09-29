using System;
using System.Collections.Generic;
using Studio;
using Timeline.Graph;
using Timeline.View;
using UnityEngine;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            /// <summary>How much of the scene the path covers: the whole of it, or seconds either side of the playhead.</summary>
            public float pathRange;
            private PathDrawer _pathDrawer;
            private readonly List<PathLine> _paths = new List<PathLine>();
            private int _pathSignature;
            private float _pathSampledAt = -1f;

            internal sealed class PathLine
            {
                public Color color;
                /// <summary>The node itself, whose position right now is marked on the path.</summary>
                public Transform target;
                /// <summary>
                /// The node's parent. Points are kept in its space, so the path follows a parent that
                /// moves (played by another track, or dragged) without being sampled again.
                /// </summary>
                public Transform space;
                public readonly List<Vector3> points = new List<Vector3>();
                public readonly List<float> times = new List<float>();
                public readonly List<Vector3> keys = new List<Vector3>();
                public readonly List<float> keyTimes = new List<float>();
                public readonly List<KeyframeKind> kinds = new List<KeyframeKind>();
                public readonly List<bool> selected = new List<bool>();
            }

            /// <summary>
            /// drawPaths(): where the selected tracks take their node, drawn in the scene. Sampled once
            /// and kept until something that changes it changes, so playing does not resample.
            /// </summary>
            private void TickPath()
            {
                bool on = showPath && visible;
                Camera cam = Camera.main;
                if (on && cam != null && (_pathDrawer == null || _pathDrawer.gameObject != cam.gameObject))
                {
                    if (_pathDrawer != null)
                        UnityEngine.Object.Destroy(_pathDrawer);
                    _pathDrawer = cam.gameObject.AddComponent<PathDrawer>();
                }
                if (_pathDrawer == null)
                    return;
                _pathDrawer.enabled = on;
                if (on == false)
                    return;

                List<Interpolable> tracks = PathTracks();
                int signature = PathSignature(tracks);
                // Sampling sets the track's value once per frame of the scene, so while keys are being
                // dragged it runs a few times a second rather than every frame.
                if (signature != _pathSignature && (Time.unscaledTime - _pathSampledAt > 0.15f || _pathSampledAt < 0f))
                {
                    _pathSignature = signature;
                    _pathSampledAt = Time.unscaledTime;
                    SamplePaths(tracks);
                }
                _pathDrawer.paths = _paths;
                _pathDrawer.time = T._playbackTime;
                _pathDrawer.range = pathRange;
                // Said once on the status line rather than written over the game screen, where it sat
                // on top of Studio's own buttons.
                bool none = tracks.Count == 0;
                if (none && _pathSaidNone == false)
                    Toast("Path is on. Select a track that moves a node to see where it goes.");
                _pathSaidNone = none;
                _pathDrawer.message = null;
            }

            private bool _pathSaidNone;

            /// <summary>The selected tracks that put a node somewhere: positions of guide objects.</summary>
            private List<Interpolable> PathTracks()
            {
                var tracks = new List<Interpolable>();
                var source = new List<Interpolable>(T._selectedInterpolables);
                foreach (KeyValuePair<float, Keyframe> k in T._selectedKeyframes)
                {
                    if (source.Contains(k.Value.parent) == false)
                        source.Add(k.Value.parent);
                }
                foreach (Interpolable tr in source)
                {
                    if (tracks.Count == 4)
                        break;
                    if (PathTarget(tr) != null && tr.keyframes.Count != 0)
                        tracks.Add(tr);
                }
                return tracks;
            }

            private static Transform PathTarget(Interpolable tr)
            {
                if (tr.keyframes.Count == 0 || tr.keyframes.Values[0].value is Vector3 == false)
                    return null;
                GuideObject guide = tr.parameter as GuideObject;
                if (guide != null)
                    return guide.transformTarget;
                // The camera's own position tracks. Other plugins' "Pos" tracks move bones, not the object.
                if (tr.owner == _ownerId && tr.oci != null && tr.oci.guideObject != null && tr.id.IndexOf("Pos", StringComparison.Ordinal) >= 0)
                    return tr.oci.guideObject.transformTarget;
                return null;
            }

            private int PathSignature(List<Interpolable> tracks)
            {
                unchecked
                {
                    int s = 23 + T._desiredFrameRate * 13 + Mathf.RoundToInt(T._duration * 100f);
                    s = s * 31 + _selectedSet.Count;
                    foreach (Interpolable tr in tracks)
                    {
                        s = s * 31 + tr.GetHashCode();
                        foreach (KeyValuePair<float, Keyframe> k in tr.keyframes)
                        {
                            s = s * 31 + k.Key.GetHashCode();
                            s = s * 31 + (k.Value.value == null ? 0 : k.Value.value.GetHashCode());
                            s = s * 31 + (int)k.Value.kind + (k.Value.shapedByCurve ? 1 : 0);
                            if (k.Value.handles != null)
                                s = s * 31 + (int)k.Value.handles.leftType * 5 + (int)k.Value.handles.rightType;
                        }
                    }
                    return s;
                }
            }

            /// <summary>
            /// Puts the track's value on its node for each frame and reads where the node lands, then
            /// puts back what was there. Only this track moves, so a parent that another track moves is
            /// where it is at the time; the playground samples the whole pose. The whole scene is sampled
            /// once, and the range around the playhead is cut from it when drawing.
            /// </summary>
            private void SamplePaths(List<Interpolable> tracks)
            {
                _paths.Clear();
                float a = 0f, b = T._duration;
                int fps = Mathf.Max(1, T._desiredFrameRate);
                int n = Mathf.Clamp(Mathf.RoundToInt((b - a) * fps), 1, 1200);
                foreach (Interpolable tr in tracks)
                {
                    Transform target = PathTarget(tr);
                    object saved = tr.GetValue();
                    var line = new PathLine { color = TrackColor(tr), target = target, space = target.parent };
                    try
                    {
                        for (int i = 0; i <= n; ++i)
                        {
                            float t = a + (b - a) * i / n;
                            line.points.Add(Place(tr, target, t));
                            line.times.Add(t);
                        }
                        foreach (KeyValuePair<float, Keyframe> k in tr.keyframes)
                        {
                            if (k.Key < a - 1e-4f || k.Key > b + 1e-4f)
                                continue;
                            line.keys.Add(Place(tr, target, k.Key));
                            line.keyTimes.Add(k.Key);
                            line.kinds.Add(k.Value.kind);
                            line.selected.Add(_selectedSet.Contains(k.Value));
                        }
                    }
                    finally
                    {
                        Put(tr, saved);
                    }
                    _paths.Add(line);
                }
            }

            private static Vector3 Place(Interpolable tr, Transform target, float t)
            {
                Vector3 v = new Vector3(ValueAt(tr, t, 0), ValueAt(tr, t, 1), ValueAt(tr, t, 2));
                Put(tr, v);
                return target.parent != null ? target.parent.InverseTransformPoint(target.position) : target.position;
            }

            /// <summary>
            /// Sets a value the way playback does: a track may only have the before or only the after
            /// step (HS2PE's bones only have before), and calling a missing one throws.
            /// </summary>
            private static void Put(Interpolable tr, object v)
            {
                if (tr.canInterpolateBefore)
                    tr.InterpolateBefore(v, v, 0f);
                if (tr.canInterpolateAfter)
                    tr.InterpolateAfter(v, v, 0f);
            }
        }
    }

    /// <summary>Draws the motion paths over the scene, after the camera has rendered it.</summary>
    internal class PathDrawer : MonoBehaviour
    {
        public List<Timeline.View.PathLine> paths;
        public float time;
        /// <summary>Seconds either side of the playhead to draw; 0 draws the whole scene.</summary>
        public float range;
        public string message;
        private static Material _material;
        private Camera _camera;
        private GUIStyle _style;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private static Material material
        {
            get
            {
                if (_material == null)
                {
                    _material = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                    _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    _material.SetInt("_ZWrite", 0);
                    _material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                }
                return _material;
            }
        }

        private void OnPostRender()
        {
            if (paths == null || _camera == null)
                return;
            material.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix();
            GL.Begin(GL.QUADS);
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            foreach (Timeline.View.PathLine line in paths)
            {
                Matrix4x4 m = line.space != null ? line.space.localToWorldMatrix : Matrix4x4.identity;
                int count = line.points.Count;
                if (_screen.Length < count)
                    _screen = new Vector3[Mathf.NextPowerOfTwo(count)];
                for (int i = 0; i < count; ++i)
                    _screen[i] = InRange(line.times[i]) ? _camera.WorldToScreenPoint(m.MultiplyPoint3x4(line.points[i])) : new Vector3(0f, 0f, -1f);
                // The played part faint and the part still to come bright, so the direction reads without arrows.
                for (int i = 1; i < count; ++i)
                {
                    Vector3 a = _screen[i - 1], b = _screen[i];
                    if (a.z <= 0f || b.z <= 0f)
                        continue;
                    float alpha = line.times[i] <= time ? 0.35f : 0.9f;
                    Segment(a, b, 2f * scale, WithAlpha(line.color, alpha));
                }
                // One dot per frame: bunched up is slow, spread out is fast.
                for (int i = 0; i < count; ++i)
                {
                    Vector3 p = _screen[i];
                    if (p.z <= 0f)
                        continue;
                    Dot(p, 1.4f * scale, WithAlpha(line.color, line.times[i] < time ? 0.3f : 0.75f));
                }
                for (int i = 0; i < line.keys.Count; ++i)
                {
                    if (InRange(line.keyTimes[i]) == false)
                        continue;
                    Vector3 p = _camera.WorldToScreenPoint(m.MultiplyPoint3x4(line.keys[i]));
                    if (p.z <= 0f)
                        continue;
                    float s = 8f * scale * 0.7071f;
                    if (line.selected[i])
                        Diamond(p, s + 2.8f * scale, Pal.accent);
                    Diamond(p, s + 0.7f * scale, Pal.C(0x111215));
                    Diamond(p, s - 0.7f * scale, line.selected[i] ? Pal.C(0xFFE2B0) : Timeline.View.KindFill(line.kinds[i]));
                }
            }
            GL.End();

            // Where the node is now: a ring that travels along the path as it plays or the playhead moves.
            GL.Begin(GL.TRIANGLES);
            foreach (Timeline.View.PathLine line in paths)
            {
                if (line.target == null)
                    continue;
                Vector3 p = _camera.WorldToScreenPoint(line.target.position);
                if (p.z <= 0f)
                    continue;
                Disc(p, 7f * scale, Pal.C(0x111215));
                Disc(p, 5.5f * scale, Pal.accent);
                Disc(p, 2.5f * scale, Pal.C(0x111215));
            }
            GL.End();
            GL.PopMatrix();
        }

        private static void Disc(Vector3 p, float r, Color color)
        {
            GL.Color(color);
            const int segments = 20;
            for (int i = 0; i < segments; ++i)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                GL.Vertex3(p.x, p.y, 0f);
                GL.Vertex3(p.x + Mathf.Cos(a0) * r, p.y + Mathf.Sin(a0) * r, 0f);
                GL.Vertex3(p.x + Mathf.Cos(a1) * r, p.y + Mathf.Sin(a1) * r, 0f);
            }
        }

        private Vector3[] _screen = new Vector3[256];

        private bool InRange(float t)
        {
            return range <= 0f || Mathf.Abs(t - time) <= range + 1e-4f;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(message) || Event.current.type != EventType.Repaint)
                return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            _style.fontSize = Mathf.RoundToInt(11 * scale);
            _style.normal.textColor = new Color(228f / 255f, 231f / 255f, 236f / 255f, 0.5f);
            GUI.Label(new Rect(14f * scale, 30f * scale, Screen.width, 30f * scale), message, _style);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        private static void Segment(Vector3 a, Vector3 b, float width, Color color)
        {
            Vector2 d = new Vector2(b.x - a.x, b.y - a.y);
            if (d.sqrMagnitude < 1e-6f)
                return;
            d.Normalize();
            Vector2 n = new Vector2(-d.y, d.x) * (width * 0.5f);
            GL.Color(color);
            GL.Vertex3(a.x + n.x, a.y + n.y, 0f);
            GL.Vertex3(b.x + n.x, b.y + n.y, 0f);
            GL.Vertex3(b.x - n.x, b.y - n.y, 0f);
            GL.Vertex3(a.x - n.x, a.y - n.y, 0f);
        }

        private static void Dot(Vector3 p, float r, Color color)
        {
            GL.Color(color);
            GL.Vertex3(p.x - r, p.y - r, 0f);
            GL.Vertex3(p.x + r, p.y - r, 0f);
            GL.Vertex3(p.x + r, p.y + r, 0f);
            GL.Vertex3(p.x - r, p.y + r, 0f);
        }

        private static void Diamond(Vector3 p, float r, Color color)
        {
            GL.Color(color);
            GL.Vertex3(p.x, p.y - r, 0f);
            GL.Vertex3(p.x + r, p.y, 0f);
            GL.Vertex3(p.x, p.y + r, 0f);
            GL.Vertex3(p.x - r, p.y, 0f);
        }
    }
}
