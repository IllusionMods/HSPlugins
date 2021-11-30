using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            this._hashCode = ComputeHashCode(this.obj);
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
            return this._hashCode;
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
            this._hashCode = ComputeHashCode(this.obj);
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
            return this._hashCode;
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
        public List<INode> tree { get { return this._rootGroup.children; } }
        #endregion

        #region Public Methods
        public LeafNode<TLeaf> AddLeaf(TLeaf obj, GroupNode<TGroup> parent = null)
        {
            LeafNode<TLeaf> node = new LeafNode<TLeaf>(obj);
            if (this._nodes.ContainsKey(node.GetHashCode()) == false)
            {
                this._nodes.Add(node.GetHashCode(), node);
                if (parent != null)
                    parent.children.Add(node);
                else
                    this._rootGroup.children.Add(node);
            }
            return node;
        }

        public GroupNode<TGroup> AddGroup(TGroup obj, GroupNode<TGroup> parent = null)
        {
            GroupNode<TGroup> node = new GroupNode<TGroup>(obj);
            if (this._nodes.ContainsKey(node.GetHashCode()) == false)
            {
                this._nodes.Add(node.GetHashCode(), node);
                if (parent != null)
                    parent.children.Add(node);
                else
                    this._rootGroup.children.Add(node);
            }
            return node;
        }

        public LeafNode<TLeaf> GetLeafNode(TLeaf obj)
        {
            int hashCode = LeafNode<TLeaf>.ComputeHashCode(obj);
            if (this._nodes.TryGetValue(hashCode, out INode node))
                return (LeafNode<TLeaf>)node;
            return null;
        }

        public GroupNode<TGroup> GetGroupNode(TGroup obj)
        {
            int hashCode = GroupNode<TGroup>.ComputeHashCode(obj);
            if (this._nodes.TryGetValue(hashCode, out INode node))
                return (GroupNode<TGroup>)node;
            return null;
        }

        public void Recurse(Action<INode, int> action)
        {
            this.Recurse(this._rootGroup, action);
        }

        public void Recurse(GroupNode<TGroup> group, Action<INode, int> action)
        {
            this.Recurse(group, action, 0);
        }

        public bool Any(Func<LeafNode<TLeaf>, bool> predicate)
        {
            return this.Any(this._rootGroup, predicate);
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
                        if (this.Any((GroupNode<TGroup>)node, predicate))
                            return true;
                        break;
                }
            }
            return false;
        }

        public void RemoveLeaf(TLeaf obj)
        {
            int hashCode = LeafNode<TLeaf>.ComputeHashCode(obj);
            if (this._nodes.TryGetValue(hashCode, out INode node))
                this.Remove(node);
        }

        public void RemoveGroup(TGroup obj)
        {
            int hashCode = GroupNode<TGroup>.ComputeHashCode(obj);
            if (this._nodes.TryGetValue(hashCode, out INode node))
                this.Remove(node);
        }

        public void Remove(INode node)
        {
            this.RemoveInternal(node);
            if (node.type == INodeType.Group)
            {
                foreach (INode child in ((GroupNode<TGroup>)node).children.ToList())
                    this.Remove(child);
            }
            while (node.parent != null && node.parent.children.Count == 0)
            {
                this.Remove(node.parent);
                node = node.parent;
            }
        }

        public void Clear()
        {
            this._rootGroup.children.Clear();
            this._nodes.Clear();
        }

        public void ParentTo(INode node, GroupNode<TGroup> group = null)
        {
            if (node == group)
                return;
            if (node.parent != null)
                node.parent.children.Remove(node);
            else
                this._rootGroup.children.Remove(node);
            node.parent = null;
            if (group != null)
                group.children.Add(node);
            else
                this._rootGroup.children.Add(node);
            node.parent = group;
        }

        public void ParentTo(IEnumerable<INode> nodes, GroupNode<TGroup> group = null)
        {
            foreach (INode node in nodes)
                this.ParentTo(node, group);
        }

        public GroupNode<TGroup> GroupTogether(IEnumerable<TLeaf> leaves, TGroup group)
        {
            List<INode> nodes = new List<INode>();
            foreach (TLeaf leaf in leaves)
                nodes.Add(this._nodes[LeafNode<TLeaf>.ComputeHashCode(leaf)]);

            GroupNode<TGroup> parent = this.AddGroup(group, (GroupNode<TGroup>)nodes[0].parent);
            this.ParentTo(nodes, parent);
            return parent;
        }

        public void MoveUp(INode node)
        {
            List<INode> list = node.parent == null ? this._rootGroup.children : node.parent.children;
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
            nodes = nodes.OrderBy(n => n.parent != null ? n.parent.children.IndexOf(n) : this._rootGroup.children.IndexOf(n));
            foreach (INode node in nodes)
                this.MoveUp(node);
        }

        public void MoveDown(INode node)
        {
            List<INode> list = node.parent == null ? this._rootGroup.children : node.parent.children;
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
            nodes = nodes.OrderBy(n => n.parent != null ? -n.parent.children.IndexOf(n) : -this._rootGroup.children.IndexOf(n));
            foreach (INode node in nodes)
                this.MoveDown(node);
        }
        #endregion

        #region Private Methods
        private void RemoveInternal(INode node)
        {
            if (this._nodes.ContainsKey(node.GetHashCode()))
            {
                this._nodes.Remove(node.GetHashCode());
                if (node.parent != null)
                    node.parent.children.Remove(node);
                else
                    this._rootGroup.children.Remove(node);
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
                        this.Recurse((GroupNode<TGroup>)child, action, depth + 1);
                        break;
                }
            }
        }
        #endregion
    }
}
