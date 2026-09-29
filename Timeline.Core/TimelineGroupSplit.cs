using System.Collections.Generic;
using System.Linq;
using Studio;

namespace Timeline
{
    /// <summary>
    /// Groups are one character's now. An older scene may have a group holding tracks of several; on
    /// load such a group becomes one group per character, same name, each right after the other, so
    /// every group sits under exactly one object in the channel list.
    /// </summary>
    public partial class Timeline
    {
        private int SplitMixedGroups()
        {
            int made = 0;
            SplitIn(_interpolablesTree.tree, null, ref made);
            return made;
        }

        private void SplitIn(List<INode> siblings, GroupNode<InterpolableGroup> parent, ref int made)
        {
            foreach (INode node in siblings.ToList())
            {
                GroupNode<InterpolableGroup> group = node as GroupNode<InterpolableGroup>;
                if (group == null)
                    continue;
                List<ObjectCtrlInfo> owners = OwnersIn(group);
                if (owners.Count > 1)
                {
                    int index = siblings.IndexOf(group);
                    for (int o = 1; o < owners.Count; ++o)
                    {
                        GroupNode<InterpolableGroup> copy = _interpolablesTree.AddGroup(new InterpolableGroup { name = group.obj.name, expanded = group.obj.expanded }, parent);
                        siblings.Remove(copy);
                        siblings.Insert(index + o, copy);
                        MoveOwner(group, copy, owners[o]);
                        ++made;
                    }
                }
                SplitIn(group.children, group, ref made);
            }
        }

        /// <summary>The objects whose tracks are in a group, in the order they first appear.</summary>
        private static List<ObjectCtrlInfo> OwnersIn(GroupNode<InterpolableGroup> group)
        {
            var owners = new List<ObjectCtrlInfo>();
            Collect(group.children, owners);
            return owners;
        }

        private static void Collect(List<INode> nodes, List<ObjectCtrlInfo> owners)
        {
            foreach (INode node in nodes)
            {
                LeafNode<Interpolable> leaf = node as LeafNode<Interpolable>;
                if (leaf != null)
                {
                    if (owners.Contains(leaf.obj.oci) == false)
                        owners.Add(leaf.obj.oci);
                    continue;
                }
                GroupNode<InterpolableGroup> group = node as GroupNode<InterpolableGroup>;
                if (group != null)
                    Collect(group.children, owners);
            }
        }

        /// <summary>Moves one object's part of a group, sub groups and all, into another group.</summary>
        private void MoveOwner(GroupNode<InterpolableGroup> from, GroupNode<InterpolableGroup> to, ObjectCtrlInfo oci)
        {
            foreach (INode child in from.children.ToList())
            {
                LeafNode<Interpolable> leaf = child as LeafNode<Interpolable>;
                if (leaf != null)
                {
                    if (leaf.obj.oci != oci)
                        continue;
                    from.children.Remove(child);
                    to.children.Add(child);
                    child.parent = to;
                    continue;
                }
                GroupNode<InterpolableGroup> sub = child as GroupNode<InterpolableGroup>;
                if (sub == null)
                    continue;
                List<ObjectCtrlInfo> owners = OwnersIn(sub);
                if (owners.Contains(oci) == false)
                    continue;
                if (owners.Count == 1)
                {
                    from.children.Remove(child);
                    to.children.Add(child);
                    child.parent = to;
                }
                else
                {
                    GroupNode<InterpolableGroup> subCopy = _interpolablesTree.AddGroup(new InterpolableGroup { name = sub.obj.name, expanded = sub.obj.expanded }, to);
                    MoveOwner(sub, subCopy, oci);
                }
            }
        }
    }
}
