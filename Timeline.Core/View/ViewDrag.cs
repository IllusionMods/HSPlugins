using System.Collections.Generic;
using System.Linq;
using Studio;
using Timeline.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            /// <summary>
            /// Rows dragged in the channel list: before or after another row, or into a group. Listed by
            /// object, only within the object they belong to, as each object shows its own part of a group;
            /// listed as grouped, anywhere, so one group can hold tracks of several objects.
            /// </summary>
            private sealed class RowDrag
            {
                public List<INode> nodes;
                public object owner;
                public Row target;
                public string zone;
                public bool valid;
            }

            private RowDrag _rowDrag;
            private Image _dropLine;
            private Image _dropInto;

            private static object OwnerKey(ObjectCtrlInfo oci)
            {
                return oci ?? _sceneKey;
            }

            /// <summary>Which character a row belongs to, as the key the object rows use.</summary>
            private object RowOwner(Row r)
            {
                switch (r.type)
                {
                    case RowType.Track:
                        return OwnerKey(r.tr.oci);
                    case RowType.Object:
                        return r.key;
                    case RowType.Group:
                        return r.tracks.Count == 0 ? null : OwnerKey(r.tracks[0].oci);
                    default:
                        return null;
                }
            }

            public void RowBeginDragFor(object row, PointerEventData e)
            {
                RowBeginDrag(row as Row, e);
            }

            private void RowBeginDrag(Row r, PointerEventData e)
            {
                if (r == null || editor == "nla")
                    return;
                var nodes = new List<INode>();
                object owner;
                if (r.type == RowType.Track)
                {
                    owner = OwnerKey(r.tr.oci);
                    // A selected track brings the rest of the selection of the same character with it.
                    IEnumerable<Interpolable> tracks = T._selectedInterpolables.Contains(r.tr)
                            ? _rows.Where(x => x.type == RowType.Track && T._selectedInterpolables.Contains(x.tr) && (Grouped || OwnerKey(x.tr.oci) == owner)).Select(x => x.tr)
                            : new[] { r.tr };
                    foreach (Interpolable t in tracks)
                    {
                        LeafNode<Interpolable> leaf = T._interpolablesTree.GetLeafNode(t);
                        if (leaf != null)
                            nodes.Add(leaf);
                    }
                }
                else if (r.type == RowType.Group)
                {
                    owner = RowOwner(r);
                    nodes.Add(r.group);
                }
                else
                    return;
                if (nodes.Count == 0)
                    return;
                _rowDrag = new RowDrag { nodes = nodes, owner = owner };
                EnsureDropMarks();
            }

            private void EnsureDropMarks()
            {
                if (_dropLine == null)
                {
                    _dropLine = Kit.Box("Dropline", _rowsClip, Pal.accent, 1f);
                    _dropInto = Kit.Box("Dropinto", _rowsClip, Pal.Accent(0.18f));
                    Kit.Ring("Ring", _dropInto.transform, Pal.accent, 0f);
                }
                _dropLine.transform.SetAsLastSibling();
                _dropInto.transform.SetAsLastSibling();
            }

            public void RowDragMove(PointerEventData e)
            {
                if (_rowDrag == null)
                    return;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_rowsClip, e.position, null, out local);
                Rect rect = _rowsClip.rect;
                float y = rect.yMax - local.y;
                // Near the top or bottom edge the list scrolls, so a row can be dragged anywhere in it.
                if (y < 16f)
                    SetScroll(scrollY - 6f);
                else if (y > rect.height - 16f)
                    SetScroll(scrollY + 6f);

                float yy = y + scrollY;
                Row target = _rows.FirstOrDefault(x => yy >= x.y && yy < x.y + x.h);
                _rowDrag.target = target;
                _rowDrag.valid = false;
                _dropLine.gameObject.SetActive(false);
                _dropInto.gameObject.SetActive(false);
                if (target == null)
                    return;
                float frac = (yy - target.y) / target.h;
                string zone;
                if (target.type == RowType.Group)
                    zone = frac < 0.25f ? "before" : frac > 0.75f ? "after" : "into";
                else if (target.type == RowType.Object)
                    zone = "into";
                else if (target.type == RowType.Track)
                    zone = frac < 0.5f ? "before" : "after";
                else
                    return;
                _rowDrag.zone = zone;
                bool valid = (Grouped || RowOwner(target) == _rowDrag.owner) && Contains(target) == false;
                _rowDrag.valid = valid;

                float top = target.y - scrollY;
                if (zone == "into" || valid == false)
                {
                    _dropInto.gameObject.SetActive(true);
                    _dropInto.rectTransform.Css(0f, top, 0f, float.NaN, float.NaN, target.h);
                    _dropInto.color = valid ? Pal.Accent(0.18f) : new Color(0f, 0f, 0f, 0f);
                    _dropInto.transform.Find("Ring").GetComponent<Image>().color = valid ? Pal.accent : Pal.Hex(0xE85A5C);
                }
                else
                {
                    _dropLine.gameObject.SetActive(true);
                    float lineY = zone == "before" ? top : top + target.h;
                    _dropLine.rectTransform.Css(6f, lineY - 1f, 6f, float.NaN, float.NaN, 2f);
                }
            }

            /// <summary>A group can not go into itself or anything inside it.</summary>
            private bool Contains(Row target)
            {
                foreach (INode node in _rowDrag.nodes)
                {
                    GroupNode<InterpolableGroup> group = node as GroupNode<InterpolableGroup>;
                    if (group == null)
                        continue;
                    if (target.group == group)
                        return true;
                    INode probe = target.type == RowType.Track ? (INode)T._interpolablesTree.GetLeafNode(target.tr) : target.group;
                    for (IGroupNode p = probe == null ? null : probe.parent; p != null; p = p.parent)
                    {
                        if (p == group)
                            return true;
                    }
                }
                return false;
            }

            public void RowEndDrag(PointerEventData e)
            {
                if (_rowDrag == null)
                    return;
                RowDrag d = _rowDrag;
                _rowDrag = null;
                _dropLine.gameObject.SetActive(false);
                _dropInto.gameObject.SetActive(false);
                if (d.target == null || d.valid == false)
                {
                    if (d.target != null)
                        Toast("Listed by object, tracks move within their own object. View › List tracks › As grouped lets one group hold several.");
                    return;
                }

                IGroupNode parent;
                int index;
                Row t = d.target;
                if (d.zone == "into")
                {
                    parent = t.type == RowType.Group ? t.group : null;
                    index = -1;
                }
                else
                {
                    INode anchor = t.type == RowType.Track ? (INode)T._interpolablesTree.GetLeafNode(t.tr) : t.group;
                    if (anchor == null || d.nodes.Contains(anchor))
                        return;
                    parent = anchor.parent;
                    List<INode> list = parent == null ? T._interpolablesTree.tree : parent.children;
                    index = list.IndexOf(anchor) + (d.zone == "after" ? 1 : 0);
                }

                List<INode> into = parent == null ? T._interpolablesTree.tree : parent.children;
                // Taken out first, then put back in order; the index moves down for each one taken from
                // above it in the same list.
                foreach (INode node in d.nodes)
                {
                    List<INode> from = node.parent == null ? T._interpolablesTree.tree : node.parent.children;
                    int at = from.IndexOf(node);
                    if (at < 0)
                        continue;
                    if (from == into && index >= 0 && at < index)
                        --index;
                    from.RemoveAt(at);
                }
                foreach (INode node in d.nodes)
                {
                    if (index < 0 || index > into.Count)
                        into.Add(node);
                    else
                        into.Insert(index++, node);
                    node.parent = parent;
                }
                if (parent is GroupNode<InterpolableGroup>)
                    ((GroupNode<InterpolableGroup>)parent).obj.expanded = true;
                T.UpdateInterpolablesView();
                _rowsDirty = true;
                ++_rowsVersion;
                Touch();
            }
        }
    }

    /// <summary>Hands a row's drag to the view.</summary>
    internal class RowDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Timeline.View view;
        public System.Func<object> row;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                view.RowBeginDragFor(row(), eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            view.RowDragMove(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            view.RowEndDrag(eventData);
        }
    }
}
