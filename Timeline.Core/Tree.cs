using System;
using System.Collections.Generic;
using System.Linq;

namespace Timeline
{
    public enum INodeType
    {
        Leaf,
        Group
    }

    public interface INode
    {
        INodeType type { get; }
        IGroupNode parent { get; set; }
    }

    public interface IGroupNode : INode
    {
        List<INode> children { get; set; }
    }

    public class LeafNode<TLeaf> : INode
    {
        public TLeaf obj;
        private readonly int _hashCode;

        public INodeType type { get { return INodeType.Leaf; } }
        public IGroupNode parent { get; set; }

        public LeafNode(TLeaf obj)
        {
            this.obj = obj;
            _hashCode = ComputeHashCode(this.obj);
        }

        internal static int ComputeHashCode(TLeaf obj)
        {
            unchecked
            {
                return (obj != null ? obj.GetHashCode() : 0) * 31 + 1;
            }
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }

    public class GroupNode<TGroup> : IGroupNode
    {
        public TGroup obj;
        private readonly int _hashCode;

        public INodeType type { get { return INodeType.Group; } }
        public IGroupNode parent { get; set; }
        public List<INode> children { get; set; } = new List<INode>();

        public GroupNode(TGroup obj)
        {
            this.obj = obj;
            _hashCode = ComputeHashCode(this.obj);
        }

        internal static int ComputeHashCode(TGroup obj)
        {
            unchecked
            {
                return (obj != null ? obj.GetHashCode() : 0) * 31 + 2;
            }
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

    }

    public class Tree<TLeaf, TGroup>
    {
        #region Types

        #endregion

        #region Private Variables
        private readonly GroupNode<TGroup> _rootGroup = new GroupNode<TGroup>(default);
        private readonly Dictionary<int, INode> _nodes = new Dictionary<int, INode>();
        #endregion

        #region Accessors
        public List<INode> tree { get { return _rootGroup.children; } }
        #endregion

        #region Public Methods
        public LeafNode<TLeaf> AddLeaf(TLeaf obj, GroupNode<TGroup> parent = null)
        {
            LeafNode<TLeaf> node = new LeafNode<TLeaf>(obj);
            if (_nodes.ContainsKey(node.GetHashCode()) == false)
            {
                _nodes.Add(node.GetHashCode(), node);
                if (parent != null)
                {
                    parent.children.Add(node);
                    node.parent = parent;
                }
                else
                {
                    _rootGroup.children.Add(node);
                    node.parent = null;
                }
            }
            return node;
        }

        public GroupNode<TGroup> AddGroup(TGroup obj, GroupNode<TGroup> parent = null)
        {
            GroupNode<TGroup> node = new GroupNode<TGroup>(obj);
            if (_nodes.ContainsKey(node.GetHashCode()) == false)
            {
                _nodes.Add(node.GetHashCode(), node);
                if (parent != null)
                {
                    parent.children.Add(node);
                    node.parent = parent;
                }
                else
                {
                    _rootGroup.children.Add(node);
                    node.parent = null;
                }
            }
            return node;
        }

        public LeafNode<TLeaf> GetLeafNode(TLeaf obj)
        {
            int hashCode = LeafNode<TLeaf>.ComputeHashCode(obj);
            if (_nodes.TryGetValue(hashCode, out INode node))
                return (LeafNode<TLeaf>)node;
            return null;
        }

        public GroupNode<TGroup> GetGroupNode(TGroup obj)
        {
            int hashCode = GroupNode<TGroup>.ComputeHashCode(obj);
            if (_nodes.TryGetValue(hashCode, out INode node))
                return (GroupNode<TGroup>)node;
            return null;
        }

        public void Recurse(Action<INode, int> action)
        {
            Recurse(_rootGroup, action);
        }

        public void Recurse(GroupNode<TGroup> group, Action<INode, int> action)
        {
            Recurse(group, action, 0);
        }

        public bool Any(Func<LeafNode<TLeaf>, bool> predicate)
        {
            return Any(_rootGroup, predicate);
        }

        public bool Any(GroupNode<TGroup> group, Func<LeafNode<TLeaf>, bool> predicate)
        {
            foreach (INode node in group.children)
            {
                switch (node.type)
                {
                    case INodeType.Leaf:
                        if (predicate((LeafNode<TLeaf>)node))
                            return true;
                        break;
                    case INodeType.Group:
                        if (Any((GroupNode<TGroup>)node, predicate))
                            return true;
                        break;
                }
            }
            return false;
        }

        public void RemoveLeaf(TLeaf obj)
        {
            int hashCode = LeafNode<TLeaf>.ComputeHashCode(obj);
            if (_nodes.TryGetValue(hashCode, out INode node))
                Remove(node);
        }

        public void RemoveGroup(TGroup obj)
        {
            int hashCode = GroupNode<TGroup>.ComputeHashCode(obj);
            if (_nodes.TryGetValue(hashCode, out INode node))
                Remove(node);
        }

        public void Remove(INode node)
        {
            RemoveInternal(node);
            if (node.type == INodeType.Group)
            {
                foreach (INode child in ((GroupNode<TGroup>)node).children.ToList())
                    Remove(child);
            }
            while (node.parent != null && node.parent.children.Count == 0)
            {
                Remove(node.parent);
                node = node.parent;
            }
        }

        public void Clear()
        {
            _rootGroup.children.Clear();
            _nodes.Clear();
        }

        public void ParentTo(INode node, GroupNode<TGroup> group = null)
        {
            if (node == group)
                return;

            var parent = node.parent;
            if (parent != null)
            {
                if (group == node.parent)
                    return;

                parent.children.Remove(node);
                node.parent = null;
                if (parent.type == INodeType.Group && parent.children.Count == 0)
                    Remove(parent);
            }
            else
            {
                _rootGroup.children.Remove(node);
            }

            if (group != null)
                group.children.Add(node);
            else
                _rootGroup.children.Add(node);

            node.parent = group;
        }

        public void ParentTo(IEnumerable<INode> nodes, GroupNode<TGroup> group = null)
        {
            foreach (INode node in nodes)
                ParentTo(node, group);
        }

        public GroupNode<TGroup> GroupTogether(IEnumerable<TLeaf> leaves, TGroup group)
        {
            List<INode> nodes = new List<INode>();
            foreach (TLeaf leaf in leaves)
                nodes.Add(_nodes[LeafNode<TLeaf>.ComputeHashCode(leaf)]);

            GroupNode<TGroup> parent = AddGroup(group, (GroupNode<TGroup>)nodes[0].parent);
            ParentTo(nodes, parent);
            return parent;
        }

        public void MoveUp(INode node)
        {
            List<INode> list = node.parent == null ? _rootGroup.children : node.parent.children;
            int index = list.IndexOf(node);
            if (index > 0)
            {
                INode temp = list[index];
                list[index] = list[index - 1];
                list[index - 1] = temp;
            }
        }

        public void MoveUp(IEnumerable<INode> nodes)
        {
            nodes = nodes.OrderBy(n => n.parent != null ? n.parent.children.IndexOf(n) : _rootGroup.children.IndexOf(n));
            foreach (INode node in nodes)
                MoveUp(node);
        }

        public void MoveDown(INode node)
        {
            List<INode> list = node.parent == null ? _rootGroup.children : node.parent.children;
            int index = list.IndexOf(node);
            if (index < list.Count - 1)
            {
                INode temp = list[index];
                list[index] = list[index + 1];
                list[index + 1] = temp;
            }
        }

        public void MoveDown(IEnumerable<INode> nodes)
        {
            nodes = nodes.OrderBy(n => n.parent != null ? -n.parent.children.IndexOf(n) : -_rootGroup.children.IndexOf(n));
            foreach (INode node in nodes)
                MoveDown(node);
        }
        #endregion

        #region Private Methods
        private void RemoveInternal(INode node)
        {
            if (_nodes.ContainsKey(node.GetHashCode()))
            {
                _nodes.Remove(node.GetHashCode());

                //if (node.parent != null)
                //    node.parent.children.Remove(node);
                //else
                //    _rootGroup.children.Remove(node);

                // This brute force approach is required because in some cases the above is not enough (dunno why, I didn't write this class I just fix things)
                RemoveRecursive(_rootGroup, node);
            }
        }

        private static void RemoveRecursive(GroupNode<TGroup> group, INode node)
        {
            for (var index = 0; index < group.children.Count; index++)
            {
                var nodea = group.children[index];
                switch (nodea.type)
                {
                    case INodeType.Leaf:
                        if (nodea.GetHashCode() == node.GetHashCode())
                        {
                            group.children.RemoveAt(index);
                            index--;
                        }
                        break;
                    case INodeType.Group:
                        var groupNode = (GroupNode<TGroup>)nodea;
                        RemoveRecursive(groupNode, node);
                        if (groupNode.children.Count == 0)
                        {
                            group.children.Remove(groupNode);
                            index--;
                        }

                        break;
                }
            }
        }

        private void Recurse(GroupNode<TGroup> group, Action<INode, int> action, int depth)
        {
            action(group, depth);
            foreach (INode child in group.children)
            {
                switch (child.type)
                {
                    case INodeType.Leaf:
                        action(child, depth);
                        break;
                    case INodeType.Group:
                        Recurse((GroupNode<TGroup>)child, action, depth + 1);
                        break;
                }
            }
        }
        #endregion
    }
}
