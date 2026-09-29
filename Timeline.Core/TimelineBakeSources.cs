using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Studio;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// Bake from something other than keys (the rewrite of ShalltyUtils' baking): a character's Studio
    /// animation, a route, or dynamic bones (hair, skirt, chest) swinging. Each becomes ordinary keys on
    /// the nodes it moved, so the motion can be edited here and comes out the same every time.
    /// </summary>
    public partial class Timeline
    {
        /// <summary>A track being recorded, and how to read its value from the scene.</summary>
        private sealed class Recorded
        {
            public Interpolable track;
            public Func<object> read;
            public readonly SortedList<float, object> samples = new SortedList<float, object>();
        }

        internal bool _bakingLive;

        internal enum BakeSource
        {
            Keys,
            Animation,
            Route,
            DynamicBones,
            Nodes
        }

        /// <summary>Which parts of a character a Studio animation is baked into.</summary>
        [Flags]
        internal enum AnimationParts
        {
            Body = 1,
            Head = 2,
            Fingers = 4
        }

        #region Tracks
        /// <summary>
        /// The position, rotation or scale track of a node, made when new in a group of its own, one
        /// per object, as a group belongs to one object.
        /// </summary>
        private Interpolable NodeTrack(ObjectCtrlInfo oci, GuideObject node, string id, Dictionary<ObjectCtrlInfo, GroupNode<InterpolableGroup>> groups, string groupName)
        {
            if (HasAnySplit(node, _ownerId, id))
                return null;
            InterpolableModel model = GetModel(_ownerId, id);
            if (model == null)
                return null;
            var track = new Interpolable(oci, node, model);
            Interpolable existing;
            if (_interpolables.TryGetValue(track.GetHashCode(), out existing))
                return existing;
            GroupNode<InterpolableGroup> group;
            if (groups.TryGetValue(oci, out group) == false)
                groups[oci] = group = _interpolablesTree.AddGroup(new InterpolableGroup { name = groupName }, null);
            _interpolables.Add(track.GetHashCode(), track);
            _interpolablesTree.AddLeaf(track, group);
            return track;
        }

        /// <summary>The samples replace the keys between the first and the last one, as Bake does.</summary>
        private static void WriteSamples(Recorded r)
        {
            if (r.samples.Count == 0)
                return;
            Interpolable track = r.track;
            float from = r.samples.Keys[0], to = r.samples.Keys[r.samples.Count - 1];
            for (int i = track.keyframes.Count - 1; i >= 0; --i)
            {
                float time = track.keyframes.Keys[i];
                if (time >= from - 0.0001f && time <= to + 0.0001f)
                    track.keyframes.RemoveAt(i);
            }
            foreach (KeyValuePair<float, object> sample in r.samples)
            {
                if (track.keyframes.ContainsKey(sample.Key) == false)
                    track.keyframes.Add(sample.Key, new Keyframe(sample.Value, track, AnimationCurve.Linear(0f, 0f, 1f, 1f)));
            }
            track.smooth = false;
            HandleMath.Convert(track.keyframes);
        }

        private void FinishRecording(List<Recorded> recorded, string what)
        {
            int keys = 0;
            foreach (Recorded r in recorded)
            {
                WriteSamples(r);
                keys += r.samples.Count;
            }
            float end = recorded.Where(r => r.samples.Count != 0).Select(r => r.samples.Keys[r.samples.Count - 1]).DefaultIfEmpty(0f).Max();
            if (end > _duration)
                _duration = end;
            SelectKeyframes();
            UpdateInterpolablesView();
            UpdateGrid();
            RefreshInterpolation();
            Logger.LogMessage("Baked " + what + ": " + keys + " keys on " + recorded.Count + " track(s).");
        }
        #endregion

        #region Studio animation
        /// <summary>
        /// Steps the character's animation frame by frame from the playhead, copies each pose to its FK
        /// and IK nodes the way Studio's "copy from animation" buttons do, and keys them. It goes round
        /// as many loops as asked; given seconds instead, as ShalltyUtils allowed for animations driven
        /// by the scene's time (MMDD), it moves the playhead that long and keys whatever pose it gets.
        /// </summary>
        internal void BakeAnimation(AnimationParts parts, int step, int loops = 1, float seconds = 0f)
        {
            OCIChar character = _selectedOCI as OCIChar;
            if (character == null)
            {
                Logger.LogMessage("Select a character in the scene first.");
                return;
            }
            if (_bakingLive)
                return;
            RecordUndo("Bake animation");
            var groups = new Dictionary<ObjectCtrlInfo, GroupNode<InterpolableGroup>>();
            var recorded = new List<Recorded>();
            if ((parts & AnimationParts.Body) != 0)
            {
                foreach (OCIChar.IKInfo ik in character.listIKTarget)
                {
                    GuideObject node = ik.guideObject;
                    if (node.enablePos)
                        AddRecorded(recorded, NodeTrack(character, node, "guideObjectPos", groups, "Baked animation"));
                    if (node.enableRot)
                        AddRecorded(recorded, NodeTrack(character, node, "guideObjectRot", groups, "Baked animation"));
                }
            }
            foreach (OCIChar.BoneInfo bone in character.listBones)
            {
                OIBoneInfo.BoneGroup g = bone.boneGroup;
                bool head = (g & OIBoneInfo.BoneGroup.Neck) != 0;
                bool fingers = (g & (OIBoneInfo.BoneGroup.RightHand | OIBoneInfo.BoneGroup.LeftHand)) != 0;
                if (head && (parts & AnimationParts.Head) != 0 || fingers && (parts & AnimationParts.Fingers) != 0)
                    AddRecorded(recorded, NodeTrack(character, bone.guideObject, "guideObjectRot", groups, "Baked animation"));
            }
            if (recorded.Count == 0)
            {
                Logger.LogMessage("Nothing to bake: pick at least one part.");
                return;
            }
            StartCoroutine(BakeAnimationRoutine(character, recorded, step, Mathf.Max(1, loops), seconds));
        }

        private static void AddRecorded(List<Recorded> list, Interpolable track)
        {
            if (track != null && list.All(r => r.track != track))
                list.Add(new Recorded { track = track, read = track.GetValue });
        }

        private IEnumerator BakeAnimationRoutine(OCIChar character, List<Recorded> recorded, int step, int loops, float seconds)
        {
            _bakingLive = true;
            Pause();
            foreach (Recorded r in recorded)
                r.track.enabled = false;
            float speed = character.animeSpeed;
            ManipulatePanelCtrl panel = Studio.Studio.Instance.manipulatePanelCtrl;
            bool panelWasActive = panel.active;
            try
            {
                Animator animator = character.charAnimeCtrl.animator;
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                float length = Mathf.Max(0.05f, state.length);
                int fps = Mathf.Max(1, _desiredFrameRate);
                float from = Mathf.Round(_playbackTime * fps) / fps;
                bool timed = seconds > 0f;
                float span = timed ? seconds : length * loops;
                int frames = Mathf.CeilToInt(span * fps);
                character.animeSpeed = 0f;
                MPCharCtrl chara = panel.charaPanelInfo.mpCharCtrl;
                for (int f = 0; f <= frames; f += Mathf.Max(1, step))
                {
                    if (_bakingLive == false)
                        break;
                    float t = Mathf.Min(f / (float)fps, span);
                    if (timed)
                        SeekPlaybackTime(from + t);
                    else
                    {
                        // Round and round: the last frame is the end of the last loop, not its start.
                        float inLoop = t >= span ? length : t % length;
                        animator.Play(state.shortNameHash, 0, inLoop / length);
                        animator.Update(0f);
                    }
                    if (panel.active == false)
                        panel.active = true;
                    chara.fkInfo.buttonAnime.onClick.Invoke();
                    chara.ikInfo.buttonAnime.onClick.Invoke();
                    yield return null;
                    float time = Mathf.Round((from + t) * fps) / fps;
                    foreach (Recorded r in recorded)
                        r.samples[time] = r.read();
                }
            }
            finally
            {
                character.animeSpeed = speed;
                panel.active = panelWasActive;
                foreach (Recorded r in recorded)
                    r.track.enabled = true;
                _bakingLive = false;
            }
            FinishRecording(recorded, "the animation");
        }
        #endregion

        #region Route and dynamic bones: recorded while the scene plays
        /// <summary>Plays the route with the scene and keys where the objects on it go.</summary>
        internal void BakeRoute(int step)
        {
            OCIRoute route = _selectedOCI as OCIRoute;
            if (route == null)
            {
                Logger.LogMessage("Select a route in the Workspace first.");
                return;
            }
            if (((OIRouteInfo)route.objectInfo).loop)
            {
                Logger.LogMessage("Turn off the route's Loop first, or it never ends.");
                return;
            }
            if (_bakingLive)
                return;
            RecordUndo("Bake route");
            var groups = new Dictionary<ObjectCtrlInfo, GroupNode<InterpolableGroup>>();
            var recorded = new List<Recorded>();
            foreach (TreeNodeObject child in route.childNodeRoot.child)
            {
                ObjectCtrlInfo oci;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out oci) == false || oci.guideObject == null)
                    continue;
                Transform t = oci.guideObject.transformTarget;
                Interpolable pos = NodeTrack(oci, oci.guideObject, "guideObjectPos", groups, "Baked route");
                if (pos != null)
                    recorded.Add(new Recorded { track = pos, read = () => t.localPosition });
                Interpolable rot = NodeTrack(oci, oci.guideObject, "guideObjectRot", groups, "Baked route");
                if (rot != null)
                    recorded.Add(new Recorded { track = rot, read = () => t.localRotation });
            }
            if (recorded.Count == 0)
            {
                Logger.LogMessage("That route has nothing on it to bake.");
                return;
            }
            route.Stop();
            route.Play();
            StartCoroutine(RecordWhilePlaying(recorded, step, 0, _duration, () => ((OIRouteInfo)route.objectInfo).active, () => route.Stop(), "the route"));
        }

        /// <summary>
        /// Plays the scene from the playhead to its end and keys the bones the dynamic bones of the
        /// selected character or item swing, through their FK nodes.
        /// </summary>
        internal void BakeDynamicBones(int step, int warmUp)
        {
            List<OCIChar.BoneInfo> bones = FkBonesOf(_selectedOCI);
            if (bones == null)
            {
                Logger.LogMessage("Select a character or an item with FK in the scene first.");
                return;
            }
            if (_bakingLive)
                return;
            HashSet<Transform> swung = DynamicBoneTransforms(_selectedOCI);
            RecordUndo("Bake dynamic bones");
            var groups = new Dictionary<ObjectCtrlInfo, GroupNode<InterpolableGroup>>();
            var recorded = new List<Recorded>();
            foreach (OCIChar.BoneInfo bone in bones)
            {
                Transform t = bone.guideObject.transformTarget;
                if (swung.Contains(t) == false)
                    continue;
                Interpolable rot = NodeTrack(_selectedOCI, bone.guideObject, "guideObjectRot", groups, "Baked dynamic bones");
                if (rot != null)
                    recorded.Add(new Recorded { track = rot, read = () => t.localRotation });
            }
            if (recorded.Count == 0)
            {
                Logger.LogMessage("No enabled dynamic bone moves an FK node of that object.");
                return;
            }
            StartCoroutine(RecordWhilePlaying(recorded, step, warmUp, _duration, () => true, null, "the dynamic bones"));
        }

        /// <summary>
        /// Bake Custom's rewrite: plays the scene and keys where the nodes selected in it really are,
        /// whatever moves them - a constraint, physics, another plugin - from their own transform.
        /// </summary>
        internal void BakeNodes(bool position, bool rotation, bool scale, int step, int warmUp)
        {
            GuideObject[] nodes = GuideObjectManager.Instance == null ? null : GuideObjectManager.Instance.selectObjects;
            if (nodes == null || nodes.Length == 0)
            {
                Logger.LogMessage("Select the nodes to record in the scene first (control + click selects several).");
                return;
            }
            if (_bakingLive)
                return;
            RecordUndo("Bake nodes");
            var groups = new Dictionary<ObjectCtrlInfo, GroupNode<InterpolableGroup>>();
            var recorded = new List<Recorded>();
            foreach (GuideObject node in nodes)
            {
                if (node == null || node.transformTarget == null)
                    continue;
                GuideObject root = node.parentGuide ?? node;
                ObjectCtrlInfo oci = Studio.Studio.Instance.dicObjectCtrl.Values.FirstOrDefault(o => o.guideObject == root);
                if (oci == null)
                    continue;
                Transform t = node.transformTarget;
                if (position && node.enablePos)
                    AddLive(recorded, NodeTrack(oci, node, "guideObjectPos", groups, "Baked nodes"), () => t.localPosition);
                if (rotation && node.enableRot)
                    AddLive(recorded, NodeTrack(oci, node, "guideObjectRot", groups, "Baked nodes"), () => t.localRotation);
                if (scale && node.enableScale)
                    AddLive(recorded, NodeTrack(oci, node, "guideObjectScale", groups, "Baked nodes"), () => t.localScale);
            }
            if (recorded.Count == 0)
            {
                Logger.LogMessage("Those nodes have nothing of that kind to record, or their tracks are split into axes.");
                return;
            }
            StartCoroutine(RecordWhilePlaying(recorded, step, warmUp, _duration, () => true, null, "the nodes"));
        }

        private static void AddLive(List<Recorded> list, Interpolable track, Func<object> read)
        {
            if (track != null && list.All(r => r.track != track))
                list.Add(new Recorded { track = track, read = read });
        }

        private static List<OCIChar.BoneInfo> FkBonesOf(ObjectCtrlInfo oci)
        {
            OCIChar character = oci as OCIChar;
            if (character != null)
                return character.listBones;
            OCIItem item = oci as OCIItem;
            return item != null && item.listBones != null && item.listBones.Count != 0 ? item.listBones : null;
        }

        /// <summary>
        /// Every transform an enabled dynamic bone component moves. Found by name and by their fields,
        /// as the games name the components differently (DynamicBone, DynamicBone_Ver02).
        /// </summary>
        private static HashSet<Transform> DynamicBoneTransforms(ObjectCtrlInfo oci)
        {
            var found = new HashSet<Transform>();
            Transform root = FlipbookRoot(oci);
            if (root == null)
                return found;
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.enabled == false || component.GetType().Name.StartsWith("DynamicBone") == false)
                    continue;
                foreach (FieldInfo field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    object value = field.GetValue(component);
                    if (field.Name == "m_Root" && value is Transform)
                        AddBelow((Transform)value, found);
                    else if (field.Name == "m_Roots" || field.Name == "Bones")
                    {
                        IEnumerable list = value as IEnumerable;
                        if (list == null)
                            continue;
                        foreach (object o in list)
                        {
                            Transform t = o as Transform;
                            if (t != null)
                            {
                                if (field.Name == "m_Roots")
                                    AddBelow(t, found);
                                else
                                    found.Add(t);
                            }
                        }
                    }
                }
            }
            return found;
        }

        private static void AddBelow(Transform t, HashSet<Transform> found)
        {
            foreach (Transform child in t.GetComponentsInChildren<Transform>(true))
                found.Add(child);
        }

        /// <summary>
        /// Plays the scene from the playhead and records each frame once it has been drawn, a key every
        /// few frames, until the end, until keepGoing says no, or until playback is paused. Warming up
        /// plays the whole scene that many times first, so physics has settled when recording starts.
        /// </summary>
        private IEnumerator RecordWhilePlaying(List<Recorded> recorded, int step, int warmUp, float end, Func<bool> keepGoing, Action done, string what)
        {
            _bakingLive = true;
            foreach (Recorded r in recorded)
                r.track.enabled = false;
            int fps = Mathf.Max(1, _desiredFrameRate);
            step = Mathf.Max(1, step);
            float from = _playbackTime;
            float last = from;
            int lastFrame = int.MinValue;
            Logger.LogMessage((warmUp > 0 ? "Warming up, then recording " : "Recording ") + what + ". Pause to stop early.");
            if (isPlaying == false)
                Play();
            try
            {
                // Round the scene warmUp times; it comes back to the start each time.
                for (int loops = 0; loops < warmUp;)
                {
                    yield return new WaitForEndOfFrame();
                    if (_bakingLive == false || isPlaying == false)
                        break;
                    if (_playbackTime < last - 0.0001f)
                        ++loops;
                    last = _playbackTime;
                }
                // Then on to where the recording starts.
                while (warmUp > 0 && _bakingLive && isPlaying && _playbackTime < from - 0.0001f)
                {
                    yield return new WaitForEndOfFrame();
                    last = _playbackTime;
                }
                while (true)
                {
                    yield return new WaitForEndOfFrame();
                    if (_bakingLive == false || isPlaying == false || keepGoing() == false)
                        break;
                    float time = _playbackTime;
                    // Wrapped around to the start: the end has been reached.
                    if (time < last - 0.0001f)
                        break;
                    last = time;
                    int frame = Mathf.RoundToInt(time * fps);
                    if (frame - lastFrame < step)
                        continue;
                    lastFrame = frame;
                    float at = frame / (float)fps;
                    foreach (Recorded r in recorded)
                    {
                        if (r.samples.ContainsKey(at) == false)
                            r.samples.Add(at, r.read());
                    }
                    if (time >= end - 0.0001f)
                        break;
                }
            }
            finally
            {
                Pause();
                if (done != null)
                    done();
                foreach (Recorded r in recorded)
                    r.track.enabled = true;
                _bakingLive = false;
            }
            FinishRecording(recorded, what);
        }
        #endregion
    }
}
