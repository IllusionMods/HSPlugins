using System;
using UnityEngine.EventSystems;

namespace UILib.EventHandlers
{
    public class ScrollHandler : UIBehaviour, IScrollHandler
    {
        public Action<PointerEventData> onScroll;

        public void OnScroll(PointerEventData eventData)
        {
            if (this.onScroll != null)
                this.onScroll(eventData);
        }
    }
}
