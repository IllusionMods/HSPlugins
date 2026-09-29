using System.Collections.Generic;
using Studio;

namespace Timeline.Nla
{
    /// <summary>One lane of an object's stack, Blender's NLA track: what it is called and how it plays.</summary>
    internal sealed class NlaLane
    {
        public int index;
        public string name = "";
        public bool mute;
        public bool solo;
        public bool locked;

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(name) ? "Track " + (index + 1) : name; }
        }
    }

    /// <summary>
    /// An object's NLA stack: its lanes, evaluated from the lowest up, and how its own keys - the
    /// action, Blender's active action - lie on top of them all.
    /// </summary>
    internal sealed class NlaStack
    {
        public ObjectCtrlInfo owner;
        public readonly List<NlaLane> lanes = new List<NlaLane>();
        /// <summary>How the keys not in any strip blend over the strips. Replace, at full influence, as in Blender.</summary>
        public StripBlendMode actionBlend = StripBlendMode.Replace;
        public float actionInfluence = 1f;

        public NlaLane Lane(int index)
        {
            foreach (NlaLane lane in lanes)
            {
                if (lane.index == index)
                    return lane;
            }
            var made = new NlaLane { index = index };
            lanes.Add(made);
            return made;
        }

        /// <summary>A lane plays unless muted, or unless another lane of the stack is soloed and it is not.</summary>
        public bool Plays(int index)
        {
            bool anySolo = false;
            NlaLane own = null;
            foreach (NlaLane lane in lanes)
            {
                if (lane.solo)
                    anySolo = true;
                if (lane.index == index)
                    own = lane;
            }
            if (own != null && own.mute)
                return false;
            return anySolo == false || own != null && own.solo;
        }

        public bool IsDefault
        {
            get
            {
                if (actionBlend != StripBlendMode.Replace || actionInfluence < 0.999f)
                    return false;
                foreach (NlaLane lane in lanes)
                {
                    if (lane.mute || lane.solo || lane.locked || string.IsNullOrEmpty(lane.name) == false)
                        return false;
                }
                return true;
            }
        }
    }
}
