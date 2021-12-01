using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UILib
{
    public class MovableWindow : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform _rectTransform;
        private readonly Vector3[] _draggableZoneCorners = new Vector3[4];
        private readonly Vector3[] _limitCorners = new Vector3[4];
        private Vector2 _cachedDragPosition;
        private Vector2 _cachedMousePosition;
        private bool _pointerDownCalled = false;

        public RectTransform toDrag;
        public RectTransform limit;

        public override void Awake()
        {
            base.Awake();
            this._rectTransform = this.GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            this._pointerDownCalled = true;
            this._cachedDragPosition = this.toDrag.position;
            this._cachedMousePosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (this._pointerDownCalled == false)
                return;
            this.toDrag.position = this._cachedDragPosition + (eventData.position - this._cachedMousePosition);
            this.Limit();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (this._pointerDownCalled == false)
                return;
            this.toDrag.position = this._cachedDragPosition + (eventData.position - this._cachedMousePosition);
            this.Limit();
            this._pointerDownCalled = false;
        }

        private void Limit()
        {
            if (this.limit != null)
            {
                this._rectTransform.GetWorldCorners(this._draggableZoneCorners);
                this.limit.GetWorldCorners(this._limitCorners);
                if (this._draggableZoneCorners[0].x < this._limitCorners[0].x)
                    this.toDrag.position += new Vector3(this._limitCorners[0].x - this._draggableZoneCorners[0].x, 0f, 0f);
                if (this._draggableZoneCorners[0].y < this._limitCorners[0].y)
                    this.toDrag.position += new Vector3(0f, this._limitCorners[0].y - this._draggableZoneCorners[0].y, 0f);
                if (this._draggableZoneCorners[2].x > this._limitCorners[2].x)
                    this.toDrag.position += new Vector3(this._limitCorners[2].x - this._draggableZoneCorners[2].x, 0f, 0f);
                if (this._draggableZoneCorners[2].y > this._limitCorners[2].y)
                    this.toDrag.position += new Vector3(0f, this._limitCorners[2].y - this._draggableZoneCorners[2].y, 0f);
            }
        }
    }
}
