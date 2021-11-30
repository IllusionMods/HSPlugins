using System;
using UnityEngine.EventSystems;

namespace UILib.EventHandlers
{
    public class PointerDownHandler : UIBehaviour, IPointerDownHandler
    {
        public Action<PointerEventData> onPointerDown;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (this.onPointerDown != null)
                this.onPointerDown(eventData);
        }
    }
}
