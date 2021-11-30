using System;
using UnityEngine.EventSystems;

namespace UILib.EventHandlers
{
    public class DragHandler : UIBehaviour, IBeginDragHandler, IInitializePotentialDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<PointerEventData> onBeginDrag;
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (this.onBeginDrag != null)
                this.onBeginDrag(eventData);
        }

        public Action<PointerEventData> onInitializePotentialDrag;
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (this.onInitializePotentialDrag != null)
                this.onInitializePotentialDrag(eventData);
        }

        public Action<PointerEventData> onDrag;
        public void OnDrag(PointerEventData eventData)
        {
            if (this.onDrag != null)
                this.onDrag(eventData);
        }

        public Action<PointerEventData> onEndDrag;
        public void OnEndDrag(PointerEventData eventData)
        {
            if (this.onEndDrag != null)
                this.onEndDrag(eventData);
        }
    }
}
